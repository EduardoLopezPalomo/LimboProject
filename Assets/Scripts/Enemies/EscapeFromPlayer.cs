using UnityEngine;

public class EscapeFromPlayer : MonoBehaviour
{
    public float escapeDistance = 10f;
    public float randomAngleRange = 30f;
    [SerializeField] private Transform player;

    public Vector3 GetScapePosition()
    {
        Vector3 direction = (transform.position - player.position).normalized;
        
        Quaternion randomRotation = Quaternion.Euler(0, Random.Range(-randomAngleRange, randomAngleRange), 0);
        direction = randomRotation * direction;
        
        Vector3 scapePosition = transform.position + (direction * escapeDistance); 
        return scapePosition;
    }
}
