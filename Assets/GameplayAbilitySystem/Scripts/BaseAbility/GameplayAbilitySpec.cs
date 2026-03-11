using System;
using UnityEngine;

public class GameplayAbilitySpec
{
    public GameplayAbility ability;
    protected GameObject owner;

    protected bool bOnCooldown;
    private bool bHasCooldown;
    private float cooldownTimer;

    public GameplayAbilitySpec(GameplayAbility ability, GameObject owner)
    {
        this.ability = ability;
        this.owner = owner;
        if (ability.Cooldown > 0)
        {
            bHasCooldown = true;
        }
        else
        {
            bHasCooldown = false;
        }
    }

    public virtual void ActivateAbility()
    {
        if (bOnCooldown)
        {
            return;
        }
        ability.ActivateAbilityEffects(owner);
        if (bHasCooldown)
        {
            bOnCooldown = true;
            cooldownTimer = ability.Cooldown;
        }

    }

    public virtual void EndAbility()
    {

    }
    public virtual void CancelAbility()
    {
        if (bOnCooldown)
        {
            bOnCooldown = false;
            cooldownTimer = 0;
        }
    }

    public virtual void Tick(float deltaTime)
    {
        if (!bOnCooldown) return;
        cooldownTimer -= deltaTime;
        if (cooldownTimer <= 0)
        {
            bOnCooldown = false;
            cooldownTimer = 0;
        }
    }

}
