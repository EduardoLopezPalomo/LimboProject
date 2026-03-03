using UnityEngine;

public class RotateToPlayer : MonoBehaviour
{

    [SerializeField] private GameObject player;
    [HideInInspector] public bool hasPlayerRotate;

    public void RotateTowardsPlayer()
    {
        hasPlayerRotate = false;
        Vector3 direction = (player.transform.position - transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
        if (Quaternion.Angle(transform.rotation, lookRotation) < 5f)
        {
            hasPlayerRotate = true;
        }
    }
}
