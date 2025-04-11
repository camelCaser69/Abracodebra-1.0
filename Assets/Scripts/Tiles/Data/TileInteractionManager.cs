using UnityEngine;
using UnityEngine.Tilemaps;
using skner.DualGrid;
using System.Collections.Generic;
using System.Linq;
using TMPro; // For optional text fields (if you use TextMeshPro)

public class TileInteractionManager : MonoBehaviour
{
    public static TileInteractionManager Instance { get; private set; }

    [System.Serializable]
    public class TileDefinitionMapping
    {
        [Tooltip("Which tile definition (ScriptableObject) this refers to.")]
        public TileDefinition tileDef;
        
        [Tooltip("Which DualGridTilemapModule is used to place/remove that tile.")]
        public DualGridTilemapModule tilemapModule;
    }

    [Header("Tile Definition Mappings")]
    [Tooltip("For each tile type, specify which module is responsible for placing it.")]
    public List<TileDefinitionMapping> tileDefinitionMappings;

    [Header("Interaction Library")]
    [Tooltip("Reference to the ScriptableObject that holds your (Tool + FromTile) => ToTile rules.")]
    public TileInteractionLibrary interactionLibrary;

    [Header("Grid & Scene References")]
    [Tooltip("The grid your tilemaps belong to (optional). If omitted, we use the first module's layoutGrid.")]
    public Grid interactionGrid;
    [Tooltip("Camera used for converting mouse position to world pos.")]
    public Camera mainCamera;
    [Tooltip("Player transform for distance checks.")]
    public Transform player;
    [Tooltip("Max distance from player to allow tile interactions.")]
    public float hoverRadius = 3f;
    [Tooltip("Prefab or object that highlights hovered tiles.")]
    public GameObject hoverHighlightObject;

    [Header("Debug / UI")]
    [Tooltip("Set to true to see debug logs each frame.")]
    public bool debugLogs = false;
    [Tooltip("Optional: Text field that shows the tile we’re hovering.")]
    public TextMeshProUGUI hoveredTileText;
    [Tooltip("Optional: Text field that shows the selected tool.")]
    public TextMeshProUGUI currentToolText;

    // Internal dictionary for quick lookups:
    // - tileDef => tilemapModule
    // - We'll also store them in reverse to detect which tile is present at a cell
    private Dictionary<TileDefinition, DualGridTilemapModule> moduleByDefinition;
    private Dictionary<DualGridTilemapModule, TileDefinition> definitionByModule; 

    // Cell currently hovered (if any)
    private Vector3Int? currentlyHoveredCell = null;
    // The tile we recognized at that cell
    private TileDefinition hoveredTileDef = null;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Build dictionary for tileDef => module
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

    private void Update()
    {
        HandleTileHover();
        UpdateDebugUI();
    }

    /// <summary>
    /// Called by PlayerTileInteractor when user left-clicks with a certain tool.
    /// </summary>
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

    /// <summary>
    /// Called each frame in Update to see which cell is under the mouse 
    /// and which tile is placed, if any.
    /// </summary>
    private void HandleTileHover()
    {
        if (mainCamera == null || player == null)
            return;

        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0f;
        Vector3Int cellPos = WorldToCell(mouseWorldPos);
        float distance = Vector2.Distance(player.position, CellCenterWorld(cellPos));

        // We'll see which tile is at this cell, if any
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

    /// <summary>
    /// Identify which tile definition is placed at cellPos, by checking each DualGridTilemapModule.
    /// Whichever module hasTile, we return the matching tile definition. 
    /// If multiple modules have a tile, we pick the first found. 
    /// If none have a tile, return null.
    /// </summary>
    private TileDefinition FindWhichTileDefinitionAt(Vector3Int cellPos)
    {
        foreach (var pair in definitionByModule)
        {
            DualGridTilemapModule module = pair.Key;
            TileDefinition def = pair.Value;

            if (module.DataTilemap.HasTile(cellPos))
            {
                // Found a tile in this module => that's our tile definition
                return def;
            }
        }
        return null;
    }

    private void RemoveTile(TileDefinition tileDef, Vector3Int cellPos)
    {
        if (!moduleByDefinition.ContainsKey(tileDef))
        {
            Debug.LogWarning($"RemoveTile: {tileDef.tileId} was not in moduleByDefinition.");
            return;
        }
        var module = moduleByDefinition[tileDef];
        module.DataTilemap.SetTile(cellPos, null);
    }

    private void PlaceTile(TileDefinition tileDef, Vector3Int cellPos)
    {
        if (!moduleByDefinition.ContainsKey(tileDef))
        {
            Debug.LogWarning($"PlaceTile: {tileDef.tileId} was not in moduleByDefinition.");
            return;
        }
        var module = moduleByDefinition[tileDef];
        module.DataTilemap.SetTile(cellPos, tileDef.tile);
    }

    private Vector3Int WorldToCell(Vector3 worldPos)
    {
        // If user assigned a Grid
        if (interactionGrid != null)
            return interactionGrid.WorldToCell(worldPos);

        // else use the layoutGrid of the first module
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
        // If user assigned a Grid
        if (interactionGrid != null)
        {
            Vector3 corner = interactionGrid.CellToWorld(cellPos);
            return corner + interactionGrid.cellSize * 0.5f;
        }

        // else fallback to the first mapping's layoutGrid
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

    private void UpdateDebugUI()
    {
        // Show "Hovering: tile" and "Current Tool: something"
        // (If you didn't assign these fields in Inspector, do nothing)

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
            // We'll see if there's a PlayerTileInteractor or ToolSwitcher to get the current tool
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
}
