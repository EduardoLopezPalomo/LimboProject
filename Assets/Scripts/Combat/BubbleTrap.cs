using System.Buffers;
using UnityEngine;

public class BubbleTrap : MonoBehaviour
{
    public float velocity = 3f;
    public float lifeTime = 3f;

    private float timer = 0f;
    private float TrapTime = 0f;
    private bool bEnemyTrap = false;
    // Update is called once per frame

    public void InitializeBubble(float timeTrap)
    {
        TrapTime = timeTrap;
    }

    void Update()
    {
        transform.position += transform.forward * velocity * Time.deltaTime;
        if (!bEnemyTrap)
        {
            timer += Time.deltaTime;
            if (timer >= lifeTime)
            {
                Destroy(gameObject);
            }
        }
        else
        {
            timer += Time.deltaTime;
            if (timer >= TrapTime)
            {
                Destroy(gameObject);
            }
        }

    }

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Enemy"))
        {
            bEnemyTrap = true;
            velocity = 0f;
            transform.position = other.transform.position + new Vector3(0f, 1f, 0f);
            timer = 0f;
            Debug.Log("Atrapado");
        }
        else
        {
            Destroy(gameObject);
        }

    }
}
