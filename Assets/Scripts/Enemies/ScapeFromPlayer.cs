using UnityEngine;

public class ScapeFromPlayer : MonoBehaviour
{
    public float scapeDistance = 10f;
    [SerializeField] private Transform player;

    public Vector3 GetScapePosition()
    {
        Vector3 direction = (transform.position - player.position).normalized;
        
        Quaternion randomRotation = Quaternion.Euler(0, Random.Range(-30f, 30f), 0);
        direction = randomRotation * direction;
        
        Vector3 scapePosition = transform.position + (direction * scapeDistance); 
        return scapePosition;
    }
}
