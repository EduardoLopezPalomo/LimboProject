/*
 * AbilityIds.cs
 *
 * Centralized registry of ability RUNTIME NAMES.
 *
 * In the GAS an ability is identified by its `Name`, hashed with
 * `Animator.StringToHash(Name)`; that hash becomes the ability's `Id`
 * (set in GameplayAbility.OnValidate). Everything that activates or queries an
 * ability does it with the *string* Name (AbilityHolder.ActivateAbility(name, ...),
 * IsAbilityActive(name), HasAbility(name), CancelAbility(name), and the enemy
 * Action nodes / boss AttackDefinition data).
 *
 * The single footgun this registry protects against: rename an ability asset's
 * Name and every string reference silently stops resolving (the hash no longer
 * matches the new Id), so the attack just does nothing and only a warning is
 * logged. This is P1.1 of SYSTEMS_REVIEW_PLAN.md.
 *
 * IMPORTANT: the values here are the RUNTIME `Name` (the `Name` field of each
 * ScriptableObject), NOT the asset file names — they differ a lot, e.g.
 *   HealPlayer.asset            -> Name "Heal"
 *   MageCounterAttack.asset     -> Name "CounterAttack"
 *   CounterAttackBarb.asset     -> Name "CounterAttack"
 * Always use the runtime `Name` as the const value.
 *
 * USAGE (prefer over magic strings):
 *     abilityHolder.ActivateAbility(AbilityIds.LightAttack, owner);
 *     if (abilityHolder.IsAbilityActive(AbilityIds.Dash)) { ... }
 *
 * NOTE: the ability definitions still live in the inspector (ScriptableObjects).
 * This registry is the single source of truth for the NAMES used to reference
 * them from code. Making the inspector Names derive from this (compile-time Ids)
 * is the planned follow-up in SYSTEMS_REVIEW_PLAN.md (P1.1, higher effort).
 */
public static class AbilityIds
{
    // ---- Player ----
    public const string LightAttack     = "LightAttack";
    public const string Dash            = "Dash";
    public const string Parry           = "Parry";
    public const string LightAura       = "LightAura";
    public const string Heal            = "Heal";
    public const string HealTarget      = "HealTarget";
    public const string BowAttack       = "BowAttack";
    public const string DamageProyectile = "DamageProyectile";
    public const string SleepProyectile  = "SleepProyectile";
    public const string AreaRayAttack   = "AreaRayAttack";
    public const string CounterAttack   = "CounterAttack";

    // ---- Enemies (non-boss) ----
    public const string BarbarianAttack = "BarbarianAttack";
    public const string ShielderAttack  = "ShielderAttack";
    public const string TrapBubble      = "TrapBubble";

    // ---- Boss: SpaceGod ----
    public const string Combo1          = "Combo1";
    public const string Combo2          = "Combo2";
    public const string HighAttack      = "HighAttack";
    public const string RotateToPlayer  = "RotateToPlayer";
    public const string SlamAttack      = "SlamAttack";
    public const string TeleportCombo   = "TeleportCombo";
    public const string TeleportCounter = "TeleportCounter";

    /*
     * Shared Names: the same runtime Name is used by more than one asset
     * (a regular ability and its boss variant, or Barbarian vs Mage counter).
     * They resolve to a single Id, so they only work when the sharing abilities
     * live in DIFFERENT AbilityHolders. Putting two of them in one holder is a
     * real collision (caught loudly by AbilityHolder.Awake) and must be avoided.
     */
    public const string FireAttack      = "FireAttack";   // FireAttack.asset + FireAttackBoss.asset
    public const string ElectroAttack   = "ElectroAttack";// ElectroAttack.asset + ElectroAttackBoss.asset
    public const string Teleport        = "Teleport";     // Teleport.asset + TeleportChangePhase.asset
    // "CounterAttack" above is shared by CounterAttackBarb.asset + MageCounterAttack.asset + CounterSpaceGod.asset
}