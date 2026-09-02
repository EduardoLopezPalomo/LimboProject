using UnityEngine;
using StarterAssets;
using System.Collections;

public class DashSpec : GameplayAbilitySpec
{
    private int dashDistance;
    private float dashTime;
    private float resistanceNeeded;
    private Animator animator;
    private AttributeSystem attributeSystem;
    private CharacterController charactercontroller;
    private ThirdPersonController personcontroller;
    private AbilityHolder abilityHolder;
    private MakeInvincible makeInvincible;
    private StarterAssetsInputs starterInputs;
    private CharacterSFX characterSFX;

    private Vector3 direction;
    private Coroutine safetyCoroutine;
    private bool dashEnded;

    public DashSpec(GameplayAbility ability, GameObject owner, int dashForce, float dashTimeIn, float resistanceNeededIn) : base(ability, owner)
    {
        animator = owner.GetComponent<Animator>();
        charactercontroller = owner.GetComponent<CharacterController>();
        personcontroller = owner.GetComponent<ThirdPersonController>();
        abilityHolder = owner.GetComponent<AbilityHolder>();
        makeInvincible = owner.GetComponent<MakeInvincible>();
        starterInputs = owner.GetComponent<StarterAssetsInputs>();
        attributeSystem = owner.GetComponent<AttributeSystem>();
        characterSFX = owner.GetComponent<CharacterSFX>();
        dashTime = dashTimeIn;
        dashDistance = dashForce;
        resistanceNeeded = resistanceNeededIn;

    }

    public override bool CanActivateAbility()
    {
        if (bOnCooldown || attributeSystem.GetAttributeValue("Resistance") < resistanceNeeded)
        {
            characterSFX.SpawnSFX(SFXType.Resistance);
            return false;
         
        }
        if (abilityHolder.HasAnyActiveAbility(this) || abilityHolder.IsAbilityActive("Dash"))
        {
            return false;
        }
        
        return true;
    }

    public override void ActivateAbility()
    {
        base.ActivateAbility();
        
        animator.SetTrigger("Dash");
        direction = GetAimDirection();
        RotateTowardsDashDirection();
        
        makeInvincible.SetInvincible(true);
        personcontroller.SetInputAviable(false);

        dashEnded = false;
        safetyCoroutine = abilityHolder.StartCoroutine(SafetyTimeout());
    }
    
    private IEnumerator SafetyTimeout()
    {
        yield return new WaitForSeconds(dashTime + 0.15f); 

        if (!dashEnded)
        {
            EndDash();
        }
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
    
    private void RotateTowardsDashDirection()
    {
        if(direction.sqrMagnitude < 0.001f)
            return;

        owner.transform.rotation = Quaternion.LookRotation(direction);
    }

    private IEnumerator waitTillEndFrame()
    {
        float elapsed = 0f;
        float speed = dashDistance / dashTime;

        while (elapsed < dashTime)
        {
            charactercontroller.Move(direction * speed * Time.deltaTime);
            elapsed += Time.deltaTime;

            yield return null;
        }
        
        EndDash();
    }

    public void StartMoving()
    {
        abilityHolder.StartCoroutine(waitTillEndFrame());
    }

    public override void CancelAbility()
    {
        base.CancelAbility();
        if (safetyCoroutine != null)
        {
            abilityHolder.StopCoroutine(safetyCoroutine);
            safetyCoroutine = null;
        }
        EndDash();
    }

    public void EndDash()
    {
        if (dashEnded) return;
        dashEnded = true;

        if (safetyCoroutine != null)
        {
            abilityHolder.StopCoroutine(safetyCoroutine);
            safetyCoroutine = null;
        }

        makeInvincible.SetInvincible(false);
        personcontroller.SetInputAviable(true);
        charactercontroller.Move(Vector3.zero);
        EndAbility();
    }
}
