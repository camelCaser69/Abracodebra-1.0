# 2026-10 Modernization — Abracodabra

**What this is:** the task pack that brings this project up to the Bitbloom workflow (Claude Code in the terminal, the Unity CLI bridge, a lean CLAUDE.md, a versioned knowledge base). It is written for Claude Code running in this repo and for Milan.

**Where it lives:** `ClaudeProjectFiles/03_Tasks/Active/2026-10_Modernization.md`. Move it to `03_Tasks/Done/` when Phase E is verified.

**Reference implementation:** Bitbloom, at `D:\Unity Projects\AbraCodebra\Bitbloom\Bitbloom 0.1a\Bitbloom\` (Git Bash: `/d/Unity Projects/AbraCodebra/Bitbloom/Bitbloom 0.1a/Bitbloom/`). Read its files for format; never write there.

**Order:** A + the audit (guide step 3) → B + C after Milan's OK (step 4) → D + E (step 5). The guide is `2026-10_Modernization_Prompts.md`, next to this file.

---

## 0. Audit snapshot (Cowork, 2026-10-05; re-verify on disk before acting)

| Area | Found | Verdict |
| --- | --- | --- |
| Unity | `6000.0.39f1`, URP 17.0.3, UI Toolkit (5 `.uss`, 3 `.uxml`), Input System 1.13, 236 `.cs` under `Assets/` (259 with the embedded DualGrid package), **no asmdefs of its own (3 third-party: HueFolders, DualGrid ×2), no tests** (the Test Framework package is installed, nothing uses it) ✔ verified 2026-10-05 | Upgrade to `6000.3.24f1` (Phase E) |
| URP | `m_EnableRenderCompatibilityMode: 0` (in `Assets/UniversalRenderPipelineGlobalSettings.asset`), no `ScriptableRendererFeature` / `ScriptableRenderPass` in code ✔ | The biggest 6.3 blocker (Compatibility Mode removed) does not apply |
| Git | `main`, remote `camelCaser69/Abracodebra-1.0`, last commit 2026-07-10; uncommitted: `projectmemory.md` (+6 lines), 6 untracked July docs, `Claude outputs/` ✔ (the "6 July docs" were really 6 untracked files plus the pack and its guide: 9 untracked entries) | Committed as baseline `3a0abe0` in Phase A |
| Git | **Corrected:** local branch `charming-elgamal` (tip `2f2260d`, 2025-12-28, "no idea what are the changes…") is **already an ancestor of `main`**: 0 commits not on main, main is 59 commits ahead, no remote branch, no worktree. The "lot of changes" (86 files, +10 449 / −7 038) are that commit itself, already in main's history since 2025 | Nothing to merge; deleting the local branch is safe (§7.1, §7.4) |
| Git | `.git/index.lock.stale_from_claude` (0 bytes; a lock left by Cowork on 2026-10-05, already renamed so it blocks nothing) ✔ | **Deleted** in Phase A |
| Git | Tracked: `.idea/` (4 files), `.vscode/` (3 files), root `Unity_EXTRACTED_*.txt` (2: 766 KB scripts + 25 KB UI) and `06_Index/` copies (2), the extractor (3 files), `.clinerules/` (1) ✔. **Corrected: `UIElementsSchema/` is also tracked (28 `.xsd` files)**, not only generated | Untrack / archive (Phase B); `UIElementsSchema/` needs `git rm -r --cached`, not just an ignore line |
| Old workflow | `unity_extractor.py/.bat/.json`, `06_Index/`, `.clinerules/` (Cline, 2025), `01_Core/project_instructions.md` (claude.ai-Project copy of CLAUDE.md), `Chat_History_Digest.md` (written because account memory was off) | Retired: Claude Code reads the live files and the Unity CLI reads the Editor |
| Strays | `Claude outputs/` holds Bitbloom's bootstrap files from 2026-09-20 (CLAUDE.md, Project_Bible.md, projectmemory.md) — not Abracodabra's | Archive |
| Strays | `UIElementsSchema/` (generated, **but tracked**, see above), `HueFolders.Editor.csproj`, `skner.DualGrid*.csproj` (generated, already ignored by `*.csproj`) ✔ | Ignore (and untrack the schema) |
| Assets | `Assets/TextMesh Pro/` = **1.2 GB** (Fonts 1 129 MB, Examples & Extras 17 MB) ✔. **Corrected:** `Fonts/Extracted Fonts/` holds 178 font families as SDF `.asset` + `.ttf` pairs (356 files, 1 078 MB); only **Handjet-Regular and Handjet-Medium** are used. `NotoColorEmoji-Regular.ttf` (24 MB) and `SEGUIEMJ.TTF` (12 MB) are **both in use** (Noto: TMP Settings emoji fallback; SEGUIEMJ: `StatusEffect_Icon.prefab`), so neither is a removal candidate | GUID-checked (§7.2); move the unreferenced ones out of the project |
| Packages | `com.unity.visualscripting` (no code uses it), `com.unity.multiplayer.center`, `com.unity.collab-proxy` (Unity Version Control; we use git) ✔ none has a dependent in `packages-lock.json`, none appears in code or ProjectSettings | Remove |
| KB | `ClaudeProjectFiles/` already has a router (`00_START_HERE.md`), 01–06 + 99 folders, **43 files, 1.6 MB** (41 + the pack and its guide). `projectmemory.md` is 26 KB (25 971 B, 70 lines) of dense prose without a Snapshot/Decision-log split ✔. `02_Design/WeGo/` holds rework 1–5 + "5 - Copy" ✔ (they are in `02_Design/WeGo/`, not `99_Archive/`). Repo: 4 415 tracked files, `.git` 145 MB. | Consolidate to the Bitbloom layout (Phase C) |

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

- [x] `git status`; commit everything currently on disk as `chore: baseline before 2026-10 modernization` (projectmemory edit, the 6 July docs, `Claude outputs/`, this pack). **Committed locally as `3a0abe0` (2026-10-05). ⏸ Push NOT done: the permission classifier denied it; Milan pushes (§7.4 Q1).**
- [x] Tag `pre-modernization` created locally on `3a0abe0`. **⏸ Tag push NOT done (same reason).**
- [x] `charming-elgamal`: already merged into main (§0, §7.1). **Waiting for Milan** (delete the local branch, or keep).
- [x] Delete `.git/index.lock.stale_from_claude` (done).
- [x] Remote branches: only `origin/main` exists (`git ls-remote --heads origin`); no `claude/*` branches, no remote tags yet.

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

_Written by Claude Code, session 1 (2026-10-05, opusplan · high). Nothing was moved, deleted or rewritten except what Phase A says, and the corrections to §0 and the Phase A boxes in §2._

### 7.1 Phase A

| Item | Result |
| --- | --- |
| Baseline commit | `3a0abe0` "chore: baseline before 2026-10 modernization": 12 files (projectmemory edit, 6 untracked July docs, `Claude outputs/`, this pack and its guide). Local only. |
| Tag | `pre-modernization` on `3a0abe0`, local only. |
| Push | **Not done.** Both `git push origin main` and the tag push were refused by the permission classifier ("Out-of-Place Publication"). I did not try to get around it. `main` is ahead of `origin/main` by 1 commit (2 after the §7 commit); `git ls-remote --tags origin` is empty. Milan: §7.4 Q1. |
| `.git/index.lock.stale_from_claude` | Deleted. |
| Remote branches | `git ls-remote --heads origin`: only `refs/heads/main` (`fea4718`). No `claude/*` branches. |
| `charming-elgamal` | **Already merged. Nothing to decide about its content.** Tip `2f2260d` ("no idea what are the changes, but there were a lot", camelCaser69, 2025-12-28); parent `d6ba21d` (2025-10-25, "submit before replacing UI with UI Toolkit"). `git merge-base --is-ancestor charming-elgamal main` is true; `git rev-list --count main..charming-elgamal` = **0**; main is 59 commits ahead. So it is not "1 commit not on main": it is main's own history up to 2025-12-28. The commit was a big housekeeping snapshot: 86 files, +10 449 / −7 038, mostly `UIElementsSchema/UnityEngine.UIElements.xsd` (+2 138), `Unity_EXTRACTED_scripts.txt` (±913), new `extract_ui.py` / `run_extract_ui.bat`, plus script edits. Local-only (no `origin/charming-elgamal`), not checked out in any worktree (`git worktree list` shows only main). Deleting the label (`git branch -d charming-elgamal`) loses nothing, and `-d` refuses if it were unmerged. |

### 7.2 Phase B

**Method.** Candidates: every non-`.meta` file under `Assets/TextMesh Pro/Fonts/` (534 files incl. the pack's four named targets) and `Assets/TextMesh Pro/Examples & Extras/` (140 files). GUID read from each `.meta`; every file under `Assets/`, `ProjectSettings/`, `Packages/`, `UserSettings/` (793 text/serialized files; images, fonts and DLLs skipped) scanned for those 32-hex GUIDs (covers `.unity .prefab .asset .mat .uss .uxml .cs .json .tss`). References from candidate to candidate were followed as a closure (SDF asset → source `.ttf`, fallback tables). Cross-checks: `.cs` for `Resources.Load` / `TMP_FontAsset` / font names (none), `.uss`/`.uxml` for font definitions (none: the UI uses the default font), `TMPro.Examples` types in project code (none; the TMP example `CameraController` is namespaced, the game's own is global, no clash). Active scene list: only `Assets/Scenes/SampleScene.unity`. Re-run the same scan right before moving anything (Phase B step B5).

**Referenced: 9 files, 40 MB. Keep.**

| File | MB | Referenced by |
| --- | ---: | --- |
| `Fonts/Extracted Fonts/Handjet-Regular SDF.asset` | 1.52 | 11 prefabs: Animal_Bunny, Animal_Deer, ItemSlotPrefab, PassiveSlotPrefab, SeedEditSlot, SequenceRowPrefab, SequenceBreakdownEntry_Prefab, Statbar_Prefab, SynergyWarningEntry_Prefab, ThoughtBubble, GardenerPrefab |
| `Fonts/Extracted Fonts/Handjet-Regular.ttf` | 0.22 | the SDF asset above (source font) |
| `Fonts/Extracted Fonts/Handjet-Medium SDF.asset` | 1.52 | `PlantPrefab.prefab` |
| `Fonts/Extracted Fonts/Handjet-Medium.ttf` | 0.22 | the SDF asset above |
| `Fonts/LiberationSans.ttf` | 0.35 | `Resources/Fonts & Materials/LiberationSans SDF.asset` and `… - Fallback.asset` (TMP default font) |
| `Fonts/NotoColorEmoji-Regular SDF emoji.asset` | 0.54 | `Resources/TMP Settings.asset` → `m_EmojiFallbackTextAssets` |
| `Fonts/NotoColorEmoji-Regular.ttf` | **24.27** | the Noto SDF asset (source font) |
| `Fonts/SEGUIEMJ SDF emoji.asset` | 0.70 | `Prefabs/Ecosystem/UI/StatusEffect_Icon.prefab` (the `UnicodeText` child) |
| `Fonts/SEGUIEMJ.TTF` | **12.42** | the SEGUIEMJ SDF asset (source font) |

No fallback chain pulls in anything else: no kept font asset lists another candidate as fallback; `TMP Settings.m_fallbackFontAssets` is empty.

**Unreferenced: 525 files, 1 157 MB (+ their `.meta`).**

| Group | Files | MB | Verdict |
| --- | ---: | ---: | --- |
| `Extracted Fonts/`, 176 families as SDF `.asset` + `.ttf` pairs (all except Handjet-Regular and Handjet-Medium) | 352 | 1 124.7 | **Move out** (breakdown below) |
| `Examples & Extras/` (Fonts 16 files 10.9 MB, Resources 30, Scenes 32, Scripts 34, Textures 17, Materials 6, Prefabs 3, Sprites 2) | 140 | 16.8 | **Move out** (whole folder + its `.meta`; no project code uses its scripts) |
| `Fonts/Kenney *.asset` ×12, `Kenney fonts/` (14 files), `m3x6` / `m5x7` / `m6x11` (`.ttf` + SDF), `LiberationSans - OFL.txt` | 33 | 15.9 | **Keep** (rec; small pixel fonts that fit the look and may be wanted for UI; the OFL text belongs with LiberationSans) |

Extracted Fonts breakdown (unreferenced files, MB): Inter_18pt 221 · Inter_24pt 221 · Inter_28pt 221 · SourceSans3 133 · Exo2 106 · ChakraPetch 45 · SourceCodePro 36 · Ubuntu 35 · Comfortaa 28 · Handjet (the other 8 weights) 14 · AtkinsonHyperlegible 10 · SpaceMono 9 · Caveat 9 · Inter (variable) 6 · UbuntuCondensed 5 · OdibeeSans 5 · UbuntuMono 3 · and 13 single-weight families of 1–2 MB (Bungee, UnicaOne, VT323, Righteous, Handlee, ShareTech, ShareTechMono, PatrickHand, ReenieBeanie, ShortStack, GloriaHallelujah, ShadowsIntoLight, ArchitectsDaughter).

**Result if the rec is followed:** 492 files + 492 `.meta` move (**1 141.5 MB ≈ 1.14 GB**); `Assets/TextMesh Pro/` drops from ~1.2 GB to ~60 MB. `.git` stays ~145 MB (the fonts remain in history). Both emoji fonts stay.

**Findings to act on**
- **`SEGUIEMJ.TTF` is in use** (status-effect icons). It is Windows' Segoe UI Emoji; Microsoft's licence does not let a shipped game redistribute it. Keep it for now; put "swap StatusEffect_Icon to the Noto emoji asset (OFL) before any public build" on the Roadmap (§7.4 Q5).
- **`NotoColorEmoji-Regular.ttf` is in use** (TMP emoji fallback), contrary to the audit row's "unused-looking".
- Only the `Assets/` + `ProjectSettings/` + `Packages/` text assets were scanned. Anything loaded from a path built at runtime would not show; there are no `Resources.Load` calls for fonts, and none of the moved files sits in a `Resources/` folder.

**`Assets/Editor/TileMappingsBackup/`** (5 KB: `TileMappingsBackup.json` + `.meta`; the folder has its own `.meta`): a JSON of tile-definition GUID → tilemap-module mappings for `SampleScene` / `Manager_Tiles`. `TileInteractionManager.cs` (lines 510–511) hard-codes the path `Assets/Editor/TileMappingsBackup` / `TileMappingsBackup.json` as its backup/restore location. Last touched in git 2026-07-08. **Not an orphan: keep, don't archive** (moving it would break that routine's restore). §7.4 Q6.

**Packages (§3.3).** `visualscripting 1.9.5`, `multiplayer.center 1.0.0`, `collab-proxy 2.7.1`: each is a direct dependency (depth 0) with **no dependents** in `packages-lock.json`; no hit in `Assets/**/*.cs`, `*.asmdef`, `*.asset` or `ProjectSettings/`; no `scriptingDefineSymbols`. Safe to remove from `Packages/manifest.json`.

### 7.3 Phase C (03_Tasks/Active triage; no file moved)

| File | Checked on disk | Verdict |
| --- | --- | --- |
| `2026-07_Fable5_Design_Decision_Ledger.md` (7 KB) | The decision record of D1–D10; D1 and D5 still wait for the Commit-&-Watch A/B experiment ("Next action"); no experiment toggle exists in code. Its warning "Planning-tick invariant not yet enforced in code" was not re-verified (`TickManager` treats Planning as action-driven, lines 29–35; nothing checks it). | **Stays in Active** (living; its D1–D10 go into CLAUDE.md §6 and projectmemory's Decision log). Becomes Done when D1/D5 are marked resolved. |
| `2026-07_Fable5_Last_Day_Plan.md` (16 KB) | F1–F4 are all **unapplied**: `RuntimeSequenceSlot` is still live in 4 files (no DNA-strand buffer), no `PlayerInventory` / `SequenceParser`, no run-loop screens. Fable access is gone; the execution routing is restated in Ledger D10, the step-by-step detail lives in the Pack Guides. | **Archive** → `99_Archive/2026-07_Cowork_Era/` (rationale for the F1–F4 ranking is kept; nothing actionable is lost). Two cross-references to patch. |
| `2026-07_Pack_Implementation_Guides.md` (53 KB) | Executable guides G1–G4 for the unapplied F1–F4; written 2026-07-07 against the live code, so symbols must be re-verified before use. Its G0 rules 2, 9 and the "re-run extractor" line (≈373) name `06_Index`, the extractor and `Abracodebra_Codebase_Map.md`, all of which this pack retires. | **Stays in Active.** Patch those stale lines in place with a dated note (§7.4 Q13). |
| `2026-07_Testing_Sandbox.md` (11 KB) | A **proposal**; no sandbox/cheat code exists on disk. Part of its "already exists" table is stale (it says `ExecutionPhaseDriver` is not wired; it was wired into `SampleScene` on 2026-07-06). It overlaps with the audit's C3 (golden-run harness) and §8's asmdef split. | **Stays in Active** as an undecided proposal; its idea goes into `Roadmap.md` next to the asmdef split. Milan decides (§7.4 Q3). |
| `Abracodabra_A_Category_Implementation.md` (61 KB) | Parts 1–2 are applied and were verified: `ExecutionPhaseDriver.cs` exists, `RequestActionTicks` is in `TickManager`, `RunSeed` in `RunManager`, `OnPlantDied` in `PlantGrowth`. Editor wiring landed 2026-07-06; projectmemory records that Milan confirmed auto-tick, Space, Tab and the run seed in play. Part 4's checklist still has **10 unticked boxes** (A1 ×4, A2 ×2, A3, A4, A5, A6); only the A1 auto-tick/speed/pause checks and the seed were informally confirmed. | **→ `Done/`**, with a closing note at the top naming the Part-4 checks that were never run; carry those into `Roadmap.md` as "verify" items. |
| `2026-10_Modernization.md`, `2026-10_Modernization_Prompts.md` | This pack and its guide. | **Stay**; the pack goes to `Done/` after Phase E (the guide with it). |

Did not fit the layout (decisions in §7.4 Q8–Q10): the seven undated files in `02_Design/Concepts/` (§4.1 wants `Name_YYYY-MM-DD.md`); `04_Reviews/Abracodabra_Foundation_Review_2026-06.md` (§4.1 wants `YYYY-MM_Name.md`); `05_Reference/07_Third_Party_Package_Guide_DualGrid.md` has a numbered prefix (harmless; leave); what goes in `90_SideIdeas/`.

### 7.4 Needs Milan

**Answered 2026-10-05: "recs" (every recommendation accepted, Q1–Q17).** Done since: Q2, local branch `charming-elgamal` deleted with `git branch -d`. Still Milan's: Q1 (push `main` and the tag), Q11, Q12 (and Q16, a Bitbloom-side commit). Everything else is executed by Phase B + C (§7.5).

1. **Push (blocked).** The permission classifier refused `git push origin main` and the `pre-modernization` tag push. **Rec:** run them yourself in the terminal: `! git push origin main` then `! git push origin pre-modernization` (the tag stays on baseline commit `3a0abe0`, before this §7 commit). Check github.com → Tags afterwards.
2. **`charming-elgamal`** is fully merged into main (§7.1). **Rec:** delete the local label (`git branch -d charming-elgamal`; safe, it refuses if anything were unmerged). No merge or cherry-pick is needed.
3. **Task triage (§7.3).** Ledger, Pack Guides, Testing Sandbox stay Active; A-Category → `Done/` with a "Part 4 never fully run" note; Last-Day Plan → archive. **Rec:** accept all five. For the Sandbox, if you want it dropped instead, say so and it goes to the archive with the Roadmap note kept.
4. **Font scope (§7.2).** **Rec:** move 492 files (176 unused Extracted-Font families + all of `Examples & Extras`, 1.14 GB); keep Handjet ×2, Liberation, Noto, SEGUIEMJ, and the 33 Kenney / m3x6 / m5x7 / m6x11 files (15.9 MB).
5. **`SEGUIEMJ.TTF` is used** by `StatusEffect_Icon.prefab` and is not redistributable. **Rec:** keep now; add a Roadmap item "replace with Noto (OFL) before any public build / Steam page".
6. **`TileMappingsBackup`:** code-referenced backup, not an orphan. **Rec:** keep where it is.
7. **Vault path** `D:\Unity Projects\AbraCodebra\_Vault\Abracodebra_TMP_fonts\` (outside every repo, keeps the `Assets/TextMesh Pro/...` sub-paths so files can be restored). **Rec:** yes.
8. **Dated names for point-in-time docs (§4.4 rule 2).** **Rec:** `git mv` the 7 Concepts files to `Name_YYYY-MM-DD.md` (date = the doc's own header date, else its first git commit) and `Abracodabra_Foundation_Review_2026-06.md` → `2026-06_Foundation_Review.md`; fix every reference in one commit.
9. **`02_Design/WeGo/README.md`** must say which rework is current and what the 2026-07 Phase-Identity doc superseded. I have not read all six reworks. **Rec:** I read rework 5 and `Phase_Identity_Final_Refinement.md` in step C3 and write it; I list anything I'm unsure of in the README as "verify". (Assumption: rework 5 is current and "5 - Copy" is an older/diverged draft.)
10. **`90_SideIdeas/`:** **Rec:** README only ("parking lot, not the design of record"), nothing moved in. `Gene_Catalog_Experimental_Annex.md` stays in `Concepts/`.
11. **Sibling `Abracodebra 1.0 - Backup 08.07.2026`** (a full project copy incl. `Library/`, `Temp/`, the extractor files). **Rec:** after the push is on GitHub, zip it without `Library/`, `Temp/`, `obj/` to cold storage and delete the folder. Your call; I touch nothing outside this repo.
12. **Old setup (yours to do):** turn off the weekly "KB doctor" scheduled task in the old Cowork desktop (it checks `06_Index`, which goes away), and stop using the old claude.ai Project after step 6 of the guide. **Rec:** yes.
13. **Stale lines in `2026-07_Pack_Implementation_Guides.md`** (G0 rules 2 and 9, ≈ line 373: `06_Index`, extractor, old map name). **Rec:** patch just those lines in place with a dated "2026-10: tooling retired" note; it is an active pack, not a closed point-in-time doc.
14. **Planning-tick invariant in CLAUDE.md §1.** The pack wants it as an invariant; the Ledger says it is not yet enforced in code. **Rec:** write it as "target invariant, **not yet enforced**; re-verify before touching `TickManager` / `PlantGrowth`".
15. **`.vscode/` and `.idea/`** are untracked and ignored (Bitbloom does the same). **Rec:** yes; the files stay on your disk.
16. **Bitbloom's `CLAUDE.md` §8 still says "Pro plan"** (confirmed). Not touched (read-only rule). **Rec:** fix in a Bitbloom code-lane session as its own commit.
17. **`.claude/settings.json` rule syntax.** Permission-rule strings for Bash/PowerShell calls to `unity.exe` are easy to get wrong. **Rec:** I check the syntax against the current Claude Code docs in step C10 and test one allowed and one denied call before committing.

### 7.5 Step plan for Phase B + C

Before step 1: Milan answers §7.4, pushes (Q1), closes the Unity Editor, runs `/clear`, model opusplan · high. Commit trailer on every commit: `Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>`. Use `git mv` for tracked files; verify each write by reading it back. Anything unexpected goes to §7.4 and the plan continues.

**Phase B (Editor closed)**

| # | Files | Action | Commit message |
| --- | --- | --- | --- |
| B1 | `.gitignore`, `.gitattributes` | Merge Bitbloom's extras into `.gitignore`: `.vscode/`, `.idea/`, `.DS_Store`, `*.slnx`, `.claude/settings.local.json`, `/[Pp]rofilerCaptures/`, `UIElementsSchema/`, `Unity_EXTRACTED_*.txt`, the Burst debug line. `.gitattributes` = Bitbloom's (`* text=auto`, `*.sh text eol=lf`). Ignore rules first so nothing reappears as untracked. | `chore: align .gitignore and .gitattributes with Bitbloom` |
| B2 | `.idea/`, `.vscode/`, `UIElementsSchema/` (28 `.xsd`), root `Unity_EXTRACTED_scripts.txt`, `Unity_EXTRACTED_ToolkitUI.txt` | `git rm -r --cached` the first three (files stay on disk); `git rm` the two root extracts (generated, deleted). `06_Index/` copies wait for C11. | `chore: untrack IDE config and generated schema, delete root code extracts` |
| B3 | `unity_extractor.py`, `unity_extractor_RUN.bat`, `unity_extractor_settings.json`, `.clinerules/` | `git mv` the extractor trio → `ClaudeProjectFiles/99_Archive/2026-07_Cowork_Era/tools/`; `.clinerules/` → `99_Archive/2025_Cline/`. | `chore: archive the Unity extractor and Cline rules` |
| B4 | `Claude outputs/` (3 files) | `git mv` → `ClaudeProjectFiles/99_Archive/2026-09_Bitbloom_Bootstrap/`. | `chore: archive Bitbloom bootstrap drafts` |
| B5 | 492 font files + `.meta` under `Assets/TextMesh Pro/Fonts/Extracted Fonts/` and `Examples & Extras/` (list = §7.2) | Re-run the GUID scan; **abort and report if any row differs from §7.2**. Move (PowerShell `Move-Item`, sub-paths preserved) to `D:\Unity Projects\AbraCodebra\_Vault\Abracodebra_TMP_fonts\`; the `Extracted Fonts` folder and its `.meta` stay (Handjet is in it); `Examples & Extras` goes whole with its `.meta`. Then `git add -A`; confirm the 9 kept files and their `.meta` still exist and `du` of `Assets/TextMesh Pro` is ~60 MB. | `chore: move unused TMP font assets out of the project (−1.14 GB)` |
| B6 | `Packages/manifest.json` | Remove `com.unity.visualscripting`, `com.unity.multiplayer.center`, `com.unity.collab-proxy`. Leave `packages-lock.json` to Unity. | `chore: remove unused packages (visualscripting, multiplayer.center, collab-proxy)` |

**Phase C (docs only; Editor stays closed)**

| # | Files | Action | Commit message |
| --- | --- | --- | --- |
| C1 | `03_Tasks/Active/Abracodabra_A_Category_Implementation.md`, `…/2026-07_Fable5_Last_Day_Plan.md` | A-Category → `03_Tasks/Done/` with a closing note listing the unrun Part-4 checks; Last-Day Plan → `99_Archive/2026-07_Cowork_Era/`; patch the two cross-references (Ledger, Guides). | `docs: close A-category pack, archive Last-Day plan` |
| C2 | `01_Core/Abracodebra_Codebase_Map.md`, `02_Design/gene_systems_deep_dive_v6.md`, 7 files in `02_Design/Concepts/`, `04_Reviews/Abracodabra_Foundation_Review_2026-06.md` | `git mv` → `01_Core/Codebase_Map.md`, `02_Design/Gene_Systems_Deep_Dive.md` (first line: "v6, 2025"), dated Concepts names, `04_Reviews/2026-06_Foundation_Review.md`. `grep -r` every old name over `ClaudeProjectFiles/` and `CLAUDE.md` and fix the references (project memory and CLAUDE.md are rewritten later; they get the new names then). | `docs: fixed names for living docs, dated names for point-in-time docs` |
| C3 | `02_Design/WeGo/wego-system-rework.md` … `rework5.md`, `rework5 - Copy.md`; new `02_Design/WeGo/README.md` | `git mv` the six → `99_Archive/2025-06_WeGo_Rework/` ("5 - Copy" kept). README (≤10 lines): which rework is current, what the 2026-07 Phase-Identity doc superseded (read both first; mark unsure points "verify"). | `docs: archive WeGo rework 1-5, add current-doc README` |
| C4 | `03_Tasks/Roadmaps/Code_Optimization_Backlog.md` → new `03_Tasks/Roadmap.md` | Merge the backlog into `Roadmap.md`, stale items flagged "verify before acting". Put near the top: **asmdef split + golden-run harness** (audit C3; Block 1 or 2 candidate), then the Ledger A/B experiment, F1–F4 (Guides), Testing Sandbox, the A-pack Part-4 unrun checks, **SEGUIEMJ → Noto**, "Planning-tick invariant not enforced". Old file → `99_Archive/2026-07_Cowork_Era/`; remove the empty `Roadmaps/`. | `docs: Roadmap.md from the merged backlog, with the audit's candidates` |
| C5 | `01_Core/projectmemory.md` → `99_Archive/projectmemory_history.md` | Create the archive file as a **verbatim** copy (byte-compare after) with a one-line header "projectmemory as of 2026-10-05 (pre-modernization)". `projectmemory.md` itself is not touched yet. | `docs: archive projectmemory verbatim before the rewrite` |
| C6 | `01_Core/projectmemory.md` (reads `Chat_History_Digest.md`) | Rewrite in Bitbloom's format: header explaining the file; **Snapshot** ≤ 40 lines; Recent entries; **Decision log** (date · decision · where; includes the 2026-10-05 row "Abracodabra stays a live project, developed alongside Bitbloom; modernized to the Bitbloom workflow (this pack)", and one row per Ledger D1–D10); Code facts; Learnings. Carry the digest's still-true facts. Every "implemented" claim checked with grep/Read first. ≤ 30 KB. | `docs: projectmemory in Snapshot / Decision-log format` |
| C7 | new `02_Design/Feedback_Log.md` | Seed with Milan's cross-project rules from Bitbloom's "Distilled rules" that are about him, not Bitbloom (rules 7, 9, 10, 21, 30, 36, 41, 42, 43, 44), each with source and date, plus Abracodabra's own: manual avatar piloting felt finicky and hectic (out of the design space), WeGo felt cortisol-heavy (find the wording and dates in projectmemory / Ledger / Concepts before writing). | `docs: Feedback_Log.md seeded with cross-project and Abracodabra rules` |
| C8 | new `90_SideIdeas/README.md` | What's here, "parking lot, not the design of record" (§7.4 Q10). | `docs: 90_SideIdeas README` |
| C9 | `CLAUDE.md` | Rewrite per §1.1 (Intro, §0–§8), the routing table from `00_START_HERE.md` as §4, the §4.4 versioning rules, the Planning-tick invariant as "target, not yet enforced" (Q14), §8 with Max 5x. §7 gets the Unity CLI loop and a "gotchas arrive in Phase D" line. Keep ≤ ~12 KB (`wc -c`; currently 11 053 B). | `docs: CLAUDE.md rewritten in the Bitbloom shape` |
| C10 | new `.claude/settings.json` | Allow git read/commit, the `unity command` calls of §4.3, `unity status`; deny `eval*`, `eval_file*`, `run_script*`. Check rule syntax against the current docs and test one allowed + one denied call (Q17). | `chore: .claude/settings.json permissions` |
| C11 | `00_START_HERE.md`, `01_Core/project_instructions.md`, `01_Core/Chat_History_Digest.md`, `06_Index/` | Last, once CLAUDE.md carries the routing table and projectmemory the digest's facts: `git mv` the three docs → `99_Archive/2026-07_Cowork_Era/`; `git rm -r ClaudeProjectFiles/06_Index`. | `docs: retire the old Cowork-era routing, instructions and index` |
| C12 | `03_Tasks/Active/2026-07_Pack_Implementation_Guides.md`, `2026-07_Fable5_Design_Decision_Ledger.md` | Patch the stale tooling lines (Q13) with a dated note; ledger line 111 "extractor re-run between". | `docs: refresh retired-tooling references in the pack guides` |
| C13 | whole `ClaudeProjectFiles/`, pack §7 | `grep -r` for dead references (`06_Index`, `extractor`, `00_START_HERE`, `Abracodebra_Codebase_Map`, `gene_systems_deep_dive_v6`, `project_instructions`, `Chat_History_Digest`) and fix; list the tree and check each file has a home per §4.1; `wc -c` CLAUDE.md (≤ ~12 KB) and projectmemory (≤ 30 KB); update this §7 with counts, MB removed and the sizes; tag `kb-2026-10`; push main + tag (Milan if still blocked). | `docs: kb-2026-10 consistency pass` |

After C13, Milan's check (the Editor opens for the first time): Unity Hub → open the project (still 6000.0.39f1) → let it resolve the package removal → Console has no red → Play SampleScene 10 s → close. Then Claude commits whatever Unity changed (expect `Packages/packages-lock.json`, possibly new `.meta` files; review the diff first, no scene or prefab changes expected): `chore: packages-lock after package removal`. Test question after `/clear`: "What is this project, what's built, what's next?" must match the Snapshot.

### 7.6 Execution results, Phase B + C (session 2, 2026-10-05, Sonnet · high)

Every step is its own commit on `main` (22 commits from the baseline `3a0abe0` to the `kb-2026-10` tag; `git log --oneline pre-modernization..kb-2026-10`).

| Step | Commit | Result |
| --- | --- | --- |
| B1 | `bce2e5a` | `.gitignore` (CRLF kept) gained IDE, `*.slnx`, ProfilerCaptures, Burst, `UIElementsSchema/`, `Unity_EXTRACTED_*.txt`, `.claude/settings.local.json`; `.gitattributes` = Bitbloom's |
| B2 | `03310a4` | 36 files untracked or deleted: `.idea` 4, `.vscode` 3, `UIElementsSchema` **27** (the pack said 28), 2 root extracts (−32 831 lines). The first three stay on disk, ignored |
| B3 / B4 | `7c756aa`, `d7d391f` | extractor trio + `.clinerules` file + 3 Bitbloom bootstrap drafts moved with `git mv`; the two empty folders removed |
| B5 | `262026f` | Re-ran the GUID scan before moving: **identical to §7.2** (492 files, 0 references from outside the set, 9 kept files and their `.meta` intact). 997 entries moved (492 files + 492 `.meta` + 13 folder `.meta`) to `_Vault\Abracodebra_TMP_fonts\Assets\TextMesh Pro\`, sub-paths preserved. **1 088.8 MiB (= 1 141.5 MB) removed; `Assets/TextMesh Pro` 59 MB; `Assets/` 123 MB in total; tracked files 4 415 → 3 385; `.git` stays 145 MB** |
| B6 | `829e2ac` | three packages removed from `manifest.json` (valid JSON, 40 dependencies left); `packages-lock.json` untouched, Unity rewrites it on first open |
| C1 | `2db6703` | A-Category → `Done/` with the Part-4 closing note; Last-Day Plan → archive; 3 cross-references patched |
| C2 | `4c39a55` | 10 `git mv` renames (map, deep dive + "v6, 2025" line, 7 Concepts files dated from their own header, Foundation Review); references fixed in 12 files outside the archive and the files rewritten later |
| C3 | `bc8af59` | 6 WeGo files → `99_Archive/2025-06_WeGo_Rework/`; `WeGo/README.md` written (see §7.4 N2) |
| C4 | `b13ee08` | `Roadmap.md` (8 candidates, the backlog's 13 items with a status each); old backlog archived; `Roadmaps/` removed |
| C5 | `1fc8b2e` | `99_Archive/projectmemory_history.md` = old file byte-for-byte after a 4-line header (byte-compared) |
| C6 | `28d4d92` | projectmemory rewritten: **25 971 B → 15 408 B**; Snapshot 16 lines; code claims re-checked by grep (see below) |
| C7 / C8 | `0735c72`, `ca47792` | `Feedback_Log.md` (18 rules, 4 open shortcomings), `90_SideIdeas/README.md` |
| C9 | `59bbcbe` | CLAUDE.md rewritten: **11 029 B → 12 430 B** (target ≤ ~12 KB: 0.4 KB over; the Unity CLI gotchas and D1–D10 account for it) |
| C10 | `chore: .claude/settings.json permissions` | 26 allow rules (git read/add/mv/commit for Bash and PowerShell; `unity.exe status`, `command console`, `recompile*`, `run_tests`, `list_tests`, `editor_play`, `editor_stop`, `capture_game_view`) and 6 deny rules (`eval*`, `eval_file*`, `run_script*` for PowerShell and Bash). Syntax checked with the docs via a subagent (`Tool(pattern *)`, deny > ask > allow). **Tests:** `unity.exe command eval "1+1"` was **denied** (rule works); `unity.exe status` ran without a prompt, but this session's mode may allow it anyway, so the allow side is not independently proven. `git push` and `git tag` are deliberately not allowed |
| C11 | `c74044f` | `00_START_HERE`, `project_instructions`, `Chat_History_Digest` → `99_Archive/2026-07_Cowork_Era/`; `06_Index/` removed (the extracts are generated) |
| C12 | `8c6deba`, `8f544bc` | stale tooling lines patched in the Pack Guides (G0 rules 2, 9, closing duty 4, a dated note), the Ledger (extractor line, D1 "sequential, not a toggle" revision), and the Codebase Map (index, backlog path, next-action anchor) |
| C13 | (this commit) | dead-reference grep over `ClaudeProjectFiles/` and `CLAUDE.md`: the remaining hits are history (this pack, the archive, point-in-time docs, projectmemory's own migration notes) |

**Code claims checked on disk before they went into projectmemory** (all confirmed 2026-10-05): 200 `.cs` under `Assets/Scripts` (236 in `Assets/`); `ExecutionPhaseDriver`, `TickManager.RequestActionTicks` / `ActionsDriveTicks`, `RunManager.RunSeed` / `randomizeSeedOnStart` / `playerDeathEnabled` / `GameOver`, `PlantGrowth.OnPlantDied`, `WaveManager.IsWaveTimerComplete`, `InventoryService.OnInventoryReady`, `InitializationManager`, `GeneServices` default seed 0; `RuntimeSequenceSlot` live in 4 files; **no** `PlayerInventory`, `SequenceParser`, `DorisMoodSystem`, `ComboDiscoverySystem`, `GeneDraftSystem`, `DorisDigestionSystem`, `RoundStatsTracker`, `AnyPlantDied`; no `RunState` check in `PlantGrowth` / `PlantSequenceExecutor` (Planning-tick invariant not enforced); build has only `SampleScene`; 36 `Random.*` and 36 legacy `Input.*` call sites; Input System 1.13 installed, `activeInputHandler` = 2 (both).

### 7.4a New items from session 2 (added to §7.4; the rest of the plan continued)

- **N1. "WeGo felt cortisol-heavy" has no source on disk.** `grep -ri cortisol` finds it only in this pack. `Feedback_Log.md` rule 2 records it as "reported by Milan 2026-10-05" with the nearest written trace (Phase Identity §4: B-rev is the "calmer, more ADHD-friendly" game). **Milan: send the original wording and date if you remember them, or confirm the rule as written.**
- **N2. WeGo reworks are not a design doc.** Rework 5 is a June-2025 code-foundation list (animal realtime cleanup, grid radius, SO migration, file splits…), and "5 - Copy" is a truncated draft of it (no Priorities 4, 5, 7). The 2026-07 Phase Identity doc does **not** supersede them (it settles Commit & Watch vs Tick Ledger). The README says so and marks the unchecked items "verify". Checked: `GetTilesInRadius` exists, no `HandleRealtimeUpdate` is left.
- **N3. Pack figures corrected:** `UIElementsSchema/` held 27 tracked files, not 28; font bytes removed are 1 141.5 MB decimal (1 088.8 MiB), the same set as §7.2.
- **N4. CLAUDE.md is 12 430 B**, slightly over the ~12 KB target. Cut D1–D10 further or move the CLI gotchas to a doc once Phase D lands them; your call.
- **N5. Push.** `main` was pushed by Milan before this session (it showed no unpushed commits at the start); tags were not on GitHub. Pushes of this session's commits and tags: see the report at the end of the run.
- **N6. Unity Editor state.** The only Unity process running during this session was **Bitbloom's** Editor (6000.3.24f1); Abracodabra's Editor was closed, so the font move and the manifest edit were safe.
- **N7. Assumed, not verified:** the A-Category closing note repeats the pack's §7.3 claim that Milan confirmed auto-tick, Space, Tab and the run seed in play (from the old projectmemory, 2026-07-06). I did not re-run any play check (the Editor is closed).

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
