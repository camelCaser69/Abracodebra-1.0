using UnityEngine;
using System.Collections.Generic; // Added for Dictionary
#if UNITY_EDITOR
using UnityEditor;
#endif

public class FloraManager : MonoBehaviour
{
    public static FloraManager Instance { get; private set; }

    [Header("Debugging - Scents")]
    [Tooltip("Show scent radii circle renderers in Game View during runtime.")]
    [SerializeField] private bool showScentRadiiRuntime = false;
    [SerializeField] private Color scentRadiusColorRuntime = Color.yellow;
    [SerializeField] private bool logGizmoCalls = false;
    [Space]
    [Tooltip("Prefab used to draw scent circles at runtime.")]
    [SerializeField] private GameObject circleVisualizerPrefab; // <<< ADDED
    [Tooltip("Parent transform for instantiated circle visualizers.")]
    [SerializeField] private Transform circleContainer; // <<< ADDED

    [Header("Debugging - Poop Absorption")] // <<< NEW HEADER
    [Tooltip("Show poop absorption radii circle renderers in Game View during runtime.")]
    [SerializeField] private bool showPoopAbsorptionRadiiRuntime = false;
    [Tooltip("Color of the poop absorption radius visualization.")]
    [SerializeField] private Color poopAbsorptionRadiusColorRuntime = new Color(0.6f, 0.4f, 0.2f, 0.5f); // Brown-ish color

    // --- Public Accessors ---
    public bool ShowScentRadiiRuntime => showScentRadiiRuntime;
    public Color ScentRadiusColorRuntime => scentRadiusColorRuntime;
    // <<< NEW ACCESSORS for poop absorption radius visualization >>>
    public bool ShowPoopAbsorptionRadiiRuntime => showPoopAbsorptionRadiiRuntime;
    public Color PoopAbsorptionRadiusColorRuntime => poopAbsorptionRadiusColorRuntime;

    // Dictionary to track circle visualizers per ScentSource
    private Dictionary<ScentSource, RuntimeCircleDrawer> activeCircleVisualizers = new Dictionary<ScentSource, RuntimeCircleDrawer>();
    // NEW: Dictionary to track poop absorption circle visualizers per PlantGrowth
    private Dictionary<PlantGrowth, RuntimeCircleDrawer> activePoopAbsorptionCircleVisualizers = new Dictionary<PlantGrowth, RuntimeCircleDrawer>();


    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // Validate Debug Dependencies
        if (circleVisualizerPrefab == null) { Debug.LogError($"[{nameof(FloraManager)}] Circle Visualizer Prefab is not assigned!", this); }
        if (circleContainer == null) { Debug.LogError($"[{nameof(FloraManager)}] Circle Container transform is not assigned!", this); }
    }

     void Update() // Added Update loop
     {
         if (!Application.isPlaying) return;
         UpdateRuntimeCircleVisualizers();
         // NEW: Update poop absorption radius visualizers
         UpdatePoopAbsorptionCircleVisualizers();
     }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        // Clean up any remaining visualizers when manager is destroyed
        foreach (var kvp in activeCircleVisualizers)
        {
            if (kvp.Value != null) Destroy(kvp.Value.gameObject);
        }
        activeCircleVisualizers.Clear();
        
        // NEW: Clean up poop absorption visualizers
        foreach (var kvp in activePoopAbsorptionCircleVisualizers)
        {
            if (kvp.Value != null) Destroy(kvp.Value.gameObject);
        }
        activePoopAbsorptionCircleVisualizers.Clear();
    }
    
    // NEW: Method to update poop absorption radius visualizers
    void UpdatePoopAbsorptionCircleVisualizers()
    {
        if (!Application.isPlaying) return;
        
        // Check if visualization is enabled
        bool showCircles = showPoopAbsorptionRadiiRuntime && circleVisualizerPrefab != null && circleContainer != null;
        
        if (!showCircles)
        {
            // If visualization is disabled, clean up any existing visualizers
            foreach (var kvp in activePoopAbsorptionCircleVisualizers)
            {
                if (kvp.Value != null) Destroy(kvp.Value.gameObject);
            }
            activePoopAbsorptionCircleVisualizers.Clear();
            return;
        }
        
        // Find all plants with the poop fertilizer effect
        PlantGrowth[] plants = FindObjectsByType<PlantGrowth>(FindObjectsSortMode.None);
        HashSet<PlantGrowth> currentPlantsSet = new HashSet<PlantGrowth>(plants);
        
        // Track plants to remove (no longer exist or don't have the effect)
        List<PlantGrowth> plantsToRemove = new List<PlantGrowth>();
        
        // First, update existing visualizers
        foreach (var kvp in activePoopAbsorptionCircleVisualizers)
        {
            PlantGrowth plant = kvp.Key;
            RuntimeCircleDrawer drawer = kvp.Value;
            
            if (plant == null || drawer == null || !plant.gameObject.activeInHierarchy ||
                !currentPlantsSet.Contains(plant))
            {
                plantsToRemove.Add(plant);
                if (drawer != null) Destroy(drawer.gameObject);
                continue;
            }
            
            // Check if the plant still has a valid poop detection radius
            float poopRadius = GetPlantPoopDetectionRadius(plant);
            bool shouldShowThis = showCircles && poopRadius > 0.01f;
            
            if (shouldShowThis)
            {
                // Update drawer position and radius
                drawer.transform.position = plant.transform.position;
                drawer.UpdateCircle(poopRadius, poopAbsorptionRadiusColorRuntime);
            }
            else
            {
                drawer.HideCircle();
                plantsToRemove.Add(plant); // If radius is too small or zero, remove the visualizer
            }
        }
        
        // Remove plants that no longer need visualization
        foreach (var plant in plantsToRemove)
        {
            if (activePoopAbsorptionCircleVisualizers.TryGetValue(plant, out RuntimeCircleDrawer drawer))
            {
                if (drawer != null) Destroy(drawer.gameObject);
                activePoopAbsorptionCircleVisualizers.Remove(plant);
            }
        }
        
        // Create new visualizers for plants with poop fertilizer effect
        foreach (PlantGrowth plant in plants)
        {
            if (plant == null || activePoopAbsorptionCircleVisualizers.ContainsKey(plant)) continue;
            
            float poopRadius = GetPlantPoopDetectionRadius(plant);
            if (poopRadius <= 0.01f) continue; // Skip if radius is too small
            
            // Create new visualizer
            GameObject circleGO = Instantiate(circleVisualizerPrefab, plant.transform.position, 
                                             Quaternion.identity, circleContainer);
            RuntimeCircleDrawer newDrawer = circleGO.GetComponent<RuntimeCircleDrawer>();
            
            if (newDrawer != null)
            {
                newDrawer.UpdateCircle(poopRadius, poopAbsorptionRadiusColorRuntime);
                activePoopAbsorptionCircleVisualizers.Add(plant, newDrawer);
            }
            else
            {
                Debug.LogError($"Circle Visualizer Prefab '{circleVisualizerPrefab.name}' missing RuntimeCircleDrawer script!", circleVisualizerPrefab);
                Destroy(circleGO);
            }
        }
    }
    
    // NEW: Helper method to get poop detection radius from a plant
    private float GetPlantPoopDetectionRadius(PlantGrowth plant)
    {
        if (plant == null) return 0f;
        return plant.GetPoopDetectionRadius();
    }

    // --- Runtime Visualizer Update ---
    void UpdateRuntimeCircleVisualizers()
    {
        if (!Application.isPlaying) return; // Only run in play mode

        // Check if lines should be shown globally
        bool showCircles = showScentRadiiRuntime && circleVisualizerPrefab != null && circleContainer != null;

        // --- Update existing circles and create new ones ---
        // Use a temporary list to avoid modifying dictionary while iterating
        List<ScentSource> sourcesToRemove = new List<ScentSource>();

        foreach (var kvp in activeCircleVisualizers)
        {
            ScentSource source = kvp.Key;
            RuntimeCircleDrawer line = kvp.Value;

            if (source == null || line == null || !source.gameObject.activeInHierarchy) // Source or drawer destroyed unexpectedly
            {
                sourcesToRemove.Add(source); // Mark for removal
                if(line != null) Destroy(line.gameObject); // Destroy orphan drawer
                continue;
            }

            // Check if circles should be shown globally and if this source is valid
            bool shouldShowThis = showCircles && source.enabled && source.definition != null && source.EffectiveRadius > 0.01f;

            if (shouldShowThis)
            {
                 // Update drawer position to match source and update circle params
                 line.transform.position = source.transform.position;
                 line.transform.rotation = source.transform.rotation; // Match rotation? Optional.
                 line.UpdateCircle(source.EffectiveRadius, scentRadiusColorRuntime);
            }
            else
            {
                 line.HideCircle(); // Hide if shouldn't be shown
            }
        }

        // Remove entries for sources that are gone
        foreach (var source in sourcesToRemove)
        {
            if (activeCircleVisualizers.TryGetValue(source, out RuntimeCircleDrawer drawer) && drawer != null)
                Destroy(drawer.gameObject);
            activeCircleVisualizers.Remove(source);
        }

        // --- Add circles for new sources ---
        if (showCircles)
        {
            // Find all active ScentSources
            ScentSource[] currentSources = FindObjectsByType<ScentSource>(FindObjectsSortMode.None);
            
            foreach (ScentSource source in currentSources)
            {
                // Skip if already has a visualizer or is invalid
                if (source == null || activeCircleVisualizers.ContainsKey(source) || !source.enabled || 
                    source.definition == null || source.EffectiveRadius <= 0.01f) continue;

                 // Create new visualizer
                 GameObject circleGO = Instantiate(circleVisualizerPrefab, source.transform.position, 
                                                 source.transform.rotation, circleContainer);
                 RuntimeCircleDrawer newDrawer = circleGO.GetComponent<RuntimeCircleDrawer>();

                 if (newDrawer != null)
                 {
                      newDrawer.UpdateCircle(source.EffectiveRadius, scentRadiusColorRuntime);
                      activeCircleVisualizers.Add(source, newDrawer); // Add to tracking
                 }
                 else
                 {
                    Debug.LogError($"Circle Visualizer Prefab '{circleVisualizerPrefab.name}' is missing RuntimeCircleDrawer script!", circleVisualizerPrefab);
                    Destroy(circleGO);
                 }
            }
        }
        // --- Hide/Destroy all if global flag turned off ---
        else if (!showCircles && activeCircleVisualizers.Count > 0)
        {
             foreach (var kvp in activeCircleVisualizers)
             {
                 if (kvp.Value != null) Destroy(kvp.Value.gameObject);
             }
             activeCircleVisualizers.Clear();
        }
    }


    // --- Gizmo Drawing (Editor Visualization - Unchanged) ---
    #if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        // Use the runtime flag to ALSO control the editor gizmo
        if (showScentRadiiRuntime) {
             if (logGizmoCalls) { /*...*/ }
             Gizmos.color = scentRadiusColorRuntime; // Use runtime color for gizmo too
             ScentSource[] scentSources = FindObjectsByType<ScentSource>(FindObjectsSortMode.None);
             if (logGizmoCalls) { /*...*/ }
             int drawnCount = 0;
             foreach (ScentSource source in scentSources) {
                if (source == null || !source.enabled || source.definition == null) continue;
                float radius = source.EffectiveRadius;
                if (radius > 0.01f) { Gizmos.DrawWireSphere(source.transform.position, radius); drawnCount++; }
             }
             if (logGizmoCalls && drawnCount > 0) { /*...*/ }
             else if (logGizmoCalls && scentSources.Length > 0) { /*...*/ }
        }
        
        // NEW: Draw poop absorption radius in editor
        if (showPoopAbsorptionRadiiRuntime) {
             Gizmos.color = poopAbsorptionRadiusColorRuntime;
             PlantGrowth[] plants = FindObjectsByType<PlantGrowth>(FindObjectsSortMode.None);
             int drawnCount = 0;
             
             foreach (PlantGrowth plant in plants) {
                if (plant == null) continue;
                float radius = GetPlantPoopDetectionRadius(plant);
                if (radius > 0.01f) {
                    Gizmos.DrawWireSphere(plant.transform.position, radius);
                    drawnCount++;
                }
             }
             
             if (logGizmoCalls && drawnCount > 0) {
                 Debug.Log($"[FloraManager] Drew {drawnCount} poop absorption radius gizmos");
             }
        }
    }
    #endif
}