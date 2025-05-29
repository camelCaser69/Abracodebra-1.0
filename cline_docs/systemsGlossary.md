# Systems Glossary

**Last Updated:** 2025-05-29  
**Project:** Abracodebra 2.0

## 🌱 Plant Systems

### Node Graph System
A visual programming system for defining plant genetics and growth patterns.

- **Node Definition**: ScriptableObject that defines a single genetic trait
- **Node Effect**: Runtime behavior executed during plant growth
- **Node Library**: Collection of available genetic traits
- **Node Graph**: Connected network of nodes defining a plant's complete genetics
- **Node Execution**: Process of evaluating node effects during growth

### Plant Growth System
Manages the lifecycle and visual representation of plants.

- **Growth Stage**: Discrete step in plant development
- **Growth Manager**: Coordinates growth across all plants
- **Growth Pattern**: Specific arrangement of stems and leaves
- **Growth Rate**: Speed of development, affected by conditions
- **Growth State**: Current development status of a plant

## 🦊 Ecosystem Systems

### Animal AI
Decentralized artificial intelligence system for creature behavior.

- **Behavior Tree**: Decision-making structure
- **Thought Pattern**: Sequence of AI considerations
- **Diet Preference**: Food type and priority settings
- **Movement Pattern**: Wandering and navigation logic
- **State Machine**: Current behavior status

### Scent System
Propagation and detection of environmental signals.

- **Scent Type**: Category of environmental signal
- **Scent Radius**: Area of effect for a scent
- **Scent Strength**: Intensity of the signal
- **Scent Layer**: Specific type of scent information
- **Scent Response**: How entities react to scents

## 🗺️ Tile Systems

### Dual-Grid System
Two-layer tile system for ground and object placement.

- **Base Grid**: Primary ground tile layer
- **Object Grid**: Secondary interactive layer
- **Wang Tiles**: Context-aware tile connections
- **Tile State**: Current condition of a tile
- **Tile Modifier**: Effect applied to a tile

### Ground System
Manages soil conditions and environmental factors.

- **Soil Type**: Base ground material
- **Moisture Level**: Water content in soil
- **Fertility**: Growth-supporting capability
- **Temperature**: Local heat level
- **Modification**: Changes from tool use

## 🛠️ Tool Systems

### Tool Manager
Handles player interaction with the environment.

- **Tool Type**: Category of interaction
- **Tool State**: Current condition/charges
- **Tool Effect**: Impact on environment
- **Tool Range**: Area of influence
- **Tool Cooldown**: Usage limitations

### Interaction System
Processes player actions with the world.

- **Action Type**: Category of interaction
- **Action Target**: Affected object/area
- **Action Result**: Outcome of interaction
- **Action Chain**: Sequence of effects
- **Action Validation**: Input verification

## 🎨 Visual Systems

### Shader Effects
Custom rendering for unique visual styles.

- **Outline Shader**: Object boundary rendering
- **Water Reflection**: Surface water effects
- **Emission**: Glowing elements
- **Overlay**: Additional visual layers
- **Post-Processing**: Global visual effects

### Animation System
Handles visual movement and transitions.

- **Animation State**: Current motion status
- **Animation Blend**: Transition between states
- **Animation Event**: Timed triggers
- **Animation Layer**: Priority and masking
- **Animation Override**: Custom motion sets

## ⚙️ Core Systems

### Event System
Manages communication between systems.

- **Event Type**: Category of message
- **Event Data**: Transmitted information
- **Event Handler**: Response logic
- **Event Queue**: Message processing order
- **Event Filter**: Selective handling

### Save System
Handles persistence of game state.

- **Save Data**: Stored information
- **Save Format**: Data structure
- **Save Validation**: Data integrity checks
- **Save Version**: Compatibility tracking
- **Save Migration**: Format updates

## 🎮 Input Systems

### Input Manager
Processes player control signals.

- **Input Action**: Mapped control
- **Input State**: Current control status
- **Input Binding**: Control mapping
- **Input Context**: Control mode
- **Input Priority**: Control hierarchy

### Camera System
Manages game view and rendering.

- **Camera Mode**: View configuration
- **Camera Target**: Focus object/area
- **Camera Bounds**: Movement limits
- **Camera Effect**: Visual modifications
- **Camera Layer**: Render masking

## 🔧 Development Tools

### Debug Tools
Aids in development and testing.

- **State Inspector**: Runtime monitoring
- **Performance Monitor**: Resource tracking
- **Visual Debugger**: Graphical debugging
- **Log System**: Error tracking
- **Test Framework**: Automated testing

### Editor Tools
Custom Unity Editor extensions.

- **Node Editor**: Genetic design tool
- **Tile Editor**: Ground design tool
- **Preview System**: Pre-runtime visualization
- **Batch Tools**: Mass operations
- **Asset Processors**: Import automation

---

**Next Update:** When adding new systems or significantly modifying existing ones
