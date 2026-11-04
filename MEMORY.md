# MEMORY.md

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

