using UnityEngine;

[CreateAssetMenu(menuName = "AbilitySystem/Ability/GodSpaceAttack")]
public class GodSpaceAttack : GameplayAbility
{
    public string AnimatorTrigger;
    public override GameplayAbilitySpec CreateSpec(GameObject owner)
    {
        return new GodSpaceAttackSpec(this, owner, AnimatorTrigger);
    }
}
