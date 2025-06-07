using System.Collections;
using UnityEngine;
using UnityEngine.Tilemaps;
using skner.DualGrid; // Assuming this is the correct namespace for your DualGrid package
using System.Collections.Generic;
using System.Linq;
using TMPro;
#if UNITY_EDITOR
using UnityEditor; // Correct placement for editor-specific using directive
#endif

public class TileInteractionManager : MonoBehaviour
{
    public static TileInteractionManager Instance { get; private set; }
    [System.Serializable] public class TileDefinitionMapping { public TileDefinition tileDef; public DualGridTilemapModule tilemapModule; }
    [Header("Tile Definition Mappings")] public List<TileDefinitionMapping> tileDefinitionMappings;
    [Header("Interaction Library")] public TileInteractionLibrary interactionLibrary; // Now contains both rule types
    [Header("Grid & Scene References")] public Grid interactionGrid; public Camera mainCamera; public Transform player; public float hoverRadius = 3f; public GameObject hoverHighlightObject;
    [Header("Tilemap Rendering Settings")] public int baseSortingOrder = 0;
    [Header("Debug / UI")] public bool debugLogs = false; public TextMeshProUGUI hoveredTileText; public TextMeshProUGUI currentToolText;
    private Dictionary<TileDefinition, DualGridTilemapModule> moduleByDefinition;
    private Dictionary<DualGridTilemapModule, TileDefinition> definitionByModule;
    private Vector3Int? currentlyHoveredCell = null;
    private TileDefinition hoveredTileDef = null;
    private struct TimedTileState { public TileDefinition tileDef; public float timeLeft; }
    private Dictionary<Vector3Int, TimedTileState> timedCells = new Dictionary<Vector3Int, TimedTileState>();

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // Initialize dictionaries here
        moduleByDefinition = new Dictionary<TileDefinition, DualGridTilemapModule>();
        definitionByModule = new Dictionary<DualGridTilemapModule, TileDefinition>();

        SetupTilemaps(); // Setup tilemaps after dictionaries are created
    }

    void Start()
    {
        // Ensure dictionaries are populated if something went wrong in Awake or for runtime recompiles
        if (moduleByDefinition == null || moduleByDefinition.Count == 0 || definitionByModule == null || definitionByModule.Count == 0)
        {
            Debug.LogWarning("[TileInteractionManager Start] Dictionaries were empty or null, re-running SetupTilemaps.");
            SetupTilemaps();
        }

        
    }

    private void SetupTilemaps()
    {
        // Clear dictionaries before rebuilding
        moduleByDefinition.Clear();
        definitionByModule.Clear();

        if (tileDefinitionMappings == null)
        {
            Debug.LogError("[TileInteractionManager SetupTilemaps] Tile Definition Mappings list is null!", this);
            return;
        }

        for (int i = 0; i < tileDefinitionMappings.Count; i++)
        {
            var mapping = tileDefinitionMappings[i];
            if (mapping == null || mapping.tileDef == null || mapping.tilemapModule == null)
            {
                 Debug.LogWarning($"[TileInteractionManager SetupTilemaps] Skipping null or incomplete mapping at index {i}.");
                 continue;
            }

            // Add to dictionaries
            if (!moduleByDefinition.ContainsKey(mapping.tileDef))
            {
                moduleByDefinition[mapping.tileDef] = mapping.tilemapModule;
                // *** CORRECTED ASSIGNMENT HERE ***
                definitionByModule[mapping.tilemapModule] = mapping.tileDef;

                // Find the RenderTilemap child
                Transform renderTilemapTransform = mapping.tilemapModule.transform.Find("RenderTilemap");
                if (renderTilemapTransform != null)
                {
                    // Set Sorting Order
                    TilemapRenderer renderer = renderTilemapTransform.GetComponent<TilemapRenderer>();
                    if (renderer != null)
                    {
                        renderer.sortingOrder = baseSortingOrder - i;
                        if (debugLogs) Debug.Log($"Setting sorting order for {mapping.tileDef.displayName} to {renderer.sortingOrder}");
                    }
                    else { Debug.LogWarning($"RenderTilemap for {mapping.tileDef.displayName} missing TilemapRenderer.", renderTilemapTransform); }

                    // Set Initial Color
                    Tilemap tilemap = renderTilemapTransform.GetComponent<Tilemap>();
                    if (tilemap != null)
                    {
                        tilemap.color = mapping.tileDef.tintColor;
                        if (debugLogs) Debug.Log($"Setting color for {mapping.tileDef.displayName} to {tilemap.color}");
                    }
                     else { Debug.LogWarning($"RenderTilemap for {mapping.tileDef.displayName} missing Tilemap component.", renderTilemapTransform); }
                }
                 else { Debug.LogWarning($"Could not find 'RenderTilemap' child for module of {mapping.tileDef.displayName}.", mapping.tilemapModule.gameObject); }

                if (debugLogs) Debug.Log($"[Mapping] Added: {mapping.tileDef.displayName} => {mapping.tilemapModule.gameObject.name}");
            }
            else
            {
                Debug.LogWarning($"[TileInteractionManager SetupTilemaps] Duplicate TileDefinition '{mapping.tileDef.displayName}' found in mappings. Ignoring subsequent entries.", mapping.tileDef);
            }
        }
         if (debugLogs) Debug.Log($"[TileInteractionManager SetupTilemaps] Setup complete. {moduleByDefinition.Count} definitions mapped.");
    }

    // Method for editor button: Update Sorting Order
    public void UpdateSortingOrder()
    {
        if (tileDefinitionMappings == null) return;
        for (int i = 0; i < tileDefinitionMappings.Count; i++)
        {
            var mapping = tileDefinitionMappings[i];
            if (mapping == null || mapping.tileDef == null || mapping.tilemapModule == null) continue;

            Transform renderTilemapTransform = mapping.tilemapModule.transform.Find("RenderTilemap");
            if (renderTilemapTransform != null)
            {
                TilemapRenderer renderer = renderTilemapTransform.GetComponent<TilemapRenderer>();
                if (renderer != null)
                {
                    renderer.sortingOrder = baseSortingOrder - i;
#if UNITY_EDITOR
                    if (!Application.isPlaying) EditorUtility.SetDirty(renderer);
#endif
                    if (debugLogs) Debug.Log($"Updated sorting order for {mapping.tileDef.displayName} to {renderer.sortingOrder}");
                }
            }
        }
    }

    // Method for editor button: Update All Colors
    public void UpdateAllColors()
    {
        if (tileDefinitionMappings == null) return;
        foreach (var mapping in tileDefinitionMappings)
        {
            if (mapping == null || mapping.tileDef == null || mapping.tilemapModule == null) continue;

            Transform renderTilemapTransform = mapping.tilemapModule.transform.Find("RenderTilemap");
            if (renderTilemapTransform != null)
            {
                Tilemap renderTilemap = renderTilemapTransform.GetComponent<Tilemap>();
                if (renderTilemap != null)
                {
                    renderTilemap.color = mapping.tileDef.tintColor;
#if UNITY_EDITOR
                    if (!Application.isPlaying) EditorUtility.SetDirty(renderTilemap);
#endif
                    if (debugLogs) Debug.Log($"Updated color for {mapping.tileDef.displayName} to {renderTilemap.color}");
                }
            }
        }
    }

    void Update()
    {
        HandleTileHover();
        UpdateReversion();
        UpdateDebugUI(); // Update UI every frame
    }

    private void UpdateReversion()
    {
        if (timedCells.Count == 0) return;

        List<Vector3Int> cellsToRevert = null; // Initialize null

        // Iterate over a temporary copy to allow modification
        foreach (var kvp in timedCells.ToList())
        {
            Vector3Int cellPos = kvp.Key;
            TimedTileState state = kvp.Value;

            // Check if the state's tile definition is still valid (safety check)
            if (state.tileDef == null) {
                timedCells.Remove(cellPos); // Clean up invalid entry
                continue;
            }

            state.timeLeft -= Time.deltaTime;

            if (state.timeLeft <= 0f)
            {
                if (cellsToRevert == null) cellsToRevert = new List<Vector3Int>();
                cellsToRevert.Add(cellPos);
            }
            else
            {
                timedCells[cellPos] = state; // Update time left
            }
        }

        if (cellsToRevert != null)
        {
            foreach (var cellPos in cellsToRevert)
            {
                // Check if the entry still exists before processing
                if (timedCells.TryGetValue(cellPos, out TimedTileState stateToRevert))
                {
                    timedCells.Remove(cellPos); // Remove before acting

                    // Always remove the reverting tile itself
                    RemoveTile(stateToRevert.tileDef, cellPos);

                    // Place the revert-to tile *if specified*
                    if (stateToRevert.tileDef.revertToTile != null)
                    {
                        if (debugLogs) Debug.Log($"Reverting tile at {cellPos} from {stateToRevert.tileDef.displayName} to {stateToRevert.tileDef.revertToTile.displayName}");
                        PlaceTile(stateToRevert.tileDef.revertToTile, cellPos);
                    } else {
                        if (debugLogs) Debug.Log($"Tile {stateToRevert.tileDef.displayName} at {cellPos} expired and removed (no revert target).");
                    }
                }
            }
        }
    }

    private void RegisterTimedTile(Vector3Int cellPos, TileDefinition tileDef)
    {
        // Only register if revert time is positive
        // RevertToTile check is now handled during reversion itself
        if (tileDef != null && tileDef.revertAfterSeconds > 0f)
        {
            TimedTileState newState = new TimedTileState
            {
                tileDef = tileDef,
                timeLeft = tileDef.revertAfterSeconds
            };
            timedCells[cellPos] = newState;
            if (debugLogs) Debug.Log($"Registered timed reversion for {tileDef.displayName} at {cellPos} ({tileDef.revertAfterSeconds}s).");
        }
         // Clear any existing timer if revertAfterSeconds is 0 or less
         else if (timedCells.ContainsKey(cellPos))
         {
              timedCells.Remove(cellPos);
              if (debugLogs) Debug.Log($"Cleared timed reversion for {cellPos} because revert time is not positive.");
         }
    }

    public void PlaceTile(TileDefinition tileDef, Vector3Int cellPos)
    {
        if (tileDef == null) { Debug.LogWarning($"PlaceTile: Attempted to place a NULL TileDefinition at {cellPos}."); return; }

        if (moduleByDefinition == null || !moduleByDefinition.TryGetValue(tileDef, out DualGridTilemapModule module) || module == null)
        {
            Debug.LogWarning($"PlaceTile: No mapped module found for TileDefinition '{tileDef.displayName}'. Cannot place tile.");
            return;
        }

        // Handle removing existing tiles based on the new tile's 'keepBottomTile' flag
        if (!tileDef.keepBottomTile)
        {
            TileDefinition existingDef = FindWhichTileDefinitionAt(cellPos);
            if (existingDef != null && existingDef != tileDef)
            {
                 if (debugLogs) Debug.Log($"Placing '{tileDef.displayName}' (KeepBottom=false), removing existing '{existingDef.displayName}' at {cellPos}.");
                 RemoveTile(existingDef, cellPos);
            }
        } else {
             if (debugLogs) Debug.Log($"Placing '{tileDef.displayName}' (KeepBottom=true) over whatever is at {cellPos}.");
        }

        // Set the tile on the correct module's DataTilemap
        if (module.DataTilemap != null)
        {
            // Using a basic Tile asset is fine for the data layer
            TileBase dataTile = ScriptableObject.CreateInstance<Tile>();
            module.DataTilemap.SetTile(cellPos, dataTile);
        } else { Debug.LogWarning($"Module for '{tileDef.displayName}' has no DataTilemap assigned.", module.gameObject); }

        // Update RenderTilemap color immediately
        Transform renderTilemapTransform = module.transform.Find("RenderTilemap");
        if (renderTilemapTransform != null)
        {
            Tilemap renderTilemap = renderTilemapTransform.GetComponent<Tilemap>();
            if (renderTilemap != null)
            {
                renderTilemap.color = tileDef.tintColor;
#if UNITY_EDITOR
                if (!Application.isPlaying) EditorUtility.SetDirty(renderTilemap);
#endif
            }
        }

        // Register for timed reversion if applicable
        RegisterTimedTile(cellPos, tileDef);
    }

    public void RemoveTile(TileDefinition tileDef, Vector3Int cellPos)
    {
        if (tileDef == null) { Debug.LogWarning($"RemoveTile: Attempted to remove a NULL TileDefinition at {cellPos}."); return; }

        if (moduleByDefinition == null || !moduleByDefinition.TryGetValue(tileDef, out DualGridTilemapModule module) || module == null)
        {
            if (debugLogs) Debug.LogWarning($"RemoveTile: No mapped module found for TileDefinition '{tileDef.displayName}'. Cannot remove tile.");
            return;
        }

        // Remove from the module's DataTilemap
        if (module.DataTilemap != null)
        {
             // Check if the tile actually exists before removing
             if (module.DataTilemap.HasTile(cellPos)) {
                 module.DataTilemap.SetTile(cellPos, null);
                 if (debugLogs) Debug.Log($"Removed '{tileDef.displayName}' from DataTilemap at {cellPos}.");
             } else {
                 if (debugLogs) Debug.Log($"RemoveTile: Tile '{tileDef.displayName}' not found on DataTilemap at {cellPos}, skipping removal.");
             }
        } else { Debug.LogWarning($"Module for '{tileDef.displayName}' has no DataTilemap assigned.", module.gameObject); }

        // Also remove from timed reversion tracking if it matches
        if (timedCells.TryGetValue(cellPos, out TimedTileState timedState) && timedState.tileDef == tileDef)
        {
            timedCells.Remove(cellPos);
            if (debugLogs) Debug.Log($"Cleared timed reversion for {tileDef.displayName} at {cellPos} during removal.");
        }
    }

    public TileDefinition FindWhichTileDefinitionAt(Vector3Int cellPos)
    {
        if (definitionByModule == null || tileDefinitionMappings == null) {
             Debug.LogError("[FindWhichTileDefinitionAt] Dictionaries not initialized!");
             return null;
        }

        TileDefinition foundDef = null;

        // Prioritize checking overlay tiles first (those with keepBottomTile = true)
        // Iterate mappings list directly to check the flag easily
        foreach (var mapping in tileDefinitionMappings)
        {
             if (mapping?.tileDef != null && mapping.tilemapModule?.DataTilemap != null && mapping.tileDef.keepBottomTile)
             {
                 if (mapping.tilemapModule.DataTilemap.HasTile(cellPos))
                 {
                     foundDef = mapping.tileDef;
                     break; // Found the top overlay tile
                 }
             }
        }

        // If no overlay found, check non-overlay tiles
        if (foundDef == null)
        {
             // Use the definitionByModule dictionary which maps modules back to definitions
             foreach (var kvp in definitionByModule)
             {
                 DualGridTilemapModule module = kvp.Key;
                 TileDefinition def = kvp.Value;

                 // Ensure it's not an overlay tile (we already checked those) and the module/tilemap are valid
                 if (module?.DataTilemap != null && def != null && !def.keepBottomTile)
                 {
                     if (module.DataTilemap.HasTile(cellPos))
                     {
                         foundDef = def;
                         break; // Found the base tile
                     }
                 }
             }
        }

        // if (debugLogs && foundDef != null) Debug.Log($"[FindWhichTileDefinitionAt] Found '{foundDef.displayName}' at {cellPos}");
        // else if (debugLogs && foundDef == null) Debug.Log($"[FindWhichTileDefinitionAt] Found no tile at {cellPos}");

        return foundDef;
    }

    private void HandleTileHover()
    {
        if (mainCamera == null || player == null) return;

        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0f; // Ensure Z is 0 for 2D comparison
        Vector3Int cellPos = WorldToCell(mouseWorldPos);

        // Check distance only if player reference exists
        float distance = (player != null) ? Vector2.Distance(player.position, CellCenterWorld(cellPos)) : float.MaxValue;

        TileDefinition foundTile = FindWhichTileDefinitionAt(cellPos);

        // Only update hover state if within radius
        if (distance <= hoverRadius)
        {
            bool changedCell = !currentlyHoveredCell.HasValue || currentlyHoveredCell.Value != cellPos;
            currentlyHoveredCell = cellPos;
            hoveredTileDef = foundTile;

            if (hoverHighlightObject != null)
            {
                hoverHighlightObject.SetActive(true);
                hoverHighlightObject.transform.position = CellCenterWorld(cellPos);
            }
            //if (debugLogs && changedCell) Debug.Log($"[Hover Enter] Cell={cellPos}, Tile={foundTile?.displayName ?? "None"}, Dist={distance:F2}");
        }
        else
        {
            bool changedCell = currentlyHoveredCell.HasValue;
            currentlyHoveredCell = null;
            hoveredTileDef = null;
            if (hoverHighlightObject != null)
            {
                hoverHighlightObject.SetActive(false);
            }
            //if (debugLogs && changedCell) Debug.Log($"[Hover Exit] Cell outside radius ({distance:F2} > {hoverRadius})");
        }
    }

    private void UpdateDebugUI()
    {
        // Hovered Tile Text
        if (hoveredTileText != null)
        {
            string tileName = hoveredTileDef != null ? hoveredTileDef.displayName : "None";
            hoveredTileText.text = $"Hover: {tileName}";
        }

        // Current Tool Text - now from inventory bar
        if (currentToolText != null)
        {
            if (InventoryBarController.Instance != null && InventoryBarController.Instance.SelectedItem != null)
            {
                var selectedItem = InventoryBarController.Instance.SelectedItem;
                if (selectedItem.Type == InventoryBarItem.ItemType.Tool)
                {
                    currentToolText.text = $"Tool: {selectedItem.GetDisplayName()}";
                }
                else if (selectedItem.Type == InventoryBarItem.ItemType.Node && selectedItem.IsSeed())
                {
                    currentToolText.text = $"Seed: {selectedItem.GetDisplayName()}";
                }
                else
                {
                    currentToolText.text = $"Selected: {selectedItem.GetDisplayName()}";
                }
            }
            else
            {
                currentToolText.text = "Nothing Selected";
            }
        }
    }

    public Vector3Int WorldToCell(Vector3 worldPos)
    {
        if (interactionGrid != null) return interactionGrid.WorldToCell(worldPos);

        // Fallback: Try to get grid from the first valid mapping
        if (tileDefinitionMappings != null && tileDefinitionMappings.Count > 0) {
             foreach(var mapping in tileDefinitionMappings) {
                 if (mapping?.tilemapModule?.DataTilemap?.layoutGrid != null) {
                     return mapping.tilemapModule.DataTilemap.layoutGrid.WorldToCell(worldPos);
                 }
             }
        }

        Debug.LogWarning("[WorldToCell] No valid interactionGrid or mapped Tilemap found to determine cell position.");
        return Vector3Int.zero;
    }

    private Vector3 CellCenterWorld(Vector3Int cellPos)
    {
         if (interactionGrid != null) return interactionGrid.GetCellCenterWorld(cellPos);

         // Fallback: Try to get grid from the first valid mapping
         if (tileDefinitionMappings != null && tileDefinitionMappings.Count > 0) {
             foreach(var mapping in tileDefinitionMappings) {
                 if (mapping?.tilemapModule?.DataTilemap?.layoutGrid != null) {
                     return mapping.tilemapModule.DataTilemap.layoutGrid.GetCellCenterWorld(cellPos);
                 }
             }
        }

        Debug.LogWarning("[CellCenterWorld] No valid interactionGrid or mapped Tilemap found to determine cell center.");
        return Vector3.zero; // Or grid.CellToWorld(cellPos) + grid.cellSize * 0.5f if grid guaranteed?
    }

    private void HandleSeedPlanting(Vector3Int cellPosition)
    {
        PlantPlacementManager plantManager = PlantPlacementManager.Instance;
        if (plantManager == null) { Debug.LogError("Cannot plant: PlantPlacementManager not found!"); return; }

        TileDefinition tileDef = FindWhichTileDefinitionAt(cellPosition);
        if (!plantManager.IsTileValidForPlanting(tileDef))
        {
            if (debugLogs) Debug.Log($"Cannot plant on {tileDef?.displayName ?? "Unknown"} - invalid tile.");
            // Maybe add player feedback here (sound/visual)
            return;
        }

        GardenerController gardener = player?.GetComponent<GardenerController>();
        if (gardener == null) { Debug.LogError("Cannot plant: GardenerController not found on player!"); return; }

        Vector3 worldPosition = CellCenterWorld(cellPosition); // Plant near cell center

        gardener.Plant(); // Trigger animation
        StartCoroutine(PlantAfterAnimation(gardener, plantManager, cellPosition, worldPosition));
    }

    private IEnumerator PlantAfterAnimation(GardenerController gardener, PlantPlacementManager plantManager, Vector3Int cellPosition, Vector3 worldPosition)
    {
        // Ensure gardener and duration are valid
        float waitTime = (gardener != null) ? gardener.plantingDuration : 0.1f;
        yield return new WaitForSeconds(waitTime);

        if (plantManager != null) // Check again in case it was destroyed
        {
            bool planted = plantManager.TryPlantSeed(cellPosition, worldPosition);
            if (debugLogs) Debug.Log(planted ? $"Planted seed successfully at {cellPosition}" : $"Failed to plant seed at {cellPosition}");
        }
    }

    // --- ApplyToolAction is MODIFIED ---
    public void ApplyToolAction(ToolDefinition toolDef)
{
    // --- Initial Checks (modified - removed ToolSwitcher check) ---
    if (toolDef == null) { Debug.LogWarning("ApplyToolAction called with a NULL toolDef."); return; }
    if (!currentlyHoveredCell.HasValue) return;
    if (hoveredTileDef == null) { if (debugLogs) Debug.Log("ApplyToolAction: No recognized tile at hovered cell."); return; }
    
    float distance = Vector2.Distance(player.position, CellCenterWorld(currentlyHoveredCell.Value));
    if (distance > hoverRadius) { if (debugLogs) Debug.Log($"ApplyToolAction: Cell too far ({distance:F2} > {hoverRadius})."); return; }
    
    // REMOVED: All playerToolSwitcher checks - we don't need them anymore!
    
    if (debugLogs) Debug.Log($"[ApplyToolAction] Using Tool='{toolDef.toolType}', On Tile='{hoveredTileDef.displayName}', At Cell={currentlyHoveredCell.Value}");

    // --- Check for REFILL Action FIRST ---
    bool wasRefillAction = false;
    if (interactionLibrary != null && interactionLibrary.refillRules != null)
    {
        foreach (var refillRule in interactionLibrary.refillRules)
        {
            if (refillRule != null && refillRule.toolToRefill == toolDef && refillRule.refillSourceTile == hoveredTileDef)
            {
                if (debugLogs) Debug.Log($"Refill rule matched: Tool '{toolDef.displayName}' on Tile '{hoveredTileDef.displayName}'.");
                
                // We can't refill without ToolSwitcher, so just log it
                Debug.Log($"[TileInteractionManager] Tool refill not implemented for inventory-based tools yet.");
                wasRefillAction = true;
                break;
            }
        }
    }
    
    if (wasRefillAction) return;

    // --- Handle Standard Tile Transformation ---
    if (interactionLibrary == null || interactionLibrary.rules == null) { Debug.LogWarning("Interaction Library or its standard rules list is null!"); return; }
    TileInteractionRule rule = interactionLibrary.rules.FirstOrDefault(r => r != null && r.tool == toolDef && r.fromTile == hoveredTileDef);

    if (rule == null)
    {
        if (debugLogs) Debug.Log($"No standard interaction rule found for tool '{toolDef.toolType}' on tile '{hoveredTileDef.displayName}'.");
        return;
    }

    // Apply the standard rule
    if (rule.toTile != null)
    {
        if (debugLogs) Debug.Log($"Applying standard rule: '{hoveredTileDef.displayName}' -> '{rule.toTile.displayName}'");
        if (!rule.toTile.keepBottomTile)
        {
            RemoveTile(hoveredTileDef, currentlyHoveredCell.Value);
        }
        PlaceTile(rule.toTile, currentlyHoveredCell.Value);
    }
    else
    {
        if (debugLogs) Debug.Log($"Applying standard rule: Remove '{hoveredTileDef.displayName}'");
        RemoveTile(hoveredTileDef, currentlyHoveredCell.Value);
    }
}
}