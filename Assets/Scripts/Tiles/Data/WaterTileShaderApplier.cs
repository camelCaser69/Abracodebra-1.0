// FILE: Assets/Scripts/Tiles/WaterTileShaderApplier.cs
using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections;

public class WaterTileShaderApplier : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Apply shader on start")]
    [SerializeField] private bool applyOnStart = true;
    
    [Tooltip("Delay before applying (to ensure tile system is initialized)")]
    [SerializeField] private float applyDelay = 0.5f;
    
    void Start()
    {
        if (applyOnStart)
        {
            StartCoroutine(ApplyWaterShaderDelayed());
        }
    }
    
    IEnumerator ApplyWaterShaderDelayed()
    {
        yield return new WaitForSeconds(applyDelay);
        ApplyWaterShaderToTiles();
    }
    
    public void ApplyWaterShaderToTiles()
    {
        if (TileInteractionManager.Instance == null)
        {
            Debug.LogError("[WaterTileShaderApplier] TileInteractionManager not found!");
            return;
        }
        
        // Find all water tile renderers
        foreach (var mapping in TileInteractionManager.Instance.tileDefinitionMappings)
        {
            if (mapping.tileDef != null && IsWaterTile(mapping.tileDef))
            {
                Transform renderTilemap = mapping.tilemapModule.transform.Find("RenderTilemap");
                if (renderTilemap != null)
                {
                    TilemapRenderer renderer = renderTilemap.GetComponent<TilemapRenderer>();
                    if (renderer != null)
                    {
                        // Let the WaterReflectionManager handle the material
                        if (WaterReflectionManager.Instance != null)
                        {
                            WaterReflectionManager.Instance.waterTilemapRenderer = renderer;
                            Debug.Log($"[WaterTileShaderApplier] Set water tilemap: {mapping.tileDef.displayName}");
                        }
                    }
                }
            }
        }
    }
    
    bool IsWaterTile(TileDefinition tileDef)
    {
        // Check if tile name contains "water" (case insensitive)
        return tileDef.displayName.ToLower().Contains("water");
    }
}