using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class ObjectTransparency : MonoBehaviour
{
    [Header("Referencias")]
    public Transform playerTransform;
    public Transform cameraTransform;
    public LayerMask wallLayer;

    [Header("Cinemachine")]
    public CinemachineVirtualCamera virtualCamera;
    public Camera mainCamera;

    [Header("Shader de Fade")]
    [Tooltip("Material/Shader Graph propio con propiedades _BaseMap y _Alpha")]
    public Material fadeShaderTemplate;

    [Header("Overlap Box")]
    public float overlapDepth = 0.5f;

    [Header("Fade")]
    [Tooltip("Velocidad del fade en unidades de alpha por segundo")]
    public float fadeSpeed = 3f;

    private class WallState
    {
        public Renderer renderer;
        public Material[] fadeMats;
        public Material[] originalSharedMats;
        public float alpha; 
    }

    private Dictionary<GameObject, WallState> states = new Dictionary<GameObject, WallState>();
    private HashSet<GameObject> currentlyBlocking = new HashSet<GameObject>();
    
    private static readonly string[] MainTexPropertyNames =
    {
        "_BaseMap", "_MainTex", "_BaseColorMap", "_AlbedoMap", "_Albedo", "_Diffuse", "_Albedo_Map" 
    };

    private static readonly string[] NormalMapPropertyNames =
    {
        "_BumpMap", "_NormalMap", "_Normal", "_Normal_Map"
    };

    void Update()
    {
        DetectWall();
        UpdateFades();
    }

    void DetectWall()
    {
        currentlyBlocking.Clear();

        if (!playerTransform || !cameraTransform) return;
        
        Vector3 direction = playerTransform.position - cameraTransform.position;
        RaycastHit[] hits = Physics.RaycastAll(cameraTransform.position, direction.normalized, direction.magnitude, wallLayer);
        foreach (RaycastHit hit in hits)
            currentlyBlocking.Add(hit.collider.gameObject);
        
        if (virtualCamera != null && mainCamera != null)
        {
            float orthoSize = virtualCamera.m_Lens.OrthographicSize;
            float aspect = mainCamera.aspect;
            Vector3 halfExtents = new Vector3(orthoSize * aspect, orthoSize, overlapDepth * 0.5f);
            Vector3 boxCenter = cameraTransform.position + cameraTransform.forward * (overlapDepth * 0.5f);

            Collider[] overlaps = Physics.OverlapBox(boxCenter, halfExtents, cameraTransform.rotation, wallLayer);
            foreach (Collider col in overlaps)
                currentlyBlocking.Add(col.gameObject);
        }
        
        foreach (GameObject wall in currentlyBlocking)
        {
            if (states.ContainsKey(wall)) continue;

            Renderer renderer = wall.GetComponent<Renderer>();
            if (renderer == null) continue;
            if (fadeShaderTemplate == null)
            {
                Debug.LogWarning("ObjectTransparency: fadeShaderTemplate no asignado.");
                continue;
            }

            WallState state = new WallState();
            state.renderer = renderer;
            state.originalSharedMats = renderer.sharedMaterials;

            Material[] sharedMats = renderer.sharedMaterials;
            Material[] fadeMats = new Material[sharedMats.Length];

            for (int i = 0; i < sharedMats.Length; i++)
            {
                Material fadeMat = new Material(fadeShaderTemplate);

                Texture mainTex = GetPropertyByNames(sharedMats[i], MainTexPropertyNames) as Texture;
                if (mainTex != null && fadeMat.HasProperty("_BaseMap"))
                    fadeMat.SetTexture("_BaseMap", mainTex);

                Texture normalTex = GetPropertyByNames(sharedMats[i], NormalMapPropertyNames) as Texture;
                if (normalTex != null && fadeMat.HasProperty("_BumpMap"))
                    fadeMat.SetTexture("_BumpMap", normalTex);

                if (fadeMat.HasProperty("_Alpha"))
                    fadeMat.SetFloat("_Alpha", 1f);

                fadeMats[i] = fadeMat;
            }

            state.fadeMats = fadeMats;
            state.alpha = 1f;

            renderer.materials = fadeMats;
            states[wall] = state;
        }
    }
    
    void UpdateFades()
    {
        if (states.Count == 0) return;

        List<GameObject> toRestore = new List<GameObject>();

        foreach (var kvp in states)
        {
            GameObject wall = kvp.Key;
            WallState state = kvp.Value;

            if (wall == null || state.renderer == null)
            {
                toRestore.Add(wall);
                continue;
            }

            bool isBlocking = currentlyBlocking.Contains(wall);
            float targetAlpha = isBlocking ? 0f : 1f; 

            state.alpha = Mathf.MoveTowards(state.alpha, targetAlpha, fadeSpeed * Time.deltaTime);

            foreach (Material mat in state.fadeMats)
            {
                if (mat.HasProperty("_Alpha"))
                    mat.SetFloat("_Alpha", state.alpha);
            }
            
            if (!isBlocking && state.alpha >= 0.999f)
                toRestore.Add(wall);
        }

        foreach (GameObject wall in toRestore)
            RestoreOriginal(wall);
    }

    void RestoreOriginal(GameObject wall)
    {
        if (!states.ContainsKey(wall)) return;

        WallState state = states[wall];

        if (wall != null && state.renderer != null)
        {
            state.renderer.materials = state.originalSharedMats;
        }

        states.Remove(wall);
    }
    
    object GetPropertyByNames(Material mat, string[] names)
    {
        foreach (string name in names)
        {
            if (mat.HasProperty(name))
                return mat.GetTexture(name);
        }
        return null;
    }
}
