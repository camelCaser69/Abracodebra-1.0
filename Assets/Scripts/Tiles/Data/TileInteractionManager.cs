using UnityEngine;
using UnityEngine.Tilemaps;
using skner.DualGrid;  // Your 3rd-party plugin
using System.Collections.Generic;
using System.Linq;
using TMPro; // if you have UI references for debugging

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

        foreach (var mapping in tileDefinitionMappings)
        {
            if (mapping.tileDef == null || mapping.tilemapModule == null) 
                continue;

            if (!moduleByDefinition.ContainsKey(mapping.tileDef))
            {
                moduleByDefinition[mapping.tileDef] = mapping.tilemapModule;
                definitionByModule[mapping.tilemapModule] = mapping.tileDef;
                if (debugLogs)
                    Debug.Log($"[Mapping] {mapping.tileDef.tileId} => {mapping.tilemapModule.gameObject.name}");
            }
            else
            {
                Debug.LogWarning($"Duplicate tileDef {mapping.tileDef.tileId} in tileDefinitionMappings.");
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
                    // revert to st.tileDef's revertToTile
                    if (st.tileDef.revertToTile != null)
                    {
                        // remove the current tile (unless it was doNotRemovePrevious)
                        // place the revertToTile
                        RemoveTile(st.tileDef, cellPos); 
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
            Debug.LogWarning($"PlaceTile: {tileDef.tileId} not found in moduleByDefinition.");
            return;
        }
        var module = moduleByDefinition[tileDef];

        // If tileDef is an overlay, do NOT remove the old tile
        // Else we remove the old tile first
        if (!tileDef.doNotRemovePrevious)
        {
            // e.g. if we are placing "DirtWet" which is overlay=false, 
            // we do remove the old tile from its tilemap
            // but we must find whichever tile is currently there
            TileDefinition existing = FindWhichTileDefinitionAt(cellPos);
            if (existing != null && existing != tileDef)
            {
                RemoveTile(existing, cellPos);
            }
        }

        // Now place this tile
        module.DataTilemap.SetTile(cellPos, tileDef.tile);

        // If it has a timed reversion, schedule that
        RegisterTimedTile(cellPos, tileDef);
    }

    public void RemoveTile(TileDefinition tileDef, Vector3Int cellPos)
    {
        if (!moduleByDefinition.ContainsKey(tileDef))
        {
            Debug.LogWarning($"RemoveTile: {tileDef.tileId} not in moduleByDefinition.");
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
            string tileName = foundTile != null ? foundTile.tileId : "NULL";
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
                string tileName = hoveredTileDef != null ? hoveredTileDef.tileId : "None";
                hoveredTileText.text = $"Hovering: {tileName}";
            }
            else
            {
                hoveredTileText.text = "Hovering: (none)";
            }
        }

        if (currentToolText != null)
        {
            ToolSwitcher sw = FindObjectOfType<ToolSwitcher>();
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
            Debug.Log($"[ApplyToolAction] Tool={toolDef.toolType}, fromTile={hoveredTileDef.tileId} at cell={currentlyHoveredCell.Value}");

        // Find matching rule
        TileInteractionRule rule = interactionLibrary.rules.FirstOrDefault(r =>
            r.tool == toolDef &&
            r.fromTile == hoveredTileDef
        );

        if (rule == null)
        {
            Debug.Log($"No rule for tool {toolDef.toolType} on tile {hoveredTileDef.tileId}.");
            return;
        }

        // Remove the old tile, place the new tile
        RemoveTile(hoveredTileDef, currentlyHoveredCell.Value);
        if (rule.toTile != null)
            PlaceTile(rule.toTile, currentlyHoveredCell.Value);
    }
}