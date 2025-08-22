// FILE: Assets/Scripts/Ecosystem/Management/FaunaManager.cs
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using WegoSystem;

public class FaunaManager : MonoBehaviour
{
    [Header("Spawning Area")]
    [Tooltip("If true, the spawn area will be centered on the middle of the grid. If false, it will use the Custom Spawn Center.")]
    [SerializeField] private bool useMapCenterAsSpawnArea = true;
    [Tooltip("A custom world-space coordinate for the spawn area center. Only used if 'Use Map Center' is false.")]
    [SerializeField] private Vector2 customSpawnCenter = Vector2.zero;
    [SerializeField] private Vector2 spawnAreaSize = new Vector2(20f, 10f);

    [Header("System References")]
    [SerializeField] private Transform ecosystemParent;

    [Header("Screen Spawning Settings")]
    [SerializeField] [Min(0f)] private float screenBoundsPadding = 0.5f;
    [SerializeField] [Min(0f)] private float offscreenSpawnMargin = 2.0f;
    [SerializeField] [Range(-10f, 10f)] private float boundsOffsetX = 0f;
    [SerializeField] [Range(-10f, 10f)] private float boundsOffsetY = 0f;
    
    [Header("Debug")]
    [SerializeField] private bool showBoundsGizmos = false;

    private List<Coroutine> activeSpawnCoroutines = new List<Coroutine>();
    private Camera mainCamera;

    public void Initialize()
    {
        InitializeManager();
    }

    private void InitializeManager()
    {
        activeSpawnCoroutines.Clear();
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

    private Vector2 GetEffectiveSpawnCenter()
    {
        if (useMapCenterAsSpawnArea && GridPositionManager.Instance != null)
        {
            return GridPositionManager.Instance.GetMapCenterWorld();
        }
        return customSpawnCenter;
    }

    public void ExecuteSpawnWave(WaveDefinition waveDef)
    {
        if (waveDef == null) { Debug.LogError("[FaunaManager] ExecuteSpawnWave called with null WaveDefinition!", this); return; }
        if (waveDef.spawnEntries == null || waveDef.spawnEntries.Count == 0) { Debug.LogWarning($"[FaunaManager] Wave '{waveDef.waveName}' has no spawn entries.", this); return; }

        Debug.Log($"[FaunaManager] Executing spawn for Wave: '{waveDef.waveName}'");
        StopAllSpawnCoroutines();

        foreach (WaveSpawnEntry entry in waveDef.spawnEntries)
        {
            if (entry.animalDefinition == null) { Debug.LogWarning($"[FaunaManager] Skipping entry '{entry.description}', null AnimalDefinition for wave '{waveDef.waveName}'."); continue; }
            if (entry.spawnCount <= 0) { Debug.LogWarning($"[FaunaManager] Skipping entry '{entry.description}', Spawn Count <= 0 for wave '{waveDef.waveName}'."); continue; }

            if (RunManager.Instance != null && RunManager.Instance.CurrentState != RunState.GrowthAndThreat)
            {
                Debug.Log($"[FaunaManager] Halting further spawn entry processing for wave '{waveDef.waveName}', RunManager not in GrowthAndThreat state.");
                break;
            }

            WaveSpawnEntry currentEntry = entry;
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
            if (RunManager.Instance != null && RunManager.Instance.CurrentState != RunState.GrowthAndThreat)
            {
                Debug.Log($"[FaunaManager] Halting spawn for entry '{entry.description}' in wave '{waveNameForDebug}', RunManager no longer in GrowthAndThreat state.");
                break;
            }

            Vector2 spawnPos = CalculateSpawnPosition(entry.spawnLocationType, entry.spawnRadius);
            bool isOffscreen = entry.spawnLocationType == WaveSpawnLocationType.Offscreen;
            GameObject spawnedAnimal = SpawnAnimal(entry.animalDefinition, spawnPos, isOffscreen);

            if (entry.spawnInterval > 0 && i < entry.spawnCount - 1)
            {
                yield return new WaitForSeconds(entry.spawnInterval);
            }
        }
    }

    private Vector2 CalculateSpawnPosition(WaveSpawnLocationType locationType, float radius)
    {
        if (mainCamera == null)
        {
            Debug.LogError("[FaunaManager] Missing Main Camera for CalculateSpawnPosition!");
            return GetEffectiveSpawnCenter();
        }

        Vector2 functionalOffset = new Vector2(boundsOffsetX, boundsOffsetY);
        Vector2 effectiveCamPos = (Vector2)mainCamera.transform.position + functionalOffset;

        Vector2 spawnPos;
        float camHeight = mainCamera.orthographicSize * 2f;
        float camWidth = camHeight * mainCamera.aspect;

        switch (locationType)
        {
            case WaveSpawnLocationType.Offscreen:
                float marginMinX = effectiveCamPos.x - camWidth / 2f - offscreenSpawnMargin;
                float marginMaxX = effectiveCamPos.x + camWidth / 2f + offscreenSpawnMargin;
                float marginMinY = effectiveCamPos.y - camHeight / 2f - offscreenSpawnMargin;
                float marginMaxY = effectiveCamPos.y + camHeight / 2f - offscreenSpawnMargin;
                float extraOffset = 0.1f;
                int edge = Random.Range(0, 4);
                if (edge == 0) { spawnPos.x = marginMinX - extraOffset; spawnPos.y = Random.Range(marginMinY, marginMaxY); }
                else if (edge == 1) { spawnPos.x = marginMaxX + extraOffset; spawnPos.y = Random.Range(marginMinY, marginMaxY); }
                else if (edge == 2) { spawnPos.x = Random.Range(marginMinX, marginMaxX); spawnPos.y = marginMinY - extraOffset; }
                else { spawnPos.x = Random.Range(marginMinX, marginMaxX); spawnPos.y = marginMaxY + extraOffset; }
                break;

            case WaveSpawnLocationType.RandomNearPlayer:
                Transform playerT = FindPlayerTransform();
                if (playerT != null)
                {
                    spawnPos = (Vector2)playerT.position + Random.insideUnitCircle * radius;
                }
                else
                {
                    Debug.LogWarning("[FaunaManager] Player not found for RandomNearPlayer. Falling back to Global.");
                    goto case WaveSpawnLocationType.GlobalSpawnArea;
                }
                break;

            case WaveSpawnLocationType.GlobalSpawnArea:
            default:
                Vector2 center = GetEffectiveSpawnCenter();
                spawnPos.x = center.x + Random.Range(-spawnAreaSize.x / 2f, spawnAreaSize.x / 2f);
                spawnPos.y = center.y + Random.Range(-spawnAreaSize.y / 2f, spawnAreaSize.y / 2f);
                break;
        }
        return spawnPos;
    }

    private Transform FindPlayerTransform()
    {
        GameObject playerGO = GameObject.FindGameObjectWithTag("Player");
        if (playerGO != null) return playerGO.transform;

        PlayerTileInteractor pti = FindAnyObjectByType<PlayerTileInteractor>();
        if (pti != null) return pti.transform;

        GardenerController gc = FindAnyObjectByType<GardenerController>();
        if (gc != null) return gc.transform;

        return null;
    }

    private GameObject SpawnAnimal(AnimalDefinition definition, Vector2 position, bool isOffscreenSpawn)
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
        if (!showBoundsGizmos) return;

        // Draw Global Spawn Area
        Vector2 center = GetEffectiveSpawnCenter();
        Gizmos.color = new Color(0f, 0.5f, 1f, 0.5f); // Blue for spawn area
        Gizmos.DrawWireCube(center, spawnAreaSize);

        if (mainCamera == null) return;

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