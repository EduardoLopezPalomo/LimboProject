using System;
using UnityEngine;

public class GameplayAbilitySpec
{
    public GameplayAbility ability;
    public event Action<GameplayAbilitySpec> OnAbilityEnded;
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
    public virtual bool CanActivateAbility()
    {
        return true;
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
        OnAbilityEnded?.Invoke(this);
    }
    public virtual void CancelAbility()
    {
        if (bOnCooldown)
        {
            bOnCooldown = false;
            cooldownTimer = 0;
        }
    }

    public float GetCooldown()
    {
        if (!bHasCooldown) return 0f;
        
        return cooldownTimer;
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
