using System.Collections.Generic;
using UnityEngine;

public class AbilityAnimationBridge : MonoBehaviour
{
    public AbilityHolder abilityHolder;

    private List<GameplayAbilitySpec> currentSpec => abilityHolder.ActiveSpecs;

    /*
     *  Notifies of SwordAttack
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

    public void EnableSwordCollider()
    {
        foreach (var spec in currentSpec)
        {
            if (spec is LightAttackSpec lightAttackSpec)
            {
                lightAttackSpec.EnableSwordCollider();
            }
            if (spec is BarbarianAttackSpec barbarianAttackSpec)
            {
                barbarianAttackSpec.EnableSwordCollider();
            }
            if (spec is CounterAttackSpec barbarianCounterAttackSpec)
            {
                barbarianCounterAttackSpec.EnableSwordCollider();
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
            if (spec is BarbarianAttackSpec barbarianAttackSpec)
            {
                barbarianAttackSpec.DisableSwordCollider();
            }
            if (spec is CounterAttackSpec barbarianCounterAttackSpec)
            {
                barbarianCounterAttackSpec.DisableSwordCollider();
            }
        }
    }
    
    public void StartAttackAssist()
    {
        foreach (var spec in currentSpec)
        {
            if (spec is LightAttackSpec lightAttackSpec)
            {
                lightAttackSpec.StartAttackAssist();
            }
            if (spec is CounterAttackSpec barbarianCounterAttackSpec)
            {
                barbarianCounterAttackSpec.StartAttackAssist();
            }
        }
    }
    
    public void EndAttackAssist()
    {
        foreach (var spec in currentSpec)
        {
            if (spec is LightAttackSpec lightAttackSpec)
            {
                lightAttackSpec.EndAttackAssist();
            }
            if (spec is CounterAttackSpec barbarianCounterAttackSpec)
            {
                barbarianCounterAttackSpec.EndAttackAssist();
            }
        }
    }

    public void DisableInputPlayer()
    {
        foreach (var spec in currentSpec)
        {
            if (spec is LightAttackSpec lightAttackSpec)
            {
                lightAttackSpec.DisableInput();
            }
        }
    }
    public void CanHaveInput()
    {
        foreach (var spec in currentSpec)
        {
            if (spec is LightAttackSpec lightAttackSpec)
            {
                lightAttackSpec.CanHaveInput();
            }
        }
    }

    public void EndBarbarianAttack()
    {
        foreach (var spec in currentSpec)
        {
            if (spec is BarbarianAttackSpec barbarianAttackSpec)
            {
                barbarianAttackSpec.EndAbility();
            }
            if (spec is CounterAttackSpec barbarianCounterAttackSpec)
            {
                barbarianCounterAttackSpec.EndAbility();
            }
        }
    }
    
    public void StayStunned()
    {
        foreach (var spec in currentSpec)
        {
            if (spec is BarbarianAttackSpec barbarianAttackSpec)
            {
                barbarianAttackSpec.StartStunned();
            }
        }
    }
    /*
     * Notifies of BowAttack
     *
     */
    
    public void ShootArrow()
    {
        foreach (var spec in currentSpec)
        {
            if (spec is BowAttackSpec BowAttackSpec)
            {
                BowAttackSpec.ShootArrow();
            }
        }
    }
    public void AlreadyShoot()
    {
        foreach (var spec in currentSpec)
        {
            if (spec is BowAttackSpec BowAttackSpec)
            {
                BowAttackSpec.AlreadyShoot();
            }
        }
    }
    
    public void ShowArrowPath()
    {
        foreach (var spec in currentSpec)
        {
            if (spec is BowAttackSpec BowAttackSpec)
            {
                BowAttackSpec.ShowArrowPath();
            }
        }
    }

    /*
    * Notifies of AreaRayAttack
    *
    */

    public void AreaRayAttack()
    {
        foreach (var spec in currentSpec)
        {
            if (spec is AreaRayAttackSpec areaRayAttackSpec)
            {
                areaRayAttackSpec.ActivateAbility();
            }
        }
    }

    /*
   * Notifies of LightAura
   *
   */

    public void LightAura()
    {
        foreach (var spec in currentSpec)
        {
            if (spec is LightAuraSpec lightAuraSpec)
            {
                lightAuraSpec.ActivateAbility();
            }
        }
    }
    
    /*
     * Notifies of FireAttack
     *
     */
    
    public void SpawnFire()
    {
        foreach (var spec in currentSpec)
        {
            if (spec is FireAttackSpec fireAttackSpec)
            {
                fireAttackSpec.SpawnFire();
            }
        }
    }
    /*
     * Notifies of HealTarget
     *
     */
    public void HealTarget()
    {
        foreach (var spec in currentSpec)
        {
            if (spec is HealTargetSpec healTargetSpec)
            {
                healTargetSpec.HealTarget();
            }
        }
    }

    public void StopHealParticle()
    {
        foreach (var spec in currentSpec)
        {
            if (spec is HealTargetSpec healTargetSpec)
            {
                healTargetSpec.StopHealParticle();
            }
        }
    }
    /*
     * Notifies of Dash
     *
     */
    public void StartMoving()
    {
        foreach (var spec in currentSpec)
        {
            if (spec is DashSpec dashSpec)
            {
                dashSpec.StartMoving();
            }
        }
    }
    
    public void EndDash()
    {
        foreach (var spec in currentSpec)
        {
            if (spec is DashSpec dashSpec)
            {
                dashSpec.EndDash();
            }
        }
    }
    
    

}
