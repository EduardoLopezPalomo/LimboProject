using System.Collections;
using UnityEngine;

public class AnimacionAleatoria : MonoBehaviour
{
    [SerializeField] float tiempoMinimo = 2f;
    [SerializeField] float tiempoMaximo = 6f;

    Animator anim;

    void Start()
    {
        anim = GetComponent<Animator>();
        StartCoroutine(Repetir());
    }

    IEnumerator Repetir()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(tiempoMinimo, tiempoMaximo));
            anim.SetTrigger("Shot");
        }
    }
}