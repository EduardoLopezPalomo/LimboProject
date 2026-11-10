using System;
using System.Collections.Generic;
using Cinemachine;
using Unity.Behavior;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Events;

/// <summary>
/// CameraShakeState enumerates different types of camera shake effects.
/// Used to control camera feedback for different combat events.
/// </summary>
[Serializable]
public enum CameraShakeState
{
    attack1,
    attack2,
    attack3,
    attack4,
    hitShield,
    gotBlocked
}

/// <summary>
/// CameraShakeEntry holds camera shake configuration data.
/// Maps a shake state to its actual shake parameters.
/// </summary>
[Serializable]
public class CameraShakeEntry
{
    public CameraShakeState State;
    public CameraShakeData Data;
}

/// <summary>
/// PlayerWeaponDamage handles all damage-related logic for player weapons.
/// Features:
/// - Combo-based damage effects (5 steps)
/// - Sweep attack collision detection
/// - Camera shake effects
/// - Counter-attack mechanics
/// - Invincibility checks
/// - Visual and haptic feedback
/// </summary>
public class PlayerWeaponDamage : MonoBehaviour
{
    /// <summary>
    /// UnityEvent triggered when the enemy performs a counter-attack.
    /// Called when player is hit while enemy is attacking.
    /// </summary>
    public UnityEvent EnemyCounter;

    /// <summary>
    /// GameplayEffect applied when enemy performs a counter-attack.
    /// Typically applies debuff or damage reduction.
    /// </summary>
    public GameplayEffect CounterEffect;
    
    /// <summary>
    /// GameObject that owns this weapon damage component.
    /// Used for reference and finding related components.
    /// </summary>
    public GameObject owner;
    
    /// <summary>
    /// Number of sweep steps for sweep attacks.
    /// Controls how many positions are checked during a sweep.
    /// Default: 5 positions
    /// </summary>
    public int sweepSteps = 5;
    
    /// <summary>
    /// Flag for drawing Gizmos in editor.
    /// Shows weapon hitbox visualization.
    /// Default: true
    /// </summary>
    public bool drawGizmos = true;

    [Header("Combo effects")]
    /// <summary>
    /// List of gameplay effects applied based on combo number.
    /// Index 0 = first hit, index 4 = fifth hit (max combo).
    /// </summary>
    [SerializeField] private List<GameplayEffect> gameplayEffects;

    [Header("Camera shakes")]
    /// <summary>
    /// List of camera shake configurations.
    /// Each entry defines a shake state and its parameters.
    /// </summary>
    [SerializeField] private List<CameraShakeEntry> cameraShakes;
    
    /// <summary>
    /// Reference to the parent constraint on the weapon.
    /// Used for weapon attachment logic.
    /// </summary>
    [HideInInspector] public ParentConstraint parentConstraint;
    
    /// <summary>
    /// Chance of landing a counter-attack (0.0 to 1.0).
    /// Used in counter-attack logic.
    /// </summary>
    public float counterAttackChance = 0.5f;

    
    private BoxCollider weaponCollider;
    private int ComboNumber = 0;
    private int enemyLayerMask;
    private CinemachineImpulseSource impulseSource;
    private Dictionary<CameraShakeState, CameraShakeData> cameraShakeDict;

    /// <summary>
    /// Rumble played when a combo hit lands (low 0.6 / high 0.6 / 0.2s).
    /// </summary>
    private static readonly DamageResolver.Rumble HitRumble = new DamageResolver.Rumble
    {
        LowFrequency = 0.6f,
        HighFrequency = 0.6f,
        Duration = 0.2f
    };

    /// <summary>
    /// Rumble played when the enemy counters the player (low 0.3 / high 1.0 / 0.2s).
    /// </summary>
    private static readonly DamageResolver.Rumble CounterRumble = new DamageResolver.Rumble
    {
        LowFrequency = 0.3f,
        HighFrequency = 1f,
        Duration = 0.2f
    };
    
    private bool isActive = false;

    /// <summary>
    /// True when this weapon could not be wired to the player at Awake time (no owner assigned and
    /// nothing usable above it) or when the EnemyLayer is missing. Sweeping is then refused instead
    /// of throwing from the middle of a swing.
    /// </summary>
    private bool isUnusable = false;
    private HashSet<Collider> alreadyHit = new HashSet<Collider>();

    private Vector3 lastCenter;
    private Quaternion lastRotation;
    private bool hasLastFrame = false;

    void Awake()
    {
        impulseSource = GetComponent<CinemachineImpulseSource>();
        weaponCollider = GetComponent<BoxCollider>();
        parentConstraint = GetComponent<ParentConstraint>();
        cameraShakeDict = new Dictionary<CameraShakeState, CameraShakeData>();

        int enemyLayer = LayerMask.NameToLayer("EnemyLayer");
        if (enemyLayer < 0)
        {
            Debug.LogError(DamageResolver.Label(gameObject) + " The 'EnemyLayer' layer does not exist in " +
                "Project Settings, so the sweep cannot filter anything. The weapon is DISABLED.");
            isUnusable = true;
            return;
        }
        enemyLayerMask = 1 << enemyLayer;

        if (owner == null)
        {
            owner = FindOwnerInParents();
        }

        if (owner == null)
        {
            Debug.LogError(DamageResolver.Label(gameObject) + " 'owner' is not assigned and no parent " +
                "carries both a GameplayEffectManager and an AttributeSystem. The weapon is DISABLED " +
                "until the owner reference is set.");
            isUnusable = true;
            return;
        }

        GameplayEffectManager ownEffects = owner.GetComponent<GameplayEffectManager>();
        AttributeSystem ownAttributes = owner.GetComponent<AttributeSystem>();

        if (ownEffects == null || ownAttributes == null)
        {
            Debug.LogError(DamageResolver.Label(gameObject) + $" Owner '{owner.name}' is missing required " +
                $"components. GameplayEffectManager={(ownEffects != null)}, " +
                $"AttributeSystem={(ownAttributes != null)}. The weapon is DISABLED.");
            isUnusable = true;
            return;
        }

        foreach (var entry in cameraShakes)
        {
            cameraShakeDict[entry.State] = entry.Data;
        }
    }

    /// <summary>
    /// Recovers a missing owner reference by walking up the hierarchy for the GameObject that owns
    /// both halves of the GameplayAbility system - the object the weapon prefab is supposed to point
    /// at. Still warns, so the asset itself gets repaired instead of relying on this.
    /// </summary>
    private GameObject FindOwnerInParents()
    {
        GameplayEffectManager effects = GetComponentInParent<GameplayEffectManager>();
        AttributeSystem attributes = GetComponentInParent<AttributeSystem>();

        if (effects == null || attributes == null || effects.gameObject != attributes.gameObject) return null;

        Debug.LogWarning(DamageResolver.Label(gameObject) + $" 'owner' was not assigned; recovered " +
            $"'{effects.gameObject.name}' from the parent hierarchy. Assign it on the prefab to " +
            "remove this warning.");
        return effects.gameObject;
    }

    
    void FixedUpdate()
    {
                if (!isActive || isUnusable) return;

        Vector3 currentCenter = weaponCollider.transform.TransformPoint(weaponCollider.center);
        Quaternion currentRotation = weaponCollider.transform.rotation;
        Vector3 halfExtents = Vector3.Scale(weaponCollider.size * 0.5f, weaponCollider.transform.lossyScale);

        if (!hasLastFrame)
        {
            CheckOverlap(currentCenter, halfExtents, currentRotation);
        }
        else
        {
            // Check overlap at interpolated positions between last and current frame
            for (int i = 1; i <= sweepSteps; i++)
            {
                float t = i / (float)sweepSteps;
                Vector3 pos = Vector3.Lerp(lastCenter, currentCenter, t);
                Quaternion rot = Quaternion.Slerp(lastRotation, currentRotation, t);
                CheckOverlap(pos, halfExtents, rot);
            }
        }

        lastCenter = currentCenter;
        lastRotation = currentRotation;
        hasLastFrame = true;
    }

    void CheckOverlap(Vector3 center, Vector3 halfExtents, Quaternion rotation)
    {
        // Find all enemy colliders within the weapon hitbox
        Collider[] hits = Physics.OverlapBox(center, halfExtents, rotation, enemyLayerMask, QueryTriggerInteraction.Collide);
        foreach (var hit in hits)
        {
            if (!alreadyHit.Contains(hit))
            {
                alreadyHit.Add(hit);
                DamageEnemy(hit);
            }
        }
    }

    private void DamageEnemy(Collider other)
    {
        BehaviorGraphAgent agent = other.gameObject.GetComponentInParent<BehaviorGraphAgent>();
        RotateAlongPlayer rotateAlongPlayer = other.gameObject.GetComponentInParent<RotateAlongPlayer>();

        // An enemy without its AI wiring used to throw here, which also aborted the sweep for every
        // remaining enemy in the same step. It can still be damaged, it just cannot offer a counter.
        if (agent != null && rotateAlongPlayer != null && CanCounter(rotateAlongPlayer, agent))
        {
            Counter(other, agent);
            return;
        }
        else if (agent == null || rotateAlongPlayer == null)
        {
            Debug.LogWarning(DamageResolver.Label(owner) + $" '{other.gameObject.name}' was hit without its " +
                $"AI wiring (BehaviorGraphAgent={(agent != null)}, " +
                $"RotateAlongPlayer={(rotateAlongPlayer != null)}). The counter check is skipped.");
        }

        // Check for invincibility
        MakeInvincible invincible = other.gameObject.GetComponentInParent<MakeInvincible>();
        if (invincible && invincible.IsInvincible())
        {
            return;
        }

        DamageResolver.ApplyHit(new DamageResolver.ComboHit
        {
            Attacker = owner,
            Target = other.gameObject,
            ComboIndex = ComboNumber,
            NormalEffects = gameplayEffects,
            SearchTargetInParent = true,
            Shake = GetShake(ComboNumber),
            ImpulseSource = impulseSource,
            Haptics = HitRumble
        });
    }

    /// <summary>
    /// True when this enemy is guarding and the roll says the attack gets countered. The blackboard
    /// lookup result is honoured: a missing AttackingState variable no longer reads as a real state.
    /// </summary>
    private bool CanCounter(RotateAlongPlayer rotateAlongPlayer, BehaviorGraphAgent agent)
    {
        if (!rotateAlongPlayer.isBlocking) return false;
        if (agent.BlackboardReference == null) return false;

        AttackingState state = default(AttackingState);
        if (!agent.BlackboardReference.GetVariableValue("AttackingState", out state)) return false;

        return state == AttackingState.Defend && UnityEngine.Random.Range(0f, 1f) < counterAttackChance;
    }

    /// <summary>
    /// The enemy caught the player mid-swing: tell the AI, hand the player its counter effect and
    /// play the counter feedback through the shared resolver.
    /// </summary>
    private void Counter(Collider other, BehaviorGraphAgent agent)
    {
        EnemyCounter.Invoke();
        agent.BlackboardReference.SetVariableValue("AttackingState", AttackingState.CounterAttack);
        DamageResolver.PlayShake(GetShake(CameraShakeState.gotBlocked), impulseSource);

        CharacterVFX vfx = other.gameObject.GetComponentInParent<CharacterVFX>();
        CharacterSFX sfx = other.gameObject.GetComponentInParent<CharacterSFX>();
        if (vfx != null && sfx != null)
        {
            vfx.SpawnVFX(VFXType.Counter);
            sfx.SpawnSFX(SFXType.Counter);
        }

        // The counter debuff lands on the attacking player, so the resolver is pointed at the owner.
        DamageResolver.ApplySingle(new DamageResolver.SingleHit
        {
            Attacker = owner,
            Target = owner,
            Effect = CounterEffect,
            Haptics = CounterRumble
        });
    }

    /// <summary>
    /// Maps a combo step to its camera shake. Returns null when the step has no shake configured
    /// (combo 5 and above intentionally have none).
    /// </summary>
    private CameraShakeData? GetShake(CameraShakeState state)
    {
        if (cameraShakeDict != null && cameraShakeDict.TryGetValue(state, out CameraShakeData data))
        {
            return data;
        }
        return null;
    }

    /// <summary>
    /// Maps a combo number to the camera shake configured for it.
    /// </summary>
    private CameraShakeData? GetShake(int combo)
    {
        switch (combo)
        {
            case 0: return GetShake(CameraShakeState.attack1);
            case 1: return GetShake(CameraShakeState.attack2);
            case 2: return GetShake(CameraShakeState.attack3);
            case 3: return GetShake(CameraShakeState.attack4);
            default: return null;
        }
    }

    /// <summary>
    /// Sets the current combo number.
    /// Called by PlayerWeaponController during attacks.
    /// </summary>
    /// <param name="value">Current combo count</param>
    public void SetComboNumber(int value)
    {
        ComboNumber = value;
    }
    
    /// <summary>
    /// Activates the weapon hitbox for collision detection.
    /// Starts the sweep attack detection.
    /// </summary>
    public void ActivateHitbox()
    {
        isActive = true;
        alreadyHit.Clear();
        hasLastFrame = false;
    }

    /// <summary>
    /// Deactivates the weapon hitbox.
    /// Stops collision detection.
    /// </summary>
    public void DeactivateHitbox()
    {
        isActive = false;
        hasLastFrame = false;
    }
    
    void OnDrawGizmosSelected()
    {
        if (!drawGizmos || weaponCollider == null) return;
        Gizmos.color = isActive ? Color.red : Color.green;
        Matrix4x4 rotMatrix = Matrix4x4.TRS(
            weaponCollider.transform.TransformPoint(weaponCollider.center),
            weaponCollider.transform.rotation,
            weaponCollider.transform.lossyScale);
        Gizmos.matrix = rotMatrix;
        Gizmos.DrawWireCube(Vector3.zero, weaponCollider.size);
    }

}
