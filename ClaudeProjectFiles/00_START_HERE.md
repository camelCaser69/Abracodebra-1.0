# 00_START_HERE — Abracodabra Knowledge Base Router
*Established 2026-07-05. This file is the table of contents + rulebook for everything in `ClaudeProjectFiles/` (renamed from "Abracodebra Project Files" in the 2026-07-06 machine migration). If a doc isn't findable from here, it's misfiled.*

---

## 1. The One Rule

**Every document has exactly one canonical home, defined by the routing table below. New docs are created there directly; nothing lives in the workspace root except `CLAUDE.md` (the auto-loaded router) and the Unity project.**

---

## 2. Folder Structure & Routing Table

| # | Folder | What goes here | Naming convention |
|---|--------|----------------|-------------------|
| 00 | `00_START_HERE.md` | This router. Update when structure changes. | fixed |
| 01 | `01_Core/` | Always-relevant context: `projectmemory.md` (durable memory), `project_instructions.md` (assistant instructions), `Abracodebra_Codebase_Map.md` (architecture map). | fixed filenames — edit in place, never fork |
| 02 | `02_Design/` | Game design: mechanics, systems, design bibles. Subfolders per system (`WeGo/`), plus `Concepts/` for new ideas/pitches. | bibles: `name_vN.md` (bump N, archive old) · concepts: `ConceptName.md` |
| 03 | `03_Tasks/` | Actionable work. `Active/` = current implementation packs & task docs · `Done/` = shipped (moved as-is) · `Roadmaps/` = plans, backlogs, phase timelines. | `YYYY-MM_Name.md` for new task docs |
| 04 | `04_Reviews/` | Code/foundation reviews, audits, playtest retros, post-mortems. | `YYYY-MM_Name.md` |
| 05 | `05_Reference/` | Third-party & tech reference (DualGrid guides, package docs, cheatsheets). | keep source name |
| 06 | `06_Index/` | **Generated** code indexes (`Unity_EXTRACTED_scripts.txt`, `Unity_EXTRACTED_ToolkitUI.txt`). Auto-synced by the extractor `.bat` — never hand-edit. | tool-managed |
| 99 | `99_Archive/` | Superseded/historical docs, grouped by era (`2025_era/`, `2025_Documentation_GeneGardenSurvivor/`). Archive, don't delete. | keep original names |

**Automatic paths for new docs (quick reference):**
- New task / implementation pack → `03_Tasks/Active/2026-MM_Name.md`
- Task finished → *move the file* to `03_Tasks/Done/` (no rename)
- New review / audit → `04_Reviews/2026-MM_Name.md`
- New concept / mechanic idea → `02_Design/Concepts/Name.md`
- New roadmap / plan → `03_Tasks/Roadmaps/Name.md`
- New system documentation ("how X works") → `02_Design/X/` or `02_Design/Name.md`
- Downloaded/external reference → `05_Reference/`
- Anything superseded → `99_Archive/` (with a one-line note here if notable)

---

## 3. Session Reading Order (how Claude gets context)

1. `CLAUDE.md` (workspace root) — loads automatically; contains rules + this routing table.
2. `01_Core/projectmemory.md` — current state, decisions, principles.
3. `01_Core/Abracodebra_Codebase_Map.md` — architecture, wiring, verified gaps (for any code task).
4. Task-specific: `03_Tasks/Active/` (what's being built), `02_Design/` (feature design), `04_Reviews/` (known issues).
5. Locate code via `06_Index/` greps → then read the **live** `.cs` files (live file always wins).

---

## 4. Maintenance Rules (keeps memory flawless)

- **projectmemory.md** — update at the end of any session that changes state, decisions, or roadmap. Claims about "implemented" systems must be disk-verified, not aspirational (this KB exists because memory once claimed A1–A6 were delivered when they weren't).
- **Codebase map** — patch the affected section after any architecture-level change (new system, moved responsibility, new singleton/event). Full re-sweep only on request.
- **Extractor** — run `unity_extractor_RUN.bat` after code changes; it now auto-copies both extracts into `06_Index/`.
- **CLAUDE.md** — single copy in the Unity project root (since the 2026-07-06 machine move the Unity folder IS the workspace root; the old two-copy layout is gone). No other file may have copies.
- **Task lifecycle** — Active → Done by moving the file; deferred/cut scope gets written into a Roadmaps doc, never left implicit.
- **Versioned design bibles** — new version = new `_vN` file in place, previous version → `99_Archive/`.
- **Nothing is deleted** without byte-identical-duplicate verification; superseded content is archived.

---

## 5. Current Inventory (2026-07-05)

**01_Core/** — `projectmemory.md` · `project_instructions.md` · `Abracodebra_Codebase_Map.md` (full 199-script sweep: boot/tick/gene/UI flows, singleton & event catalogs, red flags)
**02_Design/** — `gene_systems_deep_dive_v6.md` (gene system bible) · `WeGo/wego-system-rework 1–5 + "5 - Copy"` (WeGo migration design history; note: "5 - Copy" differs from 5 — not a duplicate) · `Concepts/Gameplay_Engagement_Research.md` (2026-07 concept research: engagement shortcomings D1–D9 + solutions, minigame audit/program, demo-slice prioritization — nothing implemented)
**03_Tasks/** — `Active/Abracodabra_A_Category_Implementation.md` (**code APPLIED 2026-07-05; remaining: in-Editor wiring + Part 4 checklist, then move to Done/**) · `Done/` (empty) · `Roadmaps/Code_Optimization_Backlog.md` (ex-Todo.md; partially stale — verify against live code before acting)
**04_Reviews/** — `Abracodabra_Foundation_Review_2026-06.md` (A/B/C issue review; A-pack unapplied as of 2026-07-05)
**05_Reference/** — DualGrid user guide + cheatsheet · `07_Third_Party_Package_Guide_DualGrid.md` (older GGS-era guide)
**06_Index/** — both extracts (regenerated + synced 2026-07-06, post-A-pack; `.bat` now auto-syncs)
**99_Archive/** — `2025_era/` (old `Memory.txt`, `PROJECT_KNOWLEDGE_BASE.md`) · `2025_Documentation_GeneGardenSurvivor/` (docs 00–06 from the pre-rename era)

**Outside the KB (by design):** `CLAUDE.md` (single copy, Unity project root) · `unity_extractor.{py,bat,json}` + live `Unity_EXTRACTED_*.txt` outputs (Unity project root).

**2026-07-06 migration note:** project copied to a new machine + Cowork account. KB folder renamed "Abracodebra Project Files" → `ClaudeProjectFiles` (all doc references updated). Root cleanup: deleted byte-identical duplicates (`Memory.txt`, `PROJECT_KNOWLEDGE_BASE.md`, `Todo.md`, `project_instructions.txt`, all root `wego-system-rework*.md`, DualGrid docs, `Documentation/` 00–07) and the stale stray `ClaudeProjectFiles/projectmemory.md`. Repaired: mojibake in `05_Reference/DualgridPackage_user-guide.md`, truncated tail of `01_Core/projectmemory.md`, stale `06_Index/Unity_EXTRACTED_ToolkitUI.txt`. Extractor `.bat` patched to actually auto-sync extracts into `06_Index/`.

---

*Next action anchor: in the Unity Editor — add the **ExecutionPhaseDriver** GameObject (pack Part 3 Step 2), check RunManager's Determinism fields (Step 3), pick ONE starting-loadout source (Step 4), then run the Part 4 test checklist. When green: move the pack to `03_Tasks/Done/` and re-run `unity_extractor_RUN.bat`.*
