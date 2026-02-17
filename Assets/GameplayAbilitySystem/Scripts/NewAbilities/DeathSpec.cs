using StarterAssets;
using UnityEngine;

public class DeathSpec : GameplayAbilitySpec
{
    Animator animator;


    public DeathSpec(GameplayAbility ability, GameObject owner)
        : base(ability, owner)
    {
        animator = owner.GetComponent<Animator>();
    }

    public override void ActivateAbility()
    {
        base.ActivateAbility();

        animator.SetBool("IsDeath", true);

    }
}
