# Combat & Enemy System — Review & Remediation Plan

> Reviewer's plan for the **combat** and **enemy** systems of *Ciro's Vengeance* (Unity).
> Rooted in a source read performed 11/4/2026. Prior documentation lives in
> `MEMORY.md` (Enemy System Analysis) and `NOTES.md` (§5 Enemy Combat, §8 Boss, §9 Enemy Group).
>
> This document is a *to-do plan*, not a changelog. **Update 11/8/2026:** P0.1/P0.2, P1.1 and most of
> P1.2 are now implemented — read §0.5 below for the verified per-item status before starting work.
> Follow the repo rule in `AGENTS.md`: **do not commit** — leave commits to the user.


## 0.5 Status of this plan — verified in-editor on 11/8/2026

Headline: **milestone A is done (melee paths only), B is done in part (registry + validator, no call-site
migration), C is done-but-partial, D–F have not started.**
Detail and evidence live in `MEMORY.md` (§ "Ability-Name Drift Guard", § "Review Verification Pass") and
`NOTES.md` (§ P1.2-notes — the `DamageResolver` hot path); the two content hazards found by that pass are
listed at the bottom of this section.

| Item | Status | Verified state |
|------|--------|----------------|
| **P0.1** combo index silently skips damage | **Complete** | `DamageResolver.TrySelectEffect` (`Assets/Scripts/Combat/DamageResolver.cs`) guards `gameplayEffects` **and** `gameplayEffectsBlock` independently and `LogError`s owner + combo + both list sizes; both weapon scripts call it before reading an effect. |
| **P0.2** null components in hit paths | **Partial — RISKY** | The two melee paths now resolve the defender centrally and loudly (`TryResolveDefender`), and empty / out-of-range effect lists already `LogError` on **both** sides (`DamageResolver.cs:184-191`). Still on the old silent pattern: `ExplosionDamage.cs:50-52,:77,:84,:88` and `LightingDamage.cs:37-39,:47,:54,:58` (`Debug.Log("effectmanager … is null")` + unguarded `CameraShakeManager.instance` + unchecked `defense`), plus `EnemyWeaponDamage.cs:87` (direct static shake). |
| **P0.3** guard / i-frame seams (added 11/8/2026 — the original plan had no P0.3) | **Missing — needs a design decision** | Verified: `EnemyWeaponDamage.OnTriggerEnter` sets `AlreadyHitPlayer = true` **before** the invincibility test (`:43-47`), so a swing that touches the player during i-frames is consumed and can never re-register. `MakeInvincible` is a timer-less bool (`:16-35`); 4 specs pair `SetInvincible(true/false)` by hand (`DashSpec.cs:63/151`, `ArcherDashSpec.cs:40/100`, `DamageProyectileSpec.cs:70/155`, `KnockBack.cs:60/89`), so any interrupt between the pair leaves the player stuck. `DefenseState.ResolveHit()` is a query that also mutates state and fires feedback (`:41-63`, incl. unguarded `CameraShakeManager.instance` `:49` / `VibrationManager.Instance` `:51`), and every caller dereferences `defense` with no null check. |
| **P1.1** magic-string ability names | **Partial (steps 1-2)** | `AbilityIds.cs` (24 names + `IsKnownName` + `TryGetId`) and the editor validator (`AbilityHolder` duplicate-Id check, `AbilityIds.<Name>` marker check over `.cs`, `AttackDefinition.ability` + behavior-graph `Ability` checks) **are** in place. **Step 3 never happened**: the only first-party code that uses `AbilityIds.` is `Scripts/Combat/KnockBack.cs`; `AbilityHolder`'s public string API, `PlayerAbilityController`, `ActivateAbilityAction`, `ExecuteAttackSequenceAction`, `AttackStep.ability` and `AttackDefinition.ability` are all still raw strings (there is no `DamageEnemyBase.cs`). Step 4 open. Caveats: raw `"Dash"` literals in `.cs` are not flagged, `IsKnownName` is **case-sensitive**, and there is **no** `IsKnownId`. Live baseline (11/8/2026, `AbilityReferenceValidator.Validate()`): **0 problems / 5 warnings** — the 5 are the duplicate-`Name` groups (Dash×2, CounterAttack×3, ElectroAttack×2, FireAttack×2, Teleport×2) — over 30 ability assets, 24 registry names, **8 attack definitions (all of them resolve)**, 12 graphs (34 string `Ability` fields) and 428 prefab/scene files (4 bindings), in ~1.3 s. |
| **P1.2** shared damage resolver | **Partial (2 of 4 sources)** | `DamageResolver` exists, the public surface is unchanged, and both melee scripts delegate the **normal-damage** path. Not migrated: the counter / parry / got-blocked branches (they still call `ApplyEffect` + `PlayShake`/`PlayRumble` — or `CameraShakeManager.instance` directly — outside `ApplyHit`), and the two other damage sources **`ExplosionDamage` / `LightingDamage`**, which keep their own copy of the pre-resolver pipeline. The enemy path also never rumbles. Testing: `com.unity.test-framework` 1.6.0 **is** installed but there is **no first-party `.asmdef`** (the only four belong to Synty), so all game code sits in the predefined `Assembly-CSharp`, which a test assembly cannot reference — re-running a harness first needs `Ciro.Combat` + `Ciro.Combat.Tests` asmdefs. |
| **P1.3** singleton decoupling | **Partial (convention + one helper)** | `DamageResolver.PlayShake` / `PlayRumble` (`:217-241`) centralise the two combat singletons and are null-safe (`PlayShake` logs when `CameraShakeManager.instance` is missing, `PlayRumble` returns silently). Direct uses remain in `EnemyWeaponDamage.cs:87`, `ExplosionDamage.cs:84,88`, `LightingDamage.cs:54,58`, `DefenseState.cs:49,51` (all unguarded) and outside combat in `GameplayMenu` (4), `MenuNavigation`, `SaveSystem`, `RegisterListener`. No interface / provider anywhere. |
| **P2.1** hardcoded layer names | **Missing** | No `Layers` helper anywhere. **9** raw literals in 9 files: `"EnemyLayer"` ×6 — `AreaRayAttackSpec.cs:9`, `HealTargetSpec.cs:16`, `LightAttackSpec.cs:48`, `LightAuraSpec.cs:11`, `RotateAlongPlayer.cs:40`, `PlayerWeaponDamage.cs:146`; `"Player"` ×2 — `CounterAttackSpec.cs:38`, `EnemyWeaponDamage.cs:43`; `"Floor"` ×1 — `MoveFireBall.cs:37`. (`MinimapIcon.cs:67` is the good pattern: the layer name is a serialized field.) |
| **P2.2** timing / time-scale | **Missing (partial by design)** | `GameplayAbilitySpec.Tick(float deltaTime)` is clock-injected, but `AbilityHolder.cs:285` still feeds `Time.deltaTime`, and `RotateAlongPlayer` mixes `Time.time` with `Time.deltaTime`. The "should cooldowns ignore slow-mo?" decision is still unmade. |
| **P2.3** public mutable state | **Missing** | `GameplayAbility` (`:14-21`) still exposes `Name`, `icon`, `Cooldown`, `ActiveTime`, `isPasive`, `equipable`, `EffectsToSelf`, `Description` as public fields (only `Id` is a get-only property), and `AbilityHolder.ActiveSpecs` is a public `List<GameplayAbilitySpec>` field (`AbilityHolder.cs:18`) that callers mutate. |
| **P2.4** tunables in constructors | **Missing** | `LightAttackSpec` (`Assets/GameplayAbilitySystem/Scripts/NewAbilities/LightAttackSpec.cs:38`) still takes its tuning as a **6-float** constructor (`ForwardDistanceRay`, `LateralDistanceRay`, `AttackAssistSpeed`, `DashStopDistance`, `DashDefaultDistance`, `RotationSpeed`) instead of reading serialized/ScriptableObject values. |
| **P3.1** typos / bilingual names | **Missing** | **53** occurrences of `Proyectile` / `Trayectory` / `isPasive` / `SetInputAviable` across **19** first-party files (third-party folders excluded from the count). |
| **P3.2** centralized balance data | **Missing** | No balance asset/CSV. |
| **P3.3** behavior-graph hygiene | **Missing** | **12** graphs (`t:BehaviorAuthoringGraph`): 6 enemy in `Assets/Enemies/BehaviorTrees/` (Archer, Barbarian, Healer, Mage, Shielder, SpaceGod) + 6 NPC in `Assets/NPC/BehaviorTrees/` (Commoner, Guardian, Merchant, Noble, Priest, Scholar). None has a sibling `.md` — there is not a single `.md` file under `Assets/`. |

**Content hazards the plan did not anticipate (found 11/8/2026, all pre-existing):**
1. **Null `owner` on an *active* weapon → NRE at spawn.** `Awake` in both weapon scripts does
   `owner.GetComponent<…>()` unguarded. `Assets/Enemies/Shielder.prefab`,
   `Assets/Characters/Humans/Enemies/Shields/Shield_masc2/Shield_masc2.prefab` and a stray `Dagger`
   under `Assets/Enemies/Barbarian.prefab` are wired to nothing while active. See
   `NOTES.md` §Auditing serialized references inside nested weapon prefabs.
2. **Weapons with empty effect lists.** `Dagger` and `SM_Wep_AncientWarrior_01` have 0 elements in
   `gameplayEffects`. This is **loud, not silent** — `TrySelectEffect` treats combo index 0 as
   out-of-range against an empty list and `LogError`s owner + both list sizes on every hit
   (`DamageResolver.cs:184-191`), so the symptom is error spam plus zero damage. Fix is content, not code.
3. **Two same-named script pairs will mislead the migration.** `ExplosionDamage.cs` and
   `LightingDamage.cs` each exist twice; only the copies under `Assets/Scripts/Enemies/Combat/` deal
   damage — `Assets/Scripts/Enemies/ExplosionDamage.cs` and `Assets/Scripts/Lightning/LightingDamage.cs`
   are VFX-only. See `NOTES.md` §Duplicate script names.


---

## 0. Verdict (one paragraph)

The architecture is **good and above average** for a project this size. The core
design choices (layered GAS, data-driven abilities, Behavior Graph AI decoupled from
combat, slot/orbit positioning, token-limited encounters, weighted boss sequences) are
the *right* ones. What's rotting is **not the foundation but the seams**: static
singletons, duplicated damage logic, magic-string ability names, and a few silent
footguns. The right move is **targeted polish, not a rewrite**. This plan orders those
fixes by importance and gives future agents concrete, safe steps.

---

## 1. How to read this document

- Issues are grouped into four tiers by importance: **P0 → P3**.
- Every item follows the same template so it is actionable at a glance:
  - **Problem** — what is wrong.
  - **Impact** — why it matters (player-facing, maintainer-facing, or risk).
  - **Where** — exact files.
  - **Approach** — how to fix it, from lowest to highest effort.
  - **Verification** — what to check after the change.
  - **Risk** — blast radius / gotchas.
- **Verification** always means: Unity compiles clean in-editor + PlayMode check of the
  named scenario. There is no headless run available here, so every fix must be
  validated by hand in the Unity Editor.

---

## 2. How issues were prioritized

Each issue was scored on:

`(player-facing severity) × (likelihood) × (blast radius) ÷ (effort)`

- **P0** — silent gameplay bugs / easy wins. Cheap, high safety, fix first.
- **P1** — foundational *buildability*. Fix before adding more content or the cost of
  every new feature keeps climbing.
- **P2** — robustness & maintainability. Worthwhile; do when P0/P1 are stable.
- **P3** — housekeeping / optional. Nice-to-have, low urgency.

---

## 3. Tiers & issues

### Tier P0 — Silent gameplay bugs (fix first, low effort, high safety)

#### P0.1 Combo-number index silently skips damage
- **Problem:** `EnemyWeaponDamage.NotParried(...)` and `PlayerWeaponDamage` both do:
  `if (ComboNumber < 0 || ComboNumber >= list.Count) { Debug.LogWarning(...); return; }`.
  An out-of-range combo means **the attack does nothing** and only logs a warning.
- **Impact:** Player-facing gameplay bug that is very hard to trace. A designer who
  forgets to fill an effects list (or shuffles a list) silently zeros an attack.
- **Where:** `Enemies/Combat/EnemyWeaponDamage.cs` (`NotParried`, line ~97),
  `Player/PlayerWeaponDamage.cs` (combo block, line ~217). Note `EnemyWeaponDamage`
  indexes *two* lists (`gameplayEffects` AND `gameplayEffectsBlock`) with one index —
  an easy off-by-one source.
- **Approach:**
  1. Replace the silent `return` with a **loud failure**: `Debug.LogError` with the
     owner name, the combo number, and both list sizes.
  2. Guard both list accesses and fail clearly if *either* list is short.
  3. (Nice) clamp/log the out-of-range value so designers see it in the inspector.
- **Verification:** Set a weapon's combo list to be shorter than its max combo, play,
  confirm a clear error names the object + combo number (not a silent miss).
- **Risk:** Very low. Purely additive error handling.

#### P0.2 Null components logged silently inside hit paths
- **Problem:** When `GameplayEffectManager` / `AttributeSystem` is missing on the
  target, `EnemyWeaponDamage` falls back to `Debug.Log("effectmanager, attributesystem
  or makeInvincible is null")` inside a live hit path.
- **Impact:** Hard-to-diagnose runtime error during combat; no clear ownership.
- **Where:** `Enemies/Combat/EnemyWeaponDamage.cs` `NotParried`, line ~84.
- **Approach:** Centralize the "get defender's effect manager + attributesystem" lookup
  (see P1.2) and log a clear, categorized error if null. Consider a tiny
  `EnsureNotNull` helper that throws a friendly, tagged exception in Debug builds.
- **Verification:** Attach the enemy/weapon to a target missing the component, confirm
  a clear tagged log.
- **Risk:** Low.

#### P0.3 Guard / i-frame seams (added 11/8/2026 — not in the original plan)
- **Problem:** three verified seams in the defence path, none of them covered by P0.1/P0.2/P1.2:
  1. **A hit is consumed before invincibility is known.** `EnemyWeaponDamage.OnTriggerEnter` sets
     `AlreadyHitPlayer = true` (`:43-47`) *before* `makeInvincible.isInvincible` is consulted, so a swing
     that merely touches the player during i-frames is spent and can never re-register — the i-frame
     suppresses the damage but does not preserve the hit.
  2. **Invincibility is a hand-paired bool.** `MakeInvincible.SetInvincible(bool)` (`:16-35`) has no
     timer, no owner and no token; four call sites pair `true`/`false` by hand (`DashSpec.cs:63/151`,
     `ArcherDashSpec.cs:40/100`, `DamageProyectileSpec.cs:70/155`, `KnockBack.cs:60/89`). Any interrupt
     between the pair (knockback, death, scene change, cancelled ability) leaves the player permanently
     invincible or permanently vulnerable, depending on which half is lost.
  3. **`DefenseState.ResolveHit()` is a query that also mutates and presents.** It flips internal guard
     state and fires `CameraShakeManager.instance` (`:49`) / `VibrationManager.Instance` (`:51`) from
     inside the resolution call, unguarded, and every caller dereferences `defense` without a null check.
- **Impact:** i-frames can silently eat or allow hits; an interrupted dash can leave the player in a
  defence state nothing can recover; a missing feedback manager turns a parry into an NRE.
- **Approach (step 2 needs a design decision):**
  1. Decide who owns "is this swing still live". Cheapest correct fix: evaluate invincibility **before**
     committing `AlreadyHitPlayer`, or have the resolver return an `Invincible`/`Ignored` outcome and let
     the adapter choose whether to consume the hit (that means adding a member to `Outcome`).
  2. Choose the invincibility model: (a) keep the paired API but add auto-expiry (duration argument or
     watchdog in `Update`) plus a token so only the setter can clear it, or (b) move i-frames into
     `DamageResolver` as timed state owned by the defender. (a) is smaller and keeps all four call sites.
  3. Split `DefenseState` into a pure query (`HitResult EvaluateHit(...)`) and an explicit
     `PlayDefenseFeedback(HitResult)`, keeping `ResolveHit()` as a thin wrapper so existing callers keep
     compiling, and route the feedback through `DamageResolver.PlayShake` / `PlayRumble` (ties into P1.3).
  4. Null-guard `defense` at every call site, the way `makeInvincible` already is.
  5. Settle the armour question and record it: there is **no** armour/mitigation concept today
     (`HitResult` is `{ PerfectParry, Blocked, FullDamage }`, `DamageData` has no `defense` field), so
     normal weapon damage ignoring armour is by design — either document that on the class, or wire a
     real input into the resolver.
- **Verification:** dash into a swing and confirm the swing can still connect afterwards if it should;
  interrupt a dash mid-i-frames (knockback / death / teleport) and confirm the player is neither locked
  invincible nor stuck vulnerable; parry still triggers slow-mo + shake; remove `DefenseState` from the
  player and confirm a tagged error instead of an NRE.
- **Risk:** Medium — this changes *when* hits are consumed, which is player-visible. Do it after P1.2 has
  gathered the branches into one place, and re-verify parry/block/full/counter/invincible together.

---

### Tier P1 — Foundational buildability (fix before scaling content)

These are the three things that make new features expensive. Do them before adding the
next batch of abilities/enemies.

#### P1.1 Magic-string ability names (`Animator.StringToHash`)
- **Problem:** Abilities are addressed by **string name**. `AbilityHolder` keys its
  `specs` dictionary by `ability.Id` (which is itself `Animator.StringToHash(Name)`) and
  `ActivateAbility(string)` / `IsAbilityActive(string)` / `HasAbility(string)` all call
  `Animator.StringToHash(name)` to look up. AI `Action` nodes pass ability names as plain
  blackboard strings (`ActivateAbilityAction.Ability`, `ExecuteAttackSequenceAction`).
- **Impact:** A designer who renames an ability asset **silently breaks every ability
  that references it** — no compiler error, no warning, just "this attack no longer
  fires." This is the single biggest latent bug source as content grows.
- **Where:** `GameplayAbilitySystem/Scripts/BaseAbility/AbilityHolder.cs`,
  `GameplayAbilitySystem/Scripts/BaseAbility/GameplayAbility.cs`,
  `Enemies/Scripts/ActivateAbilityAction.cs`,
  `Enemies/Scripts/ExecuteAttackSequenceAction.cs`.
- **Approach (incremental, lowest risk first):**
  1. Add a centralized registry: a static `AbilityIds` (or an enum + string map) that
     names every ability once (`const`/named constants). This becomes the single source
     of truth for names.
  2. In `AbilityHolder.Awake`, **validate uniqueness**: if two abilities collide on an
     `Id` or a looked-up name isn't registered, log a loud, clear error listing both.
  3. Migrate the AI `Action` nodes to look up names through the registry instead of raw
     strings.
  4. (Later) replace the string→hash indirection with compile-time IDs entirely.
  5. Extend the validator with the **holder check it only assumes today**: the duplicate-`Name` warning
     says sharing an Id is *"fine as long as each one sits in a different `AbilityHolder`"*, but nothing
     opens a holder to confirm it. Grouping each holder's `abilities` list by `Name` is what turns the 5
     standing warnings from assumed-safe into proven-safe (recipe in `NOTES.md`). Note that renaming a
     duplicate is **not** an option — the name is the persisted Id.
- **Verification:** In a scene, activate every ability type and confirm zero
  "not found" warnings. Then rename one ability asset and confirm it fails **loudly**
  with a clear message instead of silently.
- **Risk:** Medium — touches core. Keep the public string API working (backward compat)
  during the migration so nothing else breaks, then retire it.

#### P1.2 Shared damage resolver (kill the duplication)
- **Problem:** `PlayerWeaponDamage` and `EnemyWeaponDamage` duplicate the **same**
  "apply a combo hit to a target" pipeline: invincibility check → combo-index validation
  → pick effect → per-combo camera shake → vibration → `ApplyEffect`. The only real
  differences are **hit detection** (player uses a multi-step `OverlapBox` sweep with an
  `alreadyHit` set; enemy uses a single `OnTriggerEnter` box with `AlreadyHitPlayer`)
  and **hit interpretation** (the *defender* decides parry/block via `DefenseState.
  ResolveHit()`).
- **Impact:** Every new weapon type re-implements damage logic (DRY violation — already
  flagged in `MEMORY.md`). Balance changes have to be made in two places; a fix in one
  silently regresses the other.
- **Where:** `Player/PlayerWeaponDamage.cs` (sweep detection + combo apply),
  `Enemies/Combat/EnemyWeaponDamage.cs` (trigger detection + parry resolution + combo
  apply), `Player/DefenseState.cs` (`ResolveHit()` → `HitResult`), shared base
  `Combat/WeaponController.cs`.
- **Approach — split responsibilities clearly:**
  1. Create a small `Combat/DamageResolver.cs` that owns **hit application** only: given
     (target, comboNumber, effects[], blockEffects[], counterEffect, cameraShakes[],
     impulseSource) it does: invincibility check → combo-index guard → pick normal/block
     effect → counter branch → `ApplyEffect` → per-combo camera shake → vibration, and
     returns a result enum.
  2. Make `PlayerWeaponDamage` and `EnemyWeaponDamage` **thin adapters**: each keeps its
     own **hit detection**; each keeps its own **hit interpretation** (enemy resolves
     parry/block/full on the defender; player applies to enemies). Both delegate the
     *mechanical application* to `DamageResolver`.
  3. Keep the public surface identical (`SetComboNumber`, `ActivateHitbox`, etc.) so
     controllers/animations don't need changes.
- **Verification:** Player full-hit combo on enemy; enemy full-hit combo on player; a
  perfect parry still triggers time-slow + counter; a blocked hit still applies reduced
  effect; a counter still applies `counterEffect`. Confirm camera shake + vibration fire
  identically before and after. Play both weapon types back-to-back and diff feedback.
- **Risk:** **High** — combat hot path. Keep the public API stable, verify every branch
  (parry/block/full/counter/invincible) in-editor. Consider a small editor test.
- **Remaining work (verified 11/8/2026 — this item is 2 of 4 sources, not done):**
  1. **Migrate the other two damage sources.** `Assets/Scripts/Enemies/Combat/ExplosionDamage.cs` and
     `Assets/Scripts/Enemies/Combat/LightingDamage.cs` still own a copy of the pre-resolver pipeline
     (bare `Debug.Log("effectmanager … is null")`, unguarded `CameraShakeManager.instance`, unchecked
     `defense`). They are single-effect, so the shape is
     `NormalEffects = new List<GameplayEffect> { DamageEffect }`,
     `BlockedEffects = new List<GameplayEffect> { BlockedEffect }`,
     `UseBlockedEffects = result == HitResult.Blocked`. Careful: both names also exist as VFX-only
     scripts elsewhere — see `NOTES.md` §Duplicate script names.
  2. **Route the non-normal branches through the resolver.** Counter, parry and got-blocked still call
     `ApplyEffect` + shake/rumble (or `CameraShakeManager.instance` directly) outside `ApplyHit`, so a
     null effect there is an NRE instead of a `TrySelectEffect` error. Either extend `ApplyHit` with a
     counter/blocked mode or add `ApplyCounter` / `ApplyBlocked` to `DamageResolver` and keep the adapters
     free of feedback calls.
  3. **Decide the enemy-side rumble gap.** `PlayerWeaponDamage` rumbles, `EnemyWeaponDamage` never does.
     Intentional or not — record the answer either way.
  4. **Make the verification repeatable.** `com.unity.test-framework` 1.6.0 is installed but the game code
     has no asmdef of its own (it is all predefined `Assembly-CSharp`), so a test assembly cannot reference
     it. Re-running the P1.2 harness needs `Ciro.Combat` + `Ciro.Combat.Tests` asmdefs first — a decision,
     not boilerplate.
  5. **Then re-run the branch checklist**: player full combo, enemy full combo, perfect parry (slow-mo +
     counter), blocked hit (reduced effect), counter effect, invincible target, empty effect list (must
     error, not throw), and diff the feedback of both weapons side by side.

---

#### P1.3 Singleton decoupling
- **Problem:** Systems grab each other by global static, e.g. `CameraShakeManager.instance`,
  `TimeEffectsManager`, `VibrationManager.Instance`, `SlotsManager.Instance`,
  `EnemyGraphBridge.OnDeath` (global event), `KarmaManager.Instance`.
- **Impact:** Tight coupling makes features hard to write, test, and reuse in another
  project. Every new system must know about every other singleton.
- **Where:** spread across `Managers/`, `Enemies/`, `Combat/`, `UI/`.
- **Approach (incremental convention, NOT a one-shot rewrite):**
  1. Establish the rule: **new code must not call a static `.instance`** — pass what it
     needs via constructor/serialized fields.
  2. For the hottest hotspots (`CameraShakeManager`, `VibrationManager`, `TimeEffectsManager`,
     `SlotsManager`), introduce an **interface + injectable provider** so they can be
     swapped/mocked, and only *then* keep a static as a convenience fallback.
  3. Prefer per-enemy/owner references (`GetComponent`) over globals where cheap.
- **Verification:** After each refactor, confirm shake/vibration/slow-mo/slots still fire
  in a play test.
- **Risk:** Medium-High if done all at once. **Go one manager at a time.** Full sweep is
  out of scope; treat as an ongoing convention (long tail in P3).
- **Remaining work (verified 11/8/2026 — only the helper + convention exist today):**
  1. **Inside combat (small, do first):** the five unguarded singleton uses are
     `EnemyWeaponDamage.cs:87` (direct `CameraShakeManager.instance`), `DefenseState.cs:49,51`,
     `ExplosionDamage.cs:84,88` and `LightingDamage.cs:54,58`. Replace with `DamageResolver.PlayShake` /
     `PlayRumble` — do them together with the P1.2 items 1-2 so each file is touched once.
  2. **Step 2 of the original approach (interface + injectable provider) has not started** for
     `CameraShakeManager`, `VibrationManager`, `TimeEffectsManager` or `SlotsManager`. The statics are
     still the only access path, so nothing in combat is mockable — this is what blocks real tests.
  3. **Outside combat (long tail, low priority):** `GameplayMenu` (4 uses), `MenuNavigation`, `SaveSystem`,
     `RegisterListener`.
  4. **Keep the convention honest:** new code must not add a `.instance` call. A cheap guard is to extend
     the existing editor-side scan (`AbilityReferenceValidator` already walks every `.cs`) with a
     "new raw singleton outside the allowed list" warning instead of writing a new tool.

---

### Tier P2 — Robustness & maintainability

#### P2.1 Hardcoded layer names
- **Problem:** `LayerMask.GetMask("EnemyLayer")` (`RotateAlongPlayer`, etc.),
  `LayerMask.NameToLayer("Player")` (`EnemyWeaponDamage`), `CompareTag("Enemy")` /
  `CompareTag("Player")`.
- **Impact:** If a layer is renamed in *Edit → Project Settings → Tags & Layers*, every
  reference silently breaks (no compile error).
- **Where:** `Enemies/RotateAlongPlayer.cs`, `Enemies/Combat/EnemyWeaponDamage.cs`,
  `Enemies/EnemiesGroupContoller.cs`.
- **Approach:** Centralize layer resolution (serialized `LayerLookup` asset, or an Editor
  script that stores indices). Replace raw `GetMask`/`NameToLayer` with the lookup.
- **Verification:** Rename a layer in Tags & Layers and confirm nothing stops detecting.
- **Risk:** Low-MEDIUM (find/replace within the centralizer).

#### P2.2 Timing inconsistency (time-scale dependence)
- **Problem:** `GameplayAbilitySpec.Tick` accumulates `Time.deltaTime`, so cooldowns are
  time-scale dependent — they slow/pause under parry slow-mo. Visual timing (e.g.
  `RotateAlongPlayer` wobble uses `Time.time`) is inconsistent with it.
- **Impact:** Cooldowns behave differently under slow-mo than expected; timing is
  frame/time-scale sensitive rather than a clean unscaled accumulator.
- **Where:** `GameplayAbilitySystem/Scripts/BaseAbility/GameplayAbilitySpec.cs` `Tick`,
  `Enemies/RotateAlongPlayer.cs` (wobble).
- **Approach:** Decide intent (cooldowns predictable regardless of slow-mo?). If so,
  accumulate `Time.unscaledDeltaTime` in `Tick`. Align combo/visual timing to a single
  unscaled accumulator.
- **Verification:** Trigger slow-mo mid-combo, confirm cooldowns advance predictably;
  confirm orbit wobble phase doesn't jump.
- **Risk:** Medium — cooldown timing affects balance. Re-tune after the change.

#### P2.3 Public mutable state / missing `[SerializeField]`
- **Problem:** `GameplayAbility` `Name`/`Cooldown`/`EffectsToSelf` are public without
  `[SerializeField]`; `AbilityHolder.ActiveSpecs` is a public mutable list.
- **Impact:** No data protection; external code can corrupt runtime state; inspector
  serialization is inconsistent.
- **Where:** `GameplayAbilitySystem/Scripts/BaseAbility/GameplayAbility.cs`,
  `GameplayAbilitySystem/Scripts/BaseAbility/AbilityHolder.cs`.
- **Approach:** Add `[SerializeField]` + private + read accessors where an invariant must
  hold; hide `ActiveSpecs` behind an accessor preventing external mutation.
- **Risk:** Low-MEDIUM (keep read accessors public; verify nothing relied on old setters).

#### P2.4 Tunables hardcoded in constructors
- **Problem:** `LightAttackSpec` takes 8 float params directly in its constructor.
- **Impact:** Balance is impossible to change without code edits / recompiles.
- **Where:** `NewAbilities` (e.g. `LightAttackSpec`).
- **Approach:** Move tunables to ScriptableObject config or static factory defaults.
- **Risk:** Low (additive).

---

### Tier P3 — Housekeeping / optional

#### P3.1 Identifier typos / bilingual names
- **Problem:** `isPasive`, `SetInputAviable`, `Proyectile`, `Trayectory` — misspellings
  and Spanish/English mixing.
- **Impact:** Cosmetic but hurts maintainability; typos *propagate* when future code
  copies the misspelling.
- **Where:** spread across `NewAbilities`, `Combat`, `GameplayAbilitySystem`.
- **Approach:** Use Unity/VS **refactor-rename** (not manual edit) so references update.
  **Gotcha:** some may be referenced by serialized/inspector values or graph blackboards —
  search the whole `Assets` tree first, then rename and verify.
- **Risk:** Medium (rename can break serialized refs) — do in one careful pass.

#### P3.2 Centralized balance data + versioning
- **Problem:** No single place for damage/balance numbers; tuning only via inspector.
- **Impact:** Hard to version, compare, or test balance.
- **Approach:** Introduce a shared balance data asset/CSV per weapon/ability; commit it to
  git so changes are diffable.
- **Risk:** Low (additive); large in scope — treat as an ongoing initiative.

#### P3.3 Behavior-graph hygiene
- **Problem:** Enemy AI lives in **binary** `*.asset` Behavior Graphs (400–700 KB each),
  which are hard to review and hard to diff in git, and tend to become spaghetti.
- **Where:** `Enemies/BehaviorTrees/` (Barbarian, Archer, Mage, Shielder, Healer, SpaceGod).
- **Approach:** Keep graphs **decision-only** ("which ability, when") and push all "how"
  into small C# `Action` nodes (already mostly true). For each graph, keep a sibling
  `.md` documenting its flow. Version graphs carefully (small, focused commits).
- **Risk:** Low-MEDIUM (process change, not code).

---

## 4. Recommended execution order (milestones)

| Milestone | Tiers | State (11/8/2026) | Why this order |
|-----------|-------|-------------------|----------------|
| **A — Stabilize** | P0.1, P0.2 | **Partial** — P0.1 done, P0.2 done for the two melee paths only | Cheapest, highest safety, removes silent bugs that waste future debugging time. |
| **B — Make content safe** | P1.1 (registry + validation) | **Partial** — registry + validator in place (0 problems / 5 warnings), call sites still strings | Stop the silent-rename footgun *before* adding more abilities. |
| **C — Unify the hot path** | P1.2 (shared resolver) | **Partial (2 of 4 sources)** | Highest-value single refactor; do it once, carefully, with full branch verification. |
| **D — Loose the coupling** | P1.3 (one manager at a time) + P2.1 (layers) | **Not started** (`DamageResolver.PlayShake`/`PlayRumble` exist as the helper seam) | Improve buildability/reuse without destabilizing combat. |
| **E — Polish** | P2.2 (timing), P2.3 (state), P2.4 (tunables) | **Not started** | Robustness once the core is stable. |
| **F — Housekeeping** | P3.1 (rename), P3.2 (balance data), P3.3 (graph hygiene) | **Not started** | Optional long tail; keep conventions going. |

**Sequencing notes:**
- Finish **B** before adding new content (not necessarily before **C**).
- **C** is the riskiest — budget the most time and verification effort for it.
- **P1.3** should be a *convention from milestone D onward*, not a complete rewrite.

**Next up, in order (the concrete short list, from the verified state above):**
1. **Content:** wire the 3 active weapons with a null `owner` (`Enemies/Shielder`,
   `Characters/Humans/Enemies/Shields/Shield_masc2/Shield_masc2`, the stray `Dagger` under
   `Enemies/Barbarian` — the last one should not carry `PlayerWeaponDamage` at all) and fill the two empty
   `gameplayEffects` lists (`Dagger`, `SM_Wep_AncientWarrior_01`). Then harden `Awake` in both weapon
   scripts (`GetComponentInParent` → tagged error → leave the hitbox disabled) so a bad prefab can never
   NRE a spawn again.
2. **P1.2 items 1-2 + P1.3 item 1 in the same pass:** migrate `ExplosionDamage` / `LightingDamage` into
   `DamageResolver` and route counter / parry / got-blocked through it — that closes P0.2 and P1.2 for all
   four damage sources and removes the last unguarded singleton calls in combat.
3. **P0.3** once the branches live in one place: hit-consumption order, then the invincibility model
   decision, then the `DefenseState` query/feedback split.
4. **Testability:** decide the `Ciro.Combat` asmdef, then re-create the P1.2 harness as a real test assembly.
5. **Then** P2.1 (layers) → P3.1 (typos) → P2.3 / P2.4 / P2.2, and P1.1 step 3 only if the behavior-graph
   string blackboards can be migrated without breaking the 12 graphs.

---

## 5. Global hints for future agents

### 5.1 File map (where things live)
```
AI driver (Unity Behavior Graph)
  Enemies/BehaviorTrees/*.asset          (Barbarian, Archer, Mage, Shielder, Healer, SpaceGod)
  Enemies/Scripts/*.cs                    (Action + Condition node classes)

AI <-> Combat bridge
  Enemies/EnemyGraphBridge.cs             (state set, OnDeath event, death cleanup)

Movement / positioning helpers
  Enemies/GoingToPlayer.cs                (slots via SlotsManager)
  Enemies/RotateAlongPlayer.cs            (orbital strafing + wobble + neighbor slowdown)
  Enemies/RotateToPlayer.cs, RotateToTarget.cs (facing)
  Enemies/EscapeFromPlayer.cs             (retreat)
  Enemies/ShieldMaskController.cs         (shield anim layer)
  Enemies/EnemyPlayerDetection.cs         (detection flag)
  Enemies/TokenTimer.cs                   (per-enemy cooldown)

Combat (enemy as attacker)
  Scripts/Combat/WeaponController.cs      (shared base: equip/unequip, collision)
  Enemies/Combat/EnemyWeaponController.cs (extends base)
  Enemies/Combat/EnemyWeaponDamage.cs     (hitbox → DefenseState.ResolveHit → parry/block/full)
  Enemies/Combat/EnemyShieldController.cs
  Enemies/Combat/DeathCheck.cs
  Enemies/Combat/* (projectiles: ArrowMovement, MoveFireBall, ExplosionDamage, LightingDamage, PlayerDamageProyectile)

Combat (player)
  Scripts/Player/PlayerWeaponDamage.cs    (sweep detection + combo apply)
  Scripts/Player/PlayerWeaponController.cs
  Scripts/Player/DefenseState.cs          (parry/block FSM, ResolveHit)

Boss
  Enemies/Boss/PhaseController.cs         (3 phases by health: 66%→P2, 33%→P3)
  Enemies/Boss/AttackDirector.cs          (weighted random + repeat penalty)
  Enemies/Boss/BossConversation.cs
  GameplayAbilitySystem/Scripts/AttackDefinitions/
      AttackLibrary.cs                    (per-phase arrays)
      AttackSequences.cs                  (ordered steps + weight)
      AttackDefinition.cs                 (ability name + AttackKind)

Encounter / group
  Enemies/EnemiesGroupContoller.cs        (token system, AlertGroup, battle over)
  Enemies/Combat/Slot.cs, SlotUpdate.cs + SlotsManager (orbit positions Close/Mid/Far)

GAS core
  GameplayAbilitySystem/Scripts/BaseAbility/{GameplayAbility,GameplayAbilitySpec,AbilityHolder}.cs
  GameplayAbilitySystem/Scripts/Effect/GameplayEffectManager.cs
  GameplayAbilitySystem/Scripts/Attribute/AttributeSystem.cs
  GameplayAbilitySystem/Scripts/AttributeResponses/{EnemyHealthResponse,GeneralHealthResponse}.cs
Managers: CameraShakeManager, TimeEffectsManager, VibrationManager, KarmaManager
UI: EnemyHealthBar, EnemyUIBridge, PlayerHealth, etc.
```

### 5.2 The core integration point (remember this)
Everything flows through:
```
AI Action node → AbilityHolder.ActivateAbility(name, owner) → GameplayAbilitySpec.ActivateAbility()
                → ability.ActivateAbilityEffects(owner) → ApplyEffect → AttributeSystem
```
If a new AI behavior isn't working, the first place to check is whether the ability name
it passes actually resolves through `AbilityHolder` (this is P1.1's pain point).

### 5.3 Static events that tie systems together
- `EnemyGraphBridge.OnDeath(GameObject)` — fired on enemy death.
- `EnemiesGroupContoller.OnBattleStateChanged(bool)`, `.OnHideEnemyHud(List<GameObject>)`.
- `EnemyHealthResponse.OnEnemyHurt` — drives boss phase changes.

### 5.4 The enemy data chain (authorable, not code)
```
AttackLibrary (ScriptableObject) → per-phase AttackSequences[]
AttackSequences → AttackDefinition[] (ordered) + float weight
AttackDefinition → ability name + AttackKind (ComboPhysical | IndividualPhysical | Magical | Teleport)
```
Adding a new boss attack = new ScriptableObject data, **no code** (the extensible layer —
build here freely).

### 5.5 Enemy type → graph map
Barbarian (melee), Archer (ranged), Mage (projectiles), Shielder (shield tank),
Healer (support), SpaceGod (boss/endgame).

### 5.6 Process / git
- **Do NOT commit.** `AGENTS.md` is explicit: leave commits to the user.
- Prefer **refactor-rename** over manual edits for P3.1.
- Keep graph/data changes in small, focused commits (graphs are binary and hard to diff).

---

## 6. Definition of done (for any item above)
1. Unity compiles **clean** (no errors) in the Editor.
2. PlayMode verification of every scenario named in that item's **Verification** step.
3. No new `Debug.Log`/`Debug.LogWarning` noise introduced in hot paths (use tagged logs).
4. Public API kept backward-compatible unless the item explicitly changes it.
5. `MEMORY.md` / `NOTES.md` updated with what changed (per `AGENTS.md`).
6. Left uncommitted for the user to review (do not commit).

---

## 7. Known non-goals (intentionally out of scope)
- **No rewrite** of GAS or the Behavior Graph AI. The foundation is sound.
- **No full singleton sweep** — incremental convention + one manager at a time only.
- **No touching unrelated NPC AI** (NPC folder is out of scope for this plan).
- **No balance tuning** beyond the timing fix in P2.2.
```
