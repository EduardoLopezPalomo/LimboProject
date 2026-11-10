using Cinemachine;
using UnityEngine;

public enum GuardState { Idle, Parry, Block, Recovery }
public enum HitResult { PerfectParry, Blocked, FullDamage }

public class DefenseState : MonoBehaviour
{
    public GuardState State { get; private set; } = GuardState.Idle;
    
    [Header("TimeStop")]
    public float scale = 0.05f;
    public float duration = 0.1f;
    public float recoverDuration = 0.15f;

    [SerializeField] private CameraShakeData CounterCameraShake;

    /// <summary>
    /// Rumble played on a perfect parry (low 1.0 / high 1.0 / 0.2s), the same values the old
    /// inline VibrationManager call used.
    /// </summary>
    private static readonly DamageResolver.Rumble ParryRumble = new DamageResolver.Rumble
    {
        LowFrequency = 1f,
        HighFrequency = 1f,
        Duration = 0.2f
    };
    
    private AttributeSystem attributeSystem;
    private CharacterVFX vfx;
    private CharacterSFX sfx;
    private ParrySpec activeSpec;
    private Animator animator;
    private CinemachineImpulseSource impulseSource;
    private string resistanceName = "Resistance";

    private void Awake()
    {
        attributeSystem = GetComponent<AttributeSystem>();
        animator = GetComponent<Animator>();
        vfx = GetComponent<CharacterVFX>();
        sfx = GetComponent<CharacterSFX>();
        impulseSource = GetComponent<CinemachineImpulseSource>();
    }
    
    public void SetState(GuardState newState, ParrySpec caller)
    {
        activeSpec = caller;
        State = newState;
    }
    
    /// <summary>
    /// Decides what an incoming hit becomes given the current guard state. This half is deliberately
    /// free of feedback and singletons: it only flips GuardState.Parry back to Idle and reports the
    /// result, so a caller can evaluate first, apply the hit, and only then pay for the consequences.
    /// </summary>
    public HitResult EvaluateHit()
    {
        switch (State)
        {
            case GuardState.Parry:
                State = GuardState.Idle;
                return HitResult.PerfectParry;

            case GuardState.Block:
                return HitResult.Blocked;

            default:
                return HitResult.FullDamage;
        }
    }

    /// <summary>
    /// Plays the feedback and fires the guard notifications that belong to an already evaluated hit.
    /// The guard-break check lives here rather than in <see cref="EvaluateHit"/> because running out
    /// of Resistance does not change the result of the hit (it is still Blocked), only what happens
    /// to the defender afterwards.
    /// </summary>
    public void PlayHitFeedback(HitResult result)
    {
        switch (result)
        {
            case HitResult.PerfectParry:
                if (vfx != null && sfx != null)
                {
                    vfx.SpawnVFX(VFXType.Counter);
                    sfx.SpawnSFX(SFXType.Counter);
                }
                else
                {
                    Debug.LogError($"[DefenseState:{name}] perfect parry cannot play its feedback: " +
                        $"CharacterVFX={(vfx != null)}, CharacterSFX={(sfx != null)}.");
                }

                DamageResolver.PlayShake(CounterCameraShake, impulseSource);
                TimeEffectsManager.DoParrySlowMotion(scale, duration, recoverDuration);
                DamageResolver.PlayRumble(ParryRumble);
                activeSpec?.NotifyPerfectParry();
                break;

            case HitResult.Blocked:
                if (attributeSystem == null)
                {
                    Debug.LogError($"[DefenseState:{name}] has no AttributeSystem, so the guard break " +
                        "cannot be checked.");
                }
                else if (attributeSystem.GetAttributeValue(resistanceName) <= 0f)
                {
                    activeSpec?.NotifyGuardBreak();
                }
                break;
        }
    }

    /// <summary>
    /// Evaluate + feedback in one call, in the same order the single method used to run them. Kept
    /// for callers (explosion, lightning) that do not need the two halves apart.
    /// </summary>
    public HitResult ResolveHit()
    {
        HitResult result = EvaluateHit();
        PlayHitFeedback(result);
        return result;
    }

    public void SetHitBLockFalse()
    {
        if (animator == null)
        {
            Debug.LogError($"[DefenseState:{name}] has no Animator, so the BlockHit trigger cannot be cleared.");
            return;
        }

        animator.SetBool("BlockHit", false);
    }
}
