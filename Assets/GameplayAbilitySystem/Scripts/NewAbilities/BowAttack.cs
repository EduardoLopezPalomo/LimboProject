using UnityEngine;
using UnityEngine.Animations;

[CreateAssetMenu(menuName = "AbilitySystem/Ability/BowAttack")]
public class BowAttack : GameplayAbility
{
    public GameObject arrow;
    public GameObject SpecialArrow;
    public float probSpecialAttack = 0.3f;
    public GameplayEffect SpecialAttackEffect;
    public override GameplayAbilitySpec CreateSpec(GameObject owner)
    {
        return new BowAttackSpec(this, owner, arrow, SpecialArrow , probSpecialAttack , SpecialAttackEffect);
    }
}
