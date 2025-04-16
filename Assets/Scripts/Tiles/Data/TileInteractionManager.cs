using System.Collections;
using UnityEngine;
using UnityEngine.Tilemaps;
using skner.DualGrid;
using System.Collections.Generic;
using System.Linq;
using TMPro;

public class TileInteractionManager : MonoBehaviour
{
    public static TileInteractionManager Instance { get; private set; }

    [System.Serializable]
    public class TileDefinitionMapping
    {
        public TileDefinition tileDef;
        public DualGridTilemapModule tilemapModule;
    }

    [Header("Tile Definition Mappings")]
    public List<TileDefinitionMapping> tileDefinitionMappings;

    // (If you have an interaction library for tool-based transformations)
    [Header("Interaction Library")]
    public TileInteractionLibrary interactionLibrary;

    [Header("Grid & Scene References")]
    public Grid interactionGrid;
    public Camera mainCamera;
    public Transform player;
    public float hoverRadius = 3f;
    public GameObject hoverHighlightObject;

    [Header("Tilemap Rendering Settings")]
    [Tooltip("The base sorting order value (the first tilemap will be this value, subsequent ones will decrease)")]
    public int baseSortingOrder = 0;

    [Header("Debug / UI")]
    public bool debugLogs = false;
    public TextMeshProUGUI hoveredTileText;
    public TextMeshProUGUI currentToolText;

    // quick lookups
    private Dictionary<TileDefinition, DualGridTilemapModule> moduleByDefinition;
    private Dictionary<DualGridTilemapModule, TileDefinition> definitionByModule;

    // track which cell is hovered
    private Vector3Int? currentlyHoveredCell = null;
    private TileDefinition hoveredTileDef = null;

    // --------- NEW: Timed reversion dictionary -----------
    // Key = cell position, Value = struct that holds the tileDefinition & time left
    private Dictionary<Vector3Int, TimedTileState> timedCells = new Dictionary<Vector3Int, TimedTileState>();

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // build dictionaries
        moduleByDefinition = new Dictionary<TileDefinition, DualGridTilemapModule>();
        definitionByModule = new Dictionary<DualGridTilemapModule, TileDefinition>();

        // Setup tilemaps with correct sorting orders and colors
        SetupTilemaps();
    }

    void Start()
    {
        // Ensure we always initialize the dictionary in Start() for runtime
        if (moduleByDefinition == null || moduleByDefinition.Count == 0)
        {
            SetupTilemaps();
        }
    }

    // Method to set up tilemap sorting order and apply initial colors
    private void SetupTilemaps()
    {
        moduleByDefinition = new Dictionary<TileDefinition, DualGridTilemapModule>();
        definitionByModule = new Dictionary<DualGridTilemapModule, TileDefinition>();
        
        for (int i = 0; i < tileDefinitionMappings.Count; i++)
        {
            var mapping = tileDefinitionMappings[i];
            if (mapping.tileDef == null || mapping.tilemapModule == null) 
                continue;

            // Add to dictionaries
            if (!moduleByDefinition.ContainsKey(mapping.tileDef))
            {
                moduleByDefinition[mapping.tileDef] = mapping.tilemapModule;
                definitionByModule[mapping.tilemapModule] = mapping.tileDef;
                
                // Get the RenderTilemap component
                Transform renderTilemapTransform = mapping.tilemapModule.transform.Find("RenderTilemap");
                if (renderTilemapTransform != null)
                {
                    // Set the sorting order based on the index - INVERTED (negative values)
                    // First item (index 0) gets baseSortingOrder, then we subtract for each subsequent item
                    TilemapRenderer renderer = renderTilemapTransform.GetComponent<TilemapRenderer>();
                    if (renderer != null)
                    {
                        renderer.sortingOrder = baseSortingOrder - i;
                        if (debugLogs)
                            Debug.Log($"Setting sorting order for {mapping.tileDef.displayName} to {baseSortingOrder - i}");
                    }
                    
                    // Set the initial color from TileDefinition
                    Tilemap tilemap = renderTilemapTransform.GetComponent<Tilemap>();
                    if (tilemap != null)
                    {
                        tilemap.color = mapping.tileDef.tintColor;
                        if (debugLogs)
                            Debug.Log($"Setting color for {mapping.tileDef.displayName} to {mapping.tileDef.tintColor}");
                    }
                }
                
                if (debugLogs)
                    Debug.Log($"[Mapping] {mapping.tileDef.displayName} => {mapping.tilemapModule.gameObject.name}");
            }
            else
            {
                Debug.LogWarning($"Duplicate tileDef {mapping.tileDef.displayName} in tileDefinitionMappings.");
            }
        }
    }

    // New public method to update sorting order - can be called from custom editor
    public void UpdateSortingOrder()
    {
        for (int i = 0; i < tileDefinitionMappings.Count; i++)
        {
            var mapping = tileDefinitionMappings[i];
            if (mapping.tileDef == null || mapping.tilemapModule == null) 
                continue;
                
            Transform renderTilemapTransform = mapping.tilemapModule.transform.Find("RenderTilemap");
            if (renderTilemapTransform != null)
            {
                TilemapRenderer renderer = renderTilemapTransform.GetComponent<TilemapRenderer>();
                if (renderer != null)
                {
                    // Negative values - first in list gets highest order
                    renderer.sortingOrder = baseSortingOrder - i;
                    
                    #if UNITY_EDITOR
                    UnityEditor.EditorUtility.SetDirty(renderer);
                    #endif
                    
                    if (debugLogs)
                        Debug.Log($"Updated sorting order for {mapping.tileDef.displayName} to {baseSortingOrder - i}");
                }
            }
        }
    }

    // New public method to update all colors - can be called from custom editor
    public void UpdateAllColors()
    {
        foreach (var mapping in tileDefinitionMappings)
        {
            if (mapping.tileDef == null || mapping.tilemapModule == null) 
                continue;
                
            Transform renderTilemapTransform = mapping.tilemapModule.transform.Find("RenderTilemap");
            if (renderTilemapTransform != null)
            {
                Tilemap renderTilemap = renderTilemapTransform.GetComponent<Tilemap>();
                if (renderTilemap != null)
                {
                    renderTilemap.color = mapping.tileDef.tintColor;
                    
                    #if UNITY_EDITOR
                    UnityEditor.EditorUtility.SetDirty(renderTilemap);
                    #endif
                    
                    if (debugLogs)
                        Debug.Log($"Updated color for {mapping.tileDef.displayName} to {mapping.tileDef.tintColor}");
                }
            }
        }
    }

    void Update()
    {
        HandleTileHover();
        UpdateReversion();
        UpdateDebugUI();
    }

    // ------------------- Timed Reversion Logic ------------------------
    // We'll store a small struct for each cell that is on a countdown
    private struct TimedTileState
    {
        public TileDefinition tileDef;
        public float timeLeft;
    }

    private void UpdateReversion()
    {
        if (timedCells.Count == 0) return;

        // We'll gather cells that are about to revert
        List<Vector3Int> cellsToRevert = null;

        // We'll copy keys to avoid modifying dictionary while iterating
        foreach (var kvp in timedCells.ToList())
        {
            Vector3Int cellPos = kvp.Key;
            TimedTileState state = kvp.Value;
            state.timeLeft -= Time.deltaTime;
            if (state.timeLeft <= 0f)
            {
                // we revert now
                if (cellsToRevert == null) 
                    cellsToRevert = new List<Vector3Int>();
                cellsToRevert.Add(cellPos);
            }
            else
            {
                // store updated time
                timedCells[cellPos] = state;
            }
        }

        if (cellsToRevert != null)
        {
            foreach (var cellPos in cellsToRevert)
            {
                if (timedCells.TryGetValue(cellPos, out TimedTileState st))
                {
                    timedCells.Remove(cellPos);
                    
                    // Always remove the current tile, regardless of doNotRemovePrevious
                    // This is needed for timed disappearing functionality
                    RemoveTile(st.tileDef, cellPos);
                    
                    // If there's a revert-to tile, place it
                    if (st.tileDef.revertToTile != null)
                    {
                        PlaceTile(st.tileDef.revertToTile, cellPos);
                    }
                }
            }
        }
    }

    // Our method to forcibly schedule a tile for timed reversion
    private void RegisterTimedTile(Vector3Int cellPos, TileDefinition tileDef)
    {
        if (tileDef.revertAfterSeconds > 0f && tileDef.revertToTile != null)
        {
            TimedTileState newState;
            newState.tileDef = tileDef;
            newState.timeLeft = tileDef.revertAfterSeconds;
            timedCells[cellPos] = newState;
        }
    }

    // --------------- Re-Use: Placing & Removing Tiles ---------------

    public void PlaceTile(TileDefinition tileDef, Vector3Int cellPos)
    {
        if (!moduleByDefinition.ContainsKey(tileDef))
        {
            Debug.LogWarning($"PlaceTile: {tileDef.displayName} not found in moduleByDefinition.");
            return;
        }
        var module = moduleByDefinition[tileDef];

        // If tileDef is an overlay, do NOT remove the old tile
        // Else we remove the old tile first
        if (!tileDef.keepBottomTile)
        {
            // e.g. if we are placing "DirtWet" which is keepBottomTile=false, 
            // we do remove the old tile from its tilemap
            // but we must find whichever tile is currently there
            TileDefinition existing = FindWhichTileDefinitionAt(cellPos);
            if (existing != null && existing != tileDef)
            {
                RemoveTile(existing, cellPos);
            }
        }

        // Set the cell in the tilemap to have a tile
        // The actual visual appearance is handled by the DualGridTilemapModule system
        // We just need to mark this cell as "filled"
        module.DataTilemap.SetTile(cellPos, ScriptableObject.CreateInstance<Tile>());

        // Set the RenderTilemap color to match the TileDefinition's tintColor
        Transform renderTilemapTransform = module.transform.Find("RenderTilemap");
        if (renderTilemapTransform != null)
        {
            Tilemap renderTilemap = renderTilemapTransform.GetComponent<Tilemap>();
            if (renderTilemap != null)
            {
                renderTilemap.color = tileDef.tintColor;
                
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    UnityEditor.EditorUtility.SetDirty(renderTilemap);
                }
#endif
            }
        }

        // If it has a timed reversion, schedule that
        RegisterTimedTile(cellPos, tileDef);
    }

    public void RemoveTile(TileDefinition tileDef, Vector3Int cellPos)
    {
        if (!moduleByDefinition.ContainsKey(tileDef))
        {
            Debug.LogWarning($"RemoveTile: {tileDef.displayName} not in moduleByDefinition.");
            return;
        }
        var module = moduleByDefinition[tileDef];
        // remove from that tilemap
        module.DataTilemap.SetTile(cellPos, null);

        // Also if this cell was in timedCells for that tile, remove it
        if (timedCells.ContainsKey(cellPos))
        {
            // We only remove if the tile in timedCells is tileDef
            TimedTileState st = timedCells[cellPos];
            if (st.tileDef == tileDef)
            {
                timedCells.Remove(cellPos);
            }
        }
    }

    private TileDefinition FindWhichTileDefinitionAt(Vector3Int cellPos)
    {
        // To ensure we get the top-most visible tile for overlays, we need to check
        // tiles in reverse order (or specifically check overlay tiles first)
        
        // First, try to find any overlay tiles (keepBottomTile = true)
        foreach (var mapping in tileDefinitionMappings)
        {
            if (mapping.tileDef == null || mapping.tilemapModule == null) 
                continue;
                
            // Check specifically for overlay tiles first
            if (mapping.tileDef.keepBottomTile && 
                mapping.tilemapModule.DataTilemap.HasTile(cellPos))
            {
                return mapping.tileDef;
            }
        }
        
        // If no overlay tile found, find any base tile
        foreach (var pair in definitionByModule)
        {
            DualGridTilemapModule module = pair.Key;
            TileDefinition def = pair.Value;

            if (module.DataTilemap.HasTile(cellPos))
            {
                // Found a tile => that's the tile definition
                return def;
            }
        }
        return null;
    }

    // ------------------- Handle Hover & Debug UI (unchanged) -------------------
    private void HandleTileHover()
    {
        if (mainCamera == null || player == null) return;

        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0f;
        Vector3Int cellPos = WorldToCell(mouseWorldPos);
        float distance = Vector2.Distance(player.position, CellCenterWorld(cellPos));

        TileDefinition foundTile = FindWhichTileDefinitionAt(cellPos);

        if (debugLogs)
        {
            string tileName = foundTile != null ? foundTile.displayName : "NULL";
            Debug.Log($"[Hover] cell={cellPos}, tile={tileName}, dist={distance:F2}");
        }

        if (distance <= hoverRadius)
        {
            currentlyHoveredCell = cellPos;
            hoveredTileDef = foundTile;
            if (hoverHighlightObject != null)
            {
                hoverHighlightObject.SetActive(true);
                hoverHighlightObject.transform.position = CellCenterWorld(cellPos);
            }
        }
        else
        {
            currentlyHoveredCell = null;
            hoveredTileDef = null;
            if (hoverHighlightObject != null)
                hoverHighlightObject.SetActive(false);
        }
    }

    private void UpdateDebugUI()
    {
        if (hoveredTileText != null)
        {
            if (currentlyHoveredCell.HasValue)
            {
                string tileName = hoveredTileDef != null ? hoveredTileDef.displayName : "None";
                hoveredTileText.text = $"Hovering: {tileName}";
            }
            else
            {
                hoveredTileText.text = "Hovering: (none)";
            }
        }

        if (currentToolText != null)
        {
            // Changed from FindObjectOfType to FindAnyObjectByType for better performance
            ToolSwitcher sw = Object.FindAnyObjectByType<ToolSwitcher>();
            if (sw != null && sw.CurrentTool != null)
            {
                currentToolText.text = $"Tool: {sw.CurrentTool.toolType}";
            }
            else
            {
                currentToolText.text = "Tool: None";
            }
        }
    }

    private Vector3Int WorldToCell(Vector3 worldPos)
    {
        if (interactionGrid != null)
            return interactionGrid.WorldToCell(worldPos);

        if (tileDefinitionMappings.Count > 0 &&
            tileDefinitionMappings[0].tilemapModule != null &&
            tileDefinitionMappings[0].tilemapModule.DataTilemap != null)
        {
            Grid g = tileDefinitionMappings[0].tilemapModule.DataTilemap.layoutGrid;
            return g.WorldToCell(worldPos);
        }

        return Vector3Int.zero;
    }

    private Vector3 CellCenterWorld(Vector3Int cellPos)
    {
        if (interactionGrid != null)
        {
            Vector3 corner = interactionGrid.CellToWorld(cellPos);
            return corner + interactionGrid.cellSize * 0.5f;
        }

        if (tileDefinitionMappings.Count > 0 &&
            tileDefinitionMappings[0].tilemapModule != null &&
            tileDefinitionMappings[0].tilemapModule.DataTilemap != null)
        {
            Grid g = tileDefinitionMappings[0].tilemapModule.DataTilemap.layoutGrid;
            Vector3 corner = g.CellToWorld(cellPos);
            return corner + g.cellSize * 0.5f;
        }

        return Vector3.zero;
    }
    
    // Update the method to include animation and delayed planting
    private void HandleSeedPlanting(Vector3Int cellPosition)
    {
        // Check if we have a PlantPlacementManager
        PlantPlacementManager plantManager = PlantPlacementManager.Instance;
        if (plantManager == null)
        {
            Debug.LogError("Cannot plant: PlantPlacementManager not found in scene!");
            return;
        }

        // Get the player's GardenerController to trigger animation
        GardenerController gardener = player?.GetComponent<GardenerController>();
        if (gardener == null)
        {
            Debug.LogError("Cannot plant: GardenerController not found on player reference!");
            return;
        }

        // Get world position of cell center for planting
        Vector3 worldPosition = CellCenterWorld(cellPosition);
    
        // Start the planting animation
        gardener.Plant();
    
        // Start a coroutine to plant the seed after the animation completes
        StartCoroutine(PlantAfterAnimation(gardener, plantManager, cellPosition, worldPosition));
    }
    
    // New coroutine to handle delayed planting after animation
    private IEnumerator PlantAfterAnimation(GardenerController gardener, PlantPlacementManager plantManager, 
        Vector3Int cellPosition, Vector3 worldPosition)
    {
        // Wait for the planting animation to complete
        yield return new WaitForSeconds(gardener.plantingDuration);
    
        // Try to plant seed at the cell position
        bool planted = plantManager.TryPlantSeed(cellPosition, worldPosition);
    
        if (debugLogs)
        {
            Debug.Log(planted ? 
                $"Planted seed successfully at cell {cellPosition}" : 
                $"Failed to plant seed at cell {cellPosition}");
        }
    }
    
    public void ApplyToolAction(ToolDefinition toolDef)
    {
        if (!currentlyHoveredCell.HasValue)
            return;

        // If we recognized no tile, do nothing
        if (hoveredTileDef == null)
        {
            if (debugLogs) Debug.Log("ApplyToolAction: No recognized tile at hovered cell.");
            return;
        }

        // Check distance
        float distance = Vector2.Distance(player.position, CellCenterWorld(currentlyHoveredCell.Value));
        if (distance > hoverRadius)
        {
            if (debugLogs)
                Debug.Log($"ApplyToolAction: Cell is {distance:F2} away, above {hoverRadius} radius. Aborting.");
            return;
        }

        if (debugLogs)
            Debug.Log(
                $"[ApplyToolAction] Tool={toolDef.toolType}, fromTile={hoveredTileDef.displayName} at cell={currentlyHoveredCell.Value}");

        // ADDED: Special handling for SeedPouch tool type
        if (toolDef.toolType == ToolType.SeedPouch)
        {
            // Handle seed planting action separately
            HandleSeedPlanting(currentlyHoveredCell.Value);
            return;
        }

        // Find matching rule
        TileInteractionRule rule = interactionLibrary.rules.FirstOrDefault(r =>
            r.tool == toolDef &&
            r.fromTile == hoveredTileDef
        );

        if (rule == null)
        {
            Debug.Log($"No rule for tool {toolDef.toolType} on tile {hoveredTileDef.displayName}.");
            return;
        }

        // Check whether the destination tile has keepBottomTile flag before removing source tile
        if (rule.toTile != null)
        {
            // Only remove the original tile if the new tile doesn't have keepBottomTile set
            if (!rule.toTile.keepBottomTile)
            {
                RemoveTile(hoveredTileDef, currentlyHoveredCell.Value);
            }

            // Place the new tile
            PlaceTile(rule.toTile, currentlyHoveredCell.Value);
        }
        else
        {
            // If there's no destination tile, just remove the current one
            RemoveTile(hoveredTileDef, currentlyHoveredCell.Value);
        }
    }
}