using UnityEngine;

[CreateAssetMenu(menuName = "AbilitySystem/Ability/Death")]
public class Death : GameplayAbility
{
    public override GameplayAbilitySpec CreateSpec(GameObject owner)
    {
        return new DeathSpec(this, owner);
    }
}
