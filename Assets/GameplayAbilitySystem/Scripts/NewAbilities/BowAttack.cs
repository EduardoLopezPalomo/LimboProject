using UnityEngine;
using UnityEngine.Animations;

[CreateAssetMenu(menuName = "AbilitySystem/Ability/BowAttack")]
public class BowAttack : GameplayAbility
{
    public GameObject arrow;
    public override GameplayAbilitySpec CreateSpec(GameObject owner)
    {
        return new BowAttackSpec(this, owner, arrow);
    }
}
