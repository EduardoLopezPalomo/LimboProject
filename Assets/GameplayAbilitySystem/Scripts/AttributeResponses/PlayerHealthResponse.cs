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
        // Two independent guards, deliberately: these used to be one `vfx != null && sfx != null`, so a
        // character missing EITHER cue played NEITHER — deleting a hurt sound silently deleted the hit flash
        // too, with nothing in the console to follow (T-012). Each cue now degrades on its own.
        // This is not hypothetical: Assets/Characters/Ciros/mesh/player_mesh2.prefab carries CharacterSFX but no
        // CharacterVFX and is placed in SpaceBoss.unity, so that character played no hurt cue at all until now
        // (audit of every prefab that owns either component: 32 with VFX, 33 with SFX, this is the only gap).
        if (vfx != null)
        {
            vfx.SpawnVFX(VFXType.Damaged);
        }
        if (sfx != null)
        {
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
