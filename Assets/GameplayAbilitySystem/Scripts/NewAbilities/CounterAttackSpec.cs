using System;
using System.Collections;
using Unity.Behavior;
using UnityEngine;

public class CounterAttackSpec : GameplayAbilitySpec
{
    private Animator animator;
    private EnemyWeaponController weaponController;
    private CharacterController characterController;
    private AttributeSystem attributeSystem;
    private CharacterSFX characterSFX;

    private Coroutine StunnedCoroutine;

    private float forwardDistanceRay;
    private float lateralDistanceRay;
    private float attackAssistSpeed;
    private float dashStopDistance;
    private float dashDefaultDistance;
    private float rotationSpeed;
    private LayerMask playerlayer;

    private Vector3 attackDirection;
    private Transform attackTarget;
    private bool attackAssistActive;
    private Coroutine attackAssistCoroutine;
    private Vector3 attackDestination;


    public CounterAttackSpec(GameplayAbility ability, GameObject owner, float ForwardDistanceRay, float LateralDistanceRay, float AttackAssistSpeed, float DashStopDistance, float DashDefaultDistance, float RotationSpeed) : base(ability, owner)
    {
        animator = owner.GetComponent<Animator>();
        characterController = owner.GetComponent<CharacterController>();
        weaponController = owner.GetComponent<EnemyWeaponController>();
        attributeSystem = owner.GetComponent<AttributeSystem>();
        characterSFX = owner.GetComponent<CharacterSFX>();
        playerlayer = LayerMask.GetMask("Player");

        lateralDistanceRay = LateralDistanceRay;
        forwardDistanceRay = ForwardDistanceRay;
        attackAssistSpeed = AttackAssistSpeed;
        dashStopDistance = DashStopDistance;
        dashDefaultDistance = DashDefaultDistance;
        rotationSpeed = RotationSpeed;
    }

    public override void ActivateAbility()
    {
        base.ActivateAbility();
        if (attributeSystem.GetAttributeValue("BlockResistance") > 0f)
        {
            StartCounter();
        }
        else
        {
            EndAbility();
        }
    }

    void StartCounter()
    {

        if (!animator.GetBool("Grounded"))
        {
            return;
        }
        animator.SetTrigger("CounterAttack");
        weaponController.CounterAttack(true);
        weaponController.EquipWeapon();
    }

    public override void CancelAbility()
    {
        base.CancelAbility();
        weaponController.CounterAttack(false);
        DisableSwordCollider();
    }

    public override void EndAbility()
    {
        base.EndAbility();
        if (!animator.GetBool("IsStrafing"))
        {
            weaponController.UnequipWeapon();
        }
        weaponController.CounterAttack(false);
    }

    void CaptureAttackIntent()
    {
        attackDirection = owner.transform.forward;
        attackTarget = FindTargetInRectangle(attackDirection);

        if (attackTarget != null)
        {
            attackDestination = CalculateAttackPoint(attackTarget);
        }
        else
        {
            attackDestination = owner.transform.position + attackDirection * dashDefaultDistance;
        }
    }

    Vector3 CalculateAttackPoint(Transform target)
    {
        Vector3 direction = (target.position - owner.transform.position).normalized;

        Vector3 point = target.position - direction * dashStopDistance;

        point.y = owner.transform.position.y;

        return point;
    }

    Transform FindTargetInRectangle(Vector3 forward)
    {
        Vector3 origin = owner.transform.position;

        Transform best = null;
        float bestScore = Mathf.Infinity;

        Collider[] hits = Physics.OverlapBox(
            origin + forward * (forwardDistanceRay / 2f),
            new Vector3(lateralDistanceRay, 1f, forwardDistanceRay / 2f),
            Quaternion.LookRotation(forward),
            playerlayer
        );

        foreach (var hit in hits)
        {
            Vector3 toTarget = hit.transform.position - origin;
            toTarget.y = 0f;
            float distance = toTarget.magnitude;
            float angle = Vector3.Angle(forward, toTarget.normalized);

            float score = distance + angle * 0.1f;

            if (score < bestScore)
            {
                bestScore = score;
                best = hit.transform;
            }
        }

        return best;
    }

    IEnumerator AttackAssist()
    {
        while (attackAssistActive)
        {
            Vector3 destination;
            Vector3 lookDirection;

            if (attackTarget != null)
            {
                destination = CalculateAttackPoint(attackTarget);
                lookDirection = attackTarget.position - owner.transform.position;
            }
            else
            {
                destination = attackDestination;
                lookDirection = attackDirection;
            }

            Vector3 direction = destination - owner.transform.position;
            direction.y = 0;
            lookDirection.y = 0f;

            if (direction.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(lookDirection);

                owner.transform.rotation = Quaternion.Slerp(owner.transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }

            float step = attackAssistSpeed * Time.deltaTime;
            float distance = direction.magnitude;

            if (distance < 0.05f)
            {
                yield return null;
                continue;
            }

            step = Mathf.Min(step, distance);
            characterController.Move(direction.normalized * step + Vector3.down * 0.05f);
            yield return null;
        }

        attackAssistCoroutine = null;
    }


    public void EnableSwordCollider()
    {
        weaponController.EnableCollision();
        characterSFX.SpawnRandomSFX(SFXType.Attack);
    }

    public void DisableSwordCollider()
    {

        weaponController.DisableCollision();
    }

    public void StartAttackAssist()
    {

        attackAssistActive = true;
        if (attackAssistCoroutine != null)
            weaponController.StopCoroutine(attackAssistCoroutine);

        CaptureAttackIntent();
        attackAssistCoroutine = weaponController.StartCoroutine(AttackAssist());
    }

    public void EndAttackAssist()
    {
        attackAssistActive = false;
        if (attackAssistCoroutine != null)
        {
            weaponController.StopCoroutine(attackAssistCoroutine);
            attackAssistCoroutine = null;
        }
    }
}
