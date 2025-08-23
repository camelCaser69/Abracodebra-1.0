using UnityEngine;
using UnityEngine.Tilemaps;
using WegoSystem; // <-- FIX: Added this line to resolve the namespace issue.

public class TileDefinition : ScriptableObject
{
    public string displayName;    // e.g. "Grass", "Dirt", "Wet Dirt"

    public Color tintColor = Color.white;

    public int revertAfterTicks = 0;  // Changed from float revertAfterSeconds

    public TileDefinition revertToTile;

    public bool keepBottomTile = false;

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