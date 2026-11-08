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
    
    private GameplayEffectManager OwnEffectManager;
    private AttributeSystem OwnAttributeSystem;
    
    private bool AlreadyHitPlayer = false;

    private void Awake()
    {
        impulseSource = GetComponent<CinemachineImpulseSource>();
        OwnEffectManager = owner.GetComponent<GameplayEffectManager>();
        OwnAttributeSystem = owner.GetComponent<AttributeSystem>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Player") && !AlreadyHitPlayer)
        {
            AlreadyHitPlayer = true;
            DamagePlayer(other);
        }
    }

    private void DamagePlayer(Collider other)
    {
        MakeInvincible invincible = other.gameObject.GetComponent<MakeInvincible>();
        DefenseState defense = other.gameObject.GetComponent<DefenseState>();
        if (invincible != null && invincible.IsInvincible())
        {
            return;
        }
        HasHitPlayer = true;
        HitResult result = defense.ResolveHit();

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
        OwnEffectManager.ApplyEffect(GetParriedEffect, OwnAttributeSystem);
    }

    private void NotParried(GameObject player, bool isBlocked)
    {
        if (!DamageResolver.TryResolveDefender(player, false, owner, out DamageResolver.Defender defender))
        {
            return;
        }

        CameraShakeManager.instance.CameraShake(CameraShakeData, impulseSource);

        if (isCounter)
        {
            defender.Effects.ApplyEffect(counterEffect, defender.Attributes);
            isCounter = false;
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
