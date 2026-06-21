using UnityEngine;

[CreateAssetMenu(menuName = "AbilitySystem/Ability/LightAttack")]
public class LightAttack : GameplayAbility
{
    public float coneRange = 6f;
    public float coneAngle = 40f;       
    public float dashDuration = 0.15f;
    public float dashStopDistance = 1.5f;
    public float dashDefaultDistance = 1f;
    public float RotationSpeed = 0.15f;
    
    public override GameplayAbilitySpec CreateSpec(GameObject owner)
    {
        return new LightAttackSpec(this, owner, coneRange, coneAngle, dashDuration, dashStopDistance, dashDefaultDistance, RotationSpeed);
    }
}
