using UnityEngine;

/// <summary>
/// Tunables for one enemy type's counter-attack escalation (T-042).
///
/// The counter chance is no longer a constant. It starts at <see cref="BaseChance"/>, climbs by
/// <see cref="BonusPerHit"/> every time the player actually damages that enemy, and pays out when the
/// roll against the live value wins while the enemy is guarding. A counter that lands spends the whole
/// build-up (straight back to base), and an enemy left alone for <see cref="IdleTimeout"/> seconds
/// slides back down at <see cref="DecayRate"/> per second.
///
/// The live value is held by <see cref="EnemyCounterAttackState"/> on the enemy, one per instance, so two
/// enemies of the same prefab hit by the same swing keep separate chances. This asset is shared read-only
/// data: one profile can be assigned to every prefab of a type and re-tuned later without touching code.
///
/// Authoring guide for the design team: vault note "07 Designer Handbook / Counter-Attack Profiles".
/// </summary>
[CreateAssetMenu(fileName = "CounterAttackProfile", menuName = "Combat/Counter-Attack Profile")]
public class CounterAttackProfile : ScriptableObject
{
    [Header("Starting chance [0,1]")]
    [Tooltip("Chance of a counter the first time this enemy is hit, and the value every reset returns " +
             "to. 0.5 is the flat coin flip the game shipped with.")]
    [SerializeField] private float baseChance = 0.5f;

    [Header("Escalation")]
    [Tooltip("How much the chance grows after every hit that actually dealt damage. A blocked swing, a " +
             "swing eaten by a counter and a hit on an invincible enemy add nothing. 0 = never escalates.")]
    [SerializeField] private float bonusPerHit = 0f;

    [Tooltip("The chance stops climbing here no matter how many hits land. Clamped to >= Base Chance.")]
    [SerializeField] private float maxChance = 1f;

    [Header("Cool down")]
    [Tooltip("Seconds without being hit before the chance starts sliding back to Base Chance. " +
             "0 = the slide starts as soon as the enemy stops being hit.")]
    [SerializeField] private float idleTimeout = 0f;

    [Tooltip("How fast the chance falls back, in chance per second (0.25 means a full 1.0 -> 0.0 slide " +
             "takes 4 seconds). 0 = never decays.")]
    [SerializeField] private float decayRate = 0f;

    /// <summary>Chance the enemy starts with, and the value it returns to after a counter lands.</summary>
    public float BaseChance { get { return baseChance; } }

    /// <summary>Chance added by every hit that actually dealt damage.</summary>
    public float BonusPerHit { get { return bonusPerHit; } }

    /// <summary>Upper bound of the live chance.</summary>
    public float MaxChance { get { return maxChance; } }

    /// <summary>Seconds of peace before the slide back to base begins.</summary>
    public float IdleTimeout { get { return idleTimeout; } }

    /// <summary>Fall-back speed, in chance per second.</summary>
    public float DecayRate { get { return decayRate; } }

    /// <summary>True when neither escalation nor decay can ever move the value: a flat profile.</summary>
    public bool IsFlat { get { return bonusPerHit <= 0f && decayRate <= 0f; } }

    /// <summary>
    /// Keeps a hand-edited asset from producing nonsense: a chance outside [0,1] makes the roll always win
    /// or never win, and a cap below the base would mean "go down with every hit". Clamped quietly so the
    /// Inspector shows the value that will actually be used.
    /// </summary>
    private void OnValidate()
    {
        baseChance = Mathf.Clamp01(baseChance);
        maxChance = Mathf.Clamp01(maxChance);
        if (maxChance < baseChance) maxChance = baseChance;

        bonusPerHit = Mathf.Max(0f, bonusPerHit);
        idleTimeout = Mathf.Max(0f, idleTimeout);
        decayRate = Mathf.Max(0f, decayRate);
    }
}
