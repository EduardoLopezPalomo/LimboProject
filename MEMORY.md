# MEMORY.md

## Combat & Enemy System Review — Remediation Plan (11/4/2026)

A source-rooted review & remediation plan was authored in `SYSTEMS_REVIEW_PLAN.md`.
It grades issues P0–P3 and orders them into milestones A–F (cheapest / highest-safety first).
Plan's verdict: architecture is good and above average — **targeted polish, not a rewrite**.

### Execution status
- **Milestone A (Stabilize — P0.1 + P0.2): DONE ✅**
  - **P0.1** (out-of-range combo silently skips damage): the silent
    `Debug.LogWarning` + `return` was replaced with a loud `Debug.LogError` that names the
    owner, the combo index, and both list sizes. Applied in
    `Enemies/Combat/EnemyWeaponDamage.cs` (`NotParried`) and
    `Player/PlayerWeaponDamage.cs` (`DamageEnemy`).
  - **P0.2** (null components logged as a bare `Debug.Log` inside the hit path): replaced
    with a categorized `Debug.LogError` naming owner + target + which component is missing,
    in `Enemies/Combat/EnemyWeaponDamage.cs` (`NotParried`).
  - Purely additive error handling (no gameplay-logic change). Read back the edited
    regions and confirmed correctness. Not compiled via the Unity editor (no Unity CLI /
    headless run available here) — validate in-editor Play per the plan's DoD.
- **Milestone B (Make content safe — P1.1): DONE ✅**
  - **P1.1** (centralized ability-name registry + uniqueness validation in
    `AbilityHolder.Awake`). The GAS identifies abilities by `Name` hashed via
    `Animator.StringToHash(Name)` into `Id`; every caller references the **string** name
    (player specs `"Dash"`/`"Parry"`, AI `ActivateAbilityAction`/`ExecuteAttackSequenceAction`
    blackboard vars, and boss `AttackDefinition.ability`). Renaming an ability therefore
    silently broke every reference (hash no longer matched) → the attack did nothing with
    only a warning.
    - **New file** `GameplayAbilitySystem/Scripts/BaseAbility/AbilityIds.cs`: a
      static registry with `const string` for all **24 runtime Names** (extracted from the
      ability `.asset` `Name:` fields, not the filenames — they differ, e.g.
      `HealPlayer.asset` → `"Heal"`, `MageCounterAttack.asset` → `"CounterAttack"`).
    - **Edited `AbilityHolder.Awake()`**: new validation pass that logs a loud
      `Debug.LogError` on a **null** entry or two abilities that share an **Id** (i.e. share a
      Name) — previously silently dropped by the `!specs.ContainsKey(ability.Id)` guard.
    - **Edited `AbilityHolder.ActivateAbility(int, owner)`**: the "not found" path is now a
      `Debug.LogError` naming the owner + a hint that a renamed/deleted ability is to blame.
  - Purely additive. **Discovered/confirmed a latent data issue** (not fixed by this plan,
    see NOTES.md §P1.1-notes): the runtime Names `CounterAttack`, `FireAttack`, `ElectroAttack`,
    `Teleport` **and `Dash`** are each reused by more than one asset (e.g. Barbarian + Mage
    counters; `Dash.asset` player vs `ArcherDash.asset` archers), so they collide on Id — but
    only across **different** AbilityHolders, which is the only safe arrangement. Putting two
    shared-named abilities in one holder now errors loudly.
  - Public string API kept backward-compatible (registry is additive; call sites not yet
    migrated — that's P1.1's higher-effort follow-up).
  - **VERIFIED IN-EDITOR 11/8/2026** (Unity MCP, so the "not compiled" caveat from the previous
    agent no longer applies): project compiles with **0 errors / 0 warnings**; the registry was
    cross-checked against the live `AssetDatabase` (`t:GameplayAbility` → 30 assets → **24
    distinct Names**, exactly the 24 consts); all **38** prefabs carrying an `AbilityHolder` were
    scanned and contain **no duplicate Id and no null entry**, so the new `Awake` validation
    produces **zero false positives** on shipped content.
  - **Review fixes applied this session** (3 defects in the P1.1 code):
    1. `AbilityHolder` duplicate-Id message: the 2nd/3rd concatenated literals were missing the
       `$` prefix, so it printed the literal text `{firstDefinedByName}` — fixed.
    2. `Dash` was missing from the shared-Name documentation in `AbilityIds.cs` — added, plus a
       "VERIFIED" block recording the audit above.
    3. `AbilityHolder.Awake()` threw a bare `NullReferenceException` when the serialized
       `abilities` list is unassigned — now a clear `Debug.LogError` (and `specs` stays
       initialised so `Update()` cannot NRE).
- **Milestone C (Unify the hot path — P1.2): DONE ✅**
  - **New file** `Assets/Scripts/Combat/DamageResolver.cs` — a stateless `static class` that owns
    the duplicated hit pipeline once: `TryResolveDefender` (finds the defender's
    `GameplayEffectManager` + `AttributeSystem`, self or parent, loud error when missing),
    `TrySelectEffect` (combo-index guard across the normal **and** block lists + null-slot guard),
    `PlayShake` / `PlayRumble` (null-safe feedback), and `ApplyHit` / `ApplyResolved` returning a
    `DamageResolver.Outcome` enum (`Applied`, `MissingTargetComponents`, `InvalidComboIndex`,
    `NullEffect`). A `DamageResolver.ComboHit` parameter object carries the per-attacker flavour.
  - **Both weapon classes are now thin adapters** (public API untouched — `SetComboNumber`,
    `ActivateHitbox`, `DeactivateHitbox`, `GetHasHitPlayer`, `SetHasHitPlayer`, `IsCounterAttack`,
    `GetParried` all unchanged): `EnemyWeaponDamage.NotParried` and `PlayerWeaponDamage.DamageEnemy`
    now describe the hit and hand it to the resolver. `PlayerWeaponDamage.MakeComboCameraShake`
    became `GetShake(...)` (returns the data instead of firing it, and uses `TryGetValue` so a
    missing shake entry can no longer throw `KeyNotFoundException`).
  - This also **closes the P0.2 gap on the player side**: `PlayerWeaponDamage` used to call
    `effectmanager.ApplyEffect(...)` with **no** null check at all (latent NRE); it now goes
    through `TryResolveDefender`.
  - **Verified**: compiles 0 errors / 0 warnings, and a 15-case in-editor logic harness ran green
    over the guards (valid index, -1, ==count, the exact P0.1 cross-list case where the block list
    is shorter than the normal list, null slot, null list, a blocked hit with no block list,
    `OnBlocked` firing only when blocked and never on an invalid index, null-safe feedback helpers,
    unresolvable defender). See NOTES.md
    §P1.2-notes for the harness and the one intentional behaviour delta.
- **Remaining (in order):**
  - **D — Loose the coupling:** P1.3 (singleton decoupling, one manager at a time) +
    P2.1 (centralized layer-name resolution).
  - **E — Polish:** P2.2 (time-scale-independent cooldowns), P2.3 (`[SerializeField]` /
    hide mutable state), P2.4 (move tunables out of constructors).
  - **F — Housekeeping:** P3.1 (refactor-rename typos), P3.2 (balance data + versioning),
    P3.3 (behavior-graph hygiene).
  - **P1.1 follow-up (after B):** migrate the magic-string call sites (player specs + AI
    Action nodes + boss data) to `AbilityIds.*` and, later, make the inspector `Name` derive
    from the registry (compile-time Ids) so renames can no longer silently break.

### Notes for future agents
- Plan lives in `SYSTEMS_REVIEW_PLAN.md`. **Do NOT commit** (see `AGENTS.md`).
- New logs use C# interpolated strings (`$"..."`) — Unity/Roslyn supported, no extra using.
- DoD (from plan §6): Unity compiles clean in-editor; Play-mode verify every scenario; no
  new hot-path noise; keep public API backward-compatible; update MEMORY/NOTES; leave
  uncommitted.

---

## Combat System Analysis (11/3/2026)
A complete analysis of the combat system has been performed. The full report is in `NOTES.md`.

### Key Files Referenced
- All combat analysis is documented in `NOTES.md`
- Core system: `Assets/GameplayAbilitySystem/Scripts/`
- Player combat: `Assets/Scripts/Player/`, `Assets/Scripts/Combat/`
- Enemy combat: `Assets/Scripts/Enemies/Combat/`, `Assets/Scripts/Enemies/Boss/`
- UI: `Assets/Scripts/UI/PlayerHealth.cs`, etc.

---

## Combat System Strengths & Weaknesses (11/4/2026)

A good/bad characteristic review of the combat system was performed by reading the
actual source. Findings:

### STRENGTHS
1. Clean 3-layer architecture: GAS core (config) -> combat logic -> UI/feedback. Good separation of concerns.
2. Data-driven: `GameplayAbility` is a ScriptableObject definition; `GameplayEffect` is data
   (TargetAttribute/Value/Modifier/EffectTime). New abilities = new asset, not new code.
3. Unreal-GAS-like Spec/Asset split: ScriptableObject (authorable) vs GameplayAbilitySpec (runtime,
   cooldown state). Clean and scalable.
4. Good "game feel" in combos: input buffering (`bufferedInput`), animation-event combo windows
   (`ComboWindowOpen/Close`), and auto-aim/adjust (`AttackAssist`) in `LightAttackSpec`.
5. Soulslike defense FSM (`DefenseState`): perfect-parry window -> block -> guard break -> recovery,
   with time-slow + shake + vibration + SFX/VFX feedback. Strongjuicy feedback layer.
6. Robust damage pipeline: i-frames (MakeInvincible), hit de-dup (`AlreadyHitPlayer`), per-combo
   effects, counter handling, camera shake per combo step.
7. Boss combat: `AttackDirector` picks weighted attack sequences with a repeat penalty (less predictable);
   `PhaseController` drives multi-phase progression.
8. Encounter balance: `EnemiesGroupContoller` token system caps concurrent enemies; `Slot`/`SlotUpdate`
   position enemies around the player.
9. Object pooling (`PoolManager`) and recoverable-attribute regen (`AttributeSystem._recoveredAttributes`).
10. Excellent documentation (NOTES.md) with architecture/data-flow diagrams; AGENTS.md workflow.

### WEAKNESSES / RISKS
1. Typos in public identifiers (`isPasive`, `SetInputAviable`, `Proyectile`, `Trayectory`) -> hurts
   maintainability.
2. Inconsistent serialization/visibility: public fields without `[SerializeField]` (`GameplayAbility`
   Name/Cooldown/EffectsToSelf...); public mutable `ActiveSpecs`. No data protection.
3. Tunables hardcoded into constructors instead of config assets (`LightAttackSpec` takes 8 float
   params) -> fragile, hard to balance without code changes.
4. Global singletons everywhere (`CameraShakeManager.instance`, `TimeEffectsManager`,
   `VibrationManager.Instance`) -> tight coupling, hard to test / reuse.
5. Timing bugs: combo uses `Time.time` (paused under slow-mo) while cooldowns use frame-counted
   `Time.deltaTime` -> inconsistent, frame-rate dependent cooldowns. Should use unscaled accumulator.
6. Fragile list indexing: out-of-range `ComboNumber` silently skips damage with a warning; a
   designer who doesn't fill the effects list makes attacks do nothing.
7. Combat state split across many booleans + Animator bools/triggers -> tight coupling to animation
   events; breaking an event breaks combat.
8. `PlayerWeaponDamage` and `EnemyWeaponDamage` duplicate the same combo/counter/shake logic
   (violates DRY) -> should be shared.
9. Hardcoded layer names (`LayerMask.GetMask("EnemyLayer")`, `"Player"`) -> breaks if layers rename.
10. No centralized damage/balance data; tuning only via inspector edits, hard to version/test.
11. Weak null-safety: missing components reported as `Debug.Log` inside hit paths (in `EnemyWeaponDamage`).

See `NOTES.md` for the full architectural report.

---

## Enemy System Analysis (11/4/2026)

A full walkthrough of how enemies work was performed by reading the actual source.
The enemy system is NOT hand-written movement code — it is driven by **Unity Behavior
Graph** AI (the `BehaviorGraphAgent`) plus a set of C# **Action/Condition** nodes that
talk to the combat / GAS systems.

### Building Blocks (layered)

**A. AI driver — Unity Behavior Graph**
- Each enemy has a `BehaviorGraphAgent` + a blackboard (`BlackboardReference`) with custom
  enum vars: `BarbarianState`, `AttackingState`, `GroupState`, `BossPhase`, plus booleans
  like `ChangePhase` / `HasBeenAlerted`.
- 6 enemy graphs live in `Assets/Enemies/BehaviorTrees/`:
  `BarbarianGraph.asset`, `ArcherGraph.asset`, `MageGraph.asset`,
  `ShielderGraph.asset`, `HealerGraph.asset`, `SpaceGodGraph.asset`.
- AI logic is assembled from nodes in `Assets/Enemies/Scripts/` (Action + Condition classes):
  - `ActivateAbilityAction`, `ExecuteAttackSequenceAction`, `ComboControllerAction`,
    `WeaponControllAction`, `RequestTokenAction`, `StartTokenTimerAction`, `FreeSlotAction`,
    `GoingToSlotsAction`, `RotateToPlayerAction`, `RotateAlongPlayerAction`, `ScapeFromPlayerAction`,
    `CheckBossPhaseAction`, `ActivateHudAction`, `ApplyEffectAction`, `PlaySoundAction`, `SpawnVfxAction`,
    etc.
  - Conditions: `PlayerDetectedCondition`, `PlayerIsFarCondition`, `HasHitPlayerCondition`,
    `RotateHasTargetCondition`, `TimeToSlotHasPassCondition`, `CheckDistanceVectorCondition`.

**B. AI <-> Combat bridge**
- `Assets/Enemies/EnemyGraphBridge.cs` — the central bridge. Sets blackboard states
  (`SetHurt/SetVulnerable/SetTrap/SetDetection/SetChangePhase/SetDeath`), fires the static
  `OnDeath(GameObject)` event, and `DisableAllExceptRenderer()` for the death animation.

**C. Movement & positioning (helpers used by AI actions)**
- `GoingToPlayer.cs` — resolves slots via `SlotsManager`, exposes `GetSlotPosition/GetRandomSlot`.
- `RotateAlongPlayer.cs` — orbital strafing (orbit around player with wobble + neighbor slowdown).
- `RotateToPlayer.cs` / `RotateToTarget.cs` — facing turn for ranged/aimed attacks.
- `EscapeFromPlayer.cs` — retreat to escape distance.
- `ShieldMaskController.cs` — toggles shield mesh layer (anim layer weight 3).
- `EnemyPlayerDetection.cs` — trigger-collider + distance based detection flag.
- `TokenTimer.cs` — per-enemy cooldown for the token system.

**D. Combat (enemy as attacker)**
- `Assets/Scripts/Enemies/Combat/` — `EnemyWeaponController` (extends `WeaponController`),
  `EnemyWeaponDamage` (hitbox → `DefenseState.ResolveHit()` → PerfectParry/Block/FullDamage),
  `EnemyShieldController`, `DeathCheck`, plus projectiles (`ArrowMovement`, `MoveFireBall`,
  `ExplosionDamage`, `LightingDamage`, `PlayerDamageProyectile`).
- Damage is applied through GAS: `GameplayEffectManager.ApplyEffect` → `AttributeSystem`.
- Combo numbering + counter flag live on `EnemyWeaponDamage`.

**E. Boss system**
- `Enemies/Boss/PhaseController.cs` — 3 phases by health ratio (66% → P2, 33% → P3),
  listens to `EnemyHealthResponse.OnEnemyHurt`, calls `EnemyGraphBridge.SetChangePhase()`.
- `Enemies/Boss/AttackDirector.cs` — weighted-random sequence pick with a repeat penalty
  (last-used gets 0.5x weight) so boss patterns are less predictable.
- Data (ScriptableObjects in `Assets/GameplayAbilitySystem/Scripts/AttackDefinitions/`):
  `AttackLibrary` (per-phase arrays) → `AttackSequences` (ordered steps + weight) →
  `AttackDefinition` (ability name + `AttackKind`: ComboPhysical/IndividualPhysical/Magical/Teleport).
- `BossConversation.cs` — boss intro dialogue.

**F. Encounter / group management**
- `Assets/Scripts/Enemies/EnemiesGroupContoller.cs` — **token system** (`maxTokens` default 2)
  caps how many enemies can act concurrently; `AlertGroup` starts a battle, `EnemyHasDie`/
  `ReturnToken` manage spawn pressure, `CheckBattleOver` → `BattleOver` (ends music + hides HUD).
- `Slot.cs` / `SlotUpdate.cs` + `SlotsManager` — position enemies around the player
  (`SlotType`: Close/Mid/Far), refresh positions periodically.

### Data / events that tie it together
- `AbilityHolder.ActivateAbility(name, owner)` is how AI actions trigger GAS abilities.
- Static events: `EnemyGraphBridge.OnDeath`, `EnemiesGroupContoller.OnBattleStateChanged`,
  `OnHideEnemyHud`. `EnemyHealthResponse.OnEnemyHurt` drives phase changes.

See `NOTES.md` §5 (Enemy Combat), §8 (Boss), §9 (Enemy Group) for the detailed diagrams.

