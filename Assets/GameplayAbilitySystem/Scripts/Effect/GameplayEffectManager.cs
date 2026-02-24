using System.Collections.Generic;
using UnityEngine;
/*
 *  Its the manager that controls the effects
 *  It only has a list of the active effects
 * 
 */
public class GameplayEffectManager : MonoBehaviour
{
    private List<GameplayEffectInstance> activeEffects = new();
    /*Apply the instance effect*/
    public void ApplyEffect(GameplayEffect effect, AttributeSystem target)
    {
        if (effect == null || target == null) return;

        if (effect.EffectTime == EffectTime.Instant || effect.EffectTime == EffectTime.Duration)
        {
            var instance = new GameplayEffectInstance(effect, target);
            instance.Apply();
            activeEffects.Add(instance);
        }
        else if (effect.EffectTime == EffectTime.Infinite)
        {
            target.ApplyEffectOnce(effect);
        }
    }
    /*If one of the effects its finnish is remove*/
    private void Update()
    {
        float dt = Time.deltaTime;
        for (int i = activeEffects.Count - 1; i >= 0; i--)
        {
            var inst = activeEffects[i];
            inst.Tick(dt);
            if (inst.IsEnded)
            {
                activeEffects.RemoveAt(i);
            }
        }
    }
    /*Remove the effect*/
    public void RemoveEffect(GameplayEffectInstance instance)
    {
        if (instance == null) return;
        instance.End();
    }
}
