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
        if (owner.CompareTag("Player"))
        {
            if (!animator.GetBool("Grounded"))
            {
                return;
            }
            inCombo = true;
            comboIndex = 1;

            animator.SetBool("InCombo", true);
            animator.SetInteger("ComboIndex", comboIndex);
            animator.SetTrigger("Attack");   
        }
        else if (owner.CompareTag("Enemy"))
        {
            animator.SetBool("InCombo", true);
            animator.SetTrigger("Attack"); 
        }
    }

    void ContinueCombo()
    {
        Debug.Log(comboIndex);
        bufferedInput = false;
        if (comboIndex < 3)
        {
            comboIndex++;   
        }
        else
        {
            comboIndex = 1;
        }
        animator.SetInteger("ComboIndex", comboIndex);
        animator.SetTrigger("Attack");
    }

    void EndCombo()
    {
        inCombo = false;
        comboIndex = 0;
        
        animator.SetBool("InCombo", false);
    }
    public override void CancelAbility()
    {
        base.CancelAbility();
        if (inCombo)
        {
            canReceiveInput = false;
            inCombo = false;
            animator.SetBool("InCombo", false);
            DisableRootMotion();
            DisableSwordCollider();
            EndCombo();
        }

    }

    public override void EndAbility()
    {
        EndCombo();
    }

    // Animation Events
    public void ComboWindowOpen()
    {
        canReceiveInput = true;
        if (owner.CompareTag("Player"))
        {
            controller.SetInputAviable(false);   
        }
    }

    public void ComboWindowClose()
    {
        canReceiveInput = false;
    }

    public void ComboTransitionReady()
    {
        if (bufferedInput)
        {
            ContinueCombo();
        }

        if (owner.CompareTag("Player"))
        {
            controller.SetInputAviable(true);   
        }
    }

    public void EndComboAnimation() => EndCombo();

    public void EnableRootMotion() => rootMotion.EnableAnimRootMotion();
    public void DisableRootMotion() => rootMotion.DisableAnimRootMotion();

    public void EnableSwordCollider()
    {

        weaponController.EnableCollision(comboIndex);
    }

    public void DisableSwordCollider()
    {

        weaponController.DisableCollision();
    }
}
