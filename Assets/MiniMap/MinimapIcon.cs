using UnityEngine;

public class MinimapIcon : MonoBehaviour
{
    [Header("Configuración del icono")]
    public Color iconColor = Color.red;
    public float iconSize = 1.5f;
    public float heightOffset = 3f;
    public string minimapLayerName = "MinimapIcon";

    void Start()
    {
        GameObject icon = GameObject.CreatePrimitive(PrimitiveType.Quad);
        icon.name = "PlayerMinimapIcon";

        icon.transform.SetParent(transform);
        icon.transform.localPosition = new Vector3(0, heightOffset, 0);
        icon.transform.localRotation = Quaternion.Euler(90, 0, 0);
        icon.transform.localScale = Vector3.one * iconSize;

        Destroy(icon.GetComponent<Collider>());
        Renderer rend = icon.GetComponent<Renderer>();
        Material mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        mat.color = iconColor;
        rend.material = mat;
        int layer = LayerMask.NameToLayer(minimapLayerName);
        if (layer == -1){
            Debug.LogWarning($"La layer '{minimapLayerName}' no existe. Créala en Project Settings > Tags and Layers.");
        }
        else {
            icon.layer = layer;
        }
    }
}