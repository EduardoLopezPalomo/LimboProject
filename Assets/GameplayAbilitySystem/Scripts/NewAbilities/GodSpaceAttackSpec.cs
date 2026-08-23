using System.Collections;
using Unity.Behavior;
using UnityEngine;

public class GodSpaceAttackSpec : GameplayAbilitySpec
{
    private Animator animator;
    private EnemyWeaponController weaponController;
    private CharacterSFX characterSFX;

    private int comboIndex;
    private string animatorTrigger;
    private bool isCombo;
    public GodSpaceAttackSpec(GameplayAbility ability, GameObject owner, string trigger, bool combo) : base(ability, owner)
    {
        animator = owner.GetComponent<Animator>();
        weaponController = owner.GetComponent<EnemyWeaponController>();
        characterSFX = owner.GetComponent<CharacterSFX>();
        
        animatorTrigger = trigger;
        isCombo = combo;
    }

    public override void ActivateAbility()
    {
        base.ActivateAbility();
            
        StartCombo();
    }

    void StartCombo()
    {
        if (isCombo)
        {
            comboIndex = 0;    
        }
        else
        {
            comboIndex = 2;
        }
        
        if (!animator.GetBool("Grounded"))
        {
            return;
        }

        animator.SetTrigger(animatorTrigger);
    }

    public override void CancelAbility()
    {
        base.CancelAbility();
        DisableSwordCollider();
    }

    public override void EndAbility()
    {
        base.EndAbility();
        DisableSwordCollider();
    }


    public void EnableSwordCollider()
    {
        weaponController.SetComboNumber(comboIndex);
        comboIndex++;
        
        weaponController.EnableCollision();
        characterSFX.SpawnRandomSFX(SFXType.Attack);
    }

    public void DisableSwordCollider()
    {

        weaponController.DisableCollision();
    }
}
