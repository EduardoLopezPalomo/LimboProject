using System.Buffers;
using UnityEngine;

public class MoveSleepProyectile : MonoBehaviour
{
    public float velocity = 8f;
    public float lifeTime = 3f;

    private float timer = 0f;

    void Update()
    {
        transform.position += transform.forward * velocity * Time.deltaTime;

        timer += Time.deltaTime;
        if (timer >= lifeTime)
        {
            Destroy(gameObject);
        }


    }

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Enemy"))
        {
            Destroy(gameObject);

        }
        else
        {
            Destroy(gameObject);
        }

    }
}
