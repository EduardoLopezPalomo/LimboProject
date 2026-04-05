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
            var enemyGraph = other.GetComponent<EnemyGraphBridge>();
            if (enemyGraph != null)
            {
                enemyGraph.SetTrap();
            }
            //Añadir trigger y bool de animacion de con sueño
            Destroy(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

    }
}
