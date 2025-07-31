using System.Collections.Generic;
using UnityEngine;
using WegoSystem;

public class FireflyManager : MonoBehaviour, ITickUpdateable
{
    public static FireflyManager Instance { get; private set; }

    [SerializeField] private WeatherManager weatherManager;
    [SerializeField] private GameObject fireflyPrefab;
    [SerializeField] public FireflyDefinition defaultFireflyDefinition; // Made public to fix access issue
    [SerializeField] private Transform fireflyParent;

    [SerializeField] private int maxFireflies = 50;
    [SerializeField] private int spawnIntervalTicks = 3;
    [SerializeField] [Range(0f, 1f)] private float nightThreshold = 0.25f;

    [SerializeField] private Vector2 spawnCenter = Vector2.zero;
    [SerializeField] private Vector2 spawnAreaSize = new Vector2(20f, 10f);

    public float photosynthesisRadius = 3f;
    public float photosynthesisIntensityPerFly = 0.05f;
    public float maxPhotosynthesisBonus = 0.5f;

    [SerializeField] private bool showAttractionLinesRuntime = false;
    [SerializeField] private Color attractionLineColorRuntime = Color.magenta;
    [SerializeField] private GameObject lineVisualizerPrefab;
    [SerializeField] private Transform lineContainer;

    private List<FireflyController> activeFireflies = new List<FireflyController>();
    private Dictionary<FireflyController, LineRenderer> activeLineVisualizers = new Dictionary<FireflyController, LineRenderer>();

    private int spawnTickCounter = 0;
    private bool isNight = false;

    public bool ShowAttractionLinesRuntime => showAttractionLinesRuntime;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        ValidateReferences();
    }

    public void Initialize()
    {
        if (TickManager.Instance != null)
        {
            TickManager.Instance.RegisterTickUpdateable(this);
        }
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;

        // Safely get the instance once
        var tickManager = TickManager.Instance;
        if (tickManager != null)
        {
            tickManager.UnregisterTickUpdateable(this);
        }

        CleanupVisualizers();
    }

    void ValidateReferences()
    {
        if (weatherManager == null)
        {
            Debug.LogError($"[{nameof(FireflyManager)}] WeatherManager not assigned!", this);
            enabled = false;
            return;
        }

        if (fireflyPrefab == null)
        {
            Debug.LogError($"[{nameof(FireflyManager)}] Firefly Prefab not assigned!", this);
            enabled = false;
            return;
        }

        if (defaultFireflyDefinition == null)
        {
            Debug.LogError($"[{nameof(FireflyManager)}] Default Firefly Definition not assigned!", this);
            enabled = false;
            return;
        }

        FireflyController controller = fireflyPrefab.GetComponent<FireflyController>();
        if (controller == null)
        {
            Debug.LogError($"[{nameof(FireflyManager)}] Firefly Prefab missing FireflyController script!", this);
            enabled = false;
            return;
        }

        if (fireflyParent == null)
        {
            fireflyParent = transform;
        }
    }

    public void OnTickUpdate(int currentTick)
    {
        isNight = weatherManager.sunIntensity <= nightThreshold;

        if (isNight)
        {
            spawnTickCounter++;
            if (spawnTickCounter >= spawnIntervalTicks)
            {
                TrySpawnFirefly();
                spawnTickCounter = 0;
            }
        }
        else
        {
            spawnTickCounter = 0;
        }

        activeFireflies.RemoveAll(f => f == null || !f.IsAlive);
    }

    void Update()
    {
        UpdateRuntimeLineVisualizers();
    }

    void TrySpawnFirefly()
    {
        if (activeFireflies.Count >= maxFireflies) return;

        GridPosition spawnGridPos = FindValidSpawnPosition();
        if (spawnGridPos == GridPosition.Zero) return;

        Vector3 spawnWorldPos = GridPositionManager.Instance.GridToWorld(spawnGridPos);

        GameObject fireflyGO = Instantiate(fireflyPrefab, spawnWorldPos, Quaternion.identity, fireflyParent);
        FireflyController controller = fireflyGO.GetComponent<FireflyController>();

        if (controller != null)
        {
            controller.Initialize();
            activeFireflies.Add(controller);
        }
    }

    GridPosition FindValidSpawnPosition()
    {
        Vector2 minBounds = spawnCenter - spawnAreaSize * 0.5f;
        Vector2 maxBounds = spawnCenter + spawnAreaSize * 0.5f;

        GridPosition minGrid = GridPositionManager.Instance.WorldToGrid(minBounds);
        GridPosition maxGrid = GridPositionManager.Instance.WorldToGrid(maxBounds);

        for (int i = 0; i < 10; i++) // Try 10 times to find a spot
        {
            int x = Random.Range(minGrid.x, maxGrid.x + 1);
            int y = Random.Range(minGrid.y, maxGrid.y + 1);
            GridPosition pos = new GridPosition(x, y);

            if (GridPositionManager.Instance.IsPositionValid(pos) &&
                !GridPositionManager.Instance.IsPositionOccupied(pos))
            {
                return pos;
            }
        }

        return GridPosition.Zero; // Failed to find a spot
    }

    public void ReportFireflyDespawned(FireflyController firefly)
    {
        activeFireflies.Remove(firefly);

        if (activeLineVisualizers.TryGetValue(firefly, out LineRenderer line))
        {
            if (line != null) Destroy(line.gameObject);
            activeLineVisualizers.Remove(firefly);
        }
    }

    public int GetNearbyFireflyCount(Vector3 position, float radius)
    {
        int count = 0;
        float radiusSq = radius * radius;

        for (int i = activeFireflies.Count - 1; i >= 0; i--)
        {
            if (activeFireflies[i] == null)
            {
                activeFireflies.RemoveAt(i);
                continue;
            }

            if ((activeFireflies[i].transform.position - position).sqrMagnitude <= radiusSq)
            {
                count++;
            }
        }

        return count;
    }

    void UpdateRuntimeLineVisualizers()
    {
        if (!Application.isPlaying || !showAttractionLinesRuntime)
        {
            CleanupVisualizers();
            return;
        }

        var toRemove = new List<FireflyController>();
        foreach (var kvp in activeLineVisualizers)
        {
            if (kvp.Key == null || kvp.Value == null)
            {
                toRemove.Add(kvp.Key);
                continue;
            }

            Transform target = kvp.Key.AttractionTarget;
            if (target != null)
            {
                kvp.Value.enabled = true;
                kvp.Value.SetPosition(0, kvp.Key.transform.position);
                kvp.Value.SetPosition(1, target.position);
                kvp.Value.startColor = attractionLineColorRuntime;
                kvp.Value.endColor = attractionLineColorRuntime;
            }
            else
            {
                kvp.Value.enabled = false;
            }
        }

        foreach (var firefly in toRemove)
        {
            if (activeLineVisualizers.TryGetValue(firefly, out var line) && line != null)
            {
                Destroy(line.gameObject);
            }
            activeLineVisualizers.Remove(firefly);
        }

        if (lineVisualizerPrefab != null && lineContainer != null)
        {
            foreach (var firefly in activeFireflies)
            {
                if (firefly == null || activeLineVisualizers.ContainsKey(firefly)) continue;

                if (firefly.AttractionTarget != null)
                {
                    GameObject lineGO = Instantiate(lineVisualizerPrefab, lineContainer);
                    LineRenderer newLine = lineGO.GetComponent<LineRenderer>();

                    if (newLine != null)
                    {
                        newLine.SetPosition(0, firefly.transform.position);
                        newLine.SetPosition(1, firefly.AttractionTarget.position);
                        newLine.startColor = attractionLineColorRuntime;
                        newLine.endColor = attractionLineColorRuntime;
                        newLine.enabled = true;
                        activeLineVisualizers.Add(firefly, newLine);
                    }
                    else
                    {
                        Debug.LogError($"Line Visualizer Prefab missing LineRenderer!", lineVisualizerPrefab);
                        Destroy(lineGO);
                    }
                }
            }
        }
    }

    void CleanupVisualizers()
    {
        foreach (var kvp in activeLineVisualizers)
        {
            if (kvp.Value != null)
            {
                Destroy(kvp.Value.gameObject);
            }
        }
        activeLineVisualizers.Clear();
    }

    void OnDrawGizmosSelected()
    {
        // Draw spawn area
        Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
        Gizmos.DrawWireCube(spawnCenter, spawnAreaSize);

        // Draw photosynthesis radius for each active firefly
        if (Application.isPlaying)
        {
            Gizmos.color = new Color(0f, 1f, 0.5f, 0.2f);
            foreach (var firefly in activeFireflies)
            {
                if (firefly != null)
                {
                    Gizmos.DrawWireSphere(firefly.transform.position, photosynthesisRadius);
                }
            }
        }
    }
}