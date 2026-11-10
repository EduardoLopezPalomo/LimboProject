using UnityEngine;
using Unity.Behavior;

/// <summary>
/// Damage of the thrown projectile (the ProyectileThrown VFXDefinition, spawned by DamageProjectileSpec
/// through the pooled VFXManager).
///
/// The class only decides *which* effect this contact is worth. The hit itself is executed by
/// DamageResolver.ApplySingle, the same pipeline every weapon and hazard goes through, so defender
/// resolution, rumble, the loud-drop policy and the ApplyEffect call live in one place instead of being
/// re-implemented per damage source.
///
/// The prefab this sits on is tagged to hit the Enemy tag, so the guard check is the enemy-side one
/// (RotateAlongPlayer + the behaviour graph's AttackingState), exactly like PlayerWeaponDamage. A target
/// that carries a DefenseState instead (a player-guard style defender, if the prefab is ever reused the
/// other way round) is evaluated through the shared guard API so blocking and perfect parrying work too.
/// </summary>
public class PlayerDamageProjectile : MonoBehaviour, IHasInstigator
{
    [SerializeField] private GameplayEffect DamageEffect;
    [SerializeField] private GameplayEffect BlockedEffect;
    [SerializeField] private string ObjectToDamage;

    [Tooltip("Applied to whoever threw this projectile when a DefenseState defender perfect-parries it. " +
             "Optional: without it a parry only stops the damage and pays nothing back.")]
    [SerializeField] private GameplayEffect ParryEffect;

    [Header("Sound")]
    [SerializeField] private SFXDefinition def;

    /// <summary>Rumble when the projectile lands (low 0.6 / high 0.6 / 0.2s), same weight as a weapon hit.</summary>
    private static readonly DamageResolver.Rumble DamageRumble = new DamageResolver.Rumble
    {
        LowFrequency = 0.6f,
        HighFrequency = 0.6f,
        Duration = 0.2f
    };

    /// <summary>Rumble when the hit is guarded: present but lighter than taking it.</summary>
    private static readonly DamageResolver.Rumble BlockedRumble = new DamageResolver.Rumble
    {
        LowFrequency = 0.2f,
        HighFrequency = 0.4f,
        Duration = 0.15f
    };

    /// <summary>
    /// Who threw this projectile, pushed in by the spawner (see IHasInstigator). Only used to hand a
    /// perfect-parry reward back; a projectile nobody claims simply cannot be punished.
    /// </summary>
    private GameObject instigator;

    /// <summary>
    /// True when this prefab cannot do anything: ObjectToDamage is not a tag Unity knows, or no
    /// DamageEffect is assigned. Reported once and the component is disabled, instead of throwing
    /// (CompareTag on an undefined tag) or silently dropping every contact.
    /// </summary>
    private bool isUnusable = false;

    private void Awake()
    {
        if (!DamageResolver.TagExists(ObjectToDamage, this))
        {
            Debug.LogError(DamageResolver.Label(gameObject) + $" 'ObjectToDamage' is '{ObjectToDamage}', which is " +
                "not a tag defined in Project Settings (built-in tags such as 'Player' count). This projectile " +
                "is DISABLED so it stops throwing on every contact; fix the tag on the prefab.");
            isUnusable = true;
            enabled = false;
            return;
        }

        if (DamageEffect == null)
        {
            Debug.LogError(DamageResolver.Label(gameObject) + " has no DamageEffect assigned, so it can never " +
                "do anything. It is DISABLED; assign the effect on the prefab.");
            isUnusable = true;
            enabled = false;
        }
    }

    private void OnEnable()
    {
        // Projectiles come back out of the VFX pool: one that is switched on must never still carry the
        // character that threw the previous use of it.
        instigator = null;

        DamageResolver.PlaySound(def, transform);
    }

    /// <summary>See IHasInstigator.</summary>
    public void SetInstigator(GameObject instigator)
    {
        this.instigator = instigator;
    }

    void OnTriggerEnter(Collider other)
    {
        if (isUnusable) return;

        if (!other.gameObject.CompareTag(ObjectToDamage)) return;

        GameObject target = other.gameObject;

        MakeInvincible invincible = target.GetComponentInParent<MakeInvincible>();
        if (invincible != null && invincible.IsInvincible()) return;

        DefenseState defense = target.GetComponent<DefenseState>();
        if (defense == null) defense = target.GetComponentInParent<DefenseState>();

        if (defense != null)
        {
            ApplyGuardedHit(defense, target);
            return;
        }

        ApplyEnemyHit(target);
    }

    /// <summary>
    /// Defender with the shared guard component: the guard state decides the effect, and a perfect parry
    /// pays the thrower back instead of being the empty branch it used to be.
    /// </summary>
    private void ApplyGuardedHit(DefenseState defense, GameObject target)
    {
        HitResult result = defense.ResolveHit();

        switch (result)
        {
            case HitResult.PerfectParry:
                PunishInstigator();
                break;
            case HitResult.Blocked:
                ApplyBlocked(target);
                break;
            case HitResult.FullDamage:
                ApplyDamage(target);
                break;
        }
    }

    /// <summary>
    /// Enemy target, decided the way PlayerWeaponDamage decides it: an enemy that is orbiting with its
    /// weapon up and whose graph reports Defend guards the hit. Missing AI wiring is not an error - the
    /// enemy just cannot guard, so the projectile lands.
    /// </summary>
    private void ApplyEnemyHit(GameObject target)
    {
        if (IsEnemyGuarding(target))
        {
            ApplyBlocked(target);
            return;
        }

        ApplyDamage(target);
    }

    /// <summary>
    /// True when the enemy is blocking and its behaviour graph reports the Defend state. The blackboard
    /// lookup result is honoured: a missing AttackingState variable does not read as a real state.
    /// </summary>
    private bool IsEnemyGuarding(GameObject target)
    {
        RotateAlongPlayer orbit = target.GetComponentInParent<RotateAlongPlayer>();
        if (orbit == null || !orbit.isBlocking) return false;

        BehaviorGraphAgent agent = target.GetComponentInParent<BehaviorGraphAgent>();
        if (agent == null || agent.BlackboardReference == null) return false;

        AttackingState state = default(AttackingState);
        if (!agent.BlackboardReference.GetVariableValue("AttackingState", out state)) return false;

        return state == AttackingState.Defend;
    }

    private void ApplyDamage(GameObject target)
    {
        DamageResolver.ApplySingle(new DamageResolver.SingleHit
        {
            Attacker = Shooter,
            Target = target,
            Effect = DamageEffect,
            SearchTargetInParent = true,
            Haptics = DamageRumble
        });
    }

    private void ApplyBlocked(GameObject target)
    {
        if (BlockedEffect == null)
        {
            Debug.LogWarning(DamageResolver.Label(Shooter) + $" guarded a projectile but '{name}' has no " +
                "BlockedEffect assigned, so the guard is silent and does nothing. Assign one on the prefab.");
            return;
        }

        DamageResolver.ApplySingle(new DamageResolver.SingleHit
        {
            Attacker = Shooter,
            Target = target,
            Effect = BlockedEffect,
            SearchTargetInParent = true,
            Haptics = BlockedRumble
        });
    }

    /// <summary>
    /// The character the resolver credits for this hit: whoever threw the projectile, or the projectile
    /// itself when nobody claims it (one left in the scene, or one spawned without wiring).
    /// </summary>
    private GameObject Shooter
    {
        get { return instigator != null ? instigator : gameObject; }
    }

    /// <summary>
    /// Silent when nobody threw this projectile, and only reachable for a defender that carries a
    /// DefenseState - enemies counter through the blackboard instead of parrying.
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
