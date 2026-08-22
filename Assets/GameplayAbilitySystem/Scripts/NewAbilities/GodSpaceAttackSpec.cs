using System.Collections;
using Unity.Behavior;
using UnityEngine;

public class GodSpaceAttackSpec : GameplayAbilitySpec
{
    private Animator animator;
    private EnemyWeaponController weaponController;
    private BehaviorGraphAgent behaviorGraphAgent;
    private CharacterSFX characterSFX;

    private int comboIndex;
    private string animatorTrigger;
    public GodSpaceAttackSpec(GameplayAbility ability, GameObject owner, string trigger) : base(ability, owner)
    {
        animator = owner.GetComponent<Animator>();
        weaponController = owner.GetComponent<EnemyWeaponController>();
        behaviorGraphAgent = owner.GetComponent<BehaviorGraphAgent>();
        characterSFX = owner.GetComponent<CharacterSFX>();
        
        animatorTrigger = trigger;
    }

    public override void ActivateAbility()
    {
        base.ActivateAbility();
            
        StartCombo();
    }

    void StartCombo()
    {
        comboIndex = 0;
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
