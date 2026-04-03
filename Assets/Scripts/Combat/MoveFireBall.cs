using UnityEngine;

public class MoveFireBall : MonoBehaviour
{
    public float speed = 8f;
    public float lifeTime = 5f;
    public GameObject explosionEffectPrefab;

    private GameObject owner;
    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public void Init(GameObject owner)
    {
        this.owner = owner;
    }

    private void Start()
    {
        rb.useGravity = true;
        rb.linearVelocity = transform.forward * speed;

        Destroy(gameObject, lifeTime);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!collision.transform.IsChildOf(owner.transform))
        {
            ContactPoint contact = collision.contacts[0];
            Vector3 normal = contact.normal;
            Quaternion rotation = Quaternion.LookRotation(normal);
            if (explosionEffectPrefab != null)
            {
                Instantiate(explosionEffectPrefab, contact.point + (normal * 0.05f), rotation);
            }
            
            Destroy(gameObject);
        }
    }
}
