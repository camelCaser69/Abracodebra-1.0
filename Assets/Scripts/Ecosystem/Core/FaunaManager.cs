// FILE: Assets/Scripts/Ecosystem/Core/FaunaManager.cs
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class FaunaManager : MonoBehaviour
{
    // --- Headers and Fields (remain the same) ---
    [Header("Spawning Area (Global)")]
    [SerializeField] private Vector2 spawnCenter = Vector2.zero;
    [SerializeField] private Vector2 spawnAreaSize = new Vector2(20f, 10f);
    [Header("General Settings")]
    [SerializeField] private Transform ecosystemParent;
    [SerializeField] private Vector2 animalMinBounds = new Vector2(-10f, -5f);
    [SerializeField] private Vector2 animalMaxBounds = new Vector2(10f, 5f);
    [Tooltip("How far outside the camera view to spawn 'Offscreen' animals.")]
    [SerializeField] private float offscreenSpawnMargin = 2.0f;

    // --- Runtime State (remain the same) ---
    private List<Coroutine> activeSpawnCoroutines = new List<Coroutine>();
    private Camera mainCamera;

    // --- Start, InitializeManager, Update, ExecuteSpawnWave, SpawnWaveEntryCoroutine, StopAllSpawnCoroutines, SpawnAnimal (remain the same) ---
    void Start() { InitializeManager(); }
    void InitializeManager() { activeSpawnCoroutines.Clear(); if (WaveManager.Instance != null) { mainCamera = WaveManager.Instance.GetMainCamera(); } if (mainCamera == null) { mainCamera = Camera.main; if (mainCamera == null) Debug.LogError("[FaunaManager] Cannot find Main Camera!", this); } if (ecosystemParent == null) { ecosystemParent = transform; Debug.LogWarning("[FaunaManager] Ecosystem Parent assigned to self.", this); } }
    void Update() { /* Currently empty */ }
    public void ExecuteSpawnWave(WaveDefinition waveDef) { if (waveDef == null) { Debug.LogError("[FaunaManager] ExecuteSpawnWave called with null WaveDefinition!", this); return; } if (waveDef.spawnEntries == null || waveDef.spawnEntries.Count == 0) { Debug.LogWarning($"[FaunaManager] ExecuteSpawnWave called for Wave '{waveDef.waveName}' which has no spawn entries.", this); return; } Debug.Log($"[FaunaManager] Executing spawn for Wave: '{waveDef.waveName}'"); foreach (WaveSpawnEntry entry in waveDef.spawnEntries) { if (entry.animalDefinition == null) { Debug.LogWarning($"[FaunaManager] Wave '{waveDef.waveName}': Skipping entry '{entry.description}', AnimalDefinition null."); continue; } if (entry.spawnCount <= 0) { Debug.LogWarning($"[FaunaManager] Wave '{waveDef.waveName}': Skipping entry '{entry.description}', Spawn Count <= 0."); continue; } WaveSpawnEntry currentEntry = entry; Coroutine spawnCoroutine = StartCoroutine(SpawnWaveEntryCoroutine(currentEntry)); activeSpawnCoroutines.Add(spawnCoroutine); } Debug.Log($"[FaunaManager] Started {activeSpawnCoroutines.Count} spawn coroutine(s) for Wave '{waveDef.waveName}'."); }
    private IEnumerator SpawnWaveEntryCoroutine(WaveSpawnEntry entry) { if (entry.delayAfterWaveStart > 0) { yield return new WaitForSeconds(entry.delayAfterWaveStart); } for (int i = 0; i < entry.spawnCount; i++) { Vector2 spawnPos = CalculateSpawnPosition(entry.spawnLocationType, entry.spawnRadius); GameObject spawnedAnimal = SpawnAnimal(entry.animalDefinition, spawnPos); if (entry.spawnInterval > 0 && i < entry.spawnCount - 1) { yield return new WaitForSeconds(entry.spawnInterval); } } if (activeSpawnCoroutines.Count > 0) activeSpawnCoroutines.RemoveAt(0); }
    public void StopAllSpawnCoroutines() { if (activeSpawnCoroutines.Count > 0) { Debug.Log("[FaunaManager] Stopping all active spawn coroutines."); foreach (Coroutine co in activeSpawnCoroutines) { if (co != null) StopCoroutine(co); } activeSpawnCoroutines.Clear(); } }
    private GameObject SpawnAnimal(AnimalDefinition definition, Vector2 position) { if (definition == null || definition.prefab == null) { Debug.LogError("[FaunaManager] Invalid animal definition or prefab! Cannot spawn."); return null; } position.x = Mathf.Clamp(position.x, animalMinBounds.x, animalMaxBounds.x); position.y = Mathf.Clamp(position.y, animalMinBounds.y, animalMaxBounds.y); GameObject animalObj = Instantiate(definition.prefab, position, Quaternion.identity); if (ecosystemParent != null) { Transform speciesParent = ecosystemParent; if (!string.IsNullOrEmpty(definition.animalName)) { speciesParent = ecosystemParent.Find(definition.animalName); if (speciesParent == null) { GameObject subParent = new GameObject(definition.animalName); subParent.transform.SetParent(ecosystemParent); speciesParent = subParent.transform; } } animalObj.transform.SetParent(speciesParent); } AnimalController controller = animalObj.GetComponent<AnimalController>(); if (controller != null) { controller.Initialize(definition, animalMinBounds, animalMaxBounds); } else { Debug.LogError($"[FaunaManager] Prefab '{definition.prefab.name}' missing AnimalController! Destroying.", animalObj); Destroy(animalObj); return null; } return animalObj; }
    // -------------------------------------------------------------------

    /// <summary>
    /// Calculates a spawn position based on the specified type, including Offscreen.
    /// </summary>
    private Vector2 CalculateSpawnPosition(WaveSpawnLocationType locationType, float radius)
    {
        if (mainCamera == null)
        {
            Debug.LogError("[FaunaManager] Cannot CalculateSpawnPosition: Main Camera reference is missing!");
            return spawnCenter; // Fallback to center
        }

        switch (locationType)
        {
            case WaveSpawnLocationType.Offscreen:
                // (Offscreen logic remains the same)
                float camHeight = mainCamera.orthographicSize * 2f;
                float camWidth = camHeight * mainCamera.aspect;
                Vector2 camPos = mainCamera.transform.position;
                float minX = camPos.x - camWidth / 2f - offscreenSpawnMargin;
                float maxX = camPos.x + camWidth / 2f + offscreenSpawnMargin;
                float minY = camPos.y - camHeight / 2f - offscreenSpawnMargin;
                float maxY = camPos.y + camHeight / 2f + offscreenSpawnMargin;
                int edge = Random.Range(0, 4);
                float spawnX, spawnY;
                if (edge == 0) { spawnX = minX; spawnY = Random.Range(minY, maxY); }
                else if (edge == 1) { spawnX = maxX; spawnY = Random.Range(minY, maxY); }
                else if (edge == 2) { spawnX = Random.Range(minX, maxX); spawnY = minY; }
                else { spawnX = Random.Range(minX, maxX); spawnY = maxY; }
                spawnX = Mathf.Clamp(spawnX, animalMinBounds.x, animalMaxBounds.x);
                spawnY = Mathf.Clamp(spawnY, animalMinBounds.y, animalMaxBounds.y);
                return new Vector2(spawnX, spawnY);

            case WaveSpawnLocationType.RandomNearPlayer:
                 // Get player position ONLY via WaveManager or Tag lookup
                 Transform playerT = null;
                 if (WaveManager.Instance != null)
                 {
                      // Example: Assume WaveManager has a public property for player transform
                      // If not, you might need to adjust how WaveManager provides this.
                      // For now, let's assume a simple FindFirstObjectByType fallback or tag lookup
                      // if WaveManager doesn't directly expose it.
                      var playerInteractor = FindFirstObjectByType<PlayerTileInteractor>(); // Find player component
                      if (playerInteractor != null) playerT = playerInteractor.transform;
                 }
                 // Fallback if WaveManager didn't provide or doesn't exist
                 if (playerT == null) {
                     GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
                     if (playerObj != null) playerT = playerObj.transform;
                 }


                 if (playerT != null) {
                    return (Vector2)playerT.position + Random.insideUnitCircle * radius;
                 } else {
                     Debug.LogWarning("[FaunaManager] Cannot spawn near player: Player Transform could not be determined. Falling back to global spawn area.");
                     // Fallthrough to global (handled by default)
                 }
                 goto case WaveSpawnLocationType.GlobalSpawnArea; // Use goto for explicit fallthrough


            case WaveSpawnLocationType.GlobalSpawnArea:
            default:
                float globalX = spawnCenter.x + Random.Range(-spawnAreaSize.x / 2f, spawnAreaSize.x / 2f);
                float globalY = spawnCenter.y + Random.Range(-spawnAreaSize.y / 2f, spawnAreaSize.y / 2f);
                // Clamp to global movement bounds
                globalX = Mathf.Clamp(globalX, animalMinBounds.x, animalMaxBounds.x);
                globalY = Mathf.Clamp(globalY, animalMinBounds.y, animalMaxBounds.y);
                return new Vector2(globalX, globalY);
        }
    }
}