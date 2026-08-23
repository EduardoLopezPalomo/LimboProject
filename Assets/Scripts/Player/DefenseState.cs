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
    
    public HitResult ResolveHit()
    {
        switch (State)
        {
            case GuardState.Parry:
                State = GuardState.Idle;
                vfx.SpawnVFX(VFXType.Counter);
                sfx.SpawnSFX(SFXType.Counter);
                CameraShakeManager.instance.CameraShake(CounterCameraShake, impulseSource);
                TimeEffectsManager.DoParrySlowMotion(scale, duration, recoverDuration);
                activeSpec?.NotifyPerfectParry();
                return HitResult.PerfectParry;

            case GuardState.Block:
                if (attributeSystem.GetAttributeValue(resistanceName) <= 0f)
                    activeSpec?.NotifyGuardBreak();
                return HitResult.Blocked;

            default:
                return HitResult.FullDamage;
        }
    }

    public void SetHitBLockFalse()
    {
        animator.SetBool("BlockHit", false);
    }
}
