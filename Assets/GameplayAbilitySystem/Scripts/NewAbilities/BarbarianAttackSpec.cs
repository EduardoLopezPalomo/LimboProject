using System.Collections;
using Unity.Behavior;
using UnityEngine;

public class BarbarianAttackSpec : GameplayAbilitySpec
{
    private Animator animator;
    private WeaponController weaponController;
    private BehaviorGraphAgent behaviorGraphAgent;
    private float timeStuned;
    private int comboIndex;

    private Coroutine StunnedCoroutine;
   

    public BarbarianAttackSpec(GameplayAbility ability, GameObject owner, float TimeStuned) : base(ability, owner)
    {
        animator = owner.GetComponent<Animator>();
        weaponController = owner.GetComponent<WeaponController>();
        behaviorGraphAgent = owner.GetComponent<BehaviorGraphAgent>();
        timeStuned = TimeStuned;
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
        weaponController.weaponDamage.SetHasHitPlayer(false);
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
        base.EndAbility();
        EndCombo();
    }

    IEnumerator StayStunned()
    {
        animator.speed = 0f;
        yield return new WaitForSeconds(timeStuned);
        
        animator.speed = 1f;
    }

    public void StartStunned()
    {
        if (weaponController.weaponDamage.GetHasHitPlayer())
        {
            return;
        }
           
        if (StunnedCoroutine != null)
        {
            weaponController.StopCoroutine(StunnedCoroutine);
        }
        behaviorGraphAgent.BlackboardReference.SetVariableValue("AttackingState", AttackingState.Vulnerable);
        StunnedCoroutine = weaponController.StartCoroutine(StayStunned());
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
