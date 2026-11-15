using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AbilityAnimationBridge : MonoBehaviour
{
    public AbilityHolder abilityHolder;

    /// <summary>Shared empty list, so "nothing is active" costs no allocation (T-046 H3).</summary>
    private static readonly List<GameplayAbilitySpec> NoActiveSpecs = new List<GameplayAbilitySpec>();

    private bool warnedNoHolder;

    /// <summary>
    /// The snapshot every one of these animation-event handlers iterates.
    ///
    /// The copy is load-bearing, not decoration: a spec that ends itself inside a handler unsubscribes
    /// itself from <see cref="AbilityHolder.ActiveSpecs"/> (<c>AbilityHolder.RemoveFromSpec</c>), and
    /// mutating the live list during a <c>foreach</c> throws. So the copy stays.
    ///
    /// What changes is the empty case. Every handler here is an animation event, and most of them fire
    /// on a character that has no spec active at all - which used to allocate a List plus its backing
    /// array for nothing. That case now iterates one shared empty list (a <c>List&lt;T&gt;</c> enumerator
    /// is a struct, so even that allocates nothing). With specs active the result is exactly
    /// <c>live.ToList()</c> as before, and a missing <see cref="abilityHolder"/> is reported by the
    /// existing wiring rather than throwing here.
    /// </summary>
    private List<GameplayAbilitySpec> ActiveSpecsSnapshot()
    {
        if (abilityHolder == null)
        {
            // The old one-line property threw a NullReferenceException on every animation event when
            // the field was left empty. Still loud, but once per object instead of once per event.
            if (!warnedNoHolder)
            {
                warnedNoHolder = true;
                Debug.LogError($"{name} has an AbilityAnimationBridge with no AbilityHolder assigned, so every animation event on it notifies nothing: no attack collider, no combo window, no dash. Assign the holder.", this);
            }
            return NoActiveSpecs;
        }

        List<GameplayAbilitySpec> live = abilityHolder.ActiveSpecs;
        if (live == null || live.Count == 0) return NoActiveSpecs;
        return live.ToList();
    }

    /*
     *  Notifies of SwordAttack
     */
    public void ComboWindowOpen()
    {
        foreach (var spec in ActiveSpecsSnapshot())
        {
            if (spec is LightAttackSpec lightAttackSpec)
            {
                lightAttackSpec.ComboWindowOpen();
            }
        }
    }

    public void ComboWindowClose()
    {
        foreach (var spec in ActiveSpecsSnapshot())
        {
            if (spec is LightAttackSpec lightAttackSpec)
            {
                lightAttackSpec.ComboWindowClose();
            }
        }
    }

    public void EnableSwordCollider()
    {
        foreach (var spec in ActiveSpecsSnapshot())
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
            if (spec is GodSpaceAttackSpec godSpaceAttackSpec)
            {
                godSpaceAttackSpec.EnableSwordCollider();
            }
        }
    }

    public void DisableSwordCollider()
    {
        foreach (var spec in ActiveSpecsSnapshot())
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
            if (spec is GodSpaceAttackSpec godSpaceAttackSpec)
            {
                godSpaceAttackSpec.DisableSwordCollider();
            }
        }
    }
    
    public void StartAttackAssist()
    {
        foreach (var spec in ActiveSpecsSnapshot())
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
        foreach (var spec in ActiveSpecsSnapshot())
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
        foreach (var spec in ActiveSpecsSnapshot())
        {
            if (spec is LightAttackSpec lightAttackSpec)
            {
                lightAttackSpec.DisableInput();
            }
        }
    }
    public void CanHaveInput()
    {
        foreach (var spec in ActiveSpecsSnapshot())
        {
            if (spec is LightAttackSpec lightAttackSpec)
            {
                lightAttackSpec.CanHaveInput();
            }
        }
    }

    public void EndBarbarianAttack()
    {
        foreach (var spec in ActiveSpecsSnapshot())
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
        foreach (var spec in ActiveSpecsSnapshot())
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
        foreach (var spec in ActiveSpecsSnapshot())
        {
            if (spec is BowAttackSpec BowAttackSpec)
            {
                BowAttackSpec.ShootArrow();
            }
        }
    }
    public void AlreadyShoot()
    {
        foreach (var spec in ActiveSpecsSnapshot())
        {
            if (spec is BowAttackSpec BowAttackSpec)
            {
                BowAttackSpec.AlreadyShoot();
            }
        }
    }
    
    public void ShowArrowPath()
    {
        foreach (var spec in ActiveSpecsSnapshot())
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
        foreach (var spec in ActiveSpecsSnapshot())
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
        foreach (var spec in ActiveSpecsSnapshot())
        {
            if (spec is LightAuraSpec lightAuraSpec)
            {
                lightAuraSpec.ActivateAbility();
            }
        }
    }
    /*
     * Notifies of HealTarget
     *
     */
    public void HealTarget()
    {
        foreach (var spec in ActiveSpecsSnapshot())
        {
            if (spec is HealTargetSpec healTargetSpec)
            {
                healTargetSpec.HealTarget();
            }
        }
    }

    public void StopHealParticle()
    {
        foreach (var spec in ActiveSpecsSnapshot())
        {
            if (spec is HealTargetSpec healTargetSpec)
            {
                healTargetSpec.StopHealParticle();
            }
        }
    }
    /*
     * Notifies of DamageProjectile
     *
     */

    public void StartDashThrow()
    {
        foreach (var spec in ActiveSpecsSnapshot())
        {
            if (spec is DamageProjectileSpec Spec)
            {
                Spec.StartDash();
            }
        }
    }
    /*
     * Notifies of Dash
     *
     */
    public void StartMoving()
    {
        foreach (var spec in ActiveSpecsSnapshot())
        {
            if (spec is DashSpec dashSpec)
            {
                dashSpec.StartMoving();
            }
        }
    }
    
    public void EndDash()
    {
        foreach (var spec in ActiveSpecsSnapshot())
        {
            if (spec is DashSpec dashSpec)
            {
                dashSpec.EndDash();
            }
            if (spec is ArcherDashSpec archerDashSpec)
            {
                archerDashSpec.EndDash();
            }
        }
    }
    
    /*
     * Notifies of Mage Counter
     *
     */
    public void SpawnMageSpell()
    {
        foreach (var spec in ActiveSpecsSnapshot())
        {
            if (spec is MageCounterAttackSpec counterSpec)
            {
                counterSpec.SpawnCounter();
            }
        }
    }
    
    /*
     * Notifies of GodSpaceAttack
     *
     */

    public void EndGodSpaceAttack()
    {
        foreach (var spec in ActiveSpecsSnapshot())
        {
            if (spec is GodSpaceAttackSpec Spec)
            {
                Spec.EndAbility();
            }
        }
    }
    
    

}
