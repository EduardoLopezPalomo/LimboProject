using UnityEngine;

public class BarbarianAttackSpec : GameplayAbilitySpec
{
    private Animator animator;
    private WeaponController weaponController;
    private int comboIndex;
   

    public BarbarianAttackSpec(GameplayAbility ability, GameObject owner)
        : base(ability, owner)
    {
        animator = owner.GetComponent<Animator>();
        weaponController = owner.GetComponent<WeaponController>();
    }

    public override void ActivateAbility()
    {
        base.ActivateAbility();
        
        StartCombo();    
    }

    void StartCombo()
    {
        
        if (!animator.GetBool("Grounded"))
        {
            return;
        }

        comboIndex = 0;
        animator.SetBool("InCombo", true);
        animator.SetTrigger("Attack");
        weaponController.EquipWeapon();
    }
    
    void EndCombo()
    {
        if (!animator.GetBool("IsStrafing"))
        {
            weaponController.UnequipWeapon();    
        }
        animator.SetBool("InCombo", false);
    }
    
    public override void CancelAbility()
    {
        base.CancelAbility();
        
        animator.SetBool("InCombo", false);
        DisableSwordCollider();
    }

    public override void EndAbility()
    {
        EndCombo();
    }
    

    public void EnableSwordCollider()
    {
        comboIndex++;
        weaponController.EnableCollision(comboIndex);
    }

    public void DisableSwordCollider()
    {

        weaponController.DisableCollision();
    }
}
