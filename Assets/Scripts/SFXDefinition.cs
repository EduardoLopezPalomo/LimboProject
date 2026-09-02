using System;
using UnityEngine;

[Serializable]
public enum SFXType
{
    Attack,
    Counter,
    Damaged,
    Death,
    ChangePhase,
    Resistance,
    Cooldown,
    LowHealth,
    Healing,
}

[CreateAssetMenu(fileName = "SFX", menuName = "SFX/SFX Definition")]
public class SFXDefinition : ScriptableObject
{
    public SFXType type = SFXType.Attack;
    public AudioClip sfx;
    public float volume = 1f;
    public float minPitch = 1f;
    public float maxPitch = 1f;

}
