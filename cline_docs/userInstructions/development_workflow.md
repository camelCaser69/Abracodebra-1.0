# Development Workflow Guide

**Last Updated:** 2025-05-29  
**Project:** Abracodebra 2.0

## 🚀 Getting Started

### Initial Setup
1. **Clone Repository**
   ```bash
   git clone [repository-url]
   cd Abracodebra-1.0
   ```

2. **Unity Setup**
   - Open Unity Hub
   - Install Unity 6 (6000.0.39f1) if not present
   - Open project folder
   - Wait for initial package import and asset processing

3. **Scene Verification**
   - Open `Assets/Scenes/MainScene.unity`
   - Verify all manager references are assigned
   - Check Input System is in Both mode (not Legacy)
   - Ensure Post-Processing Volume is active

## 🔄 Daily Development Cycle

### 1. Project Sync
```bash
# Pull latest changes
git pull origin main

# Check for package updates
# Unity Package Manager will prompt for updates if available
```

### 2. Scene and System Validation
1. **Manager Health Check**
   - EcosystemManager: ScentLibrary assigned
   - TileInteractionManager: TileDefinitionMappings populated
   - PlantGrowthModifierManager: TileGrowthModifiers configured
   - WeatherManager: Fade sprite and curve assigned

2. **Input System Check**
   - Verify InputSystem_Actions.inputactions is generated
   - Test tool switching (Q/E keys)
   - Validate node editor toggle (Tab key)

3. **Visual System Check**
   - Post-Processing Volume enabled
   - PixelPerfectCamera configured (320x180)
   - URP 2D Renderer asset assigned

### 3. Development Loop
1. **Code Changes**
   - Follow existing partial class structure for PlantGrowth
   - Use consistent naming (PascalCase classes, camelCase fields)
   - Add [SerializeField] for private fields exposed in Inspector
   - Update XML documentation for public APIs

2. **Testing Protocol**
   - Enter Play mode for immediate testing
   - Use Unity Test Runner for automated tests
   - Profile performance with Unity Profiler
   - Test edge cases and boundary conditions

3. **Commit Process**
   ```bash
   git add .
   git commit -m "feat: add new node effect type for healing"
   git push origin feature/healing-nodes
   ```

## 🛠️ Common Development Tasks

### Adding New Plant Node Effects

#### 1. Define New Effect Type
```csharp
// In NodeEffectType.cs
public enum NodeEffectType 
{
    // ... existing effects
    [Tooltip("Heals nearby damaged plants over time.")]
    HealingAura,
}
```

#### 2. Create NodeDefinition Asset
1. Right-click in `Assets/Scriptable Objects/Nodes Plant/`
2. Create → Nodes → Node Definition (Auto-Named)
3. Configure display name, description, thumbnail
4. Set up effects with new HealingAura type
5. Library auto-updates via NodeDefinitionAutoAdder

#### 3. Implement Effect Logic
```csharp
// In PlantGrowth.NodeExecution.cs ExecuteMatureCycle()
case NodeEffectType.HealingAura:
    TriggerHealingEffect(effect.primaryValue, effect.secondaryValue);
    break;
```

#### 4. Test and Validate
- Place node in editor grid
- Plant test specimen
- Verify effect execution during mature cycles
- Check performance impact with multiple plants

### Creating New Animal Species

#### 1. Create Animal Definition
1. Right-click in `Assets/Scriptable Objects/Animals/`
2. Create → Ecosystem → Animal Definition (Simplified)
3. Configure name, health, speed, visuals

#### 2. Set Up Diet Preferences
1. Create new AnimalDiet in `Assets/Scriptable Objects/Animals Diet/`
2. Configure acceptable foods and satiation values
3. Link diet to animal definition

#### 3. Add Thought Patterns
1. Open AnimalThoughtLibrary asset
2. Add new entries for species with triggers and text
3. Test thought display in play mode

#### 4. Create Species Prefab
1. Duplicate existing animal prefab
2. Replace sprites and animations
3. Configure AnimalController parameters
4. Test spawning via WaveDefinition

### Implementing New Tool Types

#### 1. Extend Tool Enum
```csharp
// In ToolType.cs
public enum ToolType
{
    // ... existing tools
    Fertilizer, // New tool type
}
```

#### 2. Create Tool Definition
1. Create new ToolDefinition asset
2. Configure icon, usage limits, display name
3. Add to ToolSwitcher.toolDefinitions array

#### 3. Add Interaction Rules
1. Open TileInteractionLibrary
2. Add new TileInteractionRule entries
3. Configure tool → tile transformations
4. Add refill rules if applicable

#### 4. Test Tool Functionality
- Verify tool switching works
- Test tile transformations
- Validate usage limits and refills
- Check visual feedback

### Modifying Tile System

#### 1. Create New Tile Definition
1. Create TileDefinition asset
2. Configure display name, colors, properties
3. Set up auto-reversion if needed
4. Mark as water tile if applicable

#### 2. Set Up Dual-Grid Integration
1. Create new DualGridRuleTile asset
2. Import and slice 4x4 tilesheet
3. Auto-generate tiling rules
4. Add to TileInteractionManager mappings

#### 3. Configure Growth Modifiers
1. Add entry to PlantGrowthModifierManager
2. Set growth speed and energy multipliers
3. Test plant behavior on new tile type

#### 4. Add Tool Interactions
1. Create transformation rules in TileInteractionLibrary
2. Test tool effects on new tile
3. Verify visual feedback and state changes

## 🎮 Testing Protocols

### Quick Testing (Every Change)
1. **Enter Play Mode**
   - Verify no console errors
   - Test primary functionality
   - Check frame rate stability

2. **Core Systems Check**
   - Plant placement and growth
   - Animal spawning and behavior
   - Tool switching and usage
   - Node editor functionality

### Comprehensive Testing (Before Commits)
1. **Automated Tests**
   ```csharp
   // Run in Unity Test Runner
   Window → General → Test Runner
   // Execute PlayMode tests for integration
   ```

2. **Performance Testing**
   - Spawn 20+ animals, 30+ plants
   - Monitor frame rate and memory usage
   - Use Unity Profiler for bottleneck detection
   - Test for 5+ minutes continuous play

3. **Edge Case Testing**
   - Fill node editor grid completely
   - Place plants on all tile types
   - Test tool usage until depletion
   - Verify cleanup on scene reload

### Stress Testing (Weekly)
1. **Entity Scaling**
   - 50+ animals concurrent
   - 100+ plants with active effects
   - Multiple complex node graphs
   - Extended play sessions (30+ minutes)

2. **System Integration**
   - All systems active simultaneously
   - Complex interaction chains
   - Resource exhaustion scenarios
   - Error recovery testing

## 🐛 Debugging Strategies

### Unity Debug Tools
1. **Console Analysis**
   - Filter by error type and system
   - Use Debug.isDebugBuild for conditional logging
   - Stack trace analysis for null reference tracking

2. **Profiler Usage**
   - Memory profiler for leak detection
   - CPU profiler for Update() loop optimization
   - Rendering profiler for draw call analysis

3. **Frame Debugger**
   - Visual effect rendering order
   - Shader property validation
   - Batch analysis for performance

### Custom Debug Tools
1. **FloraManager Visualization**
   - Toggle scent radius display
   - Show poop absorption ranges
   - Real-time state inspection

2. **Ecosystem State Inspection**
   - Animal AI state display
   - Food availability mapping
   - Population balance monitoring

3. **Node Graph Debugging**
   - Effect execution tracing
   - Runtime value inspection
   - Performance impact measurement

### Common Issues and Solutions

#### Plant System Issues
```csharp
// Race condition in cell destruction
if (cells.ContainsKey(coord) && activeCellGameObjects.Contains(cellGO))
{
    // Safe removal with validation
}
```

#### Animal AI Problems
- **Stuck at boundaries**: Check boundsOffset in FaunaManager
- **Food seeking failures**: Validate AnimalDiet configuration
- **Thought system errors**: Ensure AnimalThoughtLibrary completeness

#### Tool System Glitches
- **State desync**: Verify ToolSwitcher event connections
- **Refill failures**: Check TileInteractionLibrary refill rules
- **Usage tracking**: Validate limited uses configuration

#### Visual Effect Issues
- **Memory leaks**: Ensure proper cleanup in OnDestroy()
- **Performance drops**: Profile shadow/outline Update() loops
- **Sorting problems**: Check SortableEntity configuration

## 📦 Building and Deployment

### Development Builds
```bash
# Unity Build Settings
File → Build Settings
Platform: PC, Mac & Linux Standalone
Target Platform: Windows/Mac/Linux
Architecture: x86_64

Development Build: ✓
Script Debugging: ✓
```

### Release Builds
```bash
# Optimized settings
Development Build: ✗
IL2CPP Scripting Backend: ✓
Strip Engine Code: ✓
Managed Stripping Level: Medium
```

### Build Validation
1. **Functionality Test**
   - All core systems operational
   - No console errors or warnings
   - Input system responsive
   - Visual effects working

2. **Performance Validation**
   - 60 FPS with moderate entity count
   - Memory usage under 256MB
   - Load times under 5 seconds

## 🔍 Code Review Guidelines

### Before Submitting Changes
1. **Code Quality**
   - Remove debug code and console logs
   - Update documentation comments
   - Follow existing code style
   - Add null reference checks

2. **Testing Evidence**
   - Screenshot/video of functionality
   - Performance impact measurement
   - Edge case testing results
   - Integration testing validation

3. **Asset Management**
   - Proper ScriptableObject configuration
   - Library updates where needed
   - Asset reference validation
   - Scene integrity check

### Review Process
1. **Create Pull Request**
   - Clear description of changes
   - Link to related issues
   - Include testing evidence
   - Request specific reviewers

2. **Code Review Focus**
   - Performance implications
   - Integration impact
   - Code maintainability
   - Asset organization

## 📝 Documentation Maintenance

### When to Update Documentation
- **Adding new systems or major features**
- **Changing existing APIs or workflows**
- **Performance optimizations or architectural changes**
- **New tools or development processes**

### Documentation Types to Maintain
1. **XML Comments**: Public API documentation
2. **README Updates**: High-level feature changes
3. **Architecture Diagrams**: System interaction changes
4. **Workflow Guides**: Process improvements

### Documentation Standards
- Clear, concise language
- Code examples for complex features
- Performance considerations noted
- Update dates and version tracking

---

**Next Update:** When development processes or tools significantly change