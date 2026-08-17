using UnityEngine;
using UnityEngine.Animations;

[CreateAssetMenu(menuName = "AbilitySystem/Ability/FireAttack")]
public class FireAttack : GameplayAbility
{
    public VFXDefinition FireGround;
    public VFXDefinition PositionPreview;
    public LayerMask groundLayer;
    public float TimeBeforeExplosion = 1f;
    public int ExposionsNumber = 4;
    public float MaxRadiusFromPlayer = 3f;
    public float MinDistanceBetweenEach = 1f;
    public override GameplayAbilitySpec CreateSpec(GameObject owner)
    {
        return new FireAttackSpec(this, owner, FireGround, PositionPreview , TimeBeforeExplosion , ExposionsNumber, MaxRadiusFromPlayer, MinDistanceBetweenEach , groundLayer);
    }
}
