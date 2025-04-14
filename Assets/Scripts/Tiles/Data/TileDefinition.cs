using UnityEngine;
using UnityEngine.Tilemaps;
using System;

[Serializable]
public class TextureOverlaySettings
{
    [Tooltip("The texture to overlay on this tilemap")]
    public Texture2D overlayTexture;
    
    [Tooltip("Tint color for the overlay texture")]
    public Color tintColor = Color.white;
    
    [Tooltip("Scale of the overlay texture (higher value = bigger texture)")]
    [Range(0.1f, 20f)]
    public float scale = 1.0f;
    
    [Tooltip("Offset for the overlay texture")]
    public Vector2 offset = Vector2.zero;
    
    [Tooltip("Should the texture repeat as tiles?")]
    public bool tileTexture = false;
    
    [Tooltip("Should this texture be animated?")]
    public bool useAnimation = false;
    
    [Tooltip("Animation speed (tiles per second)")]
    [Range(0.1f, 10f)]
    public float animationSpeed = 1.0f;
    
    [Tooltip("Number of animation frames along X axis")]
    [Range(1, 32)]
    public int animationTiles = 1;
}

[CreateAssetMenu(fileName = "TileDefinition", menuName = "Tiles/Tile Definition")]
public class TileDefinition : ScriptableObject
{
    [Header("Basic Identification")]
    [Tooltip("Display name for this tile type (used in UI and debugging)")]
    public string displayName;    // e.g. "Grass", "Dirt", "Wet Dirt"
    
    [Header("Visual Properties")]
    [Tooltip("Optional tint color to apply to the RenderTilemap")]
    public Color tintColor = Color.white;
    
    [Header("Texture Overlays")]
    [Tooltip("Texture overlays for this tile type")]
    public TextureOverlaySettings[] overlays;
    
    [Header("Auto-Reversion (optional)")]
    [Tooltip("If > 0, after this many seconds, the tile reverts to 'revertToTile'.")]
    public float revertAfterSeconds = 0f;

    [Tooltip("If revertAfterSeconds > 0, tile reverts to this tile definition.")]
    public TileDefinition revertToTile;

    [Header("Overlay Option")]
    [Tooltip("If true, this tile will be placed on top without removing the tile underneath ")]
    public bool keepBottomTile = false;

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

    public void UpdateOverlays()
    {
        // Find the TileInteractionManager in the scene
        var manager = UnityEngine.Object.FindAnyObjectByType<TileInteractionManager>();
        if (manager == null) return;

        foreach (var mapping in manager.tileDefinitionMappings)
        {
            if (mapping.tileDef == this && mapping.tilemapModule != null)
            {
                Transform renderTilemapTransform = mapping.tilemapModule.transform.Find("RenderTilemap");
                if (renderTilemapTransform != null)
                {
                    TilemapRenderer renderer = renderTilemapTransform.GetComponent<TilemapRenderer>();
                    if (renderer != null)
                    {
                        // Apply the overlay material and settings
                        manager.ApplyOverlayToTilemap(mapping.tileDef, renderer);
                        UnityEditor.EditorUtility.SetDirty(renderer);
                    }
                }
            }
        }
    }
#endif
}