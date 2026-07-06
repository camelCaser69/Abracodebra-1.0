# 06_Assets_And_Editor_Tools.md

**Synthesized:** 2025-05-31
**Project:** Gene Garden Survivor

Overview of key ScriptableObjects, prefabs, and custom editor tools.

## 📦 Key ScriptableObject Assets
Located under `Assets/Scriptable Objects/`.

### Plant & Node System
*   **`NodeDefinition`**: `Nodes Plant/`. Defines genetic traits. `displayName`, `description`, `thumbnail`, `effects (List<NodeEffectData>)`. Ex: `Node_000_Seed.asset`.
*   **`NodeDefinitionLibrary`**: `Nodes Plant/NodeDefinitionLibrary.asset`. Collection of `NodeDefinition`s. `initialNodes (List<InitialNodeConfig>)` for editor.
*   **`ScentDefinition`**: `Scents/`. Scent properties: `scentID`, `displayName`, `baseRadius`, `baseStrength`, `particleEffectPrefab`. Ex: `Scent_000_FireflyPheromone.asset`.
*   **`ScentLibrary`**: `Scents/ScentLibrary.asset`. All `ScentDefinition`s. Ref by `EcosystemManager`.

### Ecosystem
*   **`AnimalDefinition`**: `Animals/`. Species traits: `animalName`, `maxHealth`, `movementSpeed`, `diet (AnimalDiet)`, `prefab`. Ex: `Animal_000_Bunny.asset`.
*   **`AnimalLibrary`**: `Animals/AnimalLibrary.asset`. Collection.
*   **`AnimalDiet`**: `Animals Diet/`. Defines food preferences: `acceptableFoods (List<DietPreferenceSimplified>)`, hunger stats. Ex: `Diet_000_Bunny.asset`.
*   **`FoodType`**: `Food/`. Food properties: `foodName`, `icon`, `category`. Ex: `FoodType_000_Berry.asset`.
*   **`AnimalThoughtLibrary`**: `Life Thoughts/AnimalThoughtLibrary.asset`. Animal "thoughts" based on triggers.
*   **`WaveDefinition`**: `Waves/`. Animal spawn waves: `waveName`, `spawnEntries`. Used by `WaveManager`. Ex: `Wave_000.asset`.

### Tiles & Tools
*   **`TileDefinition`**: `Tiles/`. Tile properties: `displayName`, `tintColor`, `revertAfterSeconds`, `revertToTile`, `keepBottomTile`, `isWaterTile`. Ex: `TileDefinition_000_Grass.asset`.
*   **`TileInteractionLibrary`**: `Tiles/TileInteractionLibrary.asset`. `rules` for tile transformations, `refillRules` for tools. Used by `TileInteractionManager`.
*   **`ToolDefinition`**: `Tools/`. Player tools: `toolType`, `displayName`, `icon`, `iconTint`, `limitedUses`, `initialUses`. Ex: `ToolDefinition_000_GardeningHoe.asset`.

## 🛠️ Important Prefabs
Located under `Assets/Prefabs/`.

### Ecosystem
*   **Animals:** `Ecosystem/Animals/` (e.g., `Animal_Bunny.prefab`). Contain `AnimalController`, visuals.
*   **Plants (Base):** `Ecosystem/Plants/PlantPrefab.prefab`. Base for grown plants. `PlantGrowth`, `_ShadowRoot` (`PlantShadowController`), `_OutlineRoot` (`PlantOutlineController`).
*   **Plant Cells:** `Ecosystem/Plants/` (e.g., `PixelLeaf.prefab`). Instantiated by `PlantGrowth`. `PlantCell`, `SortableEntity`.
*   **Props:** `Ecosystem/Props/` (e.g., `Poop_Big.prefab`). `PoopController`.
*   **Effects:** `Ecosystem/Animals/FireflyPrefab.prefab`. `FireflyController`.

### Player
*   **`GardenerPrefab.prefab`**: `General/`. `GardenerController`, `ToolSwitcher`, `PlayerTileInteractor`, visuals.

### Projectiles
*   **`Projectile_Basic_Pixel.prefab`**: `General/`. Used by `OutputNodeEffect`. `SpellProjectile`.

### UI
*   **Node Editor:** `Ecosystem/UI/NodeView.prefab` (Contains `NodeView`, `NodeDraggable`).
*   **Indicators:** `HoverTileMarker.prefab`, `ThoughtBubble.prefab`.
*   **Debug Visualizers:** `Visualizer_Circle_Prefab.prefab`, `Visualizer_Line_Prefab.prefab`.

### Tiles
*   **`PaletteDual_Ground.prefab`**: `Tiles/Palettes/`. Unity Tile Palette for `DualGridRuleTile`s.

## ⚙️ Custom Editor Tools
Located in `Assets/Editor/`.

*   **`NodeDefinitionAutoAdder.cs`**: Auto-adds `NodeDefinition` to same-folder `NodeDefinitionLibrary`.
*   **`NodeDefinitionCreator.cs`**: Menu item "Assets/Create/Nodes/Node Definition (Auto-Named)" → `Node_XXX_.asset`.
*   **`NodeDefinitionEditor.cs`**: Custom Inspector for `NodeDefinition`. Organizes fields, resizes `effects` list.
*   **`NodeDefinitionLibraryEditor.cs`**: Custom Inspector for `NodeDefinitionLibrary`. "UPDATE" button re-populates from folder.
*   **`NodeDefinitionPostprocessor.cs`**: Enforces `Node_XXX_` naming for `NodeDefinition` assets.
*   **`NodeEffectDrawer.cs`**: `PropertyDrawer` for `NodeEffectData`. Conditional fields/labels.
*   **`TileDefinitionEditor.cs`**: Custom Inspector. "UPDATE COLOR IN SCENE" button.
*   **`TileInteractionManagerEditor.cs`**: Custom Inspector. "UPDATE SORTING ORDER", "UPDATE ALL COLORS" buttons.
*   **`HueFolders` (Package):** `Assets/HueFolders/Editor/`. Color-codes project folders.