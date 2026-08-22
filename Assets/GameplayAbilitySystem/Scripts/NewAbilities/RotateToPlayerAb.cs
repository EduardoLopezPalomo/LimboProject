using UnityEngine;
using UnityEngine.Animations;

[CreateAssetMenu(menuName = "AbilitySystem/Ability/RotateToPlayerAb")]
public class RotateToPlayerAb : GameplayAbility
{
    
    public override GameplayAbilitySpec CreateSpec(GameObject owner)
    {
        return new RotateToPlayerAbSpec(this, owner);
    }
}
