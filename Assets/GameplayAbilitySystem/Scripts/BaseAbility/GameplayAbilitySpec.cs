using UnityEngine;

public class GameplayAbilitySpec 
{
    protected GameplayAbility ability;
    protected GameObject owner;

    public GameplayAbilitySpec(GameplayAbility ability , GameObject owner)
    {
        this.ability = ability;
        this.owner = owner;
    }

    public virtual void ActivateAbility()
    {
        ability.ActivateAbilityEffects(owner);
    }

    public virtual void EndAbility()
    {

    }
}
