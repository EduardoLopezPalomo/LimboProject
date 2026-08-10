using UnityEngine;

public enum GuardState { Idle, Parry, Block, Recovery }
public enum HitResult { PerfectParry, Blocked, FullDamage }

public class DefenseState : MonoBehaviour
{
    public GuardState State { get; private set; } = GuardState.Idle;
    public GameplayEffect BlockEffect;

    private AttributeSystem attributeSystem;
    private GameplayEffectManager effectManager;
    private ParrySpec activeSpec;
    private string resistanceName = "Resistance";

    private void Awake()
    {
        attributeSystem = GetComponent<AttributeSystem>();
        effectManager = GetComponent<GameplayEffectManager>();
    }
    
    public void SetState(GuardState newState, ParrySpec caller)
    {
        activeSpec = caller;
        State = newState;
    }
    
    public HitResult ResolveHit()
    {
        switch (State)
        {
            case GuardState.Parry:
                State = GuardState.Idle;
                activeSpec?.NotifyPerfectParry();
                return HitResult.PerfectParry;

            case GuardState.Block:
                effectManager.ApplyEffect(BlockEffect, attributeSystem);
                if (attributeSystem.GetAttributeValue(resistanceName) <= 0f)
                    activeSpec?.NotifyGuardBreak();
                return HitResult.Blocked;

            default:
                return HitResult.FullDamage;
        }
    }
}
