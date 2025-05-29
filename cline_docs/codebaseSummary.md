# Codebase Summary

**Last Updated:** 2025-05-29  
**Project Structure Version:** 1.0

## 📁 Folder Structure & Key Systems

### Assets/Scripts/Battle/
Plant lifecycle, combat, and environmental effects
- **Plant/**
  - `PlantGrowth.cs` - Main plant controller with partial classes
  - `PlantGrowth.Cell.cs` - Cell management and visual spawning
  - `PlantGrowth.Growth.cs` - Time-based growth coroutines
  - `PlantGrowth.NodeExecution.cs` - Mature cycle node execution
  - `PlantCell.cs` - Individual cell behavior and destruction tracking
  - `LeafData.cs` - Leaf state for regrowth mechanics
  - `WeatherManager.cs` - Day/night cycle with pause/time scaling
- **SpellProjectile.cs** - Projectile behavior with scent application
- **Status Effects/** - Burning and other status effect system

### Assets/Scripts/Ecosystem/
Living world simulation with interconnected systems
- **Core/**
  - `EcosystemManager.cs` - Central coordinator with ScentLibrary reference
  - `FaunaManager.cs` - Animal spawning with functional offset system
  - `FloraManager.cs` - Plant visualization (scent radii, poop absorption)
  - `AnimalController.cs` - Complex AI with diet, thoughts, slowdown zones
  - `WaveManager.cs` - Wave-based animal spawning with pause controls
  - `PoopController.cs` - Fertilizer mechanics with collider requirements
  - `ScentSource.cs` - Scent emission with definition references
  - `ThoughtBubbleController.cs` - AI thought display system
  - `SlowdownZone.cs` - Area-based speed modification

- **Animals/** - `AnimalDefinition.cs`, `AnimalLibrary.cs`
- **Food/** - `AnimalDiet.cs`, `FoodItem.cs`, `FoodType.cs`
- **Scents/** - `ScentDefinition.cs`, `ScentLibrary.cs`
- **Effects/** - `FireflyController.cs`, `FireflyManager.cs` with attraction lines

### Assets/Scripts/Nodes/
Visual node programming system for plant genetics
- **Core/**
  - `NodeDefinition.cs` - ScriptableObject with effect cloning
  - `NodeDefinitionLibrary.cs` - Collection with initial node configs
  - `NodeData.cs` - Runtime data with deletion flags
  - `NodeEffectData.cs` - Effect configuration with ScentDefinition refs
  - `NodeEffectType.cs` - Comprehensive enum of all effect types
  - `OutputNodeEffect.cs` - Projectile spawning with scent application

- **Runtime/**
  - `NodeExecutor.cs` - Plant spawning from UI graphs
  - `NodeGraph.cs` - Graph data structure

- **UI/**
  - `NodeEditorGridController.cs` - Main UI controller with dropdown system
  - `NodeCell.cs` - Individual cell logic with selection handling
  - `NodeView.cs` - Visual representation with tooltips
  - `NodeDraggable.cs` - Drag-and-drop functionality
  - Helper classes for selection and interaction

### Assets/Scripts/Player/
- `GardenerController.cs` - Player movement, tool integration, speed modifiers

### Assets/Scripts/Tiles/
Dual-grid tile system with tool interactions
- **Data/**
  - `TileInteractionManager.cs` - Central tile system with refill rules
  - `TileDefinition.cs` - Individual tile properties with auto-reversion
  - `TileInteractionLibrary.cs` - Rules for tool→tile transformations + refills
  - `PlantGrowthModifierManager.cs` - Tile-based growth speed/energy modifiers
  - `PlantPlacementManager.cs` - Seed planting with randomization
  - `PlayerTileInteractor.cs` - Player input bridge

- **Tools/**
  - `ToolDefinition.cs` - Tool properties with usage limits
  - `ToolSwitcher.cs` - Tool management with events and refill system
  - `ToolType.cs` - Tool enumeration

- **Editor/** - Custom editors for tile system

### Assets/Scripts/Visuals/
Rendering effects and visual enhancements
- `PlantOutlineController.cs` - Dynamic plant outlines
- `PlantShadowController.cs` - Plant shadows with distance fading
- `OutlinePartController.cs` - Individual outline parts
- `ShadowPartController.cs` - Individual shadow parts with fade
- `WaterReflection.cs` - Water surface reflections with masking
- `WaterReflectionManager.cs` - Global reflection settings
- `RuntimeCircleDrawer.cs` - Debug circle visualization
- `NightColorPostProcess.cs` - Dynamic post-processing for day/night
- `PixelPerfectSetup.cs` - Camera configuration for pixel art

### Assets/Scripts/Core/
- `SortableEntity.cs` - Automatic sprite sorting for 2D depth

### Assets/Editor/
Automated node system tools
- `NodeDefinitionAutoAdder.cs` - Auto-adds nodes to libraries
- `NodeDefinitionCreator.cs` - Creates numbered node assets
- `NodeDefinitionEditor.cs` - Custom inspector with large effect list
- `NodeDefinitionLibraryEditor.cs` - Library management with UPDATE button
- `NodeDefinitionPostprocessor.cs` - Auto-naming for node assets
- `NodeEffectDrawer.cs` - Custom property drawer for effects

## 🔄 Data Flow Architecture

### Plant Growth Pipeline
```
NodeGraph (UI) → PlantGrowth.InitializeAndGrow() → CalculateAndApplyStats() → 
GrowthCoroutine_TimeBased() → SpawnCellVisual() → PlantCell instances → 
Visual Effects (Shadows, Outlines) → Mature Cycle Execution
```

### Ecosystem Simulation Loop
```
WaveManager → FaunaManager → AnimalController → AnimalDiet → FoodItem consumption → 
PoopController → PlantGrowth fertilizer absorption → Leaf regrowth → ScentSource → 
Animal attraction/repulsion
```

### Tile Interaction Flow
```
Input → PlayerTileInteractor → ToolSwitcher → TileInteractionManager → 
TileDefinition lookup → PlantPlacementManager (for seeds) OR 
TileInteractionRule application → PlantGrowthModifierManager updates
```

### Node System Workflow
```
NodeDefinitionLibrary → NodeEditorGridController → NodeCell/NodeView → 
NodeDraggable → NodeExecutor.SpawnPlantFromUIGraph() → PlantGrowth initialization
```

## 🏗️ Key Architectural Patterns

### Partial Classes
- `PlantGrowth` split into logical concerns (Cell, Growth, NodeExecution)

### ScriptableObject Data
- Heavy use of ScriptableObjects for configuration
- Auto-population systems in Editor scripts
- Runtime cloning for instance isolation

### Event-Driven Communication
- ToolSwitcher events for UI updates
- WeatherManager phase changes
- Animal thought system

### Manager Singleton Pattern
- EcosystemManager, TileInteractionManager, PlantGrowthModifierManager
- Instance-based access with validation

### Component-Based Effects
- Visual effects as separate components (shadows, outlines)
- Modular tool system
- Drag-and-drop UI components

## 🔌 External Dependencies

### Unity Packages
- Universal RP (URP 2D Renderer)
- New Input System (InputSystem_Actions.inputactions)
- TextMesh Pro
- 2D Tilemap Extras

### Third-Party Packages
- **DualGrid** (com.skner.dualgrid) - Wang tile implementation
- **HueFolders** - Asset organization

## 🎮 Critical System Interactions

### Plant-Ecosystem Integration
- Plants emit ScentSources based on NodeEffects
- Animals consume FoodItems (plant berries/leaves)
- Poop fertilizer enables leaf regrowth
- Weather affects photosynthesis rates

### UI-Runtime Bridge
- NodeEditorGridController builds NodeGraphs
- NodeExecutor spawns real PlantGrowth instances
- PlantPlacementManager validates tile compatibility

### Tool-World Integration
- ToolSwitcher manages state and events
- TileInteractionManager processes tool actions
- PlantGrowthModifierManager applies tile effects
- Refill rules allow tool restoration

## 📊 Performance Considerations

### Critical Paths
1. **PlantGrowth.Update()** - Growth percentage UI updates
2. **AnimalController.Update()** - AI decision making, movement
3. **TileInteractionManager.Update()** - Hover detection, timed reversions
4. **Visual Effects LateUpdate()** - Shadow/outline synchronization

### Optimization Strategies
- Coroutine-based plant growth (time-distributed)
- Cached tile lookups with dictionaries
- Event-driven UI updates
- Pooled visual effects (circles, lines)

### Scaling Concerns
- Animal AI updates (50+ animals)
- Plant visual effects (100+ plants)
- Tile system performance (large maps)
- Node graph execution complexity

## 🧪 Testing Infrastructure

### Play Mode Tests
- Critical for PlantGrowth lifecycle
- Animal AI behavior validation
- Tool interaction verification

### Editor Tests
- Node definition validation
- Asset reference integrity
- ScriptableObject cloning

### Manual Testing Hooks
- Debug visualization toggles
- Performance profiling markers
- State inspection tools

## 📝 Development Patterns

### Code Organization
- Namespace consistency (no explicit namespaces used)
- Logical file grouping by system
- Clear separation of concerns

### Asset Management
- Auto-numbering for ScriptableObjects
- Library auto-population
- Reference validation systems

### Error Handling
- Null reference checking
- Component validation
- Graceful degradation

---

**Next Update:** When adding major systems or refactoring existing architecture