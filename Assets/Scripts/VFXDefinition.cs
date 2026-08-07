using System;
using UnityEngine;

[Serializable]
public enum VFXType
{
    Slash1,
    Slash2,
    Slash3,
    Slash4,
    Counter,
    Damaged,
}

[CreateAssetMenu(fileName = "VFX", menuName = "VFX/VFX Definition")]
public class VFXDefinition : ScriptableObject
{
    public VFXType type = VFXType.Slash1;
    public GameObject prefab;
    public Vector3 rotationOffset;
    public Vector3 scaleOverride = Vector3.one;
    public float lifetime = 2f;
    public bool useObjectPool = true;

}
