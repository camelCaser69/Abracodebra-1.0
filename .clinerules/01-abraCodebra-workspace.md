## Folder & File Conventions
- **Default read/write scope:** `cline_docs/**` and `Assets/Scripts/**` only.  
  - Touch other folders (e.g. `Assets/Shaders/**`, `Assets/Content/**`) **only** when a task explicitly demands it—state the reason in your reply header.
- Scripts live under `Assets/Scripts/` with sub-folders mirroring namespaces (`Assets/Scripts/Combat/`).

## Smart Script Loading
- For wide-angle analysis (e.g., refactor, cross-cutting audit), **run**  
  - `python extract_scripts.py` (or `run_extract_scripts.bat`)  
  This generates `Unity_EXTRACTED_scripts.txt`—load that single file instead of every script to spare tokens.
- Always read `cline_docs/` first (in the usual order) **before** opening any script file.

## Required Docs (from custom instructions)
- Update `projectRoadmap.md` & `currentTask.md` on milestone/task completion.
- Refresh `codebaseSummary.md` after structural refactors.

## Full-Script Awareness
- Before modifying a feature, load all directly related scripts (dependency graph header: `fileA → fileB → fileC`).

## Minimal Manual Linking
- Hook components via `[RequireComponent]`, `GetComponent` in `Awake` or DI container—avoid inspector drag-and-drop unless for designer tweaking.

## Fool-Proofing Checks
- Fail fast: null-checks + clear logs.
- Mandatory public fields: `[SerializeField, Tooltip("Required")]` + `OnValidate` guard.

## Speed of Execution
- Replies start with **“### Code”** (single copy-paste block).  
  Optional: **“### Explanation”**, **“### Next Steps”** — keep them brief.

## Suggestion Protocol
- Cleaner pattern? Add a 📐 **“Refactor Pitch”** footer (≤ 2 lines) before implementing.