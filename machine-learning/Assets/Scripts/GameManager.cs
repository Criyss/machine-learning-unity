using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

// Controla el flujo completo del juego: rondas, spawn de células, puntaje y el sistema de machine learning que les permite camuflarse
public class GameManager : MonoBehaviour
{
    public GameObject cellPrefab;
    public Transform spawnAreaMin; // esquina inferior-izquierda del área donde pueden aparecer células
    public Transform spawnAreaMax; // esquina superior-derecha del área
    public Sprite[] variedadesSprites; // sprites de perro/gato, se elige uno al azar por célula

    public TMP_Text textoTiempo;
    public TMP_Text textoPuntaje;

    public float duracionRonda = 10f;
    public int celulasPorRonda = 12;

    // --- Parámetros del algoritmo de aprendizaje ---
    [Range(0f, 1f)]
    public float probabilidadExplorarInicial = 0.4f; // % de células al azar al inicio (búsqueda amplia)

    [Range(0f, 1f)]
    public float probabilidadExplorarMinima = 0.08f; // nunca deja de explorar del todo
    public float fuerzaMutacion = 0.4f; // que tanto puede variar el color/tamaño respecto al "exitoso"

    [Range(0.5f, 1f)]
    public float decaimientoMutacion = 0.85f; // factor que reduce mutación y exploración ronda a ronda
    public float fuerzaMutacionMinima = 0.03f;

    [Range(0.1f, 1f)]
    public float tasaAprendizaje = 0.85f; // cuánto pesa lo aprendido en ESTA ronda sobre el historial
    private float probabilidadExplorar; // valor actual, arranca en probabilidadExplorarInicial y decae

    public float tamañoMin = 0.5f;
    public float tamañoMax = 2f;
    public int maxCelulasEnPantalla = 40; // optimización: tope de seguridad para evitar exceso de células

    private int puntaje = 0;
    private float tiempoRestanteRonda;
    private readonly List<GameObject> celulasVivas = new List<GameObject>();

    // "Creencia" actual de la IA sobre qué color/tamaño le sirve para sobrevivir
    private Color colorPromedioExitoso = Color.gray;
    private float tamañoPromedioExitoso = 1f;
    private bool hayHistorial = false; // false hasta que exista al menos una ronda con sobrevivientes

    void Start()
    {
        probabilidadExplorar = probabilidadExplorarInicial;
        ActualizarUIPuntaje();
        StartCoroutine(LoopDeRondas());
    }

    void Update()
    {
        tiempoRestanteRonda -= Time.deltaTime;

        if (textoTiempo != null)
        {
            textoTiempo.text = "Tiempo: " + Mathf.Max(0, Mathf.CeilToInt(tiempoRestanteRonda));
        }
    }

    // Ciclo infinito del juego: spawn -> esperar a que se acabe el tiempo -> evaluar y aprender -> repetir.
    IEnumerator LoopDeRondas()
    {
        while (true)
        {
            tiempoRestanteRonda = duracionRonda;
            SpawnearCelulasDeLaRonda();

            while (tiempoRestanteRonda > 0f)
            {
                yield return null; // espera un frame y vuelve a chequear (permite terminar la ronda apenas llega a 0)
            }

            EvaluarSupervivientesYAprender();
        }
    }

    void SpawnearCelulasDeLaRonda()
    {
        // Optimización: nunca superar el tope de seguridad, sin importar el valor de celulasPorRonda, para evitar lag
        int aSpawnear = Mathf.Min(celulasPorRonda, Mathf.Max(0, maxCelulasEnPantalla - celulasVivas.Count));

        for (int i = 0; i < aSpawnear; i++)
        {
            Vector2 posicion = PosicionAleatoriaEnArea();
            GameObject nuevaCelula = Instantiate(cellPrefab, posicion, Quaternion.identity);

            Cell scriptCelula = nuevaCelula.GetComponent<Cell>();
            (Color colorElegido, float tamañoElegido) = GenerarGenesDeCelula();
            scriptCelula.Inicializar(this, colorElegido, tamañoElegido, ElegirSpriteAleatorio());

            celulasVivas.Add(nuevaCelula);
        }
    }

    Sprite ElegirSpriteAleatorio()
    {
        if (variedadesSprites == null || variedadesSprites.Length == 0) return null;
        return variedadesSprites[Random.Range(0, variedadesSprites.Length)];
    }

    // Núcleo del algoritmo: decide si esta célula "explora" (color/tamaño 100% al azar) o "explota" lo aprendido (muta cerca del color/tamaño que viene funcionando)
    (Color, float) GenerarGenesDeCelula()
    {
        bool explorar = !hayHistorial || Random.value < probabilidadExplorar;

        if (explorar)
        {
            Color colorRandom = new Color(Random.value, Random.value, Random.value);
            float tamañoRandom = Random.Range(tamañoMin, tamañoMax);
            return (colorRandom, tamañoRandom);
        }
        else
        {
            // Mutación: parte del color/tamaño exitoso y le suma un desvío aleatorio limitado por fuerzaMutacion, luego lo recorta a un rango válido (0-1 para color).
            Color colorMutado = new Color(
                Mathf.Clamp01(colorPromedioExitoso.r + Random.Range(-fuerzaMutacion, fuerzaMutacion)),
                Mathf.Clamp01(colorPromedioExitoso.g + Random.Range(-fuerzaMutacion, fuerzaMutacion)),
                Mathf.Clamp01(colorPromedioExitoso.b + Random.Range(-fuerzaMutacion, fuerzaMutacion))
            );
            float tamañoMutado = Mathf.Clamp(
                tamañoPromedioExitoso + Random.Range(-fuerzaMutacion, fuerzaMutacion),
                tamañoMin, tamañoMax
            );
            return (colorMutado, tamañoMutado);
        }
    }

    // Se ejecuta al final de cada ronda: mira quién quedó vivo (no fue clickeado), actualiza la "creencia" de color/tamaño exitoso y reduce mutación/exploración.
    void EvaluarSupervivientesYAprender()
    {
        List<Color> coloresSupervivientes = new List<Color>();
        List<float> tamañosSupervivientes = new List<float>();

        foreach (GameObject celula in celulasVivas)
        {
            if (celula == null) continue;

            Cell scriptCelula = celula.GetComponent<Cell>();
            coloresSupervivientes.Add(scriptCelula.colorActual);
            tamañosSupervivientes.Add(scriptCelula.tamañoActual);

            Destroy(celula);
        }

        if (coloresSupervivientes.Count > 0)
        {
            Color promedioDeEstaRonda = PromediarColores(coloresSupervivientes);
            float tamañoDeEstaRonda = PromediarFloats(tamañosSupervivientes);

            if (!hayHistorial)
            {
                // Primera vez que hay datos: arrancamos directo desde ahí
                colorPromedioExitoso = promedioDeEstaRonda;
                tamañoPromedioExitoso = tamañoDeEstaRonda;
            }
            else
            {
                // Mezcla (interpolación) entre lo aprendido hasta ahora y la evidencia nueva, en vez de reemplazarlo de golpe: tasaAprendizaje controla el peso
                colorPromedioExitoso = Color.Lerp(colorPromedioExitoso, promedioDeEstaRonda, tasaAprendizaje);
                tamañoPromedioExitoso = Mathf.Lerp(tamañoPromedioExitoso, tamañoDeEstaRonda, tasaAprendizaje);
            }
            hayHistorial = true;

            // Cada ronda que hay aprendizaje, la mutación y exploración se reducen (búsqueda amplia al principio, ajuste fino después) hasta sus pisos mínimos
            fuerzaMutacion = Mathf.Max(fuerzaMutacionMinima, fuerzaMutacion * decaimientoMutacion);
            probabilidadExplorar = Mathf.Max(probabilidadExplorarMinima, probabilidadExplorar * decaimientoMutacion);
        }
        celulasVivas.Clear();
    }

    // Llamado desde Cell.cs cuando el jugador hace click sobre una célula
    public void RegistrarEliminacion(GameObject celula)
    {
        puntaje++;
        celulasVivas.Remove(celula);
        ActualizarUIPuntaje();
    }

    void ActualizarUIPuntaje()
    {
        if (textoPuntaje != null)
        {
            textoPuntaje.text = "Puntaje: " + puntaje;
        }
    }

    Vector2 PosicionAleatoriaEnArea()
    {
        float x = Random.Range(spawnAreaMin.position.x, spawnAreaMax.position.x);
        float y = Random.Range(spawnAreaMin.position.y, spawnAreaMax.position.y);
        return new Vector2(x, y);
    }

    // Promedio simple componente a componente (R, G y B por separado)
    Color PromediarColores(List<Color> colores)
    {
        float r = 0, g = 0, b = 0;
        foreach (Color c in colores)
        {
            r += c.r;
            g += c.g;
            b += c.b;
        }
        int n = colores.Count;
        return new Color(r / n, g / n, b / n);
    }

    float PromediarFloats(List<float> valores)
    {
        float suma = 0;
        foreach (float v in valores) suma += v;
        return suma / valores.Count;
    }
}