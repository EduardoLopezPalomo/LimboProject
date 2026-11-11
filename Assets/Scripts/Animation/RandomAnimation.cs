using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

public class RandomAnimation : MonoBehaviour
{
    [FormerlySerializedAs("tiempoMinimo")]
    [SerializeField] float minDelay = 2f;
    [FormerlySerializedAs("tiempoMaximo")]
    [SerializeField] float maxDelay = 6f;

    Animator anim;

    void Start()
    {
        anim = GetComponent<Animator>();
        StartCoroutine(Repeat());
    }

    IEnumerator Repeat()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(minDelay, maxDelay));
            anim.SetTrigger("Shot");
        }
    }
}