using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "TileDefinition", menuName = "Tiles/Tile Definition")]
public class TileDefinition : ScriptableObject
{
    [Header("Basic Identification")]
    [Tooltip("Display name for this tile type (used in UI and debugging)")]
    public string displayName;    // e.g. "Grass", "Dirt", "Wet Dirt"
    
    [Header("Visual Properties")]
    [Tooltip("Optional tint color to apply to the RenderTilemap")]
    public Color tintColor = Color.white;
    
    [Header("Auto-Reversion (optional)")]
    [Tooltip("If > 0, after this many seconds, the tile reverts to 'revertToTile'.")]
    public float revertAfterSeconds = 0f;

    [Tooltip("If revertAfterSeconds > 0, tile reverts to this tile definition.")]
    public TileDefinition revertToTile;

    [Header("Overlay Option")]
    [Tooltip("If true, this tile will be placed on top without removing the tile underneath ")]
    public bool keepBottomTile = false;
    
    [Header("Special Properties")]
    [Tooltip("If true, this tile will use water reflection shader")]
    public bool isWaterTile = false;

#if UNITY_EDITOR
    // This method will be called from the custom editor
    public void UpdateColor()
    {
        // Find the TileInteractionManager in the scene using the non-deprecated method
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