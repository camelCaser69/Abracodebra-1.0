# projectmemory — Abracodabra

**How this file works (since 2026-10-05):** it is read at the start of every session, so it stays short (target ≤ 30 KB).

- **Snapshot:** where the project stands. Rewritten in place when a block closes.
- **Recent entries:** the current and the previous block, appended as work lands.
- **Decision log:** date · decision · where it lives.
- **Code facts** and **Learnings:** kept here, edited in place.

Older history is in `99_Archive/projectmemory_history.md` (verbatim copy of the file as it stood on 2026-10-05, before this rewrite; grep it only when you need a past detail). The 2026-07 chat digest is in `99_Archive/2026-07_Cowork_Era/Chat_History_Digest.md`.

**Trim rule** (CLAUDE.md §4): when a block closes, rewrite the Snapshot, move the previous block's entries and decision rows older than ~3 days of work to `99_Archive/projectmemory_history.md` (append, never delete). "Implemented" is written only after checking the code on disk.

## Snapshot (2026-10-05, modernization pack closed)

- **Project:** Abracodabra — a cozy-dark, tick-based (WeGo) roguelite that mixes farming, tower defense and plant genetics. Solo, Milan (Prague). Unity **6000.3.24f1** (since 2026-10-05; same editor as Bitbloom), URP 17.3, UI Toolkit. Repo `camelCaser69/Abracodebra-1.0`, branch `main`. **It stays a live game, developed alongside Bitbloom** (Decision log 2026-10-05).
- **Loop:** Planning (time frozen, player actions advance ticks: edit gene strands on seeds, plant) → Growth & Threat (the clock runs by itself, plants execute their genes, pest waves attack, Doris must be fed) → next round. The plant is the health bar (leaf loss, not HP).
- **Built (disk-verified 2026-10-05):** 200 C# scripts under `Assets/Scripts` (236 under `Assets/`); tick core (`TickManager`, `ExecutionPhaseDriver` with Space = pause and Tab = speed, `RunManager`/`RunState`, per-run `RunSeed` into `IDeterministicRandom`); the gene system (`GeneBase` → Active/Modifier/Payload, `PlantSequenceExecutor`, slot-based sequences); plants (growth, energy, death pipeline with `OnPlantDied`); fauna waves, `AnimalController`, status effects, Doris (`DorisController`, `DorisHungerSystem`), feeding; a UI Toolkit stack (`GameUIManager` + seed editor, inventory grid, hotbar, drag-drop, spec sheet); `InventoryService` / `HotbarSelectionService`; a dual-grid tilemap (embedded package `Packages/com.skner.dualgrid`). Only `Assets/Scenes/SampleScene.unity` is in the build.
- **Not built (design only):** the DNA-strand **buffer** model (`RuntimeSequenceSlot` is still live in 4 files; no `SequenceParser`, no `PlayerInventory`), run-loop screens (round summary, Game Over, Victory), `DorisMoodSystem`, `ComboDiscoverySystem`, `GeneDraftSystem`, `DorisDigestionSystem`, fixtures (Doris Bowl, Harvest Basket), ripeness windows, Mark & Go, a lose condition (only player starvation behind `RunManager.playerDeathEnabled` leads to `GameOver`), save/resume, audio. **The Planning-tick invariant is not enforced** (nothing in `PlantGrowth` / `PlantSequenceExecutor` checks `RunState`). Perfect ≡ Good is still the live minigame reward path.
- **Design of record** (anything not named here is history or a concept):
  - `02_Design/Gene_Systems_Deep_Dive.md` (v6, 2025): the gene system and the strand model;
  - `03_Tasks/Active/2026-07_Fable5_Design_Decision_Ledger.md`: decisions D1–D10 (summarised in the Decision log). **Revised by** `02_Design/Concepts/Phase_Identity_Final_Refinement_2026-07-20.md`: hand-piloted avatar control is out; build A (Commit & Watch, with Mark & Go) first and B-rev (Tick Ledger) only if A fails the density test. That replaces the Ledger's "same-build A/B toggle";
  - `02_Design/Concepts/Commit_And_Watch_Loop_Design_2026-07-06.md` (Rev 2): the detailed spec for A's systems (fixtures, timing asymmetry, four beats, budget);
  - taste: `02_Design/Feedback_Log.md`.
  - Concepts (not decided): gameplay engagement, UI systems, minigames, gene catalog + annex (all in `02_Design/Concepts/`, dated).
- **Tasks:** `03_Tasks/Roadmap.md` (the candidates, in order); `03_Tasks/Active/`: the Ledger, `2026-07_Pack_Implementation_Guides.md` (F1–F4 guides, all unapplied), `2026-07_Testing_Sandbox.md` (undecided).
- **Tooling state:** Claude Code in the terminal is the code lane; the Unity CLI hookup is done (2026-10-05, `com.unity.pipeline` 0.8.0-exp.1; 6.0 baseline in `04_Reviews/2026-10_Baseline_6.0/`). The extractor, `06_Index` and the Cline rules are retired (archived/deleted). No asmdefs of our own and no tests.
- **Lanes:** design in Cowork or chat (markdown into `ClaudeProjectFiles/`), code in Claude Code. Usage policy: CLAUDE.md §8.
- **Other projects:** Bitbloom (sibling, `D:\Unity Projects\AbraCodebra\Bitbloom\Bitbloom 0.1a\Bitbloom`, same workflow; never edit it from here); Fistful of Mercs (a tabletop prototype in a separate folder with its own KB; state as of 2026-07-11 in the archived digest).
- **Open for Milan:** (1) merge `upgrade/unity-6.3` into `main` in GitHub Desktop and push (main and the tags were not on GitHub when last checked; the CLI push failed with an authentication error); (2) the 6.3 smoke test left **plant growth and Doris feeding unconfirmed** (Roadmap #9: replay it with planting done in Planning); (3) decide the Testing Sandbox (keep or drop); (4) the sibling backup folder `Abracodebra 1.0 - Backup 08.07.2026`: zip to cold storage, delete; (5) turn off the old Cowork "KB doctor" task and stop using the old claude.ai Project; (6) fix Bitbloom's CLAUDE.md §8 ("Pro plan") in a Bitbloom session.
- **Next action:** the merge and push above, then Roadmap #9 (verify plant growth and Doris feeding in play, 5 min). After that, Block 1 from `03_Tasks/Roadmap.md` (asmdef split + golden-run harness, then the A proof of concept).

## Recent entries

**2026-10-05 — modernization pack, Phases D–E: Unity CLI and the 6.3 upgrade** (results: `04_Reviews/2026-10_Baseline_6.0/`, `04_Reviews/2026-10_Unity63_Upgrade.md`)
- Phase D: `com.unity.pipeline` 0.8.0-exp.1 installed; 6.0 baseline recorded (0 errors, 1 known warning, camera + screen captures); CLI gotchas in CLAUDE.md §7.
- Phase E: tag `pre-unity-6.3` on main, branch `upgrade/unity-6.3`, project on 6000.3.24f1. The reimport gave 5 errors and 13 warnings, all fixed in one commit per group: `[RuntimeInitializeOnLoad]` in the generic `SingletonMonoBehaviour<T>` (flag moved to `SingletonQuitState`), the two custom 2D sprite shaders vs URP 17.3 (duplicate `LightingUtility.hlsl` include and `SHAPE_LIGHT(n)`, unguarded debug macro), 12 unsupported USS declarations. Unity itself bumped the packages to Bitbloom's versions (URP 17.3.0, Input System 1.20.0, Test Framework 1.6.0). After: 0 errors, same 1 warning; world capture within 4/255 of the baseline.
- Smoke test: Planning UI, gene editing, Space/Tab, waves/pests, water and tiles fine. **Plant growth and Doris feeding were not confirmed** (Roadmap #9); no console error, not shown to be a regression. Side finding: the plant action fires twice. The 6.3 "Input Manager is deprecated" warning stays until the 36 legacy `Input.*` sites move to the Input System.

**2026-10-05 — modernization pack, Phases A–C** (`03_Tasks/Done/2026-10_Modernization.md`, results in its §7)
- Phase A: baseline commit `3a0abe0`, tag `pre-modernization`; `charming-elgamal` was already merged into main and its local label deleted.
- Phase B: `.gitignore` / `.gitattributes` aligned with Bitbloom; IDE config, `UIElementsSchema/` and the root code extracts untracked/deleted; extractor and Cline rules archived; 492 unused TMP font files (1.14 GB) moved to `D:\Unity Projects\AbraCodebra\_Vault\Abracodebra_TMP_fonts\` (`Assets/TextMesh Pro` 1.2 GB → 59 MB); `visualscripting`, `multiplayer.center`, `collab-proxy` removed from `manifest.json`.
- Phase C: KB consolidated (fixed names for living docs, dated names for point-in-time docs, WeGo reworks archived, `Roadmap.md`, this file rewritten, `Feedback_Log.md`, new CLAUDE.md, `.claude/settings.json`).
- Findings: `SEGUIEMJ.TTF` (Segoe UI Emoji) is used by `StatusEffect_Icon.prefab` and is not redistributable → Roadmap #7. `Assets/Editor/TileMappingsBackup/` is referenced by `TileInteractionManager.cs`: keep.

**2026-07-20 — final Fable 5 session** (design only, nothing implemented)
- The Ledger (D1–D10), the Phase Identity refinement (Mark & Go; B rebuilt as B-rev), the project audit (`04_Reviews/2026-07_Project_Audit_Gaps_And_Opportunities.md`: H1 unloseable, H2 no victory arc, H3 no save, H4 first 10 minutes; C3 golden-run harness before F1), and the gene catalog expansion (28 proposed genes; the demo-eight: Alluring + Camouflage, Catalyst, Trigger Dawn/Dusk, Sap Well, Fertilizer, Lifesteal, Leap, Last Stand) with its experimental annex.

**2026-07-05 to 07-08 — A-category pack applied, research** 
- A1–A6 applied on 2026-07-05 (auto-tick driver, plant death pipeline, timer-honest round end, idempotent `BaseEnergyPerLeaf`, init without `Invoke` delays, run seed). Editor wiring 2026-07-06; Milan confirmed auto-tick, Space, Tab and the run seed in play. The rest of Part 4 was never run (Roadmap #6).
- Concepts: gameplay engagement research (07-05; nine shortcomings), Commit & Watch loop design (07-06, Rev 2), UI systems research (07-08; input layer is the most fragile surface), minigames re-evaluation (07-08; supersedes the §7 ranking of the engagement research).

## Decision log

| Date | Decision | Where |
| --- | --- | --- |
| 2026-10-05 | Abracodabra stays a live project, developed alongside Bitbloom; modernized to the Bitbloom workflow (this pack). | `03_Tasks/Done/2026-10_Modernization.md` |
| 2026-10-05 | **Unity upgraded to 6000.3.24f1** (merged from `upgrade/unity-6.3`; tag `pre-unity-6.3` = the 6.0 state). Accepted by Milan with plant growth and Doris feeding still to verify (Roadmap #9). `com.unity.pipeline` stays at 0.8.0-exp.1 (Bitbloom 0.7.0). | `04_Reviews/2026-10_Unity63_Upgrade.md` |
| 2026-10-05 | Tooling: Claude Code in the terminal + Unity CLI (`com.unity.pipeline`) replace the extractor, `06_Index` and any MCP bridge. Upgrade to Unity 6000.3.24f1 after the CLI hookup (Phase E). | Pack §5, §6 |
| 2026-10-05 | KB layout = Bitbloom's; git is the version history (no `_vN` names; dated point-in-time docs; superseded → `99_Archive/<YYYY-MM_Era>/`; milestone tags). Pack answers Q1–Q17 all accepted ("recs"). | CLAUDE.md §4; pack §7.4 |
| 2026-10-05 | `SEGUIEMJ.TTF` stays for now; swap to Noto (OFL) before any public build. `TileMappingsBackup/` stays. 492 unused font files left the project. | Roadmap #7; pack §7.2 |
| 2026-07-20 | **Hand-piloted avatar control during a running clock is out of the design space** (manual movement felt finicky and hectic). Player actions are programmable or automatic. | Phase Identity §0; `Feedback_Log.md` |
| 2026-07-20 | Mark & Go adopted for A: click a tile → the gardener auto-paths and performs the contextual verb; queue cap 3. Old B is dead; B-rev "Tick Ledger" is built only if A fails the density test (sequential, **revises D1's toggle**). | Phase Identity §2.1, §3, §4 |
| 2026-07-20 | D1: §4.3 phase identity → A (Commit & Watch), exit: ≥ 1 meaningful decision per 15–20 s of Day, else B-rev; ≥ 2 runs per mode, same seed. | Ledger D1 (+ the revision above) |
| 2026-07-20 | D2: automation = fixtures (Doris Bowl, Harvest Basket; coverage radii, no pathing). The walking gardener is a cosmetic skin / Mark & Go executor, never a second behaviour system. | Ledger D2 |
| 2026-07-20 | D3: Doris Bowl is placeable and ships together with Cravings. | Ledger D3 |
| 2026-07-20 | D4: player hunger cut for the demo; garden-as-fail-state instead. | Ledger D4 |
| 2026-07-20 | D5: budget cost model defaults to B (verbs-only) if the experiment is inconclusive. | Ledger D5 |
| 2026-07-20 | D6: auto-pause ON for first occurrence of an event type, plant death, wave spawn; OFF for lean-in prompts; all toggleable. | Ledger D6 |
| 2026-07-20 | D7: 3 rarity tiers (common / uncommon / rare), corner-gem marker. | Ledger D7 |
| 2026-07-20 | D8: the demo's one new system = N2 harvest ripeness windows if A wins, Pruning Snip if B wins. | Ledger D8 |
| 2026-07-20 | D9: "Doris Provides" is the primary gene economy; Gene Extraction is built first; draft logic is the pity floor. | Ledger D9 |
| 2026-07-20 | D10: pack routing by model strength: F1 → strongest model with guide G1 (sandbox vectors first), F2/F3 → mid, F4 → weak is fine; one pack per fresh session. | Ledger D10 |
| 2026-07-08 | The minigames re-evaluation supersedes §7 of the gameplay engagement research (weighted re-scoring; cut: editor Splice minigame, scent-trail scouting). | `Minigames_And_Mechanics_Reevaluation_2026-07-08.md` |
| 2026-07-06 | Planning ticks advance only the player's action economy (hard invariant; **not yet enforced in code**). Automation re-based from a walking gardener AI to fixtures. | `Commit_And_Watch_Loop_Design_2026-07-06.md` Rev 2 |
| 2026-07-05 | A1–A6 applied: auto-tick in Growth & Threat, `RequestActionTicks` as the single action entry, per-run seed, gameplay RNG via `IDeterministicRandom`. | `03_Tasks/Done/Abracodabra_A_Category_Implementation.md` |
| 2025 | Gene editor: Telescope Strand (complexity scales with run progression) + Phrase Chips; Visual Genome (gene composition drives renderer parameters). | `Gene_Systems_Deep_Dive.md` |

## Code facts

All checked on disk 2026-10-05. Architecture map: `01_Core/Codebase_Map.md` (July 2026 sweep; re-verify before relying on it).

- **Layout:** `Assets/Scripts/` has A_ToolkitUI, Core, Ecosystem, Editor, Genes, Helpers, Items, Minigames, PlantSystem, ProceduralGeneration, Ticks, UI, Visual, WorldInteraction. UI Toolkit assets: 5 `.uss`, 3 `.uxml`. One assembly (Assembly-CSharp); no asmdefs of our own, no tests (the Test Framework package is installed).
- **Tick flow:** `TickManager.ActionsDriveTicks` is true in Planning (and before the game exists). `RequestActionTicks(int)` advances ticks only then. In Growth & Threat `ExecutionPhaseDriver` auto-ticks (Space pause, Tab speed, `SetPaused`). Plants tick through `PlantGrowth.OnTickUpdate` → `EnergySystem` and `PlantSequenceExecutor`.
- **Run:** `RunManager` holds `RunState` (Planning / Growth & Threat / `GameOver`), `RunSeed`, `randomizeSeedOnStart`, `playerDeathEnabled`, `IsWaveTimerComplete()` (via `WaveManager`). `GeneServices` registers `IDeterministicRandom` (default seed 0, overridden per run by `RunManager.InitializeRunSeed()`).
- **Genes:** `GeneBase` → `ActiveGene` / `ModifierGene` / `PayloadGene`; `SeedTemplate` is the default config, derived stats are recomputed from it each call; `PlantGeneRuntimeState` holds the player's edited sequence (`RuntimeSequenceSlot` list). `GeneEventBus` carries gene events.
- **Init:** `InitializationManager.IsReady/OnReady`; `InventoryService.OnInventoryReady`. Loadout is single-sourced (`StartingLoadoutApplier` sits in no scene or prefab; the starting inventory comes through `GameUIManager`).
- **Scenes:** the build has only `SampleScene` (`MainScene` exists but is not in the build). `ExecutionPhaseDriver` is wired into `SampleScene`.
- **Known gaps (verified):** 36 `Random.*` call sites remain (the A6 follow-up: fauna and cosmetic systems; check which are gameplay before touching); 36 legacy `Input.*` sites (the Input System 1.13 package is installed, `activeInputHandler` = both); `PlantGrowth.cs` is 884 lines and `GameUIManager.cs` 875 (patch these surgically).
- **Assets:** `Assets/TextMesh Pro` is 59 MB after the font move; fonts in use: Handjet Regular and Medium (game text), Liberation Sans, Noto Color Emoji (TMP emoji fallback), SEGUIEMJ (status-effect icons; not redistributable).
- **Git:** `.git` ≈ 145 MB (the moved fonts remain in history). Tags: `pre-modernization`, `kb-2026-10`, `pre-unity-6.3`.

## Learnings

- **Edit frequency governs gene-editor design:** players edit several seeds every Planning phase, so edit speed, not model depth, is the constraint; complexity must scale with run progression.
- **The plant is the health bar.** **Visual Genome:** gene composition deterministically drives renderer parameters.
- **Idempotent derived stats; deterministic gameplay RNG** (`IDeterministicRandom`, never raw `UnityEngine.Random` in gameplay).
- **Demo-first:** features that need new infrastructure are staged; deferred items go to `Roadmap.md`, not into prose.
- **Verify on disk after an interrupted session** before redoing anything (an interrupted session once looked like lost work when all 19 files had landed).
- **Pitch and explainer voice (2026-07):** explain, don't sell; first person, Milan's voice.
- **Milan's working preferences** (markdown never .docx; green/gray over orange/brown; UI that teaches itself; iterative deepening with adversarial passes; options weighted and scored; the design law "maximum combinations from minimum complications"; exact menu paths for manual Unity steps): CLAUDE.md §3, §5 and `02_Design/Feedback_Log.md`.
- **Code lane limits:** Claude cannot compile for Milan; flag anything unverified and ask for the console errors (the Unity CLI will replace this after Phase D).
