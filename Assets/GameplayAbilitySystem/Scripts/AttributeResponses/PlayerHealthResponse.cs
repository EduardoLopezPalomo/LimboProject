using UnityEngine;
using StarterAssets;
using System;

[CreateAssetMenu(menuName = "AbilitySystem/Responses/PlayerHealth")]
public class PlayerHealthResponse : GeneralHealthResponse
{
    public static event Action OnPlayerHurt;

    [SerializeField] private float deathCooldown = 3f;
    private float lastDeathTime = -Mathf.Infinity;

    // T-013: rumble and death music used to dereference their managers directly. Neither manager is on the
    // character: VibrationManager arrives on the GlobalManager prefab and MusicManager is its own prefab placed in
    // the scene, so both are null in a scene that forgot them and during the frames before their Awake. A direct
    // call there threw, which for HurtBehavior meant the hit response stopped halfway - knockback, the OnPlayerHurt
    // listeners and the cues below the throw simply never ran for a cosmetic controller buzz.
    // Both now degrade on their own, and each warns once per asset instead of once per hit: HurtBehavior fires on
    // every landed hit, so an unguarded warning per hit is a worse console than the bug it reports.
    private bool warnedMissingVibration;
    private bool warnedMissingMusic;

    protected override void HurtBehavior(AttributeSystem owner, bool CanResponse)
    {

        Rumble(0.5f, 0.8f, 0.5f);
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
        LockOn lockOn = owner.GetComponent<LockOn>();
        if (lockOn != null)
        {
            lockOn.ClearLock();
        }
        // T-001: held under its own holder name, so a dash or a throw that is still running when the player
        // dies cannot hand movement back on its way out, and the death lock itself cannot outlive the
        // character. PlayerDeath.anim's InputAfterDeath event is what frees movement afterwards (see
        // InputController.Update, which adopts that write instead of keeping the lock as debt).
        // T-011 (open): that release lives ONLY as the event string "InputAfterDeath" at 0.7 s inside
        // Assets/Animations/Other/PlayerDeath.anim - renaming it or reimporting the clip strands movement
        // in silence, exactly like the "ShowDeathMenu" twin at 0.5 s (see PlayerUIController.ShowDeathMenu).
        InputController.AcquireInputLock(owner.gameObject, InputController.LockOwner.Death);
        if (animator != null)
        {
            animator.SetTrigger("IsDeath");
        }

        Rumble(0.9f, 0.5f, 2f);
        StartDeathMusic();
    }

    /// <summary>
    /// T-013: guarded rumble. A missing VibrationManager costs the buzz only - see the field comment for why an
    /// unguarded throw here stopped the rest of the hurt response. Vibrate itself already no-ops without a
    /// gamepad, so keyboard-only play stays silent either way.
    /// </summary>
    private void Rumble(float lowFreq, float highFreq, float duration)
    {
        VibrationManager vibration = VibrationManager.Instance;
        if (vibration == null)
        {
            if (!warnedMissingVibration)
            {
                warnedMissingVibration = true;
                Debug.LogWarning("PlayerHealthResponse: no VibrationManager in this scene (it arrives on the "
                    + "GlobalManager prefab), so hit rumble is skipped.", this);
            }
            return;
        }

        vibration.Vibrate(lowFreq, highFreq, duration);
    }

    /// <summary>
    /// T-013: guarded death music. The death sequence keeps running without MusicManager; only the cue is lost.
    /// </summary>
    private void StartDeathMusic()
    {
        MusicManager music = MusicManager.Instance;
        if (music == null)
        {
            if (!warnedMissingMusic)
            {
                warnedMissingMusic = true;
                Debug.LogWarning("PlayerHealthResponse: no MusicManager in this scene, so the death music cue is "
                    + "skipped. The death animation and the input lock are unaffected.", this);
            }
            return;
        }

        music.StartDeath();
    }
}
