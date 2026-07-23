using UnityEngine;

[CreateAssetMenu(menuName = "AbilitySystem/Ability/BarbarianAttack")]
public class BarbarianAttack : GameplayAbility
{
    public float TimeStuned = 2f;
    public override GameplayAbilitySpec CreateSpec(GameObject owner)
    {
        return new BarbarianAttackSpec(this, owner, TimeStuned);
    }
}
