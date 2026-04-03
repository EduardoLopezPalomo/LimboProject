using UnityEngine;

public class RotateToPlayer : MonoBehaviour
{
    public float MinAngle = 5f;
    [SerializeField] private float Speed = 150f;
    [SerializeField] private GameObject player;
    [HideInInspector] public bool hasPlayerRotate;

    public void RotateTowardsPlayer()
    {
        hasPlayerRotate = false;
        Vector3 direction = (player.transform.position - transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, lookRotation, Time.deltaTime * Speed);
        if (Quaternion.Angle(transform.rotation, lookRotation) < MinAngle)
        {
            hasPlayerRotate = true;
        }
    }
}
