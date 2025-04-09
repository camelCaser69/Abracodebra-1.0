// FILE: Assets/Scripts/Ecosystem/Core/FloraManager.cs
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

    // --- Public Accessors ---
    public bool ShowScentRadiiRuntime => showScentRadiiRuntime;
    public Color ScentRadiusColorRuntime => scentRadiusColorRuntime;

    // --- Internal State ---
    // Dictionary to track circle visualizers per ScentSource
    private Dictionary<ScentSource, RuntimeCircleDrawer> activeCircleVisualizers = new Dictionary<ScentSource, RuntimeCircleDrawer>();


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
    }


    // --- Runtime Visualizer Update ---
    void UpdateRuntimeCircleVisualizers()
    {
        bool showCircles = showScentRadiiRuntime && circleVisualizerPrefab != null && circleContainer != null;

        // Find all active ScentSources (can be slow, consider optimizing if needed)
        // If performance becomes an issue, ScentSources could register/deregister themselves with the manager.
        ScentSource[] currentSources = FindObjectsByType<ScentSource>(FindObjectsSortMode.None);
        HashSet<ScentSource> currentSourcesSet = new HashSet<ScentSource>(currentSources); // For quick lookup

        // --- Update existing circles ---
        List<ScentSource> sourcesToRemove = new List<ScentSource>();
        foreach (var kvp in activeCircleVisualizers)
        {
            ScentSource source = kvp.Key;
            RuntimeCircleDrawer drawer = kvp.Value;

            // Check if source still exists and is valid
            if (source == null || drawer == null || !source.gameObject.activeInHierarchy || !currentSourcesSet.Contains(source))
            {
                sourcesToRemove.Add(source); // Mark for removal
                if (drawer != null) Destroy(drawer.gameObject); // Destroy visualizer
                continue;
            }

            // Check if circles should be shown globally and if this source is valid
            bool shouldShowThis = showCircles && source.enabled && source.definition != null && source.EffectiveRadius > 0.01f;

            if (shouldShowThis)
            {
                 // Update drawer position to match source and update circle params
                 drawer.transform.position = source.transform.position;
                 drawer.transform.rotation = source.transform.rotation; // Match rotation? Optional.
                 drawer.UpdateCircle(source.EffectiveRadius, scentRadiusColorRuntime);
            }
            else
            {
                 drawer.HideCircle(); // Hide if shouldn't be shown
            }
        }

        // Remove entries for sources that are gone
        foreach (var source in sourcesToRemove)
        {
            activeCircleVisualizers.Remove(source);
        }

        // --- Add circles for new sources ---
        if (showCircles)
        {
            foreach (ScentSource source in currentSources)
            {
                // Skip if already has a visualizer or is invalid
                if (source == null || activeCircleVisualizers.ContainsKey(source) || !source.enabled || source.definition == null || source.EffectiveRadius <= 0.01f) continue;

                 // Create new visualizer
                 GameObject circleGO = Instantiate(circleVisualizerPrefab, source.transform.position, source.transform.rotation, circleContainer);
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
    }
    #endif
}