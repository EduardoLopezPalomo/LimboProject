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
    SpellAura,
    Teleport,
    Slash5,
    Slash6,
    Slash7,
    Slash8,
    GroundImpact,
}

[CreateAssetMenu(fileName = "VFX", menuName = "VFX/VFX Definition")]
public class VFXDefinition : ScriptableObject
{
    public VFXType type = VFXType.Slash1;
    public string variantId;
    public GameObject prefab;
    public Vector3 positionOffset;
    public Vector3 rotationOffset;
    public Vector3 scaleOverride = Vector3.one;
    public float lifetime = 2f;
    public bool untilCancel = false;
    public bool useObjectPool = true;
    public bool followTarget = false;

}
