# MEMORY.md

## Weapon-Owner Audit + Combat Seam Hardening (11/8/2026)

### 1. Full prefab audit (authoritative, no truncation)
Scanned **118 prefabs** in the gameplay folders (`Assets/Characters`, `Assets/Enemies`, `Assets/NPC`,
`Assets/Weapon`, `Assets/Prefabs/Combat`) with `PrefabUtility.LoadPrefabContents` +
`GetComponentsInChildren<MonoBehaviour>(true)`, reading `owner` / `gameplayEffects` /
`gameplayEffectsBlock` / `counterEffect` / `GetParriedEffect` through `SerializedObject`.
Result: **23 weapon-damage components**, and only **3 problems** — the earlier "several enemies have
broken owners" note was **wrong** (it came from a truncated log).

| Owner state | Who |
|---|---|
| `owner = ROOT` OK | Space_time_god, Barbarian_fem1/2, Barbarian_masc1/2, Shield_fem1/2, Shield_masc1, Barbarian_slayer_03, Barbarian_troll_01, Shield_ork_01, Shield_pig_02/03, Player (PlayerFork) |
| `owner = NULL` **FIXED** | `Assets/Characters/Humans/Enemies/Shields/Shield_masc2/Shield_masc2.prefab` -> `Human_Polearm` |
| `owner = NULL` **FIXED** | `Assets/Enemies/Shielder.prefab` -> `Human_Polearm` |
| `owner = NULL` by design OK | the 6 weapon **templates** in `Assets/Weapon/` (owner is bound per character-prefab instance) |

Both fixes were written as a **prefab-instance override** on the nested weapon (`owner -> root GO`),
verified by re-loading from disk: `owner == root`, the root carries `GameplayEffectManager` +
`AttributeSystem`, the weapon carries `CinemachineImpulseSource`.
Side effect on `Shielder.prefab` (harmless, Unity re-serialisation): fields that had never been
written to disk appeared (`IsShooting`, `WeaponDamage`, `speed`, `wobble*`, `controller`) and the
orphaned `rotationSpeedALongPlayer` (left over from a field rename, so its value was being **lost**)
became `rotationSpeedAlongPlayer` and now actually binds.

### 2. DECISION taken (user): `Assets/Enemies/Barbarian.prefab` is OBSOLETE - leave it alone
Its `EnemyWeaponController.weapon` = `Dagger`, and that Dagger carries **`PlayerWeaponDamage`**
(the *player's* component) with `owner = NULL` and `fx = 0`, so `WeaponDamage == null` there and that
enemy cannot damage the player. User confirmed the prefab is superseded by
`Assets/Characters/Humans/Enemies/Barbarians/*` -> **do not touch the asset**; the code is hardened
against exactly this shape instead. The same shape exists legitimately on `Shielder`'s second
`EnemyWeaponController` (its `Human_Shield`) and on `Shield_masc2`'s `EnemyShieldController`.

### 3. Code changes (verified: compiles 0 errors / 5 warnings = unchanged baseline)
- **`Combat/DamageResolver.cs`** - new `SingleHit` parameter object + `ApplySingle(...)` so one-off
  effects (perfect-parry stun, counter debuff, later explosion/lightning) run the same
  resolve -> feedback -> apply pipeline instead of raw `ApplyEffect` calls.
- **`Enemies/Combat/EnemyWeaponDamage.cs`**
  - `Awake` recovers a missing `owner` from the parent hierarchy (needs GEM + AttributeSystem on the
    **same** GameObject, warns so the asset still gets fixed); if it cannot, it `LogError`s and
    **disables the weapon's colliders** (`isUnusable`) so an unconfigured enemy never half-attacks.
  - **Hit-consumption order fixed**: `AlreadyHitPlayer` is no longer set in `OnTriggerEnter` before
    the invincibility test - one invincible touch no longer burns the whole swing (no damage, no
    block, no counter). The flags are now set only once the defender is known to be evaluable.
  - Missing `DefenseState` on a Player-layer collider = loud `LogError` + drop (was a bare NRE).
  - Parry / counter / blocked paths route through `DamageResolver` (`ApplySingle` / `PlayShake`);
    the direct `CameraShakeManager.instance.CameraShake(...)` call is gone.
- **`Player/PlayerWeaponDamage.cs`** - same `owner` recovery + `isUnusable`; the `EnemyLayer` lookup
  is validated (a missing layer used to make `1 << NameToLayer(...)` an undefined shift); the counter
  branch was extracted into `CanCounter(...)` + `Counter(...)`, honours the **bool** returned by
  `BlackboardReference.GetVariableValue` (a missing `AttackingState` no longer reads as a real state)
  and no longer NREs when a hit enemy has no `BehaviorGraphAgent`/`RotateAlongPlayer` - which also
  aborted the rest of that sweep step. Counter feedback goes through `ApplySingle` + `PlayShake`.
- **`Combat/WeaponController.cs`** - `Awake` / `EquipWeapon` / `UnequipWeapon` guard a null `weapon`,
  `RightHandSlot`, `HipSlot` (all of them used to throw straight out of AI actions).
- **`Enemies/Combat/EnemyWeaponController.cs`** - `Awake` reports a missing weapon / missing
  `EnemyWeaponDamage` once; `SetComboNumber` and `CounterAttack` are guarded by `HasDamage()` instead
  of NRE-ing on weapons with no damage component (obsolete Barbarian, Shielder's shield).
- **`Player/DefenseState.cs`** - `ResolveHit()` split into **`EvaluateHit()`** (pure decision, only
  flips `Parry -> Idle`) and **`PlayHitFeedback(result)`** (VFX/SFX, shake via `DamageResolver.PlayShake`,
  slow-mo, rumble via `PlayRumble`, `NotifyPerfectParry` / `NotifyGuardBreak`). `ResolveHit()` stays as
  evaluate+feedback in the original order, so `ExplosionDamage` / `LightingDamage` keep working.
  Parry haptics became the named `ParryRumble` constant (same 1.0 / 1.0 / 0.2 values).
- **`Player/MakeInvincible.cs`** - `OnEnable` / `OnDisable` now clear the flag (safety net for the
  manual `SetInvincible(true)` / `SetInvincible(false)` pairing - see decision 5.2).
- Removed the now write-only cached `OwnEffectManager` / `OwnAttributeSystem` (and the player's
  `effectManager` / `attributeSystem`) fields; the check is done with locals in `Awake`, which keeps
  the fail-fast behaviour without tripping **CS0414** once the resolver applies the effects.

### 4. Verification
- Force refresh + compile: **0 errors**; console clean after the domain reload.
- `AbilityReferenceValidator.Validate()` re-run by reflection: **"0 problem(s) and 5 warning(s)"** -
  exactly the pre-existing baseline (the 5 duplicate-ability-Name notices). No new warnings.
- Reflection confirms the new code is in the loaded assembly (`DamageResolver+SingleHit` exists,
  `DefenseState.EvaluateHit` / `PlayHitFeedback` / `ResolveHit` all resolve after the last edit).
- `MakeInvincible` caveat: an Edit-mode probe of the safety net is **inconclusive on purpose** - Unity
  does not run `OnEnable` / `OnDisable` on a dynamically created GameObject outside Play mode, so the
  self-clearing has to be confirmed in Play mode (knock the player, then disable/enable it or die).
- **RE-VERIFIED 11/8/2026 after the safety-net edit** (forced `refresh_unity` + domain reload):
  `read_console` errors = **0 entries**, and `AbilityReferenceValidator.Validate()` printed
  **"0 problem(s) and 5 warning(s)"** over 30 ability assets / 12 behavior graphs / 428 prefab-scene
  files - exactly the baseline. Beware: `Validate()` **returns problems + warnings summed** (it returned
  `5`), so the bare number looks alarming; only the log summary line splits them.
  Reflection on the loaded `Assembly-CSharp` also confirms `MakeInvincible.OnEnable` / `OnDisable` /
  `SetInvincible` / `IsInvincible`, `DamageResolver.ApplySingle` / `ApplyHit` / `PlayShake` /
  `PlayRumble` + `DamageResolver+SingleHit`, and that `ExplosionDamage` / `LightingDamage` still expose
  every serialized field (`DamageTickRate`, `CameraShakeData`, `DamageEffect`, `BlockedEffect`,
  `ObjectToDamage`) - so renaming nothing there is a hard requirement, not a style preference.
### 5. Still open (DECIDE before implementing)
1. **Rumble parity**: enemy attacks still play no controller rumble (player attacks do). The plumbing
   exists (`ComboHit.Haptics`), it is a one-line change - needs a design yes/no.
2. **Invincibility model - DECIDED (user): keep the manual pair + auto-clear safety net (DONE).**
   `Player/MakeInvincible.cs` clears the flag in `OnEnable` and `OnDisable`, so it can never outlive
   the state change that set it. Timed i-frames were rejected as too invasive.
   - **Correction**: the earlier "only `KnockBack.cs` uses it" note was **wrong**. Every writer of
     `SetInvincible` is: `Combat/KnockBack.cs:60/89` (Player, cleared at the end of a
     `KnockBackOffsetTime` coroutine), `NewAbilities/DashSpec.cs:63/151` and
     `NewAbilities/DamageProyectileSpec.cs:70/155` (both Player), and
     `NewAbilities/ArcherDashSpec.cs:40/100` - **the archer's dodge i-frames, so `MakeInvincible` is
     NOT player-only**. All four take it from `owner.GetComponent<MakeInvincible>()`, i.e. the
     character root, and no prefab/scene binds `SetInvincible` through a persistent UnityEvent
     (grepped all of `Assets`), so nothing expects the flag to survive a disable.
   - **Exact leak this fixes**: `KnockBack.Timer` raises the flag and only lowers it after a
     `KnockBackOffsetTime`-long `yield` loop; disabling the character mid-knockback (death, respawn,
     scene / god-space transition) kills the coroutine and the player came back **unhittable**.
   - **Found, NOT implemented (DECIDE)**: the same interrupted-dash/throw path also leaves
     `personcontroller.SetInputAviable(false)` behind, because `EndDash()` is what restores input and
     it never runs. `MakeInvincible` no longer sticks, but the player can still be left **unable to
     move**. That belongs to `PersonController` / the ability layer, not to `MakeInvincible`.
3. **Mitigation/armour on weapon hits**: weapon damage currently applies a fixed GameplayEffect;
   there is no armour/mitigation path in `DamageResolver`. Needs a design yes/no.
4. Remaining backlog: migrate `Enemies/Combat/ExplosionDamage.cs` + `Enemies/Combat/LightingDamage.cs`
   to `DamageResolver` (both still call `CameraShakeManager.instance.CameraShake` + raw `ApplyEffect`),
   then route every remaining shake/rumble call through the resolver.
   NOTE (corrected 11/8/2026): there are **no** VFX-only duplicate scripts. Each class exists exactly once,
   in `Enemies/Combat/`, and both are live combat code. The previous warning about
   `Assets/Scripts/Enemies/ExplosionDamage.cs` / `Assets/Scripts/Lightning/LightingDamage.cs` was wrong —
   those paths do not exist and never existed in git history. See NOTES.md §"Duplicate script names:
   FALSE ALARM".

### 6. Gotchas learned this session (save yourself the time)
- `script_apply_edits` **anchors are regex**: escape `(` `)` and `[`, or the anchor silently fails -
  `if (!isActive) return;` has to be written `if \(!isActive\) return;`.
- A multi-edit call that fails with `overlap` can still **delete** a line it had already matched (it
  ate `[HideInInspector] public EnemyWeaponDamage WeaponDamage;`). Batch conservatively and re-read the
  file after any failure.
- Several combat `.cs` files have **mixed CRLF/LF** endings, so exact multi-line literal replacement
  fails; use `script_apply_edits` (method/anchor ops) or PowerShell `-replace` over `ReadAllText`.
- MCP `execute_code` runs **C#6 / CodeDom**: no local functions, no target-typed `new()`, fully
  qualify editor types. Scanning ~400 prefabs in one call times the bridge out - scope
  `AssetDatabase.FindAssets("t:Prefab", folders)` to the gameplay folders and write the report to a
  file instead of returning it.
- The Unity console is cleared on every domain reload. Re-run `AbilityReferenceValidator.Validate()`
  (static, `Assembly-CSharp-Editor`) by reflection to re-check the baseline instead of trusting an
  empty console.
- `AbilityReferenceValidator.Validate()` **returns problems + warnings summed** (it returned `5` while the
  state was the clean `0 problems / 5 warnings`). Read the log summary line, never the bare return value.
- The Unity bridge can stop answering for **more than the client's 60s MCP timeout** right after a forced
  `refresh_unity` (domain reload). The editor is not dead - wait, then probe cheaply with
  `manage_editor(telemetry/ ping)` before retrying `read_console` / `execute_code`. Raise `timeout` in
  `cline_mcp_settings.json` if it keeps happening.
- Pass **single-backslash, exact** absolute paths to the editor tool. A doubled segment
  (`c:\Users\super\\dev\\LimboProject\MEMORY.md`) created **only** a stray copy outside the project instead of touching the intended file - it landed at `C:\Users\super\dev\LimboProject\MEMORY.md`; it reported "File created" with no
  diff, which is the tell. Verify with `git status` / `Test-Path` after any edit that says "created".





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


---

## Ability-Name Drift Guard (11/8/2026)

`AbilityIds` is now the **single source of truth** for ability names, and an Editor validator
watches every place a name can be written by hand so drift can't ship silently.

### Registry API (additive — no const was touched)
`GameplayAbilitySystem/Scripts/BaseAbility/AbilityIds.cs`
- `AbilityIds.All` — `IReadOnlyList<string>` of all 24 registered names.
- `AbilityIds.IsKnownName(string)` — **ordinal / case-sensitive** (the `HashSet` is built with
  `StringComparer.Ordinal`, mirroring the `Animator.StringToHash` lookups). Re-verified 11/8/2026.
- There is **no** `AbilityIds.IsKnownId(int)` anywhere in the project — the class exposes only
  `All` and `IsKnownName`. (Earlier note here claimed otherwise; corrected.)
- Verified by reflection: **24 consts ↔ 24 `All` entries, 1:1, no duplicates**; every registered
  name has a `GameplayAbility` asset and every asset `Name` is registered.

### New validator
`GameplayAbilitySystem/Scripts/Editor/AbilityReferenceValidator.cs`
- Menu: **Tools/Ciro/Validate Ability References**; also runs automatically **once per Editor
  session** (cold pass ~15s from asset loading, warm pass ~1.5s — hence the session gate, not
  every reload). Silent when clean, so it cannot spam the console.
- Checks: registry ↔ `GameplayAbility` assets, `AttackDefinition.ability`, behavior-graph
  `Ability` blackboard strings, `ActivateAbility`/`CancelAbility` **UnityEvent string bindings** in
  prefabs/scenes, unused registry entries, and duplicate asset names. The `.cs` pass only looks for
  `AbilityIds.<Name>` markers to mark a name as *used* — a plain `"Dash"` literal passed straight to
  `ActivateAbility()` in code is **not** detected (verified 11/8/2026 by reading the validator).
- **Last run (11/8/2026): 0 problems / 5 warnings** over 30 ability assets, 24 names, 8 attack
  definitions, 12 behavior graphs (34 `Ability` fields, **0 skipped**), 428 prefab/scene files
  (4 event bindings, 0 non-string bindings ignored).

### Call-site migration (Phase 2)
- `Scripts/Combat/KnockBack.cs` (6 uses), `NewAbilities/DashSpec.cs` (1),
  `NewAbilities/LightAttackSpec.cs` (1) now reference `AbilityIds.*`.
- Grep across our code (excluding `DownloadPackages`, `Synty`, `_Recovery`) for
  `HasAbility(" / IsAbilityActive(" / ActivateAbility(" / CancelAbility("` → **0 raw literals left**.

### Duplicate-name policy (important)
Same `Name` in more than one asset is **legal** as long as the assets live in **different
`AbilityHolder`s** — the runtime resolves per holder. So duplicates are a **warning**, not an error.
`AbilityHolder.Awake()` still hard-errors if a *single* holder holds two abilities with the same Id.
The 5 known shared names (all across different holders): `Dash`, `CounterAttack`, `ElectroAttack`,
`FireAttack`, `Teleport`.
- **Audited 11/8/2026**: walked every prefab/scene (excluding the third-party folders above), found
  **37 `AbilityHolder` components** — **no holder contains the same ability name twice**. Each shared
  name sits in exactly one slot per holder (e.g. `Space_time_god` holds `ElectroAttackBoss`, the
  magicians hold `ElectroAttack`). So all 5 warnings are currently **safe**, not drift.

### Hazard to remember
`Id = Animator.StringToHash(Name)` and that Id can end up in saved data. Renaming an ability is a
**migration**, not a find/replace: the validator tells you every place to update, but old saves and
external references need an explicit decision. See `NOTES.md` §Ability-Name Drift Guard for the
YAML parsing details that the validator depends on.

Temp helper scripts from this work (`_apply_p1.ps1`, `_apply_p1b.ps1`, `_vfy.ps1`) were deleted.
**(Update 11/8/2026:** this line is obsolete — the user has since committed; `dev` HEAD is
`960439b7 "Magic strings miogration to ability registry"`. See the Review Verification Pass section
below.**)**


## Review Verification Pass (11/8/2026) — read-only audit of the P0/P1/P2 claims + console triage

Nothing was changed in code this pass; this records what was **actually verified** in the editor, what
the previous notes got wrong, and the defects the audit surfaced.

### Console triage: the 5 validator warnings are NOT drift
Re-ran `Tools/Ciro/Validate Ability References` → **0 problems / 5 warnings**, identical to the record
above (30 assets, 24 names, 8 attack defs, 12 graphs / 34 fields / 0 skipped, 428 prefab+scene files,
4 event bindings, 0 non-string bindings). The 5 warnings are the *duplicate asset name* advisory for
`Dash`, `CounterAttack`, `ElectroAttack`, `FireAttack`, `Teleport`. They are **expected and safe**:
each is a per-model skin of one ability (e.g. `Player/Dash{M,F,MA,FA}.asset`,
`Shielder/Human/{CounterAttackShield,MagicianCounterAttack}.asset`, `Archer/FireArrow*`) and
`AbilityHolder.NameToIds` collapses them to one `Id`. Verified holder by holder that **no holder
contains two assets resolving to the same Id** — including the 5-skin `Player` holder. A real
collision would log `ERROR: Holder X has two abilities called 'Y'` from `AbilityHolder.Awake:62`; the
console has **zero** such errors. Also verified live: every holder's serialized names in the loaded
`Forest` scene resolve, and every behavior-graph `Ability` string resolves.

### Scene-holder question answered (was open)
`Forest` has **45** `AbilityHolder` components — all prefab instances, all with ≥1 serialized ability
(`Player` 4, Magician 4, Archer/Barbarian 2, Shield/Healer 1). **No empty holder**, so no
`WARNING: Holder … has no abilities`. Side effect worth knowing: `AbilityHolder.Awake` logs
unconditionally → **45 console lines every time Forest starts**.

### Weapon `owner` audit — the real finding
Technique that works (the naive one does not): `PrefabUtility.LoadPrefabContents` +
`SerializedObject.FindProperty("owner").objectReferenceValue` only sees the **prefab's own** value,
which is null on every nested weapon — parents override it. You must `Object.Instantiate(prefab)` and
read the *instance*. Result across every prefab that has a weapon damage component or nests a weapon
GUID:

- 7 enemy weapon roots (`Assets/Enemies/*.prefab`) and 3 player roots wire `owner` ✔.
- 11 standalone `Assets/Weapon/*.prefab` are null **by design** — filled by the parent character
  (`WeaponAIBase.cs:95/106` for AI, `PlayerWeaponController` for the player).
- **3 wired-but-broken content paths (NEW, pre-existing, still open):**
  `Assets/Enemies/Shielder.prefab` → `Human_Polearm` (EnemyWeaponDamage),
  `Assets/Enemies/Barbarian.prefab` → stray `Dagger` (**PlayerWeaponDamage mounted on an enemy**),
  `Assets/Characters/Humans/Enemies/Shields/Shield_masc2/Shield_masc2.prefab` → `Human_Polearm`.
  `owner` is null *and* the weapon root is active, so `EnemyWeaponDamage.Awake` /
  `PlayerWeaponDamage.Awake` (`:37-38` / `:149-150`) throw **NRE on spawn** and leave the manager refs
  null → the perfect-parry path (`GetParried`) NREs too. Confirmed pre-existing: the same
  `owner.GetComponent` lines exist in `43d790b8^`, so **not** a P1.2 regression.
- `Assets/Enemies/AI/Shielder AI.prefab` → `Dagger` is null **but the weapon is disabled**, so no
  `Awake` runs and nothing throws. Same for the disabled weapons on Barbarian/Brute/Magician.

### Other verified gaps (ranked)
- **P1.1 step 3 (migrate call sites to the registry) is NOT done**: there is **no `DamageEnemyBase.cs`**
  in the project — the checklist named a file that does not exist — and the *only* first-party file that
  references `AbilityIds.` from code is `Scripts/Combat/KnockBack.cs` (6 uses). Everything else is still a
  raw string: `AbilityHolder.ActivateAbility/HasAbility/GetAbility/CancelAbility/IsAbilityActive(string)`
  (`AbilityHolder.cs:101,215,230,235,250`), `PlayerAbilityController.ActivateAbility(string)` (`:8`),
  `ActivateAbilityAction.cs:12` (`BlackboardVariable<string> Ability`),
  `ExecuteAttackSequenceAction.cs:33,59` (`string key = steps[i].ability`), plus `AttackStep.ability` and
  `AttackDefinition.ability`. So P1.1 = registry + editor validator (steps 1-2), which matches the older
  note at the top of this file ("call sites not yet migrated"). Scope note for step 3: Unity Behaviour
  graphs serialise **string** blackboard values, so switching those nodes to an enum means re-authoring
  every graph (or a custom drawer + migration).
- **Empty effect lists are NOT silent (correction to earlier notes)**: a weapon with 0 elements in
  `gameplayEffects` already fails loudly on **both** sides, because `TrySelectEffect` treats
  `ComboIndex 0 >= normalCount 0` as out of range and `LogError`s the owner plus both list sizes
  (`DamageResolver.cs:184-191`). `Dagger` and `SM_Wep_AncientWarrior_01` therefore spam errors and deal
  no damage — a content bug to fix, but no new logging work is needed. There is no `Outcome.NoEffects`
  and no `ComboHit.HasDefense` (and no `DamageData.defense` armor concept at all): earlier notes here
  invented them. The real defender-side gate is `DefenseState.ResolveHit()`.
- **Unguarded spots left in the two adapters**: `EnemyWeaponDamage.cs:87` still calls
  `CameraShakeManager.instance.CameraShake(...)` **directly** (no null guard, bypasses
  `DamageResolver.PlayShake`); `:91` pushes `counterEffect` straight into `defender.Effects`, so a null
  counter effect escapes the resolver's null-slot check; `:77` (`GetParried`) uses the `Awake`-cached
  attacker managers — precisely what breaks when `owner` is null; `:59` and `PlayerWeaponDamage.cs:203`
  dereference `defense` / `agent` / `rotateAlongPlayer` unchecked. The enemy path also has **no rumble
  at all** while the player path does, so hit feedback is asymmetric.
- **P1.2 covers only 2 of the 4 damage sources (NEW)**:
  `Enemies/Combat/ExplosionDamage.cs` and `Enemies/Combat/LightingDamage.cs` still carry the
  *pre-resolver* copy — the original `Debug.Log("effectmanager, attributesystem or makeInvincible is
  null")` (`:52` / `:39`), unguarded `CameraShakeManager.instance` (`:84`,`:88` / `:54`,`:58`) and
  `defense.ResolveHit()` on an unchecked `defense` (`:77` / `:47`). Migrating them is the cheapest way
  to actually finish P0.2 + P1.2.
- **Partial adoption**: the counter-attack / parry / got-blocked branches still call
  `PlayShake` / `PlayRumble` / `ApplyEffect` directly instead of `ApplyHit`, so "every hit goes through
  one resolver" only holds for the normal-damage path.
- **Hardcoded layers (P2.1 not started)**: `EnemyWeaponDamage.cs:43` `"Player"`,
  `PlayerWeaponDamage.cs:146` `"EnemyLayer"`, `LightAttackSpec.cs:153` and `RotateAlongPlayer.cs:40`
  `"EnemyLayer"`, `MoveFireBall.cs:37` `"Floor"`, plus the `Team` strings in `DamageEnemy*`. There is
  **no** `Layers` / `LayerIds` helper class in the project.
- **P2.3 / P2.4 not started**: `GameplayAbility` still exposes `Name/Cooldown/ActiveTime/isPasive/
  equipable/EffectsToSelf` as public fields, `AbilityHolder.ActiveSpecs` is a public field and its
  `Update` calls `Time.deltaTime` directly (`AbilityHolder.cs:285`) — no injectable clock.
  `LightAttackSpec` still has the 8-parameter constructor.
- **P3.1 not started**: **53** occurrences of `Proyectile` / `Trayectory` / `isPasive` /
  `SetInputAviable` across **19** first-party files (third-party folders excluded; worst offender
  `DamageProyectileSpec.cs`).
- **No test infrastructure**: zero first-party `.asmdef` files, so the P1.2 harness cannot be re-run —
  the plan's Definition-of-Done test items are not actually met.
- **No P0.3 in the original plan (correction)**: there is no `NoDefense` outcome and no `PlayDamage`
  method anywhere — the earlier note about them was wrong. `HitResult` is only
  `{ PerfectParry, Blocked, FullDamage }` (`DefenseState.cs:5`). The real open seams are:
  `EnemyWeaponDamage.OnTriggerEnter` sets `AlreadyHitPlayer = true` before testing invincibility
  (`:43-47`, so a swing that touches during i-frames is consumed), `MakeInvincible` is a timer-less bool
  paired by hand in 4 specs (`DashSpec.cs:63/151`, `ArcherDashSpec.cs:40/100`,
  `DamageProyectileSpec.cs:70/155`, `KnockBack.cs:60/89`), and `DefenseState.ResolveHit()` mutates state
  + fires feedback from inside a query (`:41-63`).

### Git state correction
The working tree is **clean** — the user committed the work. `dev` HEAD is
`960439b7 "Magic strings miogration to ability registry"` (pushed to `origin/dev`), preceded by
`43d790b8 "p1.1 review and p1.2 implementation, but needs review"`. The older note "Nothing was
committed — commit is left to the user" is obsolete.

### Suggested next steps (priority order)
1. Fix the 3 broken `owner` wirings (`Enemies/Shielder`, `Characters/…/Shield_masc2`, and the stray
   `Dagger` under `Enemies/Barbarian`), **and** make both weapon scripts resolve the owner defensively
   (`GetComponentInParent` → loud error → keep the hitbox off) instead of NRE-ing in `Awake`.
2. Migrate `ExplosionDamage` and `LightingDamage` into `DamageResolver` (they are single-effect:
   `NormalEffects = new List<GameplayEffect>{ DamageEffect }`,
   `BlockedEffects = new List<GameplayEffect>{ BlockedEffect }`,
   `UseBlockedEffects = result == HitResult.Blocked`) — that closes P0.2/P1.2 for all four damage
   sources and removes the last `Debug.Log("effectmanager … is null")` occurrences.
3. Replace the direct `CameraShakeManager.instance` call at `EnemyWeaponDamage.cs:87` with
   `DamageResolver.PlayShake`, route the counter / parry branches through the resolver so null effects
   are caught, null-guard `defense` / `agent` / `rotateAlongPlayer`, and decide whether the missing
   enemy-side rumble is intentional.
4. The explicit-`Id` option is **P1.1 step 4** ("replace the string→hash indirection with compile-time
   IDs"), not P2.2 (P2.2 is the time-scale/timing item). The 5 duplicate-name warnings can only
   disappear when the skins get unique names or ids; today the registry cannot express "these five Dash
   assets are one ability" except by the implicit `StringToHash(Name)` collapse, which is why the
   validator must keep warning. (Do **not** rename a name — the name is the persisted Id.)
5. Route the counter / parry / got-blocked branches through `ApplyHit`, add a test `.asmdef` so the
   P1.2 harness becomes re-runnable, then resume P2.1 (layers) → P3.1 (typos) → P2.3/P2.4. Note the
   prerequisite: game code has no asmdef of its own (it is all predefined `Assembly-CSharp`), so a test
   assembly cannot reference it until the code under test gets its own asmdef.

### Docs correction pass (same day) — claims that did not survive re-checking

While re-verifying the status table every claim was re-run against the source. These earlier notes were
**wrong** and are corrected above; keep the pattern (verify before writing a status):

| Earlier claim | Actual |
|---|---|
| "empty effect lists are silent on the player side" | `TrySelectEffect` errors on both sides (`DamageResolver.cs:184-191`) |
| `Outcome.NoEffects`, `ComboHit.HasDefense`, `DamageData.defense` armor | none of these exist; defender gating is `DefenseState.ResolveHit()` |
| "P1.1 step 3 done, `DamageEnemyBase` migrated" | no such file; only `KnockBack.cs` uses `AbilityIds.` |
| "P0.3 … non-`NoDefense` outcome through `PlayDamage`" | no `NoDefense`, no `PlayDamage`; P0.3 was never in the original plan |
| "5 raw `NameToLayer` lookups" | 9 raw layer literals (`GetMask` + `NameToLayer`) in 9 files |
| "6 behavior graphs" | 12 (`t:BehaviorAuthoringGraph`): 6 enemy + 6 NPC |
| "the weapon classes no longer touch the singletons" | `EnemyWeaponDamage.cs:87`, `DefenseState.cs:49,51`, `ExplosionDamage.cs:84,88`, `LightingDamage.cs:54,58` still do |
| `LightAttackSpec` "8-float constructor" | 6 floats (`LightAttackSpec.cs:38`) |
| "~49 typos in 18 files" / "no test infra at all" | 53 in 19 files; `com.unity.test-framework` 1.6.0 **is** installed — only the asmdefs are missing |

New finding from this pass: **P1.2 only covers 2 of the 4 damage sources** —
`Assets/Scripts/Enemies/Combat/ExplosionDamage.cs` and `LightingDamage.cs` still run the pre-resolver
copy (bare `Debug.Log` on missing components, unguarded `CameraShakeManager.instance`, unchecked
`defense`). Migrating them is the cheapest remaining safety win.

### Follow-up (later the same day) — the remaining work is now written into the plan

Asked whether anything is left in **P0.3 / P1.2 / P1.3**: yes in all three, and the plan now carries it
rather than only a headline row.
- **P0.3 had no section body at all** (it existed only in the new status table). It is now a full issue
  entry: the three verified seams (hit consumed before invincibility is known; `MakeInvincible` as a
  hand-paired bool over 4 call sites; `ResolveHit()` mutating + firing shake/rumble from inside a query)
  and a 5-step approach whose step 2 is a design decision about the invincibility model.
- **P1.2** has a 5-item *Remaining work* list: migrate the two other damage sources, route
  counter/parry/got-blocked through the resolver, decide the enemy-rumble gap, the asmdef prerequisite for a
  repeatable harness, then the branch checklist.
- **P1.3** has a 4-item list: the 5 unguarded singleton calls with line numbers, step 2 (interface +
  provider) never started — which is exactly what blocks mockability — the non-combat long tail, and using
  the validator's existing file walk as the convention guard.
- **§4** gained a per-milestone **State** column and a numbered **"Next up, in order"** list that starts with
  the weapon-prefab content fixes.
- Corrected the validator's shape while re-checking it: `AbilityReferenceValidator.Validate()` sits in the
  **global namespace** (no file in the project declares `namespace GameplayAbilitySystem*`) and returns a
  plain **int = problems + warnings**; the scope numbers live only in the log summary line
  (`… checked 30 ability assets, 24 registry names, 8 attack definitions, 12 behavior graphs (34 Ability
  fields), 428 prefab/scene files (4 ActivateAbility/CancelAbility bindings) in 1283ms -> 0 problem(s) and
  5 warning(s)`). Any wording here implying a returned problems/warnings object was wrong.
- Related open item found: the duplicate-`Name` warning is tolerated *"as long as each one sits in a
  different `AbilityHolder`"* per the validator's own comment, but **nothing verifies that**. A holder-level
  check (group each holder's `abilities` by `Name`) is the cheap way to turn those 5 warnings from assumed
  safe into proven safe — the recipe is already in `NOTES.md`.
- A **ready-to-paste handoff prompt** for the implementation pass now exists at `NEXT_AGENT_PROMPT.md`
  (repo root, 7 tasks in order, 4 DECIDE points flagged, verified file:line anchors and the exact
  `DamageResolver` API). Point the next agent at that file instead of re-deriving the backlog.
Re-verified after the edits: 0 compiler errors, `Validate()` still **0 problems / 5 warnings**, and
`git status` shows only `MEMORY.md` / `NOTES.md` / `SYSTEMS_REVIEW_PLAN.md` modified — no code touched.


## Hazards + Player Projectile Migration (11/8/2026, pass 3) — **DONE, compile-verified, NOT play-tested**

Brief: `NEXT_AGENT_PROMPT.md`. Goal: the last damage sources that still bypassed the shared resolver.

### Files changed (all **unstaged**, nothing committed)
| File | What it is now |
| --- | --- |
| `Assets/Scripts/Combat/IHasInstigator.cs` | **NEW** (+ `.meta`). `IHasInstigator.SetInstigator(GameObject)` + `static HazardInstigator.Assign(root, instigator)` (walks `GetComponentsInChildren<IHasInstigator>(true)`, null-safe on both args). |
| `Assets/Scripts/Combat/DamageResolver.cs` | gained `PlaySound(SFXDefinition, Transform)` (null-guards `def` **and** `SoundFXManager.instance`) and `TagExists(string, Component)` (accepts built-in tags, logs the available list once). |
| `Assets/Scripts/Enemies/Combat/LightingDamage.cs` | single-hit hazards (arrows/fireballs/lightning/ice/laser). Both `switch` branches → `ApplySingle`; raw shake/`ApplyEffect`/manual defender lookup deleted; `Awake` tag validation; `OnEnable` clears instigator + `PlaySound`; `PerfectParry` → `PunishInstigator()`. |
| `Assets/Scripts/Enemies/Combat/ExplosionDamage.cs` | DoT hazards. Same migration + the stale-cache fix: only `isIn`/`tickTimer`/`target(GameObject)` are kept, `DefenseState`/`MakeInvincible` are re-resolved **every tick** and null-checked, `OnTriggerExit` matches the stored target, `tickTimer` no longer reset during i-frames, `FirstTickOffset` exposed. |
| `Assets/Scripts/Enemies/Combat/PlayerDamageProyectile.cs` | **rewritten** against the real APIs (the previous draft used `PathologicalGames`/`SpawnPool`/`GameplayActorReferences`, none of which exist here, and `using UnityEngine.Behavior` instead of `using Unity.Behavior`). Tag + effect validation, enemy-guard path (`RotateAlongPlayer` + `AttackingState.Defend`, null-guarded `BehaviorGraphAgent.BlackboardReference`), `OnEnable` instigator reset, `BlockedEffect`/`ParryEffect` optional, sound via `DamageResolver.PlaySound`. |
| `Assets/Scripts/CharacterVFX.cs` | both spawn paths call `HazardInstigator.Assign(vfx, gameObject)` **after** `VFXManager.Play(...)`. |
| `Assets/GameplayAbilitySystem/Scripts/NewAbilities/DamageProyectileSpec.cs` | `CreateProyectile` calls `HazardInstigator.Assign(projectile, player)` after `Play`. |

### The four DECIDE points — answers implemented
1. **`PerfectParry` did nothing on any hazard** → it now applies a new **optional** `ParryEffect` to the
   instigator, mirroring `EnemyWeaponDamage`'s parry-stun. Silent when the hazard has no instigator
   (environmental fire), `LogWarning` when the instigator exists but the field is unassigned.
2. **DoN tick during i-frames** → **skipped, not consumed**: `tickTimer` is reset only once the tick is
   really evaluated, so the tick lands the frame the roll ends instead of burning 2s of damage.
3. **Rumble parity** → hazards rumble like the player weapon (`DamageRumble` 0.6/0.6/0.2s,
   `BlockedRumble` 0.2/0.4/0.15s). The **enemy weapon** path still passes no `Haptics` — deliberately left
   for the user, it is a separate feel call.
4. **`SoundFXManager.instance` in the two `OnEnable`s** → in scope: moved behind
   `DamageResolver.PlaySound(def, transform)`, which guards both the definition and the singleton.

### Bugs fixed beyond the migration
- `CompareTag(ObjectToDamage)` threw a Unity error on an unknown tag and aborted `OnTriggerEnter` **before**
  damage → hazards looked alive while never hitting. Now validated once in `Awake` (log + disable).
- `defense.ResolveHit()` on an unchecked `defense` (`NullReferenceException`) in both hazards.
- `ExplosionDamage` cached the defender's components in instance fields: a second collider overwrote them
  before the null check and `Update()` kept ticking a half-resolved/destroyed target.
- `OnTriggerExit` cleared `isIn` for **any** collider, not the one being ticked.
- `Debug.Log("effectmanager, attributesystem or makeInvincible is null")` was both wrong (never checked
  `makeInvincible`) and quiet — replaced by the resolver's `LogError` + loud drop.
- `ExplosionDamage.OnEnable` played audio with no `def` guard (LightingDamage had one).
- `PlayerDamageProyectile`: did not compile; no blocked/parry handling; pooled instances kept the previous
  instigator.


### Verification actually performed (compile-level only — no play mode was run)
- `refresh_unity` (force/all/compile) → `read_console` errors: **0 entries**.
- `AbilityReferenceValidator.Validate()` → returned **5**, log summary: **`0 problem(s) and 5 warning(s)`**
  (the same 5 pre-existing duplicate-`Name` notices).
- Reflection: `DamageResolver.ApplySingle(SingleHit) -> Outcome` resolves; `PlaySound` / `TagExists` exist;
  all three classes implement `IHasInstigator`; serialized fields preserved —
  `LightingDamage{CameraShakeData,DamageEffect,BlockedEffect,ObjectToDamage,def}` + new `ParryEffect`;
  `ExplosionDamage{DamageTickRate,CameraShakeData,DamageEffect,BlockedEffect,ObjectToDamage,def}` + new
  `FirstTickOffset`,`ParryEffect`; `PlayerDamageProyectile{DamageEffect,BlockedEffect,ObjectToDamage,def}`
  + new `ParryEffect`.
- Grep: `CameraShakeManager.` / `VibrationManager.` / `.ApplyEffect(` / `PlaySoundRandomPitch` are gone from
  all combat code (`Enemies/Combat`, `Player`, `Combat`); remaining hits are the resolver itself, the camera
  listener, save restore (`GameLoader.cs:111`) and the non-combat audio/UI long tail.
- Prefab YAML audit (script GUIDs: LightingDamage `af39d1096c785b94fac72e2ed975ce21`, ExplosionDamage
  `632f393838795e94ca51fd06c9ec0ad2`, PlayerDamageProyectile `bf277be0593fd264cbe64e5889763884`): every
  hazard uses `ObjectToDamage: Player` and the player projectile uses `Enemy` — **both tags exist** in
  `ProjectSettings/TagManager.asset`, so nothing trips the new `TagExists` guard. Gaps: `ExplosionPrefab`
  has no `BlockedEffect`/`def`; `Assets/Prefabs/Combat/DamageProyectile.prefab` is an unreferenced orphan
  with stale `MoveDamageProyectile` data (empty `ObjectToDamage`); **no prefab has `ParryEffect` yet**.

### Play-mode checklist for the user (nothing below has been executed — no first-party asmdef, so no tests either)
1. Explosion / lightning / arrow on a plain player → damage lands, one shake + one rumble per hit.
2. Same three while **blocking** → `BlockedEffect` (lighter rumble), never `DamageEffect`.
3. Same three while **perfect parrying** → no damage; with `ParryEffect` assigned the thrower is punished,
   without it a single warning is logged.
4. Same three while **dashing / right after knockback** → no damage, and the next tick/hit still lands
   (i-frames must not burn the attack).
5. Fireball explosion while blocking → expect the "blocked with no `BlockedEffect`" warning until the
   prefab gets one (content gap above).
6. Target tagged but without `DefenseState` → one `LogError`, **no** `NullReferenceException`.
7. Unassigned `DamageEffect` → resolver logs and drops the hit (shake/rumble are skipped with it — the
   resolver never shakes for a hit it did not apply).
8. Player's own projectile hitting an enemy → damage lands; a guarding enemy blocks it (`BlockedEffect`);
   a perfect parry by an enemy punishes the player only if `ParryEffect` is assigned on
   `Assets/VFX/AA_VFX/Prefabs/DamageProyectile.prefab`.
9. Pooled reuse: fire one arrow, let it return to the pool, have an enemy fire the same VFX later → the
   second one must punish the enemy, not the player.

### Still open
`ParryEffect` assets + assignment (designer call); enemy-weapon rumble parity; the dash/throw input-lock
leak; armour/mitigation; P2/P3 tail. The editor (not this task) also dirtied
`Assets/Characters/Humans/Enemies/Shields/Shield_masc2/Shield_masc2.prefab`,
`Assets/Enemies/Shielder.prefab`, `Assets/Materials/BoundaryMaterial.mat`, `Assets/Scenes/Forest.unity`
and the `Assets/_Recovery/0 (1x).unity` auto-recovery scenes — **not** part of this work.

