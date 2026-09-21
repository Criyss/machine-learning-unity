using UnityEngine;


[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Collider2D))]
public class Cell : MonoBehaviour
{
 
    [HideInInspector] public Color colorActual;
    [HideInInspector] public float tamañoActual;

    private GameManager manager;
    private SpriteRenderer spriteRenderer;

  
    public void Inicializar(GameManager managerRef, Color color, float tamaño)
    {
        manager = managerRef;
        spriteRenderer = GetComponent<SpriteRenderer>();

        colorActual = color;
        tamañoActual = tamaño;

        AplicarApariencia();
    }

    void AplicarApariencia()
    {
        spriteRenderer.color = colorActual;
        transform.localScale = new Vector3(tamañoActual, tamañoActual, 1f);
    }

   
    void OnMouseDown()
    {
        if (manager != null)
        {
            manager.RegistrarEliminacion(gameObject);
        }
        Destroy(gameObject);
    }
}