using UnityEngine;
using StarterAssets;
using System;

[CreateAssetMenu(menuName = "AbilitySystem/Responses/PlayerHealth")]
public class PlayerHealthResponse : GeneralHealthResponse
{
    public static event Action OnPlayerHurt;

    [SerializeField] private float deathCooldown = 3f;
    private float lastDeathTime = -Mathf.Infinity;

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

        OnPlayerHurt?.Invoke();

        if (!CanResponse) return;

        KnockBack knockBack = owner.GetComponent<KnockBack>();
        if (knockBack)
        {
            knockBack.StartKnockBack();
        }
    }

    protected override void DeathBehavior(AttributeSystem owner)
    {
        if (Time.time - lastDeathTime < deathCooldown) return;
        lastDeathTime = Time.time;

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
