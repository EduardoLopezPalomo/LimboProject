using UnityEngine;

public class DashSpec : GameplayAbilitySpec
{
    public int dashDistance = 50;
    private Animator animator;
    private CharacterController controller;

    public DashSpec(GameplayAbility ability, GameObject owner) : base(ability, owner)
    {
        animator = owner.GetComponent<Animator>();
        controller = owner.GetComponent<CharacterController>();
    }

    public override void ActivateAbility()
    {
        if (bOnCooldown)
        {
            return;
        }
        base.ActivateAbility();
        Vector3 dashDirection = owner.transform.forward;
        controller.Move(dashDirection * dashDistance * Time.deltaTime);

    }

    public override void EndAbility()
    {
        base.EndAbility();
    }
}
