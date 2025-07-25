﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class FaunaManager : MonoBehaviour
{
    [Header("Spawning Area (Global)")]
    [SerializeField] private Vector2 spawnCenter = Vector2.zero;
    [SerializeField] private Vector2 spawnAreaSize = new Vector2(20f, 10f);

    [Header("General Settings")]
    [SerializeField] private Transform ecosystemParent;
    [Tooltip("How far INSIDE the screen edge the effective animal movement bounds are.")]
    [SerializeField][Min(0f)] private float screenBoundsPadding = 0.5f;
    [Tooltip("How far OUTSIDE the screen edge the 'Offscreen' spawn area starts.")]
    [SerializeField][Min(0f)] private float offscreenSpawnMargin = 2.0f;

    [Header("Debugging")]
    [Tooltip("Show gizmos visualizing the Margin (Red) and Padding (Green) bounds.")]
    [SerializeField] private bool showBoundsGizmos = false;

    [Header("Functional Bounds Offset")]
    [Tooltip("Functional horizontal shift for gameplay bounds and spawning relative to camera view.")]
    [SerializeField][Range(-10f, 10f)] private float boundsOffsetX = 0f;
    [Tooltip("Functional vertical shift for gameplay bounds and spawning relative to camera view.")]
    [SerializeField][Range(-10f, 10f)] private float boundsOffsetY = 0f;

    private List<Coroutine> activeSpawnCoroutines = new List<Coroutine>();
    private Camera mainCamera; // Ensure this is assigned or found

    void Start()
    {
        InitializeManager();
    }

    void InitializeManager()
    {
        activeSpawnCoroutines.Clear();
        // Attempt to get camera from WaveManager first, then fallback
        if (WaveManager.Instance != null) mainCamera = WaveManager.Instance.GetMainCamera();
        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera == null) Debug.LogError("[FaunaManager] Cannot find Main Camera!", this);

        if (ecosystemParent == null)
        {
            if (EcosystemManager.Instance != null && EcosystemManager.Instance.animalParent != null)
            {
                ecosystemParent = EcosystemManager.Instance.animalParent;
                Debug.LogWarning("[FaunaManager] Ecosystem Parent assigned from EcosystemManager.animalParent.", this);
            }
            else
            {
                ecosystemParent = transform;
                Debug.LogWarning("[FaunaManager] Ecosystem Parent assigned to self as fallback.", this);
            }
        }
    }

    public void ExecuteSpawnWave(WaveDefinition waveDef)
    {
        if (waveDef == null) { Debug.LogError("[FaunaManager] ExecuteSpawnWave called with null WaveDefinition!", this); return; }
        if (waveDef.spawnEntries == null || waveDef.spawnEntries.Count == 0) { Debug.LogWarning($"[FaunaManager] Wave '{waveDef.waveName}' has no spawn entries.", this); return; }

        Debug.Log($"[FaunaManager] Executing spawn for Wave: '{waveDef.waveName}'");
        // Stop any previously running spawn coroutines for this FaunaManager instance
        StopAllSpawnCoroutines(); // Ensures only one waveDef's entries are spawning at a time from this manager

        foreach (WaveSpawnEntry entry in waveDef.spawnEntries)
        {
            if (entry.animalDefinition == null) { Debug.LogWarning($"[FaunaManager] Skipping entry '{entry.description}', null AnimalDefinition for wave '{waveDef.waveName}'."); continue; }
            if (entry.spawnCount <= 0) { Debug.LogWarning($"[FaunaManager] Skipping entry '{entry.description}', Spawn Count <= 0 for wave '{waveDef.waveName}'."); continue; }

            // Check RunManager state before starting new coroutines
            if (RunManager.Instance != null && RunManager.Instance.CurrentState != RunState.GrowthAndThreat)
            {
                Debug.Log($"[FaunaManager] Halting further spawn entry processing for wave '{waveDef.waveName}', RunManager not in GrowthAndThreat state.");
                break; // Don't start new coroutines if not in the correct game state
            }

            WaveSpawnEntry currentEntry = entry; // Closure capture
            Coroutine spawnCoroutine = StartCoroutine(SpawnWaveEntryCoroutine(currentEntry, waveDef.waveName));
            activeSpawnCoroutines.Add(spawnCoroutine);
        }
        Debug.Log($"[FaunaManager] Started {activeSpawnCoroutines.Count} spawn entry coroutine(s) for '{waveDef.waveName}'.");
    }

    public void StopAllSpawnCoroutines()
    {
        if (activeSpawnCoroutines.Count > 0)
        {
            Debug.Log($"[FaunaManager] Stopping all ({activeSpawnCoroutines.Count}) active spawn coroutines.");
            foreach (Coroutine co in activeSpawnCoroutines)
            {
                if (co != null) StopCoroutine(co);
            }
            activeSpawnCoroutines.Clear();
        }
    }

    private IEnumerator SpawnWaveEntryCoroutine(WaveSpawnEntry entry, string waveNameForDebug)
    {
        if (entry.delayAfterSpawnTime > 0)
        {
            yield return new WaitForSeconds(entry.delayAfterSpawnTime);
        }

        for (int i = 0; i < entry.spawnCount; i++)
        {
            // Check RunManager state before each spawn
            if (RunManager.Instance != null && RunManager.Instance.CurrentState != RunState.GrowthAndThreat)
            {
                Debug.Log($"[FaunaManager] Halting spawn for entry '{entry.description}' in wave '{waveNameForDebug}', RunManager no longer in GrowthAndThreat state.");
                break; // Exit loop if game state changed
            }

            // WaveManager.IsRunActive is now less reliable here, use RunManager state.
            // if (WaveManager.Instance != null && !WaveManager.Instance.IsRunActive)
            // {
            //     Debug.Log($"[FaunaManager] Halting spawn '{entry.description}', run no longer active.");
            //     break;
            // }

            Vector2 spawnPos = CalculateSpawnPosition(entry.spawnLocationType, entry.spawnRadius);
            bool isOffscreen = entry.spawnLocationType == WaveSpawnLocationType.Offscreen;
            GameObject spawnedAnimal = SpawnAnimal(entry.animalDefinition, spawnPos, isOffscreen);

            if (entry.spawnInterval > 0 && i < entry.spawnCount - 1) // Don't wait after the last one
            {
                yield return new WaitForSeconds(entry.spawnInterval);
            }
        }
        // Coroutine completes, remove itself from the list if it was added
        // This removal is a bit tricky if coroutines are stopped externally.
        // A more robust way is to check `activeSpawnCoroutines.Remove(thisCoroutineInstance)`
        // but `thisCoroutineInstance` is not directly available here.
        // For simplicity, we'll rely on StopAllSpawnCoroutines to clear the list.
        // Or, pass the coroutine itself into this method and remove it.
        // For now, let's clear it more carefully in StopAll and on completion.
    }


    private Vector2 CalculateSpawnPosition(WaveSpawnLocationType locationType, float radius)
    {
        if (mainCamera == null) { Debug.LogError("[FaunaManager] Missing Main Camera for CalculateSpawnPosition!"); return spawnCenter; }

        Vector2 functionalOffset = new Vector2(boundsOffsetX, boundsOffsetY);
        Vector2 effectiveCamPos = (Vector2)mainCamera.transform.position + functionalOffset;

        Vector2 spawnPos = Vector2.zero;
        float camHeight = mainCamera.orthographicSize * 2f;
        float camWidth = camHeight * mainCamera.aspect;

        switch (locationType)
        {
            case WaveSpawnLocationType.Offscreen:
                float marginMinX = effectiveCamPos.x - camWidth / 2f - offscreenSpawnMargin;
                float marginMaxX = effectiveCamPos.x + camWidth / 2f + offscreenSpawnMargin;
                float marginMinY = effectiveCamPos.y - camHeight / 2f - offscreenSpawnMargin;
                float marginMaxY = effectiveCamPos.y + camHeight / 2f + offscreenSpawnMargin;
                float extraOffset = 0.1f;
                int edge = Random.Range(0, 4);
                if (edge == 0) { spawnPos.x = marginMinX - extraOffset; spawnPos.y = Random.Range(marginMinY, marginMaxY); }
                else if (edge == 1) { spawnPos.x = marginMaxX + extraOffset; spawnPos.y = Random.Range(marginMinY, marginMaxY); }
                else if (edge == 2) { spawnPos.x = Random.Range(marginMinX, marginMaxX); spawnPos.y = marginMinY - extraOffset; }
                else { spawnPos.x = Random.Range(marginMinX, marginMaxX); spawnPos.y = marginMaxY + extraOffset; }
                break;

            case WaveSpawnLocationType.RandomNearPlayer:
                 Transform playerT = FindPlayerTransform();
                 if (playerT != null) {
                    spawnPos = (Vector2)playerT.position + Random.insideUnitCircle * radius;
                 } else {
                     Debug.LogWarning("[FaunaManager] Player not found for RandomNearPlayer. Falling back to Global.");
                     goto case WaveSpawnLocationType.GlobalSpawnArea;
                 }
                 break;

            case WaveSpawnLocationType.GlobalSpawnArea:
            default:
                spawnPos.x = effectiveCamPos.x + Random.Range(-spawnAreaSize.x / 2f, spawnAreaSize.x / 2f);
                spawnPos.y = effectiveCamPos.y + Random.Range(-spawnAreaSize.y / 2f, spawnAreaSize.y / 2f);
                break;
        }
        return spawnPos;
    }

    private Transform FindPlayerTransform()
    {
        // This can be simplified if player is consistently tagged or has a singleton reference
        GameObject playerGO = GameObject.FindGameObjectWithTag("Player");
        if (playerGO != null) return playerGO.transform;

        // Fallback or more specific searches
        PlayerTileInteractor pti = FindAnyObjectByType<PlayerTileInteractor>();
        if (pti != null) return pti.transform;

        GardenerController gc = FindAnyObjectByType<GardenerController>();
        if (gc != null) return gc.transform;

        return null;
    }

    // Add this method to FaunaManager.cs to replace the existing SpawnAnimal method

GameObject SpawnAnimal(AnimalDefinition definition, Vector2 position, bool isOffscreenSpawn)
{
    if (definition == null || definition.prefab == null)
    {
        Debug.LogError("[FaunaManager] Cannot spawn animal: null definition or prefab.");
        return null;
    }
    
    if (mainCamera == null)
    {
        Debug.LogError("[FaunaManager] Missing Main Camera for SpawnAnimal bounds calculation!");
        return null;
    }
    
    Vector2 functionalOffset = new Vector2(boundsOffsetX, boundsOffsetY);
    Vector2 effectiveCamPos = (Vector2)mainCamera.transform.position + functionalOffset;
    
    Vector2 minPaddedBounds, maxPaddedBounds;
    float camHeight = mainCamera.orthographicSize * 2f;
    float camWidth = camHeight * mainCamera.aspect;
    
    minPaddedBounds.x = effectiveCamPos.x - camWidth / 2f + screenBoundsPadding;
    maxPaddedBounds.x = effectiveCamPos.x + camWidth / 2f - screenBoundsPadding;
    minPaddedBounds.y = effectiveCamPos.y - camHeight / 2f + screenBoundsPadding;
    maxPaddedBounds.y = effectiveCamPos.y + camHeight / 2f - screenBoundsPadding;
    
    GameObject animalObj = Instantiate(definition.prefab, position, Quaternion.identity);
    
    if (ecosystemParent != null)
    {
        Transform speciesParent = ecosystemParent;
        if (EcosystemManager.Instance != null && EcosystemManager.Instance.sortAnimalsBySpecies && !string.IsNullOrEmpty(definition.animalName))
        {
            speciesParent = ecosystemParent.Find(definition.animalName);
            if (speciesParent == null)
            {
                GameObject subParentGO = new GameObject(definition.animalName);
                subParentGO.transform.SetParent(ecosystemParent);
                speciesParent = subParentGO.transform;
            }
        }
        animalObj.transform.SetParent(speciesParent);
    }
    
    AnimalController controller = animalObj.GetComponent<AnimalController>();
    if (controller != null)
    {
        // The new AnimalController doesn't need Initialize, but we can set up screen seeking if needed
        if (isOffscreenSpawn)
        {
            Vector2 screenCenter = (minPaddedBounds + maxPaddedBounds) / 2f;
            controller.SetSeekingScreenCenter(screenCenter, minPaddedBounds, maxPaddedBounds);
        }
    }
    else
    {
        Debug.LogError($"[FaunaManager] Spawned animal prefab '{definition.prefab.name}' missing AnimalController script!", animalObj);
    }
    
    return animalObj;
}

    void OnDrawGizmos()
    {
        if (!showBoundsGizmos || mainCamera == null) return;
        Vector2 functionalOffset = new Vector2(boundsOffsetX, boundsOffsetY);
        Vector2 effectiveCamPos = (Vector2)mainCamera.transform.position + functionalOffset;
        float camHeight = mainCamera.orthographicSize * 2f;
        float camWidth = camHeight * mainCamera.aspect;
        Vector2 paddedMin = new Vector2(effectiveCamPos.x - camWidth / 2f + screenBoundsPadding, effectiveCamPos.y - camHeight / 2f + screenBoundsPadding);
        Vector2 paddedMax = new Vector2(effectiveCamPos.x + camWidth / 2f - screenBoundsPadding, effectiveCamPos.y + camHeight / 2f - screenBoundsPadding);
        DrawWireRectangleGizmo(paddedMin, paddedMax, Color.green);
        Vector2 marginMin = new Vector2(effectiveCamPos.x - camWidth / 2f - offscreenSpawnMargin, effectiveCamPos.y - camHeight / 2f - offscreenSpawnMargin);
        Vector2 marginMax = new Vector2(effectiveCamPos.x + camWidth / 2f + offscreenSpawnMargin, effectiveCamPos.y + camHeight / 2f - offscreenSpawnMargin);
        DrawWireRectangleGizmo(marginMin, marginMax, Color.red);
    }

    void DrawWireRectangleGizmo(Vector2 min, Vector2 max, Color color) { Gizmos.color = color; Gizmos.DrawLine(new Vector3(min.x, min.y, 0), new Vector3(max.x, min.y, 0)); Gizmos.DrawLine(new Vector3(max.x, min.y, 0), new Vector3(max.x, max.y, 0)); Gizmos.DrawLine(new Vector3(max.x, max.y, 0), new Vector3(min.x, max.y, 0)); Gizmos.DrawLine(new Vector3(min.x, max.y, 0), new Vector3(min.x, min.y, 0)); }
}