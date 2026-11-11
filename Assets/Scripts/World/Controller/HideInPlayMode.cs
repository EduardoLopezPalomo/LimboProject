using UnityEngine;

public class HideInPlayMode : MonoBehaviour
{
    void Start()
    {
        // Al empezar el juego, desactiva este objeto
        gameObject.SetActive(false);
    }

    void OnDisable()
    {
        // Opcional: si quieres reactivar el objeto cuando pares el juego en editor
        gameObject.SetActive(true);
    }
}

