using UnityEngine;

[CreateAssetMenu(menuName = "AbilitySystem/Ability/Dash")]
public class Dash : GameplayAbility
{
    public int dashDistance = 10;
    public float dashTime = 3;
    public override GameplayAbilitySpec CreateSpec(GameObject owner)
    {
        return new DashSpec(this, owner, dashDistance, dashTime);
    }
}
