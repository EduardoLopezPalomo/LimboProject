using StarterAssets;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class LightAttackSpec : GameplayAbilitySpec
{
    private Animator animator;
    private ThirdPersonController controller;
    private CharacterController characterController;
    private WeaponController weaponController;
    private StarterAssetsInputs starterInputs;
    
    private int comboIndex;
    private bool inCombo;
    private bool canReceiveInput;
    private bool bufferedInput;
    private Vector3 attackDirection;
    private Transform attackTarget;
    private bool attackAssistActive;
    private Coroutine attackAssistCoroutine;
    private Vector3 attackDestination;
    private float comboEndTime;
    private float comboCooldown = 0.3f;
    
    private float forwardDistanceRay;
    private float lateralDistanceRay;       
    private float attackAssistSpeed;
    private float dashStopDistance; 
    private float dashDefaultDistance;
    private float rotationSpeed;
    private LayerMask enemyLayer;

    public LightAttackSpec(GameplayAbility ability, GameObject owner, float ForwardDistanceRay, float LateralDistanceRay, float AttackAssistSpeed, float DashStopDistance, float DashDefaultDistance, float RotationSpeed)
        : base(ability, owner)
    {
        animator = owner.GetComponent<Animator>();
        controller = owner.GetComponent<ThirdPersonController>();
        weaponController = owner.GetComponent<WeaponController>();
        characterController = owner.GetComponent<CharacterController>();
        starterInputs = owner.GetComponent<StarterAssetsInputs>();
        enemyLayer = LayerMask.GetMask("EnemyLayer");
        
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
        if (!inCombo)
        {
            if (Time.time < comboEndTime + comboCooldown)
            {
                return;
            }
            StartCombo();    
        }
        else
        {
            if (canReceiveInput)
            {
                bufferedInput = true;
            }
        }
        
    }

    void StartCombo()
    {
        
        if (!animator.GetBool("Grounded") || animator.GetBool("Dash"))
        {
            return;
        }
        inCombo = true;
        comboIndex = 1;

        animator.SetBool("InCombo", true);
        animator.SetTrigger("Attack");  
        weaponController.EquipWeapon();
    }

    void EndCombo()
    {
        inCombo = false;
        comboIndex = 0;
        bufferedInput = false;
        comboEndTime = Time.time;
        if (!animator.GetBool("IsStrafing"))
        {
            weaponController.UnequipWeapon();    
        }
        controller.SetInputAviable(true); 
        characterController.Move(Vector3.zero);
        animator.SetBool("InCombo", false);
    }
    public override void CancelAbility()
    {
        base.CancelAbility();
        if (inCombo)
        {
            canReceiveInput = false;   
            inCombo = false;
            EndCombo();
        }
        animator.SetBool("InCombo", false);
        attackAssistActive = false;
        if (attackAssistCoroutine != null)
        {
            controller.StopCoroutine(attackAssistCoroutine);   
        }
        DisableSwordCollider();
    }

    public override void EndAbility()
    {
        base.EndAbility();
        EndCombo();
    }
    
    private Vector3 GetAimDirection()
    {
        Vector2 move = starterInputs != null ? starterInputs.move : Vector2.zero;
        
        if (move.sqrMagnitude < 0.0001f)
        {
            return owner.transform.forward;
        }

        Camera cam = Camera.main;
        Vector3 camForward = cam.transform.forward;
        Vector3 camRight = cam.transform.right;
        camForward.y = 0f;
        camRight.y = 0f;
        camForward.Normalize();
        camRight.Normalize();

        return (camForward * move.y + camRight * move.x).normalized;
    }

    void CaptureAttackIntent()
    {
        attackDirection = GetAimDirection();
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
            enemyLayer
        );

        DrawDebugRectangle(origin, forward);

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

            if(direction.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation =  Quaternion.LookRotation(lookDirection);

                owner.transform.rotation = Quaternion.Slerp( owner.transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
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
    
    void DrawDebugRectangle(Vector3 origin, Vector3 forward)
    {
        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
        Vector3 originRaised = origin + Vector3.up * 0.1f;

        // Las 4 esquinas del rectangulo
        Vector3 frontLeft  = originRaised + forward * forwardDistanceRay - right * lateralDistanceRay;
        Vector3 frontRight = originRaised + forward * forwardDistanceRay + right * lateralDistanceRay;
        Vector3 backLeft   = originRaised - right * lateralDistanceRay;
        Vector3 backRight  = originRaised + right * lateralDistanceRay;

        // Lados del rectangulo
        Debug.DrawLine(backLeft,   frontLeft,  Color.cyan, 2f);
        Debug.DrawLine(backRight,  frontRight, Color.cyan, 2f);
        Debug.DrawLine(backLeft,   backRight,  Color.cyan, 2f);
        Debug.DrawLine(frontLeft,  frontRight, Color.cyan, 2f);

        // Linea central hacia delante
        Debug.DrawRay(originRaised, forward * forwardDistanceRay, Color.yellow, 2f);
    }

    // Animation Events
    public void ComboWindowOpen()
    {
        canReceiveInput = true;
    }

    public void ComboWindowClose()
    {
        canReceiveInput = false;
    }

    public void CanHaveInput()
    {
        if (bufferedInput)
        {
            StartCombo();
            bufferedInput = false;
            return;
        }
        EndAbility();
        controller.SetInputAviable(true); 
        characterController.Move(Vector3.zero);
    }

    public void DisableInput()
    {
        controller.SetInputAviable(false);
    }
    

    public void EnableSwordCollider()
    {

        weaponController.EnableCollision(comboIndex);
    }

    public void DisableSwordCollider()
    {

        weaponController.DisableCollision();
    }

    public void StartAttackAssist()
    {
        if (!bufferedInput && comboIndex != 1)
        {
            EndAbility();
            return;
        }
        comboIndex ++;
        attackAssistActive = true;
        bufferedInput = false;
        if (attackAssistCoroutine != null)
            controller.StopCoroutine(attackAssistCoroutine);

        CaptureAttackIntent();
        attackAssistCoroutine = controller.StartCoroutine(AttackAssist());
    }
    
    public void EndAttackAssist()
    {
        attackAssistActive = false;
        if (attackAssistCoroutine != null)
        {
            controller.StopCoroutine(attackAssistCoroutine);
            attackAssistCoroutine = null;
        }
    }
}
