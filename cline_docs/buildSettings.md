# Build Settings

**Last Updated:** 2025-05-29  
**Unity Version:** 6 (6000.0.39f1)  
**Build Version:** 1.0.0

## 🎯 Build Targets

### Primary Platform
- **Platform:** Windows 64-bit
- **API:** DirectX 11
- **Architecture:** x86_64
- **Minimum OS:** Windows 10
- **Target Framework:** .NET Standard 2.1

### Secondary Platforms
- **macOS**
  - Universal Build (Apple Silicon + Intel)
  - Metal Graphics API
  - macOS 10.15+

- **Linux**
  - x86_64 Architecture
  - Vulkan/OpenGL Graphics API
  - Ubuntu 20.04+ / SteamOS

## 🔧 Build Configuration

### Development Build
```json
{
  "Development Build": true,
  "Allow Debugging": true,
  "Wait For Managed Debugger": false,
  "Script Debugging": true,
  "Profiler": true,
  "Deep Profiling": false
}
```

### Release Build
```json
{
  "Development Build": false,
  "Strip Engine Code": true,
  "Optimize Mesh Data": true,
  "IL2CPP": false,
  "Mono": true,
  "Compression Method": "LZ4"
}
```

## 🎨 Graphics Settings

### Quality Levels
1. **Ultra (Default)**
   - Anti-aliasing: Disabled (pixel art style)
   - Texture Quality: Full
   - Shadows: Disabled (2D game)
   - Post Processing: Full (Night color effects, water reflections)

2. **High**
   - Anti-aliasing: Disabled
   - Texture Quality: Full
   - Shadows: Disabled
   - Post Processing: Medium

3. **Medium**
   - Anti-aliasing: Disabled
   - Texture Quality: Full
   - Shadows: Disabled
   - Post Processing: Low

4. **Low**
   - Anti-aliasing: Disabled
   - Texture Quality: Half
   - Shadows: Disabled
   - Post Processing: Disabled

### URP Asset Settings
```json
{
  "Render Scale": 1,
  "Main Light": "Disabled",
  "Cast Shadows": false,
  "Soft Shadows": false,
  "Additional Lights": 0,
  "HDR": true,
  "MSAA": "Disabled",
  "Render Pipeline": "UniversalRP"
}
```

## 📦 Asset Bundle Configuration

### Bundle Settings
- **Compression:** LZ4
- **Include In Build:** Core assets only
- **Load Type:** On Demand
- **Bundle Identifier:** Hash

### Bundle Groups
1. **Core Assets**
   - Essential systems (PlantGrowth, WeatherManager)
   - Core UI elements
   - Input System actions

2. **Node System Assets**
   - NodeDefinition ScriptableObjects
   - Node effect configurations
   - NodeDefinitionLibrary assets

3. **Ecosystem Assets**
   - AnimalDefinition ScriptableObjects
   - ScentDefinition assets
   - FoodType definitions
   - ThoughtLibrary configurations

4. **Environment Assets**
   - TileDefinition assets
   - Dual-grid rule tiles
   - ToolDefinition configs
   - WaveDefinition assets

5. **Visual Effects**
   - Firefly materials
   - Water reflection shaders
   - Plant shadow/outline prefabs
   - Post-processing profiles

## 🔒 Security Settings

### Code Protection
- **Scripting Backend:** Mono (for development)
- **Code Stripping:** Minimal (preserve reflection)
- **Anti-Cheat:** None (single-player focused)
- **Managed Stripping:** Disabled

### Data Protection
- **Player Prefs:** Standard Unity
- **Save Data:** JSON serialization
- **Asset Protection:** Standard
- **File I/O:** Standard Unity

## 🚀 Distribution Settings

### Standalone Build
- **DRM:** None
- **Auto-Updates:** Not implemented
- **Save Location:** AppData/LocalLow
- **Config:** Local settings
- **Resolution:** Pixel-perfect scaling

### Future Steam Build
- **Steam SDK:** TBD
- **DRM:** Steam (if needed)
- **Cloud Saves:** Planned
- **Achievements:** Planned
- **Workshop:** Potential node sharing

## 📊 Performance Settings

### Memory
- **Managed Heap:** 256MB (2D focused)
- **Job Thread Count:** Auto
- **GC Settings:** Standard
- **Memory Profile:** 2D Optimized

### Loading
- **Preload Assets:** Node definitions, core prefabs
- **Load Scene Async:** false (single scene focus)
- **Background Loading:** false
- **Asset Warmup:** ScriptableObjects

## 🔍 Debug Settings

### Development
- **Deep Profiling:** Optional (performance impact)
- **Stack Traces:** Full
- **Exception Handling:** Full
- **Log Level:** Verbose
- **Custom Debug:** FloraManager visualization, NodeGraph debugging

### Testing
- **Automated Tests:** PlayMode tests for critical systems
- **Performance Tests:** PlantGrowth scaling, Animal AI load
- **Coverage:** Core systems (PlantGrowth, EcosystemManager)
- **Validation:** NodeGraph integrity, asset references

## 📱 Input Settings

### Controls
- **Input System:** New Unity Input System
- **Default Scheme:** Keyboard/Mouse
- **Primary Actions:**
  - WASD/Arrow Keys: Player movement
  - Q/E: Tool switching
  - Left Click: Tool usage
  - Tab: Node editor toggle
  - Delete: Node deletion
  - Right Click: Context menus

### Customization
- **Rebinding:** Supported through InputSystem_Actions
- **Schemes:** Keyboard/Mouse only (current)
- **Action Maps:** Player, UI, NodeEditor
- **Devices:** Auto-detect

## 🌐 Localization Settings

### Languages
- **Default:** English
- **Supported:** English only (initial)
- **Future Plans:** Spanish, French, German
- **Text Sources:** ScriptableObject descriptions, UI text

### Implementation
- **String Tables:** ScriptableObject fields
- **Asset Loading:** Direct reference
- **Fonts:** Default Unity fonts
- **RTL Support:** Not needed

## 🎮 Specific Game Settings

### Pixel Perfect Setup
- **Reference Resolution:** 320x180
- **Assets PPU:** 16
- **Pixel Snapping:** Enabled
- **Crop Frame:** Disabled
- **Stretch Fill:** Disabled

### Ecosystem Settings
- **Max Animals:** 50 concurrent
- **Max Plants:** 100 concurrent
- **Update Frequency:** 60 FPS
- **Simulation Range:** Camera bounds + margin

### Node System
- **Max Nodes Per Graph:** 16 (UI grid limitation)
- **Effect Validation:** Runtime checking
- **Graph Serialization:** JSON format
- **Asset References:** ScriptableObject links

---

**Next Update:** When build configuration changes or optimization needs arise