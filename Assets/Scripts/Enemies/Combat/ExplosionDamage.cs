using Cinemachine;
using UnityEngine;

/// <summary>
/// Damage over time of the area hazards (ground fire, explosion clouds, laser columns). Every tick is
/// a fresh hit that goes through <see cref="DamageResolver.ApplySingle"/>, so a DoN tick reports,
/// shakes, rumbles and applies its effect exactly like a weapon hit does.
/// </summary>
public class ExplosionDamage : MonoBehaviour, IHasInstigator
{
    public float DamageTickRate = 2f;

    [Tooltip("How far ahead the tick timer is started when the target walks in, in seconds. The default " +
             "1.8 with the default DamageTickRate of 2 makes the first tick land 0.2s after entering; " +
             "with the faster rates the authored prefabs use (0.5 / 0.2) the first tick lands as soon as " +
             "the hazard touches the player, which is how it has always behaved.")]
    public float FirstTickOffset = 1.8f;

    public CameraShakeData CameraShakeData;

    [Header("Damage and blocked")]
    [SerializeField] private GameplayEffect DamageEffect;
    [SerializeField] private GameplayEffect BlockedEffect;

    [Tooltip("Applied to whoever spawned this hazard when the player perfect-parries a tick. Optional: " +
             "without it a parry of this hazard only stops the damage and pays nothing back.")]
    [SerializeField] private GameplayEffect ParryEffect;

    [SerializeField] private string ObjectToDamage;

    [Header("Sound")]
    [SerializeField] private SFXDefinition def;

    /// <summary>Rumble when a tick lands (low 0.6 / high 0.6 / 0.2s), same weight as a weapon hit.</summary>
    private static readonly DamageResolver.Rumble DamageRumble = new DamageResolver.Rumble
    {
        LowFrequency = 0.6f,
        HighFrequency = 0.6f,
        Duration = 0.2f
    };

    /// <summary>Rumble when the player blocks a tick: present but lighter than taking it.</summary>
    private static readonly DamageResolver.Rumble BlockedRumble = new DamageResolver.Rumble
    {
        LowFrequency = 0.2f,
        HighFrequency = 0.4f,
        Duration = 0.15f
    };

    private bool isIn = false;
    private float tickTimer = 0f;

    /// <summary>
    /// What the hazard is currently ticking. The components a tick needs are looked up from this
    /// GameObject on every tick instead of being cached: these prefabs are pooled and switched off
    /// without warning, and a cached DefenseState from the previous spawn used to tick whatever
    /// happened to be inside last - or throw once the old target was destroyed.
    /// </summary>
    private GameObject target;

    private CinemachineImpulseSource impulseSource;

    /// <summary>See <see cref="LightningDamage"/>'s instigator: the character that spawned this hazard, or null.</summary>
    private GameObject instigator;

    /// <summary>See <see cref="LightningDamage"/>'s isUnusable: an undefined ObjectToDamage disables the ticking.</summary>
    private bool isUnusable = false;

    private void Awake()
    {
        impulseSource = GetComponent<CinemachineImpulseSource>();

        if (DamageResolver.TagExists(ObjectToDamage, this)) return;

        Debug.LogError(DamageResolver.Label(gameObject) + $" 'ObjectToDamage' is '{ObjectToDamage}', which is " +
            "not a tag defined in Project Settings (built-in tags such as 'Player' count). This hazard is " +
            "DISABLED so it stops throwing on every contact; fix the tag or clear the field on the prefab.");
        isUnusable = true;
        enabled = false;
    }

    private void OnEnable()
    {
        DamageResolver.PlaySound(def, transform);
    }

    private void OnDisable()
    {
        // A pooled hazard is switched off without OnTriggerExit ever firing. Everything that says
        // "somebody is standing in me" has to go here, or the next spawn resumes ticking the
        // character that was inside the previous one.
        isIn = false;
        target = null;
        instigator = null;
        tickTimer = 0f;
    }

    /// <summary>See <see cref="IHasInstigator"/>.</summary>
    public void SetInstigator(GameObject instigator)
    {
        this.instigator = instigator;
    }

    void OnTriggerEnter(Collider other)
    {
        if (isUnusable) return;

        if (!other.gameObject.CompareTag(ObjectToDamage)) return;

        target = other.gameObject;
        isIn = true;
        tickTimer = FirstTickOffset;
    }

    void OnTriggerExit(Collider other)
    {
        // Only the collider that started the ticking is allowed to stop it. Used to be any collider:
        // an enemy walking out of the flame while the player stood in it cancelled the player's damage.
        if (target == null) return;
        if (other.gameObject != target) return;

        isIn = false;
        target = null;
    }

    void Update()
    {
        if (!isIn) return;

        if (target == null)
        {
            // The target was destroyed or deactivated without an OnTriggerExit (a pooled hazard, a
            // dead character). Ticking a component of a destroyed object is what used to throw here.
            isIn = false;
            return;
        }

        tickTimer += Time.deltaTime;
        if (tickTimer < DamageTickRate) return;

        MakeInvincible invincible = target.GetComponent<MakeInvincible>();
        if (invincible != null && invincible.IsInvincible())
        {
            // i-frames swallow this tick, but the timer keeps running so the tick is retried on the
            // frame the roll ends instead of being lost until the next full DamageTickRate.
            return;
        }

        DefenseState defense = target.GetComponent<DefenseState>();
        if (defense == null) defense = target.GetComponentInParent<DefenseState>();

        if (defense == null)
        {
            Debug.LogError(DamageResolver.Label(gameObject) + $"'{target.name}' is tagged " +
                $"'{ObjectToDamage}' but carries no DefenseState, so the ticking cannot be evaluated. " +
                "The hazard stopped ticking it.");
            isIn = false;
            return;
        }

        tickTimer = 0f;

        HitResult result = defense.ResolveHit();

        switch (result)
        {
            case HitResult.PerfectParry:
                PunishInstigator();
                break;
            case HitResult.Blocked:
                DamageResolver.ApplySingle(new DamageResolver.SingleHit
                {
                    Attacker = gameObject,
                    Target = target,
                    Effect = BlockedEffect,
                    Shake = CameraShakeData,
                    ImpulseSource = impulseSource,
                    Haptics = BlockedRumble
                });
                break;
            case HitResult.FullDamage:
                DamageResolver.ApplySingle(new DamageResolver.SingleHit
                {
                    Attacker = gameObject,
                    Target = target,
                    Effect = DamageEffect,
                    Shake = CameraShakeData,
                    ImpulseSource = impulseSource,
                    Haptics = DamageRumble
                });
                break;
        }
    }

    /// <summary>
    /// A perfect parry used to be an empty branch on every hazard, so timing a tick of ground fire
    /// gave the player nothing but the absence of damage. The character that spawned the hazard now
    /// takes <see cref="ParryEffect"/>, the same way <c>EnemyWeaponDamage</c> punishes a parried swing.
    ///
    /// Silent when nobody spawned this hazard: an environmental flame has no one to punish, and a
    /// player parrying it is not a configuration error.
    /// </summary>
    private void PunishInstigator()
    {
        if (instigator == null) return;

        if (ParryEffect == null)
        {
            Debug.LogWarning(DamageResolver.Label(gameObject) + $" was perfect-parried by '{instigator.name}' " +
                "but has no ParryEffect assigned, so the parry punishes nobody. Assign one on the prefab.");
            return;
        }

        DamageResolver.ApplySingle(new DamageResolver.SingleHit
        {
            Attacker = instigator,
            Target = instigator,
            Effect = ParryEffect
        });
    }
}

