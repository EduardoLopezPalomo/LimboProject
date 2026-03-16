using System.Collections.Generic;
using UnityEngine;

public class AbilityAnimationBridge : MonoBehaviour
{
    public AbilityHolder abilityHolder;

    private List<GameplayAbilitySpec> currentSpec => abilityHolder.ActiveSpecs;

    /*
     *  Notifies of LightAttack
     */
    public void ComboWindowOpen()
    {
        foreach (var spec in currentSpec)
        {
            if (spec is LightAttackSpec lightAttackSpec)
            {
                lightAttackSpec.ComboWindowOpen();
            }
        }
    }

    public void ComboWindowClose()
    {
        foreach (var spec in currentSpec)
        {
            if (spec is LightAttackSpec lightAttackSpec)
            {
                lightAttackSpec.ComboWindowClose();
            }
        }
    }

    public void EndComboAnimation()
    {
        foreach (var spec in currentSpec)
        {
            if (spec is LightAttackSpec lightAttackSpec)
            {
                lightAttackSpec.EndComboAnimation();
            }
        }
    }

    public void EnableRootMotion()
    {
        foreach (var spec in currentSpec)
        {
            if (spec is LightAttackSpec lightAttackSpec)
            {
                lightAttackSpec.EnableRootMotion();
            }
        }
    }

    public void DisableRootMotion()
    {
        foreach (var spec in currentSpec)
        {
            if (spec is LightAttackSpec lightAttackSpec)
            {
                lightAttackSpec.DisableRootMotion();
            }
        }
    }

    public void EnableSwordCollider()
    {
        foreach (var spec in currentSpec)
        {
            if (spec is LightAttackSpec lightAttackSpec)
            {
                lightAttackSpec.EnableSwordCollider();
            }
        }
    }

    public void DisableSwordCollider()
    {
        foreach (var spec in currentSpec)
        {
            if (spec is LightAttackSpec lightAttackSpec)
            {
                lightAttackSpec.DisableSwordCollider();
            }
        }
    }
}
