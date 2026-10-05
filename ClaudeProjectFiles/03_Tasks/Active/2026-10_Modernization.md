# 2026-10 Modernization — Abracodabra

**What this is:** the task pack that brings this project up to the Bitbloom workflow (Claude Code in the terminal, the Unity CLI bridge, a lean CLAUDE.md, a versioned knowledge base). It is written for Claude Code running in this repo and for Milan.

**Where it lives:** `ClaudeProjectFiles/03_Tasks/Active/2026-10_Modernization.md`. Move it to `03_Tasks/Done/` when Phase E is verified.

**Reference implementation:** Bitbloom, at `D:\Unity Projects\AbraCodebra\Bitbloom\Bitbloom 0.1a\Bitbloom\` (Git Bash: `/d/Unity Projects/AbraCodebra/Bitbloom/Bitbloom 0.1a/Bitbloom/`). Read its files for format; never write there.

**Order:** A + the audit (guide step 3) → B + C after Milan's OK (step 4) → D + E (step 5). The guide is `2026-10_Modernization_Prompts.md`, next to this file.

---

## 0. Audit snapshot (Cowork, 2026-10-05; re-verify on disk before acting)

| Area | Found | Verdict |
| --- | --- | --- |
| Unity | `6000.0.39f1`, URP 17.0.3, UI Toolkit (5 `.uss`, 3 `.uxml`), Input System 1.13, 236 `.cs`, **no asmdefs, no tests** | Upgrade to `6000.3.24f1` (Phase E) |
| URP | `m_EnableRenderCompatibilityMode: 0`, no `ScriptableRendererFeature` / `ScriptableRenderPass` in code | The biggest 6.3 blocker (Compatibility Mode removed) does not apply |
| Git | `main`, remote `camelCaser69/Abracodebra-1.0`, last commit 2026-07-10; uncommitted: `projectmemory.md` (+6 lines), 6 untracked July docs, `Claude outputs/` | Commit as baseline first (Phase A) |
| Git | Local branch `charming-elgamal` (a Claude worktree branch): 1 commit not on main, "no idea what are the changes, but there were a lot" | Diff it, report, Milan decides |
| Git | `.git/index.lock.stale_from_claude` (0 bytes; a lock left by Cowork on 2026-10-05, already renamed so it blocks nothing) | Delete |
| Git | `.idea/`, `.vscode/`, both `Unity_EXTRACTED_*.txt` pairs (766 KB each, root + `06_Index/`) and the extractor are tracked | Untrack / archive (Phase B) |
| Old workflow | `unity_extractor.py/.bat/.json`, `06_Index/`, `.clinerules/` (Cline, 2025), `01_Core/project_instructions.md` (claude.ai-Project copy of CLAUDE.md), `Chat_History_Digest.md` (written because account memory was off) | Retired: Claude Code reads the live files and the Unity CLI reads the Editor |
| Strays | `Claude outputs/` holds Bitbloom's bootstrap files from 2026-09-20 (CLAUDE.md, Project_Bible.md, projectmemory.md) — not Abracodabra's | Archive |
| Strays | `UIElementsSchema/` (generated), `HueFolders.Editor.csproj`, `skner.DualGrid*.csproj` (generated, already ignored) | Ignore |
| Assets | `Assets/TextMesh Pro/` = **1.2 GB**: ~100 unused-looking SDF font assets in `Fonts/Extracted Fonts/` (Inter ×3 sizes ×18 weights, Exo2, SourceSans3, …, 5–13 MB each), `NotoColorEmoji-Regular.ttf` 24 MB, `SEGUIEMJ.TTF` 12 MB | GUID-check, move the unreferenced ones out of the project |
| Packages | `com.unity.visualscripting` (no code uses it), `com.unity.multiplayer.center`, `com.unity.collab-proxy` (Unity Version Control; we use git) | Remove |
| KB | `ClaudeProjectFiles/` already has a router (`00_START_HERE.md`), 01–06 + 99 folders, 41 files, 1.5 MB. `projectmemory.md` is 26 KB of dense prose without a Snapshot/Decision-log split. `02_Design/WeGo/` holds rework 1–5 + "5 - Copy". | Consolidate to the Bitbloom layout (Phase C) |

---

## 1. Target state ("modern" means this)

- [ ] **Two lanes.** Code: Claude Code in the terminal, in this folder, one batch per session, `/clear` between batches. Design: a Cowork project writing `.md` into `ClaudeProjectFiles/`. The files on disk (and git) are the only source of truth.
- [ ] **Editor bridge: Unity CLI + `com.unity.pipeline`.** Replaces the extractor and any MCP bridge. Direct `unity command …` calls, not `unity mcp configure` (MCP costs more tokens for the same thing).
- [ ] **CLAUDE.md** in Bitbloom's shape, ≤ ~12 KB, sections §0–§8 (below).
- [ ] **Knowledge base** in Bitbloom's layout with fixed living docs, dated point-in-time docs, git as the version history.
- [ ] **`.claude/settings.json`** (committed) with permissions; `.claude/settings.local.json` ignored.
- [ ] **Git hygiene:** clean `.gitignore` / `.gitattributes`, tags at milestones, a commit per step.
- [ ] **Unity 6000.3.24f1**, same editor as Bitbloom.

### 1.1 The new CLAUDE.md (write it in Phase C)

Copy Bitbloom's CLAUDE.md structure and wording where it is about Milan or the workflow; write the project parts from this repo's current CLAUDE.md and the codebase map. Sections:

| § | Content | Source |
| --- | --- | --- |
| Intro | One paragraph: what Abracodabra is, who Milan is | Current CLAUDE.md intro + Bitbloom's line about Milan's background |
| 0 Session start | Read `projectmemory.md` → for code `Codebase_Map.md` → check `03_Tasks/Active/` → after an interruption check disk first. Unity CLI: console errors before and after a code change | Bitbloom §0 |
| 1 Architecture invariants | Namespaces, tick and run flow, gene model, UI Toolkit (not UGUI), services, `GridPosition` Z = 0, `IDeterministicRandom` (no `UnityEngine.Random` in gameplay), idempotent derived stats, code wiring over Inspector, `UI[Name]Controller` naming. **Plus the Planning-tick invariant from the Fable 5 ledger** (Planning ticks advance only the action economy) | Current CLAUDE.md "Architecture Overview" + Ledger |
| 2 Code output rules | Bitbloom §2 verbatim, minus the Burst/Core rule; keep this project's "surgical patches with exact anchors for files over ~300 lines" rule | Both |
| 3 Workflow | Bitbloom §3, without the dotnet-test line until tests exist | Bitbloom §3 |
| 4 Memory protocol | Bitbloom §4 table, with this project's extra folders (`04_Reviews/`, `05_Reference/`) | Bitbloom §4 + current routing table |
| 5 Deliverable style | Bitbloom §5 (markdown only, checklists + "things to consider" + one Next action, red/green never alone, UI explains itself) | Bitbloom §5 |
| 6 Design context | Pillars (the plant is the health bar, edit speed governs the editor, Visual Genome, demo-first), touchstones, the decided Ledger calls (D1–D10) in one line each | Current CLAUDE.md + Ledger |
| 7 Tools | Unity CLI loop and its gotchas (Phase D), GitHub Desktop + Claude commits | Bitbloom §7 + this pack |
| 8 Usage policy | Bitbloom §8, corrected: **Claude Max (5x)**, the weekly limit is the constraint, resets Thu 04:00 Prague; Model · Effort · Est. time · Est. usage line on every prompt; Sonnet · high default, opusplan · high for structural work, xhigh only for the hardest step; Fable for read-only design/review beside the code lane | Bitbloom §8 |

**Drop:** the extractor and `06_Index` reading order, "account memory is OFF", the weekly KB doctor, the two-copy/machine-move notes, the ✅▶️⏸️ tracker.

---

## 2. Phase A — Safety baseline (git)

- [ ] `git status`; commit everything currently on disk as `chore: baseline before 2026-10 modernization` (projectmemory edit, the 6 July docs, `Claude outputs/`, this pack). Push.
- [ ] Tag `pre-modernization` and push the tag.
- [ ] `charming-elgamal`: `git diff --stat main...charming-elgamal` and a one-paragraph summary of what it changes. **Stop and ask Milan** (merge, cherry-pick, or delete). Do not merge on your own.
- [ ] Delete `.git/index.lock.stale_from_claude`.
- [ ] Remote branches: list any `claude/*` branches on origin; report, don't delete.

**Done when:** `git status` is clean, `main` = `origin/main`, the tag exists on GitHub. **How to check:** GitHub Desktop shows no changes and no unpushed commits; github.com → Abracodebra-1.0 → Tags shows `pre-modernization`.

---

## 3. Phase B — Cleanup

Rules: never delete design content (archive it); generated files may be deleted; anything inside `Assets/` moves only with its `.meta`, and only after a GUID reference check; commit after each row group.

### 3.1 Root and repo files

| Item | Action |
| --- | --- |
| `Unity_EXTRACTED_scripts.txt`, `Unity_EXTRACTED_ToolkitUI.txt` (root) | Delete (generated); add `Unity_EXTRACTED_*.txt` to `.gitignore` |
| `unity_extractor.py`, `unity_extractor_RUN.bat`, `unity_extractor_settings.json` | Move to `ClaudeProjectFiles/99_Archive/2026-07_Cowork_Era/tools/` |
| `.clinerules/` | Move to `99_Archive/2025_Cline/` |
| `Claude outputs/` | Move to `99_Archive/2026-09_Bitbloom_Bootstrap/` (Bitbloom's first drafts; Bitbloom has its own copies) |
| `.idea/`, `.vscode/` | `git rm -r --cached`; already-ignored patterns stay |
| `UIElementsSchema/` | Add to `.gitignore` (it regenerates) |
| `.gitignore` | Merge in Bitbloom's extras: `*.slnx`, `.claude/settings.local.json`, `/[Pp]rofilerCaptures/`, `UIElementsSchema/`, `Unity_EXTRACTED_*.txt` |
| `.gitattributes` | Use Bitbloom's (`* text=auto` + `*.sh text eol=lf`) |

### 3.2 Assets (GUID check first)

- [ ] For every file in `Assets/TextMesh Pro/Fonts/Extracted Fonts/`, `Assets/TextMesh Pro/Examples & Extras/`, `NotoColorEmoji-Regular.ttf`, `SEGUIEMJ.TTF`: read its GUID from the `.meta` and grep all `*.unity`, `*.prefab`, `*.asset`, `*.mat`, `*.uss`, `*.uxml`, `*.cs` under `Assets/` and `ProjectSettings/`. Also follow font fallback chains (a used font asset can reference another as fallback).
- [ ] Write the result table (file, size, referenced by) into §7 of this pack. **Stop for Milan's OK.**
- [ ] After OK, with the **Editor closed**: move the unreferenced files and their `.meta` to `D:\Unity Projects\AbraCodebra\_Vault\Abracodebra_TMP_fonts\` (outside every repo). Commit `chore: move unused TMP font assets out of the project (−X MB)`.
- [ ] `Assets/Editor/TileMappingsBackup/`: report what it is; archive only on Milan's OK.

### 3.3 Packages

- [ ] Remove `com.unity.visualscripting`, `com.unity.multiplayer.center`, `com.unity.collab-proxy` from `Packages/manifest.json` (grep first; no code uses Visual Scripting as of the audit). Milan opens the Editor once to let it resolve; console must stay clean.

**Done when:** the repo root holds only Unity folders, `CLAUDE.md`, `ClaudeProjectFiles/`, git/IDE config and `Tools/` (if created); the project opens with 0 console errors. **How to check:** open the project in Unity Hub, Window → General → Console shows no red; `git status` clean.

---

## 4. Phase C — Knowledge base consolidation and versioning

### 4.1 Final layout

```
ClaudeProjectFiles/
  01_Core/        projectmemory.md · Codebase_Map.md · (later) Project_Bible.md        fixed names, edit in place
  02_Design/      living design docs (Gene_Systems_Deep_Dive.md, Feedback_Log.md, Glossary.md)
                  Concepts/   research and concept passes, dated: Name_YYYY-MM-DD.md
                  WeGo/       only the current WeGo doc
  03_Tasks/       Active/ · Done/ · Roadmap.md                                          packs: YYYY-MM_Name.md
  04_Reviews/     YYYY-MM_Name.md
  05_Reference/   third-party docs (DualGrid)
  90_SideIdeas/   README.md (what's here, "not the design of record")
  99_Archive/     <YYYY-MM_Era>/ folders, never deleted
```

### 4.2 Moves

| From | To |
| --- | --- |
| `00_START_HERE.md` | Its routing table goes into CLAUDE.md §4; the file → `99_Archive/2026-07_Cowork_Era/` |
| `01_Core/project_instructions.md`, `01_Core/Chat_History_Digest.md` | `99_Archive/2026-07_Cowork_Era/` (keep the digest's still-true facts in projectmemory first) |
| `01_Core/Abracodebra_Codebase_Map.md` | `git mv` → `01_Core/Codebase_Map.md`; fix references |
| `02_Design/gene_systems_deep_dive_v6.md` | `git mv` → `02_Design/Gene_Systems_Deep_Dive.md`; first line notes "v6, 2025" |
| `02_Design/WeGo/wego-system-rework.md` … `5.md`, `5 - Copy.md` | `99_Archive/2025-06_WeGo_Rework/` (keep "5 - Copy"; it differs). Leave a 10-line `02_Design/WeGo/README.md` saying which rework is current and what the 2026-07 Phase Identity doc superseded |
| `03_Tasks/Roadmaps/Code_Optimization_Backlog.md` | Merge into `03_Tasks/Roadmap.md` (flag stale items "verify before acting"); old file → archive |
| `03_Tasks/Active/*` | Triage each: still actionable → stays; plan whose decisions are recorded in the Ledger → `Done/` or archive; list the verdicts in §7 and ask before moving |
| `06_Index/` | Delete the extracts (generated); remove the folder |
| `99_Archive/2025_era/`, `2025_Documentation_GeneGardenSurvivor/` | Stay (already archived) |

### 4.3 New and rewritten files

- [ ] **`01_Core/projectmemory.md`** in Bitbloom's format: a header explaining the file; **Snapshot** (≤ 40 lines: what the game is, what's built, the design of record and where it lives, open for Milan, next action); **Recent entries**; **Decision log** table (date · decision · where); **Code facts**; **Learnings**. Everything older moves verbatim to `99_Archive/projectmemory_history.md`. Target ≤ 30 KB.
- [ ] **`02_Design/Feedback_Log.md`**: start with Milan's cross-project rules from Bitbloom's Feedback_Log "Distilled rules" that are about him rather than about Bitbloom (red/green never alone, short playtest sheets in play order, terms explain themselves with a hover, one concept in one or two places, etc.), plus the Abracodabra feedback already in projectmemory and the Ledger (manual avatar piloting felt finicky and hectic → out of the design space; WeGo felt cortisol-heavy). Each rule keeps its source and date.
- [ ] **`90_SideIdeas/README.md`**.
- [ ] **CLAUDE.md** rewritten per §1.1.
- [ ] **`.claude/settings.json`**: allow `git` read/commit, `unity command console*`, `recompile*`, `run_tests`, `list_tests`, `editor_play`, `editor_stop`, `capture_game_view`, `unity status`; **deny** `unity command eval*`, `eval_file*`, `run_script*` (they run arbitrary C# in the Editor; ask each time instead).

### 4.4 Versioning rules (write these into CLAUDE.md §4)

1. **Git is the version history.** No `_v2`, `_vN`, "- Copy" or "final" in filenames. A living doc is edited in place; its history is `git log -p`.
2. **Point-in-time docs are dated** (`YYYY-MM-DD` for design passes and reviews, `YYYY-MM_` for task packs) and never edited after they close, except a "Superseded by …" line at the top.
3. **Superseded → `99_Archive/<YYYY-MM_Era>/`**, never deleted.
4. **Milestone tags**: `pre-modernization`, `pre-unity-6.3`, `kb-2026-10`, later `demo-0.1` etc. A tag is how you get "the docs as they were".
5. **One design of record.** projectmemory's Snapshot names the documents that are current; anything not named there is history or a concept.
6. **The Cowork project holds one pointer doc only** (`claude/abracodabra_status.md`: where the repo is, what to read first, the current block). Never upload KB copies into project knowledge; they go stale.

**Done when:** every file in `ClaudeProjectFiles/` has a home per §4.1, CLAUDE.md is ≤ ~12 KB, projectmemory ≤ 30 KB, and a fresh Claude Code session that reads only CLAUDE.md + projectmemory can say what the game is, what's built and what's next. **How to check:** `/clear`, then ask Claude Code "What is this project, what's built, what's next?" — the answer must match the Snapshot. Tag `kb-2026-10`.

---

## 5. Phase D — Unity CLI hookup (on 6000.0.39f1, before the upgrade)

Milan's steps (PowerShell, in the project folder, Editor open):

1. [ ] `& "$env:LOCALAPPDATA\Unity\bin\unity.exe" status` — the CLI is already installed for Bitbloom.
2. [ ] `& "$env:LOCALAPPDATA\Unity\bin\unity.exe" pipeline install` (or Package Manager → + → Add package by name → `com.unity.pipeline`, same version as Bitbloom: `0.7.0-exp.1`). Wait for the recompile.
3. [ ] `unity status` reports this project; `unity command console --level error` returns.
4. [ ] Optional: `unity skill install claude --local` (adds Unity's own CLI skill under `.claude/skills/`; commit it).

Claude's steps:

- [ ] Record the **baseline**: the console errors and warnings (count + first 15), and a `capture_game_view` of SampleScene after 10 s of play, saved under `ClaudeProjectFiles/04_Reviews/2026-10_Baseline_6.0/` (move the PNG out of `Assets/` after capture). This is the "before" for Phase E.
- [ ] Write the CLI gotchas into CLAUDE.md §7 (all learned in Bitbloom): not on PATH, use `"$env:LOCALAPPDATA\Unity\bin\unity.exe"` from PowerShell; editor commands are `unity command <name>`; a job past the 300 s default timeout wedges the pipeline until the Editor restarts, so long test runs use `--detach` and their own `--timeout`; `capture_game_view` must save inside the project (lands under `Assets/`, delete after) and captures the camera only, not IMGUI; **with two Editors open (Bitbloom + Abracodabra) the CLI targets the Editor whose project contains the current directory; pass `--project-path` when it reports `AMBIGUOUS_EDITOR`.**
- [ ] Optional, later: adapt Bitbloom's `Tools/UnityCompileCheck/compile_check.py` (for when the Editor stops answering) to this project's single `Assembly-CSharp.csproj`.

**Done when:** `unity command console --level error` answers for this project with Bitbloom's Editor open at the same time. **How to check:** both Editors open; run the command from this folder; it reports Abracodabra's console.

---

## 6. Phase E — Upgrade to Unity 6000.3.24f1

Why: 6.0 LTS support ends in October 2026; 6.3 LTS is supported to December 2027; Bitbloom already runs 6000.3.24f1 (one editor install, identical CLI behaviour, code and shaders can move between the projects).

Known 6.3 changes that touch this project:

| Change | Risk here |
| --- | --- |
| URP Compatibility Mode removed | **None found**: already on Render Graph, no custom render passes |
| UI Toolkit: stricter USS parser blocks import of files with syntax errors or unsupported properties | 5 `.uss` files: check each after import |
| `[SerializeField]` restricted to fields | Grep found none on properties; recheck after compile |
| Package bumps (URP 17.3, 2D, Input System, Test Framework 1.6, Timeline) | Match Bitbloom's manifest versions where the package exists in both |
| Third-party: HueFolders (Assets), skner DualGrid 2.0.2 (embedded package) | Check compile + the dual-grid tilemap draws |

Steps:

1. [ ] Milan: GitHub Desktop → Branch → New branch `upgrade/unity-6.3`. Claude: tag `pre-unity-6.3` on main.
2. [ ] Milan: close the Editor. Unity Hub → Projects → the version cell of Abracodebra 1.0 → 6000.3.24f1 → Change version. Accept the API Updater if asked. Wait for the reimport (Phase B's font move makes this much faster).
3. [ ] Claude: `unity command recompile` → `recompile_status` → `console --level error`; fix in batches; commit per fix group with the error it fixed.
4. [ ] Claude: update package versions in `manifest.json` to match Bitbloom's where they overlap; recompile; console clean.
5. [ ] Claude: the same `capture_game_view` as the Phase D baseline; put both PNGs side by side in `04_Reviews/2026-10_Unity63_Upgrade.md` with the error list and what changed.
6. [ ] Milan's smoke test (≈5 min, in play order, SampleScene): planning phase shows → edit a seed's genes (drag-drop works, text renders) → Start Day → plants grow, genes fire → a wave arrives, pests eat leaves → Doris eats → Space pauses, Tab speeds → dual-grid tiles and post-processing look like the baseline.
7. [ ] Green → merge `upgrade/unity-6.3` into main (GitHub Desktop), push, update projectmemory (Unity version, Decision log row).

**Done when:** 0 console errors on 6000.3.24f1, the smoke test passes, main is on 6.3. **How to check:** `ProjectSettings/ProjectVersion.txt` says `6000.3.24f1`; the smoke test sheet is all yes.

---

## 7. Results (Claude fills this in)

### 7.1 Phase A
_(charming-elgamal summary, remote branches)_

### 7.2 Phase B
_(font GUID table: file · MB · referenced by; TileMappingsBackup; package removal)_

### 7.3 Phase C
_(03_Tasks/Active triage verdicts; anything that didn't fit the layout)_

### 7.4 Needs Milan
_(every question, one line each, with a recommendation)_

---

## 8. Things to consider

- **Decided 2026-10-05: Abracodabra stays a live game, developed alongside Bitbloom.** Bitbloom's lessons come in through a design pass (step 6 of the prompts file), not by porting code mid-block.
- **No asmdefs, no tests.** Everything compiles into Assembly-CSharp, so EditMode tests can't reference gameplay code and compile times grow with every script. The July audit's C3 (golden-run harness) needs an asmdef split first. Put "asmdef split + golden-run harness" near the top of `Roadmap.md` as a candidate for Block 1 or 2; it isn't part of this pack.
- **Time cap:** this pack takes one week at most. Its job is to get back to building the game, not to perfect the tooling.
- **Font licensing.** `SEGUIEMJ.TTF` is Windows' Segoe UI Emoji; Microsoft's system fonts generally can't be redistributed in a game build. If any UI uses it, plan a replacement (Noto Color Emoji is OFL).
- **The old Cowork desktop's KB doctor.** If a weekly "KB doctor" scheduled task still exists in the old desktop setup, turn it off: it checks `06_Index` freshness, which this pack removes.
- **The sibling folder `Abracodebra 1.0 - Backup 08.07.2026`.** Once Phase A is pushed and tagged, GitHub holds everything it holds. Zip it to cold storage or delete it; Milan's call.
- **Don't rename `D:\Unity Projects\AbraCodebra\`** or the project folder now: Bitbloom's docs, Unity Hub and the CLI paths depend on them.
- **Git LFS:** not now. The repo is ~140 MB without LFS; migrating history isn't worth it. Revisit when audio or large art arrives.
- **Bitbloom's CLAUDE.md §8 still says "Pro plan".** Fix it there too (separate commit in Bitbloom's repo, Bitbloom's code lane).

---

**Next action:** Milan follows `2026-10_Modernization_Prompts.md` (the step-by-step restart guide) from step 1.
