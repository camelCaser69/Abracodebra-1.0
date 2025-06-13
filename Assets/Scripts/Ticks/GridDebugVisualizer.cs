using UnityEngine;
using System.Collections.Generic;
using WegoSystem;

public class GridDebugVisualizer : MonoBehaviour {
    public static GridDebugVisualizer Instance { get; private set; }
    
    [Header("Visualization Settings")]
    [SerializeField] bool showRadiusVisualizations = true;
    [SerializeField] float tileVisualizationAlpha = 0.3f;
    [SerializeField] GameObject tilePrefab; // Simple square sprite
    
    [Header("Colors")]
    [SerializeField] Color animalSearchRadiusColor = new Color(1f, 0.5f, 0f, 0.3f);
    [SerializeField] Color plantPoopRadiusColor = new Color(0.6f, 0.4f, 0.2f, 0.3f);
    [SerializeField] Color scentRadiusColor = new Color(1f, 1f, 0f, 0.3f);
    [SerializeField] Color fireflyPhotosynthesisColor = new Color(0f, 1f, 0.5f, 0.3f);
    [SerializeField] Color toolUseRadiusColor = new Color(0f, 0.5f, 1f, 0.3f);
    
    Dictionary<object, List<GameObject>> visualizedRadii = new Dictionary<object, List<GameObject>>();
    
    void Awake() {
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
    
    void OnDestroy() {
        ClearAllVisualizations();
        if (Instance == this) Instance = null;
    }
    
    public void VisualizeRadius(object source, GridPosition center, int radius, Color color, float duration = 0f) {
        if (!showRadiusVisualizations || tilePrefab == null) return;
        
        ClearVisualization(source);
        
        var tiles = GridRadiusUtility.GetTilesInCircle(center, radius);
        var tileObjects = new List<GameObject>();
        
        foreach (var tile in tiles) {
            Vector3 worldPos = GridPositionManager.Instance.GridToWorld(tile);
            GameObject tileVis = Instantiate(tilePrefab, worldPos, Quaternion.identity, transform);
            
            SpriteRenderer sr = tileVis.GetComponent<SpriteRenderer>();
            if (sr != null) {
                Color finalColor = color;
                finalColor.a = tileVisualizationAlpha;
                sr.color = finalColor;
                sr.sortingOrder = -100; // Behind everything
            }
            
            tileObjects.Add(tileVis);
            
            if (duration > 0) {
                Destroy(tileVis, duration);
            }
        }
        
        if (duration <= 0) {
            visualizedRadii[source] = tileObjects;
        }
    }
    
    public void ClearVisualization(object source) {
        if (visualizedRadii.TryGetValue(source, out var tiles)) {
            foreach (var tile in tiles) {
                if (tile != null) Destroy(tile);
            }
            visualizedRadii.Remove(source);
        }
    }
    
    public void ClearAllVisualizations() {
        foreach (var kvp in visualizedRadii) {
            foreach (var tile in kvp.Value) {
                if (tile != null) Destroy(tile);
            }
        }
        visualizedRadii.Clear();
    }
    
    // Convenience methods for specific systems
    public void VisualizeAnimalSearchRadius(AnimalController animal, GridPosition center, int radius) {
        VisualizeRadius(animal, center, radius, animalSearchRadiusColor);
    }
    
    public void VisualizePlantPoopRadius(PlantGrowth plant, GridPosition center, int radius) {
        VisualizeRadius(plant, center, radius, plantPoopRadiusColor);
    }
    
    public void VisualizeScentRadius(ScentSource scent, GridPosition center, int radius) {
        VisualizeRadius(scent, center, radius, scentRadiusColor);
    }
    
    public void VisualizeToolUseRadius(GridPosition center, int radius, float duration = 1f) {
        VisualizeRadius("tool_use", center, radius, toolUseRadiusColor, duration);
    }
}