using UnityEngine;
using StarterAssets;

public class DashSpec : GameplayAbilitySpec
{
    public int dashDistance = 50;
    private Animator animator;
    private RootMotionController rootMotion;
    private CharacterController controller;
    private AbilityHolder abilityHolder;


    public DashSpec(GameplayAbility ability, GameObject owner) : base(ability, owner)
    {
        animator = owner.GetComponent<Animator>();
        controller = owner.GetComponent<CharacterController>();
        abilityHolder = owner.GetComponent<AbilityHolder>();

    }

    public override void ActivateAbility()
    {
        if (bOnCooldown)
        {
            return;
        }
        base.ActivateAbility();
        if (animator.GetBool("InCombo"))
        {
            abilityHolder.CancelAbility("LightAttack");
        }
        Vector3 dashDirection = owner.transform.forward;
        controller.Move(dashDirection * dashDistance * Time.deltaTime);

    }

    public void DisableRootMotion() => rootMotion.DisableAnimRootMotion();

    public override void EndAbility()
    {
        base.EndAbility();
    }
}
