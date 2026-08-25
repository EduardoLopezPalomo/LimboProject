using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerAbilities : MonoBehaviour
{
    public AbilityHolder AbilityHolder;
    public Image[] images;
    public TextMeshProUGUI[] texts;

    private float[] AbilitiesCooldowns = new []{0f, 0f};
    
    private HashSet<GameplayAbilitySpec> activeCooldowns = new HashSet<GameplayAbilitySpec>();


    void OnEnable()
    {
        if (AbilityHolder == null) return;
        
        AbilityHolder.OnEquipAbility += SetSprite;
        AbilityHolder.OnUnequipAbility += ClearSprite;
        AbilityHolder.OnActivateEquipAbility += ActivateAbility;
        
        SetSprite(1);
        SetSprite(2);
        
        foreach (var ability in activeCooldowns)
        {
            StartCoroutine(CooldownTimer(ability));
        }
    }
    
    void OnDisable()
    {
        if (AbilityHolder == null) return;

        AbilityHolder.OnEquipAbility -= SetSprite;
        AbilityHolder.OnUnequipAbility -= ClearSprite;
        AbilityHolder.OnActivateEquipAbility -= ActivateAbility;
    }

    Sprite GetSprite(int slot)
    {
        GameplayAbility ability = AbilityHolder.GetEquippedAbility(slot);
        AbilitiesCooldowns[slot-1] = ability ? ability.Cooldown : 0f;
        return ability ? ability.icon : null;
    }

    void SetSprite(int slot)
    {
        Sprite sprite1 = GetSprite(slot);
        if (sprite1 != null)
        {
            images[slot-1].enabled = true;
            images[slot-1].sprite = sprite1;
        }
        else
        {
            images[slot-1].enabled = false;
        }
        
        RefreshSlotText(slot);
    }
    
    void ClearSprite(int slot)
    {
        images[slot - 1].enabled = false;
        texts[slot - 1].text = "";
    }

    void ActivateAbility(int slot, GameplayAbilitySpec ability)
    {
        if(ability == null) return;
        if (activeCooldowns.Contains(ability)) return;

        activeCooldowns.Add(ability);
        StartCoroutine(CooldownTimer(ability));
    }

    private IEnumerator CooldownTimer(GameplayAbilitySpec ability)
    {
        float cooldown = ability.GetCooldown();

        while (cooldown > 0)
        {
            int currentSlot = FindSlotForAbility(ability);
            if (currentSlot != -1)
            {
                texts[currentSlot - 1].text = cooldown.ToString("F1");
            }

            yield return null; 
            
            cooldown = ability.GetCooldown();
        }
        
        int finalSlot = FindSlotForAbility(ability);
        if (finalSlot != -1)
        {
            texts[finalSlot - 1].text = "";
        }

        activeCooldowns.Remove(ability);
    }
    
    private int FindSlotForAbility(GameplayAbilitySpec spec)
    {
        for (int slot = 1; slot <= texts.Length; slot++)
        {
            GameplayAbility equipped = AbilityHolder.GetEquippedAbility(slot);
            if (equipped != null && equipped == spec.ability)
            {
                return slot;
            }
        }
        return -1;
    }
    private void RefreshSlotText(int slot)
    {
        GameplayAbility equipped = AbilityHolder.GetEquippedAbility(slot);
        if (equipped == null)
        {
            texts[slot - 1].text = "";
            return;
        }

        foreach (var spec in activeCooldowns)
        {
            if (spec.ability == equipped)
            {
                texts[slot - 1].text = spec.GetCooldown().ToString("F1");
                return;
            }
        }

        texts[slot - 1].text = "";
    }


}
