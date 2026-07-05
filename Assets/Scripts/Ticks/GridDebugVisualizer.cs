// FILE: Assets\Scripts\Ticks\GridDebugVisualizer.cs
using System.Collections.Generic;
using Abracodabra.Genes;
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
    [SerializeField] bool showRadiusVisualizations = true;
    [SerializeField] float tileVisualizationAlpha = 0.3f;
    [SerializeField] GameObject tilePrefab;

    [Header("Radius Colors (Centralized)")]
    [SerializeField] public Color animalSearchRadiusColor = new Color(1f, 0.5f, 0f, 0.3f);
    [SerializeField] public Color plantPoopRadiusColor = new Color(0.6f, 0.4f, 0.2f, 0.3f);
    [SerializeField] public Color scentRadiusColor = new Color(1f, 1f, 0f, 0.3f);
    [SerializeField] public Color fireflyPhotosynthesisColor = new Color(0f, 1f, 0.5f, 0.3f);
    [SerializeField] public Color toolUseRadiusColor = new Color(0f, 0.5f, 1f, 0.3f);

    [Header("Individual Type Controls")]
    [SerializeField] bool enableAnimalSearchRadius = true;
    [SerializeField] bool enablePlantPoopRadius = true;
    [SerializeField] bool enableScentRadius = true;
    [SerializeField] bool enableFireflyPhotosynthesis = true;
    [SerializeField] bool enableToolUseRadius = true;

    [Header("Gene Effect Overlays")]
    [Tooltip("Master toggle for all gene-driven AOE tile overlays (Aura, Cloud, Reactive Burst, etc.)")]
    [SerializeField] bool enableGeneEffectOverlays = true;

    [Tooltip("Sorting order for gene effect overlay tiles. Higher = renders on top of debug radii.")]
    [SerializeField] int geneEffectSortingOrder = -90;

    // --- Existing typed requests ---
    class RadiusRequest
    {
        public GridPosition Center;
        public int Radius;
        public RadiusType Type;
    }

    readonly Dictionary<object, List<GameObject>> oneShotVisualizations = new Dictionary<object, List<GameObject>>();
    readonly Dictionary<object, RadiusRequest> continuousRequests = new Dictionary<object, RadiusRequest>();
    readonly Dictionary<object, (GridPosition center, int radius)> lastDrawnState = new Dictionary<object, (GridPosition, int)>();

    // --- Custom-colored continuous requests (for gene effects) ---
    class ColoredRadiusRequest
    {
        public GridPosition Center;
        public int Radius;
        public Color Color;
    }

    readonly Dictionary<object, ColoredRadiusRequest> coloredContinuousRequests = new Dictionary<object, ColoredRadiusRequest>();
    readonly Dictionary<object, (GridPosition center, int radius, Color color)> coloredLastDrawnState = new Dictionary<object, (GridPosition, int, Color)>();

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
        ProcessColoredContinuousRequests();
    }

    // =========================================================================
    // EXISTING API (unchanged)
    // =========================================================================

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

        // Also check colored requests
        if (coloredContinuousRequests.Remove(source))
        {
            ClearVisualization(source);
            coloredLastDrawnState.Remove(source);
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

    // =========================================================================
    // NEW: Custom-colored continuous radius (for gene AOE effects)
    // =========================================================================

    /// <summary>
    /// Shows a persistent tile overlay with a custom color. Use this for gene effects
    /// (Aura, Cloud, Reactive Burst, Explosive blast, etc.) where each effect has
    /// its own color derived from the payload gene.
    ///
    /// Multiple overlapping calls with different sources will layer semi-transparent
    /// tiles on top of each other — overlaps naturally blend.
    ///
    /// Call HideContinuousRadius(source) to remove.
    /// </summary>
    public void ShowContinuousColoredRadius(object source, GridPosition center, int radius, Color color)
    {
        if (!showRadiusVisualizations || !enableGeneEffectOverlays || source == null) return;

        if (!coloredContinuousRequests.ContainsKey(source))
        {
            coloredContinuousRequests.Add(source, new ColoredRadiusRequest());
        }

        coloredContinuousRequests[source].Center = center;
        coloredContinuousRequests[source].Radius = radius;
        coloredContinuousRequests[source].Color = color;
    }

    /// <summary>
    /// One-shot colored radius that auto-destroys after duration seconds.
    /// Good for explosions, reactive bursts, and other momentary AOE flashes.
    /// </summary>
    public void ShowColoredRadiusBurst(object source, GridPosition center, int radius, Color color, float duration)
    {
        if (!showRadiusVisualizations || !enableGeneEffectOverlays || tilePrefab == null) return;

        var tiles = GridRadiusUtility.GetTilesInCircle(center, radius);

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
                sr.sortingOrder = geneEffectSortingOrder;
            }

            Destroy(tileVis, duration);
        }
    }

    // =========================================================================
    // Convenience methods for existing systems (unchanged)
    // =========================================================================

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

    // =========================================================================
    // Toggle setters (unchanged + new)
    // =========================================================================

    public void SetAnimalSearchRadiusEnabled(bool enabled) { enableAnimalSearchRadius = enabled; }
    public void SetPlantPoopRadiusEnabled(bool enabled) { enablePlantPoopRadius = enabled; }
    public void SetScentRadiusEnabled(bool enabled) { enableScentRadius = enabled; }
    public void SetFireflyPhotosynthesisEnabled(bool enabled) { enableFireflyPhotosynthesis = enabled; }
    public void SetToolUseRadiusEnabled(bool enabled) { enableToolUseRadius = enabled; }
    public void SetGeneEffectOverlaysEnabled(bool enabled)
    {
        enableGeneEffectOverlays = enabled;
        if (!enabled)
        {
            // Clear all colored overlays
            foreach (var source in new List<object>(coloredContinuousRequests.Keys))
            {
                ClearVisualization(source);
            }
            coloredContinuousRequests.Clear();
            coloredLastDrawnState.Clear();
        }
    }

    public void SetRadiusVisualizationsEnabled(bool enabled)
    {
        showRadiusVisualizations = enabled;
        if (!enabled)
        {
            ClearAllVisualizations();
        }
    }

    // =========================================================================
    // Internals
    // =========================================================================

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

    bool IsTypeEnabled(RadiusType type)
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

    void ProcessContinuousRequests()
    {
        if (!showRadiusVisualizations || tilePrefab == null) return;

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

        foreach (var kvp in continuousRequests)
        {
            object source = kvp.Key;
            RadiusRequest request = kvp.Value;

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

    void ProcessColoredContinuousRequests()
    {
        if (!showRadiusVisualizations || !enableGeneEffectOverlays || tilePrefab == null) return;

        // Remove drawn entries whose source no longer has a request
        List<object> sourcesToRemove = new List<object>();
        foreach (var drawnSource in coloredLastDrawnState.Keys)
        {
            if (!coloredContinuousRequests.ContainsKey(drawnSource))
            {
                sourcesToRemove.Add(drawnSource);
            }
        }
        foreach (var source in sourcesToRemove)
        {
            ClearVisualization(source);
            coloredLastDrawnState.Remove(source);
        }

        // Draw or update colored requests
        foreach (var kvp in coloredContinuousRequests)
        {
            object source = kvp.Key;
            ColoredRadiusRequest request = kvp.Value;

            bool needsRedraw = false;
            if (coloredLastDrawnState.TryGetValue(source, out var lastState))
            {
                if (lastState.center != request.Center ||
                    lastState.radius != request.Radius ||
                    lastState.color != request.Color)
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
                DrawColoredRadius(source, request.Center, request.Radius, request.Color);
                coloredLastDrawnState[source] = (request.Center, request.Radius, request.Color);
            }
        }
    }

    void DrawColoredRadius(object source, GridPosition center, int radius, Color color)
    {
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
                finalColor.a = Mathf.Min(finalColor.a, tileVisualizationAlpha);
                sr.color = finalColor;
                sr.sortingOrder = geneEffectSortingOrder;
            }
            tileObjects.Add(tileVis);
        }

        oneShotVisualizations[source] = tileObjects;
    }

    void ClearAllVisualizations()
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
        coloredContinuousRequests.Clear();
        coloredLastDrawnState.Clear();
    }

    // =========================================================================
    // Read-only state (unchanged)
    // =========================================================================

    public bool IsRadiusVisualizationEnabled => showRadiusVisualizations;
    public bool IsAnimalSearchRadiusEnabled => enableAnimalSearchRadius;
    public bool IsPlantPoopRadiusEnabled => enablePlantPoopRadius;
    public bool IsScentRadiusEnabled => enableScentRadius;
    public bool IsFireflyPhotosynthesisEnabled => enableFireflyPhotosynthesis;
    public bool IsToolUseRadiusEnabled => enableToolUseRadius;
    public bool IsGeneEffectOverlaysEnabled => enableGeneEffectOverlays;
}