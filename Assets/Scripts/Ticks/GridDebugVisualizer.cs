using System.Collections.Generic;
using UnityEngine;
using WegoSystem;

public class GridDebugVisualizer : MonoBehaviour
{
    public static GridDebugVisualizer Instance { get; private set; }

    public enum RadiusType
    {
        AnimalSearch,
        PlantPoop,
        Scent,
        FireflyPhotosynthesis,
        ToolUse
    }

    [Header("Master Control")]
    [SerializeField] private bool showRadiusVisualizations = true;
    [SerializeField] private float tileVisualizationAlpha = 0.3f;
    [SerializeField] private GameObject tilePrefab;

    [Header("Radius Colors (Centralized)")]
    [SerializeField] public Color animalSearchRadiusColor = new Color(1f, 0.5f, 0f, 0.3f);
    [SerializeField] public Color plantPoopRadiusColor = new Color(0.6f, 0.4f, 0.2f, 0.3f);
    [SerializeField] public Color scentRadiusColor = new Color(1f, 1f, 0f, 0.3f);
    [SerializeField] public Color fireflyPhotosynthesisColor = new Color(0f, 1f, 0.5f, 0.3f);
    [SerializeField] public Color toolUseRadiusColor = new Color(0f, 0.5f, 1f, 0.3f);

    [Header("Individual Type Controls")]
    [SerializeField] private bool enableAnimalSearchRadius = true;
    [SerializeField] private bool enablePlantPoopRadius = true;
    [SerializeField] private bool enableScentRadius = true;
    [SerializeField] private bool enableFireflyPhotosynthesis = true;
    [SerializeField] private bool enableToolUseRadius = true;

    private class RadiusRequest
    {
        public GridPosition Center;
        public int Radius;
        public RadiusType Type;
    }

    private readonly Dictionary<object, List<GameObject>> oneShotVisualizations = new Dictionary<object, List<GameObject>>();
    private readonly Dictionary<object, RadiusRequest> continuousRequests = new Dictionary<object, RadiusRequest>();
    private readonly Dictionary<object, (GridPosition center, int radius)> lastDrawnState = new Dictionary<object, (GridPosition, int)>();

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
        ProcessContinuousRequests();
    }

    public void ShowContinuousRadius(object source, GridPosition center, int radius, RadiusType type)
    {
        if (!showRadiusVisualizations || source == null || !IsTypeEnabled(type)) return;

        if (!continuousRequests.ContainsKey(source))
        {
            continuousRequests.Add(source, new RadiusRequest());
        }
        continuousRequests[source].Center = center;
        continuousRequests[source].Radius = radius;
        continuousRequests[source].Type = type;
    }

    public void HideContinuousRadius(object source)
    {
        if (source == null) return;

        if (continuousRequests.Remove(source))
        {
            ClearVisualization(source);
        }
    }

    public void VisualizeRadius(object source, GridPosition center, int radius, Color color, float duration = 0f)
    {
        if (!showRadiusVisualizations || tilePrefab == null) return;

        ClearVisualization(source);

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
                sr.sortingOrder = -100;
            }
            tileObjects.Add(tileVis);

            if (duration > 0)
            {
                Destroy(tileVis, duration);
            }
        }

        if (duration <= 0)
        {
            oneShotVisualizations[source] = tileObjects;
        }
    }

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

    // Specific visualization methods - all use centralized settings
    public void VisualizeAnimalSearchRadius(AnimalController animal, GridPosition center, int radius)
    {
        ShowContinuousRadius(animal, center, radius, RadiusType.AnimalSearch);
    }

    public void VisualizePlantPoopRadius(PlantGrowth plant, GridPosition center, int radius)
    {
        ShowContinuousRadius(plant, center, radius, RadiusType.PlantPoop);
    }

    public void VisualizeScentRadius(ScentSource scentSource, GridPosition center, int radius)
    {
        ShowContinuousRadius(scentSource, center, radius, RadiusType.Scent);
    }

    public void VisualizeFireflyPhotosynthesisRadius(FireflyController firefly, GridPosition center, int radius)
    {
        ShowContinuousRadius(firefly, center, radius, RadiusType.FireflyPhotosynthesis);
    }

    public void VisualizeToolUseRadius(object tool, GridPosition center, int radius)
    {
        ShowContinuousRadius(tool, center, radius, RadiusType.ToolUse);
    }

    // Public methods for external control of individual types
    public void SetAnimalSearchRadiusEnabled(bool enabled) { enableAnimalSearchRadius = enabled; }
    public void SetPlantPoopRadiusEnabled(bool enabled) { enablePlantPoopRadius = enabled; }
    public void SetScentRadiusEnabled(bool enabled) { enableScentRadius = enabled; }
    public void SetFireflyPhotosynthesisEnabled(bool enabled) { enableFireflyPhotosynthesis = enabled; }
    public void SetToolUseRadiusEnabled(bool enabled) { enableToolUseRadius = enabled; }

    // Master control
    public void SetRadiusVisualizationsEnabled(bool enabled) 
    { 
        showRadiusVisualizations = enabled;
        if (!enabled)
        {
            ClearAllVisualizations();
        }
    }

    // Color accessors for external components (like gizmos)
    public Color GetColorForType(RadiusType type)
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

    private bool IsTypeEnabled(RadiusType type)
    {
        switch (type)
        {
            case RadiusType.AnimalSearch: return enableAnimalSearchRadius;
            case RadiusType.PlantPoop: return enablePlantPoopRadius;
            case RadiusType.Scent: return enableScentRadius;
            case RadiusType.FireflyPhotosynthesis: return enableFireflyPhotosynthesis;
            case RadiusType.ToolUse: return enableToolUseRadius;
            default: return true;
        }
    }

    private void ProcessContinuousRequests()
    {
        if (!showRadiusVisualizations || tilePrefab == null) return;

        // Remove visualizations that are no longer requested
        List<object> sourcesToRemove = new List<object>();
        foreach (var drawnSource in lastDrawnState.Keys)
        {
            if (!continuousRequests.ContainsKey(drawnSource))
            {
                sourcesToRemove.Add(drawnSource);
            }
        }
        foreach (var source in sourcesToRemove)
        {
            ClearVisualization(source);
        }

        // Update active visualizations
        foreach (var kvp in continuousRequests)
        {
            object source = kvp.Key;
            RadiusRequest request = kvp.Value;

            // Check if this type is enabled
            if (!IsTypeEnabled(request.Type)) 
            {
                if (lastDrawnState.ContainsKey(source))
                {
                    ClearVisualization(source);
                }
                continue;
            }

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
                needsRedraw = true;
            }

            if (needsRedraw)
            {
                Color color = GetColorForType(request.Type);
                VisualizeRadius(source, request.Center, request.Radius, color, 0);
                lastDrawnState[source] = (request.Center, request.Radius);
            }
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

    // Public properties for read access
    public bool IsRadiusVisualizationEnabled => showRadiusVisualizations;
    public bool IsAnimalSearchRadiusEnabled => enableAnimalSearchRadius;
    public bool IsPlantPoopRadiusEnabled => enablePlantPoopRadius;
    public bool IsScentRadiusEnabled => enableScentRadius;
    public bool IsFireflyPhotosynthesisEnabled => enableFireflyPhotosynthesis;
    public bool IsToolUseRadiusEnabled => enableToolUseRadius;
}