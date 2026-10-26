# Block 2 recon: what each candidate would cost (code lane, read-only, 2026-10-26)

Fable · high · unattended. Point-in-time doc: not edited after it closes, except a "Superseded by …" line.
**Read:** `CLAUDE.md`, `01_Core/projectmemory.md`, `02_Design/Restart_Direction_2026-10-05.md` §2–3, `03_Tasks/Roadmap.md` row 10, `Phase_Identity_Final_Refinement_2026-07-20.md` §2.1, the Ledger (D3, D8), `Commit_And_Watch_Loop_Design_2026-07-06.md` §2.1, F1 guide G1 (`2026-07_Pack_Implementation_Guides.md`), `04_Reviews/2026-10_Block1_B0-B3_Code_Review.md` (cited as *Review F#*; I re-read only F3–F6).
**Method:** code read from the working tree under `Assets/Scripts/`. `git status` before and after showed only `SampleScene.unity` and `ProjectSettings.asset` modified, so scripts equal HEAD (`8d1b464` at the end; `a91dbf4` at the start). B4–B8 had not landed in anything I read (`DayTally` does not exist yet). Nothing was edited, no Unity CLI, no git writes. The one helper script (cross-folder type dependencies) lives in the session scratchpad, not the repo.
**Unit:** one *session* = one Block 1-style batch (Sonnet · high, 45–90 min, one `/clear` apart). Estimates are mine, with the main uncertainty named per candidate.

## 0. Findings that apply to several candidates

1. **Block 1 changed the premise of A and C.** In the Day nothing the player does reaches the world: `PlayerTileInteractor.Update` (`WorldInteraction/Placement/PlayerTileInteractor.cs:34`), `GardenerController.Update` (`:112`) and `TileInteractionManager.CheckAndRefillTool` (`:119`) are Planning-only, and supper (`SupperRunner.cs:96`) harvests every fruit automatically. The §2.1 verbs "ripe plant → harvest" and "pest → shoo" lost the first and still lack the second (Shoo is B8, cuttable).
2. **The Day will have exactly one click owner.** If B8 (Shoo) lands, it is the first Day click path. Mark & Go would be the second. They need one shared "Day click" component, not two `Input.GetMouseButtonDown` readers.
3. **`ReactiveBurstGene` has never run in the shipped project.** There is no `.asset` of it anywhere under `Assets/` and none of `Assets/Scriptable Objects/Genes/Active/` (only BasicFruit, Cloud, Aura). The class, `ReactiveBurstHandler` and the executor wiring exist and compile; the path is untested end to end.
4. **Assembly-CSharp is hard-wired in serialized data and in code** (E, §5): 10 persistent `UnityEvent` targets in `SampleScene.unity` and 2 `Type.GetType("…, Assembly-CSharp")` calls.
5. **Same-seed replay is not deterministic yet** (*Review F5*; my grep agrees: ~40 `UnityEngine.Random` hits in 15 files, 8 in `FaunaManager`, 6 in `AnimalController`, 4 in `AnimalMovement`, 2 in `AnimalBehavior`; also `BasicFruitGene.cs:58` shuffles fruit spawn points with `Random.value`). This caps what a golden run (E) can cover and what any candidate's playtest can replay.
6. **Fruit have no age.** `Fruit.GrowthTime` is set (`BasicFruitGene.cs:99`) and only logged (`Fruit.StartGrowing`); `Fruit` is not tick-registered (C).

---

## A. Mark & Go v0

**Premise:** click a tile → the gardener paths there and does the contextual verb; queue cap 3; ETA preview; re-click cancels; flag pin.

**Touches (verified)**
- `GardenerController.cs` (259 lines). `ITickUpdateable` is registered in `Start`; `OnTickUpdate` only ticks status effects and is not phase-gated, so path-following can live there (one step per tick, or per N). `TryMove` (`:176`) has a `!ActionsDriveTicks` branch that moves for free, unreachable from input since B2. `Update` keeps WASD Planning-only.
- `GridPositionManager.GetPath(start, end, allowDiagonal)` (`Ticks/GridPositionManager.cs:603`): **A\*, 4-neighbour, Manhattan heuristic, already exists**; `PathExists` too. `IsMovementBlockedAt` excludes tile occupants (plants, pests, Doris), but the end cell may be blocked (`neighbor != end`), so "go to a plant" returns a path ending on the plant: drop the last step and stand adjacent. `GetLowestFScore` scans a `HashSet` per step (O(n²)), fine at this map size, ties follow enumeration order (stable in practice, not by contract).
- `GridEntity.SetPosition(pos)` tweens (wall-clock) and raises `OnPositionChanged` (status-tile checks). Step cost: `PlayerActionManager.GetMovementTickCost` includes `AdditionalMoveTicks` from status effects.
- Executing a verb without the mouse: **`TileInteractionManager.ApplyToolAction` reads `currentlyHoveredCell` (`:184-187`) and cannot be used.** Reuse `TileInteractionManagerExtensions.ApplyToolAtPosition(tool, cell)` (`Minigames/…Extensions.cs:15`, hover-free, the minigame's auto-water) and `PlayerActionManager.ExecutePlayerAction(Harvest, cell)` (already takes a cell). `ToolSwitcher.TryConsumeUse` must be called by the executor (the hover path does it in `ExecuteToolUse`).
- Contextual verb lookup: `TileInteractionLibrary.rules` (tool × fromTile → toTile) and `refillRules` (tool × source tile): "dry tilled soil + watering can" and "can empty → nearest water tile" are table lookups, no new data.
- Hover cell: `TileInteractionManager.HandleTileHover` runs in every phase (`Update:116`), so `CurrentlyHoveredCell` is available in the Day.

**Reuse:** A\*, `ApplyToolAtPosition`, hover cell, tile rule tables, `GridDebugVisualizer` (radius requests; a path line is not among them).
**New:** `MarkQueue` (order list, cap 3, FIFO, cancel-by-reclick), path-follower on the gardener, verb resolver, a Day click component (finding 2), path + ETA preview (line renderer or tile tints, shape not hue), flag pins, rest position, ledger counts (`DayTally` from B5 is the natural sink).
**Risks**
- **Verb set is thin in Block 1's loop.** Without harvest, the Day verbs are water (wet dirt reverts via `TileInteractionManager.UpdateReversionTicks`, so there is a recurring chore), refill, and Shoo if B8 exists. That is the "lean-in chore" category Restart_Direction E4 argued against; the batch cost is fine, the *design* risk is the point.
- The gardener stands wherever Planning left her; pests block paths and targets.
- Day pause (Space) must still accept marks; ticks are the only clock, so `ActionsDriveTicks == false` makes actions free (already true).
- Adds a Day lever, which confounds the density test unless logged separately.
- `Input.*` legacy reads (37 sites) stay; no Input System work.
**Estimate:** 3 sessions (2 if only water + refill), +1 for tuning/polish. Biggest uncertainty: feel of walk speed (derived rate ≈ 2 ticks/s at 1×, so 1 tile/tick ≈ 2 tiles/s).
**Batch order:** A1 order model + tick path-follow + water verb executed on arrival (CLI-testable, no input) · A2 Day click component, queue/cancel/pin/ETA preview (shared with Shoo if built) · A3 refill + shoo/other verbs, ledger counts, tuning · (A4 polish).

---

## B. Trigger genes: "On Bitten", "At Dusk"

**Touches (verified)**
- `ActiveGene.isTriggerType` (`Genes/Core/ActiveGene.cs:28`). Only consumers: `PlantSequenceExecutor.InitializeTriggerGenes` (`:65-101`, hard-coded `is ReactiveBurstGene`, comment "Future trigger types can be added here") and the free pass in `TryExecuteCurrentSlot` (`:166`).
- `ReactiveBurstHandler` (`Genes/Implementations/Active/RactiveBurstHandler.cs`, 182 lines; file name typo kept): subscribes `PlantGrowth.OnLeafConsumed`, has the cooldown (B1-gated), energy spend, AoE via `TargetFinder.FindCreaturesInRadius`, and the payload loop (`:131-159`: builds a `PayloadContext` with `target = creature.gameObject`).
- `OnLeafConsumed` is raised at **two** sites: `PlantGrowth.cs:401` (`DestroyRandomLeaf`, any source: explosive self-damage, pruning, legacy) and `:699` (`HandleBeingEaten(AnimalController eater, PlantCell)`, pests only, **knows the biter**; called from `AnimalController.cs:349` and `AnimalBehavior.cs:54`). "On Bitten" is therefore a new event (`OnBitten(plant, eater, coord)`) at `:699`, not a rename of the existing one.
- "At Dusk": `RunManager.OnRunStateChanged(RunState.Dusk)` exists and fires synchronously inside `SetState` in `BeginDusk` (`RunManager.cs:228-230`), before `SupperRunner.Run`. A subscriber can therefore change fruit before supper. Ticks are stopped in `Dusk`, so no tick gating is needed.
- Payload targets are **creature-only** for every substance payload (`PayloadContext.target`); only Nutritious/Healing mean anything at dusk. "At Dusk" needs either a new virtual on `PayloadGene` (apply to fruit/plant) or its own content (e.g. dusk regrowth, a second fruit burst). `PayloadGene.ConfigureFruit(Fruit, instance)` exists for spawn time.
- `GeneLibrary.asset` must list any new gene; new assets are Editor work (ask Milan, finding 3).

**Clash with F1's buffer model (G1.1–G1.3)**
- Slot model today: a trigger Active sits in the active column with its own modifier/payload columns, so On Bitten *already fits* as a slot. "Opens a sentence" (trigger followed by free-ordered payloads until the next Active) is a buffer-model concept; it needs F1.
- F1 **rewrites `PlantSequenceExecutor.cs` whole** (G1.3), keeps `InitializeTriggerGenes` wiring "identical" but iterating `compiled.groups where isTrigger`, and makes trigger groups cost 0 ticks. The handler interface `(plant, gene, payloadInstances, modifierInstances, activeInstance)` is what survives. So B survives F1 if it touches the executor only inside `InitializeTriggerGenes` (turn `is ReactiveBurstGene` into a small registry) and keeps all new logic in new files. Anything done in `TryExecuteCurrentSlot` is thrown away.
- UI readers of the slot model that F1 also changes: `UISeedEditorController`, `UIDragDropController` (positional column → slot type), `UISpecSheetController`, `SeedTooltipData`. B adds no UI of its own except tooltip text (`GetTooltip`).
- **Collision with Block 1 in flight:** B5 edits `HandleBeingEaten` (~`:688`) and the text spawn near `RactiveBurstHandler.cs:165`; B's event goes on the same line. Do B after B5 lands; `DayTally` is then the consumer for "kills by gene".

**Reuse:** handler pattern, payload loop, cooldown/energy code, `RunManager.OnRunStateChanged`, `FloatingCombatText`, B5's attribution.
**New:** `OnBitten` event; `BittenTriggerGene` + generic trigger registry; `DuskTriggerGene` + handler; a fruit/plant payload hook (At Dusk); assets + library entries; first end-to-end test of the burst path (finding 3).
**Risks:** untested burst path (finding 3); `TargetFinder` uses `FindObjectsByType` (order not guaranteed, matters only for AoE damage order); biter-targeting needs an `AnimalController` that may be dying (`IsDying` guard exists); the At Dusk effect is a design question the code cannot answer.
**Estimate:** On Bitten v0 = 2 sessions (+ asset authoring with Milan); At Dusk = +1–2 (1 if it reuses Nutritious/Healing only); "reads as" wording = B7's job. Total 3–4.
**Batch order:** B1 author a ReactiveBurst asset, prove the existing handler under the B1 gate by CLI · B2 `OnBitten` + trigger registry + `BittenTriggerGene` (target = biter) · B3 At Dusk + payload hook + one gene · (B4 tooltips/ledger text).

---

## C. Ripeness windows + the U1 Perfect/Good split

**Touches (verified)**
- Fruit = a plant cell GameObject with `Fruit` + `FoodItem` + `HarvestableTag`, spawned by `BasicFruitGene.Execute` → `ConfigureFruit` (`:96`). No age, no stage, no tick registration. `fruit.AddVisualEffect(Color)` is the only tint hook (hue alone is not allowed as the signal: CLAUDE.md §5).
- Harvest funnel: `PlantGrowth.HarvestAllFruits` (`:833-870`), called by `PlayerActionManager.ExecuteHarvest` (hand, Planning) and `SupperRunner` (`:96`). Both build `HarvestedItem(def, fruit.DynamicProperties, payloads)`; `ItemInstance.GetNutrition` multiplies `nutrition_multiplier` and adds `nutrition_add`. **A ripeness tier can ride `DynamicProperties` and be applied in one place** (`HarvestAllFruits`).
- **The age clock must be Day-ticks, not `TickManager.CurrentTick`.** Planning action ticks advance `CurrentTick` and would age fruit in frozen time, breaking the B1 invariant. Use a Day-tick counter (or `RunManager.PhaseTickHandler`) and gate the aging like `PlantGrowth.OnTickUpdate`.
- U1: `MinigameResultTier { …, Good, Perfect }` (`MinigameTypes.cs:37-38`), `TimingCircleConfig` evaluates tiers, `IsSuccess` is true for both. The only consumer is the planting circle: `MinigameManager.ApplyPlantingReward` (`:217`) waters on any success; Perfect only changes a log prefix (`:227`) and a debug string (`PlayerActionManager.cs:263`). The split needs a reward definition per tier (a ScriptableObject; today a single `wateringCanTool` field) and a visible difference.
- Content: 1 `BasicFruitGene` asset, 1 `ItemDefinition` (`Item_Nutri-Berry`).

**Three shapes, different dependencies**
- **C-supper (ripeness scales supper nutrition):** no lean-in. Dusk reads each fruit's stage; Planning becomes a cycle-length puzzle (energy, `baseRechargeTime`, Day = 100 ticks). Independent of A. It is not a timing skill and does not need U1.
- **C-windows (Day hand-harvest, Perfect = arrive inside the window):** **hard dependency on A** (a Day verb) and a harvest verb the Day currently lacks (finding 1). This is the §2.4 design.
- **C-basket (a Harvest Basket fixture collects at Good):** depends on fixture placement (D) and still needs A for Perfect.

**Reuse:** `DynamicProperties` plumbing, `HarvestAllFruits` funnel, `Fruit.GrowthTime` field, `AddVisualEffect`, the tier enum.
**New:** fruit age + stage thresholds (a `RipenessRules` SO or `DuskRules` fields), gated aging, a telegraph (sparkle/shape, before ripe), the U1 reward SO and a Perfect effect, ledger line ("Perfect ×1").
**Risks:** animals eat fruit by nutrition regardless of ripeness (`AnimalNeeds`; unripe fruit stays bait: decide); `Fruit` objects are destroyed at harvest (copy the look first, as supper does); telegraph visuals are art work; the "Perfect ≡ Good" warning in the Ledger stands until U1 exists.
**Estimate:** C-supper 1.5–2 sessions; U1 alone 1; C-windows on top of A 2–3 (+1 tuning). Standalone without A: 2–3. With A: A + 3–4.
**Batch order:** C1 aging + stage + supper multiplier (CLI-testable) · C2 telegraph visuals + ledger line · C3 U1 reward SO + Perfect effect · C4 (only with A) window-aware harvest verb + tuning.

---

## D. Doris Bowl + Cravings

**Touches (verified)**
- `DorisController` (619 lines): `ReceiveFood(ConsumableData, feeder)` applies `DorisDefinition.GetFoodCategoryMultiplier(category)` and `CanAcceptFood`; **supper uses the legacy `FeedDoris(nutrition)` (`DorisController.cs:389`, `SupperRunner.cs:111`), which skips multipliers and `OnFed(ConsumableData, …)`.** Cravings means routing supper through `ConsumableData` (`new ConsumableData(ItemInstance)` exists, `ConsumableData.cs:53`).
- `FoodType.FoodCategory` = {Plant_Leaf, Plant_Fruit, Plant_Stem, Plant_Seed, Other}; every fruit is `Plant_Fruit` (`InferCategoryFromItem`), so category cannot express a craving. Keys available today: `ItemDefinition` (one exists) or the fruit's payload flavour (`ItemInstance.payloads`, e.g. Nutritious vs others). More fruit kinds = new content.
- No craving, mood or digestion code exists (grep: none; `DorisMoodSystem`/`DorisDigestionSystem` are design only).
- UI to extend rather than build: `UIDorisBellyController` (pips + hover box, B3), `ThoughtBubbleController` (exists), ledger (B6).
- **No fixture-placement framework.** Only plants are placeable (`PlantPlacementManager`, hotbar `UIInventoryItem.ItemType` = Seed/Tool/Gene/Resource). Doris is scene-placed (`MultiTile_001_Doris.asset`, `MultiTileEntity`). The grid side exists: `GridPositionManager.RegisterMultiTileEntityAtPosition`, `IsSeedPlantingBlockedAt`, `MultiTileFootprint` blocking flags.
- **The Bowl's original job (provision → she self-serves in the Day, Commit_And_Watch §2.1) overlaps B3's supper**, and the Ledger already said a Bowl without Cravings "reads as a checkbox". Review F3 shows hand-feeding/harvest in Planning is still open, which decides whether there is anything to provision.

**Reuse:** `ReceiveFood` multiplier path, `ConsumableData`, belly UI/hover, thought bubbles, `DuskRules` for tunables, `MultiTileFootprint`.
**New:** craving state per round (deterministic via `IDeterministicRandom`), supper rerouted through `ConsumableData`, craving UI + ledger line, ≥ 3 fruit kinds or flavour keys (assets), and for the Bowl: a placeable item type, placement + occupancy, stock, a coverage/hand-off rule, UI.
**Risks:** content gap (assets; Editor work, ask Milan); `GameUIManager` is 990 lines (*Review F13*: move out before adding); Bowl semantics must be redefined after supper (stockpile? midday feeding? a belly buffer?), which is a design call; `DorisController.cs` growth.
**Estimate:** Cravings alone 2–3 sessions; Bowl + placement framework +3–4; both 5–7. Uncertainty: what the Bowl is now.
**Batch order:** D1 supper through `ConsumableData` + multiplier test (no visible change) · D2 craving state + belly hover/bubble + ledger line · D3 fruit kinds/flavours (assets) · D4 placeable item type + Bowl (only if kept).

---

## E. asmdef split + golden-run harness

**Namespaces vs folders (verified by scan, 205 `.cs` in `Assets/Scripts`):** 118 files declare a namespace, **86 are global**. Global scope: most of `Ecosystem` (32 of 41), `PlantSystem` (13 of 15), `WorldInteraction` (13 of 19), `Core` (6 of 13), `Visual` (5 of 5), `Items`, `UI`, 3 of `Ticks`, plus `PlayerActionManager`, `PlayerTileInteractor`, `HarvestedItem`, `WaveManager`, `FaunaManager`, `FireflyManager`, `WeatherManager`, `EnvironmentalStatusEffectSystem`, `PlantPlacementManager`, `PlantGrowthModifierManager`. Namespaced: `Abracodabra.Genes.*` (57 of 58 files), `WegoSystem` (Ticks/Core), `Abracodabra.UI.Toolkit`/`.UI.Genes`, `.Ecosystem`/`.Feeding`, `.Minigames`, `WegoSystem.ProceduralGeneration`. Namespaces do not matter to asmdefs; references do.

**A per-folder split is impossible without a refactor.** The type-reference scan between the 12 code folders finds almost every touching pair mutual: UI ↔ Ecosystem/Genes/WorldInteraction (e.g. Genes → `InventoryService`/`UIInventoryItem`; Ecosystem → `GameUIManager`), Core ↔ Ticks/Genes/Ecosystem (`RunManager` is used everywhere; Core uses `SupperRunner`, `WaveDefinition`), Genes ↔ PlantSystem (`PlantGrowth`/`Fruit`/`GeneServices`), WorldInteraction ↔ Ecosystem/Minigames/Ticks. asmdefs forbid cycles.
**Feasible split:** `Abracodabra.Runtime` (everything under `Assets/Scripts` except `Editor` folders) + `Abracodabra.Editor` (editor-only; 8 files in `Scripts/Editor` and `ProceduralGeneration/Editor`; `WorldInteraction/Player/Editor` is empty) + `Abracodabra.Tests.EditMode` / `.PlayMode` (Test Framework 1.6.0 is in the manifest). Splitting Runtime further needs interfaces first; that is a separate block.

**What breaks (verified)**
1. **10 persistent `UnityEvent` targets in `SampleScene.unity`** carry `m_TargetAssemblyTypeName: <Type>, Assembly-CSharp` (lines 17994, 53423, 60212, 77919, 78273, 86805, 89954, 93226, 127266, 127518: Wave/Fauna/Weather/Firefly managers, `RunManager`, `GridPositionManager`, …; `GameEventListener` is the only runtime `UnityEvent` user). They stop resolving after the split. Fix is a mechanical text replace in the scene to the new assembly name (type names unchanged), done in a commit Milan makes after committing first. One entry (`UIManager`) targets a type that no longer exists. Whether an asmdef may be named `Assembly-CSharp` is unverified; do not rely on it.
2. **Two reflection lookups with the old assembly name:** `GameUIManager.cs:308` (`DorisHungerSystem, Assembly-CSharp`, feeds the belly/hunger binding at `:321-328,365`) and `WorldHoverTooltip.cs:322` (`DorisController`). They return null silently. Fix: replace with `typeof(...)` (same assembly, no cycle).
3. **Explicit references the new asmdef needs** (Assembly-CSharp got them automatically): `skner.DualGrid` (2 files), `Unity.TextMeshPro` (11 files), `UnityEngine.UI` (4), URP/Core render pipeline assemblies (`UnityEngine.Rendering.Universal` in 3, `UnityEngine.Rendering` in 2, `URPPixelPerfectCamera`), `UnityEngine.U2D`. The compiler will list any I missed; iterate with the CLI.
4. **Editor code** moves to its own asmdef that references Runtime (`includePlatforms: Editor`). The 9 runtime files with `using UnityEditor` are all inside `#if UNITY_EDITOR` (checked), so Runtime compiles for players.
5. **Safe:** scene/prefab/SO script references are GUID-based and do not change; no `[SerializeReference]`; `[RuntimeInitializeOnLoadMethod]` (5 sites) is assembly-agnostic; `HueFolders` already has its own editor asmdef.
6. File moves are not needed; asmdef files sit at `Assets/Scripts/` (Runtime) and in each `Editor` folder. Rider/VS project files regenerate.

**Golden-run harness: what exists and what is missing**
- Exists: `TickManager.AdvanceTick()` is public (`:71`), `ResetTicks`; `ExecutionPhaseDriver` can be disabled; `RunManager.RestartRun` + `SingletonRuntime.TearDownForRestart` (B3) reset the DontDestroyOnLoad singletons between runs; per-run `RunSeed` into `IDeterministicRandom`; `UIDevReadoutController.ComputePlantChecksum()` (plants · cells · energy · fruit) is a ready fingerprint (it lives in a UI class: move or wrap it for tests); the Unity CLI has `run_tests`; the B1/B3 scripted `eval` runs are the manual form of the same test.
- Missing: any `.asmdef`; `playModeTestRunnerEnabled: 0` in `ProjectSettings.asset:420` (unverified whether it gates anything here); determinism beyond plants and Doris (finding 5; *Review F5*); wall-clock inside tick logic (`FaunaManager` coroutine delays, `PlantGrowth.DelayedGrowthStart` 0.5 s, `WaitForSeconds` in 8 gameplay files), so wave timing varies with frame rate; fruit position shuffle (`BasicFruitGene.cs:58`; the checksum counts fruit, not positions).
- **Tiers:** (1) EditMode, pure logic: `DuskRules.BellyPipsFor` / `HungerMultiplierForRound` / `Judge` (a ScriptableObject, creatable in EditMode), `DeterministicRandom`, and F1's engine-free `SequenceParserCore` when it exists; (2) PlayMode golden, plants only: load `SampleScene`, fixed seed, plant the starter seed, step N ticks by `AdvanceTick`, assert the checksum; (3) wave-inclusive golden: needs the `Random` routing and the spawn timing moved to ticks (F4 G4.1/G4.2, 2–3 more sessions; not part of E).
- Both tests also protect the B1 invariant (Planning ticks must not change the checksum).

**Reuse:** B3's teardown, the checksum, `AdvanceTick`, the CLI `run_tests`, F1's sandbox vectors.
**New:** 4 asmdef files, 2 `typeof` fixes, a scene text patch, a test base class (scene load, singleton reset), the golden recording step.
**Risks:** a 30 s default CLI timeout and the 300 s wedge rule (use `--detach` for PlayMode runs); the scene patch touches a file another session is modifying right now (do it only when the tree is clean); the first compile after the split will surface reference errors in a batch; `SampleScene` init order in PlayMode needs a proven pattern (B2/B3 notes: wait a few seconds after `recompile_status` or init may not run).
**Estimate:** 2 sessions for the split + EditMode tests + plants-only golden; +2–3 for a wave-inclusive golden. Uncertainty: the references the compiler reveals.
**Batch order:** E1 asmdefs + references + the 2 reflection fixes + scene patch, zero errors by CLI, game still starts · E2 EditMode tests on `DuskRules` / RNG + PlayMode plants-only golden, recorded · (E3 route fauna `Random` and spawn timing through ticks, then a wave golden).

---

## Comparison (no recommendation: the design lane decides after the playtest)

| | A Mark & Go v0 | B Trigger genes | C Ripeness + U1 | D Bowl + Cravings | E asmdef + golden run |
| --- | --- | --- | --- | --- | --- |
| Cost (sessions) | 3–4 | 3–4 | 2–3 alone; A + 3–4 for windows | 2–3 Cravings; 5–7 with Bowl | 2 (+2–3 for waves) |
| Reuse | High: A\*, `ApplyToolAtPosition`, hover cell, tile rules | Medium: handler, payload loop, `OnRunStateChanged`; burst path untested | Medium: `DynamicProperties`, harvest funnel; no fruit age exists | Medium for Cravings (`ReceiveFood`, belly UI); low for the Bowl (no placement framework) | High: B3 teardown, checksum, `AdvanceTick`, CLI `run_tests` |
| Risk | Medium: thin Day verb set; Day click shared with Shoo; adds a lever | Medium-high: F1 rewrites the executor; B5 collision; At Dusk target undefined | Medium: aging must be Day-ticks; windows depend on A; telegraph art | Medium-high: content gap; Bowl meaning after supper; `GameUIManager` size | Medium: scene `UnityEvent` patch; silent reflection nulls; compile-error loop |
| Hard dependency | Day click owner (with B8); verbs from Planning-side tables | B5 landed; F1 interplay; assets via Milan | A for windows; D for the basket variant | Content assets; Review F3 decision (is there fruit to hand over?) | Clean tree for the scene patch |
| Block 1 collision | B8 Shoo, B5 `DayTally` | B5 (`HandleBeingEaten`, line 165) | B6 ledger | B6 ledger, B3 belly UI, F13 | Scene file modified by the active session |
| Protects / unlocks | Day lever for the playtest | Reactive grammar; F1-compatible if confined | Perfect/Good meaning; supper depth | Provisioning as a choice | Safety net for F1, F4 and every later pack (Roadmap #1) |
| Source item | §2.4 item 3 (deferred 2026-10-26) | D8 rival | D8 incumbent, §2.4 item 4 | D3 | Roadmap #1 |
