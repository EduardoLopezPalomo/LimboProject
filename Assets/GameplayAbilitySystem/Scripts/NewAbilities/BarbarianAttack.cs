using UnityEngine;

[CreateAssetMenu(menuName = "AbilitySystem/Ability/BarbarianAttack")]
public class BarbarianAttack : GameplayAbility
{
    public override GameplayAbilitySpec CreateSpec(GameObject owner)
    {
        return new BarbarianAttackSpec(this, owner);
    }
}
