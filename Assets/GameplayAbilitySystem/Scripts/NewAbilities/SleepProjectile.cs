using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(menuName = "AbilitySystem/Ability/SleepProjectile")]
public class SleepProjectile : GameplayAbility
{
    [FormerlySerializedAs("proyectile")]
    public GameObject projectile;
    public override GameplayAbilitySpec CreateSpec(GameObject owner)
    {
        return new SleepProjectileSpec(this, owner, projectile);
    }
}
