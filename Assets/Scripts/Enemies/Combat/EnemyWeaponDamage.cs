using System.Collections.Generic;
using Unity.Behavior;
using UnityEngine;
using UnityEngine.Events;

public class EnemyWeaponDamage : MonoBehaviour
{
    private bool HasHitPlayer = false;
    private bool isCounter = false;
    private int ComboNumber = 0;
    [Header("Combo effects")]
    [SerializeField] private List<GameplayEffect> gameplayEffects;
    
    [Header("Counter attack effect")]
    [SerializeField] private GameplayEffect counterEffect;
    
    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            DamagePlayer(other);
        }
    }

    private void DamagePlayer(Collider other)
    {
        MakeInvincible invincible = other.gameObject.GetComponent<MakeInvincible>();
        GameplayEffectManager effectmanager = other.gameObject.GetComponent<GameplayEffectManager>();
        AttributeSystem attributeSystem = other.gameObject.GetComponent<AttributeSystem>();
        if (effectmanager == null || attributeSystem == null)
        {
            Debug.Log("effectmanager, attributesystem or makeInvincible is null");
            return;
        }
        if (invincible != null && invincible.IsInvincible())
        {
            return;
        }

        if (isCounter)
        {
            effectmanager.ApplyEffect(counterEffect, attributeSystem);
            isCounter = false;
            return;
        }
        
        HasHitPlayer = true;
        
        if (ComboNumber < 0 || ComboNumber >= gameplayEffects.Count)
        {
            Debug.LogWarning("the combo is " + ComboNumber + " But the list doent have that number");
            return;
        }
        
        GameplayEffect effect = gameplayEffects[ComboNumber];

        if (effect == null)
        {
            Debug.LogWarning("the combo number has no effect on the player weapon");
            return;
        }
        
        effectmanager.ApplyEffect(effect, attributeSystem);
        
    }

    public void SetComboNumber(int value)
    {
        ComboNumber = value;
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
    }
}
