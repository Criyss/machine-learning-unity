using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Collider2D))]
public class Cell : MonoBehaviour
{
    [HideInInspector] public Color colorActual;
    [HideInInspector] public float tamañoActual;
    public AudioClip deathSound;

    private GameManager manager;
    private SpriteRenderer spriteRenderer;

    /// Inicializa la célula con sus "genes" (color y tamaño) y su variedad de sprites
    /// El sprite es solo visual, el aprendizaje se basa únicamente en color y tamaño
    public void Inicializar(GameManager managerRef, Color color, float tamaño, Sprite sprite)
    {
        manager = managerRef;
        spriteRenderer = GetComponent<SpriteRenderer>();

        colorActual = color;
        tamañoActual = tamaño;

        if (sprite != null)
        {
            spriteRenderer.sprite = sprite;
        }

        AplicarApariencia();
    }

    void AplicarApariencia()
    {
        spriteRenderer.color = colorActual;
        transform.localScale = new Vector3(tamañoActual, tamañoActual, 1f);
    }

    // Unity llama esto cuando el jugador hace click sobre el Collider2D de la celula
    void OnMouseDown()
    {
        if (manager != null)
        {
            manager.RegistrarEliminacion(gameObject);
        }
        AudioManager.instance.PlaySFX(deathSound, 1, Random.Range(0.8f, 1.2f));
        Destroy(gameObject);
    }
}