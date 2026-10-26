# Block 1, B0–B3: code review (2026-10-26)

Read-only review of `git log 2d33fe6..e2bda58` (16 commits, 18 C# files changed, 1718 lines added), read from the commits with `git show` / `git diff`, never from the working tree. Method: CLAUDE.md §1 and §2, projectmemory, the pack (`2026-10_Block1_Doris_By_Dusk.md` §1 and B0–B3). Nothing was compiled or run; every finding comes from reading code. Line numbers refer to the file as it stands at `e2bda58`.

**Summary:** no blocker for Checkpoint A. One real soft-lock path (F1), one latent skip of the dusk check (F2), and a cluster of "the invariant is stated stronger than the code" items (F3–F6). Determinism of the B3 code itself is clean; the non-determinism is in older fauna code that B3's "Restart (same seed)" button now advertises. Counts: Must 1, Should 11, Could 5.

---

## Findings, most severe first

### F1 · Supper has no failure path; one exception or a double `Run` freezes the game in `Dusk` — **Must**
- `SupperRunner.cs:49-55` (`Run` returns without calling `onFinished` when `IsRunning`), `:66-136` (the coroutine body has no try/catch; `onFinished` is called only on the last line, `:135`), `RunManager.cs:228-233` (`BeginDusk` sets `Dusk` first, then starts supper), `:236-237`.
- **Failure:** `HarvestAllFruits` (`:96`), `items[i].GetNutritionValue()` (`:110`) or `FeedDoris` (`:111`) throws on one odd fruit (a null `ConsumableData` or payload, a plant destroyed mid-iteration). Unity kills the coroutine silently, `IsRunning` stays true, `onFinished` never fires. State is `Dusk` for good: the driver is stopped, `TickManager.ActionsDriveTicks` is false, every click gate is false, no overlay. Milan sees a frozen garden and must stop Play. A second `Run` call (see F2) hits the same dead end.
- **Fix:** wrap the per-fruit body in try/catch (log, skip the fruit, continue); always reach the `onFinished` line. In `Run`, if already running, call `onFinished` with `LastResult` or stop the old coroutine first. Optional watchdog in `BeginDusk`: if `Dusk` lasts longer than `maxSupperSeconds + holdAfterSupper + 2 s` of real time, judge with whatever was fed.

### F2 · `StartNewPlanningPhase` called in `Dusk` skips the dusk check — **Should**
- `RunManager.cs:205-218`. The `Dusk` guard exists only for `GrowthAndThreat` (`:210`); from `Dusk` with the timer complete the code falls through to `StartNewRound()` (`:214`), from `Dusk` with the timer not complete to `SetState(Planning)` (`:217`).
- **Failure:** today the only caller is `WaveManager.EndCurrentWave` (`WaveManager.cs:227`, once per wave), so it is not reachable. Any later caller (B6's Continue button, B5 auto-pause code, a dev hotkey) that calls it during supper advances the round with no verdict while the supper coroutine keeps feeding. `HandleSupperFinished` then returns at `:237` and the loss or win is lost.
- **Fix:** add `if (currentState == RunState.Dusk) return;` next to the `GameOver` guard (`:200`). B6 should call a dedicated `ContinueFromDusk()`, not this method.

### F3 · Hand-feeding and hand-harvest in Planning contradict pack §1 ("food reaches Doris automatically at dusk; no carrying") — **Should** (Milan's call)
- `FeedingSystem.cs:23-26` (`restrictToPlanningPhase`, default true) and `:108-118`.
- **Failure:** B3 moved right-click feeding from the Day to Planning. In frozen time the player can harvest a mature plant by hand and feed Doris from inventory, topping up the belly for free before every Day. That bypasses the pack's rule that the garden is the economy and removes fruit that supper would count. The Done-when test (a)–(f) never exercises it.
- **Fix:** either set `restrictToPlanningPhase`'s allowed phase to none (`FeedingSystem` returns early in every phase while `DuskRules.handFeedingAllowed` is false; one bool in `DuskRules`) or write the decision into the Decision log as "feeding by hand stays, costs N ticks". Not a code defect, a pack contradiction that Checkpoint A will blur.

### F4 · Last Day's pests stay on the map across rounds (frozen, then live again) — **Should**
- `WaveManager.cs:160-163` and `:248-251` (`deletePreviousWaveAnimals`, scene value 0), `AnimalController.cs:142-146` (B1 gate), `RunManager.cs:275-283` (`StartNewRound` → `ResetForNewRound` does not clear when the flag is off).
- **Failure:** pests alive at dusk 1 freeze through supper and Planning, then act again on Day 2 next to Wave_001's new group. Round n therefore faces waves 1..n, not wave n. This breaks the pack §1 numbers (round 1 ≤ 3 pests; "never two groups within 60 s") from round 2 on and makes B4's tuning table meaningless. Measured B3 run (d) used forced rounds, so it would not show this.
- **Fix:** decide in `DuskRules` (`clearPestsAtDusk`, default true) and call `ClearAllActiveAnimals()` from `ResetForNewRound` when it is set; keep the scene flag only as a dev override. Decide before B4.

### F5 · "Restart (same seed)" is not the same run: raw `UnityEngine.Random` still drives the Day — **Should** (Must before B4)
- `FaunaManager.cs:155-179` (spawn edge and position), `AnimalController.cs:324` (which leaf a pest eats), `AnimalMovement.cs:169` (flee direction), `:466-481` (wander pause, direction shuffle), `AnimalBehavior.cs:61-68` (poop). CLAUDE.md §1.7 forbids raw `Random` in gameplay; projectmemory lists 36 legacy sites.
- **Failure:** the run end card prints the seed (`UIRunEndOverlayController.cs:115`) and offers "Restart (same seed)" (`:80`). Plants and Doris replay, pests do not: which leaf is eaten decides whether a plant dies, so the dusk verdict can differ on the same seed. A playtest note "I lost on seed X" cannot be replayed.
- **Fix:** route these five files through `GeneServices.Get<IDeterministicRandom>()` (mechanical, ~20 lines). Pack B4 already requires deterministic spawns. Until then, relabel the button "Restart (same garden)".

### F6 · Planning-tick invariant: three tick paths are still ungated — **Should**
- `EnvironmentalStatusEffectSystem.cs:92-95` → `RefreshAllEntityTileEffects` (`:124-137`) → `CheckAndApplyTileEffects` (`:139-166`), `PoopController.cs:79-89`, `PlantGrowthModifierManager.cs:81-84`.
- **Failure:** every Planning action tick the environmental system re-applies tile status effects (`ApplyStatusEffect`, stack++ for stackable ones, `StatusEffectManager.cs:84-93`) to every pest and to the gardener. Pest status managers are ticked by the gated `AnimalController`, so the effects never expire in Planning: a frozen pest standing on a stacking tile reaches max stacks while the player walks 40 steps, and starts the Day fully affected. `PoopController` lifetime also counts down in Planning (poop fades while walking). `PlantGrowthModifierManager` only logs tile changes (harmless today). projectmemory says "Every other `RegisterTickUpdateable` checked": these three are not in the gate list (Code facts, Planning-tick invariant).
- **Fix:** same early-out (`!= GrowthAndThreat → return`) in the first two; leave `PlantGrowthModifierManager` and cosmetics (Firefly*, `WeatherManager`, paused by `PauseCycleAtDay`) and say so in `Codebase_Map.md` §3. The checksum in the F9 readout would not catch this (it counts plants only); add pest status stacks to the harder test if you want proof.

### F7 · Missing `WaveManager` bypasses dusk entirely and the Day never ends — **Should**
- `RunManager.cs:208-218`, `WaveManager.cs:217-229`.
- **Failure:** the Day's end exists only as `WaveManager.EndCurrentWave`. With no `WaveManager` (scene variant, test scene) `StartGrowthAndThreatPhase` starts a Day that nothing ends; if it were called, `StartNewPlanningPhase` takes the `else` branch and returns to Planning without supper or verdict. The `CRITICAL` log in the singleton getter is the only signal. Same dependency if `StartWave` returns early (`WaveManager.cs:166-170`, no `TickConfiguration`): state stays `Idle`, no end.
- **Fix:** let `RunManager` own the Day length (`DuskRules.dayTicks`, or read `WaveManager`'s end tick) and call `BeginDusk` itself; at minimum log an error from `StartGrowthAndThreatPhase` when `WaveManager.Instance == null`.

### F8 · Each Day after the first opens unpaused, but the docs say "the Day starts paused" — **Should**
- `ExecutionPhaseDriver.cs:40` sets `isPaused = startPaused` once in `Start`; `HandleRunStateChanged` (`:57-61`) never re-applies it. Needs Milan (B0/B3 sections) and projectmemory (Snapshot, Next action) state the Day starts paused.
- **Failure:** Day 1 opens paused (scene `startPaused: 1`); the player presses Space, plays; Day 2 starts with `isPaused` still false and the clock runs the moment START DAY is clicked, so there is no "get ready" beat from round 2. D6's first-occurrence auto-pause (B5) will need this hook anyway.
- **Fix:** in `HandleRunStateChanged`, when `newState == GrowthAndThreat`, set `isPaused = startPaused` (and fire `OnPauseChanged`).

### F9 · Restart destroys `GameUIManager` from inside its own button callback — **Should**
- `UIRunEndOverlayController.cs:83-84, 125-130` → `RunManager.RestartRun` (`RunManager.cs:294-304`) → `SingletonRuntime.TearDownForRestart` → `SingletonMonoBehaviour.ReleaseForRestart` (`SingletonMonoBehaviour.cs:138-148`, `DestroyImmediate(gameObject)`).
- **Failure:** the `clicked` handler runs inside UI Toolkit's event dispatch on the `UIDocument` that `DestroyImmediate` is about to remove. It worked in B3's measurements (f), but this is the order Unity documents as unsafe; a different Unity patch or a second `clicked` listener may throw or leave the panel half torn down. The same `DestroyImmediate` also runs on `RunManager`, whose method is still on the stack (`:302` then `:303` executes on a destroyed object; it works only because C# keeps running).
- **Fix:** defer: `RestartRun` records the seed, hides the overlay, and a tiny non-singleton helper (`new GameObject("RestartHelper")` + `DontDestroyOnLoad`) runs `TearDownForRestart` + `LoadScene` next frame and then destroys itself. Keep `DestroyImmediate` inside the helper.

### F10 · Doris eats a plant with `Destroy(plant.gameObject)`, bypassing the death pipeline — **Should**
- `DorisController.cs:511-531` (`EatPlant`), compare `PlantGrowth.cs:370` (death goes through `DeathSequence` and `OnPlantDied`).
- **Failure:** B5 wants an auto-pause on "every plant death" and B6 a ledger "You lost 1 plant: Sunfern, to rabbits". A plant eaten by Doris fires `OnDorisAtePlant` but not `OnPlantDied`; its tile and `AllActivePlants` entry go at end-of-frame rather than through the pipeline B0 fixed (`4fbf5b5`). A ledger fed from `OnPlantDied` undercounts; one fed from both double-counts if the pipeline is later unified.
- **Fix:** call the plant's existing kill method with a cause (`Doris`) so there is one death path; keep `OnDorisAtePlant` for the mercy-cap and the pause. Do it before B5.

### F11 · Hover text hard-codes "once a Day"; the cap is a `DuskRules` field — **Should**
- `UIDorisBellyController.cs:142-145` vs `DuskRules.cs:44` (`maxPlantsEatenPerDay`). The `0` case (cap = never) also reads wrong ("she eats a plant").
- **Failure:** set `maxPlantsEatenPerDay` to 2 or 0 for the Checkpoint A tuning; the hover keeps promising one. Pack rule 42 (one name, two places) is about exactly this.
- **Fix:** build the third line from the rule: 0 → "she is too well fed to eat plants", 1 → "once a Day", n → "up to n a Day".

### F12 · Doris tie-break and no-Doris verdict are implicit — **Could**
- `DorisController.cs:491-503`: `dist < nearestDist` keeps the first of equal distances; order comes from `GetEntitiesInRadius` (`GridPositionManager.cs:529-543`), which is deterministic only because `GetTilesInCircle` is. With a mercy cap of one, which plant dies is now the whole outcome of a Day. Fix: tie-break explicitly by (y descending, x ascending), the same order supper uses.
- `RunManager.cs:241` / `SupperRunner.cs:119`: with no `DorisController` supper is skipped and the belly is judged as full, so the run cannot be lost through her (only a warning log). Fix: log an error once and refuse to start a Day without her (`RunManager.Initialize`), rather than a silent pass.

### F13 · `GameUIManager` growth to 990 lines: what to move out — **Should** (do before B6, which adds a banner)
- `GameUIManager.cs` 875 → 990 (B3 added ~115 net). CLAUDE.md §2.3 already puts it in the "patch only" class.
- Candidates, with current anchors:
  1. **Garden view + Start Day bar** (`CreateGardenViewControls` :433, `MakePlanningBarButton`, `SetGardenView` :481, `IsTextFieldFocused`, the static `IsPlanningPanelOpen`, G key in `Update`, `ShowPlanningUI`/`ShowHUDUI` flags): ~110 lines → `UIPlanningBarController` (owns the buttons and the panel/HUD toggle). Also the natural home for B6's phase banner.
  2. **Doris reflection subscription and footer** (`TryFindAndSubscribeToDoris` :303, `UnsubscribeFromDoris` :359, `OnDorisHungerChanged` :373, `UpdateDorisHungerDisplay` :589, `UpdateDorisStateDisplayByInt` :606): ~110 lines, reflection (`dorisType.GetEvent("OnHungerChanged")`) that `UIDorisBellyController` replaces with a typed event it already subscribes to. Fold the footer into that controller and delete the reflection.
  3. **Gene drag/drop handlers** (`HandleGeneDroppedToInventory` :771 … `HandleGeneDrop` :873): ~200 lines → `UIGeneEditHandlers`. Pre-existing, not B3's; do it when the file is next touched.
- After 1 + 2: ~770 lines. Result should be a coordinator, as invariant 5 describes.

### F14 · `DuskRules` is incomplete as the single source of tunables — **Could**
- **Two sources of truth for hunger rate:** `DuskRules.dorisHungerPerTick` (`DuskRules.cs:38`, 0.5) overrides `DorisDefinition.hungerPerTick` (2), via `DorisHungerSystem.cs:39-49`. The tooltip's "50 hunger over a 100-tick Day" silently depends on `ticksPerDay × waveDurationInDays` (`WaveManager.cs:173`); change the Day length and the belly budget changes with no warning. Fix: express it as `bellyPipsLostPerDayRound1` and derive the per-tick rate from the Day's tick count.
- **In code, not in `DuskRules`:** supper visuals `SupperRunner.cs:40` (sort boost 50), `:89`/`:201` (0.05 s floor), `:221` (0.15 pop), `:222` (0.6 shrink); end card strings "YOU MADE IT", "THE RUN IS OVER", "Restart (same seed)", "New seed" (`UIRunEndOverlayController.cs:80-81, 104`) while the three reason texts are in `DuskRules` (split ownership); garden bar offset `GameUIManager` `bottom = 132`. Visual constants are fine in code; the strings belong with `DuskRules` end texts.
- **Pack deviation, documented:** asset lives at `Run/Resources/DuskRules.asset` (pack said `Run/DuskRules.asset`) so `Resources.Load` works without wiring. Fine.

### F15 · Docs drift — **Could**
- projectmemory Code facts (Planning-tick invariant) says `PlayerHungerSystem` "still ticks in Planning"; `PlayerHungerSystem.cs:185-188` is gated on `GrowthAndThreat`. B1 open item "player hunger still ticks in Planning" is stale. Legacy `Input.*` count: G key adds a site (`GameUIManager` `Update`), memory says 36 / 37.
- `Codebase_Map.md` §3 should list the three ungated paths from F6 as "deliberately ungated" or gate them.

### F16 · Singleton teardown housekeeping — **Could**
- `SingletonMonoBehaviour.cs:35-42, 118`: `releasers` keeps one closure per singleton for the process life (cleared only by a restart or editor reset); destroyed singletons stay referenced until then. `RunManager.cs:92`: `PhaseTickHandler` is never unregistered (safe today because `TickManager` dies with it). `RunManager.OnDestroy` (`:127-132`) calls `FindFirstObjectByType` inside teardown (finds an old, half-destroyed object or null; harmless). Fix: cache the subscribed `PlayerHungerSystem` instead of searching; unregister releasers in `OnDestroy`.

### F17 · Belly readout is created once and only if Doris exists at init — **Could**
- `GameUIManager.cs` (InitializeHUD, ~277-290): `FindFirstObjectByType<DorisController>()` once; `RefreshDorisReference` (:386) never rebuilds the controller. If Doris spawns later the pips never appear while the footer (reflection path) does. Fix: build in `RefreshDorisReference` too.

---

## Pack §1 versus the code

| Rule | Status |
| --- | --- |
| Planning frozen: plants, energy, genes, fauna, timed tiles | Done, except F6 (status stacks, poop) |
| Day hands off (clicks, WASD) | Done; hotbar keys, zoom, G (Planning only) still respond, harmless |
| Belly = round((Max − hunger)/Max × pips), 8 pips | Done, `DuskRules.BellyPipsFor` (`:98-102`), half-up |
| Ramp 1 + 0.15 (round − 1) | Done, recomputed per tick from the round (`DorisHungerSystem.cs:39-49`); base rate 0.5 not the definition's 2 (documented) |
| Supper 0.3 s per fruit, grid order, within a cap | Done; ≤ 8 s cap is an addition (good) |
| Mercy cap once per Day | Done (`DorisController.cs:458-462`, reset `:445-452`); eats only at belly 0 (addition) |
| Lose: belly 0 / no plant; win at dusk 5 | Done; check order is garden first (pack lists belly first, text only; already in Needs Milan) |
| `playerDeathEnabled` false | Done via `DuskRules.playerStarvationEndsRun` (`RunManager.cs:88`) |
| Food reaches Doris only through supper | **Contradicted** by hand-feeding in Planning (F3) |
| Per-round wave, round 1 ≤ 3 pests | **At risk** by pest carry-over (F4) |
| Restart same seed / new seed | Works for the garden; not for pests (F5) |

---

## Looks right (checked, no finding)

- **Determinism of new code:** no `UnityEngine.Random`, `Time.time` or `Time.realtimeSinceStartup` decides any outcome in `DuskRules`, `RunManager`, `SupperRunner`, `DorisController`'s B3 additions. `Environment.TickCount` is used only to pick a seed (`RunManager.cs:114, 307`). Supper order is a full sort by (y desc, x asc) over unique grid cells (`SupperRunner.cs:144-148`), tie impossible with one plant per tile; frame timing (`Time.deltaTime`) changes only the animation, and the clamp-at-0 feed sum is order-independent. The dusk check reads pips after the hold, with nothing ticking in `Dusk`.
- **Idempotency:** hunger rate, belly pips, ramp and `roundsToWin` are recomputed from `DuskRules` and the round number on every call; `HarvestAllFruits` called twice returns nothing the second time; fruit/GameObject lists are index-aligned (`PlantGrowth.cs` hunk adds `harvestedFruitObjects?.Add` inside the same `if` as the item); `plantsEatenToday` resets on entering the Day, not on a guess.
- **Planning-tick invariant, gating twice:** none found. `GardenerController.TryMove`'s `!ActionsDriveTicks` branch is unreachable from input (already noted); `WorldEffect` gate covers `AuraWorldEffect` (inherits `OnTickUpdate`); `Dusk` and `GameOver` are not Planning, so `TickManager.ActionsDriveTicks` (`TickManager.cs:32-37`) is false and `RequestActionTicks` is a no-op there; the driver stops on any state but `GrowthAndThreat` (`ExecutionPhaseDriver.cs:58`, `:83-88`).
- **Tick-path audit:** all 18 `RegisterTickUpdateable` sites were read. Gated: `PlantGrowth`, `AnimalController` (with its movement/needs/behavior/status parts, none registered separately), `ReactiveBurstHandler`, `TileInteractionManager`, `DorisHungerSystem`, `WorldEffect`/`AuraWorldEffect`, `PlayerHungerSystem`. Ungated by design: `RunManager.PhaseTickHandler`, `GardenerController` (player economy), `WeatherManager` (paused), `FireflyManager/Controller`. Not gated and should be: F6.
- **End-of-Day inside a tick:** `EndCurrentWave` fires from `OnTickAdvanced` after the tick loop (`TickManager.cs:104`), so the state change to `Dusk` is not made mid-iteration; the driver's loop breaks on the state change (`ExecutionPhaseDriver.cs:83-88`).
- **Sticky `GameOver`:** `StartNewPlanningPhase` (`RunManager.cs:200`), `EndRun` (`:263`), `StartGrowthAndThreatPhase` (`:185`, Planning only) cannot leave it; `RequestActionTicks` is a no-op in it.
- **Statics after the Restart fix:** `InventoryService`/`HotbarSelectionService`/`ToolSwitcher` events are unsubscribed in `GameUIManager.OnDestroy`; `GameUIManager.IsPlanningPanelOpen` is reset there; `PlantGrowth.AllActivePlants` removes on destroy; `GeneServices` and its `GeneEventBus` survive the reload, but the bus has no subscribers outside `PlantSequenceExecutor`, and the RNG is reseeded by `InitializeRunSeed`; `RunManager.pendingRunSeed` is consumed once and reset in the editor; `DuskRules.active` is a read-only cache. `SingletonRuntime.IsRestarting` clears on `sceneLoaded`.
- **Subscribe/unsubscribe pairs:** `DorisController` ↔ `RunManager.OnRunStateChanged` (Start/OnDestroy, `HasInstance` + local read for quit), `UIDorisBellyController.Initialize/Dispose` ↔ `OnHungerChanged`/`OnHoverChanged`, `GameUIManager` ↔ `OnRunStateChanged` (`:116`/`:152`), `RunManager` ↔ `OnStarvation`.
- **Null paths:** `DuskRules` missing → code defaults, scene wave list used, one warning; Doris missing → supper skipped with a warning, belly UI not built, overlay and `Supper` null-checked (`UIRunEndOverlayController.cs:109`); wave-less round → the Day still ends (`WaveManager.cs:149-153, 178`). Exception: F7 (no `WaveManager`).
- **Input gates (B2):** `PlayerTileInteractor`, `GardenerController`, `TileInteractionManager.CheckAndRefillTool` and the B0 minigame guard are consistent; `IsPlanningPanelOpen` keeps the seed editor from leaking clicks.
- **B0 fixes** (double plant, dead plants never destroyed): the root causes in `PlayerActionManager.cs` (new guard before `:61`) and `PlantWorldUI.cs:28-34` are sound.
- **Colour rule:** pips are filled/hollow (shape + brightness), the footer drops red/amber (`GameUIManager` `UpdateDorisHungerDisplay`), the end card differs by title text, not hue alone.

## Not checked

Runtime behaviour, Unity serialization of the new `Dusk` enum value in saved scenes, `FaunaManager`/`AnimalMovement` beyond the `Random` grep, the 36-site `Random` and `Input` inventory beyond the sites named, UXML/USS.

## Next action

Fix F1 (and F2's one-line guard) before Checkpoint A so the first full run cannot freeze at dusk; decide F3 and F4 with Milan before B4 starts; do F13 with B6.
