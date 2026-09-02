using UnityEngine;


public class HealPlayerSpec : GameplayAbilitySpec
{
    private Animator animator;
    private AbilityHolder abilityHolder;
    private AttributeSystem attributeSystem;
    private VFXDefinition healEffectVFX;
    private float resistanceNeeded;
    private CharacterSFX characterSFX;

    public HealPlayerSpec(GameplayAbility ability, GameObject owner, VFXDefinition vfx, float resistance) : base(ability, owner)
    {
        animator = owner.GetComponent<Animator>();
        abilityHolder = owner.GetComponent<AbilityHolder>();
        attributeSystem = owner.GetComponent<AttributeSystem>();
        healEffectVFX = vfx;
        resistanceNeeded = resistance;
        characterSFX = owner.GetComponent<CharacterSFX>();
    }
    
    public override bool CanActivateAbility()
    {
        if (bOnCooldown || attributeSystem.GetAttributeValue("Resistance") < resistanceNeeded)
        {
            characterSFX.SpawnSFX(SFXType.Resistance);
            return false;

        }
        if (abilityHolder.HasAnyActiveAbility(this))
        {
            return false;
        }
        return true;
    }

    public override void ActivateAbility()
    {
        base.ActivateAbility();
        animator.SetTrigger("Magic");
        VFXManager.Instance.Play(healEffectVFX, owner.transform, owner);
        characterSFX.SpawnSFX(SFXType.Healing);
        EndAbility();
    }
    
}
