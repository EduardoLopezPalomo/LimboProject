using System.Collections.Generic;
using UnityEngine;

public class ObjectTransparency : MonoBehaviour
{
    public Transform playerTransform;
    public Transform cameraTransform;
    public LayerMask wallLayer;
    public float transparency = 0.3f;

    private Dictionary<GameObject, Material> originalMaterials = new Dictionary<GameObject, Material>();
    private List<GameObject> transparentWalls = new List<GameObject>();


    // Update is called once per frame
    void Update()
    {
        DetectWall();
    }

    void DetectWall()
    {
        if (!playerTransform)
        {
            Debug.Log("illooo");
        }
        // Reset transparency for previously detected walls
        ResetWallTransparency();

        // Raycast from the camera to the player
        Vector3 direction = playerTransform.position - cameraTransform.position;
        RaycastHit[] hits = Physics.RaycastAll(cameraTransform.position, direction.normalized, direction.magnitude, wallLayer);

        foreach (RaycastHit hit in hits)
        {
            GameObject wall = hit.collider.gameObject;
            if (!originalMaterials.ContainsKey(wall))
            {
                // Store the original material once
                Renderer renderer = wall.GetComponent<Renderer>();
                if (renderer != null)
                {
                    originalMaterials[wall] = renderer.material;

                    // Create a new material instance
                    Material transparentMat = new Material(renderer.material);
                    SetMaterialTransparent(transparentMat, transparency);
                    renderer.material = transparentMat;

                    transparentWalls.Add(wall);
                }
            }
        }
    }

    void ResetWallTransparency()
    {
        foreach (GameObject wall in transparentWalls)
        {
            if (wall != null && originalMaterials.ContainsKey(wall))
            {
                Renderer renderer = wall.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material = originalMaterials[wall];
                }
                originalMaterials.Remove(wall);
            }
        }
        transparentWalls.Clear();
    }

    void SetMaterialTransparent(Material material, float alpha)
    {
        Color color = material.color;
        color.a = alpha;
        material.color = color;

        //Correct transparency and blending settings
        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetInt("_ZWrite", 0);
        material.SetInt("_Surface", 1);
        material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
    }
}
