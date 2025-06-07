using UnityEngine;
using UnityEngine.Rendering.Universal;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class FireflyManager : MonoBehaviour
{
    public static FireflyManager Instance { get; private set; }

    [Header("Core Dependencies")]
    [SerializeField] private WeatherManager weatherManager;
    [SerializeField] private GameObject fireflyPrefab;
    [SerializeField] private Transform fireflyParent;

    // (Keep Spawning Settings, Spawn Area, Movement Bounds, Photosynthesis Bonus)
    [Header("Spawning Settings")]
    [SerializeField] private int maxFireflies = 50;
    [SerializeField] private float spawnInterval = 0.5f;
    [SerializeField] [Range(0f, 1f)] private float nightThreshold = 0.25f;

    [Header("Spawn Area")]
    [SerializeField] private Vector2 spawnCenter = Vector2.zero;
    [SerializeField] private Vector2 spawnAreaSize = new Vector2(20f, 10f);

    [Header("Movement Bounds (for Fireflies)")]
    [SerializeField] private Vector2 movementMinBounds = new Vector2(-12f, -7f);
    [SerializeField] private Vector2 movementMaxBounds = new Vector2(12f, 7f);

    [Header("Photosynthesis Bonus Settings")]
    public float photosynthesisRadius = 3f;
    public float photosynthesisIntensityPerFly = 0.05f;
    public float maxPhotosynthesisBonus = 0.5f;


    [Header("Debugging")]
    [Tooltip("Show attraction lines in Game View during runtime.")]
    [SerializeField] private bool showAttractionLinesRuntime = false;
    [SerializeField] private Color attractionLineColorRuntime = Color.magenta; // Renamed for Gizmo
    [SerializeField] private bool logGizmoCalls = false;
    [Space] // Add space for visual separation
    [Tooltip("Prefab used to draw attraction lines at runtime.")]
    [SerializeField] private GameObject lineVisualizerPrefab; // <<< ADDED
    [Tooltip("Parent transform for instantiated line visualizers.")]
    [SerializeField] private Transform lineContainer; // <<< ADDED

    // --- Public Accessor ---
    public bool ShowAttractionLinesRuntime => showAttractionLinesRuntime;

    // --- Internal State ---
    private List<FireflyController> activeFireflies = new List<FireflyController>();
    private float spawnTimer;
    private bool isNight = false;
    
    

    // Dictionary to track line visualizers per firefly
    private Dictionary<FireflyController, LineRenderer> activeLineVisualizers = new Dictionary<FireflyController, LineRenderer>();

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        // Validate Core Dependencies
        if (weatherManager == null) { Debug.LogError($"[{nameof(FireflyManager)}] WeatherManager missing!", this); enabled = false; return; }
        if (fireflyPrefab == null) { Debug.LogError($"[{nameof(FireflyManager)}] Firefly Prefab missing!", this); enabled = false; return; }
        if (fireflyPrefab.GetComponent<FireflyController>() == null) { Debug.LogError($"[{nameof(FireflyManager)}] Firefly Prefab missing Controller script!", this); enabled = false; return; }
        if (fireflyParent == null) { fireflyParent = transform; }

        // Validate Debug Dependencies
        if (lineVisualizerPrefab == null) { Debug.LogError($"[{nameof(FireflyManager)}] Line Visualizer Prefab is not assigned!", this); }
        if (lineContainer == null) { Debug.LogError($"[{nameof(FireflyManager)}] Line Container transform is not assigned!", this); }
    }

    void Update()
    {
        isNight = weatherManager.sunIntensity <= nightThreshold;

        if (isNight) {
            spawnTimer -= Time.deltaTime;
            if (spawnTimer <= 0f) { TrySpawnFirefly(); spawnTimer = spawnInterval; }
        } else { spawnTimer = spawnInterval; }

        // Update runtime visualizers in Update
        UpdateRuntimeLineVisualizers();
    }

    void TrySpawnFirefly()
    {
        if (activeFireflies.Count >= maxFireflies) return;

        float spawnX = spawnCenter.x + Random.Range(-spawnAreaSize.x / 2f, spawnAreaSize.x / 2f);
        float spawnY = spawnCenter.y + Random.Range(-spawnAreaSize.y / 2f, spawnAreaSize.y / 2f);
        Vector2 spawnPos = new Vector2(spawnX, spawnY);

        GameObject fireflyGO = Instantiate(fireflyPrefab, spawnPos, Quaternion.identity, fireflyParent);
        FireflyController controller = fireflyGO.GetComponent<FireflyController>();

        if (controller != null) {
            controller.Initialize(this, movementMinBounds, movementMaxBounds);
            activeFireflies.Add(controller);
            // Don't create line visualizer here, do it in Update when needed
        } else { /* LogError, Destroy */ }
    }

    public void ReportFireflyDespawned(FireflyController firefly)
    {
        activeFireflies.Remove(firefly);

        // Clean up associated line visualizer
        if (activeLineVisualizers.TryGetValue(firefly, out LineRenderer line))
        {
            if (line != null) Destroy(line.gameObject); // Destroy the visualizer GO
            activeLineVisualizers.Remove(firefly);
        }
    }

    // --- Runtime Visualizer Update ---
    void UpdateRuntimeLineVisualizers()
    {
        if (!Application.isPlaying) return; // Only run in play mode

        // Check if lines should be shown globally
        bool showLines = showAttractionLinesRuntime && lineVisualizerPrefab != null && lineContainer != null;

        // --- Update existing lines and create new ones ---
        // Use a temporary list to avoid modifying dictionary while iterating
        List<FireflyController> firefliesToRemoveLine = new List<FireflyController>();

        foreach (var kvp in activeLineVisualizers)
        {
            FireflyController firefly = kvp.Key;
            LineRenderer line = kvp.Value;

            if (firefly == null || line == null) // Firefly or line destroyed unexpectedly
            {
                firefliesToRemoveLine.Add(firefly); // Mark for removal
                if(line != null) Destroy(line.gameObject); // Destroy orphan line
                continue;
            }

            Transform target = firefly.AttractionTarget;

            if (showLines && target != null) // Should be visible and has target
            {
                line.enabled = true;
                line.SetPosition(0, firefly.transform.position);
                line.SetPosition(1, target.position);
                
                // IMPORTANT: Ensure the color is set properly
                if (line.startColor != attractionLineColorRuntime || line.endColor != attractionLineColorRuntime)
                {
                    line.startColor = attractionLineColorRuntime;
                    line.endColor = attractionLineColorRuntime;
                    
                    if (Debug.isDebugBuild)
                        Debug.Log($"[FireflyManager] Set line color to {attractionLineColorRuntime}", line.gameObject);
                }
            }
            else // Should be hidden or lost target
            {
                line.enabled = false;
            }
        }

        // Remove entries whose fireflies are gone
        foreach (var firefly in firefliesToRemoveLine)
        {
            activeLineVisualizers.Remove(firefly);
        }


        // --- Add lines for fireflies that don't have one yet ---
        if (showLines)
        {
            foreach (FireflyController firefly in activeFireflies)
            {
                if (firefly == null || activeLineVisualizers.ContainsKey(firefly)) continue; // Skip nulls or those already processed

                Transform target = firefly.AttractionTarget;
                if (target != null) // Only create if it has a target AND should be shown
                {
                    GameObject lineGO = Instantiate(lineVisualizerPrefab, lineContainer); // Instantiate under container
                    LineRenderer newLine = lineGO.GetComponent<LineRenderer>();
                    if (newLine != null)
                    {
                        // Configure initial points (will be updated next frame anyway)
                        newLine.SetPosition(0, firefly.transform.position);
                        newLine.SetPosition(1, target.position);
                        
                        // IMPORTANT: Set the line color correctly
                        newLine.startColor = attractionLineColorRuntime;
                        newLine.endColor = attractionLineColorRuntime;
                        
                        newLine.enabled = true;
                        activeLineVisualizers.Add(firefly, newLine); // Add to tracking dictionary
                        
                        if (Debug.isDebugBuild)
                            Debug.Log($"[FireflyManager] Created new line with color {attractionLineColorRuntime}", newLine.gameObject);
                    }
                    else
                    {
                         Debug.LogError($"Line Visualizer Prefab '{lineVisualizerPrefab.name}' is missing LineRenderer component!", lineVisualizerPrefab);
                         Destroy(lineGO); // Destroy invalid instance
                    }
                }
            }
        }
        // --- Hide/Destroy lines if global flag turned off ---
        else if (!showLines && activeLineVisualizers.Count > 0)
        {
             // Destroy all active line visualizers if the flag is off
             foreach (var kvp in activeLineVisualizers)
             {
                 if (kvp.Value != null) Destroy(kvp.Value.gameObject);
             }
             activeLineVisualizers.Clear();
        }
    }


    // (Keep GetNearbyFireflyCount)
     public int GetNearbyFireflyCount(Vector3 position, float radius)
    {
        int count = 0; float radiusSq = radius * radius;
        for (int i = activeFireflies.Count - 1; i >= 0; i--)
        {
            if (activeFireflies[i] == null) { activeFireflies.RemoveAt(i); continue; }
            if ((activeFireflies[i].transform.position - position).sqrMagnitude <= radiusSq) { count++; }
        }
        return count;
    }

    // --- Gizmos (Editor Visualization - Unchanged) ---
    #if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green; Gizmos.DrawWireCube(spawnCenter, spawnAreaSize);
        Gizmos.color = Color.blue;
        Vector3 boundsCenter = (movementMinBounds + movementMaxBounds) / 2f;
        Vector3 boundsSize = movementMaxBounds - movementMinBounds;
        Gizmos.DrawWireCube(boundsCenter, boundsSize);

        // Gizmo drawing for attraction lines
        if (showAttractionLinesRuntime && Application.isPlaying) {
             if (logGizmoCalls) { /*...*/ }
             bool didDrawLine = false;
             Gizmos.color = attractionLineColorRuntime; // Use Gizmo color
             foreach (FireflyController firefly in activeFireflies) {
                if (firefly == null) continue;
                Transform target = firefly.AttractionTarget;
                if (target != null) { Gizmos.DrawLine(firefly.transform.position, target.position); didDrawLine = true; }
             }
             if (logGizmoCalls && !didDrawLine && activeFireflies.Count > 0) { /*...*/ }
        }
    }
    #endif
}