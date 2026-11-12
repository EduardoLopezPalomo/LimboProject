using UnityEngine;

/// <summary>
/// The live counter-attack escalation of ONE enemy instance (T-042).
///
/// Before this component existed the chance was a single float on the PLAYER's weapon
/// (PlayerWeaponController.counterAttackChance -> PlayerWeaponDamage.counterAttackChance), copied once at
/// Awake, so every enemy on screen shared one coin flip and it had no memory. The tunables now live in a
/// CounterAttackProfile and the value lives here, per instance: two enemies caught by the same swing
/// escalate separately, and a freshly spawned enemy always starts at the profile's base.
///
/// Who calls what:
///   - PlayerWeaponDamage asks for a roll (RollCounter) when it hits a guarding enemy whose behaviour
///     graph reports AttackingState.Defend. The guard gate stays on the caller: this class only answers
///     "does the roll win", because a counter against an enemy that is not guarding has no animation.
///   - Every damage source calls NotifyDamageTaken once the hit actually landed. A swing that was blocked,
///     eaten by a counter or absorbed by invincibility must NOT call it - a hit that did nothing escalates
///     nothing.
///
/// The value is spent, not banked: a counter that lands resets it to base the same frame, so the player's
/// read is "bait one out, then commit". Scaled Time.deltaTime, like the rest of the enemy AI.
/// </summary>
public class EnemyCounterAttackState : MonoBehaviour
{
    [Tooltip("This enemy's tunables. Assign one from Assets/Enemies/CounterAttackProfiles/. Without a " +
             "profile the enemy never counters, and the absence is reported once instead of silently.")]
    [SerializeField] private CounterAttackProfile profile;

    [Tooltip("Logs every hit, roll and cool-down step of THIS enemy. Leave off outside a balancing " +
             "session - it is one line per hit.")]
    [SerializeField] private bool debugLog = false;

    /// <summary>The chance the next roll will use. 0 until a profile is assigned.</summary>
    public float CurrentChance { get { return currentChance; } }

    /// <summary>False when this enemy has no profile and therefore cannot counter at all.</summary>
    public bool HasProfile { get { return profile != null; } }

    /// <summary>The live chance raised to whole percent, for logs and debug HUDs.</summary>
    public int CurrentChancePercent { get { return Mathf.RoundToInt(currentChance * 100f); } }

    private float currentChance = 0f;
    private float lastHitTime = 0f;
    private bool missingProfileReported = false;

    /// <summary>
    /// A pooled or freshly instantiated enemy must never inherit the previous life's build-up, so the
    /// value is rebuilt from the profile on every enable.
    /// </summary>
    private void OnEnable()
    {
        ResetToBase();
    }

    /// <summary>
    /// Call once per hit that actually dealt damage to this enemy. Pushes the chance up by the profile's
    /// BonusPerHit (never past MaxChance) and restarts the cool-down timer.
    /// </summary>
    public void NotifyDamageTaken()
    {
        if (!HasUsableProfile()) return;

        currentChance = Mathf.Min(currentChance + profile.BonusPerHit, profile.MaxChance);
        lastHitTime = Time.time;

        if (debugLog)
        {
            Debug.Log($"[CounterAttack:{name}] hit -> chance {CurrentChancePercent}% " +
                $"(base {Mathf.RoundToInt(profile.BaseChance * 100f)}%, cap " +
                $"{Mathf.RoundToInt(profile.MaxChance * 100f)}%)");
        }
    }

    /// <summary>
    /// Rolls the counter against the live chance. A win spends the build-up: the value goes straight back
    /// to base, which is what keeps a guard from becoming a permanent counter wall.
    /// The caller owns the guard gate - this only answers the dice.
    /// </summary>
    public bool RollCounter()
    {
        if (!HasUsableProfile()) return false;

        float roll = UnityEngine.Random.Range(0f, 1f);
        bool countered = roll < currentChance;

        if (countered) ResetToBase();

        if (debugLog)
        {
            Debug.Log($"[CounterAttack:{name}] roll {roll:F2} vs {CurrentChancePercent}% -> " +
                $"{(countered ? "COUNTER (spent, back to base)" : "no counter")}");
        }

        return countered;
    }

    /// <summary>
    /// Back to the profile's base immediately. Used by a successful counter and by OnEnable; the
    /// cool-down path in Update slides there instead of snapping.
    /// </summary>
    public void ResetToBase()
    {
        currentChance = profile != null ? profile.BaseChance : 0f;
        lastHitTime = Time.time;
    }

    /// <summary>
    /// Cool down: once the enemy has gone IdleTimeout seconds without a damaging hit, slide the chance
    /// back to base at DecayRate per second. Does nothing for the flat profile shipped today, so the cost
    /// is two comparisons until a profile actually escalates.
    /// </summary>
    private void Update()
    {
        if (profile == null || profile.DecayRate <= 0f) return;
        if (currentChance <= profile.BaseChance) return;
        if (Time.time - lastHitTime < profile.IdleTimeout) return;

        currentChance = Mathf.MoveTowards(currentChance, profile.BaseChance,
            profile.DecayRate * Time.deltaTime);

        if (debugLog && currentChance <= profile.BaseChance)
        {
            Debug.Log($"[CounterAttack:{name}] cooled down to base {CurrentChancePercent}%");
        }
    }

    /// <summary>
    /// Without a profile the enemy simply never counters, and that is reported once per component instead
    /// of once per swing - the same shape EnemyWeaponController uses for a weapon with no damage component.
    /// </summary>
    private bool HasUsableProfile()
    {
        if (profile != null) return true;

        if (!missingProfileReported)
        {
            missingProfileReported = true;
            Debug.LogError($"[EnemyCounterAttackState:{name}] has no CounterAttackProfile assigned, so " +
                "this enemy can never counter-attack. Assign one on the prefab " +
                "(Assets/Enemies/CounterAttackProfiles/ - Example.asset is the flat 0.5 reference).");
        }
        return false;
    }
}
