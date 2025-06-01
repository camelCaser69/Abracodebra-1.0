# 04_Current_Sprint_And_Roadmap.md

**Synthesized:** 2025-05-31
**Project:** Gene Garden Survivor

Current development focus (Sprint 0), project roadmap, success metrics, risk mitigation.

## 🎯 Current Focus: Sprint 0 – Round Loop Foundation

**Duration:** Week 1 (Est. 14.5h)
**Goal:** Establish player-controlled Planning → Growth & Threat → Recovery loop.

### Sprint 0 Tasks

| ID | Category            | Task                                                                                                  | Est. | Prio.    | Scripts Involved                  |
| :--| :------------------ | :---------------------------------------------------------------------------------------------------- | :--- | :------- | :-------------------------------- |
| A1 | Core State Mgmt     | `RunManager.cs` (New): Singleton, states (`Planning`, `GrowthAndThreat`, `Recovery`), transitions. Controls `Time.timeScale`. | 2.0h | Critical | `RunManager.cs`                   |
| A2 | Scene Integration   | Add `RunManager` GO to `MainScene`. Wire `WeatherManager`, `WaveManager` refs.                         | 0.5h | Critical | `MainScene.unity`                 |
| B1 | Time Control        | `WeatherManager.SimulateDay()`: Freeze cycle at daylight, invoke events. Controlled by `RunManager`.   | 1.5h | Critical | `WeatherManager.cs`               |
| B2 | WaveManager Integ.  | `WaveManager.NoActiveThreats()` (bool) & `SpawnNextWave()` for manual control.                      | 0.5h | Critical | `WaveManager.cs`                  |
| C1 | UI Overhaul         | `UIManager.cs` (New): Manages Planning/Running/Recovery panel visibility. Buttons link to `RunManager`. | 2.0h | High     | `UIManager.cs`                    |
| C2 | UI Canvas Panels    | Design 3 UI Panels (Planning/Green, Running/Orange, Recovery/Blue) with TMP_Text, Buttons.            | 2.0h | High     | Unity Editor (Canvas)             |
| C3 | Legacy Timer Removal| Remove/disable auto countdown UIs & wave triggers in `WaveManager`.                                   | 1.0h | High     | `WaveManager.cs`, UI GOs          |
| D1 | WeatherManager Ctrl | `RunManager` calls `SimulateDay()` in Planning/Growth. Verify daylight visuals.                     | 1.0h | Medium   | `RunManager.cs`, `WeatherManager.cs` |
| E1 | WaveManager Ctrl    | `RunManager` calls `SpawnNextWave()` in Threat phase. Round ends on `NoActiveThreats()`.                | 1.0h | Medium   | `RunManager.cs`, `WaveManager.cs` |
| F1 | Testing/Validation  | End-to-end cycle. System integration. Perf & edge cases.                                              | 3.0h | High     | Gameplay                          |
| -  | Buffer/Review       | Contingency.                                                                                          | 1.0h | -        | -                                 |
|    | **Total Estimated** |                                                                                                       |**14.5h**|        |                                   |

### Sprint 0 Success
*   **Tech:** No errors, 60 FPS (mod. load), stable memory.
*   **Func:** Player controls Planning → Growth & Threat → Recovery. `Time.timeScale` correct (0 | ~6 | 0). Plants grow, threats spawn.
*   **Quality:** Clean code, error handling, no regressions.

## 🗺️ Roadmap (Post-Sprint 0)

### Phase 2: Integration & Enhanced Gameplay
| Sprint | Duration | Focus                     | Deliverable                                          |
| :----- | :------- | :------------------------ | :--------------------------------------------------- |
| **1**  | Wk 2     | Genetics & Combat         | Leaf health, 15+ combat genes, threat AI targets leaves. |
| **2**  | Wk 3     | Player Classes & Agency   | 4 classes, scarce seeds, emergency tools.             |
| **3**  | Wk 4     | Biomes & Meta-Game        | 5 biomes, Gene Echo currency, Gene Library unlock.     |
| **4**  | Wk 5-6   | Advanced Systems & Polish | Gene synergies, adaptive AI, tutorial, audio/VFX pass. |

### Phase 3: Content Expansion (Future)
*   **Genetics:** Advanced nodes (healing, poison, temporal), environmental interactions.
*   **Ecosystem:** Predator-prey, seasons, resource cycles.
*   **Gameplay:** Research challenges, genetic puzzles, tool durability.

### Phase 4: Roguelike Features (Future)
*   **Discovery:** Hidden genes, mutations, Research Lab.
*   **Challenges:** Scenarios, environmental constraints, procedural content.

### Phase 5: Advanced Systems (Long-Term)
*   ECS conversion, full save/load, multiplayer experiments, procedural worlds.

## 🚀 Post-Launch (Conceptual)
*   **M1-2:** Gene-sharing, daily challenges, leaderboards.
*   **M3-4:** New biomes, legendary genes, mutations.
*   **M5-6:** Multiplayer, Workshop, speed-run mode.

## 📊 Success Metrics (Project)
*   **Tech:** 60 FPS (100+ plants, 50+ animals). <512MB RAM. <5s load.
*   **Content:** 50+ node types, 20+ animal species, 15+ tools, 10+ tile types.
*   **Engagement:** 70% first run completion. 5+ gene combos discovered/user. 85%+ positive reviews.

## ⚠️ Risk Mitigation
| Area         | Mitigation                                                     |
| :----------- | :------------------------------------------------------------- |
| **Perf**     | Profile each sprint. Cap 100+ entities @ 60 FPS. Optimize.     |
| **Complexity** | Gradual unlocks, tutorial (Phase 4). Clear UI/UX.             |
| **Market**   | Market "peaceful genetics roguelike". $15-$20 price. Early feedback. |
| **Scope**    | Adhere to sprint goals. Prune scope. Focus on core vision.     |