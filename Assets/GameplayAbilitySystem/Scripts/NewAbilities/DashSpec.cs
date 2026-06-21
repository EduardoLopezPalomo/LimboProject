using UnityEngine;
using StarterAssets;
using System.Collections;

public class DashSpec : GameplayAbilitySpec
{
    private int dashDistance;
    private float dashTime;
    private Animator animator;
    private RootMotionController rootMotion;
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
        if (bOnCooldown)
        {
            return;
        }
        base.ActivateAbility();
        if (animator.GetBool("InCombo"))
        {
            abilityHolder.CancelAbility("LightAttack");
            animator.SetBool("InCombo", false);
        }
        animator.SetBool("Dash", true);
        direction = GetAimDirection();
        abilityHolder.StartCoroutine(waitTillEndFrame());
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

    private IEnumerator waitTillEndFrame()
    {
        makeInvincible.SetInvincible(true);
        personcontroller.SetInputAviable(false);

        float elapsed = 0f;
        float speed = dashDistance / dashTime;

        while (elapsed < dashTime)
        {
            charactercontroller.Move(direction * speed * Time.deltaTime);
            elapsed += Time.deltaTime;

            yield return null;
        }
        
        animator.SetBool("Dash", false);
        makeInvincible.SetInvincible(false);
        personcontroller.SetInputAviable(true);
    }

    public override void EndAbility()
    {
        base.EndAbility();
    }
}
