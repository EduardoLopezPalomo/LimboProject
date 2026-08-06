using UnityEngine;

[CreateAssetMenu(menuName = "AbilitySystem/Ability/LightAttack")]
public class LightAttack : GameplayAbility
{
    public float ForwardDistanceRay = 6f;
    public float LateralDistanceRay = 40f;
    public float AttackAssistSpeed = 0.15f;
    public float dashStopDistance = 1.5f;
    public float dashDefaultDistance = 1f;
    public float RotationSpeed = 0.15f;
    public SFX[] sounds;

    public override GameplayAbilitySpec CreateSpec(GameObject owner)
    {
        return new LightAttackSpec(this, owner, ForwardDistanceRay, LateralDistanceRay, AttackAssistSpeed, dashStopDistance, dashDefaultDistance, RotationSpeed, sounds);
    }
}
