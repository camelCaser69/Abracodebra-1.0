# 03_Gameplay_Systems_Manual.md

**Synthesized:** 2025-05-31
**Project:** Gene Garden Survivor

Manual for gameplay systems. Reference specific sections as needed.

## Sections:
1.  [Core Game Loop & State Management](#1-core-game-loop--state-management)
2.  [Plant Systems](#2-plant-systems)
3.  [Ecosystem Systems](#3-ecosystem-systems)
4.  [Environment & Tile Systems](#4-environment--tile-systems)
5.  [Tool Systems](#5-tool-systems)
6.  [Weather & Time Systems](#6-weather--time-systems)
7.  [Visual Systems](#7-visual-systems)
8.  [Input & UI Systems](#8-input--ui-systems)

---
## 1. Core Game Loop & State Management
Player-controlled three-phase loop via `RunManager` (Sprint 0).

### Phases
*   **Planning (Paused, `Time.timeScale = 0`):** Edit plant DNA (Node Editor), design garden, analyze threats, allocate resources. UI: Planning Panel.
*   **Growth & Threat (Real-time, `Time.timeScale ~ 6`):** Plants grow rapidly. Threats spawn (`WaveManager`). Plants auto-combat. Limited player panic tools. UI: Running Panel.
*   **Recovery (Paused, `Time.timeScale = 0`):** Review performance, get rewards (Gene Echoes), unlock research. UI: Recovery Panel.

### `RunManager.cs` (Sprint 0)
*   Singleton. `RunState` enum (`Planning`, `GrowthAndThreat`, `Recovery`).
*   Methods: `StartGrowthAndThreatPhase()`, `EndRound()`, `StartNewPlanningPhase()`.
*   Controls `Time.timeScale`, coordinates phase transitions with `WeatherManager`, `WaveManager`, UI.

### Player Classes & Roguelike Structure (Sprint 2+)
*   Starting classes (Defensive, Aggressive, Symbiotic, Survival) with unique genes, tools, passives. Finite Seeds, Gene Echoes.
*   Biome progression (Grasslands → Toxic Wasteland), ~7-10 rounds/biome, permadeath (meta-currency/genes persist). Victory tiers.

---
## 2. Plant Systems

### Node Graph System
Defines plant genetics.
*   **`NodeDefinition` (SO):** A gene. `displayName`, `description`, `thumbnail`, `effects (List<NodeEffectData>)`. `CloneEffects()` for runtime copies.
*   **`NodeEffectData`:** `effectType (NodeEffectType)`, `primaryValue`, `secondaryValue`, `isPassive (bool)`, `scentDefinitionReference (ScentDefinition)`.
*   **`NodeEffectType` (Enum):**
    *   Passive (Growth): `SeedSpawn` (required), `EnergyStorage`, `Photosynthesis`, `StemLength`, `GrowthSpeed`, `LeafGap`, `LeafPattern`, `StemRandomness`, `Cooldown`, `CastDelay`, `PoopFertilizer`.
    *   Active (Mature Cycle): `EnergyCost`, `Output` (projectile), `Damage` (mod), `GrowBerry`, `ScentModifier`.
*   **`NodeGraph` (Runtime):** `List<NodeData>` for a plant instance. Cloned from UI graph by `PlantPlacementManager`.
*   **`NodeData` (Runtime):** Node instance in `NodeGraph`. `nodeId`, `displayName`, `effects`, `orderIndex`, `canBeDeleted`.
*   **`NodeDefinitionLibrary` (SO):** All available `NodeDefinition`s. `InitialNodeConfig` for editor defaults.
*   **Editor Tools:** Auto-adders, creators, custom inspectors for `NodeDefinition`/`Library`/`EffectData`.

### Plant Growth System
Plant lifecycle. "Leaf = Life": no leaves = death; fewer leaves = less energy. Leaf damage is permanent unless genes allow regen.
*   **`PlantGrowth.cs` (Partial):** State machine (`Initializing`...`Mature_Executing`).
    *   **Init:** Takes `NodeGraph`. `CalculateAndApplyStats()` from passive nodes (energy, stem, speed, leaf pattern, poop fertilizer stats, etc.). Spawns `Seed` cell.
    *   **Growth (`GrowthCoroutine_TimeBased`):** `PreCalculateGrowthPlan()` creates `GrowthStep` list. `finalGrowthSpeed` + tile modifiers (`PlantGrowthModifierManager`) + `RunManager` `Time.timeScale` control speed. `SpawnCellVisual()` instantiates `PlantCell` prefabs. `UpdateGrowthPercentageUI()`.
    *   **Mature Cycle (`ExecuteMatureCycle`):** During "Growth & Threat" phase. Accumulates active node effects (`EnergyCost`, `Damage` mods, `ScentModifier` bonuses). Checks `PoopFertilizer`. If enough energy, executes actions (`Output`, `GrowBerry`). `nodeCastDelay` between nodes.
    *   **Energy:** `AccumulateEnergy()` from photosynthesis (sunlight, leaves, fireflies, tile mods).
    *   **Poop Fertilization:** `CheckForPoopAndAbsorb()` → `TryRegrowLeaf()` or gain energy.
*   **`PlantCell.cs`:** Individual part (Seed, Stem, Leaf, Fruit). `GridCoord`, `CellType`. `OnDestroy()` calls `ReportCellDestroyed()`.
*   **`LeafData.cs`:** Tracks `GridCoord`, `IsActive` for leaf regrowth.
*   **Dependencies:** `NodeGraph`, `WeatherManager`, `FireflyManager`, `PlantGrowthModifierManager`, `TileInteractionManager`, `PoopController`, `OutputNodeEffect`, `RunManager`.

### Plant Effects System
*   **Shadows (`PlantShadowController`):** On `_ShadowRoot`. Global settings (`color`, `squash`, `angle`). `distanceFade`. Creates `ShadowPartController` per cell.
*   **Outlines (`PlantOutlineController`):** On `_OutlineRoot`. Updates on cell add/remove. `OutlinePartController` syncs sprite. Exclusions (`outerCorners`, `baseCell`).
*   **Scents (`ScentSource`):** On berries/projectiles. `definition`, `radiusModifier`, `strengthModifier`. Visualized by `FloraManager`.
*   **Projectiles (`OutputNodeEffect`):** On plant root. `Activate()` called by `Output` node. Spawns `projectilePrefab`, applies scents, inits `SpellProjectile` (damage, speed).

---
## 3. Ecosystem Systems

### Animal AI System
Creature behavior. Threats in "Growth & Threat" phase.
*   **`AnimalController.cs`:** Core AI. `Rigidbody2D`, `SortableEntity`, `Collider2D`.
    *   **Init:** `AnimalDefinition`, movement bounds (from `FaunaManager` + offset), `spawnedOffscreen` flag.
    *   **Behavior:** Wanders, seeks food (plant leaves), eats, poops. Reacts to scents.
    *   **Threats:** Herbivores target leaves. Carnivores (future) TBD.
    *   **Movement:** Clamped by `FaunaManager` bounds. Speed modifiable by `SlowdownZone`.
    *   **Diet/Hunger:** Uses `AnimalDiet`. Hunger drives food seeking. Starvation damage. Eating `FoodItem` (leaves) reduces hunger.
    *   **Pooping:** Timed after eating. `SpawnPoop()`.
    *   **Health/Death:** Takes damage (starvation, combat). `Die()` fades out.
    *   **Visuals:** Animator, sprite flip, UI text (HP, hunger), damage flash.
*   **`AnimalDefinition` (SO):** Species traits: `name`, `health`, `speed`, `prefab`, `diet (AnimalDiet)`.
*   **`AnimalDiet` (SO):** `acceptableFoods (List<DietPreferenceSimplified>)`. Hunger stats. `GetSatiationValue()`, `FindBestFood()`.
*   **Spawning (`FaunaManager`, `WaveManager`):**
    *   `WaveManager`: `RunManager` calls `SpawnNextWave()`. `NoActiveThreats()` signals wave clear.
    *   `FaunaManager`: Instantiates animals. `CalculateSpawnPosition()` (Global, NearPlayer, Offscreen, uses `boundsOffsetX/Y`). `SpawnAnimal()` inits `AnimalController`.
    *   `WaveDefinition` (SO): `List<WaveSpawnEntry>`. `WaveSpawnEntry`: `animalDefinition`, `count`, `delay`, `interval`, `locationType`, `radius`.

### Thought & Communication System
*   **`AnimalThoughtLibrary` (SO):** `AnimalThoughtLine` collection.
*   **`AnimalThoughtLine`:** `speciesName`, `trigger (ThoughtTrigger)`, `lines (List<string>)`.
*   **`ThoughtTrigger` (Enum):** `Hungry`, `Eating`, `HealthLow`, `Fleeing`, `Pooping`.
*   **`ThoughtBubbleController.cs`:** Displays thought text, follows animal.
*   `AnimalController.ShowThought()` on trigger + cooldown.

### Food & Consumption System
*   **`FoodType` (SO):** Food properties (`foodName`, `icon`, `category`).
*   **`FoodItem.cs`:** Marks consumable GameObjects (berries, leaves). `foodType` ref.
*   **`PoopController.cs`:** Poop lifecycle. `lifetime`, `fadeDuration`. `Collider2D` (trigger) for plant detection.
*   **Flow:** Animal eats `FoodItem`/leaf → satiation → may produce `PoopController`.

### Scent System
*   **`ScentDefinition` (SO):** `scentID`, `displayName`, `baseRadius`, `baseStrength`, `particleEffectPrefab`.
*   **`ScentLibrary` (SO):** Collection. Ref by `EcosystemManager`.
*   **`ScentSource.cs`:** Emits scent. `definition`, `radiusModifier`, `strengthModifier`. `EffectiveRadius`/`Strength`.
*   **Visualization (`FloraManager`):** `RuntimeCircleDrawer` for radii. Also for `PlantGrowth.poopDetectionRadius`.

### Firefly System
*   **`FireflyManager.cs`:** Spawns `fireflyPrefab` at night (`weatherManager.sunIntensity <= nightThreshold`). `maxFireflies`. Provides photosynthesis bonus to nearby plants. Visualizes attraction lines.
*   **`FireflyController.cs`:** Movement (wander, bounds), lifetime, glow/flicker (normal & spawn effects), scent attraction (`attractiveScentDefinitions`).

---
## 4. Environment & Tile Systems

### Dual-Grid Tile System
Third-party package for Wang-tiles.
*   **Concept:** Hidden **Data Tilemap** (logic), visible **Render Tilemap** (offset, 4 render tiles per data tile).
*   **`DualGridRuleTile` (Asset):** Custom `RuleTile`. Rules use Data Tilemap neighbors. Needs 4x4 16-tile tilesheet (sprites `_0` to `_15`). Auto-populates rules.
*   **`DualGridTilemapModule` (Component):** On Data Tilemap GO. `DualGridRuleTile` assigned here. Updates Render Tilemap.
*   **`TileDefinition` (SO):** Project's tile type abstraction. `displayName`, `tintColor`, `revertAfterSeconds`, `revertToTile`, `keepBottomTile`, `isWaterTile`.
*   **Setup:** See `07_Third_Party_Package_Guide_DualGrid.md`.

### Tile Interaction System
Manages tool/event tile modifications.
*   **`TileInteractionManager.cs` (Singleton):**
    *   **Mappings:** `tileDefinitionMappings` links `TileDefinition` SOs to scene `DualGridTilemapModule`s. `SetupTilemaps()` builds lookup dictionaries, sets sorting/color.
    *   **Hover:** `HandleTileHover()` detects hovered cell, checks player distance, updates `hoverHighlightObject`.
    *   **Timed Reversion:** `timedCells` tracks tiles to revert. `RegisterTimedTile()` on placement. `UpdateReversion()` processes.
    *   **`PlaceTile(TileDefinition, Vector3Int)`:** If `!tileDef.keepBottomTile`, removes existing. Sets placeholder `Tile` on module's DataTilemap. Updates RenderTilemap color. Registers for reversion.
    *   **`RemoveTile(TileDefinition, Vector3Int)`:** Removes from DataTilemap, clears timed reversion.
    *   **`FindWhichTileDefinitionAt(Vector3Int)`:** Returns logical `TileDefinition` at cell (overlays first).
    *   **`ApplyToolAction(ToolDefinition)`:** Core tool logic. Triggered by `PlayerTileInteractor`.
        *   Validates hover, distance.
        *   **Refill Check:** Uses `interactionLibrary.refillRules`. If match, calls `playerToolSwitcher.RefillCurrentTool()`. (Tool use consumed *before* refill).
        *   **Consume Use:** Calls `playerToolSwitcher.TryConsumeUse()`. Stops if false (and not refill).
        *   **Seed Pouch:** Calls `HandleSeedPlanting()`.
        *   **Standard Transform:** Uses `interactionLibrary.rules`. Applies `fromTile` → `toTile`.
*   **`TileInteractionLibrary` (SO):** `rules (List<TileInteractionRule>)`, `refillRules (List<ToolRefillRule>)`.
*   **`TileInteractionRule`:** `tool`, `fromTile`, `toTile`.
*   **`ToolRefillRule`:** `toolToRefill`, `refillSourceTile`.

### Plant Placement System
Player plant spawning.
*   **`PlantPlacementManager.cs` (Singleton):**
    *   **`TryPlantSeed(gridPos, worldPos)`:** Called by `TileInteractionManager` for SeedPouch.
        *   Checks `IsPositionOccupied()`, `IsTileValidForPlanting()` (uses `invalidPlantingTiles` set).
        *   Gets `NodeGraph` from `NodeEditorGridController`. Validates `SeedSpawn` effect.
        *   `GetRandomizedPlantingPosition()` offsets from `worldPos`.
        *   Calls `SpawnPlant()`. Tracks in `plantsByGridPosition`. Registers with `PlantGrowthModifierManager`.
    *   **`SpawnPlant(NodeGraph, position)`:** Instantiates `plantPrefab`. Deep copies `NodeGraph`. Calls `PlantGrowth.InitializeAndGrow()`.
*   **Dependencies:** `NodeEditorGridController`, `TileInteractionManager`, `PlantGrowthModifierManager`.

### Growth Modifier System
Tile-based effects on plants.
*   **`PlantGrowthModifierManager.cs` (Singleton):**
    *   `tileModifiers (List<TileGrowthModifier>)` define per-`TileDefinition` multipliers. `BuildModifierLookup()` for fast access.
    *   `RegisterPlantTile()`/`UnregisterPlant()` track plant locations.
    *   `UpdateAllPlantTiles()` periodically checks for tile changes.
    *   `GetGrowthSpeedMultiplier()`/`GetEnergyRechargeMultiplier()` provide values to `PlantGrowth`.
*   **`TileGrowthModifier`:** `tileDefinition`, `growthSpeedMultiplier`, `energyRechargeMultiplier`.

### Slowdown Zones
*   **`SlowdownZone.cs`:** `speedMultiplier`. `OnTriggerEnter/Exit2D` calls `Apply/RemoveSpeedMultiplier` on `AnimalController`/`GardenerController`.

---
## 5. Tool Systems

### Tool Management
Player tools, usage, state. Player classes may have "Special Tools".
*   **`ToolDefinition` (SO):** `toolType (ToolType)`, `displayName`, `icon`, `iconTint`, `limitedUses (bool)`, `initialUses (int)`.
*   **`ToolType` (Enum):** `None`, `Hoe`, `WateringCan`, `SeedPouch`.
*   **`ToolSwitcher.cs`:** Attached to player. `toolDefinitions[]` for standard tools. `CurrentTool`, `CurrentRemainingUses (-1 for unlimited)`.
    *   **Events:** `OnToolChanged (Action<ToolDefinition>)`, `OnUsesChanged (Action<int>)`.
    *   Input: Q/E cycles tools.
    *   `InitializeToolState()`: Sets current tool/uses, fires events.
    *   `TryConsumeUse()`: Decrements uses if limited. Fires `OnUsesChanged`. Returns success.
    *   `RefillCurrentTool()`: Resets uses to `initialUses`. Fires `OnUsesChanged`.
*   **Integration:** `GardenerController` updates UI icon via `OnToolChanged`. `PlayerTileInteractor` gets `CurrentTool`. `TileInteractionManager` consumes uses/refills.

### Tool Interaction Processing
*   **`PlayerTileInteractor.cs`:** On Left Click → `TileInteractionManager.ApplyToolAction(CurrentTool)`.
*   **`TileInteractionManager.ApplyToolAction()`:** Validates. Checks Refill Rules. If not refill, consumes use. If `SeedPouch`, plants. Else, applies standard TileInteractionRule.

---
## 6. Weather & Time Systems

### Weather Management
Day/night cycle. Role adapts with `RunManager`.
*   **`WeatherManager.cs` (Singleton):**
    *   `CyclePhase` enum. `day/night/transitionDuration`. `sunIntensity (0-1)` based on phase.
    *   `sunIntensity` affects: `PlantGrowth` photosynthesis, `FireflyManager` spawning, `NightColorPostProcess`.
    *   `timeScaleMultiplier` (internal cycle speed), `IsPaused` (cycle pause).
    *   `OnPhaseChanged` event.
*   **Sprint 0 `RunManager` Integration:**
    *   `RunManager` controls global `Time.timeScale`.
    *   `WeatherManager` cycle pauses if `Time.timeScale = 0`.
    *   `RunManager` may call `WeatherManager.SimulateDay()` to force daylight.
    *   `WaveManager` timing via `WeatherManager` events likely replaced by direct `RunManager` calls.

### Time Control Integration (Sprint 0+)
*   **`RunManager` is primary `Time.timeScale` controller:** Planning (0), Growth & Threat (~6), Recovery (0).
*   `WeatherManager.timeScaleMultiplier` scales its cycle *relative to global `Time.timeScale`*.

---
## 7. Visual Systems

### Dynamic Plant Visuals
*   **Shadows:** `PlantShadowController` (see Plant Effects).
*   **Outlines:** `PlantOutlineController` (see Plant Effects).

### Environmental Visuals
*   **Water Reflections (`WaterReflection.cs`, `WaterReflectionManager.cs`):**
    *   `WaterReflectionManager`: Global defaults (opacity, tint, sort offset, gradient material, masking).
    *   `WaterReflection`: Per-sprite overrides. Creates reflection GO. Optional gradient fade. Optional water tilemap masking.
*   **Fireflies:** `FireflyController` uses emissive material & `Light2D`. `FireflyManager` draws attraction lines.
*   **Scents:** `ScentDefinition.particleEffectPrefab` instantiated by `PlantGrowth`.
*   **Debug Vis (`RuntimeCircleDrawer`, `FloraManager`):** `LineRenderer` circles for radii (scents, poop absorption).

### Sorting & Rendering
*   **`SortableEntity.cs`:** Y-sorting for sprites. `sortingOrder = -(yPos + offset) * 1000f`.
*   **`PixelPerfectSetup.cs`:** Configures `PixelPerfectCamera` (320x180 ref, 16 PPU).
*   **Sorting Layers:** Default, Shadows, Objects, Outlines, UI.
*   **URP 2D Renderer.**
*   **Post-Processing (`NightColorPostProcess.cs`):** Interpolates Volume effects (ColorAdjusts, FilmGrain, Vignette) by `WeatherManager.sunIntensity`.

---
## 8. Input & UI Systems

### Input Management
*   New Input System: `InputSystem_Actions.inputactions`.
*   **Action Maps:** "Player" (WASD, Q/E, LMB), "UI" (Tab, Del, RMB, Esc).
*   Scripts (`PlayerTileInteractor`, `GardenerController`, `ToolSwitcher`, `NodeEditorGridController`) handle relevant actions.

### Node Editor UI
For plant genetics design (Planning phase).
*   **`NodeEditorGridController.cs`:** Manages UI panel (`gridUIParent`). Creates `NodeCell`s. Handles `InitialNodeConfig`. `nodeDropdown` for adding nodes. `HandleNodeDrop()`. `RefreshGraph()` updates `_uiGraphRepresentation`.
*   **`NodeCell.cs`:** Grid slot. `CurrentlySelectedCell`. `AssignNode()`/`AssignNodeView()`. RMB on empty shows dropdown. `IDropHandler`.
*   **`NodeView.cs`:** Visual of `NodeData`. Thumbnail, background. Tooltip. `OnPointerDown` selects cell. `Highlight()`/`Unhighlight()`.
*   **`NodeDraggable.cs`:** On `NodeView`. Drag/drop. Reparents to canvas, transparent, raycasts disabled during drag.
*   **`DeselectOnClickOutside.cs`:** Clears node selection.

### In-Game UI
*   **Sprint 0 `UIManager.cs` (New):** Manages phase-specific panels (Planning, Running, Recovery) with buttons linking to `RunManager`.
*   **Existing Elements (to integrate/adapt):**
    *   Tool Icon (`GardenerController.toolIconRenderer`).
    *   Hovered Tile Text, Current Tool Text (`TileInteractionManager`).
    *   Plant Growth % (`PlantGrowth.energyText`).
    *   Animal Stats (`AnimalController.hpText`/`hungerText`).
    *   Old `WaveManager` UI (`waveStatusText`, `timeTrackerText`, `startRunButton`) likely replaced by new `UIManager` panels.