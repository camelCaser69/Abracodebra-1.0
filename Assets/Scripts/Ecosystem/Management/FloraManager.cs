using UnityEngine;
using System.Collections.Generic;
using WegoSystem;

#region Using Statements
// This region is for AI formatting. It will be removed in the final output.
#endregion

public class FloraManager : MonoBehaviour
{
    public static FloraManager Instance { get; set; }

    [SerializeField] private bool enableDebugLogging = false;

    // --- NEW FIELD ---
    [Header("Plant Balance Settings")]
    [Tooltip("The base energy generated per leaf, per tick, at 100% sunlight, before any gene modifications.")]
    [SerializeField] public float basePhotosynthesisRatePerLeaf = 0.1f;
    
    private readonly HashSet<ScentSource> trackedScentSources = new HashSet<ScentSource>();
    private readonly HashSet<PlantGrowth> trackedPlants = new HashSet<PlantGrowth>();

    private void Awake()
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

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;

        CleanupAllVisualizations();
    }

    private void Update()
    {
        if (!Application.isPlaying || GridDebugVisualizer.Instance == null) return;

        UpdateScentRadiusTracking();
        UpdatePoopAbsorptionTracking();
    }

    public float GetPlantPoopDetectionRadius(PlantGrowth plant)
    {
        if (plant == null || plant.NodeExecutor == null) return 0f;
        return plant.GetPoopDetectionRadius();
    }

    private void UpdateScentRadiusTracking()
    {
        bool shouldShow = GridDebugVisualizer.Instance.IsScentRadiusEnabled &&
                          GridDebugVisualizer.Instance.IsRadiusVisualizationEnabled;

        if (!shouldShow)
        {
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

        ScentSource[] currentSources = FindObjectsByType<ScentSource>(FindObjectsSortMode.None);
        var currentSourcesSet = new HashSet<ScentSource>(currentSources);

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

            GridEntity gridEntity = source.GetComponent<GridEntity>();
            if (gridEntity != null)
            {
                int radiusTiles = Mathf.RoundToInt(radius);
                GridDebugVisualizer.Instance.VisualizeScentRadius(source, gridEntity.Position, radiusTiles);
                trackedScentSources.Add(source);
            }
            else if (GridPositionManager.Instance != null)
            {
                GridPosition gridPos = GridPositionManager.Instance.WorldToGrid(source.transform.position);
                int radiusTiles = Mathf.RoundToInt(radius);
                GridDebugVisualizer.Instance.VisualizeScentRadius(source, gridPos, radiusTiles);
                trackedScentSources.Add(source);
            }
        }
    }

    private void UpdatePoopAbsorptionTracking()
    {
        bool shouldShow = GridDebugVisualizer.Instance.IsPlantPoopRadiusEnabled &&
                          GridDebugVisualizer.Instance.IsRadiusVisualizationEnabled;

        if (!shouldShow)
        {
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

        var currentPlantsSet = new HashSet<PlantGrowth>(PlantGrowth.AllActivePlants);

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

            GridEntity gridEntity = plant.GetComponent<GridEntity>();
            if (gridEntity != null)
            {
                int radiusTiles = Mathf.RoundToInt(poopRadius);
                GridDebugVisualizer.Instance.VisualizePlantPoopRadius(plant, gridEntity.Position, radiusTiles);
                trackedPlants.Add(plant);
            }
            else if (GridPositionManager.Instance != null)
            {
                GridPosition gridPos = GridPositionManager.Instance.WorldToGrid(plant.transform.position);
                int radiusTiles = Mathf.RoundToInt(poopRadius);
                GridDebugVisualizer.Instance.VisualizePlantPoopRadius(plant, gridPos, radiusTiles);
                trackedPlants.Add(plant);
            }
        }
    }

    private void CleanupAllVisualizations()
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

    public bool HasScentSourcesTracked => trackedScentSources.Count > 0;
    public bool HasPlantsTracked => trackedPlants.Count > 0;
    public int TrackedScentSourceCount => trackedScentSources.Count;
    public int TrackedPlantCount => trackedPlants.Count;
}