# Abracodabra — Codebase Map
**Full-codebase sweep, 2026-07-05. All 199 `.cs` files under `Assets/Scripts` read (live disk), plus UXML/USS, asset folders, and docs.**

*Block 1 "Doris by Dusk" (B0-B7, 2026-10-26) is patched in: §3 (Dusk, ledger, invariant), §7 (waves, tally), §8 (belly, end overlay, banners, Reads as, dev readout), §10 (events), §11. B9 verified every class named there exists under `Assets/Scripts`.*

How to use: this is the orientation layer. Trust it for *architecture and wiring*; before editing any file, still read its current on-disk version (live file wins). Line counts are as of sweep date and serve as regression signals.

---

## 1. Verified Reality vs. Documented State (critical)

The Foundation Review (`04_Reviews/2026-06_Foundation_Review.md`) and A-Category pack (`03_Tasks/Done/Abracodabra_A_Category_Implementation.md`) describe fixes A1–A6 — **applied to the repo 2026-07-05** (code Parts 1–2 complete; Editor wiring + Part 4 tests pending). State after application:

| Item | Disk reality (2026-07-05, post-application) |
|---|---|
| A1 auto-tick | **APPLIED.** `ExecutionPhaseDriver.cs` created (auto-ticks Growth & Threat; Space=pause, Tab=speed, flood guard). `TickManager` gained `ActionsDriveTicks` + `RequestActionTicks` (single action-driven entry); `PlayerActionManager`, `GardenerController`, `FeedingSystem` all route through it — actions are free during the auto-driven phase. **Manual:** the driver GameObject must be added to the scene. |
| A2 plant death | **APPLIED.** `PlantGrowth.Die()` → `OnPlantDied` event, immediate tick-unregister, `DeathSequence()` fade → `Destroy` → `PlantPlacementManager.CleanupDestroyedPlants()` frees the tile. |
| A3 wave naming | **APPLIED.** `WaveManager.IsWaveTimerComplete()` (honest name) + `[Obsolete]` `IsCurrentWaveDefeated()` shim; `RunManager.StartNewPlanningPhase()` uses the new name. Rounds are survive-the-timer. |
| A4 energy zeroing | **APPLIED.** `PlantGrowthLogic.CalculateAndApplyPassiveStats()` derives `PhotosynthesisEfficiencyPerLeaf` from the template every call (idempotent, single source); redundant assignment removed from `InitializeWithState`; per-tick `GetComponent` removed from `PlantEnergySystem`. |
| A5 init cleanup | **APPLIED.** `StartingLoadoutApplier` binds to new `InventoryService.OnInventoryReady`; `GeneRewardSystem` binds directly at Start (no LateBind Invoke); `InitializationManager.IsReady`/`OnReady` added. (Out-of-pack-scope Invokes remain: FeedingSystem 0.2s, AnimalFeedableAdapter/PlayerHungerSystem 0.3s.) |
| A6 determinism | **APPLIED.** GeneServices default seed = 0; `RunManager.InitializeRunSeed()` seeds per-run (`RunSeed`, `randomizeSeedOnStart` inspector fields); reward selection + leaf-death selection now deterministic. Fauna/firefly/cosmetic `UnityEngine.Random` sites remain (scoped follow-up, see §12). |
| `DorisMoodSystem` / `ComboDiscoverySystem` / `GeneDraftSystem` | **Still design-only.** Doris = `DorisController` + `DorisHungerSystem` + `DorisDefinition`. Gene acquisition = `GeneRewardSystem` (post-round drops). |
| `PlantGrowth.cs` | Now **885 lines** (825 + A2/A6 additions; still the refactor-watch leader with GameUIManager 875). |

Newest scripts on disk: 2026-07-05 (A-pack application; 200 .cs files total). (2026-10: the extractor and `06_Index/` are retired; grep the live files.) Editor wiring landed 2026-07-06. Map spot-verified against live disk 2026-07-06: all A1–A6 symbols present (`ExecutionPhaseDriver.cs`, `RequestActionTicks`/`ActionsDriveTicks`, `OnPlantDied`, `RunSeed`/`randomizeSeedOnStart`, `IsWaveTimerComplete` + Obsolete shim, `OnInventoryReady`, `RactiveBurstHandler.cs` typo).

---

## 2. Boot & Initialization Flow

1. `GameBootstrap` (`DefaultExecutionOrder(-100)`): `GeneServices.Initialize()` → registers `GeneEventBus`, `DeterministicRandom` (time-seeded); registers `GeneEffectPool`. Static `hasInitialized` guard + editor reset via `RuntimeInitializeOnLoadMethod`.
2. `GeneLibraryLoader` (`-90`): finds `GeneLibrary` asset, `SetActiveInstance()`, registers `IGeneLibrary` service.
3. Singletons self-install in `Awake` (`SingletonMonoBehaviour<T>`: thread-safe, DontDestroyOnLoad, destroys duplicates): `TickManager`, `GridPositionManager`, `RunManager`, `InitializationManager`, `EcosystemManager`, `TileInteractionManager`, `ResolutionManager`. **Restart (Block 1 B3):** each registers with `SingletonRuntime`; `RunManager.RestartRun(sameSeed)` calls `SingletonRuntime.TearDownForRestart()` (DestroyImmediate of their GameObjects, getter log silenced while `IsRestarting`) and reloads the scene, so the reload starts like a fresh Play (before, the old DDOL copies survived and the reloaded ones, plus `Manager_GridPosition` via `GridDebugVisualizer`, were destroyed as duplicates). The seed crosses the reload in a static. Plain `static Instance` pattern: `WeatherManager`, `PlayerActionManager`, `ToolSwitcher`, `WaveManager`, `FeedingSystem`, `PlantPlacementManager`, `PlantGrowthModifierManager`, `EnvironmentalStatusEffectSystem`, `FireflyManager`, `FloraManager`, `WaterReflectionManager`, `MinigameManager`, `GridDebugVisualizer`, `InventoryColorManager`.
4. `InitializationManager.Start()` coroutine raises 3 `GameEvent` assets in order: CoreSystems → GameManagers → GameplaySystems; manually initializes `EnvironmentalStatusEffectSystem`. UI self-initializes (`GameUIManager.Start()`).
5. `GridSnapStartup.Start()` snaps player/animals/plants to grid.
6. Timing hacks remaining after A5: `FeedingSystem.AutoRegisterFeedables` (0.2s) and `AnimalFeedableAdapter`/`PlayerHungerSystem` delayed feeding registration (0.3s). (`StartingLoadoutApplier` and `GeneRewardSystem` Invoke-delays were removed 2026-07-05 — they now bind via `InventoryService.OnInventoryReady` / direct singleton access; `InitializationManager.IsReady`/`OnReady` provides the canonical "fully initialized" signal.)

---

## 3. Tick & Phase Flow (the WeGo heart)

- **`TickManager`** (`WegoSystem`): `AdvanceTick()` → `OnTickStarted` → process deferred add/remove of `ITickUpdateable`s → `OnTickUpdate(tick)` on each (exceptions caught) → `OnTickAdvanced` → `OnTickCompleted`. Debug: T key. Config: `TickConfiguration` SO (ticksPerRealSecond, day/night ticks, animal intervals; preset methods are dead code).
- **Who advances ticks (post-A1, 2026-07-05):** during **Planning**, player activity drives time via the single entry `TickManager.RequestActionTicks(n)` — called by `PlayerActionManager` (UseTool/PlantSeed/Harvest/Interact, `tickCostPerAction` default 1), `GardenerController` (movement; multi-tick moves via coroutine + `WaitForSeconds(multiTickDelay)` — B5 wall-clock note applies), and `FeedingSystem`. During **Growth & Threat**, `ExecutionPhaseDriver` auto-advances ticks from `TickConfiguration.ticksPerRealSecond × speedMultiplier` (Space=pause, Tab=cycle speed) and `ActionsDriveTicks` turns player-action ticks into no-ops (actions are free — no double-advance). `GetMovementTickCost()` adds `StatusEffectManager.AdditionalMoveTicks`.
- **`RunManager`**: `RunState` {Planning, GrowthAndThreat, GameOver, **Dusk** (B3)} + `GamePhase` {Planning, Execution}; `RunOutcome` {InProgress, Lost, Won} + `EndReason` + `LastVerdict`; `PhaseTickHandler : ITickUpdateable` counts phase ticks; round counter; events `OnRunStateChanged`, `OnPhaseChanged`, `OnRoundChanged`, `OnRunEnded` (B3). Subscribes `PlayerHungerSystem.OnStarvation` → `EndRun` (gated by `playerDeathEnabled`, which `Initialize` sets from `DuskRules.playerStarvationEndsRun` = false). Talks to `WeatherManager`, `WaveManager`, `SupperRunner`.
- **Dusk ledger and phase banner (Block 1 B6, 2026-10-26):** `RunManager.HandleSupperFinished` computes the verdict, then, if `DuskRules.ledgerWaitsForContinue` and `OnDuskLedger` has a listener, parks it (`AwaitingLedger`) and fires `OnDuskLedger`; `GameUIManager` shows `UIDuskLedgerController` (top right under the banner: three rows DORIS / PESTS / PLANTS from `SupperResult` + `DayTally`, `BuildLines` is static); **Continue** → `RunManager.ContinueFromLedger()` → `ApplyVerdict` (next round, win or loss). `UIPhaseBannerController` (root of the UI document, 38 px strip at the top edge, `Height` const; polls `RunManager` state each frame: PLANNING / DAY / DUSK + a "Now:" line, START DAY button in Planning, hidden on GameOver); `GameUIManager.CreatePhaseBannerAndLedger` pushes the HUD top bar and the Planning panel below it, the auto-pause banner sits under it. The player HUNGER panel is `visibility: hidden` while `DuskRules.playerStarvationEndsRun` is false. `DayTally` also keeps `LostPlants` (name + cause) for the ledger. Scripted runs: `ContinueFromLedger()` after each dusk, or set `ledgerWaitsForContinue` false.
- **Dusk (Block 1 B3):** wave timer ends → `StartNewPlanningPhase` → `BeginDusk` → `RunState.Dusk` (no ticks: the driver stops, all Day systems are G&T-gated) → `SupperRunner` (on RunManager's GameObject, added in `Initialize`) feeds every fruit of every living plant to Doris → `DuskRules.Judge`: no living plant → lost "The garden is gone.", belly 0 → lost "Doris went to bed hungry.", round 5 → won, else `StartNewRound`. `GameOver` is sticky (`StartNewPlanningPhase` ignores it). Tunables: `DuskRules` (`WegoSystem`, `Assets/Scriptable Objects/Run/Resources/DuskRules.asset`, `DuskRules.Active` = `Resources.Load`).
- **Phase gating found across systems:** `PlayerTileInteractor.Update` (input only in Planning since B2, 2026-10-26), `PlayerHungerSystem`/`DorisHungerSystem` (hunger only in GrowthAndThreat), `WorldEffect` (ticks only in GrowthAndThreat), `FaunaManager` (spawns only in GrowthAndThreat), `UISeedEditorController.SetInteractable(false)` outside Planning, `FeedingSystem` optional phase restriction.
- **Planning-tick invariant (enforced 2026-10-26, Block 1 B1):** `OnTickUpdate` early-outs unless `RunState.GrowthAndThreat` in `PlantGrowth` (covers `PlantEnergySystem`, `PlantSequenceExecutor`, regrowth, withering, growth), `AnimalController` (needed: `SampleScene` has `WaveManager.deletePreviousWaveAnimals` off, so last Day's pests stay into Planning), `ReactiveBurstHandler` (cooldown), `TileInteractionManager` (timed-tile reversion) — same pattern as `DorisHungerSystem` and `WorldEffect`. Not gated on purpose: `RunManager.PhaseTickHandler`, `GardenerController`/`PlayerHungerSystem` (player economy), `WeatherManager` (paused in Planning by `PauseCycleAtDay`), `FireflyManager`/`FireflyController` (cosmetic; firefly photosynthesis is read inside the gated energy tick), `PoopController`, `EnvironmentalStatusEffectSystem` (animals + gardener only), `PlantGrowthModifierManager` (tile bookkeeping only), `WaveManager` (Idle in normal Planning). `PlantGrowth.DelayedGrowthStart` (0.5 s wall-clock, Initializing → Growing) still runs in Planning: state only, no cells.
- **`WeatherManager`** (`ITickUpdateable`): Day → TransitionToNight → Night → TransitionToDay via tick thresholds; `sunIntensity` curve feeds photosynthesis and `FireflyManager` (night = intensity ≤ 0.25); `NightColorPostProcess` lerps a color filter.
- **`WaveManager`**: subscribes `TickManager.OnTickAdvanced`; state Idle→Active→Spawning; fires `FaunaManager.ExecuteSpawnWave(WaveDefinition)` at `waveSpawnTick`; wave ends at `waveEndTick` (timer, not defeat). `GameUIManager` reads its private `waveStartTick`/`waveEndTick` **via reflection**. B3: the wave for round r comes from `DuskRules.waves[r − 1]` (Wave_000–004) when that list is filled, else the scene's `wavesSequence` (only 2 entries in `SampleScene`); a round without a wave still runs its Day timer (before, it went Idle and the Day never ended). **B4:** `PeekWaveForRound(round)` + `SecondsUntilEntryStarts(entry)` feed the Planning markers; `EndCurrentWave` calls `ClearAllActiveAnimals()` so the Day's pests leave at dusk (code, not the scene's `deletePreviousWaveAnimals`).

---

## 4. Player Action Pipeline

Input (`PlayerTileInteractor.Update`, Planning only since B2; the Day is hands off, blocked while `FoodSelectionPopup.IsBlockingInput` and, since B3, while `GameUIManager.IsPlanningPanelOpen`, i.e. outside the garden view):
- **Left click** → selected hotbar item (`HotbarSelectionService.SelectedItem`):
  - Tool → `ToolSwitcher.SelectToolByDefinition` → `PlayerActionManager.ExecutePlayerAction(UseTool)` → `TileInteractionManager.ApplyToolAction(tool)`; on success `ToolSwitcher.TryConsumeUse()` (uses consumed on success only). HarvestPouch routes to Harvest instead.
  - Seed → `ExecutePlayerAction(PlantSeed)`; if `MinigameManager` has Planting trigger enabled → `TimingCircleMinigame` with **deferred plant action** (plant + tick + seed consumption happen after minigame; Good/Perfect auto-waters via `TileInteractionManagerExtensions.ApplyToolAtPosition`), else immediate `PlantPlacementManager.TryPlantSeedFromInventory`.
- **Right click** → eat: world `FoodItem`/`Fruit` under cursor (nutrition from `RepresentingItemDefinition.baseNutrition` × `nutrition_multiplier` dynamic prop) or selected consumable Resource (`ItemInstance.GetNutrition()` → `InventoryService.RemoveItemAtIndex`); both feed `PlayerHungerSystem.Eat()` and fire an Interact action (advances tick).
- **Harvest** → `PlantGrowth.HarvestAllFruits()` → `List<HarvestedItem>` (carries dynamicProps + `RuntimeGeneInstance` payloads) → `InventoryService.AddHarvestedItem` per item.
- Every success → `AdvanceGameTick` → `TickManager.RequestActionTicks` (advances time in Planning; free in Growth & Threat) → `OnActionExecuted(actionType, payload)` (consumed by `EnvironmentalStatusEffectSystem` for tool-based status effects).

Movement lives in `GardenerController` (WASD via `HandlePlayerInput` only in Planning since B2; in the Day she stands still; `TileInteractionManager.CheckAndRefillTool`, the tool-refill click, is Planning-only too) (`ITickUpdateable`, `GridEntity`-based, multi-tick move delay, `IStatusEffectable`).

---

## 5. Gene System

**Class hierarchy** (`Abracodabra.Genes.Core`, all ScriptableObjects with GUID persistence + version migration + `PlaceholderGene` fallback via `SafeGeneLoader`):
- `GeneBase` → `PassiveGene` (stat multipliers: GrowthSpeed, EnergyGeneration, EnergyStorage, FruitYield, Defense, LeafRegrowth, ThornDamage; additive or multiplicative stacking; `TerrainAffinityGene` special-cases tile allow/prefer lists), `ActiveGene` (energy cost, slots for modifiers/payloads, optional delay ticks, `isTriggerType`), `ModifierGene` (Cost/Trigger/Behavior/Condition; Pre/PostExecution hooks), `PayloadGene` (Substance/Nutrition/Healing/Special; `ConfigureFruit` + `ApplyToTarget`).
- **Actives on disk:** BasicFruitGene, ProjectileGene, CloudGene, AuraGene, TrapGene, PruningGene (leaf→energy), ReactiveBurstGene (trigger-type; logic in `ReactiveBurstHandler` — file is misspelled `RactiveBurstHandler.cs`).
- **Modifiers:** CostReduction, Overcharge (cost↑ power↑), TriggerProximity (gates slot on creature in range).
- **Passives:** EnergyRoots, GrowthSpeed, ThickBark, Regrowth, ThornedLeaves, TerrainAffinity.
- **Payloads:** Explosive (AoE + self-leaf-damage), Healing (`IsPlantHealingPayload`, drives Cloud/Aura leaf regrowth), Poison, Freeze (stacking → full freeze), Slow, Fear (immunity flag on AnimalDefinition), Nutritious.

**Data flow:** `SeedTemplate` (species config: slots, energy, archetype Standard/Grass/Canopy/Bush, growth params) → `CreateRuntimeState()` → `PlantGeneRuntimeState` (passive instances + `RuntimeSequenceSlot[]` with `RuntimeGeneInstance`s = serializable gene refs + per-instance float dict) → planted via pipeline in §4 → `PlantGrowth.InitializeWithState`.

**Execution:** `PlantSequenceExecutor.OnTickUpdate` (mature plants, one slot per tick): skip empty/trigger slots → honor `delayTicksRemaining` → build `ActiveGeneContext` → target check → modifier trigger conditions → energy check (fail → `GeneValidationFailedEvent`) → spend energy → `PreExecution` → `Execute` (or arm delay) → `PostExecution` → `GeneExecutedEvent`. Sequence end → `SequenceCompletedEvent`, cursor reset, `rechargeTicksRemaining = template.baseRechargeTime`.

**World effects** (`Genes/WorldEffects`): `WorldEffect` base (tick-registered, GrowthAndThreat-gated, payload tinting, fade+destroy) → `CloudWorldEffect` (periodic AoE + leaf regrow via HealingPayload, legacy Nutrition fallback 50%), `AuraWorldEffect` (infinite duration, drains plant energy per tick, shrinks when starved). `ProjectileWorldEffect` + `TrapWorldEffect` are real-time (`Update()`), not tick-based. `TargetFinder` = static spatial queries (uses `FindObjectsByType` — O(n) per call, every effect tick).

**Acquisition:** `GeneRewardSystem` on `RunManager.OnRoundChanged`: 1–3 random genes, tier-gated by round, straight into `InventoryService` (raw `Random.Range`). `StartingLoadoutConfig`/`Applier` seed the run. No draft/extraction system on disk.

---

## 6. Plant Lifecycle ("the plant is the health bar")

`PlantGrowth` (825L, states Initializing→Growing→Mature→Withering→Dead, static `AllActivePlants`) composes:
- `PlantCellManager` — cell dict + `LeafData` list (leaf = `GridCoord` + `IsActive`); cells are child GameObjects (`PlantCell` reports destruction back via `OnDestroy`).
- `PlantGrowthLogic` — recomputes passive stat multipliers (per-stat additive vs multiplicative), growth progression.
- `PlantEnergySystem` — per tick: `leafCount × BaseEnergyPerLeaf × energyGenerationMultiplier × sunlight` + firefly bonus (`FireflyManager.GetNearbyFireflyCount × intensityPerFly`, capped) × tile multiplier (`PlantGrowthModifierManager.GetEnergyRechargeMultiplier`). Recomputed each tick from state — idempotent. Global base rate on `FloraManager.basePhotosynthesisRatePerLeaf`.
- `PlantVisualManager` (+ `PlantShadowController`/`PlantOutlineController`/`ShadowPartController`/`OutlinePartController`, `PlantWorldUI` energy bar).
- `PlantSequenceExecutor` (§5).

Leaf loss: pests eat leaves (`AnimalController` pest attack picks random active leaf) → `OnLeafConsumed` event (feeds `ReactiveBurstHandler`, thorn damage) → 0 leaves ⇒ Withering (3-tick countdown) ⇒ Dead. `RegrowthGene` regrows a leaf every N ticks (works during Withering). Doris starving eats whole plants (`Object.Destroy`). Tile speed/energy multipliers come from `PlantGrowthModifierManager` — **keyed by tile `displayName` string** (fragile).

Fruits: `BasicFruitGene` spawns `Fruit` components at spawn points; payloads configure them (`DynamicProperties`: is_poisonous, freeze_stacks, nutrition_multiplier…); harvest transfers payload instances into `ItemInstance.payloads` via `HarvestedItem`.

---

## 7. Ecosystem

- **Animals:** `AnimalController` (609L hub; `ITickUpdateable`, `IStatusEffectable`, `ITriggerTarget`) + `AnimalMovement` (738L; A* via `GridPositionManager.GetPath`, decision priority: flee → screen-center seek → pest plant targeting → food seeking → wander), `AnimalNeeds` (hunger per tick interval, starvation damage, unified eating: `Fruit` dynamic nutrition first, else `FoodType.baseSatiationValue`), `AnimalBehavior` (eat/poop; `Destroy` on eaten food). Config: `AnimalDefinition`/`AnimalLibrary`/`AnimalDiet`/`AnimalThoughtLibrary` (+`ThoughtBubbleController`).
- **Waves:** `WaveDefinition` SO → `WaveManager` (tick-timer lifecycle) → `FaunaManager` (coroutine spawn bursts; **B4: waits are in Day ticks (`WaitDayTicks`), so pause and Tab move them**). `WaveSpawnLocationType.FixedEntry` (B4, appended) + `WaveSpawnEntry.entryEdge/entryAlong` → `WaveEntryGeometry` (static: spawn point just off the camera view edge, marker point just inside; no RNG). `UIWaveMarkersController` (HUD panel, Planning only, `GameUIManager.WaveMarkers`) draws one marker per entry of the coming wave.
- **Doris:** `DorisController` (619L; `MultiTileEntity`, `IWorldInteractable` + `IFeedable`, hover tint, eats nearby plants when starving on a tick cadence) + `DorisHungerSystem` (tick-based hunger ↑, states Satisfied/Hungry/Starving, rich event surface) + `DorisDefinition` (category diet multipliers). B3: hunger per Day tick = `DuskRules.dorisHungerPerTick` (0.5, overrides the definition's 2) × (1 + 0.15 (round − 1)) (`CurrentHungerPerTick`); belly = `BellyPips` (8, a view over hunger); she eats a plant only at belly 0 and at most once a Day (`PlantsEatenToday`, reset when a Day starts); `UpdateHover` polls `TileInteractionManager.CurrentlyHoveredCell` and raises `OnHoverChanged` (nothing called `OnHoverEnter` before); `BellyAnchorWorldPosition`, `MouthWorldPosition`.
- **Day tally and auto-pauses (Block 1 B5):** `WegoSystem.DayTally` (static, `Core/DayTally.cs`, 235L): counts bites per species, kills (+ the gene that did it when the code knew it), repels, plants lost, Doris's plant-eats and fruit set for the current Day; `RunManager.StartGrowthAndThreatPhase` calls `BeginDay(round)` (round 1 also resets the per-run pause flags). Reporters call it directly: `PlantGrowth.HandleBeingEaten` (`NoteBite` + the "-1 leaf · species" text) and `Die` (`NotePlantDied`), `AnimalController.StartDying` (`NoteKill`, "bunny down · gene" text), `DorisController.EatPlant`, `PlantCellManager` (fruit cell = `NoteFruitSet`), `FaunaManager.SpawnWaveEntryCoroutine` (`NoteGroupArrived`), `FearPayload` (`NoteRepel`). Sources are named only where known: `AnimalController.NoteDamageSource(name)` (thorns, Reactive Burst, explosive and poison genes, projectiles, a damaging status effect's name). D6 auto-pauses (`DuskRules.autoPauseFirstWave / autoPausePlantDeath / autoPauseDorisFirstEat`, default on): `DayTally.AutoPause` = `ExecutionPhaseDriver.SetPaused(true)` + `OnAutoPause(reason)`; `UIAutoPauseBannerController` (HUD panel, top edge, hides when the Day runs again) shows the reason. `EventCount` / `EventsLine()` / `BreakdownLine()` feed the F9 readout (the density test's number).
- **Supper:** `Abracodabra.Ecosystem.SupperRunner` (B3, 228L): visits living plants top row first then by x, `PlantGrowth.HarvestAllFruits(list)` (the optional list receives each item's fruit GameObject), flyer sprites arc to Doris one at a time (0.3 s, total ≤ `maxSupperSeconds` 8), `DorisController.FeedDoris(nutrition)` on arrival; `SupperResult` (fruit, nutrition, hunger and pips before/after, living plants), `OnSupperFinished` (for the B6 ledger), static `CountLivingPlants()`.
- **Feeding:** `FeedingSystem` (510L singleton; right-click flow, range checks, `FoodSelectionPopup` 648L UI with static `IsBlockingInput`), `IFeedable` implemented by `DorisController`, `AnimalFeedableAdapter`, `PlayerHungerSystem`. `ConsumableData` unifies FoodType/ItemDefinition/ItemInstance (+payload passthrough). B3: `restrictToPlanningPhase` (was `restrictToGrowthPhase`, `FormerlySerializedAs`): hand-feeding only in Planning, never in the Day; also ignored while the seed editor covers the world.
- **Status effects:** `StatusEffect` SO (freeze-type stacking → full immobilize at max stacks, decay, per-tick damage/heal, move-cost penalty, visual tint interpolation) + `StatusEffectManager` per entity; `EnvironmentalStatusEffectSystem` maps tiles/tools → effects (O(1) lookup dicts; hooks `PlayerActionManager.OnActionExecuted` + tile changes).
- **Fireflies:** `FireflyManager` (night spawning, ≤50, photosynthesis bonus radius) + `FireflyController` (tick movement toward scents/growing plants, wall-clock visual flight). **Scents:** `ScentSource`/`ScentDefinition`/`ScentLibrary`. **Poop:** `PoopController` (fertilizer entity, tick lifetime).

---

## 8. UI Layer (UI Toolkit)

- **`GameUIManager` (1043L after Block 1, was 875; biggest file)** — creates/wires all controllers from `GameUI_Document.uxml` (PlanningPanel: SeedEditor + Inventory + SpecSheet; HUDPanel: tick info, hotbar, hunger bars, Doris state). Owns `playerInventory` (list of `UIInventoryItem`) and registers it into `InventoryService` → **UI owns game data (review item B1)**. Panel switching driven by `RunManager.OnRunStateChanged`.
- Controllers: `UIInventoryGridController` (grid + selection + locked seed slot), `UIHotbarController` (keys 1–8), `UIDragDropController` (all drag-drop incl. gene editor internal moves; 6 events), `UISeedEditorController` (784L; slots for passive/active/modifier/payload, name editor, color picker, Planning-only lock), `UISpecSheetController` (quality via `SeedQualityCalculator`, leaf-balance display from `SeedTooltipData`).
- `UIInventoryItem` = unified wrapper (Seed | Tool | Gene | Resource) with `SeedRuntimeState`, `GeneInstance`, `ResourceInstance`.
- **Block 1 B3 UI (2026-10-26):** `UIDorisBellyController` (plain class, GameUIManager-owned, lives in the HUD panel: 8 pips filled/hollow above Doris, repositioned in `GameUIManager.LateUpdate`, hover box from `DorisController.OnHoverChanged`); the HUD footer label is set to "DORIS'S BELLY" and shows "N / 8" in one sage colour; `UIRunEndOverlayController` (plain class on the root: kicker "DUSK n OF 5", title, reason, tonight's supper, seed, Restart (same seed) / New seed → `RunManager.RestartRun`), shown on `RunState.GameOver`; **garden view** (`GameUIManager.SetGardenView`, key G, VIEW GARDEN / EDIT SEEDS buttons, a second START DAY in the garden bar; static `GameUIManager.IsPlanningPanelOpen` makes `PlayerTileInteractor` and `FeedingSystem` ignore the mouse while the seed editor covers the world). Accessors `GameUIManager.DorisBelly` / `RunEndOverlay` for checks.
- **Reads as (Block 1 B7, 2026-10-26):** `UISpecSheetController.BuildReadsAs(PlantGeneRuntimeState)` (static, pure) writes one sentence from the `RuntimeSequenceSlot` list (no buffer model / `SequenceParser` exists; when it does, only this method changes). Shown under a READS AS header in `UISeedEditorController` (rebuilt at the end of `DisplaySeed` and in `Clear`) and in the spec sheet under the description (seeds only). New genes need a branch in `DescribeStep` / `DescribeReaction` / `PayloadAdjectives` / `StepNoun`. `GameUIManager.SeedEditor` / `.SpecSheet` accessors.
- **`UIDevReadoutController` (temporary dev readout, Block 1 B1, 2026-10-26):** not in any scene; a `[RuntimeInitializeOnLoadMethod(AfterSceneLoad)]` bootstrap makes one `DontDestroyOnLoad` object with its own `UIDocument` (borrows the scene UIDocument's PanelSettings, sortingOrder 900). F9 toggles (hidden by default). Bottom-left, fixed size: tick, phase (+ Day pause/speed), plant checksum (living plants · cells · energy · fruit). `UIDevReadoutController.ComputePlantChecksum()` is public static for CLI checks. Remove after the Block 1 playtest.
- **Static services** (`Abracodabra.UI.Genes`): `InventoryService` (inventory list, `OnInventoryChanged`/`OnSlotChanged`, hotbar rows, `AddHarvestedItem`), `HotbarSelectionService` (`SelectedItem`/`SelectedIndex`, `OnSelectionChanged` → consumed by `PlayerTileInteractor` and `GameUIManager`).
- World-space (TextMeshPro, outside UI Toolkit): `WorldHoverTooltip` (364L), `ThoughtBubbleController`, `PlantWorldUI`, `HungerUI` (legacy UGUI Slider — small second stack, review B3).
- **Reflection coupling (review B2), 3 sites in `GameUIManager`/`WorldHoverTooltip`:** DorisHungerSystem event binding via `Type.GetType` + `Delegate.CreateDelegate`; WaveManager private tick fields; DorisController type lookup.
- UXML: `GameUI_Document`, `InventorySlot`, `GeneSlot`. USS: Base/Planning/HUD/Slots/FoodSelectionPopup (~960 lines total).

---

## 9. World, Tiles & ProcGen

- **Grid:** `GridPosition` struct (int x,y; distances, neighbors) · `GridEntity` (tween between cells, `isPositionLocked` for placements) · `MultiTileEntity` + `MultiTileFootprint` SO (granular blocking: movement/seedPlanting/toolUsage) · `GridPositionManager` (828L; occupancy dicts, A*, radius queries, `IsMovementBlockedAt`/`IsSeedPlantingBlockedAt`/`IsToolUsageBlockedAt`). Z stays 0 throughout.
- **Tiles:** `TileInteractionManager` (767L; DualGrid package integration via `TileDefinitionMapping`, hover cell + range check, `ApplyToolAction`, timed tile reversion `revertAfterTicks`) + `TileDefinition` (priority, tint, revert chain, water flag) + `TileInteractionLibrary` (rules: tool×fromTile→toTile; tool refill rules e.g. watering can at water; hover colors) + `TileInteractionRule`. `WorldInteractionHelper` resolves `IWorldInteractable` by priority before tile fallthrough.
- **Placement:** `PlantPlacementManager` (spawns via `NodeExecutor.SpawnPlantFromState`, invalid-tile blacklist, occupancy dict) + `PlantGrowthModifierManager` (§6).
- **ProcGen:** `ProceduralMapGenerator` (Simplex octaves → normalized noise → `BiomeLayer` thresholds → tiles via TileInteractionManager, underlay support) + `MapGenerationProfile` (seed, `useRandomSeed`) + `MapConfiguration` (mapSize, reference resolution) + `SimplexNoise` static + `MapBoundsVisualizer`, `SceneSetupManager`, `ResolutionManager` (PPU profiles) + editor wrappers.
- **Visual:** `WaterReflection` (506L, over-engineered per Todo.md) + `WaterReflectionManager` defaults, `NightColorPostProcess`, `RuntimeCircleDrawer`, `FloatingCombatText` (static `Spawn`).

---

## 10. Event Catalog (who talks to whom)

| Event | Source | Known consumers |
|---|---|---|
| `OnTickStarted/Advanced/Completed` | TickManager | WaveManager (Advanced), GameUIManager (Advanced) |
| `OnRunStateChanged` / `OnPhaseChanged` / `OnRoundChanged` | RunManager | GameUIManager, GeneRewardSystem (Round), DorisController (mercy-cap reset), ExecutionPhaseDriver, phase-gated systems poll `CurrentState` |
| `OnRunEnded(RunOutcome, reason)` (B3) | RunManager | (none yet; the end overlay reads `Outcome`/`EndReason` on `GameOver`) |
| `OnSupperFinished(SupperResult)` (B3) | SupperRunner | RunManager (via the `Run` callback) |
| `OnDuskLedger(SupperResult, DuskVerdict)` (B6) | RunManager | GameUIManager → UIDuskLedgerController; `ContinueFromLedger()` applies the verdict |
| `OnHoverChanged(bool)` (B3) | DorisController | UIDorisBellyController |
| `DayTally.OnAutoPause(string reason)` (B5, static) | DayTally | UIAutoPauseBannerController |
| `OnActionExecuted` / `OnActionFailed` | PlayerActionManager | EnvironmentalStatusEffectSystem |
| `OnLeafConsumed` | PlantGrowth | ReactiveBurstHandler |
| GeneEventBus: `GeneExecutedEvent`, `SequenceCompletedEvent`, `GeneValidationFailedEvent` | PlantSequenceExecutor | (UI/analytics-ready; few subscribers today) |
| `OnInventoryChanged` / `OnSlotChanged` | InventoryService | GameUIManager |
| `OnSelectionChanged` | HotbarSelectionService | GameUIManager, PlayerTileInteractor (poll) |
| `OnToolChanged` / `OnUsesChanged` | ToolSwitcher | GameUIManager |
| `OnHungerChanged`/`OnStateChanged`/`OnStarvation` | PlayerHungerSystem | HungerUI, GameUIManager, RunManager (starvation→GameOver) |
| Doris events (hunger/state/fed/ate-plant) | DorisHungerSystem/Controller | DorisController; GameUIManager **via reflection** |
| `OnPositionChanged`/`OnMovementStart/Complete` | GridEntity | GridPositionManager occupancy |
| GameEvent SO assets (3 init phases) | InitializationManager | GameEventListener components |
| `OnMinigameStarted/Completed` | MinigameManager | PlayerActionManager callback |
| `OnPhaseChanged` (day/night) | WeatherManager | NightColorPostProcess, FireflyManager (poll intensity) |

---

## 11. Big Files (refactor watch list)

`GameUIManager` 1043 (post-B7, 2026-10-26; was 875) · `PlantGrowth` 913 · `GridPositionManager` 828 · `TileInteractionManager` 775 · `UISeedEditorController` 784 · `AnimalMovement` 738 · `FoodSelectionPopup` 648 · `DorisController` 620 · `AnimalController` 635 · `UISpecSheetController` 408 · `FeedingSystem` 510 · `WaterReflection` 506. (Review B4 targets PlantGrowth split before archetype content.)

---

## 12. Red Flags & Invariant Violations (verified)

1. **Raw `UnityEngine.Random` in gameplay** (remaining after A6, scoped follow-up): BasicFruitGene (shuffle), CloudWorldEffect (regrow chance), AnimalController (pest leaf pick, flee dir), AnimalMovement (wander/flee), AnimalBehavior (poop timing), FireflyController (lifetime/speed/targets), PlantPlacementManager (cosmetic jitter). FIXED 2026-07-05: GeneRewardSystem drops + PlantGrowth leaf-death now use `IDeterministicRandom`, seeded per-run by `RunManager.RunSeed`.
2. **Wall-clock inside tick logic** (review B5): ~~`FaunaManager` WaitForSeconds spawn delays~~ (fixed in B4, now Day ticks); `PlayerActionManager.multiTickActionDelay` coroutine; animal damage-flash and firefly visuals mix `Time.deltaTime` with tick conversions; `PlantGrowth.DelayedGrowthStart` coroutine.
3. **`Invoke`-based init timing (remaining after A5):** FeedingSystem 0.2s, AnimalFeedableAdapter/PlayerHungerSystem 0.3s. (StartingLoadoutApplier + GeneRewardSystem delays removed 2026-07-05.)
4. **Reflection coupling** (review B2): 3 sites (§8).
5. **UI owns inventory data** (review B1): `GameUIManager.playerInventory` + static `InventoryService`.
6. **String-keyed tile modifiers:** `PlantGrowthModifierManager` looks up by `TileDefinition.displayName`.
7. **O(n) spatial queries per effect tick:** `TargetFinder`/`FindObjectsByType`; `TickDebugMonitor` FindObjectsByType per frame.
8. **Dead/vestigial code:** `WaveManager.IsCurrentWaveDefeated()` (timer-semantics misnomer, see §1), TickConfiguration presets, `HarvestableTag` (references defunct NodeGraph), `ITriggerTarget` barely used, `GeneEffectPool` registered but effects mostly instantiated directly, `FruitConsumptionHandler` incomplete, `PlaceholderGene`/`SafeGeneLoader` fine but `RactiveBurstHandler.cs` filename typo.
9. **Two HUD stacks (minor, review B3):** UI Toolkit HUD + legacy `HungerUI` (UGUI Slider) + world-space TMP elements.

---

## 13. Assets, Scenes & Docs

- `Assets/Scriptable Objects/` — 51 `.asset` files (recounted 2026-07-06): Animals, Animals Diet, Doris, Fireflies, Food, **Genes** (Active/Modifier/Passive/Payload + `GeneLibrary.asset`), Items, Life Thoughts, Map Generation, Minigames, MultiTiles, Scents, Settings, Status Effects, Tiles, Tools, Waves. `Assets/Prefabs/` — 41 (Ecosystem/General/Tiles). Scenes: `SampleScene.unity` (**primary — the sole scene in Build Settings**; ExecutionPhaseDriver wired here 2026-07-06), `MainScene.unity` (secondary, not in build). No custom Resources/ loading — all direct references (good), **except** (B3, 2026-10-26) `Assets/Scriptable Objects/Run/Resources/DuskRules.asset`, loaded by `DuskRules.Active` so the run rules need no scene wiring. Waves: Wave_000–Wave_004 (003/004 added in B3, bunnies only).
- Third-party: skner DualGrid, local package at `Packages/com.skner.dualgrid` (tilemap rendering; guides in `05_Reference/`).
- Historical docs relocated 2026-07-05: GGS-era Documentation 00–06, `PROJECT_KNOWLEDGE_BASE.md`, `Memory.txt` → `99_Archive/`; WeGo rework docs 1–5 (+ divergent "5 - Copy") → `02_Design/WeGo/`; old `Todo.md` → the code-optimization backlog (merged into `03_Tasks/Roadmap.md` in 2026-10); DualGrid guides (incl. old 07 package guide) → `05_Reference/`. Current canon: root `CLAUDE.md`, `01_Core/` (memory · this map), `02_Design/Gene_Systems_Deep_Dive.md`; routing rules in `CLAUDE.md` §4.

---

## Next action anchor

A-pack code is applied and wired (2026-07-06). The Part 4 play checks that were never run are listed in `03_Tasks/Roadmap.md` (#6). Next code block: `03_Tasks/Roadmap.md` §1 (asmdef split + golden-run harness, then the A proof of concept). Map last verified against live disk 2026-07-06; spot-check symbols before relying on them.
