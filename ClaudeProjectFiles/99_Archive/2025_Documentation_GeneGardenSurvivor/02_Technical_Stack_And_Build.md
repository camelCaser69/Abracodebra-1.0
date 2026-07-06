# 02_Technical_Stack_And_Build.md

**Synthesized:** 2025-05-31
**Project:** Gene Garden Survivor

Technical stack, Unity config, build, and performance details.

## 🎮 Unity Configuration

### Core
*   **Version:** 6 (6000.0.39f1)
*   **API Compatibility:** .NET Standard 2.1
*   **Scripting Backend:** Dev: Mono; Release: IL2CPP.
*   **Primary Target:** Windows 64-bit.
*   **Code Stripping (Release):** Minimal-Medium (preserve reflection).

### Graphics
*   **Pipeline:** Universal Render Pipeline (URP) with 2D Renderer.
    *   Settings: Main Light Disabled, No Cast Shadows, HDR True, MSAA Disabled.
*   **Color Space:** Linear.
*   **Post Processing:** Volume system (`NightColorPostProcess`).
*   **Pixel Perfect:** `PixelPerfectCamera` via `PixelPerfectSetup`.
    *   Ref Res: 320x180; Assets PPU: 16; Snapping: On; Upscale RT: On.

## 📦 Package Dependencies

### Unity
*   Universal RP, Input System, TextMesh Pro, 2D Sprite, 2D Animation, 2D Tilemap Editor, 2D Pixel Perfect, 2D Tilemap Extras.

### Third-Party
*   **DualGrid (com.skner.dualgrid):** Core tile system. In project.
*   **HueFolders:** Editor asset organization.

### Dev Tools
*   Unity Test Framework, Unity Profiler, Package Manager.

## 🛠️ Architecture
*   **Partial Classes:** e.g., `PlantGrowth`.
*   **Singletons:** Managers (e.g., `EcosystemManager`, `RunManager`).
*   **ScriptableObjects:** Data-driven config (Nodes, Animals, Tiles, etc.).
*   **Events:** C# `event Action<T>` (e.g., `ToolSwitcher.OnToolChanged`).
*   **State Machines:** `PlantGrowth`, `RunManager`.

## 🎨 Rendering & Visuals

### Sprites & Tiles
*   **PPU:** 16. Filter: Point. Compression: None/High Quality.
*   **Tilemaps:** DualGrid system. Sorting via `TileInteractionManager`.
*   **Custom Shaders:** `WaterReflection.shader`, `SpriteEmissiveUnlit.shader`, `BezierCurveAA.shader`, `Sprite-Lit-Default_OverlayCustom.shader`.

## ⚙️ Project Settings

### Physics2D
*   Standard gravity. Layers/Matrix for interactions. Discrete collision.

### Input
*   `InputSystem_Actions.inputactions`. Maps: Player, UI. Scheme: Keyboard/Mouse.

### Quality
*   Levels affect Post-Processing, Texture Res (Low). AA/Shadows Disabled.

### Tags & Layers
*   Tags: "Player", "Water". Sorting Layers: Default, Shadows, Objects, Outlines, UI.

## 🚀 Build & Distribution

### Targets
*   Primary: Win 64-bit (DX11). Secondary: macOS (Universal, Metal), Linux (x86_64, Vulkan).

### Configs
*   **Dev:** `Development Build` On, Debug On, Profiler On, Mono.
*   **Release:** `Development Build` Off, Strip Engine Code On, IL2CPP, LZ4. *(IL2CPP preferred over original doc's Mono for release)*.

### Asset Bundles
*   Planned, LZ4, On Demand. Groups: Core, Nodes, Ecosystem, Env, VFX.

### Data
*   Saves: JSON (AppData/LocalLow).

### Performance Goals
*   256MB Managed Heap. Single scene focus (async load false). Preload criticals.

### Localization
*   Initial: English. Future: Spanish, French, German (via SO string tables).