using Cinemachine;
using UnityEngine;

/// <summary>
/// Damage of the single-hit hazards: arrows, fireballs, lightning strikes, ice hits and the laser
/// impact. The class only decides *which* effect this contact is worth; the hit itself is executed by
/// <see cref="DamageResolver.ApplySingle"/>, which owns defender resolution, camera shake, rumble and
/// the ApplyEffect call for every damage path in the game.
/// </summary>
public class LightingDamage : MonoBehaviour, IHasInstigator
{
    public CameraShakeData CameraShakeData;

    [SerializeField] private GameplayEffect DamageEffect;
    [SerializeField] private GameplayEffect BlockedEffect;

    [Tooltip("Applied to whoever spawned this hazard when the player perfect-parries the hit. Optional: " +
             "without it a parry of this hazard only stops the damage and pays nothing back.")]
    [SerializeField] private GameplayEffect ParryEffect;

    [SerializeField] private string ObjectToDamage;

    [Header("Sound")]
    [SerializeField] private SFXDefinition def;

    /// <summary>Rumble when the hazard lands (low 0.6 / high 0.6 / 0.2s), same weight as a weapon hit.</summary>
    private static readonly DamageResolver.Rumble DamageRumble = new DamageResolver.Rumble
    {
        LowFrequency = 0.6f,
        HighFrequency = 0.6f,
        Duration = 0.2f
    };

    /// <summary>Rumble when the player blocks the hazard: present but lighter than taking it.</summary>
    private static readonly DamageResolver.Rumble BlockedRumble = new DamageResolver.Rumble
    {
        LowFrequency = 0.2f,
        HighFrequency = 0.4f,
        Duration = 0.15f
    };

    private CinemachineImpulseSource impulseSource;

    /// <summary>
    /// The character that spawned this hazard, pushed in by whoever created it (see
    /// <see cref="IHasInstigator"/>). Null for hazards that belong to nobody - ground fire, wall
    /// decals, anything placed in the scene - which is a valid authoring choice, not an error.
    /// </summary>
    private GameObject instigator;

    /// <summary>
    /// True when <see cref="ObjectToDamage"/> is not a tag Unity knows. The trigger is then refused
    /// instead of throwing a UnityException on every single contact the prefab makes, which is what
    /// used to happen with a typo: the exception aborted OnTriggerEnter before any damage was done,
    /// so the hazard looked alive while never hitting anything.
    /// </summary>
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
        // Hazards are pooled: the object that comes back from the pool must never still carry the
        // character that spawned the previous use of it.
        instigator = null;

        DamageResolver.PlaySound(def, transform);
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

        MakeInvincible invincible = other.gameObject.GetComponent<MakeInvincible>();
        if (invincible != null && invincible.IsInvincible()) return;

        DefenseState defense = other.gameObject.GetComponent<DefenseState>();
        if (defense == null) defense = other.gameObject.GetComponentInParent<DefenseState>();

        if (defense == null)
        {
            Debug.LogError(DamageResolver.Label(gameObject) + $"'{other.gameObject.name}' is tagged " +
                $"'{ObjectToDamage}' but carries no DefenseState, so the hit cannot be evaluated. " +
                "The hit is being DROPPED.");
            return;
        }

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
                    Target = other.gameObject,
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
                    Target = other.gameObject,
                    Effect = DamageEffect,
                    Shake = CameraShakeData,
                    ImpulseSource = impulseSource,
                    Haptics = DamageRumble
                });
                break;
        }
    }

    /// <summary>
    /// A perfect parry used to be an empty branch on every hazard, so timing a fireball gave the
    /// player nothing but the absence of damage. The character that threw it now takes
    /// <see cref="ParryEffect"/>, the same way <c>EnemyWeaponDamage</c> punishes a parried swing.
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

