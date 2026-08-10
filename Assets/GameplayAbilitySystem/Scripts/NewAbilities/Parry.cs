using UnityEngine;

[CreateAssetMenu(menuName = "AbilitySystem/Ability/Parry")]
public class Parry : GameplayAbility
{
    public float parryWindowDuration = 0.2f;

    public override GameplayAbilitySpec CreateSpec(GameObject owner)
    {
        return new ParrySpec(this, owner, parryWindowDuration);
    }
}
