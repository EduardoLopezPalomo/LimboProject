# COMBAT SYSTEM COMPLETE ANALYSIS REPORT

> **Remediation plan:** `SYSTEMS_REVIEW_PLAN.md` (11/4/2026) — issues graded P0–P3,
> executed in milestones A–F. **DONE:** Milestone A (P0.1 + P0.2) — loud `Debug.LogError`
> in `EnemyWeaponDamage.NotParried` and `PlayerWeaponDamage.DamageEnemy`; Milestone B
> (P1.1) — centralized ability-name registry `AbilityIds.cs` + loud uniqueness validation in
> `AbilityHolder.Awake` (kills the silent-rename footgun); and Milestone C (P1.2) — both weapon
> damage classes now delegate the hit pipeline to the new shared `Assets/Scripts/Combat/
> DamageResolver.cs`. Execution status also tracked in `MEMORY.md`. **Do not commit** (see
> `AGENTS.md`).

## TABLE OF CONTENTS
1. [Overview](#1-overview)
2. [Architecture Overview](#2-architecture-overview)
3. [Core Foundation: Gameplay Ability System (GAS)](#3-core-foundation-gameplay-ability-system-gas)
4. [Player Combat Components](#4-player-combat-components)
5. [Enemy Combat Components](#5-enemy-combat-components)
6. [Defense System](#6-defense-system)
7. [Damage Flow](#7-damage-flow)
8. [Boss Combat](#8-boss-combat)
9. [Enemy Group System](#9-enemy-group-system)
10. [Camera & Feedback Systems](#10-camera--feedback-systems)
11. [UI/HUD Components](#11-uihud-components)
12. [Complete Data Flow Diagram](#12-complete-data-flow-diagram)
13. [Summary of All Scripts](#13-summary-of-all-scripts)

---

## 1. OVERVIEW

This is a **3D action combat game** (similar to soulslike/Dark Souls genre) built in Unity. The combat system is built on a custom **Gameplay Ability System (GAS)** inspired by Unreal Engine 4's GAS. The system features:

- **Combo-based melee attacks** (5-step combo chain)
- **Parry/Block defense system** with guard break mechanics
- **Dash ability** costing stamina (Resistance attribute)
- **Ranged attacks** (bows, projectiles, area effects)
- **Lock-on targeting** system
- **Enemy AI** with Behavior Trees and multiple enemy types
- **Boss fights** with multi-phase progression
- **Enemy grouping** with token-based encounter system
- **Time-slow effects** on parries
- **Camera shake** feedback
- **Invincibility frames** (i-frames) on dodge

---

## 2. ARCHITECTURE OVERVIEW

The combat system follows a **layered architecture**:

┌─────────────────────────────────────────────────────┐ │ INPUT / CONTROLS │ (StarterAssets) ├─────────────────────────────────────────────────────┤ │ PlayerAbilityController │ (Input → Abilities) ├─────────────────────────────────────────────────────┤ │ GameplayAbilitySystem (GAS) Core │ ── LAYER 1: Core Framework │ ── GameplayAbility (ScriptableObject) │ │ ── GameplayAbilitySpec (Runtime instance) │ │ ── AbilityHolder (Manages all abilities) │ │ ── AttributeSystem (Stats management) │ │ ── GameplayEffect / GameplayEffectManager │ ├─────────────────────────────────────────────────────┤ │ Combat Implementation │ ── LAYER 2: Combat Logic │ ── PlayerWeaponController / PlayerWeaponDamage │ │ ── DefenseState (Guard states, hit resolution) │ │ ── KnockBack, LockOn, MakeInvincible │ │ ── EnemyWeaponController / EnemyWeaponDamage │ ├─────────────────────────────────────────────────────┤ │ UI / Feedback │ ── LAYER 3: Presentation │ ── PlayerHealth, PlayerStamina, PlayerAbilities │ │ ── CameraShakeManager, TimeEffectsManager │ │ ── VibrationManager, CharacterSFX/VFX │ └─────────────────────────────────────────────────────┘


---

## 3. CORE FOUNDATION: GAMEPLAY ABILITY SYSTEM (GAS)

### 3.1 GameplayAbility (Base Class)
**File:** `GameplayAbilitySystem/Scripts/BaseAbility/GameplayAbility.cs`

An **abstract ScriptableObject** that defines what an ability IS. Each concrete ability (LightAttack, Dash, Parry, etc.) inherits from this.

**Properties:**
| Property | Type | Description |
|----------|------|-------------|
| `Id` | `int` | Hash of ability name (used as unique identifier) |
| `Name` | `string` | Display name |
| `icon` | `Sprite` | UI icon |
| `Cooldown` | `float` | Cooldown time in seconds |
| `ActiveTime` | `float` | How long the ability lasts |
| `isPasive` | `bool` | Whether it auto-activates |
| `equipable` | `bool` | Whether it can be equipped to a slot |
| `EffectsToSelf` | `List<GameplayEffect>` | Effects applied to self on activation |

**Key Methods:**
- `ActivateAbilityEffects(owner)` - Applies all EffectsToSelf to the owner

### 3.2 GameplayAbilitySpec (Runtime Instance)
**File:** `GameplayAbilitySystem/Scripts/BaseAbility/GameplayAbilitySpec.cs`

The **runtime wrapper** around a GameplayAbility. Each ability on a character gets one Spec.

**Responsibilities:**
- Tracks **cooldown state** (bOnCooldown, cooldownTimer)
- Calls `ActivateAbility()` which triggers effects
- Tracks ability end via `OnAbilityEnded` event
- `Tick(dt)` - decrements cooldown each frame

**Key Methods:**
- `CanActivateAbility()` - Override in subclasses for custom activation rules
- `ActivateAbility()` - Starts the ability, applies effects, starts cooldown
- `CancelAbility()` - Cancels ability and clears cooldown
- `EndAbility()` - Fires `OnAbilityEnded` event

**Concrete Implementations (18 abilities):**

| Ability | Spec Class | Purpose |
|---------|-----------|---------|
| `LightAttack` | `LightAttackSpec` | Combo-based sword attacks |
| `Dash` | `DashSpec` | Stamina-consuming dodge roll |
| `Parry` | `ParrySpec` | Guard/parry system |
| `CounterAttack` | `CounterAttackSpec` | Backstab counter-attacks |
| `BarbarianAttack` | `BarbarianAttackSpec` | Enemy barbarian melee |
| `BowAttack` | `BowAttackSpec` | Archery attacks |
| `ArcherDash` | `ArcherDashSpec` | Archer dodge |
| `DamageProyectile` | `DamageProyectileSpec` | Projectile attacks |
| `HealPlayer` | `HealPlayerSpec` | Self-heal |
| `HealTarget` | `HealTargetSpec` | Heal ally |
| `FireAttack` | `FireAttackSpec` | Fire magic |
| `ElectroAttack` | `ElectroAttackSpec` | Electric magic |
| `SleepProyectile` | `SleepProyectileSpec` | Sleep magic |
| `Teleport` | `TeleportSpec` | Teleportation |
| `LightAura` | `LightAuraSpec` | Passive aura |
| `TrapBubble` | `TrapBubbleSpec` | Trap ability |
| `MageCounterAttack` | `MageCounterAttackSpec` | Mage counter |
| `GodSpaceAttack` | `GodSpaceAttackSpec` | God-class attack |

### 3.3 AbilityHolder
**File:** `GameplayAbilitySystem/Scripts/BaseAbility/AbilityHolder.cs`

The **central manager** for all abilities on a character.

**Data Structures:**
- `List<GameplayAbility> abilities` - All defined abilities (from inspector)
- `Dictionary<int, GameplayAbilitySpec> specs` - All runtime specs, keyed by ability ID
- `GameplayAbility abilitySlot1, abilitySlot2` - Equipped abilities (2 slots)
- `List<GameplayAbilitySpec> ActiveSpecs` - Currently active abilities

**Key Methods:**
- `ActivateAbility(abilityName, owner)` - Activate any ability
- `ActivateEquippedAbility(slot, owner)` - Activate equipped ability from slot 1-2
- `EquipAbility(ability, slot)` - Equip to slot 1 or 2
- `UnequipAbility(slot)` - Remove from slot
- `CancelAbility(abilityName)` - Cancel specific ability
- `CancelAllAbility()` - Cancel all active abilities
- `IsAbilityActive(abilityName)` - Check if ability is running
- `HasAbility(abilityName)` - Check if character knows the ability

**Events:**
- `OnEquipAbility(int slot)` - Fired when ability equipped
- `OnUnequipAbility(int slot)` - Fired when ability unequipped
- `OnActivateEquipAbility(int slot, GameplayAbilitySpec spec)` - Fired when equipped ability used

### 3.4 AttributeSystem
**File:** `GameplayAbilitySystem/Scripts/Attribute/AttributeSystem.cs`

Manages all **stats/attributes** of an entity (health, resistance, etc.).

**Data Structures:**
- `List<GameplayAttributeInstance> attributes` - All attributes
- `Dictionary<string, GameplayAttributeInstance> _lookup` - Fast lookup by name
- `List<GameplayAttributeInstance> _recoveredAttributes` - Attributes that regenerate

**Key Methods:**
- `AddModifier(name, id, modifier, value, canResponse, duration)` - Add buff/debuff
- `RemoveModifier(name, id, canResponse)` - Remove buff/debuff
- `GetAttributeValue(name)` - Get current value
- `GetAttributeMaxValue(name)` - Get maximum value
- `ApplyEffectOnce(effect, notify)` - Apply permanent effect
- `CaptureValues()` / `ApplyValues(values)` - Save/restore state

### 3.5 GameplayAttributeData
**File:** `GameplayAbilitySystem/Scripts/Attribute/GameplayAttributeData.cs`

ScriptableObject defining an attribute's **configuration**:

| Property | Description |
|----------|-------------|
| `Name` | Unique identifier (e.g., "PlayerHealth", "Resistance") |
| `BaseValue` | Starting value |
| `MaxValue` | Maximum cap |
| `CanRecover` | Whether it regenerates over time |
| `RecoverValuePerSecond` | Regen rate |
| `WaitTillRecover` | Delay before regen starts |
| `Response` | AttributeResponse (reactive callback) |

### 3.6 GameplayAttributeInstance
**File:** `GameplayAbilitySystem/Scripts/Attribute/GameplayAttributeInstance.cs`

The **runtime instance** with actual values and modifiers.

**Calculation Formula:**
CurrentValue = Clamp((RuntimeBaseValue + SumAdds) * ProductMult, 0, MaxValue)

Override modifiers take precedence if they're the most recent.


**Modifier Types:**
| Type | Symbol | Behavior |
|------|--------|----------|
| `Add` | += | Adds to base value |
| `Multiply` | *= | Multiplies base value |
| `Override` | = | Replaces all, takes highest timestamp |

### 3.7 GameplayEffect System
**Files:** `GameplayAbilitySystem/Scripts/Effect/GameplayEffect*.cs`

**GameplayEffect** (ScriptableObject) - Defines what effect to apply:
| Property | Description |
|----------|-------------|
| `TargetAttribute` | Which attribute to modify |
| `Value` | Amount to modify by |
| `Modifier` | Add/Multiply/Override |
| `EffectTime` | Instant/Infinite/Duration |
| `Duration` | How long (for Duration effects) |
| `CanAttributeResponse` | Whether to trigger response callbacks |

**GameplayEffectManager** - Tracks all active effects on an entity and ticks them.

### 3.8 AttributeResponse System
**Files:** `GameplayAbilitySystem/Scripts/AttributeResponses/*.cs`

Reactive system that fires when attributes change.

**Inheritance Hierarchy:**
AttributeResponse (abstract) └── GeneralHealthResponse (base health logic) ├── PlayerHealthResponse (player death/hurt) └── EnemyHealthResponse (enemy death/hurt) └── ResistanceResponse (stamina/resistance changes) └── BossHealthResponse (boss-specific)


**PlayerHealthResponse triggers:**
- **Hurt:** Screen flash, VFX, SFX, controller vibration, knockback
- **Death:** Death animation, music transition, lock-on clear
- **Over Health:** Visual feedback on healing past base

**EnemyHealthResponse triggers:**
- **Hurt:** AI state change to "Hurt", knockback, VFX/SFX, health bar update
- **Death:** Death animation, remove from group, Karma change
- **Over Health:** Health bar update

---

### P1.1-notes: latent shared-Names data issue (found during Milestone B)

The GAS identifies an ability by its `Name`, hashed via `Animator.StringToHash(Name)` into `Id`
(seen in `GameplayAbility.Id` and `OnValidate`). All 24 runtime Names (the registry in
`AbilityIds.cs`) are accounted for. However, **some runtime Names are genuinely reused** by
more than one ability asset:

| Shared runtime Name | Assets that use it |
|---------------------|--------------------|
| `CounterAttack`     | `CounterAttackBarb.asset`, `MageCounterAttack.asset`, `CounterSpaceGod.asset` |
| `FireAttack`        | `FireAttack.asset`, `FireAttackBoss.asset` |
| `ElectroAttack`     | `ElectroAttack.asset`, `ElectroAttackBoss.asset` |
| `Teleport`          | `Teleport.asset`, `TeleportChangePhase.asset` |
| `Dash`              | `Dash.asset` (player), `ArcherDash.asset` (all archer variants) |

**Why this is OK today:** abilities with the same Name share an `Id`, so they work as long as
each lives in a **different `AbilityHolder`** (which is how the game already uses them — a
Barbarian holder never coexists with a Mage holder). The shared-name warnings in `AbilityIds.cs`
flag this on purpose.

**Why this is the core risk for P1.1:** because identity = the string `Name`/`Id`, **renaming**
any of these assets silently breaks every string reference (the old hash no longer matches the
new Id). Milestone B doesn't fix that yet — it only makes *existing* collisions and *null*
entries **fail loudly instead of silently** (see `AbilityHolder.Awake` + `ActivateAbility`).

**Follow-up (not done yet):** (a) migrate the remaining magic-string call sites
(player specs in `LightAttack.cs`, AI `AttackDefinition`/Action nodes, boss data) to
`AbilityIds.*`; (b) eventually make the inspector `Name` derive from the registry (compile-time
Id) so renames can no longer silently break — this is P1.1's higher-effort follow-up in
`SYSTEMS_REVIEW_PLAN.md`.

> **Audit note (11/8/2026):** the table above was re-verified against the live `AssetDatabase`
> (`t:GameplayAbility`): **30 ability assets → 24 distinct Names**, and all 5 shared Names above
> are the only collisions. All **38** prefabs holding an `AbilityHolder` were scanned: none puts
> two same-Id abilities in one holder and none has a null entry — so the new `Awake` validation
> is silent on shipped content (no false positives).

---

### P1.2-notes: the shared `DamageResolver` hot path (Milestone C, 11/8/2026)

**File:** `Assets/Scripts/Combat/DamageResolver.cs` — stateless `static class`, no MonoBehaviour,
no state, nothing to place in a scene.

**API surface**

| Member | Purpose |
|---|---|
| `enum Outcome` | `Applied`, `MissingTargetComponents`, `InvalidComboIndex`, `NullEffect` |
| `struct Defender` | `GameplayEffectManager Effects` + `AttributeSystem Attributes`, with `IsValid` so a half-resolved defender can't be passed on |
| `struct Rumble` | `LowFrequency` / `HighFrequency` / `Duration` — maps 1:1 onto `VibrationManager.Vibrate(...)` |
| `class ComboHit` | parameter object: `Attacker`, `Target`, `ComboIndex`, `NormalEffects`, `BlockedEffects`, `UseBlockedEffects`, `OnBlocked`, `SearchTargetInParent`, `Shake`, `ImpulseSource`, `Haptics` |
| `TryResolveDefender(target, searchInParent, attacker, out Defender)` | the P0.2 guard, in one place |
| `TrySelectEffect(ComboHit, out GameplayEffect, out Outcome)` | the P0.1 guard (normal **and** block list) + null-slot guard |
| `PlayShake(CameraShakeData?, CinemachineImpulseSource)` / `PlayRumble(Rumble?)` | null-safe feedback |
| `ApplyHit(ComboHit)` / `ApplyResolved(ComboHit, Defender)` | full pipeline / entry point when the caller already resolved the defender |

**Two gotchas that cost time — remember these:**
1. `CameraShakeData` is a `[Serializable] struct`, **not** a ScriptableObject. "No shake" must be
   `CameraShakeData?` (null), never `== null`, and it has no `.name`.
2. A nested type and a method cannot share a name in one class, hence `struct Rumble` +
   `PlayRumble(...)` (not `Rumble(...)`), and `PlayShake` rather than `Shake`.

**Intentional behaviour delta (the only one) — flag to the designer:**
previously the player's combo camera shake fired *before* the null-effect check, so a combo step
with an unassigned effect slot still shook the camera while dealing no damage. `ApplyResolved`
now plays shake/rumble only when the hit will actually apply. On correctly configured data this
is identical; it only changes the misconfigured case. `EnemyWeaponDamage` is untouched here — it
still shakes first, before its counter branch, exactly as before.

**Pre-existing asymmetries deliberately PRESERVED** (unifying them is a game-feel decision, not a
refactor — raise with the designer rather than "fixing" them silently):
- The enemy applies **no** controller rumble on a landed hit; the player applies
  `(0.6, 0.6, 0.2)`. Player counter-attack rumble is `(0.3, 1.0, 0.2)`.
- The player's counter branch calls `effectManager.ApplyEffect(CounterEffect, attributeSystem)`
  using the **player's own** components (resolved from `owner` in `Awake`), i.e. the counter
  effect lands on the player, while the enemy's counter effect lands on the target. Suspicious
  but left as-is.
- Enemy shakes with a single `CameraShakeData` for all combo steps; the player maps
  combo 0–3 → `attack1`–`attack4` and has no shake on step 5.
- Component lookup depth differs: enemy hits the player capsule directly (`GetComponent`), the
  player hits a child collider (`GetComponentInParent`) — now the `SearchTargetInParent` flag.
- Only the enemy has a block-effect list + `BlockHit` animator trigger (passed in as the
  `OnBlocked` callback so it still fires *after* the index guard, exactly where it used to).

**Verification harness** (re-runnable via the Unity MCP `execute_code`, edit mode, no Play needed):
loads 6 real `GameplayEffect` assets, builds synthetic attacker/defender `GameObject`s and
asserts 15 cases — valid index, `-1`, `== Count`, the P0.1 cross-list case (normal list 5 / block
list 3 at index 4 must report `InvalidComboIndex`), null slot, null list, a BLOCKED hit with no
blocked-effects list (must drop with `InvalidComboIndex` instead of NRE), `OnBlocked` firing only
when blocked and never on an invalid index, `PlayShake(null)`/`PlayRumble(null)` not throwing,
`ApplyHit(null)` / `ApplyHit(unresolvable)` returning `MissingTargetComponents`, and `Label(null)`.
All 15 green on 11/8/2026 with a 0-error / 0-warning compile. **The player and enemy weapon paths
still need a manual Play-mode pass** (real combos, block, parry, counter) — that part cannot be
automated here.

---

## 4. PLAYER COMBAT COMPONENTS

### 4.1 LightAttack / LightAttackSpec (Core Combo System)
**Files:** `NewAbilities/LightAttack.cs`, `NewAbilities/LightAttackSpec.cs`

**Most complex ability** - implements the full melee combo system.

**Combo Flow:**
Player presses Light Attack ↓ Check combo timing window (0.3s) ↓ (within window) Next combo hit (0→1→2→3→4) ↓ (outside window) New combo starts (resets to 0) ↓ No more input → End combo


**Combo Window Mechanism:**
- Animation events fire: `ComboWindowOpen()` → combat → `ComboWindowClose()`
- During window, player can buffer next input
- `bufferedInput` allows queuing the next hit

**Attack Assist (Auto-aim):**
- When combo ends with direction held toward enemy, character **steps toward target**
- Uses `Physics.BoxCast` rectangle in front of player
- Parameters: `ForwardDistanceRay = 6f`, `LateralDistanceRay = 40f`
- Rotates toward target at `RotationSpeed = 0.15`
- Moves at `AttackAssistSpeed = 0.15` units/frame

**Animation Events (called from animator):**
| Event | Action |
|-------|--------|
| `ComboWindowOpen()` | Allow next input |
| `ComboWindowClose()` | Stop accepting input |
| `CanHaveInput()` | Execute buffered input or end combo |
| `DisableInput()` | Disable movement input |
| `EnableSwordCollider()` | Enable hitbox, increment combo index, play SFX |
| `DisableSwordCollider()` | Disable hitbox |
| `StartAttackAssist()` | Begin moving toward target |
| `EndAttackAssist()` | Stop moving |

### 4.2 Dash / DashSpec
**File:** `NewAbilities/Dash.cs`, `NewAbilities/DashSpec.cs`

**Stamina-consuming dodge roll:**
- Checks: `Resistance >= resistanceNeeded` (from GameplayEffect)
- Checks: No other abilities active
- Makes player **invincible** during dash (i-frames)
- Direction based on movement input relative to camera
- Has **safety timeout** - ends after `dashTime` even if not finished
- Vibration and SFX feedback

### 4.3 Parry / ParrySpec
**File:** `NewAbilities/Parry.cs`, `NewAbilities/ParrySpec.cs`

**Two-phase defense:**
Player holds guard button ↓ PHASE 1: Parry Window (0.2s) - Character faces lock-on target - If hit during this window → PERFECT PARRY! ↓ (if not hit) PHASE 2: Block (held) - Reduces damage - Drains Resistance (stamina) - If Resistance = 0 → GUARD BREAK


**Animation States:**
- `Blocking = true` during guard
- `BlockHit` on being hit while blocking
- `ParrySuccess` on perfect parry
- `GuardBreak` when stamina depleted

### 4.4 PlayerWeaponController / PlayerWeaponDamage
**Files:** `Player/PlayerWeaponController.cs`, `Player/PlayerWeaponDamage.cs`

**PlayerWeaponController** extends `WeaponController`:
- Manages weapon parenting (hip ↔ hand)
- Enables/disables weapon collision
- Handles counter-attack chance (default 50%)

**PlayerWeaponDamage** - Hitbox and damage application:
- **Sweep attack detection:** Uses `Physics.OverlapBox` to detect enemies
- Checks each hit against `alreadyHit` set (prevents multi-hit)
- **Combo effects:** Each combo step (0-4) has different `GameplayEffect`
- **Counter-attack check:** If enemy is counter-attacking and player hits first → counter-attack applied to player
- **Camera shake:** Each combo step has different shake intensity
- **Vibration feedback:** Different intensity per hit type

**Camera Shake Types:**
| State | Description |
|-------|-------------|
| `attack1-4` | Player attack hits (increasing intensity) |
| `hitShield` | Enemy shield block |
| `gotBlocked` | Player attack blocked |

### 4.5 DefenseState
**File:** `Player/DefenseState.cs`

Central **state machine for defense resolution**:

**Guard States:**
| State | Description |
|-------|-------------|
| `Idle` | Not guarding |
| `Parry` | Perfect parry window open |
| `Block` | Holding guard (draining stamina) |
| `Recovery` | After guard break |

**Hit Resolution (`ResolveHit()`):**
| Current State | Result | Effect |
|--------------|--------|--------|
| `Parry` | `PerfectParry` | Time slow, counter VFX, trigger parry spec, cancel attacker |
| `Block` + Resistance > 0 | `Blocked` | Reduced damage, drain stamina |
| `Block` + Resistance = 0 | `Blocked` + Guard Break | Full guard break animation |
| `Idle` | `FullDamage` | Take full damage |

### 4.6 LockOn
**File:** `Player/LockOn.cs`

**Auto-aim targeting system:**
- Detects enemies within `detectionRadius` (default 15)
- Filters by: Tag, visibility on screen
- Picks **nearest enemy to screen center**
- Visual VFX on target acquisition
- Smooth rotation to lock-on target
- Auto-relock when target dies or moves out of range

**Screen visibility check:** Uses viewport coordinates (x: 5%-95%, y: 5%-95%, z > 0)

### 4.7 Other Player Combat Components
| Component | File | Purpose |
|-----------|------|---------|
| `MakeInvincible` | `Player/MakeInvincible.cs` | Simple bool flag for i-frames |
| `KnockBack` | `Combat/KnockBack.cs` | Knockback animation + invincibility frames |
| `RootMotionController` | `Combat/RootMotionController.cs` | Enable/disable animator root motion |
| `PlayerAbilityController` | `BaseAbility/PlayerAbilityController.cs` | Input → Ability activation bridge |
| `AbilityAnimationBridge` | `BaseAbility/AbilityAnimationBridge.cs` | Animator events → Spec methods |
| `AbilityDataBasePlayer` | `BaseAbility/AbilityDataBasePlayer.cs` | Centralized ability lookup by ID |

---

## 5. ENEMY COMBAT COMPONENTS

### 5.1 EnemyWeaponController / EnemyWeaponDamage
**Files:** `Enemies/Combat/EnemyWeaponController.cs`, `Enemies/Combat/EnemyWeaponDamage.cs`

**EnemyWeaponController** extends `WeaponController`:
- References `EnemyWeaponDamage` component
- `CounterAttack(bool)` - marks enemy as counter-attacking

**EnemyWeaponDamage** - Enemy hitbox logic:
- On trigger enter vs Player:
  1. Check `MakeInvincible` (skip if invincible)
  2. Call `DefenseState.ResolveHit()` → gets result
  3. Based on result:
     - **PerfectParry:** Enemy gets parried (applies `GetParriedEffect` to enemy)
     - **Blocked:** Apply `gameplayEffectsBlock[ComboNumber]` (reduced damage)
     - **FullDamage:** Apply `gameplayEffects[ComboNumber]` (full damage)
  4. Each combo hit has different effects
- Counter attacks apply `counterEffect` instead of combo effect
- Camera shake on hit

### 5.2 Enemy Types and Their Abilities
Each enemy type has specific GameplayAbility implementations:

| Enemy | Abilities | File |
|-------|-----------|------|
| **Barbarian** | Melee combo attacks | `BarbarianAttack` |
| **Archer** | Bow attacks, dash | `BowAttack`, `ArcherDash` |
| **Shielder** | Shield bash, shield charge | (uses ShieldController) |
| **Mage** | Fire, lightning, sleep projectiles, counter | `FireAttack`, `ElectroAttack`, `SleepProyectile`, `MageCounterAttack` |
| **God** | Space attacks, area ray attacks | `GodSpaceAttack`, `AreaRayAttack` |

### 5.3 Projectile Damage Components
| Component | File | Purpose |
|-----------|------|---------|
| `PlayerDamageProyectile` | `Enemies/Combat/PlayerDamageProyectile.cs` | Player projectile vs enemies |
| `ArrowMovement` | `Enemies/Combat/ArrowMovement.cs` | Arrow physics/movement |
| `ArrowTrayectory` | `Enemies/Combat/ArrowTrayectory.cs` | Arrow arc trajectory |
| `MoveFireBall` | `Enemies/Combat/MoveFireBall.cs` | Fireball movement |
| `ExplosionDamage` | `Enemies/Combat/ExplosionDamage.cs` | Area damage over time |
| `LightingDamage` | `Enemies/Combat/LightingDamage.cs` | Instant lightning hit |

### 5.4 Enemy AI Integration
**File:** `Enemies/EnemyGraphBridge.cs`

Bridges **Unity Behavior (Behavior Graph AI)** with combat:

| Method | Effect |
|--------|--------|
| `SetHurt()` | Sets AI state to `BarbarianState.Hurt` |
| `SetVulnerable()` | Sets `AttackingState.Vulnerable` (enemy can be attacked) |
| `SetTrap()` | Sets AI state to `BarbarianState.Trap` |
| `SetDetection()` | Sets AI state to `BarbarianState.Detection` + alert |
| `SetChangePhase()` | Triggers boss phase transition |
| `SetDeath()` | Sets death state, fires `OnDeath` event, disables components |

**Static Event:** `OnDeath(GameObject)` - Fired when any enemy dies

### 5.5 Enemy Death
**File:** `Enemies/Combat/DeathCheck.cs`

- Checks if not already dead
- Calls `EnemiesGroupContoller.ReturnToken()` and `EnemyHasDie()`
- `EnemyHealthResponse.DeathBehavior()` handles the rest:
  - Plays death SFX
  - Calls `EnemyGraphBridge.SetDeath()`
  - Adds karma (-10)
  - Destroys gameobject after 2s

### 5.6 Enemy Shield
**File:** `Enemies/Combat/EnemyShieldController.cs`

Extends `WeaponController` with shield-specific behavior for shielder enemies.

### 5.7 Slot System (Positioning)
**Files:** `Enemies/Combat/Slot.cs`, `Enemies/Combat/SlotUpdate.cs`

**Slot:** Position-based positioning for enemies around the player:
| Property | Type | Description |
|----------|------|-------------|
| `position` | `Vector3` | World position |
| `reservedBy` | `GameObject` | Which enemy claims this slot |
| `IsAvailable()` | `bool` | Whether slot is free |
| `GetAngle(playerPos)` | `float` | Angle from player |

**SlotType enum:** `Close`, `Mid`, `Far` - Different distances from player

**SlotUpdate:** Periodically updates slot positions (every `timeToUpdate` seconds, default 10s)

---

## 6. DEFENSE SYSTEM (Player)

The defense system is centered around the **DefenseState** component with the **ParrySpec** as its controller.

### 6.1 Guard States State Machine
     ┌──────────────────────────────────────┐
     │           GuardState.Idle             │
     │   (Not guarding, free to act)         │
     └──────────┬────────────────────────────┘
                │ Press Guard Button
                ↓
     ┌──────────────────────────────────────┐
     │         GuardState.Parry              │
     │   (0.2s perfect parry window)         │
     │   → If hit → PERFECT PARRY            │
     │   → If time expires → Block           │
     └──────────┬────────────────────────────┘
                │ Time expires
                ↓
     ┌──────────────────────────────────────┐
     │          GuardState.Block             │
     │   (Held guard, drains stamina)        │
     │   → If hit + stamina > 0 → Block      │
     │   → If hit + stamina = 0 → GuardBreak │
     │   → Release → Idle                    │
     └──────────┬────────────────────────────┘
                │ Stamina depleted
                ↓
     ┌──────────────────────────────────────┐
     │        GuardState.Recovery            │
     │   (Vulnerable after guard break)      │
     └──────────────────────────────────────┘

### 6.2 Hit Resolution
Enemy attacks → Player hitbox triggers ↓ DefenseState.ResolveHit() ↓ ┌─────────────────────┐ │ Current Guard State │ └────────┬────────────┘ │ ┌─────────────┼─────────────┐ ↓ ↓ ↓ Parry Block Idle │ │ │ ↓ ↓ ↓ PerfectParry Blocked FullDamage (Time slow) (Reduced) (Full)


### 6.3 Perfect Parry Effects
When a perfect parry occurs:
1. **Time slow:** `TimeEffectsManager.DoParrySlowMotion(scale=0.05, duration=0.1s, recover=0.15s)`
2. **Camera shake:** Counter-specific camera shake
3. **VFX:** Counter visual effect
4. **SFX:** Counter sound effect
5. **Controller vibration:** Strong vibration (1.0, 1.0, 0.2)
6. **Notify ParrySpec:** Triggers `NotifyPerfectParry()` animation

---

## 7. DAMAGE FLOW

### 7.1 Player → Enemy Damage
Player presses Light Attack ↓ LightAttackSpec.ActivateAbility() ↓ Animation event: EnableSwordCollider() ↓ PlayerWeaponDamage.ActivateHitbox() ↓ Physics.OverlapBox (sweep detection) ↓ For each enemy hit: → Check MakeInvincible (skip if true) → Check if already hit (skip if yes) → Get counter chance (50% default) → If enemy counter-attacking: → Apply CounterEffect to player → Else: → Apply gameplayEffects[ComboNumber] to enemy → Camera shake → Vibration → VFX on enemy (CharacterVFX.Damaged) → SFX on enemy (CharacterSFX.Damaged) ↓ GameplayEffectManager.ApplyEffect() ↓ AttributeSystem.AddModifier() ↓ AttributeValue recalculated ↓ AttributeResponse.OnAttributeChanged() ↓ EnemyHealthResponse.HurtBehavior(): → EnemyGraphBridge.SetHurt() → KnockBack.StartKnockBack() → EnemyUIBridge.ShowHealth() → EnemyHealthBar updates


### 7.2 Enemy → Player Damage
Enemy attacks → hitbox triggers ↓ EnemyWeaponDamage.OnTriggerEnter(Player) ↓ Check MakeInvincible (skip if true) ↓ DefenseState.ResolveHit() ↓ Result: ├── PerfectParry → Enemy gets parried (GetParriedEffect) ├── Blocked → Apply gameplayEffectsBlock[ComboNumber] to player └── FullDamage → Apply gameplayEffects[ComboNumber] to player ↓ Camera shake (CameraShakeManager) ↓ GameplayEffectManager.ApplyEffect() ↓ AttributeSystem.AddModifier() ↓ PlayerHealthResponse.HurtBehavior(): → Controller vibration → CharacterVFX.Damaged → CharacterSFX.Damaged → PlayerHealthResponse.OnPlayerHurt (UI flash) → KnockBack.StartKnockBack() ↓ PlayerHealth.fillAmount updates ↓ If health = 0: → PlayerHealthResponse.DeathBehavior() → Death animation → Music transition → Show DeathMenu


### 7.3 GameplayEffect Application Flow
GameplayEffectManager.ApplyEffect(effect, targetAttributeSystem) ↓ Effect.EffectTime check: ├── Instant → Apply once, immediately end ├── Infinite → Apply permanent modifier └── Duration → Create GameplayEffectInstance, tick until done ↓ GameplayEffectInstance.Apply(): → Target.AddModifier(attributeName, effectId, modifier, value, canResponse, duration) ↓ AttributeSystem.AddModifier(): → GameplayAttributeInstance.AddModifier(): → Add to appropriate dictionary (add/mult/override) → RecalculateCurrent() → Formula: Clamp((RuntimeBaseValue + adds) * mult, 0, MaxValue) → If notify: Data.Response.OnAttributeChanged()


---

## 8. BOSS COMBAT

### 8.1 BossPhase Enum
```csharp
enum BossPhase { Phase1, Phase2, Phase3 }

8.2 PhaseController
File: Enemies/Boss/PhaseController.cs

Monitors boss health and triggers phase changes:

Phase 1 → 2: At 66% health (LifeForPhase2 = 0.66f)
Phase 2 → 3: At 33% health (LifeForPhase3 = 0.33f)
Flow:

OnEnemyHurt event
    ↓
Calculate health ratio
    ↓
Determine new phase
    ↓
If phase changed:
    → CurrentPhase = newPhase
    → EnemyGraphBridge.SetChangePhase()
    → AI Behavior Graph updates
8.3 AttackDirector
File: Enemies/Boss/AttackDirector.cs

Selects which attack sequence to use:

Reads AttackLibrary for current phase
Uses weighted random selection
Repeat penalty: Last-used sequence gets 0.5x weight (reduces repetition)
8.4 AttackLibrary & AttackSequences
Files: GameplayAbilitySystem/Scripts/AttackDefinitions/

AttackLibrary: Contains arrays of sequences per phase (phase1Sequences, phase2Sequences, phase3Sequences)
AttackSequences: A sequence is an array of AttackDefinition steps
AttackDefinition: Maps an ability name + kind (ComboPhysical, IndividualPhysical, Magical, Teleport)
Data structure:

AttackLibrary
  ├── phase1Sequences[]
  │     ├── AttackSequence (weight: 1.0)
  │     │     └── AttackDefinition[ability: "LightAttack", kind: ComboPhysical]
  │     │           AttackDefinition[ability: "FireAttack", kind: Magical]
  │     └── AttackSequence (weight: 0.5)
  ├── phase2Sequences[]
  └── phase3Sequences[]
9. ENEMY GROUP SYSTEM
9.1 EnemiesGroupContoller
File: Enemies/EnemiesGroupContoller.cs

Manages multi-enemy encounters with token system:

Token System:

maxTokens (default: 2) - Maximum concurrent enemies that can act
currentToken - Decrements when enemy requests, increments when enemy dies
RequestToken() - Returns true if token available
ReturnToken() - Called on enemy death
Group States:

GroupState.Group - Multiple enemies (default)
GroupState.Alone - Single enemy
Events:

OnHideEnemyHud(List<GameObject>) - Hide all enemy health bars
OnBattleStateChanged(bool) - True when battle starts, false when all enemies dead
Battle Flow:

AlertGroup(alerter) called
    ↓
OnBattleStateChanged(true)
    ↓
MusicManager.StartBattle()
    ↓
All enemies SetDetection()
    ↓
Enemies fight (tokens limit concurrent action)
    ↓
Enemy dies:
    → ReturnToken()
    → EnemyHasDie()
    → CheckBattleOver()
    ↓
If all dead:
    → BattleOver()
    → MusicManager.EndBattle()
    → OnHideEnemyHud()
    → OnBattleStateChanged(false)
10. CAMERA & FEEDBACK SYSTEMS
10.1 CameraShakeManager
File: Managers/CameraShakeManager.cs

Singleton that controls Cinemachine impulse-based camera shake:

Uses CinemachineImpulseSource and CinemachineImpulseListener
CameraShake(CameraShakeData, impulseSource) - Apply shake with parameters
10.2 CameraShakeData
File: Combat/CameraShakeData.cs

struct CameraShakeData {
    // Impulse Source (emitter)
    float impactTime;       // Duration of shake
    float impactForce;      // Strength of shake
    Vector3 defaultVelocity;
    AnimationCurve impulseCurve;
    
    // Impulse Listener (receiver - camera)
    float ListenerAmplitude;
    float ListenerFrequency;
    float ListenerDuration;
}
10.3 TimeEffectsManager
File: Managers/TimeEffectsManager.cs

Controls Time.timeScale for slow-motion effects:

PauseGame() / ResumeGame() - Full pause
DoParrySlowMotion(scale, duration, recoverDuration) - Parry slow-mo
Slow-mo flow:

timeScale = scale (0.05)  // Very slow
    ↓
Wait duration (0.1s real-time)
    ↓
Lerp timeScale back to 1.0 over recoverDuration (0.15s)
10.4 VibrationManager
File: Managers/VibrationManager.cs

Singleton for controller rumble:

Vibrate(magnitude, frequency, duration) - Controller vibration
10.5 CharacterSFX / CharacterVFX
Files: CharacterSFX.cs, CharacterVFX.cs

Play sound/VFX effects based on SFXType / VFXType enums with:

Random pitch variation
Volume control
3D spatial positioning (attached to game object)
11. UI/HUD COMPONENTS
11.1 Player UI
| Component | File | Purpose | |-----------|------|---------| | PlayerHealth | UI/PlayerHealth.cs | Health bar fill amount, low health SFX | | PlayerDamage | UI/PlayerDamage.cs | Red screen flash on damage | | PlayerStamina | UI/PlayerStamina.cs | Resistance (stamina) bar with recovery indicator | | PlayerAbilities | UI/PlayerAbilities.cs | Ability slot icons + cooldown countdown | | PlayerUIController | UI/PlayerUIController.cs | Menu navigation (Pause, Abilities, Death) |

11.2 Enemy UI
| Component | File | Purpose | |-----------|------|---------| | EnemyHealthBar | UI/EnemyHealthBar.cs | Health bar + resistance bar (floating above enemy) | | EnemyUIBridge | UI/EnemyUIBridge.cs | Show/hide health bars, connected to group controller |

11.3 Health Bar Details
Player:

Fill amount = currentHealth / maxHealth
Low health effect at 10% threshold (screen vignette + SFX)
Red flash on damage (subscribe to PlayerHealthResponse.OnPlayerHurt)
Enemy:

Health bar + overfill bar (healing past base)
Resistance bar (shown when enemy is vulnerable)
Follows enemy in world space, faces camera
Auto-hides when battle ends
12. COMPLETE DATA FLOW DIAGRAM
┌─────────────────────────────────────────────────────────────────────┐
│                        INPUT LAYER                                   │
│  ThirdPersonController + StarterAssetsInputs                        │
│  (move, look, attack, dash, guard, lock-on)                         │
└───────────────────────┬─────────────────────────────────────────────┘
                        ↓
┌─────────────────────────────────────────────────────────────────────┐
│                    ABILITY TRIGGER                                   │
│  PlayerAbilityController.ActivateAbility("LightAttack")             │
│  PlayerAbilityController.ActivateEquippedAbility(slot)              │
└───────────────────────┬─────────────────────────────────────────────┘
                        ↓
┌─────────────────────────────────────────────────────────────────────┐
│                    ABILITY HOLDER                                    │
│  AbilityHolder.ActivateAbility(id, owner)                           │
│  → Get GameplayAbilitySpec from dictionary                          │
│  → Check CanActivateAbility() (cooldown, requirements)              │
│  → Add to ActiveSpecs                                               │
│  → ability.ActivateAbility()                                        │
└───────────────────────┬─────────────────────────────────────────────┘
                        ↓
┌─────────────────────────────────────────────────────────────────────┐
│                    ABILITY SPEC (Concrete)                           │
│  LightAttackSpec.ActivateAbility()                                  │
│  → Check timing window                                              │
│  → StartCombo() / AdvanceCombo()                                    │
│  → Trigger animator (InCombo, Attack)                               │
│  → Disable input                                                    │
│  → Animation Events (via AbilityAnimationBridge):                   │
│     → EnableSwordCollider() → PlayerWeaponDamage.ActivateHitbox()   │
│     → DisableSwordCollider() → PlayerWeaponDamage.DeactivateHitbox()│
│  → EndAbility() → Remove from ActiveSpecs                           │
└───────────────────────┬─────────────────────────────────────────────┘
                        ↓
┌─────────────────────────────────────────────────────────────────────┐
│                    COLLISION / DAMAGE                                │
│  PlayerWeaponDamage.OnTriggerEnter(other):                          │
│  → Physics.OverlapBox (sweep)                                       │
│  → Already hit? Skip                                                │
│  → Counter chance? Apply counter effect                             │
│  → else: Apply GameplayEffect[ComboNumber]                          │
└───────────────────────┬─────────────────────────────────────────────┘
                        ↓
┌─────────────────────────────────────────────────────────────────────┐
│                    GAMEPLAY EFFECT                                   │
│  GameplayEffectManager.ApplyEffect(effect, targetAttrSystem)        │
│  → Create GameplayEffectInstance                                    │
│  → target.AddModifier(attributeName, modifier, value, ...)          │
└───────────────────────┬─────────────────────────────────────────────┘
                        ↓
┌─────────────────────────────────────────────────────────────────────┐
│                    ATTRIBUTE SYSTEM                                  │
│  AttributeSystem.AddModifier(name, id, modifier, value, ...)        │
│  → GameplayAttributeInstance.AddModifier()                          │
│  → RecalculateCurrent():                                            │
│     CurrentValue = Clamp((Base + Adds) * Mult, 0, Max)             │
│  → If notify: Data.Response.OnAttributeChanged()                    │
└───────────────────────┬─────────────────────────────────────────────┘
                        ↓
┌─────────────────────────────────────────────────────────────────────┐
│                    ATTRIBUTE RESPONSE                                │
│  PlayerHealthResponse / EnemyHealthResponse.OnAttributeChanged()    │
│                                                                     │
│  ON HURT:                                                           │
│  → VFX.Damaged()                                                    │
│  → SFX.Damaged()                                                    │
│  → KnockBack.StartKnockBack()                                       │
│  → OnPlayerHurt / OnEnemyHurt events                                │
│                                                                     │
│  ON DEATH:                                                          │
│  → Death animation                                                  │
│  → Music transition                                                 │
│  → Remove from group                                                │
│  → Destroy after delay                                              │
└───────────────────────┬─────────────────────────────────────────────┘
                        ↓
┌─────────────────────────────────────────────────────────────────────┐
│                    PRESENTATION                                      │
│  → UI updates (health bars, stamina, cooldowns)                     │
│  → Camera shake (CameraShakeManager)                                │
│  → Screen flash (PlayerDamage)                                      │
│  → Vibration (VibrationManager)                                     │
│  → Slow motion (TimeEffectsManager)                                 │
└─────────────────────────────────────────────────────────────────────┘
13. SUMMARY OF ALL SCRIPTS
GameplayAbilitySystem Core (18 files)
| Script | File Path | Role | |--------|-----------|------| | GameplayAbility | BaseAbility/GameplayAbility.cs | Abstract ability (ScriptableObject) | | GameplayAbilitySpec | BaseAbility/GameplayAbilitySpec.cs | Runtime ability wrapper | | AbilityHolder | BaseAbility/AbilityHolder.cs | Manages all abilities | | PlayerAbilityController | BaseAbility/PlayerAbilityController.cs | Input → Ability bridge | | AbilityAnimationBridge | BaseAbility/AbilityAnimationBridge.cs | Animator events → Specs | | AbilityDataBasePlayer | BaseAbility/AbilityDataBasePlayer.cs | Ability lookup table | | AttributeSystem | Attribute/AttributeSystem.cs | Stats management | | GameplayAttributeData | Attribute/GameplayAttributeData.cs | Attribute config | | GameplayAttributeInstance | Attribute/GameplayAttributeInstance.cs | Runtime attribute | | AttributeResponse | Attribute/AttributeResponse.cs | Abstract response | | GeneralHealthResponse | AttributeResponses/GeneralHealthResponse.cs | Base health response | | PlayerHealthResponse | AttributeResponses/PlayerHealthResponse.cs | Player death/hurt | | EnemyHealthResponse | AttributeResponses/EnemyHealthResponse.cs | Enemy death/hurt | | ResistanceResponse | AttributeResponses/ResistanceResponse.cs | Stamina response | | GameplayEffect | Effect/GameplayEffect.cs | Effect config | | GameplayEffectInstance | Effect/GameplayEffectInstance.cs | Runtime effect | | GameplayEffectManager | Effect/GameplayEffectManager.cs | Track active effects |

Player Combat (7 files)
| Script | File Path | Role | |--------|-----------|------| | LightAttack / LightAttackSpec | NewAbilities/LightAttack*.cs | Combo attacks | | Dash / DashSpec | NewAbilities/Dash*.cs | Dodge roll | | Parry / ParrySpec | NewAbilities/Parry*.cs | Guard/parry | | CounterAttack / CounterAttackSpec | NewAbilities/CounterAttack*.cs | Backstab | | PlayerWeaponController | Player/PlayerWeaponController.cs | Weapon equip/unequip | | PlayerWeaponDamage | Player/PlayerWeaponDamage.cs | Hitbox + damage | | DefenseState | Player/DefenseState.cs | Guard state machine | | LockOn | Player/LockOn.cs | Auto-aim targeting | | MakeInvincible | Player/MakeInvincible.cs | I-frame flag |

Combat Support (4 files)
| Script | File Path | Role | |--------|-----------|------| | WeaponController | Combat/WeaponController.cs | Base weapon handler | | KnockBack | Combat/KnockBack.cs | Knockback + i-frames | | RootMotionController | Combat/RootMotionController.cs | Root motion toggle | | CameraShakeData | Combat/CameraShakeData.cs | Shake config |

Enemy Combat (13 files)
| Script | File Path | Role | |--------|-----------|------| | EnemyWeaponController | Enemies/Combat/EnemyWeaponController.cs | Enemy weapon handler | | EnemyWeaponDamage | Enemies/Combat/EnemyWeaponDamage.cs | Enemy hitbox + damage | | EnemyShieldController | Enemies/Combat/EnemyShieldController.cs | Shield logic | | BowController | Enemies/Combat/BowController.cs | Bow weapon | | PlayerDamageProyectile | Enemies/Combat/PlayerDamageProyectile.cs | Player projectile | | ArrowMovement | Enemies/Combat/ArrowMovement.cs | Arrow physics | | ArrowTrayectory | Enemies/Combat/ArrowTrayectory.cs | Arrow arc | | ExplosionDamage | Enemies/Combat/ExplosionDamage.cs | AoE damage tick | | LightingDamage | Enemies/Combat/LightingDamage.cs | Instant lightning | | MoveFireBall | Enemies/Combat/MoveFireBall.cs | Fireball movement | | DeathCheck | Enemies/Combat/DeathCheck.cs | Death coordination | | Slot | Enemies/Combat/Slot.cs | Position slot | | SlotUpdate | Enemies/Combat/SlotUpdate.cs | Slot refresh |

Enemy Boss (4 files)
| Script | File Path | Role | |--------|-----------|------| | PhaseController | Enemies/Boss/PhaseController.cs | Phase tracking | | AttackDirector | Enemies/Boss/AttackDirector.cs | Sequence selection | | AttackLibrary | AttackDefinitions/AttackLibrary.cs | Phase attack arrays | | AttackSequences | AttackDefinitions/AttackSequences.cs | Sequence definition |

Enemy AI Bridge (2 files)
| Script | File Path | Role | |--------|-----------|------| | EnemyGraphBridge | Enemies/EnemyGraphBridge.cs | AI ↔ Combat bridge | | EnemiesGroupContoller | Enemies/EnemiesGroupContoller.cs | Multi-enemy management |

Managers (4 files)
| Script | File Path | Role | |--------|-----------|------| | CameraShakeManager | Managers/CameraShakeManager.cs | Camera shake control | | TimeEffectsManager | Managers/TimeEffectsManager.cs | Slow motion | | VibrationManager | Managers/VibrationManager.cs | Controller vibration | | KarmaManager | Managers/KarmaManager.cs | Karma tracking |

UI (5 files)
| Script | File Path | Role | |--------|-----------|------| | PlayerHealth | UI/PlayerHealth.cs | Health bar | | PlayerDamage | UI/PlayerDamage.cs | Damage flash | | PlayerStamina | UI/PlayerStamina.cs | Stamina bar | | PlayerAbilities | UI/PlayerAbilities.cs | Ability cooldowns | | PlayerUIController | UI/PlayerUIController.cs | Menu navigation | | EnemyHealthBar | UI/EnemyHealthBar.cs | Enemy health bar | | EnemyUIBridge | UI/EnemyUIBridge.cs | Enemy UI show/hide |

New Abilities (20 files)
All in GameplayAbilitySystem/Scripts/NewAbilities/:

BarbarianAttack / BarbarianAttackSpec - Enemy melee
BowAttack / BowAttackSpec - Archery
ArcherDash / ArcherDashSpec - Archer dodge
CounterAttack / CounterAttackSpec - Backstab
DamageProyectile / DamageProyectileSpec - Projectiles
Dash / DashSpec - Player dodge
ElectroAttack / ElectroAttackSpec - Lightning
FireAttack / FireAttackSpec - Fire
GodSpaceAttack / GodSpaceAttackSpec - God attacks
HealPlayer / HealPlayerSpec - Self heal
HealTarget / HealTargetSpec - Heal ally
LightAttack / LightAttackSpec - Player melee
LightAura / LightAuraSpec - Aura
MageCounterAttack / MageCounterAttackSpec - Mage counter
Parry / ParrySpec - Parry
RotateToPlayerAb / RotateToPlayerAbSpec - Rotation
SleepProyectile / SleepProyectileSpec - Sleep
Teleport / TeleportSpec - Teleport
TrapBubble / TrapBubbleSpec - Trap
AreaRayAttack / AreaRayAttackSpec - Ray attack
AI Action/Condition Scripts (36 files)
All in Enemies/Scripts/:

AttackingState.cs - AI attack state
ExecuteAttackSequences.cs - Run attack sequences
ComboControllerAction.cs - Combo control
WeaponControlAction.cs - Weapon control
Various action/condition scripts for Behavior Graph AI