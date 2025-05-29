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
  "Profiler": true
}
```

### Release Build
```json
{
  "Development Build": false,
  "Strip Engine Code": true,
  "Optimize Mesh Data": true,
  "IL2CPP": true,
  "Compression Method": "LZ4"
}
```

## 🎨 Graphics Settings

### Quality Levels
1. **Ultra**
   - Anti-aliasing: Disabled (pixel art)
   - Texture Quality: Full
   - Shadows: Disabled
   - Post Processing: Full

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
   - Texture Quality: Full
   - Shadows: Disabled
   - Post Processing: Minimal

### URP Asset Settings
```json
{
  "Render Scale": 1,
  "Main Light": "Disabled",
  "Cast Shadows": false,
  "Soft Shadows": false,
  "Additional Lights": 0,
  "HDR": true
}
```

## 📦 Asset Bundle Configuration

### Bundle Settings
- **Compression:** LZ4
- **Include In Build:** false
- **Load Type:** On Demand
- **Bundle Identifier:** Hash

### Bundle Groups
1. **Core Assets**
   - Essential game data
   - Always included

2. **Plant Assets**
   - Node definitions
   - Growth patterns
   - Visual assets

3. **Animal Assets**
   - Behavior definitions
   - Animation data
   - Sound effects

4. **Environment**
   - Tile sets
   - Background elements
   - Weather effects

## 🔒 Security Settings

### Code Protection
- **IL2CPP Scripting Backend**
- **Code Stripping: Medium**
- **Anti-Cheat: Basic**
- **Managed Stripping: Medium**

### Data Protection
- **Encrypted Player Prefs**
- **Obfuscated Save Data**
- **Protected Assets**
- **Secure File I/O**

## 🚀 Distribution Settings

### Steam Build
- **Steam SDK:** Latest
- **DRM:** Steam
- **Cloud Saves:** Enabled
- **Achievements:** Enabled
- **Workshop:** Planned

### Standalone Build
- **DRM:** None
- **Auto-Updates:** Built-in
- **Save Location:** AppData
- **Config:** Local

## 📊 Performance Settings

### Memory
- **Managed Heap:** 512MB
- **Job Thread Count:** Auto
- **GC Settings:** Incremental
- **Memory Profile:** Balanced

### Loading
- **Preload Assets:** Minimal
- **Load Scene Async:** true
- **Background Loading:** true
- **Asset Warmup:** true

## 🔍 Debug Settings

### Development
- **Deep Profiling:** Optional
- **Stack Traces:** Full
- **Exception Handling:** Full
- **Log Level:** Verbose

### Testing
- **Automated Tests:** Enabled
- **Performance Tests:** Optional
- **Coverage:** Basic
- **Validation:** Full

## 📱 Input Settings

### Controls
- **Input System:** New
- **Default Scheme:** Keyboard/Mouse
- **Fallback:** Classic Input
- **Touch:** Supported

### Customization
- **Rebinding:** Supported
- **Schemes:** Multiple
- **Action Maps:** Modular
- **Devices:** Auto-detect

## 🌐 Localization Settings

### Languages
- **Default:** English
- **Supported:** Planned
- **Asset Bundles:** Per Language
- **Text Assets:** Tagged

### Implementation
- **String Tables:** Scriptable Objects
- **Asset Loading:** Dynamic
- **Fonts:** Dynamic
- **RTL Support:** Planned

---

**Next Update:** When build configuration changes or new platforms are added
