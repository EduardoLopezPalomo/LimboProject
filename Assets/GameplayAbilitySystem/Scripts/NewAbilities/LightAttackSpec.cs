using StarterAssets;
using Unity.VisualScripting;
using UnityEngine;

public class LightAttackSpec : GameplayAbilitySpec
{
    private Animator animator;
    private RootMotionController rootMotion;
    private ThirdPersonController controller;

    private WeaponController weaponController;



    private int comboIndex;
    private bool inCombo;
    private bool canReceiveInput;
    private bool bufferedInput;

    public LightAttackSpec(GameplayAbility ability, GameObject owner)
        : base(ability, owner)
    {
        animator = owner.GetComponent<Animator>();
        rootMotion = owner.GetComponent<RootMotionController>();
        controller = owner.GetComponent<ThirdPersonController>();
        weaponController = owner.GetComponent<WeaponController>();
    }

    public override void ActivateAbility()
    {
        base.ActivateAbility();

        if (canReceiveInput)
            bufferedInput = true;
        else if (!inCombo)
            StartCombo();
    }

    void StartCombo()
    {
        if (!animator.GetBool("Grounded"))
        {
            return;
        }
        inCombo = true;
        comboIndex = 1;
        if (controller != null)
        {
            controller.SetInputAviable(false);
        }


        animator.SetBool("InCombo", true);
        animator.SetInteger("ComboIndex", comboIndex);
        animator.SetTrigger("Attack");
    }

    void ContinueCombo()
    {
        bufferedInput = false;
        comboIndex++;
        animator.SetInteger("ComboIndex", comboIndex);
        animator.SetTrigger("Attack");
    }

    void EndCombo()
    {
        inCombo = false;
        comboIndex = 0;

        if (controller != null)
        {
            controller.SetInputAviable(true);
        }
        animator.SetBool("InCombo", false);
    }

    public override void EndAbility()
    {
        EndCombo();
    }

    // Animation Events
    public void ComboWindowOpen() => canReceiveInput = true;

    public void ComboWindowClose()
    {
        canReceiveInput = false;

        if (bufferedInput && comboIndex < 3)
            ContinueCombo();
        else
            EndCombo();
    }

    public void EnableRootMotion() => rootMotion.EnableAnimRootMotion();
    public void DisableRootMotion() => rootMotion.DisableAnimRootMotion();

    public void EnableSwordCollider()
    {

        weaponController.EnableCollision();
    }

    public void DisableSwordCollider()
    {

        weaponController.DisableCollision();
    }
}
