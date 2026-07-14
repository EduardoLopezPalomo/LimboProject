using UnityEngine;

[RequireComponent(typeof(MeshRenderer), typeof(MeshFilter))]
public class TreeWindBounds : MonoBehaviour
{
    static readonly int MinHeightID = Shader.PropertyToID("_MinHeight");
    static readonly int MaxHeightID = Shader.PropertyToID("_MaxHeight");

    void Awake()
    {
        var mr = GetComponent<MeshRenderer>();
        var mf = GetComponent<MeshFilter>();

        if (mf.sharedMesh == null) return;

        Bounds b = mf.sharedMesh.bounds; 
        
        Debug.Log($"{gameObject.name} -> MinHeight: {b.min.y:F4}  MaxHeight: {b.max.y:F4}");

        var mpb = new MaterialPropertyBlock();
        mr.GetPropertyBlock(mpb);
        mpb.SetFloat(MinHeightID, b.min.y);
        mpb.SetFloat(MaxHeightID, b.max.y);
        mr.SetPropertyBlock(mpb);
    }
}
