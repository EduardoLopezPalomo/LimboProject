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
 *
 * ENFORCEMENT (both directions): the menu Tools/Ciro/Validate Ability References
 * (and an automatic pass after every script reload) checks that every ability name
 * used by DATA - GameplayAbility asset Name, AttackDefinition.ability, behavior
 * graph "Ability" fields, prefab/scene UnityEvent string arguments - exists in this
 * registry, and that every const below is still referenced by at least one asset.
 * It logs errors, it never edits or blocks anything. See
 * Assets/GameplayAbilitySystem/Scripts/Editor/AbilityReferenceValidator.cs.
 *
 * NEVER rename a value below on its own: id = Animator.StringToHash(Name) and saved
 * player data stores those hashes, so a renamed value silently invalidates saves.
 * Value changes only ship with a save migration (P3.1).
 */
public static class AbilityIds
{
    // ---- Player ----
    public const string LightAttack     = "LightAttack";
    public const string Dash            = "Dash";           // *** SHARED: Dash.asset (player) + ArcherDash.asset (archers)
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
    // "Dash" above is shared by Dash.asset (player) + ArcherDash.asset (archers)

    /*
     * VERIFIED 11/8/2026 against the actual assets (AssetDatabase, t:GameplayAbility):
     *   30 ability assets -> 24 distinct Names, and the 24 consts above cover exactly those 24.
     *   The 5 shared Names above are the ONLY collisions, and every AbilityHolder in the project
     *   (38 prefabs) holds at most one asset per Name, so none of them collide in practice today.
     */

    // -------------------------------------------------------------------
    //  REGISTRY API
    //  Used by AbilityReferenceValidator (editor only) to cross-check every ability
    //  name that data references against the consts above. Built once at class load;
    //  IsKnownName does a single hash-set lookup and allocates nothing.
    //  Runtime code should keep using the consts, never this list.
    // -------------------------------------------------------------------

    /// <summary>Every registered ability runtime Name, exactly as the assets spell it.</summary>
    public static readonly System.Collections.Generic.IReadOnlyList<string> All = new string[]
    {
        LightAttack, Dash, Parry, LightAura, Heal, HealTarget, BowAttack,
        DamageProyectile, SleepProyectile, AreaRayAttack, CounterAttack,
        BarbarianAttack, ShielderAttack, TrapBubble,
        Combo1, Combo2, HighAttack, RotateToPlayer, SlamAttack, TeleportCombo, TeleportCounter,
        FireAttack, ElectroAttack, Teleport,
    };

    static readonly System.Collections.Generic.HashSet<string> AllSet =
        new System.Collections.Generic.HashSet<string>(All, System.StringComparer.Ordinal);

    /// <summary>
    /// True when name is exactly one of the registered ability runtime Names
    /// (ordinal / case-sensitive, like the Animator.StringToHash lookups).
    /// </summary>
    public static bool IsKnownName(string name)
    {
        return !string.IsNullOrEmpty(name) && AllSet.Contains(name);
    }
}