using UnityEngine;

[CreateAssetMenu(menuName = "AbilitySystem/Ability/LightAttack")]
public class LightAttack : GameplayAbility
{
    public override GameplayAbilitySpec CreateSpec(GameObject owner)
    {
        return new LightAttackSpec(this, owner);
    }
}
