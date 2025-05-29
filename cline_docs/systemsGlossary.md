# Systems Glossary

**Last Updated:** 2025-05-29  
**Project:** Abracodebra 2.0

## 🌱 Plant Systems

### Node Graph System
Visual programming system for defining plant genetics and behaviors.

- **NodeDefinition**: ScriptableObject defining a genetic trait with effects and visuals
- **NodeEffectData**: Individual effect configuration with type, values, and scent references  
- **NodeEffectType**: Enum of all possible effects (passive growth traits, active mature effects)
- **NodeGraph**: Runtime collection of NodeData representing a complete plant genome
- **NodeData**: Runtime instance of a node with order index and deletion permissions
- **NodeDefinitionLibrary**: Collection of available nodes with initial spawn configurations

### Plant Growth System
Manages plant lifecycle from seed to mature organism.

- **PlantGrowth**: Main controller with partial classes (Cell, Growth, NodeExecution)
- **PlantCell**: Individual cell instances with type (Seed, Stem, Leaf, Fruit) and parent references
- **GrowthStep**: Pre-calculated growth sequence for time-based development
- **LeafData**: Tracking structure for leaf regrowth mechanics after consumption
- **Growth State**: Current phase (Initializing, Growing, GrowthComplete, Mature_Idle, Mature_Executing)
- **Cell Spacing**: 0.08f unit grid spacing for precise plant structure

### Plant Effects System
Visual and behavioral effects attached to plants.

- **PlantShadowController**: Dynamic shadow generation with distance fading
- **PlantOutlineController**: Dynamic outline generation tracking cell additions/removals
- **ScentSource**: Scent emission component with definition references and modifiers
- **OutputNodeEffect**: Projectile spawning component for active plant abilities

## 🦊 Ecosystem Systems

### Animal AI System
Decentralized intelligence for creature behavior and decision making.

- **AnimalController**: Main AI controller with diet, movement, thoughts, and speed modifiers
- **AnimalDefinition**: ScriptableObject defining species traits, visuals, and base stats
- **AnimalDiet**: Food preference system with satiation values and priority rankings
- **DietPreferenceSimplified**: Individual food type preference with priority and satiation
- **AnimalSpawnData**: Wave spawning configuration with rate multipliers and limits

### Thought & Communication System
AI personality and communication mechanics.

- **AnimalThoughtLibrary**: Collection of species-specific thought patterns
- **AnimalThoughtLine**: Individual thought with trigger conditions and text options
- **ThoughtTrigger**: Enum of AI states (Hungry, Eating, HealthLow, Fleeing, Pooping)
- **ThoughtBubbleController**: Visual thought display with lifetime and positioning

### Food & Consumption System
Resource management for ecosystem sustainability.

- **FoodType**: ScriptableObject defining food properties and categories
- **FoodItem**: Component marking objects as consumable with type reference
- **Consumption Flow**: FoodItem → AnimalDiet lookup → Satiation → Poop production
- **PoopController**: Fertilizer lifecycle with auto-cleanup and collision detection

### Scent System
Environmental communication through chemical signals.

- **ScentDefinition**: ScriptableObject defining scent properties and visual effects
- **ScentLibrary**: Central collection of all available scent types
- **ScentSource**: Component emitting scents with radius and strength modifiers
- **Scent Detection**: Animal AI uses scent sources for attraction/repulsion behaviors

## 🗺️ Tile & Environment Systems

### Dual-Grid Tile System
Two-layer tilemap system for seamless terrain and interactions.

- **DualGridTilemapModule**: Third-party component managing data/render tilemap pairs
- **TileDefinition**: ScriptableObject defining tile properties, colors, and behaviors
- **TileDefinitionMapping**: Links TileDefinition to DualGridTilemapModule in manager
- **Wang Tiles**: Context-aware tile connections for seamless terrain
- **Auto-Reversion**: Time-based tile state changes with configurable delays

### Tile Interaction System
Player tool interactions with environment modification.

- **TileInteractionManager**: Central processor for all tile modifications
- **TileInteractionRule**: Configuration for tool + tile → new tile transformations
- **TileInteractionLibrary**: Collection of transformation and refill rules
- **ToolRefillRule**: Configuration for tool restoration at specific tiles
- **Hover System**: Real-time tile detection with range validation

### Plant Placement System
Seed planting with environmental validation and randomization.

- **PlantPlacementManager**: Handles plant spawning with tile validation
- **Placement Validation**: Checks tile compatibility and existing plant conflicts
- **Position Randomization**: Offset placement within tile bounds for organic feel
- **Spawn Radius**: Configurable randomization distance with pixel-perfect snapping

### Growth Modifier System
Environmental effects on plant development and energy.

- **PlantGrowthModifierManager**: Applies tile-based modifiers to plant stats
- **TileGrowthModifier**: Configuration linking tiles to speed/energy multipliers
- **Growth Speed Multiplier**: Affects time between growth steps
- **Energy Recharge Multiplier**: Affects photosynthesis and energy accumulation
- **Tile Update Tracking**: Monitors plant position changes for modifier updates

## 🛠️ Tool Systems

### Tool Management
Player interaction tools with usage limits and state management.

- **ToolDefinition**: ScriptableObject defining tool properties, icons, and usage limits
- **ToolType**: Enum of available tools (Hoe, WateringCan, SeedPouch)
- **ToolSwitcher**: Manager for tool selection, usage tracking, and events
- **Usage Limits**: Configurable limited uses with refill mechanics
- **Tool Events**: OnToolChanged and OnUsesChanged for UI integration

### Tool Interaction Processing
Handles tool application to environment with validation.

- **PlayerTileInteractor**: Input handler requiring ToolSwitcher component
- **Tool Application**: Hover validation → Use consumption → Effect application
- **Refill Mechanics**: Water tiles refill WateringCan, other rules configurable
- **Range Validation**: Distance checks and target tile validation

## 🌤️ Weather & Time Systems

### Weather Management
Day/night cycle with environmental effects.

- **WeatherManager**: Central time controller with phase transitions
- **CyclePhase**: Enum (Day, TransitionToNight, Night, TransitionToDay)
- **Sun Intensity**: 0-1 value affecting photosynthesis and visual effects
- **Time Scaling**: Configurable speed multiplier with pause functionality
- **Phase Events**: OnPhaseChanged event for system synchronization

### Environmental Effects
Systems responding to weather and time changes.

- **Photosynthesis**: Plant energy generation based on sunlight and firefly proximity
- **Animal Spawning**: Fireflies spawn at night, other animals via wave system
- **Visual Effects**: Post-processing changes based on sun intensity
- **Growth Rates**: Weather affects plant development speed

## 🎨 Visual Systems

### Dynamic Visual Effects
Real-time visual enhancements for plant representation.

- **Shadow System**: PlantShadowController with configurable angle, squash, and distance fade
- **Outline System**: PlantOutlineController with cell tracking and exclusion rules
- **Water Reflection**: WaterReflection with gradient fading and water masking
- **Runtime Visualization**: Debug circles and lines for scent radii and firefly attraction

### Shader & Material System
Custom rendering for unique visual styles.

- **Water Reflection Shader**: Custom gradient fading based on distance from origin
- **Emissive Materials**: Glowing effects for fireflies and special elements
- **Post-Processing**: NightColorPostProcess for day/night visual transitions
- **Pixel Perfect**: PixelPerfectSetup for consistent pixel art rendering

### Sorting & Layering
Depth management for 2D rendering.

- **SortableEntity**: Automatic sprite sorting based on Y position
- **Sorting Layers**: Configured layers for shadows, main objects, outlines, UI
- **Sorting Order**: Automatic assignment based on tilemap priority and object hierarchy
- **Parent Y Coordinate**: Option for child objects to use parent position for sorting

## ⚙️ Core Systems

### Ecosystem Management
Central coordination of all living systems.

- **EcosystemManager**: Singleton coordinator with library references
- **FaunaManager**: Animal spawning with functional offset and bounds management
- **FloraManager**: Plant visualization with debug toggles for scent/poop radius
- **Wave System**: WaveManager with pause controls and spawning coordination

### Spawning & Population Control
Management of entity creation and lifecycle.

- **WaveDefinition**: ScriptableObject defining spawn patterns and timing
- **WaveSpawnEntry**: Individual spawn configuration with delays and locations
- **WaveSpawnLocationType**: Enum (GlobalSpawnArea, RandomNearPlayer, Offscreen)
- **Population Limits**: Configurable maximum entities per type

### State Management
Persistent data and configuration systems.

- **ScriptableObject Architecture**: Heavy use for data-driven configuration
- **Library Pattern**: Central collections (NodeDefinitionLibrary, ScentLibrary, etc.)
- **Auto-Population**: Editor scripts automatically update libraries
- **Reference Validation**: Runtime checks for missing or null references

## 🎮 Input & UI Systems

### Input Management
Player control processing and tool interaction.

- **Input System**: New Unity Input System with InputSystem_Actions asset
- **Control Mapping**: WASD movement, Q/E tool switching, Tab node editor
- **Context Switching**: Different input maps for gameplay vs UI modes
- **Tool Integration**: Direct input processing through PlayerTileInteractor

### Node Editor UI
Visual programming interface for plant genetics.

- **NodeEditorGridController**: Main UI controller with grid-based layout
- **NodeCell**: Individual cell logic with selection and drop handling
- **NodeView**: Visual representation with tooltips and effect display
- **NodeDraggable**: Drag-and-drop functionality with canvas group management
- **Auto-Layout**: Automatic cell positioning and dropdown menu handling

### User Interface Integration
UI elements responding to game state.

- **Tool UI**: Real-time display of current tool and remaining uses
- **Growth UI**: Plant growth percentage display with continuous/discrete modes
- **Debug UI**: Hover tile information and system status displays
- **Selection System**: Node selection with highlight states and keyboard shortcuts

## 🔧 Development Tools

### Editor Automation
Custom Unity Editor tools for asset management.

- **NodeDefinitionAutoAdder**: Automatically adds new nodes to libraries
- **NodeDefinitionCreator**: Creates auto-numbered node assets
- **NodeDefinitionPostprocessor**: Auto-renames and organizes node assets
- **NodeEffectDrawer**: Custom property drawer for effect configuration
- **Library Management**: UPDATE buttons for refreshing ScriptableObject collections

### Debug & Visualization Tools
Development aids for system monitoring and debugging.

- **Runtime Circle Drawer**: Dynamic circle visualization for radii and ranges
- **Scent Radius Visualization**: Toggle-able display of scent source ranges
- **Poop Absorption Visualization**: Debug display for fertilizer detection ranges
- **Firefly Attraction Lines**: Visual lines showing firefly-scent attraction
- **Performance Monitoring**: Built-in debug logging and state inspection

### Asset Management
Organization and validation systems for project assets.

- **HueFolders**: Asset organization with color-coded folder system
- **Auto-Numbering**: Consistent naming for ScriptableObject assets
- **Reference Tracking**: Validation systems for asset dependencies
- **Assembly Definitions**: Logical grouping for faster compilation

---

**Next Update:** When adding new systems or significantly modifying existing ones