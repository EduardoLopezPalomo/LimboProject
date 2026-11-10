using System;
using System.Collections.Generic;
using Cinemachine;
using Unity.Behavior;
using UnityEngine;
using UnityEngine.Events;

public class EnemyWeaponDamage : MonoBehaviour
{
    private bool HasHitPlayer = false;
    private bool isCounter = false;
    private int ComboNumber = 0;
    public GameObject owner;
    public GameplayEffect GetParriedEffect;
    [Header("Combo effects")]
    [SerializeField] private List<GameplayEffect> gameplayEffects;
    
    [Header("Combo effects for player block")]
    [SerializeField] private List<GameplayEffect> gameplayEffectsBlock;

    [Header("Counter attack effect")]
    [SerializeField] private GameplayEffect counterEffect;

    [Header("Camera shakes")]
    public CameraShakeData CameraShakeData;

    private CinemachineImpulseSource impulseSource;
    
    
    private bool AlreadyHitPlayer = false;

    /// <summary>
    /// True when this weapon could not be wired to its owning enemy at Awake time (no owner
    /// assigned and nothing usable above it in the hierarchy). The hitbox is switched off and every
    /// hit is refused instead of throwing from the middle of a swing: an enemy that cannot report
    /// its own damage must not be able to half-attack the player.
    /// </summary>
    private bool isUnusable = false;

    private void Awake()
    {
        impulseSource = GetComponent<CinemachineImpulseSource>();

        if (owner == null)
        {
            owner = FindOwnerInParents();
        }

        if (owner == null)
        {
            Debug.LogError(DamageResolver.Label(gameObject) + " 'owner' is not assigned and no parent " +
                "carries both a GameplayEffectManager and an AttributeSystem. The weapon hitbox is " +
                "DISABLED until the owner reference is set on the character prefab.");
            DisableHitbox();
            return;
        }

        GameplayEffectManager ownEffects = owner.GetComponent<GameplayEffectManager>();
        AttributeSystem ownAttributes = owner.GetComponent<AttributeSystem>();

        if (ownEffects == null || ownAttributes == null)
        {
            Debug.LogError(DamageResolver.Label(gameObject) + $" Owner '{owner.name}' is missing required " +
                $"components. GameplayEffectManager={(ownEffects != null)}, " +
                $"AttributeSystem={(ownAttributes != null)}. The weapon hitbox is DISABLED.");
            DisableHitbox();
        }
    }

    /// <summary>
    /// Recovers a missing owner reference by walking up the hierarchy for the GameObject that owns
    /// both halves of the GameplayAbility system - the object the character prefab is supposed to
    /// point at. Still warns, so the asset itself gets repaired instead of relying on this.
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

    /// <summary>
    /// Switches this weapon's own colliders off so an unconfigured weapon cannot keep triggering hits.
    /// </summary>
    private void DisableHitbox()
    {
        isUnusable = true;

        Collider[] colliders = GetComponents<Collider>();
        for (int i = 0; i < colliders.Length; i++)
        {
            colliders[i].enabled = false;
        }
    }


    void OnTriggerEnter(Collider other)
    {
        if (isUnusable) return;

        if (other.gameObject.layer == LayerMask.NameToLayer("Player") && !AlreadyHitPlayer)
        {
            DamagePlayer(other);
        }
    }

    private void DamagePlayer(Collider other)
    {
        MakeInvincible invincible = other.gameObject.GetComponent<MakeInvincible>();
        if (invincible == null)
        {
            invincible = other.gameObject.GetComponentInParent<MakeInvincible>();
        }

        // Invincibility is evaluated before anything is consumed. AlreadyHitPlayer used to be set
        // in OnTriggerEnter, before this check ran, so a single invincible touch burned the whole
        // swing: no damage, no block, no counter, and the attack simply disappeared.
        if (invincible != null && invincible.IsInvincible())
        {
            return;
        }

        DefenseState defense = other.gameObject.GetComponent<DefenseState>();
        if (defense == null)
        {
            defense = other.gameObject.GetComponentInParent<DefenseState>();
        }
        if (defense == null)
        {
            Debug.LogError(DamageResolver.Label(owner) + $" '{other.gameObject.name}' is on the Player " +
                "layer but has no DefenseState, so the hit cannot be evaluated. The hit is being DROPPED.");
            return;
        }

        AlreadyHitPlayer = true;
        HasHitPlayer = true;

        // Decide first, then pay for the feedback, then apply the effect. Same order the old single
        // ResolveHit() call had, but now the decision can be taken without side effects when a
        // caller needs to know the result before committing to it.
        HitResult result = defense.EvaluateHit();
        defense.PlayHitFeedback(result);

        switch (result)
        {
            case HitResult.PerfectParry:
                GetParried();
                break;
            case HitResult.Blocked:
                NotParried(other.gameObject, true);
                break;
            case HitResult.FullDamage:
                NotParried(other.gameObject, false);
                break;
        }
    }

    private void GetParried()
    {
        // The parry consequence lands on the attacking enemy itself, so the resolver is pointed
        // back at the owner instead of at the player.
        DamageResolver.ApplySingle(new DamageResolver.SingleHit
        {
            Attacker = owner,
            Target = owner,
            Effect = GetParriedEffect,
            ImpulseSource = impulseSource
        });
    }

    private void NotParried(GameObject player, bool isBlocked)
    {
        if (!DamageResolver.TryResolveDefender(player, false, owner, out DamageResolver.Defender defender))
        {
            return;
        }

        DamageResolver.PlayShake(CameraShakeData, impulseSource);

        if (isCounter)
        {
            isCounter = false;
            DamageResolver.ApplySingle(new DamageResolver.SingleHit
            {
                Attacker = owner,
                Target = player,
                Effect = counterEffect,
                ImpulseSource = impulseSource
            });
            return;
        }

        DamageResolver.ApplyResolved(new DamageResolver.ComboHit
        {
            Attacker = owner,
            Target = player,
            ComboIndex = ComboNumber,
            NormalEffects = gameplayEffects,
            BlockedEffects = gameplayEffectsBlock,
            UseBlockedEffects = isBlocked,
            OnBlocked = () => player.GetComponent<Animator>()?.SetTrigger("BlockHit"),
            ImpulseSource = impulseSource
        }, defender);
    }

    public void SetComboNumber(int value)
    {
        ComboNumber = value;
        AlreadyHitPlayer = false;
    }

    public bool GetHasHitPlayer()
    {
        return HasHitPlayer;
    }

    public void SetHasHitPlayer(bool value)
    {
        HasHitPlayer = value;
    }

    public void IsCounterAttack(bool value)
    {
        isCounter = value;
        AlreadyHitPlayer = false;
    }
}
