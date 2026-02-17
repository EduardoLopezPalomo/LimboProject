using UnityEngine;
/*
 *  Its the instance of the gameplay effect
 *  It has teh gameplat effect, the atribute target, the time remaining if nedeed and a check if its ended
 * 
 */
public class GameplayEffectInstance
{
    private GameplayEffect Effect;
    private AttributeSystem Target;
    public float TimeRemaining;
    public bool IsEnded { get; private set; }

    public readonly System.Guid Id = System.Guid.NewGuid();

    public GameplayEffectInstance(GameplayEffect effect, AttributeSystem target)
    {
        this.Effect = effect;
        this.Target = target;
        TimeRemaining = effect.EffectTime == EffectTime.Instant ? Time.deltaTime : effect.Duration;
    }
    /*Apply the effect*/
    public void Apply()
    {
        Target.AddModifier(Effect.TargetAttribute, Id, Effect.Modifier, Effect.Value);
    }
    /*If the effect has a duration check the timer*/
    public void Tick(float deltaTime)
    {
        if (IsEnded) return;

        if (Effect.EffectTime == EffectTime.Duration || Effect.EffectTime == EffectTime.Instant)
        {
            TimeRemaining -= deltaTime;
            if (TimeRemaining <= 0f)
            {
                End();
            }
        }
    }
    /*End the effect*/
    public void End()
    {
        if (IsEnded) return;
        Target.RemoveModifier(Effect.TargetAttribute, Id);
        IsEnded = true;
    }
}
