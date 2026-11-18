using System;
using UnityEngine;

[Serializable]
public enum AttackKind
{
    ComboPhysical,
    IndividualPhysical,
    Magical,
    Teleport
}
[CreateAssetMenu(menuName = "Combat/AttackDefinition")]
public class AttackDefinition : ScriptableObject
{
    [AbilityName]
    public string ability;
    public AttackKind kind;
}
