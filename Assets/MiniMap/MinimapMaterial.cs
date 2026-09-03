using UnityEngine;

public class MinimapMaterial : MonoBehaviour
{
    public Material material;
    public static Material minimapMaterial;

    void Awake()
    {
        minimapMaterial = material;
    }
}
