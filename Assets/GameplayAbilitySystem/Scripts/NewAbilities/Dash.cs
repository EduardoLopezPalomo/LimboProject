using UnityEngine;

[CreateAssetMenu(menuName = "AbilitySystem/Ability/Dash")]
public class Dash : GameplayAbility
{
    public override GameplayAbilitySpec CreateSpec(GameObject owner)
    {
        return new DashSpec(this, owner);
    }
}
