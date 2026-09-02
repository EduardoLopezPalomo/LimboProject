using System.Collections;
using StarterAssets;
using UnityEngine;

public class DamageProyectileSpec : GameplayAbilitySpec
{
    private Animator animator;
    private CharacterController charactercontroller;
    private ThirdPersonController personcontroller;
    private AbilityHolder abilityHolder;
    private MakeInvincible makeInvincible;
    private AttributeSystem attributeSystem;

    private float angle;
    private float DistanceOffset;
    private float backDashDistance;
    private float dashTime;
    private VFXDefinition proyectile;
    private GameObject player;

    private Coroutine safetyCoroutine;
    private bool dashEnded;
    private float resistanceNeeded;

    public DamageProyectileSpec(GameplayAbility ability, GameObject owner, VFXDefinition proyectileObject, float throwAngle, float offset, float distance, float time, float resistance) : base(ability, owner)
    {
        animator = owner.GetComponent<Animator>();
        charactercontroller = owner.GetComponent<CharacterController>();
        personcontroller = owner.GetComponent<ThirdPersonController>();
        abilityHolder = owner.GetComponent<AbilityHolder>();
        makeInvincible = owner.GetComponent<MakeInvincible>();
        attributeSystem = owner.GetComponent<AttributeSystem>();

        player = owner;
        proyectile = proyectileObject;
        angle = throwAngle;
        resistanceNeeded = resistance;
        DistanceOffset = offset;
        backDashDistance = distance;
        dashTime = time;
    }

    public override bool CanActivateAbility()
    {
        if (abilityHolder.HasAnyActiveAbility(this) || bOnCooldown || attributeSystem.GetAttributeValue("Resistance") < resistanceNeeded)
        {
            return false;
        }
        return true;
    }

    public override void ActivateAbility()
    {
        if (bOnCooldown)
        {
            EndAbility();
            return;
        }
        base.ActivateAbility();
        animator.SetTrigger("Throw");

        makeInvincible.SetInvincible(true);
        personcontroller.SetInputAviable(false);

        dashEnded = false;
        safetyCoroutine = abilityHolder.StartCoroutine(SafetyTimeout());
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

    private void ThrowProyectiles()
    {
        Vector3 initialPosition = player.transform.position + new Vector3(0f, 1f, 0f);
        initialPosition += player.transform.forward * DistanceOffset;

        Vector3 right = player.transform.right;

        Vector3 position1 = initialPosition + right;
        Vector3 position2 = initialPosition - right;

        Vector3 direction1 = Quaternion.Euler(0f, angle, 0f) * player.transform.forward;
        Vector3 direction2 = Quaternion.Euler(0f, -angle, 0f) * player.transform.forward;

        CreateProyectile(initialPosition, player.transform.rotation);
        CreateProyectile(position1, Quaternion.LookRotation(direction1));
        CreateProyectile(position2, Quaternion.LookRotation(direction2));
        VibrationManager.Instance.Vibrate(0.3f, 0.8f, 0.15f);
    }

    private void CreateProyectile(Vector3 position, Quaternion direction)
    {
        VFXManager.Instance.Play(proyectile, position, direction, player);
    }

    private IEnumerator SafetyTimeout()
    {
        yield return new WaitForSeconds(dashTime + 0.5f);

        if (!dashEnded)
        {
            EndDash();
        }
    }

    private IEnumerator waitTillEndFrame()
    {
        float elapsed = 0f;
        float speed = backDashDistance / dashTime;

        while (elapsed < dashTime)
        {
            charactercontroller.Move(-player.transform.forward * speed * Time.deltaTime);
            elapsed += Time.deltaTime;

            yield return null;
        }

        EndDash();
    }

    public void StartDash()
    {
        ThrowProyectiles();
        abilityHolder.StartCoroutine(waitTillEndFrame());
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
