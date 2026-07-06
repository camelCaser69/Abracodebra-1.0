# 07_Third_Party_Package_Guide_DualGrid.md

**Synthesized:** 2025-05-31
**Source:** `DualgridPackage_user-guide.md` (Condensed for project context)

Essential guide for using the **DualGrid Tilemap Package** in Gene Garden Survivor.

## 1. Concept
Dual-grid:
*   **Data Tilemap:** Logical grid for tile data. Interacted with via scripts (e.g., `TileInteractionManager`).
*   **Render Tilemap:** Visual grid, half-unit offset. 4 render tiles represent corners of 1 data tile for seamless transitions.

## 2. Dual Grid Rule Tile
Custom `RuleTile` for DualGrid. Rules use Data Tilemap neighbors to select Render Tilemap sprite.
Requires a 4x4 16-tile tilesheet format.

### Tilesheet Prep
1.  **Format:** 4x4 grid of corner configurations.
2.  **Import:** Sprite Mode: Multiple.
3.  **Sprite Editor:** Slice: Grid By Cell Count, 4C x 4R. **Tick "Keep Empty Rects"**. Apply.
4.  **Naming:** Sliced sprites must end `_0` to `_15`. **Do not change suffixes.**

### Creating Asset
1.  Right-click sliced Texture asset → `Create -> 2D -> Tiles -> Dual Grid Rule Tile`.
2.  Dialog: Auto-populate rules? → Yes.
3.  Creates `.asset` (e.g., `DualGridRuleTile_Grass.asset`). Preview in Inspector to verify.

## 3. Dual Grid Tilemap GameObject
In-scene setup.
1.  **Create:** `GameObject -> 2D Object -> Tilemap -> Dual Grid Tilemap`.
    *   Creates parent Grid.
    *   Children: `DataTilemap_...` (with `DualGridTilemapModule`, `Tilemap`), `RenderTilemap_...` (child of Data, with `Tilemap`, `TilemapRenderer`).
2.  **Assign Rule Tile:**
    *   Select `DataTilemap_...` GO.
    *   Inspector: `DualGrid Tilemap Module` component.
    *   Drag your `DualGridRuleTile` asset to its "Default Tile" (or similar) field.
    *   **Project Context:** `TileInteractionManager` maps `TileDefinition` SOs to these modules. It places generic `Tile`s on the module's `DataTilemap`; the module uses its assigned `DualGridRuleTile` for rendering.

## 4. Usage
*   **Painting:** With Rule Tile assigned, paint on Data Tilemap (e.g., via Tile Palette). Module updates Render Tilemap.
*   **Scripting:** `TileInteractionManager` sets/clears tiles on the `DataTilemap` of the relevant module.

## 5. Multiple Layers
Multiple `DualGridTilemap` setups (Data/Render pairs) can exist.
*   `TileInteractionManager.tileDefinitionMappings` manages these layers.
*   **Tilesheet Note:** Original guide: "no support for different tilesheets per Data Tilemap." Implies all `DualGridRuleTile`s might need same 16-tile *format*. Verify if diverse art styles are needed.

## 6. Advanced Features (Original Guide)

### Colliders
Configured in `DualGridRuleTile` asset & `DualGridTilemapModule`.
1.  **Grid:** `TilemapCollider2D` on **DataTilemap**. Box collider per filled Data Tile.
2.  **Sprite:** `TilemapCollider2D` on **RenderTilemap**. Colliders from Render Tile sprites. Good for complex shapes.

### GameObjects on Tiles
1.  **Data Origin:** Single GO type per `DualGridRuleTile`. Instantiated at Data Tile center.
2.  **Render Origin:** GOs at Render Tile centers (Data Tile "corners"). Different GOs per Tiling Rule possible.

**Config:**
*   **Rule Tile:** `GameObject` field in main settings (Data Origin) or per Tiling Rule (Render Origin).
*   **Module:** Select "Game Object Origin" (Data/Render).