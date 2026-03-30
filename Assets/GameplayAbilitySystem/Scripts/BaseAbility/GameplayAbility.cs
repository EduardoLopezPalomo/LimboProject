using UnityEngine;
using System.Collections.Generic;
/*
 *   Is the father class of gameplay abilities
 *   Its posible to override activate ability and end ability for especific logic 
 *
 *   It has a name, a cooldown, an active time and a list of GameplayEffects
*/
[CreateAssetMenu(menuName = "AbilitySystem/Ability")]
public abstract class GameplayAbility : ScriptableObject
{
    public string Name;
    public Sprite icon;
    public float Cooldown;
    public float ActiveTime;
    public List<GameplayEffect> Effects;

    public virtual void ActivateAbilityEffects(GameObject owner)
    {
        var effectManager = owner.GetComponent<GameplayEffectManager>();
        var attributeSystem = owner.GetComponent<AttributeSystem>();
        if (!attributeSystem || !effectManager)
        {
            Debug.LogWarning("The owner doesn't have attribute system or effect manager");
            return;
        }

        foreach (var effect in Effects)
        {
            effectManager.ApplyEffect(effect, owner.GetComponent<AttributeSystem>());
        }
    }

    public abstract GameplayAbilitySpec CreateSpec(GameObject owner);
}
