using UnityEngine;

public class RotateToPlayer : MonoBehaviour
{
    public float MinAngle = 5f;
    [SerializeField] private float Speed = 150f;
    [SerializeField] private GameObject player;
    [HideInInspector] public bool hasPlayerRotate;
    [HideInInspector] public bool IsShooting;
    
    private Quaternion GetFlatLookRotation()
    {
        Vector3 direction = player.transform.position - transform.position;
        direction.y = 0f; 

        if (direction.sqrMagnitude < 0.0001f)
        {
            return transform.rotation;
        }

        return Quaternion.LookRotation(direction);
    }

    public void RotateTowardsPlayer()
    {
        hasPlayerRotate = false;
        Quaternion lookRotation = GetFlatLookRotation();
        transform.rotation = Quaternion.RotateTowards(transform.rotation, lookRotation, Time.deltaTime * Speed);
        if (Quaternion.Angle(transform.rotation, lookRotation) < MinAngle)
        {
            hasPlayerRotate = true;
        }
    }

    public bool RotateUntilShoot()
    {
        if (IsShooting) return false;
        
        Quaternion lookRotation = GetFlatLookRotation();
        transform.rotation = Quaternion.RotateTowards(transform.rotation, lookRotation, Time.deltaTime * Speed);
        return true;
    }

    public void RotateInstant()
    {
        Quaternion lookRotation = GetFlatLookRotation();
        transform.rotation = lookRotation;
    }
}
