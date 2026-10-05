# Abracodabra restart — step by step (with prompts)

**Goal:** get Abracodabra back to "I open it and know what to build next" in about a week, then return to building the game. It runs alongside Bitbloom; it doesn't replace it.
**Hard cap:** modernization (steps 1–5) takes **one week at most**. If it spills into a second week, it has become a way to avoid the game: stop, and jump to step 6.
**The pack all prompts point to:** `ClaudeProjectFiles/03_Tasks/Active/2026-10_Modernization.md`.

**Where things run**

| Lane | Where | How to start |
| --- | --- | --- |
| Code | **Claude Code** in Windows Terminal (PowerShell) | `cd "D:\Unity Projects\AbraCodebra\Abracodebra 1.0"` → `claude` → `/model` (pick the model and effort the step names) → paste the prompt. `/clear` before every new prompt. |
| Design | **Cowork**, in a new project "Abracodabra" (step 2) | New chat in that project → paste the prompt |
| You | GitHub Desktop, Unity Hub, Unity Editor | As the step says |

**Calendar (week of Mon 5 Oct; usage resets Thu 04:00).** This week Bitbloom needs your playtime (The Hollow), not the code lane, so the code lane is free.

| When | Step | Who | Model · Effort · Time · Usage |
| --- | --- | --- | --- |
| Mon/Tue | 1 Check git · 2 Cowork project | You | — · 15 min |
| Tue | 3 Kickoff + audit | Claude Code | opusplan · high · 30–45 min · medium |
| Tue/Wed | 4 Cleanup + docs | Claude Code | opusplan · high · 60–90 min · medium–high |
| Wed/Thu | 5 Unity CLI + upgrade to 6000.3.24f1 | You + Claude Code | opusplan · high · 60–120 min · medium |
| Thu, after 04:00 reset | 6 Design re-entry (what to build first) | Cowork | Fable (or Opus · high) · 60–90 min · high, on the Fable limit |
| Fri → | 7 First code block | Claude Code | as the block's pack marks each batch |
| End of block 1 | 8 Playtest | You | — · 15 min |

---

## Step 1 — Check git (you, 5 min, now)

1. [ ] GitHub Desktop → Current repository → **Abracodebra-1.0**. You'll see uncommitted changes (projectmemory, six July docs, `Claude outputs/`, the two new pack files). **Don't commit them yourself**: prompt 1 commits them as the baseline.
2. [ ] Don't open the Unity Editor for this project until step 4 says so.

**Done when:** Desktop shows the repo and the changes, nothing committed yet.

---

## Step 2 — New Cowork project (you, 10 min)

1. [ ] Claude desktop app → Projects → New project **"Abracodabra"**.
2. [ ] Connect folders: `D:\Unity Projects\AbraCodebra\Abracodebra 1.0` (the project) and `D:\Unity Projects\AbraCodebra\Bitbloom` (read-only reference for step 6).
3. [ ] Project instructions, paste:

```
Abracodabra: Milan's solo Unity 6 game (cozy-dark garden roguelite; you program plants like spells; feed Doris). Developed alongside Bitbloom, his pixel-sim experiment.
Source of truth: the repo "D:\Unity Projects\AbraCodebra\Abracodebra 1.0" (GitHub camelCaser69/Abracodebra-1.0). Read CLAUDE.md, then ClaudeProjectFiles/01_Core/projectmemory.md, before real work. Never copy repo docs into project knowledge; this project holds one pointer doc only (claude/abracodabra_status.md).
This is the design lane: design passes, task packs, playtest analysis as .md in ClaudeProjectFiles/ per CLAUDE.md §4. Code is done in Claude Code.
Bitbloom (D:\…\Bitbloom\Bitbloom 0.1a\Bitbloom) is read-only reference: its Feedback_Log "Distilled rules" are Milan's taste and apply here too.
How Milan works: a Model · Effort · Est. time · Est. usage line on every prompt; options with a scored recommendation (he answers "recs"); never red/green alone; short playtest sheets in play order; checklists + a separate "things to consider" + one Next action.
```

4. [ ] Leave the old claude.ai project alone for now. In step 6 you'll check it once for anything missing from the repo, then stop using it.

**Done when:** the project exists with both folders connected.

---

## Step 3 — Kickoff: baseline + audit (Claude Code)

**Model · Effort · Est. time · Est. usage:** opusplan · high · 30–45 min · medium

```
Read ClaudeProjectFiles/03_Tasks/Active/2026-10_Modernization.md in full. It is the task pack for this session and the next two. Then skim CLAUDE.md and ClaudeProjectFiles/01_Core/projectmemory.md (the OLD workflow you are replacing).

Reference implementation (read only, never write): Bitbloom at "D:\Unity Projects\AbraCodebra\Bitbloom\Bitbloom 0.1a\Bitbloom\" — its CLAUDE.md, .gitignore, .gitattributes, ClaudeProjectFiles/01_Core/projectmemory.md (format) and ClaudeProjectFiles/02_Design/Feedback_Log.md ("Distilled rules").

This session:
1. Phase A in full (pack §2). Stop before merging or deleting charming-elgamal; summarise it in §7.1.
2. Re-verify the audit snapshot (pack §0) on disk; correct any row that is wrong.
3. Phase B §3.2 GUID check only (no moves): fill the font table in §7.2.
4. Phase C §4.2 triage of 03_Tasks/Active only (no moves): verdicts in §7.3.
5. Plan Phase B + C as a numbered step list at the end of §7 (each step: files, action, commit message). Every open question goes in §7.4 with your recommendation.

Rules: don't move, delete or rewrite anything in this session except what Phase A says. Commit the pack's §7 updates at the end. Finish with a 5-line summary and the list from §7.4. I answer "recs" to accept all your recommendations.
```

**Your part afterwards:** read §7.4 (in the pack, or Claude's summary). Answer "recs", or note the exceptions. For `charming-elgamal`, if Claude can't tell what the commit is worth, choose "keep the branch, don't merge"; deleting it can wait.

**Done when:** GitHub Desktop shows the baseline commit pushed and the tag `pre-modernization` on github.com.

---

## Step 4 — Cleanup + knowledge base (Claude Code)

**Model · Effort · Est. time · Est. usage:** opusplan · high · 60–90 min · medium–high
**Before pasting:** the Unity Editor for this project stays **closed** (fonts move out of Assets).

```
Continue the task pack ClaudeProjectFiles/03_Tasks/Active/2026-10_Modernization.md: execute Phase B (§3) and Phase C (§4) using the step plan at the end of §7. My answers to §7.4: <recs / your answers>.

How to work:
- One commit per step, message as planned. Use git mv for tracked files so history follows them.
- Anything inside Assets/ moves only with its .meta and only if §7.2 shows no reference. The Editor is closed.
- Never delete design content; archive it under 99_Archive/<YYYY-MM_Era>/. Generated files (extracts, 06_Index) may be deleted.
- Before rewriting projectmemory.md, copy the old file verbatim to 99_Archive/projectmemory_history.md (append). Every "implemented" claim in the new Snapshot must be checked against the code on disk.
- Decision log: add a row dated today: "Abracodabra stays a live project, developed alongside Bitbloom; modernized to the Bitbloom workflow (this pack)."
- Write the new CLAUDE.md per pack §1.1 and the versioning rules in §4.4. Keep it ≤ ~12 KB. Write .claude/settings.json per §4.3.
- Verify every file you write by reading it back with the file tools.
- Update §7 as you go; anything unexpected goes in §7.4 and you continue with the rest.

At the end: tag kb-2026-10 and push. Report: what moved (counts), MB removed from Assets, CLAUDE.md and projectmemory sizes, and what's in §7.4. Then tell me what to check: open the project in Unity Hub and confirm the console has no red errors.
```

**Your check (5 min):** open the project in Unity Hub (still 6000.0.39f1). Window → General → Console: no red. Press Play in SampleScene for 10 s. Close the Editor.
**Test question:** run `/clear`, then ask Claude Code *"What is this project, what's built, what's next?"*. The answer must match the new Snapshot. If it doesn't, tell it what's wrong and let it fix projectmemory.

---

## Step 5 — Unity CLI, then upgrade to 6000.3.24f1 (you + Claude Code)

**Why now:** after the cleanup (no 1 GB of fonts to reimport), before any new code. Never upgrade in the middle of a code block. Support for 6.0 LTS ends this month; Bitbloom already runs 6000.3.24f1.
**Before you start:** close Bitbloom's Editor (the reimport needs RAM) and commit anything open in Bitbloom.

### 5a — Hook up the CLI on the old version (you, 10 min)

1. [ ] Open Abracodebra in Unity Hub (6000.0.39f1). Wait until it's idle.
2. [ ] PowerShell, in the project folder:
   - `& "$env:LOCALAPPDATA\Unity\bin\unity.exe" pipeline install`: wait for the Editor to recompile. (Running `status` before this says "No Unity Editor instances found with the Pipeline package installed"; that's expected.) If it asks you to log in, run `unity.exe auth login` first. If it still fails, use Package Manager → + → Add package by name → `com.unity.pipeline` (version `0.7.0-exp.1`, as in Bitbloom).
   - `& "$env:LOCALAPPDATA\Unity\bin\unity.exe" status`: now shows this project.
   - `& "$env:LOCALAPPDATA\Unity\bin\unity.exe" command console --level error`: answers.
3. [ ] Leave the Editor open and paste prompt 5 (Claude Code):

**Model · Effort · Est. time · Est. usage:** opusplan · high · 60–120 min (most of it is reimport and your smoke test) · medium

```
Continue ClaudeProjectFiles/03_Tasks/Active/2026-10_Modernization.md: Phase D (§5), then Phase E (§6). CLAUDE.md §7 has the Unity CLI rules; follow them (PowerShell, "$env:LOCALAPPDATA\Unity\bin\unity.exe", --project-path if AMBIGUOUS_EDITOR, never run eval/eval_file/run_script without asking).

Phase D: record the 6.0 baseline (console errors/warnings + capture_game_view of SampleScene after 10 s of play) into ClaudeProjectFiles/04_Reviews/2026-10_Baseline_6.0/, then make sure CLAUDE.md §7 carries every CLI gotcha in pack §5. Commit and push.

Phase E: tag pre-unity-6.3 on main and push it, then tell me exactly when to switch the Editor version in Unity Hub (I'll create the branch upgrade/unity-6.3 in GitHub Desktop and do the Hub step). Wait for me to say "reimported". Then: recompile → console errors → fix in groups, one commit per group naming the error → bump overlapping packages to Bitbloom's versions → console clean → after-capture → write 04_Reviews/2026-10_Unity63_Upgrade.md (before/after, every error and fix). Give me the smoke test from pack §6 step 6 as a short copy-paste checklist in play order. After I confirm, merge to main, push, update projectmemory (Snapshot + a Decision log row) and move the pack to 03_Tasks/Done/.
```

### 5b — Move to the new version (you, when Claude says so)

1. [ ] GitHub Desktop → Branch → **New branch** `upgrade/unity-6.3` (from main) → Publish branch.
2. [ ] Close the Unity Editor.
3. [ ] Unity Hub → Projects → in the Abracodebra 1.0 row, click the **Editor version** (6000.0.39f1) → choose **6000.3.24f1** → **Change version**. If Hub says the version isn't installed: Installs → Install Editor → 6000.3.24f1 (Bitbloom's is already there).
4. [ ] Hub opens the project. Confirm the "Opening project in a different Editor version" dialog → **Continue**. If the API Updater asks → **"I Made a Backup. Go Ahead!"** (git is the backup).
5. [ ] Wait for the reimport (several minutes). Ignore the first console flood.
6. [ ] Tell Claude **"reimported"**. It fixes errors over the CLI and then gives you the smoke test.
7. [ ] Run the smoke test (~5 min). All yes → tell Claude to merge. Something broke → tell Claude what you saw. Nothing goes to main until the test passes.

**If it goes badly wrong:** GitHub Desktop → switch to `main` → Hub → set the version back to 6000.0.39f1. Nothing is lost; the branch keeps the attempt.

**Done when:** `ProjectSettings/ProjectVersion.txt` on main says `6000.3.24f1`, the console is clean, and the smoke test passes.

---

## Step 6 — Design re-entry: what to build first (Cowork, the new Abracodabra project)

The July work ended with a clear next action that was never started: the A proof-of-concept (`Phase_Identity_Final_Refinement.md` §2.4). Since then, eight weeks of Bitbloom playtests have answered part of its open question (D1: does a watch-heavy day hold attention?). This pass decides the first block **with those answers**, before any code.

**Model · Effort · Est. time · Est. usage:** Fable (or Opus · high if Fable is spent) · 60–90 min · high
**When:** after the Thursday 04:00 reset, or on leftover Fable usage before it.

```
Design re-entry for Abracodabra. Read, in this order:
1. CLAUDE.md and ClaudeProjectFiles/01_Core/projectmemory.md (this repo).
2. ClaudeProjectFiles/03_Tasks/Active/2026-07_Fable5_Design_Decision_Ledger.md (or its new location) and 02_Design/Concepts/Phase_Identity_Final_Refinement.md (§2.4 is the planned first build).
3. From Bitbloom (read only; D:\Unity Projects\AbraCodebra\Bitbloom\Bitbloom 0.1a\Bitbloom\ClaudeProjectFiles\): 02_Design/Feedback_Log.md (Distilled rules + the 2026-10 log entries), 02_Design/Truths_2026-10-04.md, 02_Design/Abracodabra_Pitch_2026-10-05.md, 02_Design/Trap_v2_2026-10-05.md. These live on Bitbloom's branch ideation/merge; if a file is missing on disk, read it with: git -C "<Bitbloom path>" show ideation/merge:ClaudeProjectFiles/02_Design/<file>. Never switch Bitbloom's branch.

Task:
A. What did Bitbloom's playtests teach that bears on Abracodabra's open decisions (D1 phase identity and the under-stimulation risk, D5, D8, Mark & Go, the hectic-feel guards)? Be adversarial: name every planned Abracodabra choice that Bitbloom's evidence now argues against, and every one it supports.
B. Re-validate or revise the first build (§2.4 items 1–7). Give 2–3 options for "Block 1", scored (agency, readability, tension, build cost, reuse of existing code), with one recommendation. Block 1 must end in something Milan can play within ~2 weeks of code-lane time.
C. Check once whether anything in the old claude.ai Abracodabra project is missing from the repo: list what I should look for; I'll paste anything you need.

Write: 02_Design/Restart_Direction_2026-10-DD.md (findings, options, recommendation, things to consider, Next action) and, after I answer "recs", 03_Tasks/Active/2026-10_Block1_<Name>.md (batches, each with Model · Effort · Est. time · Est. usage, Done when + How to check, a cut list, a short playtest sheet in play order). Add the decisions to projectmemory's Decision log. Also create the project doc claude/abracodabra_status.md (≤ 15 lines: where things stand, what to read first, Next action).
```

**Done when:** the Block 1 pack exists and you know what you'll play at its end.

---

## Step 7 — First code block (Claude Code, from Friday)

Per batch, in a fresh session (`/clear`), with the model the pack marks:

```
Read CLAUDE.md, then ClaudeProjectFiles/01_Core/projectmemory.md, then ClaudeProjectFiles/03_Tasks/Active/2026-10_Block1_<Name>.md. Do batch <N> only, exactly as specced. Unity CLI loop after each step (recompile → console errors); commit per step; update projectmemory as you go. End with Done when + How to check for me.
```

**Cadence alongside Bitbloom:** the weekly limit is shared, so give each project a lane per day rather than mixing within a session. A simple default: Abracodabra's code lane gets one batch a day while Bitbloom is in a play-and-decide phase; when Bitbloom has a code block running, Abracodabra gets its design lane (Cowork/Fable) and playtests instead.

---

## Step 8 — Playtest and loop

1. [ ] Play with Block 1's sheet (short, in play order).
2. [ ] Paste the answers into a Cowork chat in the Abracodabra project: *"Playtest of Block 1: <answers>. Log it per CLAUDE.md §4 and propose Block 2."* (Opus · high, 20–30 min, medium.)
3. [ ] Repeat steps 7–8. Two blocks in, the project is back to a normal rhythm.

---

## Things to consider

- **Modernization can become a hiding place.** Cleanup feels productive and asks no hard design questions. The one-week cap and step 6 coming straight after it are there for that reason.
- **The real blocker in July wasn't the pipeline.** It was an experiment (D1) that was never run, plus the finicky-avatar verdict. Step 6 has to deal with that, or the stall returns on a cleaner codebase.
- **Don't port Bitbloom features into Abracodabra mid-block.** Lessons come in through step 6's design pass, code doesn't. If a shared idea (e.g. programmable traps) earns a place, it goes into a pack first.
- **Two Editors at once:** fine after step 5 (same version). Claude Code always starts from the project's own folder so the CLI talks to the right Editor.
- **The old backup folder** `Abracodebra 1.0 - Backup 08.07.2026`: once step 3 has pushed and tagged, git holds everything; zip it to cold storage or delete it.

**Next action:** step 1 (look at GitHub Desktop), then step 2.
