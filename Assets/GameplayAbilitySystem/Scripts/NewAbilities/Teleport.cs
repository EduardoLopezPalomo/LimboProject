using UnityEngine;
using UnityEngine.Animations;

[CreateAssetMenu(menuName = "AbilitySystem/Ability/Teleport")]
public class Teleport : GameplayAbility
{
    public override GameplayAbilitySpec CreateSpec(GameObject owner)
    {
        return new TeleportSpec(this, owner);
    }
}
