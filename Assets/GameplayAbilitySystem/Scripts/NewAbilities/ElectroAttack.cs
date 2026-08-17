using UnityEngine;
using UnityEngine.Animations;

[CreateAssetMenu(menuName = "AbilitySystem/Ability/ElectroAttack")]
public class ElectroAttack : GameplayAbility
{
    public VFXDefinition Lightning;
    public VFXDefinition PositionPreview;
    public LayerMask groundLayer;
    public float TimeBeforeLighting = 1f;
    public float AttacksOfsetTime = 1.5f;
    public int NumberOfLightings = 3;
    public int MaxLightingPerColum = 15;
    public float StartRadiusFromPlayer = 3f;
    public float MinDistanceBetweenEach = 1f;
    public override GameplayAbilitySpec CreateSpec(GameObject owner)
    {
        return new ElectroAttackSpec(this, owner, Lightning, PositionPreview , TimeBeforeLighting ,AttacksOfsetTime,
            NumberOfLightings , MaxLightingPerColum, StartRadiusFromPlayer, MinDistanceBetweenEach , groundLayer);
    }
}
