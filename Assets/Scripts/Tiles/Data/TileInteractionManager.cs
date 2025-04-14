using UnityEngine;
using UnityEngine.Tilemaps;
using skner.DualGrid;
using System.Collections.Generic;
using System.Linq;
using TMPro;
#if UNITY_EDITOR
using UnityEditor; // For EditorUtility and asset marking
#endif

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

    [Header("Overlay Settings")]
    [Tooltip("Reference to the custom TilemapOverlay shader (Required for overlays)")]
    public Shader overlayShader;
    [Tooltip("Assign a Material asset using the 'Custom/TilemapOverlay' shader. This is used in Edit Mode for all previews.")]
    public Material editModeSharedMaterial;

    [Header("Debug / UI")]
    public bool debugLogs = false;
    public TextMeshProUGUI hoveredTileText;
    public TextMeshProUGUI currentToolText;

    // Private dictionaries for mapping definitions to modules.
    private Dictionary<TileDefinition, DualGridTilemapModule> moduleByDefinition;
    private Dictionary<DualGridTilemapModule, TileDefinition> definitionByModule;
    // Hover state – note: our hover detection now uses a reverse-iteration method (old behavior) so that the underlying tile is returned.
    private Vector3Int? currentlyHoveredCell = null;
    private TileDefinition hoveredTileDef = null;
    // Timed reversion storage
    private Dictionary<Vector3Int, TimedTileState> timedCells = new Dictionary<Vector3Int, TimedTileState>();

    private struct TimedTileState { public TileDefinition tileDef; public float timeLeft; }

    #region Initialization and Setup
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (overlayShader == null)
        {
            Debug.LogError($"[{nameof(TileInteractionManager)}] Overlay Shader is not assigned. Overlay effects will not work.", this);
        }
        if (editModeSharedMaterial == null)
        {
            Debug.LogWarning($"[{nameof(TileInteractionManager)}] Edit Mode Shared Material is not assigned. Edit mode rendering may not show overlays correctly.", this);
        }
        else if (editModeSharedMaterial.shader != overlayShader)
        {
            Debug.LogWarning($"[{nameof(TileInteractionManager)}] The assigned Edit Mode Shared Material does not use the assigned Overlay Shader. Edit mode previews may be incorrect.", this);
        }

        moduleByDefinition = new Dictionary<TileDefinition, DualGridTilemapModule>();
        definitionByModule = new Dictionary<DualGridTilemapModule, TileDefinition>();

        SetupTilemaps();
    }

    void Start()
    {
        if (moduleByDefinition == null || moduleByDefinition.Count == 0)
            SetupTilemaps();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    // Sets up mappings and assigns initial sorting orders and colors.
    private void SetupTilemaps()
    {
        moduleByDefinition.Clear();
        definitionByModule.Clear();

        for (int i = 0; i < tileDefinitionMappings.Count; i++)
        {
            var mapping = tileDefinitionMappings[i];
            if (mapping.tileDef == null || mapping.tilemapModule == null)
                continue;

            if (!moduleByDefinition.ContainsKey(mapping.tileDef))
            {
                moduleByDefinition[mapping.tileDef] = mapping.tilemapModule;
                definitionByModule[mapping.tilemapModule] = mapping.tileDef;

                Transform renderTilemapTransform = mapping.tilemapModule.transform.Find("RenderTilemap");
                if (renderTilemapTransform != null)
                {
                    TilemapRenderer renderer = renderTilemapTransform.GetComponent<TilemapRenderer>();
                    Tilemap tilemap = renderTilemapTransform.GetComponent<Tilemap>();

                    if (renderer != null)
                    {
                        renderer.sortingOrder = baseSortingOrder - i;
                        if (debugLogs)
                            Debug.Log($"Setup: Sorting order for {mapping.tileDef.displayName} set to {baseSortingOrder - i}");
                    }
                    if (tilemap != null)
                    {
                        tilemap.color = mapping.tileDef.tintColor;
                        if (debugLogs)
                            Debug.Log($"Setup: Tilemap.color for {mapping.tileDef.displayName} set to {mapping.tileDef.tintColor}");
                    }
                    if (renderer != null)
                        ApplyOverlayToTilemap(mapping.tileDef, renderer);
                }
                if (debugLogs)
                    Debug.Log($"[Mapping] {mapping.tileDef.displayName} => {mapping.tilemapModule.gameObject.name}");
            }
            else Debug.LogWarning($"Duplicate tileDef {mapping.tileDef.displayName}");
        }
    }
    #endregion

    #region Editor Convenience Methods
#if UNITY_EDITOR
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
                    renderer.sortingOrder = baseSortingOrder - i;
                    EditorUtility.SetDirty(renderer);
                    if (debugLogs)
                        Debug.Log($"Updated sorting order for {mapping.tileDef.displayName} to {baseSortingOrder - i}");
                }
            }
        }
    }

    public void UpdateAllColors()
    {
        foreach (var mapping in tileDefinitionMappings)
        {
            if (mapping.tileDef == null || mapping.tilemapModule == null)
                continue;
            Transform renderTilemapTransform = mapping.tilemapModule.transform.Find("RenderTilemap");
            if (renderTilemapTransform != null)
            {
                Tilemap tilemap = renderTilemapTransform.GetComponent<Tilemap>();
                if (tilemap != null)
                {
                    tilemap.color = mapping.tileDef.tintColor;
                    EditorUtility.SetDirty(tilemap);
                    if (debugLogs)
                        Debug.Log($"Updated Tilemap.color for {mapping.tileDef.displayName} to {mapping.tileDef.tintColor}");
                }
            }
        }
    }

    public void UpdateAllOverlays()
    {
        if (Application.isPlaying)
        {
            Debug.LogWarning("UpdateAllOverlays button is primarily for Edit Mode previews. Changes in Play Mode are handled dynamically.");
            return;
        }
        Debug.Log("Updating Edit Mode overlay previews. Note: All tilemaps will reflect the settings of the *last* processed TileDefinition due to shared material usage.");
        foreach (var mapping in tileDefinitionMappings)
        {
            if (mapping.tileDef == null || mapping.tilemapModule == null)
                continue;
            Transform renderTilemapTransform = mapping.tilemapModule.transform.Find("RenderTilemap");
            if (renderTilemapTransform != null)
            {
                TilemapRenderer renderer = renderTilemapTransform.GetComponent<TilemapRenderer>();
                if (renderer != null)
                    ApplyOverlayToTilemap(mapping.tileDef, renderer);
            }
        }
        Debug.Log("Edit Mode overlay preview update complete.");
    }
#endif
    #endregion

    #region Overlay Methods
    public void ApplyOverlayToTilemap(TileDefinition tileDef, TilemapRenderer renderer)
    {
        if (tileDef == null || renderer == null || overlayShader == null)
        {
            if (renderer != null && renderer.sharedMaterial != null)
                renderer.sharedMaterial = null;
            return;
        }

        Tilemap tilemap = renderer.GetComponent<Tilemap>();

        bool useOverlayFeature = tileDef.overlays != null &&
                                 tileDef.overlays.Length > 0 &&
                                 tileDef.overlays[0].overlayTexture != null;
        bool useAnimationFeature = useOverlayFeature && tileDef.overlays[0].useAnimation;

        if (Application.isPlaying)
        {
            Material material;
            bool needsNewInstance = true;
            if (renderer.sharedMaterial != null && renderer.sharedMaterial.shader == overlayShader)
            {
                material = renderer.material;
                if (material.name.Contains("Instance"))
                    needsNewInstance = false;
            }
            if (needsNewInstance)
            {
                material = new Material(overlayShader);
                material.name = $"OverlayMatInstance_{tileDef.displayName}_{renderer.GetInstanceID()}";
                renderer.material = material;
            }
            else
                material = renderer.material;

            material.SetKeyword(new UnityEngine.Rendering.LocalKeyword(overlayShader, "_USEOVERLAY_ON"), useOverlayFeature);
            material.SetKeyword(new UnityEngine.Rendering.LocalKeyword(overlayShader, "_USEANIMATION_ON"), useAnimationFeature);

            if (useOverlayFeature)
                ApplyOverlayProperties(material, tileDef.overlays[0]);
            else
                material.SetTexture("_OverlayTex", null);
        }
        else
        {
            if (editModeSharedMaterial == null)
            {
                if (renderer.sharedMaterial != null)
                    renderer.sharedMaterial = null;
                return;
            }
            if (renderer.sharedMaterial != editModeSharedMaterial)
                renderer.sharedMaterial = editModeSharedMaterial;

            editModeSharedMaterial.SetKeyword(new UnityEngine.Rendering.LocalKeyword(overlayShader, "_USEOVERLAY_ON"), useOverlayFeature);
            editModeSharedMaterial.SetKeyword(new UnityEngine.Rendering.LocalKeyword(overlayShader, "_USEANIMATION_ON"), useAnimationFeature);

            if (useOverlayFeature)
                ApplyOverlayProperties(editModeSharedMaterial, tileDef.overlays[0]);
            else
                editModeSharedMaterial.SetTexture("_OverlayTex", null);

#if UNITY_EDITOR
            EditorUtility.SetDirty(editModeSharedMaterial);
            EditorUtility.SetDirty(renderer);
            if (tilemap != null)
            {
                tilemap.color = tileDef.tintColor;
                EditorUtility.SetDirty(tilemap);
            }
#endif
        }
    }

    private void ApplyOverlayProperties(Material mat, TextureOverlaySettings overlay)
    {
        if (mat == null || overlay == null)
            return;
        mat.SetTexture("_OverlayTex", overlay.overlayTexture);
        mat.SetColor("_OverlayColor", overlay.tintColor);
        mat.SetFloat("_OverlayScaleValue", overlay.scale);
        mat.SetVector("_OverlayOffset", new Vector4(overlay.offset.x, overlay.offset.y, 0, 0));
        if (overlay.useAnimation)
        {
            mat.SetFloat("_AnimSpeed", overlay.animationSpeed);
            mat.SetFloat("_AnimTiles", Mathf.Max(1f, overlay.animationTiles));
        }
    }
    #endregion

    #region Hover Detection and Debug UI
    // In this version, we revert to the reverse-iteration method (FindWhichTileDefinitionAt) so that the underlying tile is returned.
    private TileDefinition FindWhichTileDefinitionAt(Vector3Int cellPos)
    {
        for (int i = tileDefinitionMappings.Count - 1; i >= 0; i--)
        {
            var mapping = tileDefinitionMappings[i];
            if (mapping.tileDef != null &&
                mapping.tilemapModule != null &&
                mapping.tilemapModule.DataTilemap.HasTile(cellPos))
            {
                return mapping.tileDef;
            }
        }
        return null;
    }

    private void HandleTileHover()
    {
        if (mainCamera == null || player == null)
            return;

        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0f;
        Vector3Int cellPos = WorldToCell(mouseWorldPos);
        if (cellPos.Equals(new Vector3Int(int.MinValue, int.MinValue, int.MinValue)))
            return;
        float distance = Vector2.Distance(player.position, CellCenterWorld(cellPos));
        // For hover display, we want the top layer – you mentioned that hovered tile text is correct, so we call GetHoveredTileInfo.
        var hovered = GetHoveredTileInfo(cellPos);
        if (distance <= hoverRadius && hovered != null)
        {
            currentlyHoveredCell = cellPos;
            hoveredTileDef = hovered.Value.tileDef;
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

    // For hover display, we use forward iteration so that the top layer is shown.
    private (TileDefinition tileDef, Vector3Int cellPos)? GetHoveredTileInfo(Vector3Int cellPos)
    {
        int bestSortingOrder = int.MinValue;
        TileDefinition bestTileDef = null;
        // Forward iteration: index 0 is highest priority for display.
        for (int i = 0; i < tileDefinitionMappings.Count; i++)
        {
            var mapping = tileDefinitionMappings[i];
            if (mapping.tileDef == null || mapping.tilemapModule == null)
                continue;
            var dataTilemap = mapping.tilemapModule.DataTilemap;
            if (dataTilemap != null && dataTilemap.HasTile(cellPos))
            {
                int currentOrder = baseSortingOrder - i;
                if (currentOrder > bestSortingOrder)
                {
                    bestSortingOrder = currentOrder;
                    bestTileDef = mapping.tileDef;
                }
            }
        }
        if (bestTileDef != null)
            return (bestTileDef, cellPos);
        return null;
    }

    private void UpdateDebugUI()
    {
        if (hoveredTileText != null)
        {
            string tileName = "None";
            if (currentlyHoveredCell.HasValue && hoveredTileDef != null)
                tileName = hoveredTileDef.displayName;
            else if (currentlyHoveredCell.HasValue)
                tileName = "(Empty)";
            hoveredTileText.text = $"Hover: {tileName}";
        }
        if (currentToolText != null)
        {
            ToolSwitcher sw = FindAnyObjectByType<ToolSwitcher>();
            string toolName = "None";
            if (sw != null && sw.CurrentTool != null)
                toolName = sw.CurrentTool.toolType.ToString();
            currentToolText.text = $"Tool: {toolName}";
        }
    }
    #endregion

    #region Timed Reversion and Tile Placement/Removal
    private void UpdateReversion()
    {
        if (timedCells.Count == 0)
            return;
        List<Vector3Int> cellsToRevert = null;
        foreach (var kvp in timedCells.ToList())
        {
            Vector3Int cellPos = kvp.Key;
            TimedTileState state = kvp.Value;
            state.timeLeft -= Time.deltaTime;
            if (state.timeLeft <= 0f)
            {
                if (cellsToRevert == null)
                    cellsToRevert = new List<Vector3Int>();
                cellsToRevert.Add(cellPos);
            }
            else
            {
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
                    RemoveTile(st.tileDef, cellPos);
                    if (st.tileDef.revertToTile != null)
                        PlaceTile(st.tileDef.revertToTile, cellPos);
                }
            }
        }
    }

    private void RegisterTimedTile(Vector3Int cellPos, TileDefinition tileDef)
    {
        if (tileDef.revertAfterSeconds > 0f)
        {
            TimedTileState newState = new TimedTileState
            {
                tileDef = tileDef,
                timeLeft = tileDef.revertAfterSeconds
            };
            timedCells[cellPos] = newState;
        }
        else
        {
            timedCells.Remove(cellPos);
        }
    }

    public void PlaceTile(TileDefinition tileDef, Vector3Int cellPos)
    {
        if (!moduleByDefinition.TryGetValue(tileDef, out var module))
        {
            Debug.LogWarning($"PlaceTile: {tileDef.displayName} not found in moduleByDefinition.");
            return;
        }
        if (!tileDef.keepBottomTile)
        {
            TileDefinition existing = FindWhichTileDefinitionAt(cellPos);
            if (existing != null && existing != tileDef)
                RemoveTile(existing, cellPos);
        }
        Tile presenceTile = ScriptableObject.CreateInstance<Tile>();
        module.DataTilemap.SetTile(cellPos, presenceTile);

        Transform renderTilemapTransform = module.transform.Find("RenderTilemap");
        if (renderTilemapTransform != null)
        {
            Tilemap tilemap = renderTilemapTransform.GetComponent<Tilemap>();
            TilemapRenderer renderer = renderTilemapTransform.GetComponent<TilemapRenderer>();
            if (tilemap != null)
                tilemap.color = tileDef.tintColor;
            if (renderer != null)
                ApplyOverlayToTilemap(tileDef, renderer);
        }
        RegisterTimedTile(cellPos, tileDef);
    }

    public void RemoveTile(TileDefinition tileDef, Vector3Int cellPos)
    {
        if (!moduleByDefinition.TryGetValue(tileDef, out var module))
        {
            Debug.LogWarning($"RemoveTile: {tileDef.displayName} not in moduleByDefinition.");
            return;
        }
        module.DataTilemap.SetTile(cellPos, null);
        timedCells.Remove(cellPos);

        Transform renderTilemapTransform = module.transform.Find("RenderTilemap");
        if (renderTilemapTransform != null)
        {
            TilemapRenderer renderer = renderTilemapTransform.GetComponent<TilemapRenderer>();
            if (renderer != null)
            {
                TileDefinition remainingTile = FindWhichTileDefinitionAt(cellPos);
                if (remainingTile != null)
                    ApplyOverlayToTilemap(remainingTile, renderer);
#if UNITY_EDITOR
                else if (!Application.isPlaying)
                {
                    if (editModeSharedMaterial != null)
                    {
                        renderer.sharedMaterial = editModeSharedMaterial;
                        editModeSharedMaterial.SetKeyword(new UnityEngine.Rendering.LocalKeyword(overlayShader, "_USEOVERLAY_ON"), false);
                        editModeSharedMaterial.SetKeyword(new UnityEngine.Rendering.LocalKeyword(overlayShader, "_USEANIMATION_ON"), false);
                        EditorUtility.SetDirty(editModeSharedMaterial);
                    }
                    else
                    {
                        renderer.sharedMaterial = null;
                    }
                    EditorUtility.SetDirty(renderer);
                }
#endif
            }
        }
    }
    #endregion

    #region Coordinate Conversion and Tool Action
    private Vector3Int WorldToCell(Vector3 worldPos)
    {
        Grid targetGrid = interactionGrid;
        if (targetGrid == null)
        {
            if (tileDefinitionMappings.Count > 0 &&
                tileDefinitionMappings[0].tilemapModule?.DataTilemap?.layoutGrid != null)
                targetGrid = tileDefinitionMappings[0].tilemapModule.DataTilemap.layoutGrid;
            else
            {
                Debug.LogWarning($"[{nameof(TileInteractionManager)}] No Grid reference found for WorldToCell.", this);
                return new Vector3Int(int.MinValue, int.MinValue, int.MinValue);
            }
        }
        return targetGrid.WorldToCell(worldPos);
    }

    private Vector3 CellCenterWorld(Vector3Int cellPos)
    {
        Grid targetGrid = interactionGrid;
        if (targetGrid == null)
        {
            if (tileDefinitionMappings.Count > 0 &&
                tileDefinitionMappings[0].tilemapModule?.DataTilemap?.layoutGrid != null)
                targetGrid = tileDefinitionMappings[0].tilemapModule.DataTilemap.layoutGrid;
            else
            {
                Debug.LogWarning($"[{nameof(TileInteractionManager)}] No Grid reference found for CellCenterWorld.", this);
                return Vector3.zero;
            }
        }
        return targetGrid.GetCellCenterWorld(cellPos);
    }

    // ------------------- Tool Action Logic -------------------
    public void ApplyToolAction(ToolDefinition toolDef)
    {
        if (!currentlyHoveredCell.HasValue)
            return;
        Vector3Int cell = currentlyHoveredCell.Value;

        // For tool actions, we need the "base" tile in the cell.
        // We always use the reverse-iteration method (old behavior) to get the underlying tile.
        TileDefinition currentTileDef = FindWhichTileDefinitionAt(cell);
        if (currentTileDef == null)
        {
            if (debugLogs)
                Debug.Log("ApplyToolAction: No recognized tile at hovered cell.");
            return;
        }
        float distance = Vector2.Distance(player.position, CellCenterWorld(cell));
        if (distance > hoverRadius)
        {
            if (debugLogs)
                Debug.Log($"ApplyToolAction: Cell is {distance:F2} away (max {hoverRadius}). Aborting.");
            return;
        }
        if (debugLogs)
            Debug.Log($"[ApplyToolAction] Tool={toolDef.toolType}, FromTile={currentTileDef.displayName}, Cell={cell}");

        // Look up the rule using reference equality (as was the case in the old version)
        TileInteractionRule rule = interactionLibrary?.rules.FirstOrDefault(r =>
            r.tool == toolDef &&
            r.fromTile == currentTileDef
        );
        if (rule == null)
        {
            if (debugLogs)
                Debug.Log($"No rule found for Tool: {toolDef.toolType} on Tile: {currentTileDef.displayName}.");
            return;
        }
        // Execute the rule
        if (rule.toTile != null)
        {
            PlaceTile(rule.toTile, cell);
            if (debugLogs)
                Debug.Log($"Applied Rule: Placed {rule.toTile.displayName}.");
        }
        else
        {
            RemoveTile(currentTileDef, cell);
            if (debugLogs)
                Debug.Log($"Applied Rule: Removed {currentTileDef.displayName}.");
        }
        // Immediately update hover state after action.
        hoveredTileDef = FindWhichTileDefinitionAt(cell);
        UpdateDebugUI();
    }
    #endregion

    #region Update Loop
    void Update()
    {
        HandleTileHover();
        UpdateReversion();
        UpdateDebugUI();
    }
    #endregion
}
