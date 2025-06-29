using System.Collections.Generic;
using UnityEngine;
using WegoSystem;

public class FloraManager : MonoBehaviour
{
    public static FloraManager Instance { get; private set; }

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogging = false;

    // Tracking sets for what we're currently monitoring (not controlling!)
    private readonly HashSet<ScentSource> trackedScentSources = new HashSet<ScentSource>();
    private readonly HashSet<PlantGrowth> trackedPlants = new HashSet<PlantGrowth>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (enableDebugLogging)
        {
            Debug.Log($"[FloraManager] Initialized - all radius visualization controlled by GridDebugVisualizer only");
        }
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;

        // Clean up any active visualizations through GridDebugVisualizer
        CleanupAllVisualizations();
    }

    void Update()
    {
        if (!Application.isPlaying || GridDebugVisualizer.Instance == null) return;

        // Automatically track scent sources and plants based on GridDebugVisualizer settings
        UpdateScentRadiusTracking();
        UpdatePoopAbsorptionTracking();
    }

    public float GetPlantPoopDetectionRadius(PlantGrowth plant)
    {
        if (plant == null || plant.NodeExecutor == null) return 0f;
        return plant.GetPoopDetectionRadius();
    }

    void UpdateScentRadiusTracking()
    {
        // Check if scent radius visualization is enabled in GridDebugVisualizer
        bool shouldShow = GridDebugVisualizer.Instance.IsScentRadiusEnabled && 
                         GridDebugVisualizer.Instance.IsRadiusVisualizationEnabled;

        if (!shouldShow)
        {
            // Hide all scent visualizations if disabled in GridDebugVisualizer
            foreach (var source in trackedScentSources)
            {
                if (source != null)
                {
                    GridDebugVisualizer.Instance.HideContinuousRadius(source);
                }
            }
            trackedScentSources.Clear();
            return;
        }

        // Get all current scent sources
        ScentSource[] currentSources = FindObjectsByType<ScentSource>(FindObjectsSortMode.None);
        var currentSourcesSet = new HashSet<ScentSource>(currentSources);

        // Remove visualizations for sources that no longer exist or are disabled
        var sourcesToRemove = new HashSet<ScentSource>();
        foreach (var source in trackedScentSources)
        {
            if (source == null || !source.enabled || !currentSourcesSet.Contains(source))
            {
                sourcesToRemove.Add(source);
                if (source != null)
                {
                    GridDebugVisualizer.Instance.HideContinuousRadius(source);
                }
            }
        }

        foreach (var source in sourcesToRemove)
        {
            trackedScentSources.Remove(source);
        }

        // Add visualizations for active scent sources
        foreach (ScentSource source in currentSources)
        {
            if (source == null || !source.enabled || source.Definition == null) continue;

            float radius = source.EffectiveRadius;
            if (radius <= 0.01f) 
            {
                if (trackedScentSources.Contains(source))
                {
                    GridDebugVisualizer.Instance.HideContinuousRadius(source);
                    trackedScentSources.Remove(source);
                }
                continue;
            }

            // Get the grid position for the scent source
            GridEntity gridEntity = source.GetComponent<GridEntity>();
            if (gridEntity != null)
            {
                int radiusTiles = Mathf.RoundToInt(radius);
                GridDebugVisualizer.Instance.VisualizeScentRadius(source, gridEntity.Position, radiusTiles);
                trackedScentSources.Add(source);
            }
            else if (GridPositionManager.Instance != null)
            {
                // Fallback for sources without GridEntity
                GridPosition gridPos = GridPositionManager.Instance.WorldToGrid(source.transform.position);
                int radiusTiles = Mathf.RoundToInt(radius);
                GridDebugVisualizer.Instance.VisualizeScentRadius(source, gridPos, radiusTiles);
                trackedScentSources.Add(source);
            }
        }
    }

    void UpdatePoopAbsorptionTracking()
    {
        // Check if poop absorption radius visualization is enabled in GridDebugVisualizer
        bool shouldShow = GridDebugVisualizer.Instance.IsPlantPoopRadiusEnabled && 
                         GridDebugVisualizer.Instance.IsRadiusVisualizationEnabled;

        if (!shouldShow)
        {
            // Hide all poop absorption visualizations if disabled in GridDebugVisualizer
            foreach (var plant in trackedPlants)
            {
                if (plant != null)
                {
                    GridDebugVisualizer.Instance.HideContinuousRadius(plant);
                }
            }
            trackedPlants.Clear();
            return;
        }

        // Get all current plants
        var currentPlantsSet = new HashSet<PlantGrowth>(PlantGrowth.AllActivePlants);

        // Remove visualizations for plants that no longer exist or are inactive
        var plantsToRemove = new HashSet<PlantGrowth>();
        foreach (var plant in trackedPlants)
        {
            if (plant == null || !plant.gameObject.activeInHierarchy || !currentPlantsSet.Contains(plant))
            {
                plantsToRemove.Add(plant);
                if (plant != null)
                {
                    GridDebugVisualizer.Instance.HideContinuousRadius(plant);
                }
            }
        }

        foreach (var plant in plantsToRemove)
        {
            trackedPlants.Remove(plant);
        }

        // Add visualizations for active plants
        foreach (PlantGrowth plant in PlantGrowth.AllActivePlants)
        {
            if (plant == null || !plant.gameObject.activeInHierarchy) continue;

            float poopRadius = GetPlantPoopDetectionRadius(plant);
            if (poopRadius <= 0.01f)
            {
                if (trackedPlants.Contains(plant))
                {
                    GridDebugVisualizer.Instance.HideContinuousRadius(plant);
                    trackedPlants.Remove(plant);
                }
                continue;
            }

            // Get the grid position for the plant
            GridEntity gridEntity = plant.GetComponent<GridEntity>();
            if (gridEntity != null)
            {
                int radiusTiles = Mathf.RoundToInt(poopRadius);
                GridDebugVisualizer.Instance.VisualizePlantPoopRadius(plant, gridEntity.Position, radiusTiles);
                trackedPlants.Add(plant);
            }
            else if (GridPositionManager.Instance != null)
            {
                // Fallback for plants without GridEntity
                GridPosition gridPos = GridPositionManager.Instance.WorldToGrid(plant.transform.position);
                int radiusTiles = Mathf.RoundToInt(poopRadius);
                GridDebugVisualizer.Instance.VisualizePlantPoopRadius(plant, gridPos, radiusTiles);
                trackedPlants.Add(plant);
            }
        }
    }

    void CleanupAllVisualizations()
    {
        if (GridDebugVisualizer.Instance == null) return;

        foreach (var source in trackedScentSources)
        {
            if (source != null)
            {
                GridDebugVisualizer.Instance.HideContinuousRadius(source);
            }
        }

        foreach (var plant in trackedPlants)
        {
            if (plant != null)
            {
                GridDebugVisualizer.Instance.HideContinuousRadius(plant);
            }
        }

        trackedScentSources.Clear();
        trackedPlants.Clear();
    }

    // Read-only status methods for external components that need to know what's being tracked
    public bool HasScentSourcesTracked => trackedScentSources.Count > 0;
    public bool HasPlantsTracked => trackedPlants.Count > 0;
    public int TrackedScentSourceCount => trackedScentSources.Count;
    public int TrackedPlantCount => trackedPlants.Count;

    // NO ENABLE/DISABLE METHODS - All control is in GridDebugVisualizer!
}