using System;
using UnityEngine;
using UnityEngine.UI;

public class PlayerAbilities : MonoBehaviour
{
    public AbilityHolder AbilityHolder;
    public Image[] images;

    void OnEnable()
    {
        if (AbilityHolder == null) return;
        
        SetSprite(1);
        SetSprite(2);
    }

    Sprite GetSprite(int slot)
    {
        GameplayAbility ability = AbilityHolder.GetEquippedAbility(slot);
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
        
    }


}
