// FILE: Assets/Scripts/Ecosystem/Core/FaunaManager.cs
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq; // Needed for removing nulls

public enum WaveManagerState
{
    Idle,           // Doing nothing, waiting to start
    SpawningWave,   // Currently running spawn coroutines for the wave
    WaitingForWaveEnd, // Spawning finished, waiting for end condition
    BetweenWaves,   // Wave ended, pausing before the next
    SequenceComplete // All waves finished
}

public class FaunaManager : MonoBehaviour
{
    [Header("Wave Setup")]
    [Tooltip("The sequence of Wave Definitions to execute.")]
    [SerializeField] private List<WaveDefinition> wavesSequence;
    [Tooltip("Reference to the player transform (needed for 'RandomNearPlayer' spawning).")]
    [SerializeField] private Transform playerTransform; // <<< ADDED: Need player reference

    [Header("Spawning Area (Global)")]
    [Tooltip("Center point for the 'GlobalSpawnArea' location type.")]
    [SerializeField] private Vector2 spawnCenter = Vector2.zero;
    [Tooltip("Size of the rectangle for the 'GlobalSpawnArea' location type.")]
    [SerializeField] private Vector2 spawnAreaSize = new Vector2(20f, 10f);

    [Header("General Settings")]
    [Tooltip("Parent transform for spawned animals (e.g., 'SpawnedEcosystem/Animals').")]
    [SerializeField] private Transform ecosystemParent;
    [Tooltip("Global movement bounds applied to spawned animals.")]
    [SerializeField] private Vector2 animalMinBounds = new Vector2(-10f, -5f);
    [SerializeField] private Vector2 animalMaxBounds = new Vector2(10f, 5f);

    [Header("Debugging & State")]
    [SerializeField] private bool startFirstWaveAutomatically = true;
    [SerializeField] private bool loopSequence = false; // Should waves repeat after finishing?
    [SerializeField] private WaveManagerState currentState = WaveManagerState.Idle;
    [SerializeField] private int currentWaveIndex = -1; // -1 means not started yet

    // --- Runtime State ---
    private WaveDefinition currentWaveDefinition = null;
    private List<GameObject> spawnedWaveAnimals = new List<GameObject>();
    private List<Coroutine> activeSpawnCoroutines = new List<Coroutine>();
    private float waveTimer = 0f;
    private float betweenWaveTimer = 0f;
    private float waveStartedTimestamp = 0f;

    // --- Public Accessors ---
    public WaveManagerState CurrentState => currentState;
    public int CurrentWaveNumber => currentWaveIndex + 1; // User-friendly wave number (1-based)
    public int TotalWaves => wavesSequence != null ? wavesSequence.Count : 0;


    void Start()
    {
        InitializeManager();

        if (startFirstWaveAutomatically)
        {
            // Add a small delay to ensure other systems initialize
            Invoke(nameof(StartNextWave), 0.5f);
        }
    }

    void InitializeManager()
    {
        currentState = WaveManagerState.Idle;
        currentWaveIndex = -1;
        spawnedWaveAnimals.Clear();
        activeSpawnCoroutines.Clear();
        currentWaveDefinition = null;

        // Basic Validations
        if (wavesSequence == null || wavesSequence.Count == 0)
        {
            Debug.LogWarning("[FaunaManager] No waves assigned in wavesSequence!", this);
            currentState = WaveManagerState.SequenceComplete; // Can't start if no waves
        }
        if (playerTransform == null)
        {
            // Attempt to find player by tag if not assigned
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                playerTransform = playerObj.transform;
                Debug.LogWarning("[FaunaManager] Player Transform not assigned, found by tag 'Player'.", this);
            }
             else {
                  Debug.LogError("[FaunaManager] Player Transform reference is missing and not found by tag! 'RandomNearPlayer' spawning will fail.", this);
             }
        }
        if (ecosystemParent == null)
        {
             // Fallback to own transform if parent not set
             ecosystemParent = transform;
             Debug.LogWarning("[FaunaManager] Ecosystem Parent not assigned, spawned animals will be parented to FaunaManager.", this);
        }
    }

    void Update()
    {
        // State Machine Logic
        switch (currentState)
        {
            case WaveManagerState.Idle:
                // Waiting for StartNextWave() to be called
                break;

            case WaveManagerState.SpawningWave:
                CheckIfSpawningComplete();
                break;

            case WaveManagerState.WaitingForWaveEnd:
                CheckWaveEndCondition();
                break;

            case WaveManagerState.BetweenWaves:
                UpdateBetweenWavesTimer();
                break;

             case WaveManagerState.SequenceComplete:
                 // Do nothing further, sequence is done (unless looping)
                 break;
        }

        // --- Simple Debug Input --- (REMOVE FOR FINAL GAME)
        if (Input.GetKeyDown(KeyCode.N)) // Manually start next wave
        {
            if (currentState == WaveManagerState.Idle || currentState == WaveManagerState.BetweenWaves || currentState == WaveManagerState.SequenceComplete)
            {
                StartNextWave();
            } else {
                 Debug.Log("[FaunaManager] Cannot start next wave manually, current wave is still active.");
            }
        }
        // -------------------------
    }

    /// <summary>
    /// Checks if all spawn coroutines for the current wave have finished.
    /// </summary>
    void CheckIfSpawningComplete()
    {
        if (activeSpawnCoroutines.Count == 0)
        {
            Debug.Log($"[FaunaManager] Wave {CurrentWaveNumber}: All spawn entries initiated/completed.");
            currentState = WaveManagerState.WaitingForWaveEnd;
            // Reset wave timer ONLY if the condition uses it
            if (currentWaveDefinition != null && currentWaveDefinition.endCondition == WaveEndCondition.Timer)
            {
                waveTimer = currentWaveDefinition.durationSeconds;
                Debug.Log($"[FaunaManager] Wave {CurrentWaveNumber}: Starting end condition timer: {waveTimer}s");
            }
        }
    }

    /// <summary>
    /// Checks if the current wave's end condition has been met.
    /// </summary>
    void CheckWaveEndCondition()
    {
        if (currentWaveDefinition == null) return; // Should not happen in this state

        bool waveComplete = false;

        switch (currentWaveDefinition.endCondition)
        {
            case WaveEndCondition.DefeatAllSpawned:
                // Remove destroyed animals (null entries)
                spawnedWaveAnimals.RemoveAll(animal => animal == null);
                if (spawnedWaveAnimals.Count == 0)
                {
                    Debug.Log($"[FaunaManager] Wave {CurrentWaveNumber}: Completed (DefeatAllSpawned).");
                    waveComplete = true;
                }
                break;

            case WaveEndCondition.Timer:
                waveTimer -= Time.deltaTime;
                if (waveTimer <= 0f)
                {
                    Debug.Log($"[FaunaManager] Wave {CurrentWaveNumber}: Completed (Timer expired).");
                    waveComplete = true;
                }
                break;
        }

        if (waveComplete)
        {
            EndCurrentWave();
        }
    }

    /// <summary>
    /// Handles the timer countdown between waves.
    /// </summary>
    void UpdateBetweenWavesTimer()
    {
        betweenWaveTimer -= Time.deltaTime;
        if (betweenWaveTimer <= 0f)
        {
            // Option 1: Automatically start next wave
             // StartNextWave();

            // Option 2: Transition to Idle and wait for external trigger (e.g., player presses button)
             currentState = WaveManagerState.Idle;
             Debug.Log($"[FaunaManager] Between wave delay finished. Ready for next wave (Current State: {currentState}).");
             // Fire an event here if needed: OnReadyForNextWave?.Invoke();
        }
    }


    /// <summary>
    /// Starts the next wave in the sequence, if available.
    /// </summary>
    public void StartNextWave()
    {
        if (currentState == WaveManagerState.SpawningWave || currentState == WaveManagerState.WaitingForWaveEnd)
        {
            Debug.LogWarning("[FaunaManager] Cannot start next wave, current wave is still in progress.");
            return;
        }

        currentWaveIndex++;

        // Check if sequence completed
        if (currentWaveIndex >= wavesSequence.Count)
        {
            if (loopSequence)
            {
                Debug.Log("[FaunaManager] Wave sequence finished, looping back to start.");
                currentWaveIndex = 0;
            }
            else
            {
                Debug.Log("[FaunaManager] Wave sequence complete.");
                currentState = WaveManagerState.SequenceComplete;
                currentWaveDefinition = null;
                // Fire event: OnSequenceComplete?.Invoke();
                return;
            }
        }

        // Load the next wave definition
        currentWaveDefinition = wavesSequence[currentWaveIndex];
        if (currentWaveDefinition == null)
        {
            Debug.LogError($"[FaunaManager] Wave definition at index {currentWaveIndex} is NULL! Skipping wave.");
            // Immediately try to start the *next* one to avoid getting stuck
            // This could lead to infinite loop if all remaining are null. Add safety counter?
            StartNextWave();
            return;
        }

        Debug.Log($"[FaunaManager] Starting Wave {CurrentWaveNumber}/{TotalWaves}: '{currentWaveDefinition.waveName}'");

        // Reset state for the new wave
        currentState = WaveManagerState.SpawningWave;
        waveStartedTimestamp = Time.time;
        spawnedWaveAnimals.Clear();
        activeSpawnCoroutines.Clear();
        // waveTimer reset happens in CheckIfSpawningComplete if needed

        // --- Start Spawn Coroutines ---
        if (currentWaveDefinition.spawnEntries == null || currentWaveDefinition.spawnEntries.Count == 0)
        {
             Debug.LogWarning($"[FaunaManager] Wave {CurrentWaveNumber} has no spawn entries defined. Moving to WaitingForWaveEnd state immediately.");
             // Need to manually transition if no coroutines started
             CheckIfSpawningComplete(); // This will immediately transition state
             return;
        }

        foreach (WaveSpawnEntry entry in currentWaveDefinition.spawnEntries)
        {
            if (entry.animalDefinition == null)
            {
                 Debug.LogWarning($"[FaunaManager] Wave {CurrentWaveNumber}: Skipping spawn entry '{entry.description}' because AnimalDefinition is null.");
                 continue;
            }
            if (entry.spawnCount <= 0)
            {
                Debug.LogWarning($"[FaunaManager] Wave {CurrentWaveNumber}: Skipping spawn entry '{entry.description}' because Spawn Count is zero or less.");
                continue;
            }
            Coroutine spawnCoroutine = StartCoroutine(SpawnWaveEntryCoroutine(entry));
            activeSpawnCoroutines.Add(spawnCoroutine);
        }
        Debug.Log($"[FaunaManager] Wave {CurrentWaveNumber}: Started {activeSpawnCoroutines.Count} spawn coroutine(s).");

        // Check immediately in case all entries had 0 delay and 0 interval/count
        if (activeSpawnCoroutines.Count == 0)
        {
             CheckIfSpawningComplete();
        }
    }

    /// <summary>
    /// Coroutine to handle spawning a single entry within a wave.
    /// </summary>
    private IEnumerator SpawnWaveEntryCoroutine(WaveSpawnEntry entry)
    {
        // 1. Initial Delay
        if (entry.delayAfterWaveStart > 0)
        {
            yield return new WaitForSeconds(entry.delayAfterWaveStart);
        }

        // 2. Spawn Loop
        for (int i = 0; i < entry.spawnCount; i++)
        {
            // Check if state changed mid-spawn
            if (currentState != WaveManagerState.SpawningWave && currentState != WaveManagerState.WaitingForWaveEnd)
            {
                Debug.Log($"[FaunaManager] Halting spawn '{entry.description}' due to state change ({currentState}).");
                break; // Exit coroutine
            }

            Vector2 spawnPos = CalculateSpawnPosition(entry.spawnLocationType, entry.spawnRadius);
            GameObject spawnedAnimal = SpawnAnimal(entry.animalDefinition, spawnPos);

            if (spawnedAnimal != null)
            {
                spawnedWaveAnimals.Add(spawnedAnimal);
            }

            // 3. Interval Delay
            if (entry.spawnInterval > 0 && i < entry.spawnCount - 1)
            {
                yield return new WaitForSeconds(entry.spawnInterval);
            }
        }

        // 4. Coroutine Complete - Remove self from tracking list
        // Coroutine thisCoroutine = null; // <<< REMOVED THIS LINE
        // [...] (Rest of the comments explaining the difficulty remain)
        // Simplistic removal:
        if (activeSpawnCoroutines.Count > 0) activeSpawnCoroutines.RemoveAt(0);

        Debug.Log($"[FaunaManager] Spawn coroutine finished for entry: {entry.description}. Remaining active: {activeSpawnCoroutines.Count}");
    }

    /// <summary>
    /// Called when the current wave's end condition is met.
    /// </summary>
    private void EndCurrentWave()
    {
        Debug.Log($"[FaunaManager] Wave {CurrentWaveNumber} ended.");
        currentState = WaveManagerState.BetweenWaves;

        // Stop any remaining spawn coroutines for this wave (important for Timer condition)
        foreach (Coroutine co in activeSpawnCoroutines)
        {
            if (co != null) StopCoroutine(co);
        }
        activeSpawnCoroutines.Clear();

        // Set the timer for the break *after* the wave
        if (currentWaveDefinition != null && currentWaveDefinition.delayBeforeNextWave > 0)
        {
            betweenWaveTimer = currentWaveDefinition.delayBeforeNextWave;
            Debug.Log($"[FaunaManager] Starting between-wave delay: {betweenWaveTimer}s");
        }
        else
        {
            // If no delay, maybe transition straight to Idle or start next immediately?
            // For now, just finish the delay instantly.
            betweenWaveTimer = 0f;
             UpdateBetweenWavesTimer(); // Process the zero delay immediately
        }

        // Clear definition reference for safety
        currentWaveDefinition = null;

        // Optional: Clear remaining animals if wave ended by timer? (Gameplay decision)
        // if (currentWaveDefinition != null && currentWaveDefinition.endCondition == WaveEndCondition.Timer) {
        //     foreach(var animal in spawnedWaveAnimals) { if (animal != null) Destroy(animal); }
        //     spawnedWaveAnimals.Clear();
        // }

        // Fire event: OnWaveComplete?.Invoke(CurrentWaveNumber);
    }


    /// <summary>
    /// Calculates a spawn position based on the specified type.
    /// </summary>
    private Vector2 CalculateSpawnPosition(WaveSpawnLocationType locationType, float radius)
    {
        switch (locationType)
        {
            case WaveSpawnLocationType.RandomNearPlayer:
                if (playerTransform != null)
                {
                    // Calculate position near player and return immediately
                    return (Vector2)playerTransform.position + Random.insideUnitCircle * radius;
                }
                else
                {
                    // Log warning and *explicitly calculate fallback position*
                    Debug.LogWarning("[FaunaManager] Cannot spawn near player: Player Transform is null. Falling back to global spawn area.");
                    // Calculate global spawn position *within this case*
                    float fallbackX = spawnCenter.x + Random.Range(-spawnAreaSize.x / 2f, spawnAreaSize.x / 2f);
                    float fallbackY = spawnCenter.y + Random.Range(-spawnAreaSize.y / 2f, spawnAreaSize.y / 2f);
                    // Return the fallback position
                    return new Vector2(fallbackX, fallbackY);
                }
            // NOTE: No 'break' needed here because we used 'return' in both paths.

            case WaveSpawnLocationType.GlobalSpawnArea:
            default:
                // Calculate global spawn position
                float spawnX = spawnCenter.x + Random.Range(-spawnAreaSize.x / 2f, spawnAreaSize.x / 2f);
                float spawnY = spawnCenter.y + Random.Range(-spawnAreaSize.y / 2f, spawnAreaSize.y / 2f);
                // Return the calculated position
                return new Vector2(spawnX, spawnY);
            // NOTE: No 'break' needed here because 'return' exits the method.
        }
        // The compiler should now recognize that all paths within the switch return a value.
    }

    /// <summary>
    /// Instantiates and initializes an animal. (Modified slightly from original)
    /// </summary>
    private GameObject SpawnAnimal(AnimalDefinition definition, Vector2 position)
    {
        if (definition == null || definition.prefab == null)
        {
            Debug.LogError("[FaunaManager] Invalid animal definition or missing prefab! Cannot spawn.");
            return null;
        }

        // Clamp position to global bounds BEFORE instantiation
        position.x = Mathf.Clamp(position.x, animalMinBounds.x, animalMaxBounds.x);
        position.y = Mathf.Clamp(position.y, animalMinBounds.y, animalMaxBounds.y);


        GameObject animalObj = Instantiate(definition.prefab, position, Quaternion.identity);

        // --- Parenting Logic (copied from original, seems okay) ---
        if (ecosystemParent != null)
        {
            Transform speciesParent = ecosystemParent;
            if (!string.IsNullOrEmpty(definition.animalName)) // Ensure species name exists
            {
                // Try find existing parent for this species
                speciesParent = ecosystemParent.Find(definition.animalName);
                if (speciesParent == null)
                {
                    // Create parent if it doesn't exist
                    GameObject subParent = new GameObject(definition.animalName);
                    subParent.transform.SetParent(ecosystemParent);
                    speciesParent = subParent.transform;
                }
            }
            animalObj.transform.SetParent(speciesParent);
        }
        // --------------------------------------------------------


        // Get and Initialize the AnimalController
        AnimalController controller = animalObj.GetComponent<AnimalController>();
        if (controller != null)
        {
            controller.Initialize(definition, animalMinBounds, animalMaxBounds); // Pass bounds
        }
        else
        {
            Debug.LogError($"[FaunaManager] Animal prefab '{definition.prefab.name}' is missing the AnimalController script! Destroying spawned object.", animalObj);
            Destroy(animalObj);
            return null;
        }

        return animalObj;
    }
}