using UnityEngine;
using StarterAssets;
using System.Collections;

public class DashSpec : GameplayAbilitySpec
{
    private int dashDistance;
    private float dashTime;
    private Animator animator;
    private RootMotionController rootMotion;
    private CharacterController charactercontroller;
    private ThirdPersonController personcontroller;
    private AbilityHolder abilityHolder;
    private MakeInvincible makeInvincible;


    public DashSpec(GameplayAbility ability, GameObject owner, int dashForce, float dashTimeIn) : base(ability, owner)
    {
        animator = owner.GetComponent<Animator>();
        charactercontroller = owner.GetComponent<CharacterController>();
        personcontroller = owner.GetComponent<ThirdPersonController>();
        abilityHolder = owner.GetComponent<AbilityHolder>();
        makeInvincible = owner.GetComponent<MakeInvincible>();
        dashTime = dashTimeIn;
        dashDistance = dashForce;

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

        abilityHolder.StartCoroutine(waitTillEndFrame());
    }

    private IEnumerator waitTillEndFrame()
    {
        makeInvincible.SetInvincible(true);
        personcontroller.SetInputAviable(false);

        float elapsed = 0f;

        Vector3 dashDirection = owner.transform.forward;

        while (elapsed < dashTime)
        {
            charactercontroller.Move(dashDirection * dashDistance * Time.deltaTime);
            elapsed += Time.deltaTime;

            yield return null;
        }

        makeInvincible.SetInvincible(false);
        personcontroller.SetInputAviable(true);
    }

    public void DisableRootMotion() => rootMotion.DisableAnimRootMotion();

    public override void EndAbility()
    {
        base.EndAbility();
    }
}
