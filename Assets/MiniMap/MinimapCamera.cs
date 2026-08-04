using UnityEngine;

public class MinimapCamera : MonoBehaviour
{
    public Transform player;
    public float height = 20f;

    void LateUpdate()
    {
        Vector3 pos = player.position;
        pos.y = height;
        transform.position = pos;
    }
}