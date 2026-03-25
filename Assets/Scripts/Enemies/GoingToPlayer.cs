using UnityEngine;
using UnityEngine.UIElements;

public class GoingToPlayer : MonoBehaviour
{
    [SerializeField] private GameObject player;
    public float offsetPosiiton = 15f;

    public bool IsPlayerInRange()
    {
        return Vector3.Distance(transform.position, player.transform.position) <= offsetPosiiton;
    }

    public Vector3 GetPlayerPosition()
    {
        Vector3 direction = (transform.position - player.transform.position).normalized;
        Vector3 Position = player.transform.position + direction * offsetPosiiton;


        return Position;
    }
}
