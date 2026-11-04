# Combat & Enemy System — Review & Remediation Plan

> Reviewer's plan for the **combat** and **enemy** systems of *Ciro's Vengeance* (Unity).
> Rooted in a source read performed 11/4/2026. Prior documentation lives in
> `MEMORY.md` (Enemy System Analysis) and `NOTES.md` (§5 Enemy Combat, §8 Boss, §9 Enemy Group).
>
> This document is a *to-do plan*, not a changelog. Nothing here has been implemented yet.
> Follow the repo rule in `AGENTS.md`: **do not commit** — leave commits to the user.

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

| Milestone | Tiers | Why this order |
|-----------|-------|----------------|
| **A — Stabilize** | P0.1, P0.2 | Cheapest, highest safety, removes silent bugs that waste future debugging time. |
| **B — Make content safe** | P1.1 (registry + validation) | Stop the silent-rename footgun *before* adding more abilities. |
| **C — Unify the hot path** | P1.2 (shared resolver) | Highest-value single refactor; do it once, carefully, with full branch verification. |
| **D — Loose the coupling** | P1.3 (one manager at a time) + P2.1 (layers) | Improve buildability/reuse without destabilizing combat. |
| **E — Polish** | P2.2 (timing), P2.3 (state), P2.4 (tunables) | Robustness once the core is stable. |
| **F — Housekeeping** | P3.1 (rename), P3.2 (balance data), P3.3 (graph hygiene) | Optional long tail; keep conventions going. |

**Sequencing notes:**
- Finish **B** before adding new content (not necessarily before **C**).
- **C** is the riskiest — budget the most time and verification effort for it.
- **P1.3** should be a *convention from milestone D onward*, not a complete rewrite.

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
