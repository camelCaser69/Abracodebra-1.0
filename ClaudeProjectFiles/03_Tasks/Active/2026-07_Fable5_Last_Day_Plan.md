# Fable 5 — Last-Day Maximum-Value Plan (2026-07-07)

**Purpose:** One day of Claude Fable 5 access remains. This doc ranks where that capability buys the most for Abracodabra: hard, multi-file, code-only work that runs with near-zero manual intervention (no Inspector wiring, no scene babysitting, no play-testing mid-task). Grounded in: Codebase Map (2026-07-05 sweep), Foundation Review 2026-06, gene_systems_deep_dive_v6 §1+§3, projectmemory active threads.

**Verified before writing:** no `SequenceParser` or `PlayerInventory` class exists on disk; slot model (`RuntimeSequenceSlot`) is live; A1–A6 applied and Editor-wired.

---

## 0. What Fable is uniquely worth spending on

Fable's edge over Sonnet/Opus for this repo: holding 6–15 interacting files coherently while restructuring them, getting cross-file wiring right the first time, and not inventing symbols. That edge matters most on:

1. **Architectural inversions** — many call sites, one conceptual change (B1 inventory).
2. **Model migrations** — slot → buffer gene refactor, where runtime, executor, and UI must change in lockstep.
3. **Greenfield systems that must self-wire** — screens/trackers that hook existing events with zero Inspector work.

It matters least on: tuning, wave authoring, visual feel, anything requiring play-testing between steps. Don't spend Fable there.

**The autonomy filter** (every pack below passes it):

- Pure `.cs` / `.uxml` / `.uss` edits. No new scene objects requiring manual placement — new MonoBehaviours self-install from bootstrap code (established pattern: A5, ExecutionPhaseDriver).
- No dependency on mid-task compile/play feedback. Compile happens once per pack, as a batch triage (see §4).
- Serialized-data safety: existing `SeedTemplate`/gene `.asset` files must keep loading — keep serialized field names or use `[FormerlySerializedAs]`; never rename SO class names.
- Every referenced symbol grep-verified against live disk before use (standing rule).

**Known failure mode & mitigation:** Fable writes blind of the compiler. Expect 2–10 trivial errors per big pack. Mitigation: you open Unity once per pack, paste the Console errors back, Fable fixes in minutes. That 5-minute loop is your only required involvement per pack.

---

## 1. The ranked plan

| # | Pack | Value | Fable effort | Your effort | Risk |
|---|---|---|---|---|---|
| F1 | **DNA Strand buffer migration (full)** — the Noita bus | Core-identity feature; cheapest it will ever be | 2 sessions (backend, then editor UI) | 2× compile triage + 1 play check | Medium-high |
| F2 | **B1: Inventory model out of UI** | Highest-value rework in the Foundation Review; unblocks draft/save/headless | 1 session | 1× compile triage | Medium |
| F3 | **Run-loop screens pack** — RoundStatsTracker, round summary, Game Over screen, HUD consolidation (B3+B2) | Demo cannot ship without it; pure feel-payoff | 1 session | 1× compile triage | Low |
| F4 | **Hygiene sweep** — A6 leftovers, B5 wall-clock, Invoke delays, B6 nits, TargetFinder O(n) | Removes whole bug categories; mechanical | 0.5–1 session | 1× compile triage | Low |
| F5 | Design-gated items (draft UI, Doris Bowl, Standing Orders) | High, but blocked on your decisions | — | design call first | — |

**Recommended order: F1 → F2 → F3 → F4.** F1 first while budget is largest (riskiest, most valuable). F2 before F3 because the summary/draft screens want the clean inventory model underneath. F4 is a cooldown pack — safe filler for whatever time remains, or interleave while you compile-check a bigger pack.

**Dependency note:** F1 and F2 overlap at `UIInventoryItem.SeedRuntimeState` / `PlantGeneRuntimeState`. Run them in separate sessions with an extractor re-run between, and tell the F2 session that F1 landed.

---

## 2. Task packs

### F1 — DNA Strand Sequencer: slot → buffer migration (flagship)

The design is already complete in `02_Design/gene_systems_deep_dive_v6.md` §3: modifiers ACCUMULATE, an Active CONSUMES the buffer, payloads ATTACH to the last Active; wrap-trick; trigger/continuous Actives arm without blocking; cycle time = strand length. This pack implements it. The Foundation Review deferred this for *pedagogical shipping order*, not architectural cost — building it now, pre-content, with Fable, is precisely when it's cheap. Slot semantics remain expressible inside the buffer model (a slot is just a pre-grouped strand), so nothing designed for the demo is lost.

**Stage 1 — engine-agnostic parser core.** New `SequenceParser` (pure C#, zero UnityEngine references) consuming an ordered `List<RuntimeGeneInstance>` + gene category/flags, producing `ExecutionGroup`s {active, modifiers[], payloads[], tickCost, isTrigger/isContinuous} + per-gene diagnostics (wasted payload, dangling modifier → wrap target). Being engine-free means Fable can compile and unit-test it **in the sandbox with dotnet before it ever touches Unity** — the one place true pre-verification is possible. Port the §3 Build A/B/C examples + wrap trick + `[Trap][Poison][Fruit][Nutritious]` 2-tick example directly into tests.
*Open semantic to pin during implementation (from §3 examples): non-trigger genes cost 1 tick each — modifiers and payloads included; trigger/continuous Actives and their attached payloads cost 0. Verify against §3 before coding; flag if examples disagree.*

**Stage 2 — runtime + executor swap.** `PlantGeneRuntimeState`: `RuntimeSequenceSlot[]` → flat ordered `List<RuntimeGeneInstance>` (keep `RuntimeSequenceSlot.cs` as `[Obsolete]` shim only if anything external still binds it; otherwise retire). `SeedTemplate`: `activeSequenceLength` → strand length semantics (keep the serialized field name). `PlantSequenceExecutor`: parse once at strand load / on edit-commit, then execute `ExecutionGroup`s — per the deep dive, "execution logic barely changes, just input format": energy check, Pre/PostExecution, delay ticks, GeneEventBus events all stay. Touchpoints to sweep: `SeedTemplate.CreateRuntimeState()`, `NodeExecutor.SpawnPlantFromState`, `SeedQualityCalculator`/`SeedTooltipData`, `GeneRewardSystem`, `StartingLoadoutConfig/Applier`, anything constructing or indexing slots (grep `RuntimeSequenceSlot` for the authoritative list).

**Stage 3 — seed editor UI.** `UISeedEditorController` (742L): passive row stays; Active/Mod/Payload rows collapse into one horizontal gene strip. `UIDragDropController` already centralizes drag-drop (good — extend, don't fork). Editor must make parsing visible (§3 "The Editor Experience"): colored bracket grouping per ExecutionGroup, payload-attachment arrows (or simpler: group-tint gene borders v1), warning icons on wasted genes, and Spec Sheet per-cycle breakdown ("Cycle: Cloud 8E w/ Efficiency → Poison+Slow") sourced from parser diagnostics — the parser is the single source of truth for both execution and preview. UXML/USS: `GeneSlot.uxml` reuse, new strip container styles.

**Deliberately out of scope:** new genes, Telescope Strand progression, Phrase Chips, Visual Genome — all layer on later; the parser just needs clean seams (diagnostics API, group model).

**Risks, honestly:** Stage 3 is the largest UI file in the project being restructured blind of visual output — expect one iteration with you looking at it. Serialized seed content in `StartingLoadoutConfig`/inventory assets that stored per-slot groupings needs a migration path (one-time flatten in `CreateRuntimeState` covers templates; grep for any persisted `RuntimeSequenceSlot` serialization). Executor timing change (slots ticked 1:1 → genes ticked per parser cost) shifts balance — expected, this IS the feature.

**Done when:** parser tests green in sandbox against all §3 canon examples; plant executes `[Efficiency][Cloud][Poison]` per Build-B semantics in play; rearranging the same genes in the editor visibly re-groups brackets and changes the Spec Sheet cycle preview; existing SeedTemplate assets load without Inspector surgery; no `RuntimeSequenceSlot` references remain outside a shim.

**Session prompt:** *"Read `03_Tasks/Active/2026-07_Fable5_Last_Day_Plan.md` pack F1 and `02_Design/gene_systems_deep_dive_v6.md` §1+§3. Implement Stage 1+2 (parser core with sandbox dotnet tests, runtime+executor swap). Stage 3 only when 1+2 are grep-verified. Full files where >3 methods change; report the compile-triage list at the end."*

---

### F2 — B1: invert inventory ownership (UI → model)

Straight from the Foundation Review (its "highest-value rework"): plain-C# `PlayerInventory` model (`InventoryEntry { ItemDefinition; PlantGeneRuntimeState seedState; GeneBase gene; ResourceInstance; }`), owned by a self-installing `PlayerInventorySystem` created from the bootstrap/`InitializationManager` sequence (no scene wiring). `InventoryService` keeps its static API but fronts the model — call-site churn stays small by design. `UIInventoryItem` becomes a view wrapper built on demand by `UIInventoryGridController`. `GeneRewardSystem`, loadout, feeding stop touching UI types. This is the prerequisite for save/load, the pick-1-of-3 draft, and headless economy testing — and it's ~6 systems touching inventory today versus ~16 later.

**Watch:** hotbar row logic and the locked seed slot live in service/UI seams — map them before moving; `OnInventoryReady` (A5) must fire from the new owner at the same point in the init sequence.

**Done when:** `GameUIManager` no longer constructs or owns inventory data; reward/loadout grant items with zero `Abracodabra.UI` references; UI rebuilds entirely from model events; compile triage clean.

**Session prompt:** *"Read plan pack F2 + Foundation Review B1. Extract the inventory model per the review's sketch. PlayerInventorySystem self-installs in code — no Inspector step. Note F1's buffer migration landed: seed entries hold flat-strand PlantGeneRuntimeState."*

---

### F3 — Run-loop screens pack (design-neutral musts)

Everything here is uncontroversial, code-only, and self-wiring off existing events:

1. **`RoundStatsTracker`** (plain C#): subscribes `OnPlantDied`, `OnLeafConsumed`, `GeneExecutedEvent`, wave/round events, Doris state, harvest counts. Feeds 2 and 3.
2. **End-of-round summary panel** (UI Toolkit, into `GameUI_Document` flow): "Leaves lost 7 · Pests outlasted 12 · Doris: Satisfied" on `RunManager.StartNewPlanningPhase`. The Review calls this the loop's missing emotional close.
3. **Game Over / Demo Complete screen**: `RunState.GameOver` exists with no UI anywhere. Stats + `RunSeed` display (A6 gives it to you free) + Restart via existing `RestartGame()`.
4. **B3+B2 finisher**: move wave/day/round display fully into the UI Toolkit HUD; add `WaveManager.WaveProgress01 { get; }` and delete the reflection reads in `GameUIManager` + the TMP fields from `WaveManager` (also kills its per-frame string churn). Leftover TMP scene objects = a 2-minute deletion for you later, nothing blocks.

**Done when:** a session reads Plan → Watch → Summary → Plan and starvation ends in a real screen with seed + stats; zero reflection sites remain in `GameUIManager` for waves; HUD shows wave progress from the public property.

**Session prompt:** *"Read plan pack F3 + Foundation Review C1/C3-summary-half/B2/B3. Build all four items; wire through GameUIManager + existing events only; no new scene objects."*

---

### F4 — Hygiene sweep (mechanical, safe, interruptible)

Ordered by payoff; each item independent, do until time runs out:

1. **A6 follow-up:** route remaining gameplay `UnityEngine.Random` sites through `IDeterministicRandom` — BasicFruitGene shuffle, CloudWorldEffect regrow, AnimalController (leaf pick, flee), AnimalMovement (wander/flee), AnimalBehavior (poop timing). Leave cosmetic-only (firefly visuals, placement jitter) on UnityEngine.Random, commented as such.
2. **B5 wall-clock → tick:** `PlantGrowth.DelayedGrowthStart` (WaitForSeconds 0.5 → tick counter), `FaunaManager` spawn-burst WaitForSeconds → tick-spaced spawns, `GardenerController` multi-tick move coroutine (documented dormant branch — verify against ExecutionPhaseDriver before touching).
3. **Remaining Invoke-delays:** FeedingSystem 0.2s, AnimalFeedableAdapter/PlayerHungerSystem 0.3s → `InitializationManager.OnReady` (pattern already established by A5).
4. **TargetFinder O(n):** replace `FindObjectsByType` per effect-tick with registries (`AllActivePlants` pattern exists; add equivalent for animals via FaunaManager bookkeeping). Also `TickDebugMonitor` per-frame find.
5. **B6 nits:** `GeneInstanceData` field-vs-dict duplication (dict wins — it's what `PlantGrowthLogic` reads); `RactiveBurstHandler.cs` → `ReactiveBurstHandler.cs` (rename `.cs` + `.meta` pair together, GUID preserved); `PlantGrowth.TakeDamage` caller audit → one damage path; `GameLog.Verbose` gate over per-tick Debug.Log churn (PlantGrowth withering, PlantPlacementManager `verboseLogging`, Doris, feeding).
6. *(Optional, lowest priority)* singleton unification on `SingletonMonoBehaviour<T>` — cheap but touches lifecycles; skip if anything above remains.

**Done when:** grep for `UnityEngine.Random` in gameplay namespaces returns only commented cosmetic sites; no `WaitForSeconds` inside tick-state logic; no `Invoke(` init delays; effect ticks allocate no FindObjectsByType calls.

**Session prompt:** *"Read plan pack F4. Work top-down, one item at a time, grep-verifying each call site. Stop cleanly at session end and list what's done/remaining."*

---

### F5 — Explicitly NOT for the autonomous day (design-gated)

- **Pick-1-of-3 gene draft UI** — wants B1 done *and* the "Doris Provides vs. draft as pity floor" decision finalized.
- **Doris Bowl / Standing Orders v0** — gated on your §4.3 Commit & Watch experiment afternoon (`Commit_And_Watch_Loop_Design.md`).
- **Minigame upgrade pack U1–U8, wave authoring, Doris tuning** — need play-feel iteration, the one thing Fable can't do alone.
- **A-pack Part 4 test checklist** — yours, in-Editor, unchanged.

If you make the draft/Doris design call early in the day, the draft UI slots in after F2 at roughly F3 cost.

---

## 3. Protocol for the day

1. **One pack per session, fresh session each.** Point it at this doc's pack section **and at the matching G-section of `2026-07_Pack_Implementation_Guides.md`** — the companion doc with disk-verified symbols, locked design decisions, step-by-step recipes, and do-not lists, written so any model (or human) can execute without Fable. CLAUDE.md auto-loads the rest.
2. **Between packs:** open Unity → let it compile → paste all Console errors into the same session → Fable fixes → re-run `unity_extractor_RUN.bat`. Budget 5–10 min of your time per pack; it's the only manual step.
3. **Verification rules stand:** host-side Read/Grep after writes (bash mount lags); F1 Stage 1 additionally proves itself with sandbox dotnet tests.
4. **KB per Memory Protocol:** each session updates projectmemory Current state + flags `06_Index` stale; architecture-level changes (F1, F2) patch the Codebase Map sections §5/§8 in-session.
5. **Realistic day shape:** F1 fills most of it. F1+F2 is a strong day. F1+F2+F3 is a maximal day; F4 is the flex buffer. If F1 Stage 3 (editor UI) overruns, ship Stages 1+2 and leave Stage 3 as a written continuation pack — the parser+runtime alone is the hard, valuable part.

---

## Next action

Open a fresh Fable session and paste the F1 session prompt from pack F1 above.
