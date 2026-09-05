using UnityEngine;

public class EnemyPlayerDetection : MonoBehaviour
{
    public float maxDetectionDistance = 10f;
    private bool playerDetected = false;
    private GameObject player;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            player = other.gameObject;
            playerDetected = true;
        }
    }
    void Update()
    {
        if (playerDetected)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);
            if (distanceToPlayer >= maxDetectionDistance)
            {
                ResetDetection();
            }
        }
    }
    public bool IsPlayerDetected()
    {
        return playerDetected;
    }

    public void ResetDetection()
    {
        playerDetected = false;
    }
}
