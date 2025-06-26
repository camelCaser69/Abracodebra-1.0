using System.Collections.Generic;
using UnityEngine;
using WegoSystem;

public class GridDebugVisualizer : MonoBehaviour
{
    public static GridDebugVisualizer Instance { get; set; }

    // Enum to define what kind of radius we are showing
    public enum RadiusType
    {
        AnimalSearch,
        PlantPoop,
        Scent,
        FireflyPhotosynthesis,
        ToolUse
    }

    [SerializeField] bool showRadiusVisualizations = true;
    [SerializeField] float tileVisualizationAlpha = 0.3f;
    [SerializeField] GameObject tilePrefab;

    [Header("Radius Colors")]
    [SerializeField] Color animalSearchRadiusColor = new Color(1f, 0.5f, 0f, 0.3f);
    [SerializeField] Color plantPoopRadiusColor = new Color(0.6f, 0.4f, 0.2f, 0.3f);
    [SerializeField] Color scentRadiusColor = new Color(1f, 1f, 0f, 0.3f);
    [SerializeField] Color fireflyPhotosynthesisColor = new Color(0f, 1f, 0.5f, 0.3f);
    [SerializeField] Color toolUseRadiusColor = new Color(0f, 0.5f, 1f, 0.3f);

    // --- State Management for Visualizations ---
    private class RadiusRequest
    {
        public GridPosition Center;
        public int Radius;
        public RadiusType Type;
    }

    // Stores visualizations that are cleared manually or when the source is destroyed
    private Dictionary<object, List<GameObject>> oneShotVisualizations = new Dictionary<object, List<GameObject>>();
    // Stores visualizations that are updated every frame (like the player's tool range)
    private Dictionary<object, RadiusRequest> continuousRequests = new Dictionary<object, RadiusRequest>();
    // Stores the last drawn state to check if a redraw is needed
    private Dictionary<object, (GridPosition center, int radius)> lastDrawnState = new Dictionary<object, (GridPosition, int)>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void OnDestroy()
    {
        ClearAllVisualizations();
        if (Instance == this) Instance = null;
    }

    void Update()
    {
        // Process continuous radius requests every frame
        ProcessContinuousRequests();
    }
    
    // --- Public Methods ---

    /// <summary>
    /// Shows a radius visualization that persists and updates until hidden.
    /// Ideal for things like player interaction range.
    /// </summary>
    public void ShowContinuousRadius(object source, GridPosition center, int radius, RadiusType type)
    {
        if (!showRadiusVisualizations || source == null) return;
        
        if (!continuousRequests.ContainsKey(source))
        {
            continuousRequests.Add(source, new RadiusRequest());
        }
        continuousRequests[source].Center = center;
        continuousRequests[source].Radius = radius;
        continuousRequests[source].Type = type;
    }

    /// <summary>
    /// Hides a continuous radius visualization.
    /// </summary>
    public void HideContinuousRadius(object source)
    {
        if (source == null) return;
        
        if (continuousRequests.Remove(source))
        {
            // The Update loop will handle removing the visuals
        }
    }

    /// <summary>
    /// Shows a static, one-shot radius visualization.
    /// Ideal for things like an animal's search that happens on a single tick.
    /// </summary>
    public void VisualizeRadius(object source, GridPosition center, int radius, Color color, float duration = 0f)
    {
        if (!showRadiusVisualizations || tilePrefab == null) return;

        ClearVisualization(source); // Clear any previous vis for this source

        var tiles = GridRadiusUtility.GetTilesInCircle(center, radius);
        var tileObjects = new List<GameObject>();

        foreach (var tile in tiles)
        {
            Vector3 worldPos = GridPositionManager.Instance.GridToWorld(tile);
            GameObject tileVis = Instantiate(tilePrefab, worldPos, Quaternion.identity, transform);

            SpriteRenderer sr = tileVis.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                Color finalColor = color;
                finalColor.a = tileVisualizationAlpha;
                sr.color = finalColor;
                sr.sortingOrder = -100; // Behind everything
            }
            tileObjects.Add(tileVis);

            if (duration > 0)
            {
                Destroy(tileVis, duration);
            }
        }

        // Only store if it's not a timed "fire and forget" visualization
        if (duration <= 0)
        {
            oneShotVisualizations[source] = tileObjects;
        }
    }

    /// <summary>
    /// Clears a specific one-shot visualization.
    /// </summary>
    public void ClearVisualization(object source)
    {
        if (oneShotVisualizations.TryGetValue(source, out var tiles))
        {
            foreach (var tile in tiles)
            {
                if (tile != null) Destroy(tile);
            }
            oneShotVisualizations.Remove(source);
        }
        if (lastDrawnState.ContainsKey(source))
        {
            lastDrawnState.Remove(source);
        }
    }
    
    // --- Specific Visualization Triggers (for convenience) ---

    public void VisualizeAnimalSearchRadius(AnimalController animal, GridPosition center, int radius)
    {
        // Use the one-shot visualizer that auto-clears
        VisualizeRadius(animal, center, radius, animalSearchRadiusColor, 0.5f);
    }
    
    // --- Internal Logic ---

    private void ProcessContinuousRequests()
    {
        // Check for any continuous visualizations that need to be removed
        List<object> sourcesToRemove = new List<object>();
        foreach (var drawnSource in lastDrawnState.Keys)
        {
            if (!continuousRequests.ContainsKey(drawnSource))
            {
                sourcesToRemove.Add(drawnSource);
            }
        }
        foreach(var source in sourcesToRemove)
        {
            ClearVisualization(source);
        }

        // Update active continuous visualizations
        foreach (var kvp in continuousRequests)
        {
            object source = kvp.Key;
            RadiusRequest request = kvp.Value;

            bool needsRedraw = false;
            if (lastDrawnState.TryGetValue(source, out var lastState))
            {
                if (lastState.center != request.Center || lastState.radius != request.Radius)
                {
                    needsRedraw = true;
                }
            }
            else
            {
                needsRedraw = true; // First time drawing for this source
            }

            if (needsRedraw)
            {
                Color color = GetColorForType(request.Type);
                VisualizeRadius(source, request.Center, request.Radius, color, 0); // Use the managed one-shot visualizer
                lastDrawnState[source] = (request.Center, request.Radius);
            }
        }
    }

    private Color GetColorForType(RadiusType type)
    {
        switch (type)
        {
            case RadiusType.AnimalSearch: return animalSearchRadiusColor;
            case RadiusType.PlantPoop: return plantPoopRadiusColor;
            case RadiusType.Scent: return scentRadiusColor;
            case RadiusType.FireflyPhotosynthesis: return fireflyPhotosynthesisColor;
            case RadiusType.ToolUse: return toolUseRadiusColor;
            default: return Color.white;
        }
    }

    private void ClearAllVisualizations()
    {
        foreach (var kvp in oneShotVisualizations)
        {
            foreach (var tile in kvp.Value)
            {
                if (tile != null) Destroy(tile);
            }
        }
        oneShotVisualizations.Clear();
        continuousRequests.Clear();
        lastDrawnState.Clear();
    }
}