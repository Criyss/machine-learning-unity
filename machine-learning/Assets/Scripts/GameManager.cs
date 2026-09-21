using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// GameManager: cerebro del juego.
/// - Controla las rondas de 10 segundos.
/// - Hace spawn de células con color/tamaño.
/// - Muestra puntaje y tiempo restante en pantalla.
/// - Guarda qué colores/tamaños "sobrevivieron" (no fueron clickeados)
///   y usa ese historial para que las próximas células se parezcan
///   más a las que sobrevivieron (esto ES el "machine learning": un
///   aprendizaje por refuerzo muy simple, tipo algoritmo evolutivo).
/// </summary>
public class GameManager : MonoBehaviour
{
    
    [Header("Referencias de juego")]
    public GameObject cellPrefab;      
    public Transform spawnAreaMin;   
    public Transform spawnAreaMax;     

    [Header("Referencias de UI (Paso 1: Canvas → TextoTiempo / TextoPuntaje)")]
    public TMP_Text textoTiempo;       
    public TMP_Text textoPuntaje;   

   
    [Header("Parámetros de ronda")]
    public float duracionRonda = 10f;     
    public int celulasPorRonda = 5;      

    [Header("Parámetros de aprendizaje")]
    [Range(0f, 1f)]
    public float probabilidadExplorar = 0.3f; 
                                             
    public float fuerzaMutacion = 0.15f;      

    [Header("Límites de tamaño/color (pedidos en la consigna)")]
    public float tamañoMin = 0.5f;
    public float tamañoMax = 2f;

   
    private int puntaje = 0;
    private int rondaActual = 0;
    private float tiempoRestanteRonda;
    private List<GameObject> celulasVivas = new List<GameObject>();

    
    private Color colorPromedioExitoso = Color.gray;
    private float tamañoPromedioExitoso = 1f;
    private bool hayHistorial = false; 

    void Start()
    {
        ActualizarUIPuntaje();
        StartCoroutine(LoopDeRondas());
    }

    
    void Update()
    {
        if (textoTiempo != null)
        {
            tiempoRestanteRonda -= Time.deltaTime;
            textoTiempo.text = "Tiempo: " + Mathf.Max(0, Mathf.CeilToInt(tiempoRestanteRonda));
        }
    }

   
    IEnumerator LoopDeRondas()
    {
        while (true)
        {
            rondaActual++;
            tiempoRestanteRonda = duracionRonda; 
            SpawnearCelulasDeLaRonda();

            yield return new WaitForSeconds(duracionRonda);

            EvaluarSupervivientesYAprender();
        }
    }

    void SpawnearCelulasDeLaRonda()
    {
        for (int i = 0; i < celulasPorRonda; i++)
        {
            Vector2 posicion = PosicionAleatoriaEnArea();
            GameObject nuevaCelula = Instantiate(cellPrefab, posicion, Quaternion.identity);

            Cell scriptCelula = nuevaCelula.GetComponent<Cell>();
            (Color colorElegido, float tamañoElegido) = GenerarGenesDeCelula();
            scriptCelula.Inicializar(this, colorElegido, tamañoElegido);

            celulasVivas.Add(nuevaCelula);
        }
    }

   
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
            colorPromedioExitoso = PromediarColores(coloresSupervivientes);
            tamañoPromedioExitoso = PromediarFloats(tamañosSupervivientes);
            hayHistorial = true;
        }

        celulasVivas.Clear();
    }

    
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

    Color PromediarColores(List<Color> colores)
    {
        float r = 0, g = 0, b = 0;
        foreach (Color c in colores) { r += c.r; g += c.g; b += c.b; }
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
