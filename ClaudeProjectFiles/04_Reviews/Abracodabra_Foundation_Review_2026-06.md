# Abracodabra — Foundation Review (June 2026)
**Scope:** Full pass over `Unity_EXTRACTED_scripts.txt` (~24,250 lines, 130+ scripts), `Unity_EXTRACTED_ToolkitUI.txt`, and `gene_systems_deep_dive_v6.md`.
**Goal:** Identify what to rework *now*, before content fills in and systems interlock — plus the path to a testable beta/POC build.

**Verdict up front:** The architecture is genuinely good. The gene runtime (GUID + fallback name + version migration), the leaf-vitality model, the service layer, the tick registration pattern, and the UI Toolkit controller split are all solid foundations. But there are **two gameplay-breaking structural gaps (A1, A2)**, **one masked bug that will detonate later (A4)**, and **one architectural inversion (B1)** that gets exponentially more expensive to fix with every system you add. Fix the A-list before writing any more content. Everything else can be scheduled.

---

## ⚠️ Reality check first: documented state vs. actual code

Several systems recorded as "implemented" in project notes/roadmaps **do not exist in the extracted codebase**:

| System | Documented status | Actual status in code |
|---|---|---|
| GeneDraftSystem (pick-1-of-3) | "Implemented" | ❌ Absent. `GeneRewardSystem` exists but dumps 2–4 **random** genes straight into inventory — no draft, no choice. |
| DorisMoodSystem | "Implemented" | ❌ Absent. Only `DorisHungerSystem` (Satisfied/Hungry/Starving) exists. |
| ComboDiscoverySystem (13 archetypes) | "Implemented" | ❌ Absent. Closest thing: synergy/warning strings inside `SeedTooltipData`. |
| RoundStatsTracker | "Implemented" | ❌ Absent. No per-round stats anywhere. |
| Tutorial hint system | "Implemented" | ❌ Absent. |
| AOE tile overlay | "Implemented" | ⚠️ Partial — `GridDebugVisualizer` exists with radius show/hide APIs. |

Either these live in files outside the extraction, or planning notes got ahead of reality. **Re-run the extractor and verify.** If they're genuinely missing, every plan built on "combo discovery already works" needs revision. This document assumes the extract is ground truth.

---

# A. Critical — fix before continuing (these block or break the core loop)

## A1. The Growth & Threat phase has no engine. Ticks never advance on their own.

**The design doc (§1) says:** *"Growth & Threat Phase — The TickManager advances automatically."*

**The code says otherwise.** The only callers of `TickManager.AdvanceTick()` are:
- `GardenerController.TryMove` / `ProcessMultiTickMovement` (player walks)
- `PlayerActionManager.AdvanceGameTickStatic` (player acts)
- `FeedingSystem` (player feeds)
- Debug key `T` in `TickManager.Update()`

`TickConfiguration.ticksPerRealSecond` exists but **nothing consumes it to drive time**. Consequence: during the "watch your farm operate" phase, if the player stands still, *nothing happens* — plants don't grow, waves never reach `waveEndTick`, Doris never gets hungry, the round never ends. The entire defend-phase fantasy currently runs on the player pacing in circles. This is the single largest gap between design intent and implementation.

**Fix — `ExecutionPhaseDriver` (new, small class):**
1. Subscribes to `RunManager.OnRunStateChanged`.
2. While state == `GrowthAndThreat`: accumulate `Time.deltaTime`, call `TickManager.AdvanceTick()` every `1/ticksPerRealSecond` seconds.
3. While state == `Planning`: inert — ticks stay action-driven (Stoneshard-style), exactly as now.

**Mandatory companion change — centralize tick authority.** Right now three classes call `AdvanceTick()` directly. The moment auto-ticking exists, a player move during the execution phase **double-advances time**. Route everything through one gate, e.g.:

```csharp
// On TickManager (or a thin TickFlowController)
public void RequestActionTicks(int count) {
    // Planning: advance immediately (player-driven time)
    // GrowthAndThreat: actions are free OR queued — time flows on its own
    if (RunManager.Instance.CurrentState == RunState.Planning)
        AdvanceMultipleTicks(count);
}
```

Then replace every direct `AdvanceTick()` call site (`GardenerController`, `PlayerActionManager`, `FeedingSystem`) with `RequestActionTicks`. This is a ~half-day change that unlocks the actual game.

**While you're there:** add pause / 1× / 2× speed to the driver. Every TD player will reach for it, and it's three lines once the driver owns the clock.

## A2. Dead plants never leave the world — tiles are permanently consumed.

`PlantGrowth.Die()` is, in full:

```csharp
void Die() {
    CurrentState = PlantState.Dead;
    Debug.Log($"[PlantGrowth] '{name}' has died.");
}
```

The GameObject persists forever. It stays registered with `TickManager` (only `OnDestroy` unregisters), it stays in `PlantPlacementManager.plantsByGridPosition` (`CleanupDestroyedPlants` only removes *null* entries, and this object is never null), and **its tile can never be replanted for the rest of the run**. In a game where plant death is a core economic event, this silently shrinks the playable farm every time a plant dies.

**Fix — a real death pipeline on `PlantGrowth`:**
1. `Die()` → fire a `public event Action<PlantGrowth> OnPlantDied;` (UI, stats, Doris reactions will want this later).
2. Optional corpse: tint sprites, wait N ticks (tick-based, not `WaitForSeconds`).
3. Unregister from `TickManager`, then `Destroy(gameObject)` — `OnDestroy` already handles `AllActivePlants` removal; `PlantPlacementManager.CleanupDestroyedPlants` then correctly frees the tile because the entry *becomes* null.

Half a day, including a corpse-fade. Do it together with A1, because plant death only starts actually happening once waves run on real time.

## A3. "Wave defeated" is a lie — rounds end on a timer, and there's a dead code branch pretending otherwise.

`WaveManager.IsCurrentWaveDefeated()` returns `state == Idle && currentWaveIndex >= 0` — i.e., *"the wave timer expired."* Nothing counts living pests. Survivors are bulk-`Destroy`ed by `ClearAllActiveAnimals()` at the next round. Meanwhile `RunManager.StartNewPlanningPhase()` branches on `IsCurrentWaveDefeated()` as if defeat-tracking existed.

This isn't a bug so much as an **undecided design encoded as misleading names**. Decide now, because reward pacing, wave balancing, and the end-of-round flow all depend on it:

- **Option 1 (recommended for demo): timer-based "survive the day."** Rename to `IsWaveTimerComplete()`, delete the dead branch, and make the *cost of surviving badly* explicit: leaves lost, plants killed, Doris hunger gained. Pests fleeing at dawn is thematically fine (cozy-dark, realistic forest animals).
- **Option 2: kill-based rounds.** `FaunaManager` reports spawned-count to `WaveManager`; pests decrement on death; round ends when count hits 0 *or* timer expires (whichever first). More TD-classic, more balancing work.

Either way, the round needs a **visible ending** — see E3 (end-of-round summary).

## A4. Masked bug: any future stat recalculation zeroes plant energy generation.

`PlantGrowthLogic.CalculateAndApplyPassiveStats()` ends with:

```csharp
if (plant.EnergySystem != null) {
    plant.EnergySystem.BaseEnergyPerLeaf = PhotosynthesisEfficiencyPerLeaf;
}
```

`PhotosynthesisEfficiencyPerLeaf` is **never assigned anywhere** — it's always `0`. The only reason plants generate energy today is call order in `PlantGrowth.InitializeWithState`: stats are calculated *first*, then `EnergySystem.BaseEnergyPerLeaf = seedTemplate.energyRegenRate` overwrites the zero.

The moment anything re-invokes `CalculateAndApplyPassiveStats()` mid-life — a fertilizer buff, a grafting mechanic, a passive gained mid-run, a balance hotfix that recalculates stats — every affected plant silently stops generating energy, and you will hunt that bug for a day. **Fix now (10 minutes):** delete the line, or set `PhotosynthesisEfficiencyPerLeaf = template.energyRegenRate` inside the method so it's idempotent and safe to call at any time. Idempotent stat recalc is also a prerequisite for several genes on your remaining list.

**Adjacent micro-fix:** `PlantEnergySystem.OnTickUpdate` calls `plant.gameObject.GetComponent<PlantGrowth>()` every tick despite already holding `plant`. Use the field.

## A5. Initialization is held together by timers — three competing mechanisms and four `Invoke`-style delays.

Current init paths, all live simultaneously:
- `InitializationManager` raising 3 phased `GameEvent`s,
- `GameBootstrap.Awake` initializing `GeneServices`,
- self-healing fallbacks: `GeneServices.Get<T>` auto-initializes *and* hunts for `GeneLibrary` via `Resources.FindObjectsOfTypeAll`,
- `GeneRewardSystem`: `Invoke(LateBindRunManager, 0.5f)`,
- `StartingLoadoutApplier`: `Invoke(ApplyLoadout, 0.5f)`,
- `GameUIManager`: `schedule.Execute(...).StartingIn(10)` for hotbar selection,
- plant growth start: `WaitForSeconds(0.5f)` in `DelayedGrowthStart`.

On top of that there are **two parallel starting-item systems**: `StartingInventory` (consumed by `GameUIManager.SetupPlayerInventory`) *and* `StartingLoadoutConfig` + `StartingLoadoutApplier`. Two sources of truth for what the player starts with.

Time-based init is the classic "works until it doesn't" pattern — it breaks on slower hardware, after adding a loading screen, or when a demo tester's machine takes 0.6 s instead of 0.4 s to load the scene, and the failure is non-reproducible on your machine. **Fix before content:**
1. One owner: `InitializationManager` runs an explicit ordered sequence (`GeneServices → GeneLibrary → TickManager → RunManager.Initialize → WaveManager.Initialize → Inventory model → UI → loadout`), each step awaited/verified, no `yield return null` hoping.
2. Delete every `Invoke(..., 0.5f)` and replace with either direct sequencing or event subscription on the system's `Initialized` event.
3. Pick **one** starting-loadout path (recommend `StartingLoadoutConfig`, it's richer) and delete the other.
4. Keep `GeneServices` fallbacks as `Debug.LogError` tripwires, not as silent self-repair — you want to *know* when ordering broke.

## A6. Decide on determinism now — currently it's branding, not behavior.

`DeterministicRandom` is seeded with `DateTime.Now.Millisecond` (i.e., not deterministic), and 39 call sites use `UnityEngine.Random` directly in gameplay logic: `PlantGrowth.DestroyRandomLeaf`, `GeneRewardSystem`, `PlantPlacementManager.GetRandomizedPlantingPosition`, fauna spawning, and more.

This is a **decision**, not automatically a bug:
- **If seeded runs / reproducible balancing / mid-round save-resume ever matter** (they usually do for a WeGo roguelite — being able to replay a reported bug from a seed is gold): route *all gameplay* randomness through `IDeterministicRandom`, seed once per run in `RunManager`, display the seed on the game-over screen. Visual-only randomness (firefly drift, flash timing) may stay on `UnityEngine.Random`.
- **If not:** delete the `IDeterministicRandom` abstraction so it stops implying a guarantee that doesn't exist.

Retrofitting determinism after 28 genes, waves, and statuses are interlocked is exactly the late-refactor you said you want to avoid. Cost now: ~1 day of mechanical call-site replacement. Cost in 6 months: a week plus regressions.

---

# B. Architectural debt — schedule before content scaling (not emergencies, but compounding)

## B1. The UI owns the game's data. Invert it. *(Highest-value rework on this list.)*

`GameUIManager` constructs and owns `playerInventory : List<UIInventoryItem>` and hands it to `InventoryService.Register(...)`. `UIInventoryItem` is simultaneously a **UI construct and the canonical data model** — it even holds `SeedRuntimeState`, the player's edited gene sequences. Consequences already visible:
- `GeneRewardSystem` (pure game logic) must construct *UI* items to grant rewards.
- The inventory cannot exist without the UI document loading first (hence the 0.5 s `Invoke` hacks in A5).
- Save/load — whenever it comes — would have to serialize UI objects.
- Headless testing/balancing of the economy is impossible.

**Fix:** a plain-C# `PlayerInventory` model (slots of `InventoryEntry { ItemDefinition def; PlantGeneRuntimeState seedState; GeneBase gene; ... }`) owned by a `PlayerInventorySystem` MonoBehaviour, initialized in the bootstrap sequence (A5). `InventoryService` keeps its static API but fronts the model. `UIInventoryItem` becomes a thin view wrapper created on demand by the grid controller. Reward/loadout/feeding systems talk to the model only.

This is ~2–3 focused days now. After 10 more systems touch `UIInventoryItem`, it's two weeks. It's the textbook case of the pile-up you asked me to flag.

## B2. Reflection-based coupling in `GameUIManager` — delete it.

Two instances:
- Doris hunger events subscribed via `System.Reflection.EventInfo` + `GetMethod("OnDorisHungerChanged", NonPublic...)`.
- Wave progress bar reads `WaveManager`'s **private** `waveStartTick` / `waveEndTick` via `GetField(...)`.

Both are rename-fragile (a refactor silently kills the HUD with zero compiler errors), slow, and unnecessary — `DorisHungerSystem` is a normal accessible class. Replace with direct references; add `public float WaveProgress01 { get; }` to `WaveManager`. One hour, removes a whole category of invisible breakage.

## B3. Two UI stacks for screen-space HUD.

`WaveManager` (wave/time `TextMeshProUGUI`), `HungerUI`, and `AnimalController` debug labels still live in uGUI/TMP while the real HUD is UI Toolkit. World-space elements (thought bubbles, floating combat text, status icons above heads) are *legitimately* fine staying on TMP — don't migrate those. But the screen-space wave status / day tracker duplicate what `GameUIManager`'s HUD shows (via reflection, see B2). Move wave/day/round display fully into the UI Toolkit HUD, delete the TMP fields from `WaveManager`, and `WaveManager` stops being a UI class entirely (also kills its per-frame `Update → UpdateUI` string churn).

## B4. `PlantGrowth` is a ~670-line god class — split before adding archetypes.

It currently contains: 4 archetype growth algorithms, withering state, leaf regrowth + animation, damage handling, thorn retaliation, fruit spawn-point discovery (creating **temporary GameObjects** cleaned by a delayed coroutine), harvesting, damage-flash visuals, and static plant registry. You have more archetypes planned (Creeper, Mushroom — deferred list). Each one grows the monolith.

**Minimal split, in order of payoff:**
1. `IGrowthPattern { void Step(PlantGrowth p); }` with `StandardGrowth / GrassGrowth / CanopyGrowth / BushGrowth` — archetype selected once at init. New archetypes become new files, not new switch cases.
2. Fruit spawn-point logic → return `List<Vector2Int>` from `PlantCellManager` and convert to world positions at the call site. Kills the temp-GameObject + cleanup-coroutine hack entirely (`BasicFruitGene` just needs positions, not Transforms).
3. Withering/death → can stay, but make durations tick-constants on `SeedTemplate` or a config SO, not `const int WITHERING_DURATION = 3` buried in the class (you'll want to balance this per-archetype).

## B5. Wall-clock time leaks into tick logic — will collide with A1.

- `DelayedGrowthStart`: `WaitForSeconds(0.5f)` before a plant may grow. Once auto-ticking runs at 2–4 ticks/sec, a plant misses 1–2 growth ticks *non-deterministically* depending on frame timing.
- `GardenerController.ProcessMultiTickMovement`: advances ticks inside a real-time coroutine with `multiTickDelay` waits — under auto-tick this both double-advances and desyncs.
- `WaveManager.ExecuteWaveSpawn`: `WaitForSeconds(1f)` state hold.

Rule going forward: **gameplay state changes happen on tick boundaries; only visuals may use real time.** Convert these three to tick counters when you do A1 — they're the exact places the new driver will misbehave.

## B6. Consistency nits (cheap, do opportunistically)

- **Singletons:** `SingletonMonoBehaviour<T>` exists, yet `WaveManager`, `PlantPlacementManager`, `FoodSelectionPopup`, `InventoryColorManager`, `WeatherManager` hand-roll `static Instance`. Unify on the base class (your own stated consistency principle).
- **`GeneInstanceData` duplication:** public `powerMultiplier` / `stackCount` fields **and** a `"power_multiplier"` dict entry are both written. Two sources of truth; the dict is what `PlantGrowthLogic` reads — delete the fields or migrate fully to them.
- **Filename typo:** `RactiveBurstHandler.cs` (class is fine, file is grep-hostile). Rename to `ReactiveBurstHandler.cs`.
- **`PlantGrowth.TakeDamage`** is a "legacy" passthrough to `DestroyRandomLeaf` — audit callers, then either bless it as the official damage entry point or remove it so there's exactly one damage path.
- **`GetRandomizedPlantingPosition`:** random offset applied, then `SnapEntityToGrid` — in a grid game this is either a no-op or (if radius grows past half a cell) plants in the *wrong cell*. Plant at cell center; add visual jitter on the sprite child if you want organic looks.
- **Log hygiene:** verbose per-tick `Debug.Log` everywhere (`PlantGrowth` withering, `PlantPlacementManager` `verboseLogging = true` by default, Doris, feeding). Wrap in a project-wide `GameLog.Verbose(...)` gated by a define or static flag before the demo — string allocation per tick × N plants is real GC pressure, and testers' logs become unreadable.

---

# C. Design-level gaps the demo will expose

1. **No Game Over screen.** `RunState.GameOver` exists, `RestartGame()` exists, but no UI panel exists anywhere (`GameOverPanel` appears in neither C# nor UXML). On starvation the HUD just… stays. A demo cannot ship without a run-end screen (stats + restart). Pairs naturally with a `RoundStatsTracker` (see C3).
2. **Doris consequence is one-dimensional.** Starving Doris eats plants (`DorisController.EatPlant` works) — good — but feeding her well does nothing, and her panel is a `// TODO: Open Doris UI panel`. For the central emotional pressure of the game, the minimum demo bar is: visible hunger state on/near Doris at all times, the popup feed flow polished, and *some* positive feedback for keeping her fed (even a flat mood line of dialogue). Full DorisMoodSystem can wait.
3. **The reward ritual is missing.** Random genes silently appearing in inventory (`GeneRewardSystem`) gives away the roguelite's best moment. The documented **pick-1-of-3 draft** is a small system (one UI Toolkit panel + selection callback into the inventory model from B1) with outsized feel impact. Combined with an end-of-round summary ("Leaves lost: 7 · Pests repelled: 12 · Doris: Satisfied → **choose your gene**"), this *is* the loop closure the POC needs to feel like a game rather than a sandbox.
4. **Waves don't scale past the authored list.** `currentWaveIndex = roundNumber - 1`; past the list, waves just stop. Fine for a fixed-length demo (recommended: author 6–8 rounds and make round N the explicit demo end — "demo complete" screen), but decide the demo's shape now because wave authoring and Doris hunger tuning depend on run length.
5. **No save system.** Acceptable for a 20–40 min single-run demo; not acceptable beyond that. The gene runtime is already serialization-ready (GUIDs, version migration — nice work), but `PlantGrowth`/world state is not. Decision: keep demo runs short and saveless, and **note** that B1 (inventory model extraction) is the prerequisite that keeps save/load cheap later.

---

# D. What's genuinely solid (don't touch)

- **Gene runtime data model** — `RuntimeGeneInstance` with GUID + name fallback + version migration via `SafeGeneLoader` is better than most shipped indie games. Keep.
- **Slot model for POC** — the v6 doc itself prescribes slot-now/buffer-later, and `PlantSequenceExecutor` is cleanly isolated: the buffer migration later genuinely only swaps the input format (parser producing groups) while execution logic stays. The deferral is safe. ✅ Keep deferring.
- **Leaf vitality** — fully wired: `leafDurabilityMultiplier` correctly multiplies pest `baseEatSpeedTicks` in `AnimalController`, thorns retaliate on consumption, withering grace window works, `ReactiveBurstHandler` triggers off leaf loss. The doc's Design Principle 8 is real in code.
- **TickManager** — pending add/remove lists during iteration, exception isolation per updateable. Correct and robust.
- **Status effects, animal component split** (`Controller/Movement/Needs/Behavior`), **GridPosition discipline**, **ScriptableObject content pipeline** — all sound.

---

# E. Roadmap to the testable beta / POC demo

Ordered so each step unblocks the next. "Done when" criteria included, your style.

### Phase 1 — Make the loop actually run *(≈1 week)*
1. **ExecutionPhaseDriver + centralized tick authority** (A1, B5). *Done when: pressing Start Day makes the farm run hands-free at a configurable speed, with pause/1×/2×, and walking during execution doesn't double-advance time.*
2. **Plant death pipeline** (A2). *Done when: a plant stripped to 0 leaves withers, dies, fades, and its tile is replantable next planning phase.*
3. **Bug fixes A4 + B2 reflection removal.** *Done when: stat recalc can be called twice with identical results; HUD wave bar reads a public property.*

### Phase 2 — Close the loop emotionally *(≈1–1.5 weeks)*
4. **Round end decision (A3) + end-of-round summary panel.** Timer-based "survive the day," stats collected by a minimal `RoundStatsTracker` (leaves lost, pests killed/fled, food harvested, Doris state).
5. **Pick-1-of-3 gene draft** replacing the random dump.
6. **Game Over / Demo Complete screen** with run stats + restart.
*Done when: a full session reads Plan → Watch → Summary → Draft → Plan, and ends in a real screen either way.*

### Phase 3 — Foundation hardening before content fill *(≈1 week)*
7. **Init cleanup (A5)** — one bootstrap sequence, zero `Invoke` delays, one starting-loadout system.
8. **Inventory model extraction (B1).** Do it *now*, while only ~6 systems touch inventory.
9. **Determinism decision (A6)** — implement or delete; if implementing, seed shown on game-over screen.
*Done when: scene loads cold with no race-condition warnings, rewards/loadout grant items without referencing any UI type, and (if chosen) the same seed reproduces the same run.*

### Phase 4 — Content & tuning to demo scope *(2–3 weeks, feel-based)*
10. Author 6–8 wave definitions with a difficulty curve; tune Doris `hungerPerTick` against round length; tune gene energy costs against `ticksPerDay`.
11. Implement only the remaining genes the demo combos *need* (the 23 in place are likely enough — breadth is not the demo's bottleneck, pacing is).
12. Doris minimum bar (C2): persistent hunger readout + polished feed flow + one positive feedback.
13. `PlantGrowth` archetype split (B4) **only if** you add an archetype for the demo; otherwise defer.

### Phase 5 — Polish & ship prep *(your existing plan, unchanged)*
14. Log gate + debug-key strip, VFX hooks, post-processing, gene icon set (resolve the rarity-tier question — note `GetMaxTierForRound` already implies a 3-tier ladder, which argues for the corner-gem variant), then the single focused Steam page session.

### Explicitly *not* now (your skip list, endorsed)
Buffer model migration, new archetypes, Seedpod/Root Network/Mycorrhizal/Carnivorous, sound, localization, save system. The codebase confirms all of these are cleanly deferred — none of them are entangled in the A/B items above.

---

## One-page priority summary

| # | Item | Effort | Why now |
|---|---|---|---|
| A1 | Auto-tick driver + tick authority | 0.5–1 d | The game loop literally doesn't run without it |
| A2 | Plant death/cleanup pipeline | 0.5 d | Tiles permanently lost; core economy broken |
| A4 | BaseEnergyPerLeaf zeroing bug | 10 min | Time bomb under every future stat recalc |
| B2 | Remove reflection coupling | 1 h | Silent-breakage risk for free |
| A3 | Round-end decision + naming | 0.5 d | Everything in Phase 2 builds on it |
| A5 | Init sequence cleanup | 1–2 d | Race conditions will hit testers, not you |
| B1 | Inventory model out of UI | 2–3 d | Cheapest it will ever be; prerequisite for saves, rewards, tests |
| A6 | Determinism: commit or delete | 1 d / 1 h | Retrofit later = a week + regressions |
| B4 | PlantGrowth split | 1–2 d | Only before next archetype; otherwise defer |
| B3/B6 | UI stack + consistency nits | drip | Opportunistic, alongside touched files |

Estimated total for A-list + B1/B2: **~2 weeks** of focused work — after which the foundation genuinely matches the "strong base before piling up" goal, and everything remaining is content, tuning, and polish.
