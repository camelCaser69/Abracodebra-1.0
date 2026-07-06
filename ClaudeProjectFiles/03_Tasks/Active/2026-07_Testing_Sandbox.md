# Testing Sandbox — Options & Proposal (2026-07)

**Status: PROPOSAL — awaiting review. No code changed.**
Priorities per Milan: plant growth first, genes & seed editing second.
All APIs cited below were verified against live disk on 2026-07-05.

---

## 1. Verdict

**Yes, worth building — but only a thin layer (~3 scripts), and NOT as separate isolated scenes.**

Two facts drive the whole design:

1. **Your iteration loop today is genuinely slow.** Testing one gene tweak = boot MainScene → walk/click through Planning → drag genes → end planning → wait real-time growth → observe → restart. Minutes per iteration, mostly ceremony.
2. **A "minimal test scene" would be a trap in this codebase.** `SingletonMonoBehaviour<T>` does **not** lazy-create (it errors if the manager isn't in the scene), and there are ~20 managers (7 singleton-base + ~14 plain `static Instance`) plus tilemaps, DualGrid, camera, player. A stripped scene means maintaining a second manager hierarchy that silently drifts from MainScene — you'd end up debugging the test rig instead of the game. Exactly the failure mode you want to avoid.

**Conclusion:** don't isolate systems into separate scenes. Instead, make the *real* scene boot directly into any configured test situation, plus cheat hotkeys to jump around inside it. Isolation comes from config flags (no waves, no hunger, frozen day), not from parallel scenes.

Also: ~60% of a sandbox already exists as scattered affordances (§2). The missing piece is small.

---

## 2. What already exists (don't rebuild)

| Affordance | Where | Notes |
|---|---|---|
| Manual tick step (T key) | `TickManager.DebugAdvanceTick()` | Single-step; requires TickManager `debugMode` flag ON |
| Pause / speed cycle (Space / Tab) | `ExecutionPhaseDriver` (`SetPaused`, `CycleSpeed`, `SetSpeedIndex`) | GameObject not yet wired into scene — already on your A-pack checklist |
| Debug overlay (F3) | `TickDebugMonitor` (tick/animal/plant counts, UGUI) | Needs scene wiring |
| Radius/grid visualization | `GridDebugVisualizer` | |
| Phase forcing | `RunManager.EndPlanningPhase()`, `StartNewPlanningPhase()`, `ForcePhase(GamePhase)` (editor/debug-gated) | |
| Day/night control | `WeatherManager.PauseCycleAtDay()`, `ResumeCycle()`, `ForcePhase(CyclePhase)` | |
| No-death toggle | `RunManager.playerDeathEnabled` | Inspector checkbox |
| Reproducible runs | `RunManager.randomizeSeedOnStart` OFF + fixed `runSeed` (A6) | Same seed ⇒ same growth/drops |
| Inventory seeding | `StartingLoadoutConfig` + `StartingLoadoutApplier` (binds to `InventoryService.OnInventoryReady`) | |
| Boot-complete signal | `InitializationManager.IsReady` / `OnReady` (A5) | The hook a sandbox driver waits on |

**Free win, zero code:** widen `ExecutionPhaseDriver.speedMultipliers` from `{1, 2}` to `{1, 2, 4, 8}` in the Inspector when you wire it.

---

## 3. Options considered

### Option A — Sandbox boot scene (additive wrapper) ✅ recommended
A tiny `Sandbox.unity` containing **one GameObject** (`SandboxController` + reference to a `SandboxConfig` ScriptableObject). On Play it additively loads MainScene, waits for `InitializationManager.OnReady`, then applies the config: force run state, grant gene/seed loadout, auto-plant templates, disable waves/hunger, freeze day, fix the RNG seed.

- **Cost:** 2 scripts + 1 SO + 1 near-empty scene (~250–350 lines total).
- **Value:** every Play press boots straight into the situation you want. Multiple `SandboxConfig` assets = multiple "test setups" with zero extra scenes.
- **Why elegant:** zero drift (reuses the real scene and real boot path), zero test objects inside MainScene, delete the folder and the game is untouched.
- **Risks:** planting origin must be a plantable tile (invalid-tile blacklist in `PlantPlacementManager`); double-loadout if `StartingLoadoutApplier` also fires (you already have a "pick ONE loadout source" checklist item); depends on ExecutionPhaseDriver being wired.

### Option B — Cheat hotkeys ✅ recommended (same package)
One `SandboxHotkeys` component (editor/debug-build gated, same guard style as `RunManager.ForcePhase`). All actions use existing public APIs — no game-code edits:

| Key | Action | API |
|---|---|---|
| F5 | End planning → Growth & Threat | `RunManager.EndPlanningPhase()` |
| F6 | Back to Planning (keeps plants) | `RunManager.StartNewPlanningPhase()` |
| F7 / F8 | +10 / +50 ticks instantly | loop `TickManager.AdvanceTick()` ¹ |
| F9 | Refill energy on all plants | `PlantGrowth.AllActivePlants` → `PlantEnergySystem.AddEnergy()` |
| F10 | Spawn a chosen wave now | `FaunaManager.ExecuteSpawnWave(WaveDefinition)` |
| F11 | Clear all plants | `Object.Destroy` on `AllActivePlants` members (same external kill path Doris uses; `PlantGrowth.Die()` and `CleanupDestroyedPlants()` are private — tiles are freed by PlantPlacementManager's internal cleanup on next placement) |
| F12 | Console report per plant | state, energy, leaves, executor cursor from `PlantGrowth` |

¹ Bulk-advance must call `AdvanceTick()` directly — post-A1, `RequestActionTicks` is a no-op during the auto-driven phase. Known caveat: wall-clock coroutines (§12 of codebase map, e.g. `DelayedGrowthStart`) won't compress; acceptable for testing.

Plus a config-toggled **gene event console tap** (~10 lines): `GeneServices.Get<IGeneEventBus>().Subscribe<GeneExecutedEvent>(…)` (+ `GeneValidationFailedEvent`, `SequenceCompletedEvent`) → `Debug.Log`. Today almost nothing subscribes to these — this is the cheapest possible "what did my sequence actually do" visibility.

### Option C — Plant inspector overlay ⏸ defer
Click-a-plant panel showing energy/leaves/cursor/recharge live. Real value, but the F12 console report covers ~70% of it for ~5% of the cost. Build only if the console version proves insufficient — and then in UI Toolkit, not UGUI (avoid deepening the two-HUD-stacks issue, review B3).

### Option D — Automated tests (Unity Test Framework) ⏸ defer, honestly
Blockers: `Assets/` has **zero asmdefs**, and UTF test assemblies cannot reference the predefined `Assembly-CSharp` — automated tests first require asmdef-izing `Assets/Scripts` (plus anything it references, e.g. DualGrid if it lives in Assets). Doable (~half a day of compile whack-a-mole) but pure infrastructure. The one automation genuinely worth it *later*: a **determinism smoke test** — two seeded boots, N ticks, compare a state hash — because seed-based reproducibility is the invariant your whole WeGo design leans on. Park it in the roadmap; don't block the demo on it.

### Option E — Editor-time simulation ("run a seed off-screen, print results") ❌ skip
`PlantSequenceExecutor` is coupled to a live `PlantGrowth` in a running scene. Headless simulation needs the PlantGrowth split (review B4) first. Revisit after that refactor, if ever — the sandbox makes in-scene observation fast enough.

---

## 4. Proposed solution (Tier 1)

**New files — everything lives in two folders, fully deletable:**

```
Assets/Scripts/Testing/SandboxController.cs   (~120 lines)
Assets/Scripts/Testing/SandboxHotkeys.cs      (~150 lines)
Assets/Scripts/Testing/SandboxConfig.cs       (~60 lines, ScriptableObject)
Assets/Scenes/Sandbox.unity                   (1 GameObject)
Assets/Scriptable Objects/Testing/*.asset     (config presets)
```

**`SandboxConfig` fields:**

- `startState` — Planning | GrowthAndThreat (apply via `EndPlanningPhase()`)
- `grantAllGenes` (enumerate `GeneLibrary.GetAllGenes()` via a direct serialized reference to the library asset; capped by inventory size) or explicit gene/seed lists — items built exactly like `StartingLoadoutApplier` does: `new UIInventoryItem(gene)` → `InventoryService.AddItem(item)`
- `autoPlantTemplates` — list of `SeedTemplate` + grid offsets from a configured origin; planted via `template.CreateRuntimeState()` → `PlantPlacementManager.TryPlantSeedFromInventory(state, gridPos, worldPos)` (real path: validation, occupancy, snapping)
- `disableWaves` / `disableFauna` — disable those manager GameObjects (`RunManager` already null-guards `WaveManager.Instance?.`)
- `disableHunger` — set `playerDeathEnabled = false` (+ disable `PlayerHungerSystem` if drain annoys)
- `lockDay` — `WeatherManager.PauseCycleAtDay()`
- `fixedSeed` — int, -1 = random; sets `runSeed` + `randomizeSeedOnStart` behavior
- `logGeneEvents` — the GeneEventBus console tap

**Sequencing:** `SandboxController` lives in `Sandbox.unity`, additively loads MainScene, applies config only after `InitializationManager.OnReady`. Playing MainScene directly = normal game, guaranteed untouched.

**The two loops this buys you:**

- **Growth lab** (priority 1): config = auto-plant 3 templates, no waves, no hunger, locked day, fixed seed, start in GrowthAndThreat. Press Play → plants are already growing. Space/Tab/T to scrub time, F7/F8 to jump, F12 to dump state. Tweak the `SeedTemplate` asset → Play again. **Iteration: ~10s, fully reproducible** (fixed seed ⇒ identical growth every run ⇒ true A/B comparison of edits).
- **Gene workbench** (priority 2): config = all genes granted, Planning, no waves. Edit a seed in the real editor UI → F5 to execute → watch with `logGeneEvents` on → F6 back → re-edit. No rounds, no rewards, no waves between you and the next edit.

**Done when:**
1. Play from `Sandbox.unity` reaches a configured situation with zero manual clicks.
2. Play from `MainScene.unity` behaves exactly as before (no game-code edits shipped).
3. Growth-lab loop ≤ ~15s per iteration; identical results across two runs with the same seed.
4. All hotkeys no-op in release builds.

**Effort:** ~half a day + a playtest pass. **Prerequisite:** ExecutionPhaseDriver wired into MainScene (already on your checklist).

---

## 5. Explicitly skipped (scope discipline)

- Per-system minimal scenes (drift trap — the core design decision of this doc)
- EditorWindows / custom inspectors / menu tooling
- UTF suite + asmdef migration (→ roadmap: determinism smoke test, post-demo)
- Headless gene simulation (→ after PlantGrowth split, B4)
- Any new UGUI; any change to shipping game code

**Optional appendix — Enter Play Mode Settings (disable domain reload):** would cut Play-enter to ~1s. Static-reset hygiene partially exists (`SingletonMonoBehaviour`, `GeneServices` have `RuntimeInitializeOnLoadMethod` resets) but `InventoryService` / `HotbarSelectionService` / plain `static Instance` fields are unaudited. Try it after the sandbox works; revert on first weirdness.

---

## 6. Open questions for review

1. Merge `SandboxHotkeys` into `SandboxController` (1 file) or keep split so hotkeys can also be dropped into MainScene directly?
2. F-key layout OK? (F3 = existing TickDebugMonitor, T/Space/Tab taken.)
3. Should "grant all genes" replace inventory contents or append?
4. `MinigameManager` planting trigger in sandbox: leave on or auto-disable? (Exact toggle API to confirm at implementation.)

---

## Next action anchor

**Milan reviews → picks Tier-1 scope (A+B as specced, or trimmed) → then implementation ships as one pack: 3 scripts + 1 scene + 2 starter configs ("GrowthLab", "GeneWorkbench"), and this doc moves to `03_Tasks/Done/` with results noted.**
