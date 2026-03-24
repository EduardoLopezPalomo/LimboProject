using UnityEngine;

[CreateAssetMenu(menuName = "AbilitySystem/Ability/TrapBubble")]
public class TrapBubble : GameplayAbility
{
    public float TrapTime = 3;
    public GameObject bubble;
    public override GameplayAbilitySpec CreateSpec(GameObject owner)
    {
        return new TrapBubbleSpec(this, owner, TrapTime, bubble);
    }
}
