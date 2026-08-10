using UnityEngine;
using StarterAssets;
using System.Collections;

public class DashSpec : GameplayAbilitySpec
{
    private int dashDistance;
    private float dashTime;
    private Animator animator;
    private CharacterController charactercontroller;
    private ThirdPersonController personcontroller;
    private AbilityHolder abilityHolder;
    private MakeInvincible makeInvincible;
    private StarterAssetsInputs starterInputs;

    private Vector3 direction;


    public DashSpec(GameplayAbility ability, GameObject owner, int dashForce, float dashTimeIn) : base(ability, owner)
    {
        animator = owner.GetComponent<Animator>();
        charactercontroller = owner.GetComponent<CharacterController>();
        personcontroller = owner.GetComponent<ThirdPersonController>();
        abilityHolder = owner.GetComponent<AbilityHolder>();
        makeInvincible = owner.GetComponent<MakeInvincible>();
        starterInputs = owner.GetComponent<StarterAssetsInputs>();
        dashTime = dashTimeIn;
        dashDistance = dashForce;

    }

    public override void ActivateAbility()
    {
        if (bOnCooldown || abilityHolder.HasAnyActiveAbility(this))
        {
            base.EndAbility();
            return;
        }
        base.ActivateAbility();
        
        animator.SetBool("Dash", true);
        direction = GetAimDirection();
        RotateTowardsDashDirection();
        
        makeInvincible.SetInvincible(true);
        personcontroller.SetInputAviable(false);
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
        
        animator.SetBool("Dash", false);
    }

    public void StartMoving()
    {
        abilityHolder.StartCoroutine(waitTillEndFrame());
    }

    public void EndDash()
    {
        makeInvincible.SetInvincible(false);
        personcontroller.SetInputAviable(true);
        charactercontroller.Move(Vector3.zero);
        EndAbility();
    }
}
