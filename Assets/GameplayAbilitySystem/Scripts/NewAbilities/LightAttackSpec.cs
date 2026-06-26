using StarterAssets;
using Unity.VisualScripting;
using UnityEngine;

public class LightAttackSpec : GameplayAbilitySpec
{
    private Animator animator;
    private ThirdPersonController controller;
    private CharacterController characterController;
    private MakeInvincible invincible;
    private WeaponController weaponController;
    private StarterAssetsInputs starterInputs;



    private int comboIndex;
    private bool inCombo;
    private bool canReceiveInput;
    private bool bufferedInput;
    
    private float forwardDistanceRay;
    private float lateralDistanceRay;       
    private float dashDuration;
    private float dashStopDistance; 
    private float dashDefaultDistance;
    private float rotationSpeed;
    private LayerMask enemyLayer;

    public LightAttackSpec(GameplayAbility ability, GameObject owner, float ForwardDistanceRay, float LateralDistanceRay, float DashDuration, float DashStopDistance, float DashDefaultDistance, float RotationSpeed)
        : base(ability, owner)
    {
        animator = owner.GetComponent<Animator>();
        controller = owner.GetComponent<ThirdPersonController>();
        weaponController = owner.GetComponent<WeaponController>();
        characterController = owner.GetComponent<CharacterController>();
        invincible = owner.GetComponent<MakeInvincible>();
        starterInputs = owner.GetComponent<StarterAssetsInputs>();
        enemyLayer = LayerMask.GetMask("EnemyLayer");
        
        lateralDistanceRay = LateralDistanceRay;
        forwardDistanceRay = ForwardDistanceRay;
        dashDuration = DashDuration;
        dashStopDistance = DashStopDistance;
        dashDefaultDistance = DashDefaultDistance;
        rotationSpeed = RotationSpeed;
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
            if (!animator.GetBool("Grounded") || animator.GetBool("Dash"))
            {
                return;
            }
            inCombo = true;
            comboIndex = 1;

            animator.SetBool("InCombo", true);
            animator.SetInteger("ComboIndex", comboIndex);
            animator.SetTrigger("Attack");  
            
            TryAimAssistDash();
        }
        else if (owner.CompareTag("Enemy"))
        {
            animator.SetBool("InCombo", true);
            animator.SetTrigger("Attack"); 
        }
    }
    void ContinueCombo()
    {
        bufferedInput = false;
        if (comboIndex < 3)
        {
            comboIndex++;   
        }
        else
        {
            comboIndex = 1;
        }
        TryAimAssistDash();
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
            EndCombo();
        }
        animator.SetBool("InCombo", false);
        DisableSwordCollider();
    }

    public override void EndAbility()
    {
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
    
    void TryAimAssistDash()
    {
        Transform target = FindTargetInRectangle();
        if (target != null)
        {
            controller.StartCoroutine(DashTowards(target));
        }
        else
        {
            controller.StartCoroutine(DefaultDash());
        }
    }
    
    Transform FindTargetInRectangle()
    {
        Vector3 origin = owner.transform.position;
        Vector3 forward = GetAimDirection();

        Transform best = null;
        float bestDistance = Mathf.Infinity;

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

            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = hit.transform;
            }
        }

        return best;
    }

    System.Collections.IEnumerator DashTowards(Transform target)
    {
        float elapsed = 0f;
        
        Vector3 initialToTarget = target.position - owner.transform.position;
        initialToTarget.y = 0f;
        
        Vector3 dashDestination = target.position - initialToTarget.normalized * dashStopDistance;
        dashDestination.y = owner.transform.position.y;
        
        float travelDistance = Vector3.Distance(owner.transform.position, dashDestination);
        float speed = travelDistance / dashDuration;
        
        Quaternion targetRotation = Quaternion.LookRotation(initialToTarget.normalized);


        while (elapsed < dashDuration)
        {
            Vector3 toDestination = dashDestination - owner.transform.position;
            toDestination.y = 0f;
            float remainingDistance = toDestination.magnitude;

            if (remainingDistance <= 0.05f)
                break;

            Vector3 dashDirection = toDestination.normalized;

            float step = speed * Time.deltaTime;
            step = Mathf.Min(step, remainingDistance);

            characterController.Move(dashDirection * step + Vector3.down * 0.05f);
            owner.transform.rotation = Quaternion.Slerp(owner.transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

            elapsed += Time.deltaTime;
            yield return null;
        }
    }
    
    System.Collections.IEnumerator DefaultDash()
    {
        Vector3 dashDirection = GetAimDirection();
        Quaternion targetRotation = dashDirection.sqrMagnitude > 0.001f ? Quaternion.LookRotation(dashDirection) : owner.transform.rotation;

        float elapsed = 0f;
        while (elapsed < dashDuration)
        {
            float step = (dashDefaultDistance / dashDuration) * Time.deltaTime;
            characterController.Move(dashDirection * step + Vector3.down * 0.05f);
            owner.transform.rotation = Quaternion.Slerp(owner.transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            elapsed += Time.deltaTime;
            yield return null;
        }
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
    }

    public void CanHaveInput()
    {
        if (owner.CompareTag("Player"))
        {
            controller.SetInputAviable(true);   
        }
    }

    public void EndComboAnimation() => EndCombo();
    

    public void EnableSwordCollider()
    {

        weaponController.EnableCollision(comboIndex);
    }

    public void DisableSwordCollider()
    {

        weaponController.DisableCollision();
    }

    public void MakeInvincible()
    {
        if (invincible)
        {
            //invincible.SetInvincible(true);   
        }
    }
    
    public void DisableInvincible()
    {
        if (invincible)
        {
            //invincible.SetInvincible(false);   
        }
    }
}
