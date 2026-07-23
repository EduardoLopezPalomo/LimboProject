using System;
using UnityEngine;

public class ArrowMovement : MonoBehaviour
{
    public float InitialSpeed = 20f;
    
    private Rigidbody rb;
    private GameObject owner;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }
    public void Init(GameObject owner)
    {
        this.owner = owner;
        
        Collider[] ownerColliders = owner.GetComponentsInChildren<Collider>();
        Collider myCollider = GetComponent<Collider>();

        foreach (var col in ownerColliders)
        {
            Physics.IgnoreCollision(myCollider, col);
        }
    }
    
    public void Shoot(Vector3 direction)
    {
        rb.linearVelocity = direction.normalized * InitialSpeed;
    }

    void FixedUpdate()
    {
        if (rb.linearVelocity.magnitude > 0.1f)
        {
            transform.forward = rb.linearVelocity.normalized;
        }
    }
    
    void OnTriggerEnter(Collider other)
    {
        if (!other.transform.IsChildOf(owner.transform))
        {
            Destroy(gameObject);
        }
    }
}
