# Abracodebra 2.0 ‒ Project Readme

(Last updated 2025‑04‑18 by AI assistant)

## 🚀 Project Overview

Abracodebra 2.0 is a 2D pixel‑art ecosystem rogue-like sandbox built with **Unity 6 (6000.0.39f1)** experience centres around a living world where **plants grow procedurally from node‑graphs**, **animals wander, eat, think and poop**, and the environment reacts via a day/night cycle, scents, tile interactions and visual post‑processing. The player is trying to keep his harvest intact by genetically engineering the plants (editing gene nodes) and obtaining/inventing gene sequences in a rogue-like fassion. The game is inspired by Noita game, one can think of it as a blend of Noita and Stardew Valley.

### High‑level feature list

- Dual‑grid Wang‑tile ground system (3rd‑party package)
- Modular plant growth driven by scriptable node graphs
- Dynamic fauna AI with hunger, thoughts and pathfinding‑free wandering
- Ecosystem scent system (sources + radius visualisers)
- Weather/lighting manager with tunable day/night transitions
- Tile interaction layer (tools, modifiers, growth multipliers)
- Player “Gardener” controller with tool switching
- URP 2D renderer with custom shaders & post effects

## 🎮 Current Gameplay Loop (prototype)

1. Player spawns seeds → `PlantGrowth` instantiates **seed cells**.
2. The seed evaluates its **NodeGraph** and grows a stem & leaves over time using tile‑based speed multipliers.
3. Mature plants execute cycles (grow berries, emit scents, spawn projectiles, etc.) while the player observes.
4. **Fireflies** spawn at night, boosting photosynthesis.
5. **Bunnies/Foxes/Birds** spawn via `FaunaManager`, seek food (`FoodItem`s), produce **Poop** that eventually fades.
6. Day/night changes light + photosynthesis rate; tile modifiers influence growth speed & energy recharge.

## 📁 Folder & Module Map

| Path                          | Purpose                                                          |
| ----------------------------- | ---------------------------------------------------------------- |
| `Assets/Scripts/Editor`       | Auto‑generation/editors for **NodeDefinition** assets.           |
| `Assets/Scripts/Nodes`        | Runtime + UI for node graphs controlling plants.                 |
| `Assets/Scripts/Battle/Plant` | Core **PlantGrowth** state‑machine, weather & plant cells.       |
| `Assets/Scripts/Ecosystem`    | Animal AI, effects (Firefly), scents, thoughts, poop, managers.  |
| `Assets/Scripts/Tiles`        | Definitions/interaction rules, modifiers & Dual‑grid rule‑tiles. |
| `Assets/Scripts/Player`       | Gardener input + tool switching.                                 |
| `Assets/Prefabs`              | Visual prefabs for entities, UI & effects.                       |
| `Assets/Scenes/MainScene`     | Primary sandbox scene.                                           |

## 🌿 Dual‑Grid Tilemap System (3rd‑party)

The ground uses a **dual‑grid Wang‑tile** approach: a hidden **Data Tilemap** drives a half‑unit‑offset **Render Tilemap** for seamless transitions.

- **Rule‑tile assets** live under `Assets/Prefabs/Tiles/Rule Tiles` (Grass, Dirt, DirtWet).
- Authoring workflow ➜ import 4×4 tilesheet ➜ auto‑create **Dual Grid Rule Tile** ➜ create **Dual Grid Tilemap** (GameObject → 2D Object → Tilemap → Dual Grid Tilemap) ➜ assign rule‑tile in the **Dual Grid Tilemap Module**.
- Advanced options (colliders, GameObject spawning) are documented in the included *cheatsheet* and *advanced‑features* guides.

## 🛠️ Build & Setup

- **Unity 6 (6000.0.39f1)** + URP 2D template.
- Required packages: TextMeshPro (core), 2D Tilemap Extras, Dual Grid Tilemap package (included under `Packages`).
- Open `Assets/Scenes/MainScene.unity` and press Play.
- Ensure `InputSystem_Actions.inputactions` is auto‑generated before entering play‑mode.
- Inspector warnings will highlight missing scriptable‑object refs (e.g., `ScentLibrary` in `EcosystemManager`).

## 👩‍💻 Contributing & Coding Guidelines

> **These rules are optimised for both human and AI collaborators. Stick to them to avoid merge chaos and hallucinations.**

### Core Principles

- **Consistency** – mirror existing folder, naming & namespace patterns.
- **Memory** – load and pass the *current* object state; never invent fields.
- **Fool‑proofing** – architect for clarity over cleverness.
- **Scalability & Modularity** – prefer data‑driven or dependency‑injected solutions; avoid magic numbers or hard‑coded paths.
- **Accuracy** – double‑check field names & types against source; compile before committing.
- **Speed of execution** – provide copy‑paste‑ready code; move commentary to docs.
- **Full script awareness** – new systems must register with existing managers (e.g., scents, sorting).
- **Minimal manual linking** – hook up components (e.g., add `SortableEntity`) via `AddComponent` in code or prefab.
- **Implementation‑first thinking** – deliver runnable feature, then document.
- **Suggestions welcome** – if a cleaner pattern exists (e.g., generic StateMachine, ECS), propose it in PR description.

### Added Conventions

- **Naming**: `PascalCase` for classes/SO assets (`NodeDefinition_LeafBoost.asset`), `camelCase` private fields with `[SerializeField]`.
- **Assembly Definitions**: group by feature (Ecosystem, Tiles, Nodes) to speed compilation.
- **Scenes**: keep *MainScene* light; use additive scenes for testbeds.
- **Testing**: place PlayMode tests under `Tests/PlayMode` for crit‑path systems (growth, AI).
- **Events**: favour C# events or `UnityEvent` over polling; consider ScriptableObject signals for decoupling.
- **Version Control**: trunk‑based – `main` (release), `dev`, short‑lived `feature/xyz` branches; use `.meta` files; lock binary assets when editing.
- **Documentation**: update this README and relevant guides when adding public APIs.
- **Editor‑only code** inside `#if UNITY_EDITOR` blocks.

If you're AI model, make sure to:
1) When editing a method, ALWAYS return me the whole updated method (bits like " // …existing code…" inside a method are strictly forbidden for easier implementation flow)
2) when editing more than a single method, either give me the whole script if it's not too long, or make sure to organize it very nicely so I have easy time copy pasting and implementing it

### AI Helper Tips

- Ask for the **latest Node graph** or **Tile definitions** before generating growth logic.
- When adding a Dual‑grid tile, remember to regenerate rule‑tile hashes or the tile will paint blank.
- Validate scriptable‑object links in play‑mode – AI often forgets to assign them.
- Use `FindObjectsByType<T>(FindObjectsSortMode.None)` instead of legacy `FindObjectsOfType`.
- Prefer `RandomVariation` parameters over multiple similar prefabs.

## 🤖 Potential Future Improvements

- Convert scent & thought logic to an **event‑queue** to decouple systems.
- Introduce **Unity ECS/Entities** for mass plant instances to gain performance.
- Create a **Save/Load JSON** layer for NodeGraphs and tilemaps.
- Hook a lightweight **unit‑test harness** for formula balancing.
- Replace manual `Dictionary` scent storage with a strongly‑typed `struct` for Burst/ECS readiness.

## 🔗 Repository

GitHub: [https://github.com/camelCaser69/Abracodebra-1.0](https://github.com/camelCaser69/Abracodebra-1.0)\
Scripts live under [`Assets/Scripts`](https://github.com/camelCaser69/Abracodebra-1.0/tree/main/Assets/Scripts) and [`Assets/Editor`](https://github.com/camelCaser69/Abracodebra-1.0/tree/main/Assets/Editor).

---

**End of README** – feel free to suggest edits or request deeper dives into any subsystem.

