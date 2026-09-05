using UnityEngine;
using StarterAssets;
using System;

[CreateAssetMenu(menuName = "AbilitySystem/Responses/PlayerHealth")]
public class PlayerHealthResponse : GeneralHealthResponse
{
    public static event Action OnPlayerHurt;

    protected override void HurtBehavior(AttributeSystem owner, bool CanResponse)
    {

        VibrationManager.Instance.Vibrate(0.5f, 0.8f, 0.5f);
        CharacterVFX vfx = owner.GetComponent<CharacterVFX>();
        CharacterSFX sfx = owner.GetComponent<CharacterSFX>();
        if (vfx != null && sfx != null)
        {
            vfx.SpawnVFX(VFXType.Damaged);
            sfx.SpawnRandomSFX(SFXType.Damaged);
        }

        if (!CanResponse) return;

        KnockBack knockBack = owner.GetComponent<KnockBack>();
        if (knockBack)
        {
            knockBack.StartKnockBack();
        }
        OnPlayerHurt?.Invoke();
    }

    protected override void DeathBehavior(AttributeSystem owner)
    {
        Animator animator = owner.GetComponent<Animator>();
        ThirdPersonController personController = owner.GetComponent<ThirdPersonController>();
        LockOn lockOn = owner.GetComponent<LockOn>();
        if (lockOn != null)
        {
            lockOn.ClearLock();
        }
        if (personController != null)
        {
            personController.SetInputAviable(false);
        }
        if (animator != null)
        {
            animator.SetTrigger("IsDeath");
        }

        VibrationManager.Instance.Vibrate(0.9f, 0.5f, 2f);
        MusicManager.Instance.StartDeath();
    }
}
