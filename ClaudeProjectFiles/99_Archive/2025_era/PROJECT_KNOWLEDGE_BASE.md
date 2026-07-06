# Gene Garden Survivor - Project Knowledge Base

**Last Updated:** 2025-05-31  
**Project Version:** Sprint 0 Development  
**Unity Version:** 6 (6000.0.39f1)

---

## Table of Contents

1. [Project Overview & Vision](#1-project-overview--vision)
2. [Technical Architecture](#2-technical-architecture)
3. [Current Sprint Focus](#3-current-sprint-focus)
4. [Core Systems Documentation](#4-core-systems-documentation)
5. [Development Guidelines](#5-development-guidelines)
6. [Asset Organization](#6-asset-organization)
7. [Known Issues & Technical Debt](#7-known-issues--technical-debt)
8. [Roadmap & Future Development](#8-roadmap--future-development)

---

## 1. Project Overview & Vision

### Core Concept
Gene Garden Survivor is a peaceful genetics-based roguelike where players design plant DNA through a node-based system and watch their creations survive in a living ecosystem. The game combines strategic planning with real-time ecosystem simulation.

### Gameplay Loop
**Three-Phase Player-Controlled Loop:**
- **Planning Phase** (`Time.timeScale = 0`): Edit plant DNA via Node Editor, design garden layout, analyze threats, allocate resources
- **Growth & Threat Phase** (`Time.timeScale ~ 6`): Plants grow rapidly, threats spawn via waves, plants auto-combat with limited player intervention
- **Recovery Phase** (`Time.timeScale = 0`): Review performance, collect rewards (Gene Echoes), unlock research

### Long-Term Goals
- **Market Position:** "Peaceful genetics roguelike" targeting $15-$20 price point
- **Content Goals:** 50+ node types, 20+ animal species, 15+ tools, 10+ tile types
- **Engagement Targets:** 70% first run completion, 5+ gene combos discovered per user, 85%+ positive reviews
- **Performance Goals:** 60 FPS with 100+ plants and 50+ animals, <512MB RAM, <5s load times

---

## 2. Technical Architecture

### Unity Configuration
- **Version:** Unity 6 (6000.0.39f1)
- **Render Pipeline:** Universal Render Pipeline (URP) with 2D Renderer
- **API Compatibility:** .NET Standard 2.1
- **Scripting Backend:** Mono (Dev), IL2CPP (Release)
- **Primary Target:** Windows 64-bit
- **Graphics:** Linear color space, Pixel Perfect Camera (320x180 ref, 16 PPU)

### Core Architectural Patterns
- **Singletons:** Manager classes ([`EcosystemManager`](Assets/Scripts/Ecosystem/Core/EcosystemManager.cs), [`TileInteractionManager`](Assets/Scripts/Tiles/Data/TileInteractionManager.cs), [`PlantGrowthModifierManager`](Assets/Scripts/Tiles/Data/PlantGrowthModifierManager.cs))
- **ScriptableObjects:** Data-driven configuration for all game content
- **Partial Classes:** Complex systems split across multiple files (e.g., [`PlantGrowth`](Assets/Scripts/Battle/Plant/PlantGrowth.cs))
- **State Machines:** Core gameplay states and plant lifecycle management
- **Event System:** C# `event Action<T>` for loose coupling

### Key Dependencies
- **Third-Party:** DualGrid tilemap system (com.skner.dualgrid), HueFolders
- **Unity Packages:** URP, Input System, TextMesh Pro, 2D Sprite/Animation, Tilemap Editor, Pixel Perfect

---

## 3. Current Sprint Focus

### Sprint 0: Round Loop Foundation (Week 1, Est. 14.5h)
**Goal:** Establish player-controlled Planning → Growth & Threat → Recovery loop

#### Critical Tasks
| Priority | Task | Scripts Involved | Status |
|----------|------|------------------|---------|
| Critical | [`RunManager.cs`](Assets/Scripts/Core/RunManager.cs) - Singleton state management | New file needed | Pending |
| Critical | Scene integration with [`WeatherManager`](Assets/Scripts/Battle/Plant/WeatherManager.cs), [`WaveManager`](Assets/Scripts/Ecosystem/Core/WaveManager.cs) | Existing files | Pending |
| Critical | Time control via [`WeatherManager.SimulateDay()`](Assets/Scripts/Battle/Plant/WeatherManager.cs) | Existing file | Pending |
| High | [`UIManager.cs`](Assets/Scripts/UI/UIManager.cs) - Phase panel management | New file needed | Pending |
| High | UI Canvas design (Planning/Running/Recovery panels) | Unity Editor | Pending |

#### Success Criteria
- **Technical:** No errors, 60 FPS, stable memory
- **Functional:** Player controls all three phases, correct `Time.timeScale` values, plants grow, threats spawn
- **Quality:** Clean code, error handling, no regressions

---

## 4. Core Systems Documentation

### 4.1 Plant Systems

#### Node Graph System
**Purpose:** Defines plant genetics through a visual node-based editor

**Key Scripts:**
- [`NodeDefinition`](Assets/Scripts/Nodes/Core/NodeDefinition.cs) - ScriptableObject defining individual genes
- [`NodeGraph`](Assets/Scripts/Nodes/Runtime/NodeGraph.cs) - Runtime collection of nodes for plant instance
- [`NodeData`](Assets/Scripts/Nodes/Core/NodeData.cs) - Individual node instance in runtime graph
- [`NodeEffectType`](Assets/Scripts/Nodes/Core/NodeEffectType.cs) - Enum defining all possible node effects
- [`NodeDefinitionLibrary`](Assets/Scripts/Nodes/Core/NodeDefinitionLibrary.cs) - Collection of all available nodes

**Current Status:** Fully implemented, supports passive and active effects
**Integration Points:** [`PlantGrowth`](Assets/Scripts/Battle/Plant/PlantGrowth.cs), [`NodeEditorGridController`](Assets/Scripts/Nodes/UI/NodeEditorGridController.cs)
**Development Priority:** Stable, focus on new node types in future sprints

#### Plant Growth System
**Purpose:** Manages plant lifecycle from seed to mature plant with combat capabilities

**Key Scripts:**
- [`PlantGrowth.cs`](Assets/Scripts/Battle/Plant/PlantGrowth.cs) - Main growth controller (partial class)
- [`PlantGrowth.Growth.cs`](Assets/Scripts/Battle/Plant/PlantGrowth.Growth.cs) - Growth coroutine logic
- [`PlantGrowth.Cell.cs`](Assets/Scripts/Battle/Plant/PlantGrowth.Cell.cs) - Cell spawning and management
- [`PlantGrowth.NodeExecution.cs`](Assets/Scripts/Battle/Plant/PlantGrowth.NodeExecution.cs) - Mature cycle execution
- [`PlantCell.cs`](Assets/Scripts/Battle/Plant/PlantCell.cs) - Individual plant parts
- [`LeafData.cs`](Assets/Scripts/Battle/Plant/LeafData.cs) - Leaf tracking for regrowth

**Current Status:** Core functionality complete, "Leaf = Life" system implemented
**Integration Points:** [`NodeGraph`](Assets/Scripts/Nodes/Runtime/NodeGraph.cs), [`WeatherManager`](Assets/Scripts/Battle/Plant/WeatherManager.cs), [`PlantGrowthModifierManager`](Assets/Scripts/Tiles/Data/PlantGrowthModifierManager.cs)
**Development Priority:** Optimize performance, add combat genes

#### Plant Effects System
**Purpose:** Visual and gameplay effects for plants

**Key Scripts:**
- [`PlantShadowController`](Assets/Scripts/Visuals/PlantShadowController.cs) - Dynamic shadow generation
- [`PlantOutlineController`](Assets/Scripts/Visuals/PlantOutlineController.cs) - Plant outline visualization
- [`ScentSource`](Assets/Scripts/Ecosystem/Core/ScentSource.cs) - Scent emission from plants
- [`OutputNodeEffect`](Assets/Scripts/Nodes/Core/OutputNodeEffect.cs) - Projectile spawning system

**Current Status:** Fully functional visual effects
**Integration Points:** [`PlantGrowth`](Assets/Scripts/Battle/Plant/PlantGrowth.cs), [`FloraManager`](Assets/Scripts/Ecosystem/Core/FloraManager.cs)
**Development Priority:** Performance optimization for large plant populations

### 4.2 Ecosystem Systems

#### Animal AI System
**Purpose:** Creature behavior, threats, and ecosystem interactions

**Key Scripts:**
- [`AnimalController.cs`](Assets/Scripts/Ecosystem/Core/AnimalController.cs) - Core AI behavior
- [`AnimalDefinition`](Assets/Scripts/Ecosystem/Animals/AnimalDefinition.cs) - Species configuration
- [`AnimalDiet`](Assets/Scripts/Ecosystem/Food/AnimalDiet.cs) - Feeding behavior
- [`FaunaManager`](Assets/Scripts/Ecosystem/Core/FaunaManager.cs) - Animal spawning and management
- [`WaveManager`](Assets/Scripts/Ecosystem/Core/WaveManager.cs) - Threat wave coordination

**Current Status:** Basic AI functional, needs threat targeting updates for Sprint 0
**Integration Points:** [`RunManager`](Assets/Scripts/Core/RunManager.cs) (pending), [`PlantGrowth`](Assets/Scripts/Battle/Plant/PlantGrowth.cs)
**Development Priority:** Adapt for new game loop, improve pathfinding

#### Scent & Communication System
**Purpose:** Chemical communication between plants and animals

**Key Scripts:**
- [`ScentDefinition`](Assets/Scripts/Ecosystem/Scents/ScentDefinition.cs) - Scent properties
- [`ScentLibrary`](Assets/Scripts/Ecosystem/Scents/ScentLibrary.cs) - Scent collection
- [`ThoughtBubbleController`](Assets/Scripts/Ecosystem/Core/ThoughtBubbleController.cs) - Animal thoughts
- [`AnimalThoughtLibrary`](Assets/Scripts/Ecosystem/Core/AnimalThoughtLibrary.cs) - Thought content

**Current Status:** Fully functional
**Integration Points:** [`AnimalController`](Assets/Scripts/Ecosystem/Core/AnimalController.cs), [`PlantGrowth`](Assets/Scripts/Battle/Plant/PlantGrowth.cs)
**Development Priority:** Stable, expand content

#### Firefly System
**Purpose:** Nighttime photosynthesis bonus and atmospheric effects

**Key Scripts:**
- [`FireflyManager`](Assets/Scripts/Ecosystem/Effects/FireflyManager.cs) - Spawning and management
- [`FireflyController`](Assets/Scripts/Ecosystem/Effects/FireflyController.cs) - Individual firefly behavior

**Current Status:** Fully functional
**Integration Points:** [`WeatherManager`](Assets/Scripts/Battle/Plant/WeatherManager.cs), [`PlantGrowth`](Assets/Scripts/Battle/Plant/PlantGrowth.cs)
**Development Priority:** Stable

### 4.3 Environment & Tile Systems

#### Dual-Grid Tile System
**Purpose:** Advanced tilemap rendering using Wang tiles

**Key Components:**
- **Third-party package:** com.skner.dualgrid
- **Data Tilemap:** Hidden logic layer
- **Render Tilemap:** Visible 4x4 tile rendering
- [`TileDefinition`](Assets/Scripts/Tiles/Data/TileDefinition.cs) - Project tile abstraction

**Current Status:** Fully integrated and functional
**Integration Points:** [`TileInteractionManager`](Assets/Scripts/Tiles/Data/TileInteractionManager.cs)
**Development Priority:** Stable, add new tile types

#### Tile Interaction System
**Purpose:** Tool-based tile modification and plant placement

**Key Scripts:**
- [`TileInteractionManager.cs`](Assets/Scripts/Tiles/Data/TileInteractionManager.cs) - Core interaction logic
- [`TileInteractionLibrary`](Assets/Scripts/Tiles/Data/TileInteractionLibrary.cs) - Interaction rules
- [`PlayerTileInteractor`](Assets/Scripts/Tiles/Data/PlayerTileInteractor.cs) - Player input handling
- [`PlantPlacementManagement`](Assets/Scripts/Tiles/Data/PlantPlacementManagement.cs) - Plant spawning

**Current Status:** Fully functional
**Integration Points:** [`ToolSwitcher`](Assets/Scripts/Tiles/Tools/ToolSwitcher.cs), [`NodeEditorGridController`](Assets/Scripts/Nodes/UI/NodeEditorGridController.cs)
**Development Priority:** Stable

#### Growth Modifier System
**Purpose:** Tile-based effects on plant growth and energy

**Key Scripts:**
- [`PlantGrowthModifierManager.cs`](Assets/Scripts/Tiles/Data/PlantGrowthModifierManager.cs) - Modifier application

**Current Status:** Fully functional
**Integration Points:** [`PlantGrowth`](Assets/Scripts/Battle/Plant/PlantGrowth.cs), [`TileInteractionManager`](Assets/Scripts/Tiles/Data/TileInteractionManager.cs)
**Development Priority:** Stable

### 4.4 Tool Systems

#### Tool Management
**Purpose:** Player tool switching, usage tracking, and special abilities

**Key Scripts:**
- [`ToolDefinition`](Assets/Scripts/Tiles/Tools/ToolDefinition.cs) - Tool configuration
- [`ToolSwitcher.cs`](Assets/Scripts/Tiles/Tools/ToolSwitcher.cs) - Tool state management
- [`ToolType`](Assets/Scripts/Tiles/Tools/ToolType.cs) - Tool categories

**Current Status:** Fully functional
**Integration Points:** [`GardenerController`](Assets/Scripts/Player/GardenerController.cs), [`TileInteractionManager`](Assets/Scripts/Tiles/Data/TileInteractionManager.cs)
**Development Priority:** Add player class special tools

### 4.5 Weather & Time Systems

#### Weather Management
**Purpose:** Day/night cycle and environmental effects

**Key Scripts:**
- [`WeatherManager.cs`](Assets/Scripts/Battle/Plant/WeatherManager.cs) - Cycle management

**Current Status:** Needs Sprint 0 integration with [`RunManager`](Assets/Scripts/Core/RunManager.cs)
**Integration Points:** [`PlantGrowth`](Assets/Scripts/Battle/Plant/PlantGrowth.cs), [`FireflyManager`](Assets/Scripts/Ecosystem/Effects/FireflyManager.cs), [`RunManager`](Assets/Scripts/Core/RunManager.cs) (pending)
**Development Priority:** Critical for Sprint 0

### 4.6 Visual Systems

#### Rendering & Effects
**Purpose:** Visual polish and atmospheric effects

**Key Scripts:**
- [`WaterReflection.cs`](Assets/Scripts/Visuals/WaterReflection.cs) - Water reflection effects
- [`WaterReflectionManager.cs`](Assets/Scripts/Visuals/WaterReflectionManager.cs) - Global reflection settings
- [`NightColorPostProcess.cs`](Assets/Scripts/Visuals/NightColorPostProcess.cs) - Day/night visual transitions
- [`SortableEntity.cs`](Assets/Scripts/Core/SortableEntity.cs) - Y-sorting for sprites
- [`PixelPerfectSetup.cs`](Assets/Scripts/Visuals/PixelPerfectSetup.cs) - Camera configuration

**Current Status:** Fully functional
**Integration Points:** [`WeatherManager`](Assets/Scripts/Battle/Plant/WeatherManager.cs)
**Development Priority:** Performance optimization

### 4.7 Input & UI Systems

#### Node Editor UI
**Purpose:** Plant genetics design interface

**Key Scripts:**
- [`NodeEditorGridController.cs`](Assets/Scripts/Nodes/UI/NodeEditorGridController.cs) - Main editor controller
- [`NodeCell.cs`](Assets/Scripts/Nodes/UI/NodeCell.cs) - Grid slot management
- [`NodeView.cs`](Assets/Scripts/Nodes/UI/NodeView.cs) - Node visualization
- [`NodeDraggable.cs`](Assets/Scripts/Nodes/UI/NodeDraggable.cs) - Drag and drop functionality

**Current Status:** Fully functional, central to Planning phase
**Integration Points:** [`PlantPlacementManagement`](Assets/Scripts/Tiles/Data/PlantPlacementManagement.cs)
**Development Priority:** Usability improvements

#### Game UI
**Purpose:** In-game interface and phase management

**Key Scripts:**
- [`UIManager.cs`](Assets/Scripts/UI/UIManager.cs) - Phase panel management (Sprint 0, pending)
- [`GardenerController`](Assets/Scripts/Player/GardenerController.cs) - Player UI elements

**Current Status:** Needs Sprint 0 implementation
**Integration Points:** [`RunManager`](Assets/Scripts/Core/RunManager.cs) (pending)
**Development Priority:** Critical for Sprint 0

---

## 5. Development Guidelines

### Core Principles
- **Consistency:** Mirror existing project patterns (folders, names, architecture)
- **Accuracy:** Use latest provided info/scripts, provide complete code
- **Clarity:** Readable, maintainable code over complex solutions
- **Modularity:** Prefer data-driven (ScriptableObjects) or DI, avoid hard-coding
- **System Awareness:** New code must integrate with existing managers

### Coding Conventions
- **Classes/SOs:** `PascalCase` (e.g., `PlantGrowth`, `NodeDefinition_Berry.asset`)
- **Serialized Private Fields:** `camelCase` with `[SerializeField]`
- **Non-Serialized Private Fields:** `_camelCase`
- **Methods/Enums:** `PascalCase`
- **Assemblies:** Group by feature (Ecosystem, Tiles, Nodes, etc.)

### AI Collaboration Rules
- **Scripts are Ground Truth:** Code is current state, docs show intent
- **Complete Methods:** Always return entire modified method bodies, no `// ...`
- **No Hallucinations:** Don't invent features/code not specified or existing
- **System Integration:** New code must work with [`RunManager`](Assets/Scripts/Core/RunManager.cs) (Sprint 0)

### Testing & Debugging
- **Quick Tests:** Play Mode, console check per change
- **Performance:** Profile each sprint, cap 100+ entities @ 60 FPS
- **Debug Tools:** Unity Console, Profiler, custom visualizations

---

## 6. Asset Organization

### ScriptableObject Structure
**Location:** `Assets/Scriptable Objects/`

#### Plant & Node System
- **Nodes Plant/:** [`NodeDefinition`](Assets/Scripts/Nodes/Core/NodeDefinition.cs) assets (`Node_XXX_.asset`)
- **Nodes Plant/NodeDefinitionLibrary.asset:** Master collection
- **Scents/:** [`ScentDefinition`](Assets/Scripts/Ecosystem/Scents/ScentDefinition.cs) assets
- **Scents/ScentLibrary.asset:** Master scent collection

#### Ecosystem
- **Animals/:** [`AnimalDefinition`](Assets/Scripts/Ecosystem/Animals/AnimalDefinition.cs) assets
- **Animals Diet/:** [`AnimalDiet`](Assets/Scripts/Ecosystem/Food/AnimalDiet.cs) configurations
- **Food/:** [`FoodType`](Assets/Scripts/Ecosystem/Food/FoodType.cs) definitions
- **Waves/:** [`WaveDefinition`](Assets/Scripts/Ecosystem/Core/WaveDefinition.cs) spawn patterns

#### Environment
- **Tiles/:** [`TileDefinition`](Assets/Scripts/Tiles/Data/TileDefinition.cs) assets
- **Tools/:** [`ToolDefinition`](Assets/Scripts/Tiles/Tools/ToolDefinition.cs) assets

### Prefab Organization
**Location:** `Assets/Prefabs/`

#### Key Prefabs
- **Ecosystem/Plants/PlantPrefab.prefab:** Base plant with [`PlantGrowth`](Assets/Scripts/Battle/Plant/PlantGrowth.cs)
- **Ecosystem/Animals/:** Animal prefabs with [`AnimalController`](Assets/Scripts/Ecosystem/Core/AnimalController.cs)
- **General/GardenerPrefab.prefab:** Player with [`GardenerController`](Assets/Scripts/Player/GardenerController.cs)
- **Ecosystem/UI/NodeView.prefab:** Node editor components

### Custom Editor Tools
**Location:** `Assets/Editor/`

- **NodeDefinitionAutoAdder.cs:** Auto-adds nodes to libraries
- **NodeDefinitionCreator.cs:** Auto-named node creation
- **TileDefinitionEditor.cs:** Tile color updates
- **TileInteractionManagerEditor.cs:** Sorting and color management

---

## 7. Known Issues & Technical Debt

### High Priority (Sprint 0 Blockers)
1. **Missing [`RunManager`](Assets/Scripts/Core/RunManager.cs):** Core state management system needs implementation
2. **UI System Overhaul:** Phase-specific panels need creation
3. **[`WeatherManager`](Assets/Scripts/Battle/Plant/WeatherManager.cs) Integration:** Needs [`RunManager`](Assets/Scripts/Core/RunManager.cs) coordination

### Performance Issues (Pre-Optimization)
1. **[`AnimalController.Update()`](Assets/Scripts/Ecosystem/Core/AnimalController.cs) loops:** ~45 FPS with 50+ animals
2. **Plant visual effects:** [`PlantShadowController`](Assets/Scripts/Visuals/PlantShadowController.cs)/[`PlantOutlineController`](Assets/Scripts/Visuals/PlantOutlineController.cs) `LateUpdate()` overhead
3. **Entity count:** Frame drops >50 entities
4. **Memory leaks:** Animal thought bubble verification needed

### Medium Priority
1. **AI boundary pathfinding:** Animals clustering/stuck near bounds
2. **Tool feedback:** Missing visual/audio feedback for tool use
3. **Node editor UX:** Steep learning curve, needs tutorial

### Low Priority
1. **Visual transitions:** Abrupt changes need smoothing
2. **Audio system:** Complete absence of audio feedback
3. **Plant placement:** Randomization confusing to players

---

## 8. Roadmap & Future Development

### Phase 2: Integration & Enhanced Gameplay (Post-Sprint 0)

#### Sprint 1 (Week 2): Genetics & Combat
- Leaf health system implementation
- 15+ combat genes addition
- Threat AI targeting leaves specifically
- **Key Scripts:** [`PlantGrowth`](Assets/Scripts/Battle/Plant/PlantGrowth.cs), [`AnimalController`](Assets/Scripts/Ecosystem/Core/AnimalController.cs)

#### Sprint 2 (Week 3): Player Classes & Agency
- 4 starting classes with unique genes
- Scarce seed system
- Emergency tool implementation
- **Key Scripts:** New player class system, [`ToolDefinition`](Assets/Scripts/Tiles/Tools/ToolDefinition.cs) expansion

#### Sprint 3 (Week 4): Biomes & Meta-Game
- 5 biome environments
- Gene Echo currency system
- Gene Library unlock progression
- **Key Scripts:** New biome management system

#### Sprint 4 (Weeks 5-6): Advanced Systems & Polish
- Gene synergy mechanics
- Adaptive AI improvements
- Tutorial system implementation
- Audio/VFX pass

### Phase 3: Content Expansion (Future)
- **Genetics:** Advanced nodes (healing, poison, temporal)
- **Ecosystem:** Predator-prey relationships, seasons
- **Gameplay:** Research challenges, genetic puzzles

### Phase 4: Roguelike Features (Future)
- **Discovery:** Hidden genes, mutations, Research Lab
- **Challenges:** Scenarios, environmental constraints

### Phase 5: Advanced Systems (Long-Term)
- ECS conversion for performance
- Full save/load system
- Multiplayer experiments
- Procedural world generation

### Success Metrics
- **Technical:** 60 FPS (100+ plants, 50+ animals), <512MB RAM, <5s load
- **Content:** 50+ node types, 20+ animal species, 15+ tools, 10+ tile types
- **Engagement:** 70% first run completion, 5+ gene combos discovered/user

---

## Quick Reference

### Critical Sprint 0 Files
- **[`RunManager.cs`](Assets/Scripts/Core/RunManager.cs)** - State management (needs creation)
- **[`WeatherManager.cs`](Assets/Scripts/Battle/Plant/WeatherManager.cs)** - Time control integration
- **[`WaveManager.cs`](Assets/Scripts/Ecosystem/Core/WaveManager.cs)** - Threat wave management
- **[`UIManager.cs`](Assets/Scripts/UI/UIManager.cs)** - Phase UI panels (needs creation)

### Core System Entry Points
- **Plants:** [`PlantGrowth.cs`](Assets/Scripts/Battle/Plant/PlantGrowth.cs)
- **Animals:** [`AnimalController.cs`](Assets/Scripts/Ecosystem/Core/AnimalController.cs)
- **Tiles:** [`TileInteractionManager.cs`](Assets/Scripts/Tiles/Data/TileInteractionManager.cs)
- **Nodes:** [`NodeEditorGridController.cs`](Assets/Scripts/Nodes/UI/NodeEditorGridController.cs)
- **Tools:** [`ToolSwitcher.cs`](Assets/Scripts/Tiles/Tools/ToolSwitcher.cs)

### Performance Monitoring
- **Target:** 60 FPS with 100+ plants, 50+ animals
- **Current:** ~45 FPS with 50+ entities
- **Bottlenecks:** [`AnimalController.Update()`](Assets/Scripts/Ecosystem/Core/AnimalController.cs), plant visual effects, entity count

---

*This document serves as the definitive source of truth for Gene Garden Survivor development. Update as systems evolve and new features are implemented.*