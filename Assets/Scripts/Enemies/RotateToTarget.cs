using UnityEngine;

public class RotateToTarget : MonoBehaviour
{
    public float MinAngle = 5f;
    private GameObject target = null;
    [SerializeField] private float Speed = 150f;
    [HideInInspector] public bool hasPlayerRotate;

    public void RotateTowardsTarget()
    {
        if(target == null)
        {
            Debug.Log("Target is null");
            return;
        }
        hasPlayerRotate = false;
        Vector3 direction = (target.transform.position - transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, lookRotation, Time.deltaTime * Speed);
        if (Quaternion.Angle(transform.rotation, lookRotation) < MinAngle)
        {
            hasPlayerRotate = true;
        }
    }

    public void SetTarget(GameObject target)
    {
        this.target = target;
    }

    public bool HasTarget()
    {
        if(target != null)
        {
            return true;
        }
       
        return false;
        
    }
}
