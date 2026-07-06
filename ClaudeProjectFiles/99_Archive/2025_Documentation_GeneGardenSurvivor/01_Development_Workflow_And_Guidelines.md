# 01_Development_Workflow_And_Guidelines.md

**Synthesized:** 2025-05-31

Workflow, coding guidelines, and AI collaboration best practices for *Gene Garden Survivor*.

## 📜 Core Principles (Human & AI)

*   **Consistency:** Mirror existing project patterns (folders, names, architecture).
*   **Accuracy (AI Focus):**
    *   Use latest provided info/scripts.
    *   Provide complete, copy-paste-ready code (no `// ...existing code...` in methods).
    *   Verify names/types against source. Do not invent unstated features.
*   **Clarity:** Readable, maintainable code over complex solutions.
*   **Modularity:** Prefer data-driven (ScriptableObjects) or DI. Avoid hard-coding.
*   **System Awareness:** New code must integrate with existing managers (e.g., `RunManager`).
*   **Minimal Inspector Linking:** Favor `[RequireComponent]`, `GetComponentIn...()` in `Awake`. Inspector linking for SOs or essential scene refs.
*   **Implementation First:** Deliver runnable features, then document.
*   **Suggestions:** Propose refactors briefly before implementing.

## 🚀 Daily Cycle

1.  **Sync:** `git pull`. Check Unity Package Manager.
2.  **Validate:** Quick check of `MainScene` manager references.
3.  **Develop:**
    *   **Code:** Adhere to conventions.
    *   **Test:** Frequent Play Mode tests. Use Test Runner. Profile performance.
    *   **Commit:** Clear messages (e.g., `feat: ...`). Push to feature branches.

## 📝 Naming & Coding Conventions

*   **Classes/SOs:** `PascalCase` (e.g., `PlantGrowth`, `NodeDefinition_Berry.asset`).
*   **Serialized Private Fields:** `camelCase` with `[SerializeField]`.
*   **Non-Serialized Private Fields:** `_camelCase`.
*   **Methods/Enums:** `PascalCase`.
*   **Assemblies:** Group by feature (Ecosystem, Tiles, Nodes, etc.).
*   **Editor Code:** Use `#if UNITY_EDITOR`.
*   **Comments:** XML for public APIs. Brief for complex logic.
*   **Error Handling:** Null-checks for critical refs. Clear logs.
*   **Events:** C# `event Action<T>` or `UnityEvent`.

## 📂 Folder & Asset Conventions

*   Use existing directory structure (`Unity_EXTRACTED_scripts.txt` as guide).
*   **SOs:** `Assets/Scriptable Objects/[Category]/`.
*   **Prefabs:** `Assets/Prefabs/[Category]/`.
*   Utilize editor scripts for auto-naming/numbering (e.g., `Node_XXX_`).

## 🤝 AI Collaboration Guidelines (For AI Agent)

Optimize our work with these focused instructions.

### 1. Document Prioritization & Scope:
   *   **Core Context (Always Read):**
        *   `00_Project_GeneGardenSurvivor_Overview.md`
        *   `04_Current_Sprint_And_Roadmap.md`
   *   **Task-Specific (Consult as Needed):**
        *   **Cross-System Changes:** `03_Gameplay_Systems_Manual.md` (relevant parts).
        *   **Build/Platform Issues:** `02_Technical_Stack_And_Build.md`.
        *   **UX/Balance:** `05_Playtest_Feedback_And_Known_Issues.md`.
        *   **New Systems/Onboarding:** This file, `03_Gameplay_Systems_Manual.md`.
        *   **Dual-Grid:** `07_Third_Party_Package_Guide_DualGrid.md`.
   *   **Scripts are Ground Truth:** Code (`Unity_EXTRACTED_scripts.txt` or individual files) is current state. Docs show intent. For implementing doc changes, docs define target state.
   *   **File Scope:** Primarily `Assets/Scripts/`. Modify other assets (shaders, prefabs) only if task demands and reason is stated.

### 2. Script Handling:
   *   **Full Context:** Use full script context if available.
   *   **Dependencies:** Note key script dependencies before modifying.
   *   **Complete Methods:** Always return entire modified method bodies. No `// ...`.
   *   **Full Scripts:** For extensive changes, provide entire updated script if not too long.

### 3. Information Integrity:
   *   **No Hallucinations:** Don't invent features/code not specified or existing.
   *   **Precise Naming:** Use exact names from codebase/docs.
   *   **Latest Info Priority:** Newest documents override older ones. Scripts = current actual; latest docs = target state.

### 4. Output Formatting:
   *   Start code blocks with `### Code`.
   *   Optional, brief `### Explanation`, `### Next Steps`. Markdown for readability.

### 5. System-Specific AI Notes:
   *   **Node System:** Request current `NodeEffectType` enum / `NodeDefinition` structures if needed.
   *   **Dual-Grid:** RuleTile changes may need editor regeneration by user.
   *   **SO Links:** Code should expect SO references; AI cannot assign in Inspector.
   *   **Unity API:** Use modern APIs (e.g., `FindObjectsByType<T>`).
   *   **`RunManager` (New):** This is the primary game state/`Time.timeScale` controller. `WaveManager`/`WeatherManager` integrate with it.

## 🧪 Testing & 🐛 Debugging

*   **Quick Tests:** Play Mode, console check per change.
*   **Comprehensive Tests:** Automated tests, performance profiling, edge cases before commit.
*   **Debug Tools:** Unity Console, Profiler, Frame Debugger. Custom visualizations (`FloraManager`). Clear logs.

## 📚 Documentation
*   Update relevant `.md` files for system/API changes. XML comments for public C# APIs. AI indicates if changes need doc updates.