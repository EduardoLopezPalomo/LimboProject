using UnityEngine;
using UnityEngine.Animations;

[CreateAssetMenu(menuName = "AbilitySystem/Ability/Teleport")]
public class Teleport : GameplayAbility
{
    public SlotType slotType = SlotType.Far;
    public override GameplayAbilitySpec CreateSpec(GameObject owner)
    {
        return new TeleportSpec(this, owner, slotType);
    }
}
