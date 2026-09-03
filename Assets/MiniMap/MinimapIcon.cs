using UnityEngine;

public class MinimapIcon : MonoBehaviour
{
    [Header("Configuracion del icono")]
    public Color iconColor = Color.red;
    public float iconSize = 1.5f;
    public float HeightOffset = 30f;
    public string minimapLayerName = "MinimapIcon";

    [Header("Behavior in edge")]
    public bool clampToMinimapEdge = false;

    [Header("Color when is and objective")]
    public Color goalColor = Color.cyan;
    public float goalSizeMultiplier = 2f;


    private GameObject icon;
    private Renderer[] renderers;
    private Material iconMaterial;
    private bool iconCreated = false;
    private Material minimapMaterial;

    void Start()
    {
        minimapMaterial = MinimapMaterial.minimapMaterial;
        CreateIconIfNeeded();
    }

    void OnEnable()
    {
        CreateIconIfNeeded();
        if (icon != null) icon.SetActive(true);
    }

    void OnDisable()
    {
        if (icon != null) icon.SetActive(false);
    }

    void OnDestroy()
    {
        if (icon != null) Destroy(icon);
    }

    private void CreateIconIfNeeded()
    {
        if (iconCreated || minimapMaterial == null) return;
        iconCreated = true;

        icon = GameObject.CreatePrimitive(PrimitiveType.Quad);
        icon.name = "MinimapIcon_" + gameObject.name;
        icon.transform.SetParent(transform);
        icon.transform.localScale = Vector3.one * iconSize;

        Destroy(icon.GetComponent<Collider>());

        Renderer rend = icon.GetComponent<Renderer>();
        iconMaterial = new Material(minimapMaterial);
        iconMaterial.SetColor("_BaseColor", iconColor);
        rend.material = iconMaterial;

        int layer = LayerMask.NameToLayer(minimapLayerName);
        if (layer == -1)
            Debug.LogWarning($"La layer '{minimapLayerName}' no existe. Créala en Project Settings > Tags and Layers.");
        else
            icon.layer = layer;

        renderers = GetComponentsInChildren<Renderer>();
        UpdateIconTransform();
    }

    void LateUpdate() => UpdateIconTransform();

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
                if (!hasBounds) { bounds = r.bounds; hasBounds = true; }
                else bounds.Encapsulate(r.bounds);
            }

            if (hasBounds) { x = bounds.center.x; z = bounds.center.z; }
        }

        Vector3 realWorldPos = new Vector3(x, transform.position.y + HeightOffset, z);

        if (clampToMinimapEdge && MinimapCamera.Instance != null)
        {
            bool dentro = MinimapCamera.Instance.IsWithinView(realWorldPos);
            icon.transform.position = dentro ? realWorldPos : MinimapCamera.Instance.ClampToEdge(realWorldPos);
        }
        else
        {
            icon.transform.position = realWorldPos;
        }

        icon.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
    }

    public void SetAsGoal(bool esObjetivo)
    {
        clampToMinimapEdge = esObjetivo;
        if (iconMaterial != null)
        {
            iconMaterial.color = esObjetivo ? goalColor : iconColor;
        }

        if (icon != null)
        {
            float multiplier = esObjetivo ? goalSizeMultiplier : 1f;
            icon.transform.localScale = Vector3.one * iconSize * multiplier;
        }
    }
}