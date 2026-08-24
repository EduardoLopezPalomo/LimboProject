using UnityEngine;

public class MinimapIcon : MonoBehaviour
{
    [Header("Configuración del icono")]
    public Color iconColor = Color.red;
    public float iconSize = 1.5f;
    public float heightOffset = 3f;
    public string minimapLayerName = "MinimapIcon";

    private GameObject icon;
    private Renderer[] renderers;

    void Start()
    {
        icon = GameObject.CreatePrimitive(PrimitiveType.Quad);
        icon.name = "PlayerMinimapIcon";
        icon.transform.SetParent(transform); 
        icon.transform.localScale = Vector3.one * iconSize;

        Destroy(icon.GetComponent<Collider>());

        Renderer rend = icon.GetComponent<Renderer>();
        Material mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        mat.color = iconColor;
        rend.material = mat;

        int layer = LayerMask.NameToLayer(minimapLayerName);
        if (layer == -1)
        {
            Debug.LogWarning($"La layer '{minimapLayerName}' no existe. Créala en Project Settings > Tags and Layers.");
        }
        else
        {
            icon.layer = layer;
        }


        renderers = GetComponentsInChildren<Renderer>();

        UpdateIconTransform();
    }

    void LateUpdate()
    {
        UpdateIconTransform();
    }

    private void UpdateIconTransform()
    {
        if (icon == null) return;

       
        float x = transform.position.x;
        float z = transform.position.z;

        if (renderers != null && renderers.Length > 0)
        {
            Bounds bounds = default;
            bool hasBounds = false;

            foreach (Renderer r in renderers)
            {
                if (r == null || r.gameObject == icon) continue;

                if (!hasBounds)
                {
                    bounds = r.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(r.bounds);
                }
            }

            if (hasBounds)
            {
                x = bounds.center.x;
                z = bounds.center.z;
            }
        }

        icon.transform.position = new Vector3(x, transform.position.y + heightOffset, z);

        icon.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
    }
}