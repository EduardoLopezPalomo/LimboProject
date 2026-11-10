using System;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;

/// <summary>
/// Shared, stateless pipeline for applying a melee/ranged hit to a target.
///
/// Both <see cref="PlayerWeaponDamage"/> (player hitting enemies) and
/// <see cref="EnemyWeaponDamage"/> (enemies hitting the player) used to carry their own copy of:
///   - resolving the defender's GameplayEffectManager / AttributeSystem,
///   - guarding the combo index against the configured effect lists,
///   - rejecting a null effect slot,
///   - firing camera shake and controller rumble,
///   - calling ApplyEffect.
///
/// Those copies had already drifted apart (only one of them null-checked the defender, only one
/// checked the block list size, and each invented its own error text), which is how the P0.1 and
/// P0.2 bugs were able to exist in the first place. This class owns that logic once so the two
/// weapon classes only have to describe the *flavour* of their hit.
///
/// Every failure here is loud (LogError) and drops the hit on purpose rather than silently
/// mis-applying the wrong combo step.
/// </summary>
public static class DamageResolver
{
    /// <summary>
    /// Result of a hit attempt. Returned so callers can assert/react without parsing logs.
    /// </summary>
    public enum Outcome
    {
        /// <summary>The effect was applied to the defender.</summary>
        Applied,
        /// <summary>The defender is missing GameplayEffectManager and/or AttributeSystem.</summary>
        MissingTargetComponents,
        /// <summary>The combo index is outside one of the configured effect lists.</summary>
        InvalidComboIndex,
        /// <summary>The list slot for this combo step is not assigned.</summary>
        NullEffect
    }

    /// <summary>
    /// The two components a hit needs on the receiving side. Always used as a pair so a half
    /// resolved defender cannot be passed down the pipeline.
    /// </summary>
    public struct Defender
    {
        public GameplayEffectManager Effects;
        public AttributeSystem Attributes;

        public bool IsValid { get { return Effects != null && Attributes != null; } }
    }

    /// <summary>
    /// Gamepad rumble to play alongside a hit. Maps straight onto
    /// <see cref="VibrationManager.Vibrate(float, float, float)"/>.
    /// </summary>
    public struct Rumble
    {
        public float LowFrequency;
        public float HighFrequency;
        public float Duration;
    }

    /// <summary>
    /// Everything that describes one combo hit. Built by the weapon class that owns the attack;
    /// the fields that differ between player and enemy attacks are exactly the optional ones.
    /// </summary>
    public sealed class ComboHit
    {
        /// <summary>The character performing the attack. Used for ownership and error messages.</summary>
        public GameObject Attacker;

        /// <summary>The character being hit.</summary>
        public GameObject Target;

        /// <summary>Which combo step landed. Indexes into the effect lists below.</summary>
        public int ComboIndex;

        /// <summary>Effects for a clean hit. Required.</summary>
        public IList<GameplayEffect> NormalEffects;

        /// <summary>
        /// Effects for a hit the target blocked. Optional. When present its size is validated
        /// against ComboIndex too, so a combo list that is shorter than the normal list is
        /// reported instead of being silently skipped.
        /// </summary>
        public IList<GameplayEffect> BlockedEffects;

        /// <summary>Selects <see cref="BlockedEffects"/> instead of <see cref="NormalEffects"/>.</summary>
        public bool UseBlockedEffects;

        /// <summary>
        /// Invoked once the combo index is known to be valid and just before the blocked effect is
        /// read. Used for the defender's block reaction without the resolver needing to know about it.
        /// </summary>
        public Action OnBlocked;

        /// <summary>
        /// Whether to find the defender's components on the hit collider itself (enemies hit the
        /// player capsule directly) or on its parent hierarchy (weapons hit a child collider).
        /// </summary>
        public bool SearchTargetInParent;

        /// <summary>Camera shake for this hit. Optional: null plays no shake.</summary>
        public CameraShakeData? Shake;

        /// <summary>Impulse source paired with <see cref="Shake"/>.</summary>
        public CinemachineImpulseSource ImpulseSource;

        /// <summary>Controller rumble for this hit. Optional: null plays no rumble.</summary>
        public Rumble? Haptics;
    }

    /// <summary>
    /// Prefix used by every message this resolver emits, so combat logs stay greppable per attacker.
    /// </summary>
    public static string Label(GameObject attacker)
    {
        return "[WeaponDamage:" + (attacker != null ? attacker.name : "Unknown") + "]";
    }

    /// <summary>
    /// Resolves the defender's <see cref="GameplayEffectManager"/> and <see cref="AttributeSystem"/>.
    /// Returns false and logs an error when either is missing, which previously threw a
    /// NullReferenceException from the middle of a hit.
    /// </summary>
    public static bool TryResolveDefender(GameObject target, bool searchInParent, GameObject attacker, out Defender defender)
    {
        defender = new Defender();

        if (target == null)
        {
            Debug.LogError(Label(attacker) + " Cannot resolve a defender from a null target. The hit is being DROPPED.");
            return false;
        }

        if (searchInParent)
        {
            defender.Effects = target.GetComponentInParent<GameplayEffectManager>();
            defender.Attributes = target.GetComponentInParent<AttributeSystem>();
        }
        else
        {
            defender.Effects = target.GetComponent<GameplayEffectManager>();
            defender.Attributes = target.GetComponent<AttributeSystem>();
        }

        if (defender.IsValid) return true;

        Debug.LogError(Label(attacker) + $" Cannot apply hit to '{target.name}': target is missing required components. " +
            $"GameplayEffectManager={(defender.Effects != null)}, AttributeSystem={(defender.Attributes != null)}. " +
            "The hit is being DROPPED.");
        return false;
    }

    /// <summary>
    /// Validates the combo index against the configured lists and picks the effect for this step.
    /// </summary>
    public static bool TrySelectEffect(ComboHit hit, out GameplayEffect effect, out Outcome outcome)
    {
        effect = null;
        outcome = Outcome.NullEffect;

        if (hit.NormalEffects == null)
        {
            Debug.LogError(Label(hit.Attacker) + " has no combo effects list assigned. The hit is being DROPPED.");
            outcome = Outcome.InvalidComboIndex;
            return false;
        }

        int normalCount = hit.NormalEffects.Count;

        if (hit.UseBlockedEffects && hit.BlockedEffects == null)
        {
            Debug.LogError(Label(hit.Attacker) + " resolved this hit as BLOCKED but has no blocked-effects list " +
                "assigned. The attack is being DROPPED rather than silently applying unblocked damage.");
            outcome = Outcome.InvalidComboIndex;
            return false;
        }

        int blockedCount = hit.BlockedEffects != null ? hit.BlockedEffects.Count : normalCount;

        if (hit.ComboIndex < 0 || hit.ComboIndex >= normalCount || hit.ComboIndex >= blockedCount)
        {
            Debug.LogError(Label(hit.Attacker) + $" Out-of-range combo index {hit.ComboIndex}: " +
                $"gameplayEffects.Count={normalCount}, gameplayEffectsBlock.Count={blockedCount}. " +
                "The attack is being DROPPED until the effects list is corrected.");
            outcome = Outcome.InvalidComboIndex;
            return false;
        }

        if (hit.UseBlockedEffects)
        {
            hit.OnBlocked?.Invoke();
            effect = hit.BlockedEffects[hit.ComboIndex];
        }
        else
        {
            effect = hit.NormalEffects[hit.ComboIndex];
        }

        if (effect == null)
        {
            Debug.LogWarning(Label(hit.Attacker) + $" Combo step {hit.ComboIndex} has no GameplayEffect assigned on " +
                $"'{hit.Attacker?.name}'. The hit is being DROPPED.");
            return false;
        }

        outcome = Outcome.Applied;
        return true;
    }

    /// <summary>
    /// Plays the camera shake half of a hit's feedback. Safe to call with no manager or no shake.
    /// </summary>
    public static void PlayShake(CameraShakeData? data, CinemachineImpulseSource impulseSource)
    {
        if (!data.HasValue) return;

        if (CameraShakeManager.instance == null)
        {
            Debug.LogError(Label(null) + " CameraShakeManager.instance is missing from the scene; the hit's camera shake was skipped.");
            return;
        }

        CameraShakeManager.instance.CameraShake(data.Value, impulseSource);
    }

    /// <summary>
    /// Plays the rumble half of a hit's feedback. Safe to call with no manager or no rumble.
    /// </summary>
    public static void PlayRumble(Rumble? rumble)
    {
        if (!rumble.HasValue) return;

        if (VibrationManager.Instance == null) return;

        Rumble r = rumble.Value;
        VibrationManager.Instance.Vibrate(r.LowFrequency, r.HighFrequency, r.Duration);
    }

    /// <summary>
    /// Plays the sound half of a hit / hazard cue. Safe to call with no manager.
    ///
    /// A null definition is a legitimate choice made per prefab ("this variant is silent"), so it is
    /// ignored. A missing <see cref="SoundFXManager.instance"/> is a scene problem: the hazard scripts
    /// used to call it unguarded from OnEnable, which is one missing manager away from killing the
    /// activation of every pooled VFX that has a sound assigned.
    /// </summary>
    public static void PlaySound(SFXDefinition def, Transform source)
    {
        if (def == null) return;

        if (SoundFXManager.instance == null)
        {
            Debug.LogError(Label(null) + $" SoundFXManager.instance is missing from the scene; the sound '{def.name}' was skipped.");
            return;
        }

        SoundFXManager.instance.PlaySoundRandomPitch(def.sfx, source, def.volume, def.minPitch, def.maxPitch);
    }

    /// <summary>
    /// True when <paramref name="tag"/> is a tag name Unity actually knows (a built-in one such as
    /// "Player", or one listed in Project Settings &gt; Tags and Layers).
    ///
    /// There is no API that lists the defined tags, so this asks Unity to compare the name once and
    /// treats the <see cref="UnityException"/> it throws for an undefined name as the answer. Doing it
    /// in Awake means a typo is reported a single time on the asset that owns it: left alone, the same
    /// typo throws from inside every OnTriggerEnter of every hazard that shares the prefab.
    /// </summary>
    public static bool TagExists(string tag, MonoBehaviour probe)
    {
        if (string.IsNullOrEmpty(tag) || probe == null) return false;

        try
        {
            probe.gameObject.CompareTag(tag);
            return true;
        }
        catch (UnityException)
        {
            return false;
        }
    }

    /// <summary>
    /// Applies an already validated hit to an already resolved defender: feedback first, then
    /// <see cref="GameplayEffectManager.ApplyEffect"/>.
    /// </summary>
    public static Outcome ApplyResolved(ComboHit hit, Defender defender)
    {
        if (!TrySelectEffect(hit, out GameplayEffect effect, out Outcome outcome)) return outcome;

        PlayShake(hit.Shake, hit.ImpulseSource);
        PlayRumble(hit.Haptics);

        defender.Effects.ApplyEffect(effect, defender.Attributes);
        return Outcome.Applied;
    }

    /// <summary>
    /// Full pipeline for one combo hit: resolve the defender, validate and select the effect,
    /// play feedback, apply it.
    /// </summary>
    public static Outcome ApplyHit(ComboHit hit)
    {
        if (hit == null)
        {
            Debug.LogError("[WeaponDamage:Unknown] ApplyHit was called with no ComboHit.");
            return Outcome.MissingTargetComponents;
        }

        if (!TryResolveDefender(hit.Target, hit.SearchTargetInParent, hit.Attacker, out Defender defender))
        {
            return Outcome.MissingTargetComponents;
        }

        return ApplyResolved(hit, defender);
    }

    /// <summary>
    /// Everything that describes a one-off hit: a single explicit effect instead of a combo list.
    /// Used by the paths that used to call <c>ApplyEffect</c> directly (counter attack, perfect
    /// parry, explosion, lightning) so that component resolution, feedback and the loud-drop
    /// policy stay in one place for those too.
    /// </summary>
    public sealed class SingleHit
    {
        /// <summary>The character responsible for the effect. Used for ownership and error messages.</summary>
        public GameObject Attacker;

        /// <summary>
        /// Who receives the effect. This is the victim for a damage effect, but the attacker itself
        /// for a self-effect such as the enemy's perfect-parry stun or the player's counter debuff.
        /// </summary>
        public GameObject Target;

        /// <summary>The effect to apply. Required.</summary>
        public GameplayEffect Effect;

        /// <summary>See <see cref="ComboHit.SearchTargetInParent"/>.</summary>
        public bool SearchTargetInParent;

        /// <summary>Camera shake for this hit. Optional: null plays no shake.</summary>
        public CameraShakeData? Shake;

        /// <summary>Impulse source paired with <see cref="Shake"/>.</summary>
        public CinemachineImpulseSource ImpulseSource;

        /// <summary>Controller rumble for this hit. Optional: null plays no rumble.</summary>
        public Rumble? Haptics;
    }

    /// <summary>
    /// Full pipeline for a one-off effect: resolve the receiver, play feedback, apply the effect.
    /// A null effect is reported and dropped instead of reaching
    /// <see cref="GameplayEffectManager.ApplyEffect"/> with nothing in it.
    /// </summary>
    public static Outcome ApplySingle(SingleHit hit)
    {
        if (hit == null)
        {
            Debug.LogError("[WeaponDamage:Unknown] ApplySingle was called with no SingleHit.");
            return Outcome.MissingTargetComponents;
        }

        if (hit.Effect == null)
        {
            Debug.LogError(Label(hit.Attacker) + " A single-effect hit has no GameplayEffect assigned." +
                " The effect is being DROPPED.");
            return Outcome.NullEffect;
        }

        if (!TryResolveDefender(hit.Target, hit.SearchTargetInParent, hit.Attacker, out Defender defender))
        {
            return Outcome.MissingTargetComponents;
        }

        PlayShake(hit.Shake, hit.ImpulseSource);
        PlayRumble(hit.Haptics);

        defender.Effects.ApplyEffect(hit.Effect, defender.Attributes);
        return Outcome.Applied;
    }
}
