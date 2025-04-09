using UnityEngine;
using System.Collections.Generic;

public class FireflyManager : MonoBehaviour
{
    public static FireflyManager Instance { get; private set; }

    [Header("Core Dependencies")]
    [SerializeField] private WeatherManager weatherManager;
    [SerializeField] private GameObject fireflyPrefab;
    [SerializeField] private Transform fireflyParent; // Optional: Parent for spawned fireflies

    [Header("Spawning Settings")]
    [SerializeField] private int maxFireflies = 50;
    [Tooltip("Time in seconds between spawn attempts at night.")]
    [SerializeField] private float spawnInterval = 0.5f;
    [Tooltip("Sun intensity below which fireflies start spawning (0=full night, 1=full day).")]
    [SerializeField] [Range(0f, 1f)] private float nightThreshold = 0.25f;

    [Header("Spawn Area")]
    [SerializeField] private Vector2 spawnCenter = Vector2.zero;
    [SerializeField] private Vector2 spawnAreaSize = new Vector2(20f, 10f);

    [Header("Movement Bounds (for Fireflies)")]
    [SerializeField] private Vector2 movementMinBounds = new Vector2(-12f, -7f);
    [SerializeField] private Vector2 movementMaxBounds = new Vector2(12f, 7f);
    
    // Add these fields in the Header sections
    [Header("Photosynthesis Bonus Settings")]
    [Tooltip("Radius around a Plant within which Fireflies contribute to photosynthesis.")]
    public float photosynthesisRadius = 3f;
    [Tooltip("Photosynthesis rate bonus provided per nearby Firefly.")]
    public float photosynthesisIntensityPerFly = 0.05f;
    [Tooltip("Maximum photosynthesis rate bonus achievable from Fireflies.")]
    public float maxPhotosynthesisBonus = 0.5f;

    // Internal State
    private List<FireflyController> activeFireflies = new List<FireflyController>();
    private float spawnTimer;
    private bool isNight = false;

    void Awake()
    {
        // Singleton Pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Validate Dependencies
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
        if (fireflyPrefab.GetComponent<FireflyController>() == null)
        {
             Debug.LogError($"[{nameof(FireflyManager)}] Assigned Firefly Prefab is missing the FireflyController script!", fireflyPrefab);
             enabled = false;
             return;
        }
        if (fireflyParent == null)
        {
            Debug.LogWarning($"[{nameof(FireflyManager)}] Firefly Parent not assigned. Fireflies will spawn at root level.", this);
            fireflyParent = transform; // Default to this manager's transform
        }
    }

    void Update()
    {
        // Check Day/Night State
        isNight = weatherManager.sunIntensity <= nightThreshold;

        if (isNight)
        {
            // Spawn Timer Logic
            spawnTimer -= Time.deltaTime;
            if (spawnTimer <= 0f)
            {
                TrySpawnFirefly();
                spawnTimer = spawnInterval; // Reset timer
            }
        }
        else
        {
            // Optional: Could despawn fireflies instantly when day comes,
            // or let them naturally time out via their own lifetime.
            // For now, we let them time out.
            spawnTimer = spawnInterval; // Reset timer when not night
        }
    }

    void TrySpawnFirefly()
    {
        if (activeFireflies.Count >= maxFireflies)
        {
            return; // Limit reached
        }

        // Calculate random spawn position
        float spawnX = spawnCenter.x + Random.Range(-spawnAreaSize.x / 2f, spawnAreaSize.x / 2f);
        float spawnY = spawnCenter.y + Random.Range(-spawnAreaSize.y / 2f, spawnAreaSize.y / 2f);
        Vector2 spawnPos = new Vector2(spawnX, spawnY);

        // Instantiate and Initialize
        GameObject fireflyGO = Instantiate(fireflyPrefab, spawnPos, Quaternion.identity, fireflyParent);
        FireflyController controller = fireflyGO.GetComponent<FireflyController>();

        if (controller != null)
        {
            controller.Initialize(this, movementMinBounds, movementMaxBounds);
            activeFireflies.Add(controller);
        }
        else
        {
            // This check should ideally be caught in Awake, but safety first
            Debug.LogError($"[{nameof(FireflyManager)}] Spawned Firefly Prefab is missing the FireflyController script!", fireflyGO);
            Destroy(fireflyGO); // Clean up invalid spawn
        }
    }
    
    public int GetNearbyFireflyCount(Vector3 position, float radius)
    {
        int count = 0;
        float radiusSq = radius * radius; // Use squared distance for efficiency

        // Iterate backwards for safe removal if needed (though not used here)
        for (int i = activeFireflies.Count - 1; i >= 0; i--)
        {
            // Check if firefly is null or destroyed (safety check)
            if (activeFireflies[i] == null)
            {
                activeFireflies.RemoveAt(i);
                continue;
            }

            // Calculate squared distance
            if ((activeFireflies[i].transform.position - position).sqrMagnitude <= radiusSq)
            {
                count++;
            }
        }
        return count;
    }
    
    // Called by FireflyController when its lifetime expires
    public void ReportFireflyDespawned(FireflyController firefly)
    {
        if (activeFireflies.Contains(firefly))
        {
            activeFireflies.Remove(firefly);
        }
    }

    // --- Gizmos for Visualization ---
    void OnDrawGizmosSelected()
    {
        // Draw Spawn Area
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(spawnCenter, spawnAreaSize);

        // Draw Movement Bounds
        Gizmos.color = Color.yellow;
        Vector3 boundsCenter = (movementMinBounds + movementMaxBounds) / 2f;
        Vector3 boundsSize = movementMaxBounds - movementMinBounds;
        Gizmos.DrawWireCube(boundsCenter, boundsSize);
    }
}