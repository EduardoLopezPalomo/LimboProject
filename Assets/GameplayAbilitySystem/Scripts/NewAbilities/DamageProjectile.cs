using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(menuName = "AbilitySystem/Ability/DamageProjectile")]
public class DamageProjectile : GameplayAbility
{

    public float angle = 25f;
    public float distanceOffset = 1f;
    [FormerlySerializedAs("proyectile")]
    public VFXDefinition projectile;

    public float backDashDistance = 3f;
    public float dashTime = 0.5f;
    public override GameplayAbilitySpec CreateSpec(GameObject owner)
    {
        return new DamageProjectileSpec(this, owner, projectile, angle, distanceOffset, backDashDistance, dashTime, Mathf.Abs(EffectsToSelf[0].Value));
    }
}
