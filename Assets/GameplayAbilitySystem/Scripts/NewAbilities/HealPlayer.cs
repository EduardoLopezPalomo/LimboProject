using System;
using UnityEngine;
using UnityEngine.Animations;

[CreateAssetMenu(menuName = "AbilitySystem/Ability/HealPlayer")]
public class HealPlayer : GameplayAbility
{
    
    public VFXDefinition healEffectVFX;
    public override GameplayAbilitySpec CreateSpec(GameObject owner)
    {
        return new HealPlayerSpec(this, owner, healEffectVFX, Math.Abs(EffectsToSelf[1].Value));
    }
}
