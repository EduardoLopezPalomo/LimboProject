using UnityEngine;

[CreateAssetMenu(menuName = "AbilitySystem/Ability/BarbarianAttack")]
public class BarbarianAttack : GameplayAbility
{
    public float TimeStuned = 2f;
    public float SpecialAttackChance = 0.2f;
    public GameplayEffect SpecialAttackEffect;
    public override GameplayAbilitySpec CreateSpec(GameObject owner)
    {
        return new BarbarianAttackSpec(this, owner, TimeStuned, SpecialAttackChance, SpecialAttackEffect);
    }
}
