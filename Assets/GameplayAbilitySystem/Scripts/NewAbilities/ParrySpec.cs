using UnityEngine;
using StarterAssets;
using System.Collections;

public class ParrySpec : GameplayAbilitySpec
{
    private Animator animator;
    private AbilityHolder abilityHolder;
    private DefenseState defenseState;
    private KnockBack kncokBack;
    private ThirdPersonController personController;

    private float parryWindowDuration;

    private bool holdingButton;

    public ParrySpec(GameplayAbility ability, GameObject owner, float parryWindowDurationIn) : base(ability, owner)
    {
        animator = owner.GetComponent<Animator>();
        abilityHolder = owner.GetComponent<AbilityHolder>();
        defenseState = owner.GetComponent<DefenseState>();
        kncokBack = owner.GetComponent<KnockBack>();
        personController = owner.GetComponent<ThirdPersonController>();

        parryWindowDuration = parryWindowDurationIn;
    }

    public override void ActivateAbility()
    {
        if (bOnCooldown || abilityHolder.HasAnyActiveAbility(this))
        {
            base.EndAbility();
            return;
        }
        base.ActivateAbility();

        holdingButton = true;
        personController.SetInputAviable(false);
        animator.SetBool("Blocking", true);
        abilityHolder.StartCoroutine(GuardRoutine());
    }

    private IEnumerator GuardRoutine()
    {
        defenseState.SetState(GuardState.Parry, this);

        float t = 0f;
        while (t < parryWindowDuration)
        {
            // Si el estado ya no es Parry, es que un ataque lo consumió (ResolveHit)
            if (defenseState.State != GuardState.Parry)
                yield break;

            t += Time.deltaTime;
            yield return null;
        }

        // Se acabó la ventana de parry sin recibir golpe → pasa a bloqueo normal
        if (!holdingButton)
        {
            EndGuard();
            yield break;
        }

        defenseState.SetState(GuardState.Block, this);

        // Se mantiene en bloqueo mientras el jugador aguante el botón
        while (holdingButton && defenseState.State == GuardState.Block)
        {
            yield return null;
        }

        if (defenseState.State == GuardState.Block)
            EndGuard();
    }

    public override void CancelAbility()
    {
        base.CancelAbility();
        holdingButton = false;
        animator.SetBool("Blocking", false);
        personController.SetInputAviable(true);
        defenseState.SetState(GuardState.Idle, this);
    }

    // Llamado por DefenseState cuando ResolveHit detecta un parry perfecto
    public void NotifyPerfectParry()
    {
        animator.SetTrigger("ParrySuccess");
        EndGuard();
    }

    // Llamado por DefenseState cuando se rompe la guardia por falta de stamina
    public void NotifyGuardBreak()
    {
        animator.SetBool("Blocking", false);
        animator.SetTrigger("GuardBreak");
        kncokBack.StartKnockBack();
        defenseState.SetState(GuardState.Recovery, this);
    }
    
    private void EndGuard()
    {
        animator.SetBool("Blocking", false);
        defenseState.SetState(GuardState.Idle, this);
        personController.SetInputAviable(true);
        EndAbility();
    }
}
