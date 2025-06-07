using UnityEngine;
using System.Collections.Generic;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class FloraManager : MonoBehaviour
{
    public static FloraManager Instance { get; private set; }

    [Header("Scent Visualization")]
    [SerializeField] private bool showScentRadiiRuntime = false;
    [SerializeField] private Color scentRadiusColorRuntime = Color.yellow;

    [Header("Poop Absorption Visualization")]
    [SerializeField] private bool showPoopAbsorptionRadiiRuntime = false;
    [SerializeField] private Color poopAbsorptionRadiusColorRuntime = new Color(0.6f, 0.4f, 0.2f, 0.5f); // Brown-ish color

    [Header("System References")]
    [SerializeField] private GameObject circleVisualizerPrefab;
    [SerializeField] private Transform circleContainer;

    [Header("Debugging")]
    [SerializeField] private bool logGizmoCalls = false;

    public bool ShowScentRadiiRuntime => showScentRadiiRuntime;
    public Color ScentRadiusColorRuntime => scentRadiusColorRuntime;
    public bool ShowPoopAbsorptionRadiiRuntime => showPoopAbsorptionRadiiRuntime;
    public Color PoopAbsorptionRadiusColorRuntime => poopAbsorptionRadiusColorRuntime;

    private readonly Dictionary<ScentSource, RuntimeCircleDrawer> _activeCircleVisualizers = new Dictionary<ScentSource, RuntimeCircleDrawer>();
    private readonly Dictionary<PlantGrowth, RuntimeCircleDrawer> _activePoopAbsorptionCircleVisualizers = new Dictionary<PlantGrowth, RuntimeCircleDrawer>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (circleVisualizerPrefab == null) { Debug.LogError($"[{nameof(FloraManager)}] Circle Visualizer Prefab is not assigned!", this); }
        if (circleContainer == null) { Debug.LogError($"[{nameof(FloraManager)}] Circle Container transform is not assigned!", this); }
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;

        foreach (var kvp in _activeCircleVisualizers)
        {
            if (kvp.Value != null) Destroy(kvp.Value.gameObject);
        }
        _activeCircleVisualizers.Clear();

        foreach (var kvp in _activePoopAbsorptionCircleVisualizers)
        {
            if (kvp.Value != null) Destroy(kvp.Value.gameObject);
        }
        _activePoopAbsorptionCircleVisualizers.Clear();
    }

    private void Update()
    {
        if (!Application.isPlaying) return;
        UpdateRuntimeCircleVisualizers();
        UpdatePoopAbsorptionCircleVisualizers();
    }

    private void UpdatePoopAbsorptionCircleVisualizers()
    {
        if (!Application.isPlaying) return;

        bool showCircles = showPoopAbsorptionRadiiRuntime && circleVisualizerPrefab != null && circleContainer != null;

        if (!showCircles)
        {
            foreach (var kvp in _activePoopAbsorptionCircleVisualizers)
            {
                if (kvp.Value != null) Destroy(kvp.Value.gameObject);
            }
            _activePoopAbsorptionCircleVisualizers.Clear();
            return;
        }

        // --- MODIFICATION START ---
        // Use the cached static list of plants instead of FindObjectsByType
        var currentPlantsSet = new HashSet<PlantGrowth>(PlantGrowth.AllActivePlants);
        // --- MODIFICATION END ---
        
        var plantsToRemove = new List<PlantGrowth>();

        // Prune visualizers for destroyed or inactive plants
        foreach (var kvp in _activePoopAbsorptionCircleVisualizers)
        {
            PlantGrowth plant = kvp.Key;
            RuntimeCircleDrawer drawer = kvp.Value;

            if (plant == null || drawer == null || !plant.gameObject.activeInHierarchy || !currentPlantsSet.Contains(plant))
            {
                plantsToRemove.Add(plant);
                if (drawer != null) Destroy(drawer.gameObject);
                continue;
            }

            float poopRadius = GetPlantPoopDetectionRadius(plant);
            bool shouldShowThis = showCircles && poopRadius > 0.01f;

            if (shouldShowThis)
            {
                drawer.transform.position = plant.transform.position;
                drawer.UpdateCircle(poopRadius, poopAbsorptionRadiusColorRuntime);
            }
            else
            {
                drawer.HideCircle();
                plantsToRemove.Add(plant); // If radius is too small or zero, remove the visualizer
            }
        }

        foreach (var plant in plantsToRemove)
        {
            if (_activePoopAbsorptionCircleVisualizers.TryGetValue(plant, out var drawer))
            {
                if (drawer != null) Destroy(drawer.gameObject);
                _activePoopAbsorptionCircleVisualizers.Remove(plant);
            }
        }

        // --- MODIFICATION START ---
        // Add visualizers for new plants by iterating the static list
        foreach (PlantGrowth plant in PlantGrowth.AllActivePlants)
        // --- MODIFICATION END ---
        {
            if (plant == null || _activePoopAbsorptionCircleVisualizers.ContainsKey(plant)) continue;

            float poopRadius = GetPlantPoopDetectionRadius(plant);
            if (poopRadius <= 0.01f) continue;

            GameObject circleGO = Instantiate(circleVisualizerPrefab, plant.transform.position, Quaternion.identity, circleContainer);
            var newDrawer = circleGO.GetComponent<RuntimeCircleDrawer>();

            if (newDrawer != null)
            {
                newDrawer.UpdateCircle(poopRadius, poopAbsorptionRadiusColorRuntime);
                _activePoopAbsorptionCircleVisualizers.Add(plant, newDrawer);
            }
            else
            {
                Debug.LogError($"Circle Visualizer Prefab '{circleVisualizerPrefab.name}' missing RuntimeCircleDrawer script!", circleVisualizerPrefab);
                Destroy(circleGO);
            }
        }
    }

    private float GetPlantPoopDetectionRadius(PlantGrowth plant)
    {
        if (plant == null) return 0f;
        return plant.GetPoopDetectionRadius();
    }

    private void UpdateRuntimeCircleVisualizers()
    {
        if (!Application.isPlaying) return;

        bool showCircles = showScentRadiiRuntime && circleVisualizerPrefab != null && circleContainer != null;
        var sourcesToRemove = new List<ScentSource>();

        // Prune visualizers for destroyed or inactive sources
        foreach (var kvp in _activeCircleVisualizers)
        {
            ScentSource source = kvp.Key;
            RuntimeCircleDrawer line = kvp.Value;

            if (source == null || line == null || !source.gameObject.activeInHierarchy)
            {
                sourcesToRemove.Add(source);
                if (line != null) Destroy(line.gameObject);
                continue;
            }

            bool shouldShowThis = showCircles && source.enabled && source.definition != null && source.EffectiveRadius > 0.01f;

            if (shouldShowThis)
            {
                line.transform.position = source.transform.position;
                line.transform.rotation = source.transform.rotation;
                line.UpdateCircle(source.EffectiveRadius, scentRadiusColorRuntime);
            }
            else
            {
                line.HideCircle();
            }
        }

        foreach (var source in sourcesToRemove)
        {
            if (_activeCircleVisualizers.TryGetValue(source, out var drawer) && drawer != null)
                Destroy(drawer.gameObject);
            _activeCircleVisualizers.Remove(source);
        }

        if (showCircles)
        {
            // Add visualizers for new ScentSource objects
            ScentSource[] currentSources = FindObjectsByType<ScentSource>(FindObjectsSortMode.None);
            foreach (ScentSource source in currentSources)
            {
                if (source == null || _activeCircleVisualizers.ContainsKey(source) || !source.enabled ||
                    source.definition == null || source.EffectiveRadius <= 0.01f) continue;

                GameObject circleGO = Instantiate(circleVisualizerPrefab, source.transform.position, source.transform.rotation, circleContainer);
                var newDrawer = circleGO.GetComponent<RuntimeCircleDrawer>();

                if (newDrawer != null)
                {
                    newDrawer.UpdateCircle(source.EffectiveRadius, scentRadiusColorRuntime);
                    _activeCircleVisualizers.Add(source, newDrawer);
                }
                else
                {
                    Debug.LogError($"Circle Visualizer Prefab '{circleVisualizerPrefab.name}' is missing RuntimeCircleDrawer script!", circleVisualizerPrefab);
                    Destroy(circleGO);
                }
            }
        }
        else if (!showCircles && _activeCircleVisualizers.Count > 0)
        {
            // If disabled, destroy all existing scent visualizers
            foreach (var kvp in _activeCircleVisualizers)
            {
                if (kvp.Value != null) Destroy(kvp.Value.gameObject);
            }
            _activeCircleVisualizers.Clear();
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (showScentRadiiRuntime)
        {
            Gizmos.color = scentRadiusColorRuntime;
            ScentSource[] scentSources = FindObjectsByType<ScentSource>(FindObjectsSortMode.None);
            foreach (ScentSource source in scentSources)
            {
                if (source == null || !source.enabled || source.definition == null) continue;
                float radius = source.EffectiveRadius;
                if (radius > 0.01f)
                {
                    Gizmos.DrawWireSphere(source.transform.position, radius);
                }
            }
        }

        if (showPoopAbsorptionRadiiRuntime)
        {
            Gizmos.color = poopAbsorptionRadiusColorRuntime;
            // This still uses FindObjectsByType as it's editor-only and doesn't impact runtime performance.
            PlantGrowth[] plants = FindObjectsByType<PlantGrowth>(FindObjectsSortMode.None); 
            int drawnCount = 0;

            foreach (PlantGrowth plant in plants)
            {
                if (plant == null) continue;
                float radius = GetPlantPoopDetectionRadius(plant);
                if (radius > 0.01f)
                {
                    Gizmos.DrawWireSphere(plant.transform.position, radius);
                    drawnCount++;
                }
            }

            if (logGizmoCalls && drawnCount > 0)
            {
                Debug.Log($"[FloraManager] Drew {drawnCount} poop absorption radius gizmos");
            }
        }
    }
#endif
}