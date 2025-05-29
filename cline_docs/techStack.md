# Technical Stack Documentation

**Last Updated:** 2025-05-29  
**Project:** Abracodebra 2.0

## 🎮 Unity Configuration

### Core Setup
- **Unity Version:** 6 (6000.0.39f1)
- **API Compatibility:** .NET Standard 2.1
- **Scripting Backend:** Mono (Development) / IL2CPP (Release)
- **Build Target:** Standalone Windows/Mac/Linux
- **Code Stripping:** Minimal (preserves reflection for ScriptableObjects)

### Graphics Pipeline
- **Render Pipeline:** Universal Render Pipeline (URP)
- **2D Renderer:** URP 2D Renderer with custom shaders
- **Color Space:** Linear
- **HDR:** Enabled for post-processing effects
- **Post Processing:** Volume system with NightColorPostProcess
- **Pixel Perfect:** PixelPerfectCamera with 320x180 reference resolution

## 📦 Package Dependencies

### Core Unity Packages
- **Universal RP:** Latest compatible with Unity 6
- **Input System:** New Input System for tool switching and player controls
- **TextMesh Pro:** UI text rendering and thought bubbles
- **2D Packages:**
  - 2D Sprite (Core sprite rendering)
  - 2D Animation (Animal and effect animations)
  - 2D Tilemap Editor (Dual-grid tile system)
  - 2D Tilemap Extras (Additional tilemap functionality)

### Third-Party Packages
- **DualGrid:** com.skner.dualgrid
  - Wang tile implementation for seamless terrain
  - Data/Render tilemap separation
  - Custom rule tile system integration
- **HueFolders:** Asset organization with color-coded folders

### Development Tools
- **Unity Test Framework:** Unit and PlayMode testing
- **Visual Studio Code:** Primary IDE with Unity integration
- **Unity Package Manager:** Automatic dependency resolution

## 🛠️ Architecture Patterns

### Code Organization
- **Partial Classes:** PlantGrowth split into Cell, Growth, NodeExecution
- **Singleton Pattern:** Manager classes (EcosystemManager, TileInteractionManager)
- **ScriptableObject Data:** Heavy use for configuration and content
- **Component Architecture:** Modular systems with clear separation
- **Event-Driven:** ToolSwitcher events, WeatherManager phase changes

### Asset Pipeline
- **Auto-Generation:** Editor scripts for node creation and library management
- **Data Validation:** Runtime checking for ScriptableObject references
- **Library Pattern:** Central collections for all content types
- **Reference Cloning:** Deep copying for runtime instance isolation

## 🎨 Rendering Configuration

### Texture & Sprite Settings
- **Pixels Per Unit:** 16 (consistent across all sprites)
- **Filter Mode:** Point (pixel art preservation)
- **Compression:** None (quality preservation)
- **Max Size:** 2048x2048
- **Import Settings:** Sprite mode with grid slicing support

### Tilemap Configuration
- **Dual-Grid System:** Data tilemap drives render tilemap
- **Wang Tiles:** 4x4 tilesheet format with context-aware connections
- **Sorting Order:** Base sorting (0) with negative offsets for depth
- **Collision:** Optional sprite-based colliders for water masking

### Visual Effects
- **Custom Shaders:**
  - WaterReflectionGradient for distance-based fading
  - SpriteEmissiveUnlit for firefly glow effects
  - BezierCurveAA for smooth line rendering
- **Post-Processing:** Dynamic color adjustments, film grain, vignette
- **Dynamic Lighting:** Light2D components for firefly effects

## 🔧 Project Settings

### Physics2D Configuration
- **Gravity:** -9.81 (standard)
- **Default Material:** Friction 0.4, Bounciness 0
- **Layer Collision Matrix:** Custom setup for ecosystem interactions
- **Collision Detection:** Discrete (performance optimized)

### Input System Setup
- **Input Actions Asset:** InputSystem_Actions.inputactions
- **Action Maps:**
  - Player: Movement, tool usage
  - UI: Node editor interactions
  - Tool: Tool switching and application
- **Control Schemes:** Keyboard/Mouse primary

### Quality Settings
- **Render Pipeline:** UniversalRP asset
- **Texture Quality:** Full resolution
- **Anti-Aliasing:** Disabled (pixel art style)
- **Shadow Quality:** Disabled (2D game)
- **Particle Raycast Budget:** 256
- **Soft Particles:** Disabled

## 🎥 Camera & Display

### Camera Configuration
- **Projection:** Orthographic
- **Pixel Perfect Camera:** Enabled with PixelPerfectSetup component
- **Reference Resolution:** 320x180 (base game resolution)
- **Assets PPU:** 16 (matches sprite import settings)
- **Pixel Snapping:** Enabled for crisp pixel art

### Rendering Features
- **2D Renderer Data:** Custom configuration for URP 2D
- **Sorting Layers:** Default, Shadows, Objects, Outlines, UI
- **Post-Processing:** Global volume with custom profiles
- **Render Scale:** 1.0 (no upscaling at render level)

## 📊 Performance Configuration

### Runtime Targets
- **Target FPS:** 60
- **Memory Budget:** 256MB (2D-focused)
- **Draw Call Limit:** 50 (2D batching optimized)
- **Max Entities:** 50 animals + 100 plants simultaneously
- **Physics Updates:** Fixed 50Hz timestep

### Optimization Settings
- **Object Pooling:** Planned for visual effects and projectiles
- **Batching:** Static and dynamic batching enabled
- **Occlusion Culling:** Disabled (2D single-scene focus)
- **LOD System:** Not applicable (2D pixel art)

### Build Optimization
- **Asset Bundle Loading:** On-demand for content expansion
- **Texture Streaming:** Disabled (small texture sizes)
- **Audio Compression:** Vorbis for music, PCM for short SFX
- **Script Compilation:** Assembly definitions for faster iteration

## 🔐 Platform Considerations

### Build Targets
- **Primary:** Windows 64-bit (DirectX 11)
- **Secondary:** macOS Universal (Metal), Linux 64-bit (Vulkan/OpenGL)
- **Future:** Potential mobile adaptation with UI scaling

### Platform-Specific Settings
- **Windows:** Full feature set, all rendering paths supported
- **macOS:** Metal rendering, universal binary for Apple Silicon + Intel
- **Linux:** Vulkan preferred, OpenGL fallback for compatibility

## 🧪 Development Workflow

### Version Control
- **Git LFS:** Enabled for binary assets (prefabs, scenes, textures)
- **Ignore Patterns:** Standard Unity + project-specific exclusions
- **Branch Strategy:** main/develop/feature with automated asset import

### Testing Framework
- **Unit Tests:** Core system validation (PlantGrowth, NodeGraph)
- **PlayMode Tests:** Integration testing for complex interactions
- **Performance Tests:** Entity scaling and memory leak detection
- **Editor Tests:** Asset validation and ScriptableObject integrity

### Debug Tools
- **Unity Profiler:** Memory, rendering, and script performance
- **Frame Debugger:** Visual effect and shader debugging
- **Custom Tools:** FloraManager visualization, ecosystem state inspection
- **Console Logging:** Configurable debug levels per system

## 🔍 Asset Management

### ScriptableObject Architecture
- **NodeDefinition:** Plant genetics configuration
- **AnimalDefinition:** Species behavior and stats
- **TileDefinition:** Environment tile properties
- **ScentDefinition:** Chemical signal configuration
- **ToolDefinition:** Player tool properties

### Library System
- **Auto-Population:** Editor scripts maintain library references
- **Validation:** Runtime checks for missing or null references
- **Organization:** Numbered naming convention (Node_000_, Animal_001_)
- **Dependencies:** Clear reference chains with fallback handling

### Asset Loading
- **Preloaded:** Core definitions and UI assets
- **Runtime:** Dynamic loading for expandable content
- **Addressables:** Planned for future content expansion
- **Memory Management:** Proper cleanup for runtime instances

## 🚀 Build Pipeline

### Development Builds
- **Scripting:** Mono backend for faster iteration
- **Debugging:** Full symbols and stack traces
- **Profiler:** Deep profiling support for optimization
- **Hot Reload:** Assembly reload for script changes

### Release Builds
- **Scripting:** IL2CPP for performance and security
- **Optimization:** Aggressive code stripping and size optimization
- **Distribution:** Compressed LZ4 for balanced size/speed
- **Platform:** Multi-platform simultaneous builds

---

**Next Review:** When upgrading Unity version or adding major technical dependencies