using UnityEngine;

[CreateAssetMenu(menuName = "AbilitySystem/Ability/CounterAttack")]
public class CounterAttack : GameplayAbility
{
    public float ForwardDistanceRay = 6f;
    public float LateralDistanceRay = 2f;
    public float AttackAssistSpeed = 11f;
    public float dashStopDistance = 1f;
    public float dashDefaultDistance = 1.5f;
    public float RotationSpeed = 50f;
    public override GameplayAbilitySpec CreateSpec(GameObject owner)
    {
        return new CounterAttackSpec(this, owner, ForwardDistanceRay, LateralDistanceRay, AttackAssistSpeed, dashStopDistance, dashDefaultDistance, RotationSpeed);
    }
}
