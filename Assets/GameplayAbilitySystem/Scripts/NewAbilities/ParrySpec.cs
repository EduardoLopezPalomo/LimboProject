using UnityEngine;
using StarterAssets;
using System.Collections;

public class ParrySpec : GameplayAbilitySpec
{
    private Animator animator;
    private AbilityHolder abilityHolder;
    private DefenseState defenseState;
    private ThirdPersonController personController;
    private LockOn lockOn;
    private PlayerWeaponController weaponController;
    private Transform ownerTransform;

    private float parryWindowDuration;

    private bool holdingButton;

    public ParrySpec(GameplayAbility ability, GameObject owner, float parryWindowDurationIn) : base(ability, owner)
    {
        animator = owner.GetComponent<Animator>();
        abilityHolder = owner.GetComponent<AbilityHolder>();
        defenseState = owner.GetComponent<DefenseState>();
        personController = owner.GetComponent<ThirdPersonController>();
        weaponController = owner.GetComponent<PlayerWeaponController>();
        lockOn = owner.GetComponent<LockOn>();
        ownerTransform = owner.transform;

        parryWindowDuration = parryWindowDurationIn;
    }

    public override bool CanActivateAbility()
    {
        if (bOnCooldown || abilityHolder.HasAnyActiveAbility(this))
        {
            return false;
        }
        return true;
    }

    public override void ActivateAbility()
    {
        base.ActivateAbility();

        holdingButton = true;
        personController.SetInputAviable(false);
        animator.SetBool("Blocking", true);
        weaponController.EquipWeapon();
        weaponController.WeaponDamage.parentConstraint.enabled = true;
        abilityHolder.StartCoroutine(GuardRoutine());
    }

    private IEnumerator GuardRoutine()
    {
        defenseState.SetState(GuardState.Parry, this);

        float t = 0f;
        while (t < parryWindowDuration)
        {
            if (defenseState.State != GuardState.Parry)
                yield break;

            RotateTowardsLockOnTarget();

            t += Time.deltaTime;
            yield return null;
        }
        
        if (!holdingButton)
        {
            EndGuard();
            yield break;
        }

        defenseState.SetState(GuardState.Block, this);
        
        while (holdingButton && defenseState.State == GuardState.Block)
        {
            RotateTowardsLockOnTarget();
            yield return null;
        }

        if (defenseState.State == GuardState.Block)
            EndGuard();
    }
    
    private void RotateTowardsLockOnTarget()
    {
        if (lockOn == null || !lockOn.isLocked || lockOn.currentTarget == null)
            return;

        Vector3 lookDir = lockOn.currentTarget.position - ownerTransform.position;
        lookDir.y = 0f;

        if (lookDir.sqrMagnitude > 0.001f)
        {
            Quaternion strafeRotation = Quaternion.LookRotation(lookDir);
            ownerTransform.rotation = Quaternion.Slerp(ownerTransform.rotation, strafeRotation, Time.deltaTime * 10f);
        }
    }

    public override void CancelAbility()
    {
        base.CancelAbility();
        holdingButton = false;
        animator.SetBool("Blocking", false);
        personController.SetInputAviable(true);
        defenseState.SetState(GuardState.Idle, this);
        weaponController.WeaponDamage.parentConstraint.enabled = false;
        if (!lockOn.isLocked)
        {
            weaponController.UnequipWeapon();   
        }
    }
    
    public void NotifyPerfectParry()
    {
        animator.SetTrigger("ParrySuccess");
        EndGuard();
    }
    
    public void NotifyGuardBreak()
    {
        animator.SetBool("Blocking", false);
        animator.SetTrigger("GuardBreak");
        defenseState.SetState(GuardState.Recovery, this);
        weaponController.WeaponDamage.parentConstraint.enabled = false;
        EndAbility();
    }
    
    private void EndGuard()
    {
        animator.SetBool("Blocking", false);
        defenseState.SetState(GuardState.Idle, this);
        personController.SetInputAviable(true);
        weaponController.WeaponDamage.parentConstraint.enabled = false;
        EndAbility();
    }
}
