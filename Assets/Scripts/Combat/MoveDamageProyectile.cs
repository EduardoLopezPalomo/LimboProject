using System.Buffers;
using UnityEngine;

public class MoveDamageProyectile : MonoBehaviour
{
    public float velocity = 8f;
    public float lifeTime = 3f;

    public GameplayEffect damage;

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
            //dormir al enemigo
            GameplayEffectManager effectmanager = other.gameObject.GetComponent<GameplayEffectManager>();
            AttributeSystem attributeSystem = other.gameObject.GetComponent<AttributeSystem>();
            if (effectmanager == null || attributeSystem == null)
            {
                Debug.Log("effectmanager, attributesystem or makeInvincible is null");
                Destroy(gameObject);
                return;
            }

            effectmanager.ApplyEffect(damage, attributeSystem);

            Destroy(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

    }
}
