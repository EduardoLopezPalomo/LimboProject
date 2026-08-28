using UnityEngine;

public class MinimapCamera : MonoBehaviour
{
    public static MinimapCamera Instance { get; private set; }
    
    public Transform player;
    public float height = 20f;
    
    [Range(0.5f, 1f)]
    [Tooltip("Margin for the icon to not be in the edge")]
    public float edgeSafeMargin = 0.92f;

    private Camera cam;
    
    void Awake()
    {
        Instance = this;
        cam = GetComponent<Camera>();
    }


    void LateUpdate()
    {
        Vector3 pos = player.position;
        pos.y = height;
        transform.position = pos;
    }
    
    public Vector3 Center => transform.position;

    public float Radius => (cam != null ? cam : (cam = GetComponent<Camera>())).orthographicSize * edgeSafeMargin;

    public bool IsWithinView(Vector3 worldPos)
    {
        float dx = worldPos.x - Center.x;
        float dz = worldPos.z - Center.z;
        return (dx * dx + dz * dz) <= (Radius * Radius);
    }

    public Vector3 ClampToEdge(Vector3 worldPos)
    {
        Vector3 c = Center;
        float dx = worldPos.x - c.x;
        float dz = worldPos.z - c.z;
        float distSqr = dx * dx + dz * dz;
        float r = Radius;

        if (distSqr <= r * r) return worldPos;

        float dist = Mathf.Sqrt(distSqr);
        float scale = r / dist;
        return new Vector3(c.x + dx * scale, worldPos.y, c.z + dz * scale);
    }
}