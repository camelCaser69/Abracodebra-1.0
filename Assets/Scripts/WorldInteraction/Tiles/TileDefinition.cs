using UnityEngine;
using UnityEngine.Tilemaps;
using WegoSystem;

public class TileDefinition : ScriptableObject
{
    [Header("Display")]
    public string displayName;    // e.g. "Grass", "Dirt", "Wet Dirt"

    [Header("Interaction Priority")]
    [Tooltip("Higher priority tiles are detected first when multiple tiles overlap. " +
             "Use higher values for surface tiles (Grass=100, TilledSoil=90) and lower for underlay tiles (Dirt=10).")]
    [Range(0, 200)]
    public int interactionPriority = 50;

    [Header("Visuals")]
    public Color tintColor = Color.white;

    [Header("Tile Behavior")]
    [Tooltip("If > 0, this tile will revert after this many ticks")]
    public int revertAfterTicks = 0;

    [Tooltip("The tile to revert to when revertAfterTicks expires")]
    public TileDefinition revertToTile;

    [Tooltip("If true, the tile below this one is kept when this tile is placed")]
    public bool keepBottomTile = false;

    [Tooltip("If true, this tile is treated as water for reflection masking")]
    public bool isWaterTile = false;

#if UNITY_EDITOR
    public void UpdateColor()
    {
        var manager = UnityEngine.Object.FindAnyObjectByType<TileInteractionManager>();
        if (manager == null) return;

        foreach (var mapping in manager.tileDefinitionMappings)
        {
            if (mapping.tileDef == this && mapping.tilemapModule != null)
            {
                Transform renderTilemapTransform = mapping.tilemapModule.transform.Find("RenderTilemap");
                if (renderTilemapTransform != null)
                {
                    Tilemap renderTilemap = renderTilemapTransform.GetComponent<Tilemap>();
                    if (renderTilemap != null)
                    {
                        renderTilemap.color = tintColor;
                        UnityEditor.EditorUtility.SetDirty(renderTilemap);
                    }
                }
            }
        }
    }
#endif
}