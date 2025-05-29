# Technical Stack Documentation

**Last Updated:** 2025-05-29  
**Project:** Abracodebra 2.0

## 🎮 Unity Configuration

### Core Setup
- **Unity Version:** 6 (6000.0.39f1)
- **API Compatibility:** .NET Standard 2.1
- **Scripting Backend:** Mono
- **Build Target:** Standalone Windows/Mac/Linux

### Graphics Pipeline
- **Render Pipeline:** Universal Render Pipeline (URP)
- **2D Renderer:** URP 2D Renderer
- **Color Space:** Linear
- **HDR:** Enabled for post-processing
- **Post Processing:** Volume system with custom profiles

## 📦 Key Packages

### Core Packages
- **Universal RP:** Latest compatible version
- **Input System:** New Input System for tool and player controls
- **TextMesh Pro:** UI text rendering
- **2D Packages:**
  - 2D Sprite
  - 2D Animation
  - 2D IK
  - 2D Tilemap Editor

### Custom Packages
- **DualGrid:** com.skner.dualgrid (Wang tile system)
- **HueFolders:** Asset organization and folder coloring

### Development Tools
- **Unity Test Framework:** Unit testing capability
- **Visual Studio Code:** Primary IDE
- **Unity VS Code Package:** Editor integration

## 🛠️ Build Configuration

### Target Platforms
- **Primary:** Windows 64-bit
- **Secondary:** macOS, Linux
- **Future:** Potential mobile/web builds

### Build Settings
- **Compression:** LZ4
- **Stripping Level:** Minimal
- **Scripting Define Symbols:**
  - UNITY_POST_PROCESSING_STACK_V2
  - ENABLE_INPUT_SYSTEM

## 🎨 Asset Pipeline

### Texture Settings
- **Default Import Settings:**
  - Pixel Per Unit: 16
  - Filter Mode: Point
  - Compression: None
  - Max Size: 2048

### Animation Settings
- **Default Import Settings:**
  - Legacy Animation Support: False
  - Import Animation: True
  - Import Animation Legacy: False

### Audio Settings
- **Sample Rate:** 44100 Hz
- **Format:** Compressed
- **Load Type:** Decompress on Load

## 🔧 Project Settings

### Physics2D
- **Default Material:** Friction: 0.4, Bounciness: 0
- **Gravity:** -9.81
- **Layer Collision Matrix:** Custom for ecosystem interactions

### Input System
- **Input Actions Asset:** InputSystem_Actions
- **Default Scheme:** Keyboard/Mouse
- **Action Maps:**
  - Player Controls
  - Tool System
  - UI Navigation

### Quality Settings
- **Pixel Light Count:** 4
- **Texture Quality:** Full Res
- **Anti Aliasing:** Disabled (pixel art)
- **Soft Particles:** Disabled

## 🎥 Camera & Display

### Camera Setup
- **Main Camera:** Orthographic
- **Pixel Perfect Camera:** Enabled
- **Reference Resolution:** 320x180
- **Assets Pixel Per Unit:** 16

### Display Settings
- **Target FPS:** 60
- **VSync:** Enabled
- **Resolution:** Dynamic scaling supported

## 🔍 Development Tools

### Debug Tools
- Unity Profiler
- Frame Debugger
- Physics2D Debugger
- Memory Profiler

### Custom Tools
- Node Graph Editor
- Plant Growth Visualizer
- Ecosystem State Inspector

## 🔐 Version Control

### Git Configuration
- **LFS:** Enabled for binary assets
- **Ignore Patterns:** Standard Unity + custom
- **Branch Strategy:** main/develop/feature

## 📊 Performance Targets

### Runtime
- **Target FPS:** 60
- **Max Draw Calls:** 100
- **Max Batch Count:** 50
- **Memory Budget:** 512MB

### Load Times
- **Initial Load:** < 5 seconds
- **Scene Load:** < 2 seconds
- **Asset Load:** < 100ms

---

**Next Review:** When adding new packages or changing core configurations
