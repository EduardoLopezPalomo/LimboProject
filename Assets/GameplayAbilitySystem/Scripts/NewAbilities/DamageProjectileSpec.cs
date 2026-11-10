using System.Collections;
using StarterAssets;
using UnityEngine;

public class DamageProjectileSpec : GameplayAbilitySpec
{
    private Animator animator;
    private CharacterController charactercontroller;
    private ThirdPersonController personcontroller;
    private AbilityHolder abilityHolder;
    private MakeInvincible makeInvincible;
    private AttributeSystem attributeSystem;
    private CharacterSFX characterSFX;

    private float angle;
    private float DistanceOffset;
    private float backDashDistance;
    private float dashTime;
    private VFXDefinition projectile;
    private GameObject player;

    private Coroutine safetyCoroutine;
    private bool dashEnded;
    private float resistanceNeeded;

    public DamageProjectileSpec(GameplayAbility ability, GameObject owner, VFXDefinition projectileObject, float throwAngle, float offset, float distance, float time, float resistance) : base(ability, owner)
    {
        animator = owner.GetComponent<Animator>();
        charactercontroller = owner.GetComponent<CharacterController>();
        personcontroller = owner.GetComponent<ThirdPersonController>();
        abilityHolder = owner.GetComponent<AbilityHolder>();
        makeInvincible = owner.GetComponent<MakeInvincible>();
        attributeSystem = owner.GetComponent<AttributeSystem>();
        characterSFX = owner.GetComponent<CharacterSFX>();

        player = owner;
        projectile = projectileObject;
        angle = throwAngle;
        resistanceNeeded = resistance;
        DistanceOffset = offset;
        backDashDistance = distance;
        dashTime = time;
    }

    public override bool CanActivateAbility()
    {
        if (bOnCooldown || attributeSystem.GetAttributeValue("Resistance") < resistanceNeeded)
        {
            characterSFX.SpawnSFX(SFXType.Resistance);
            return false;

        }
        if (abilityHolder.HasAnyActiveAbility(this))
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

    private void ThrowProjectiles()
    {
        Vector3 initialPosition = player.transform.position + new Vector3(0f, 1f, 0f);
        initialPosition += player.transform.forward * DistanceOffset;

        Vector3 right = player.transform.right;

        Vector3 position1 = initialPosition + right;
        Vector3 position2 = initialPosition - right;

        Vector3 direction1 = Quaternion.Euler(0f, angle, 0f) * player.transform.forward;
        Vector3 direction2 = Quaternion.Euler(0f, -angle, 0f) * player.transform.forward;

        CreateProjectile(initialPosition, player.transform.rotation);
        CreateProjectile(position1, Quaternion.LookRotation(direction1));
        CreateProjectile(position2, Quaternion.LookRotation(direction2));
        VibrationManager.Instance.Vibrate(0.3f, 0.8f, 0.15f);
    }

    private void CreateProjectile(Vector3 position, Quaternion direction)
    {
        GameObject spawned = VFXManager.Instance.Play(projectile, position, direction, player);

        // The projectile is the player's: whoever guards or parries it has to know who to answer to
        // (see IHasInstigator). Assigned after Play, which is what activates the pooled object.
        HazardInstigator.Assign(spawned, player);
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
        ThrowProjectiles();
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
