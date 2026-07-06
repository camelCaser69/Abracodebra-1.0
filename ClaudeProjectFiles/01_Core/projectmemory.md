**Purpose & context**

Milan is a solo indie developer based in Prague building **Abracodabra** in Unity 6 (C#, URP, UI Toolkit) — a cozy-dark roguelite combining WeGo (simultaneous turn-based) farming, tower defense, and plant genetics. The core loop alternates between a Planning phase (player edits gene strands on seeds, time frozen, action-driven) and a Growth & Threat phase (plants execute genes automatically, pest waves attack, Doris the companion must be fed).

Design touchstones: Noita, Backpack Battles, Into the Breach, Inscryption, Opus Magnum. Target audience is "pace-casual but not easy-casual."

Key namespaces: `WegoSystem` (TickManager, RunManager, GridPosition), `Abracodabra.Genes.*` (gene system), `Abracodabra.UI.Toolkit` / `Abracodabra.UI.Genes` (UI and InventoryService).

**Current state**

The live repo stands at 200 C# scripts under Assets/Scripts (199 from the 2026-07-05 full sweep + new `ExecutionPhaseDriver.cs`; architecture map: `ClaudeProjectFiles/01_Core/Abracodebra_Codebase_Map.md`). **A-category pack A1–A6 was APPLIED to the repo on 2026-07-05** — all 9 Part-1 full files and all 6 Part-2 method patches, every referenced symbol verified on disk: ExecutionPhaseDriver auto-drives ticks in Growth & Threat; player actions route through `TickManager.RequestActionTicks` (free during the auto-driven phase); plant death pipeline (`OnPlantDied` event, fade, `Destroy` frees the tile); timer-honest `WaveManager.IsWaveTimerComplete()` (+[Obsolete] shim); idempotent `BaseEnergyPerLeaf` derived from the template; loadout/reward Invoke-delays replaced by `InventoryService.OnInventoryReady` + direct singleton binding (+`InitializationManager.IsReady/OnReady`); per-run seed on RunManager (`RunSeed`, `randomizeSeedOnStart`) seeding `IDeterministicRandom` (GeneServices default now 0, not time-based). Still manual in the Editor (pack Part 3/4): add the ExecutionPhaseDriver GameObject, check RunManager seed fields, pick ONE starting-loadout source (StartingInventory vs StartingLoadoutApplier — both configured = doubled items), run the Part 4 test checklist. Compile not yet verified in Unity. DorisMoodSystem / ComboDiscoverySystem / GeneDraftSystem remain design-only. 2026-07-06 (post-migration session): extractor re-run and `06_Index/` synced (extracts now include A-pack code); truncated `01_Core/project_instructions.md` and `projectmemory.md` tails repaired; codebase map spot-verified against live disk (all A-pack symbols confirmed; SO count corrected to 51 .asset, prefabs 41, DualGrid = local package `Packages/com.skner.dualgrid`).

Active design threads:

- **Gene acquisition**: "Doris Provides" digestion economy recommended as the primary system — Doris produces seed pellets biased toward her diet; existing draft logic demoted to pity floor. Gene Extraction (disassembling seeds into raw genes) flagged as a low-cost shared dependency to build immediately.
- **Gene editor UX**: Final recommendation is Telescope Strand (complexity scales with run progression) + Phrase Chips (Active+Modifier+Payload fused as reusable library objects). Visual Genome (gene composition deterministically drives renderer parameters) replaces manual morphology building. Demo cut-line preserves existing slot model semantics underneath.
- **Pixel art icon system**: 23 player-facing genes across 5 categories. Category encoded by full background color fill; motifs use parchment tone on warm-category backgrounds. Rarity marker approach (corner gem vs. gold frame) pending final tier count decision.
- **Gameplay engagement research** (2026-07-05): `02_Design/Concepts/Gameplay_Engagement_Research.md` — nine diagnosed shortcomings (no run arc, toothless threat, phase identity crisis, energy-starved execution show, narrow buildcraft, silent rewards, Doris-as-timer, thin tactility, invisible feedback) + solutions, minigame audit/program (TimingCircle upgrade pack U1–U8; Cascade Harvest, Pour Arc, Shoo!, Firefly Jar concepts; graduation-valve policy), and a demo-slice prioritization. Key pending decision: §4.3 phase-identity experiment (Commit & Watch vs. Live Garden). Notable code facts verified: minigame Perfect≡Good (default reward path), wave list past round N → no wave at all, `maxPlanningPhaseTicks` removed as unimplemented (stale in .asset), world verbs hard-gated to GrowthAndThreat (`PlayerTileInteractor.cs:33`). Concept only — nothing implemented.

**On the horizon**

- Implement `DorisDigestionSystem` state class and residue stamping on fruit production
- Resolve remaining `UnityEngine.Random` call sites in fauna/cosmetic systems (scoped follow-up from A6)
- B5: wall-clock interaction with `DelayedGrowthStart` (documented, untouched)
- Dormant Planning-phase movement branch (documented, untouched)
- Cross-pollination / bee system — ranked second as post-demo flagship
- Steam page and public marketing push deferred until visual polish is complete ("Stardew Valley from Aliexpress" placeholder visuals need post-processing pass first)
- Realistic forecast: demo-ready build ~late 2026 after content, polish, and Steam prep phases

**Key learnings & principles**

- **Edit frequency governs gene editor design**: Players edit multiple seeds every Planning phase in an already thinking-heavy WeGo game — this shifts the governing constraint to edit speed, not model depth. Complexity must match run progression (Telescope Strand).
- **The plant is the health bar**: Leaf vitality replaced abstract HP. Structural loss (leaf count) is more legible and opens more design space than numerical HP.
- **Surgical vs. full-file output**: Large files like `PlantGrowth.cs` (~825 lines) get method-level patches with exact anchors to avoid reconstruction errors from compressed extract format; smaller/self-contained scripts get full replacements.
- **Scope discipline**: Explicit skip lists prevent scope creep. Post-demo deferred items are documented in roadmap files, not left implicit.
- **Demo-first architecture**: Every design evaluation applies a demo-shippability criterion. Features that require new infrastructure get staged — visual/UX changes ship first, parser migrations deferred.
- **Idempotency and determinism**: Stats like `PhotosynthesisEfficiencyPerLeaf` must be derived from seed template each call, not accumulated; gameplay RNG routes through `IDeterministicRandom` with a per-run seed on RunManager.
- **Visual Genome principle**: Gene composition should deterministically drive renderer parameters (payload tints fruit color, modifier count drives leaf density) — delivers "it IS what I made" satisfaction without player time cost.

**Approach & patterns**

- **ADHD-aware batch output**: Large deliverables in single markdown documents; buffer weeks in roadmaps; feel-based phase completion criteria; single "next action" anchor at document bottom.
- **Read before writing**: Claude reads the full compressed codebase extract before writing any code to avoid fabricating interfaces or wiring that doesn't exist.
- **"Done when" criteria**: Every task includes explicit completion conditions, not just descriptions.
- **Design analysis before implementation**: Comprehensive markdown evaluation documents (with ranked options and concrete recommendations) precede any implementation work.
- **Honest, unsentimental feedback**: Milan explicitly requests no sugarcoating on concept viability, timeline realism, or architectural problems.
- **Regression awareness**: File length comparison used as a regression signal — future deliveries of modified existing files should include explicit line count or diff notes for verification.
- **Cowork verification quirk**: after batch file writes, the bash sandbox's mounted view can lag and serve stale/truncated content — always verify freshly written code host-side (Read/Grep tools), never via bash cat/md5 right after writing.

**Tools & resources**

- **Engine**: Unity 6, URP, UI Toolkit (not UGUI)
- **UI architecture**: Controllers in `Abracodabra.UI.Toolkit` namespace, files in `Assets/Scripts/A_ToolkitUI/`. Key controllers: `GameUIManager` (coordinator), `UISeedEditorController`, `UIInventoryGridController`, `UIHotbarController`, `UIDragDropController`, `UISpecSheetController`
- **Service layer**: `InventoryService` and `HotbarSelectionService` are static bridges between UI and game systems
- **Codebase reference files**: `ClaudeProjectFiles/06_Index/Unity_EXTRACTED_scripts.txt` (C#) + `06_Index/Unity_EXTRACTED_ToolkitUI.txt` (UXML/USS) — grep to locate, then open live files (originals in Unity root; extractor .bat auto-syncs 06_Index)
- **Knowledge base**: `ClaudeProjectFiles/` — 01_Core (memory · instructions · codebase map) · 02_Design (+Concepts, WeGo) · 03_Tasks (Active/Done/Roadmaps) · 04_Reviews · 05_Reference · 06_Index · 99_Archive; routing + lifecycle rules in `00_START_HERE.md`; Unity project root holds only the single CLAUDE.md router + extractor tooling + live extracts (2026-07-06 migration: KB folder renamed from "Abracodebra Project Files" to `ClaudeProjectFiles`, all root duplicates deleted after byte-identical verification)
- **Grid constraint**: Z-coordinate must stay 0 for tilemap queries; use `GridPosition` struct and `GridPositionManager` for validation
- **Gene architecture**: `GeneBase` (ScriptableObject) → `ActiveGene` / `ModifierGene` / `PayloadGene`; `SeedTemplate` = default config; `PlantGeneRuntimeState` = player's edited sequence. Key separation: genetic traits (define behaviors) vs. physical items (consumable objects)
- **Output rules**: Full copy-pasteable methods/scripts; no placeholders like `// rest of code`; no versioned filenames (use original names); hook things up in code over Inspector linking to minimize manual work
- **Design philosophy**: Long-term architectural solutions over quick fixes; modular service-oriented design; ScriptableObjects for designer-friendly configuration
