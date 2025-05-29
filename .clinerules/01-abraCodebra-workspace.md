## Folder & File Conventions
- **Default read/write scope:** `cline_docs/**` (only the *flagged* docs below) and `Assets/Scripts/**`.
- Touch other folders (e.g. `Assets/Shaders/**`, `Assets/Content/**`) **only when a task explicitly demands it**—state the reason in your reply header.

## Lean Documentation Pass
1. **Read these every task (in order):**
   - `cline_docs/currentTask.md`
   - `cline_docs/projectRoadmap.md`
2. **Read *one* of the following only if the task type warrants it:**
   - `codebaseSummary.md` → when editing multiple systems or doing cross-cutting refactor.
   - `techStack.md` / `buildSettings.md` → when build, package, or platform issues arise.
   - `playtestFeedback.md` → when tackling UX/polish tasks or balancing.
   - `development_workflow.md` / `systemsGlossary.md` → when onboarding new contributors or documenting systems.
3. Skip any unneeded doc to save tokens.  
   If unsure which doc you need, ask Milan.

## Smart Script Loading
- For broad audits (e.g., “scan all scripts for null checks”), **run**  
  `python extract_scripts.py` (or `run_extract_scripts.bat`) – then read only the generated `Unity_EXTRACTED_scripts.txt`.
- Otherwise load just the directly related files.

## Full-Script Awareness
- Before modifying code, build a quick dependency graph header (`fileA → fileB → fileC`) so you (and the AI) keep context straight.

## Minimal Manual Linking
- Hook components via `[RequireComponent]`, `GetComponent` in `Awake`, or DI container—avoid inspector drag-and-drop unless for designer tweaking.

## Fool-Proofing Checks
- Fail fast: null-checks + clear logs.
- Mandatory public fields: `[SerializeField, Tooltip("Required")]` + `OnValidate` guard.

## Speed of Execution
- Replies start with **“### Code”** (single copy-paste block).  
  Optional: **“### Explanation”**, **“### Next Steps”** — keep them brief.

## Suggestion Protocol
- Cleaner pattern? Add a 📐 **“Refactor Pitch”** footer (≤ 2 lines) before implementing.