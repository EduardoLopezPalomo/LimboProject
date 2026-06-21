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
    
    private float coneRange;
    private float coneAngle;       
    private float dashDuration;
    private float dashStopDistance; 
    private float dashDefaultDistance;
    private float rotationSpeed;
    private LayerMask enemyLayer;

    public LightAttackSpec(GameplayAbility ability, GameObject owner, float ConeRange, float ConeAngle, float DashDuration, float DashStopDistance, float DashDefaultDistance, float RotationSpeed)
        : base(ability, owner)
    {
        animator = owner.GetComponent<Animator>();
        controller = owner.GetComponent<ThirdPersonController>();
        weaponController = owner.GetComponent<WeaponController>();
        characterController = owner.GetComponent<CharacterController>();
        invincible = owner.GetComponent<MakeInvincible>();
        starterInputs = owner.GetComponent<StarterAssetsInputs>();
        enemyLayer = LayerMask.GetMask("EnemyLayer");
        
        coneRange = ConeRange;
        coneAngle = ConeAngle;
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
            if (!animator.GetBool("Grounded"))
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
            animator.SetBool("InCombo", false);
            DisableSwordCollider();
            EndCombo();
        }

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
        Transform target = FindTargetInCone();
        if (target != null)
        {
            controller.StartCoroutine(DashTowards(target));
        }
        else
        {
            controller.StartCoroutine(DefaultDash());
        }
    }
    
    Transform FindTargetInCone()
    {
        Vector3 origin = owner.transform.position;
        Vector3 direction = GetAimDirection();

        Collider[] hits = Physics.OverlapSphere(origin, coneRange, enemyLayer);
        Transform best = null;
        float bestDot = Mathf.Cos(coneAngle * Mathf.Deg2Rad);
        float bestDistance = Mathf.Infinity;
        
        DrawDebugCone(origin, direction);
        
        foreach (var hit in hits)
        {
            Vector3 toTarget = hit.transform.position - origin;
            toTarget.y = 0f;
            float distance = toTarget.magnitude;
            if (distance < 0.01f) continue;

            Vector3 dirToTarget = toTarget.normalized;
            float dot = Vector3.Dot(direction, dirToTarget);

            if (dot >= bestDot)
            {
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = hit.transform;
                }
            }
        }

        return best;
    }

    System.Collections.IEnumerator DashTowards(Transform target)
    {
        Vector3 startPos = owner.transform.position;
        Vector3 toTarget = target.position - startPos;
        toTarget.y = 0f;

        float fullDistance = toTarget.magnitude;
        float travelDistance = Mathf.Max(0f, fullDistance - dashStopDistance);
        Vector3 dashDirection = toTarget.normalized;

        Quaternion targetRotation = dashDirection.sqrMagnitude > 0.001f ? Quaternion.LookRotation(dashDirection) : owner.transform.rotation;

        float traveled = 0f;
        float speed = travelDistance / dashDuration;

        float elapsed = 0f;
        while (elapsed < dashDuration && traveled < travelDistance)
        {
            float step = speed * Time.deltaTime;
            step = Mathf.Min(step, travelDistance - traveled);

            characterController.Move(dashDirection * step + Vector3.down * 0.05f);
            owner.transform.rotation = Quaternion.Slerp(owner.transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

            traveled += step;
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
    
    void DrawDebugCone(Vector3 origin, Vector3 aimDirection)
    {
        Vector3 originRaised = origin + Vector3.up * 0.1f;

        // Linea central: direccion de aim
        Debug.DrawRay(originRaised, aimDirection * coneRange, Color.yellow, 2f, false);

        // Bordes del cono
        Quaternion leftRotation = Quaternion.Euler(0, -coneAngle, 0);
        Quaternion rightRotation = Quaternion.Euler(0, coneAngle, 0);

        Vector3 leftEdge = leftRotation * aimDirection;
        Vector3 rightEdge = rightRotation * aimDirection;

        Debug.DrawRay(originRaised, leftEdge * coneRange, Color.cyan, 2f, false);
        Debug.DrawRay(originRaised, rightEdge * coneRange, Color.cyan, 2f, false);

        // Circulo aproximado del rango (opcional, para ver coneRange completo)
        int segments = 20;
        Vector3 prevPoint = originRaised + new Vector3(Mathf.Sin(0), 0, Mathf.Cos(0)) * coneRange;
        for (int i = 1; i <= segments; i++)
        {
            float angle = (i / (float)segments) * 360f * Mathf.Deg2Rad;
            Vector3 nextPoint = originRaised + new Vector3(Mathf.Sin(angle), 0, Mathf.Cos(angle)) * coneRange;
            Debug.DrawLine(prevPoint, nextPoint, new Color(1f, 1f, 1f, 0.2f), 2f, false);
            prevPoint = nextPoint;
        }
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
            invincible.SetInvincible(true);   
        }
    }
    
    public void DisableInvincible()
    {
        if (invincible)
        {
            invincible.SetInvincible(false);   
        }
    }
}
