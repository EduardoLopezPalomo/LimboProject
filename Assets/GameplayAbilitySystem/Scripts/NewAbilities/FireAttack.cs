using UnityEngine;
using UnityEngine.Animations;

[CreateAssetMenu(menuName = "AbilitySystem/Ability/FireAttack")]
public class FireAttack : GameplayAbility
{
    public GameObject firePrefab;
    public float inclineAngle = 0.6f;
    public override GameplayAbilitySpec CreateSpec(GameObject owner)
    {
        return new FireAttackSpec(this, owner, firePrefab, inclineAngle);
    }
}
