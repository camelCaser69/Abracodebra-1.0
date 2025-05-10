// FILE: Assets/Scripts/Ecosystem/Core/FaunaManager.cs
using UnityEngine;
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

    [Header("Functional Bounds Offset")] // <<< UPDATED HEADER NAME
    [Tooltip("Functional horizontal shift for gameplay bounds and spawning relative to camera view.")] // <<< UPDATED TOOLTIP
    [SerializeField][Range(-10f, 10f)] private float boundsOffsetX = 0f; // <<< RENAMED FIELD
    [Tooltip("Functional vertical shift for gameplay bounds and spawning relative to camera view.")] // <<< UPDATED TOOLTIP
    [SerializeField][Range(-10f, 10f)] private float boundsOffsetY = 0f; // <<< RENAMED FIELD

    // --- Runtime State ---
    private List<Coroutine> activeSpawnCoroutines = new List<Coroutine>();
    private Camera mainCamera;

    // --- Start, InitializeManager, Update, ExecuteSpawnWave, StopAllSpawnCoroutines, SpawnWaveEntryCoroutine (Unchanged) ---
    void Start() { InitializeManager(); }
    void InitializeManager() { activeSpawnCoroutines.Clear(); if (WaveManager.Instance != null) { mainCamera = WaveManager.Instance.GetMainCamera(); } if (mainCamera == null) { mainCamera = Camera.main; if (mainCamera == null) Debug.LogError("[FaunaManager] Cannot find Main Camera!", this); } if (ecosystemParent == null) { ecosystemParent = transform; Debug.LogWarning("[FaunaManager] Ecosystem Parent assigned to self.", this); } }
    void Update() { /* ... */ }
    public void ExecuteSpawnWave(WaveDefinition waveDef) { if (waveDef == null) { Debug.LogError("[FaunaManager] ExecuteSpawnWave null WaveDefinition!", this); return; } if (waveDef.spawnEntries == null || waveDef.spawnEntries.Count == 0) { Debug.LogWarning($"[FaunaManager] Wave '{waveDef.waveName}' has no spawn entries.", this); return; } Debug.Log($"[FaunaManager] Executing spawn for Wave: '{waveDef.waveName}'"); foreach (WaveSpawnEntry entry in waveDef.spawnEntries) { if (entry.animalDefinition == null) { Debug.LogWarning($"[FaunaManager] Skipping entry '{entry.description}', null AnimalDefinition."); continue; } if (entry.spawnCount <= 0) { Debug.LogWarning($"[FaunaManager] Skipping entry '{entry.description}', Spawn Count <= 0."); continue; } WaveSpawnEntry currentEntry = entry; Coroutine spawnCoroutine = StartCoroutine(SpawnWaveEntryCoroutine(currentEntry)); activeSpawnCoroutines.Add(spawnCoroutine); } Debug.Log($"[FaunaManager] Started {activeSpawnCoroutines.Count} coroutine(s) for '{waveDef.waveName}'."); }
    public void StopAllSpawnCoroutines() { if (activeSpawnCoroutines.Count > 0) { Debug.Log("[FaunaManager] Stopping all spawn coroutines."); foreach (Coroutine co in activeSpawnCoroutines) { if (co != null) StopCoroutine(co); } activeSpawnCoroutines.Clear(); } }
     private IEnumerator SpawnWaveEntryCoroutine(WaveSpawnEntry entry) { if (entry.delayAfterSpawnTime > 0) { yield return new WaitForSeconds(entry.delayAfterSpawnTime); } for (int i = 0; i < entry.spawnCount; i++) { if (WaveManager.Instance != null && !WaveManager.Instance.IsRunActive) { Debug.Log($"[FaunaManager] Halting spawn '{entry.description}', run no longer active."); break; } Vector2 spawnPos = CalculateSpawnPosition(entry.spawnLocationType, entry.spawnRadius); bool isOffscreen = entry.spawnLocationType == WaveSpawnLocationType.Offscreen; GameObject spawnedAnimal = SpawnAnimal(entry.animalDefinition, spawnPos, isOffscreen); if (entry.spawnInterval > 0 && i < entry.spawnCount - 1) { yield return new WaitForSeconds(entry.spawnInterval); } } if (activeSpawnCoroutines.Count > 0) activeSpawnCoroutines.RemoveAt(0); } // Simplistic removal

    /// <summary>
    /// Calculates a spawn position based on the specified type, applying functional offset.
    /// </summary>
    private Vector2 CalculateSpawnPosition(WaveSpawnLocationType locationType, float radius) // <<< MODIFIED
    {
        if (mainCamera == null) { Debug.LogError("[FaunaManager] Missing Main Camera!"); return spawnCenter; }

        // --- Calculate the FUNCTIONAL offset ---
        Vector2 functionalOffset = new Vector2(boundsOffsetX, boundsOffsetY);
        // --- Apply offset to camera position for ALL calculations below ---
        Vector2 effectiveCamPos = (Vector2)mainCamera.transform.position + functionalOffset;

        Vector2 spawnPos = Vector2.zero;
        float camHeight = mainCamera.orthographicSize * 2f;
        float camWidth = camHeight * mainCamera.aspect;

        switch (locationType)
        {
            case WaveSpawnLocationType.Offscreen:
                // Use effectiveCamPos for calculations
                float marginMinX = effectiveCamPos.x - camWidth / 2f - offscreenSpawnMargin;
                float marginMaxX = effectiveCamPos.x + camWidth / 2f + offscreenSpawnMargin;
                float marginMinY = effectiveCamPos.y - camHeight / 2f - offscreenSpawnMargin;
                float marginMaxY = effectiveCamPos.y + camHeight / 2f + offscreenSpawnMargin;
                float extraOffset = 0.1f; // To spawn strictly outside the line
                int edge = Random.Range(0, 4);
                if (edge == 0) { spawnPos.x = marginMinX - extraOffset; spawnPos.y = Random.Range(marginMinY, marginMaxY); }
                else if (edge == 1) { spawnPos.x = marginMaxX + extraOffset; spawnPos.y = Random.Range(marginMinY, marginMaxY); }
                else if (edge == 2) { spawnPos.x = Random.Range(marginMinX, marginMaxX); spawnPos.y = marginMinY - extraOffset; }
                else { spawnPos.x = Random.Range(marginMinX, marginMaxX); spawnPos.y = marginMaxY + extraOffset; }
                break;

            case WaveSpawnLocationType.RandomNearPlayer:
                 Transform playerT = FindPlayerTransform();
                 if (playerT != null) {
                    // Spawn relative to player, still respecting the overall bounds offset implicitly
                    spawnPos = (Vector2)playerT.position + Random.insideUnitCircle * radius;
                 } else {
                     Debug.LogWarning("[FaunaManager] Player not found for RandomNearPlayer. Falling back to Global.");
                     goto case WaveSpawnLocationType.GlobalSpawnArea; // Fallthrough
                 }
                 break;

            case WaveSpawnLocationType.GlobalSpawnArea:
            default:
                // Use effectiveCamPos OR a fixed world space center? Let's stick to camera relative for now.
                // If you want truly fixed global spawn, use spawnCenter directly.
                // This uses the *shifted* camera center as the basis for the global area.
                spawnPos.x = effectiveCamPos.x + Random.Range(-spawnAreaSize.x / 2f, spawnAreaSize.x / 2f);
                spawnPos.y = effectiveCamPos.y + Random.Range(-spawnAreaSize.y / 2f, spawnAreaSize.y / 2f);
                break;
        }
        return spawnPos;
    }

    private Transform FindPlayerTransform()
    {
        Transform pT = null;
        if (WaveManager.Instance != null)
        {
            var pI = FindAnyObjectByType<PlayerTileInteractor>();
            if (pI != null) pT = pI.transform;
        }
        if (pT == null)
        {
            GameObject pO = GameObject.FindGameObjectWithTag("Player");
            if (pO != null) pT = pO.transform;
        }
        return pT;
    }
    
    /// <summary>
    /// Instantiates and initializes an animal, passing SHIFTED screen bounds.
    /// </summary>
    private GameObject SpawnAnimal(AnimalDefinition definition, Vector2 position, bool isOffscreenSpawn) // <<< MODIFIED
    {
        if (definition == null || definition.prefab == null) { /* Error Log */ return null; }
        if (mainCamera == null) { /* Error Log */ return null; }

        // --- Calculate SHIFTED Padded Screen Bounds ---
        Vector2 functionalOffset = new Vector2(boundsOffsetX, boundsOffsetY);
        Vector2 effectiveCamPos = (Vector2)mainCamera.transform.position + functionalOffset;

        Vector2 minPaddedBounds, maxPaddedBounds;
        float camHeight = mainCamera.orthographicSize * 2f;
        float camWidth = camHeight * mainCamera.aspect;

        minPaddedBounds.x = effectiveCamPos.x - camWidth / 2f + screenBoundsPadding;
        maxPaddedBounds.x = effectiveCamPos.x + camWidth / 2f - screenBoundsPadding;
        minPaddedBounds.y = effectiveCamPos.y - camHeight / 2f + screenBoundsPadding;
        maxPaddedBounds.y = effectiveCamPos.y + camHeight / 2f - screenBoundsPadding;
        // ---------------------------------------------

        GameObject animalObj = Instantiate(definition.prefab, position, Quaternion.identity);

        // Parenting (unchanged)
        if (ecosystemParent != null) { Transform sP = ecosystemParent; if (!string.IsNullOrEmpty(definition.animalName)) { sP = ecosystemParent.Find(definition.animalName); if (sP == null) { GameObject subP = new GameObject(definition.animalName); subP.transform.SetParent(ecosystemParent); sP = subP.transform; } } animalObj.transform.SetParent(sP); }

        // Initialize Controller, passing the SHIFTED bounds
        AnimalController controller = animalObj.GetComponent<AnimalController>();
        if (controller != null) {
            controller.Initialize(definition, minPaddedBounds, maxPaddedBounds, isOffscreenSpawn); // Pass shifted bounds
        } else { /* Error Log & Destroy */ Destroy(animalObj); return null; }
        return animalObj;
    }

    /// <summary>
    /// Draws debug rectangles applying the functional offset.
    /// </summary>
    void OnDrawGizmos() // <<< MODIFIED to use offset
    {
        if (!showBoundsGizmos || mainCamera == null) return;

        // --- Apply functional offset for Gizmo drawing ---
        Vector2 functionalOffset = new Vector2(boundsOffsetX, boundsOffsetY);
        Vector2 effectiveCamPos = (Vector2)mainCamera.transform.position + functionalOffset;
        // --------------------------------------------------

        float camHeight = mainCamera.orthographicSize * 2f;
        float camWidth = camHeight * mainCamera.aspect;

        // Calculate corners using effectiveCamPos
        Vector2 paddedMin = new Vector2(effectiveCamPos.x - camWidth / 2f + screenBoundsPadding, effectiveCamPos.y - camHeight / 2f + screenBoundsPadding);
        Vector2 paddedMax = new Vector2(effectiveCamPos.x + camWidth / 2f - screenBoundsPadding, effectiveCamPos.y + camHeight / 2f - screenBoundsPadding);
        DrawWireRectangleGizmo(paddedMin, paddedMax, Color.green);

        Vector2 marginMin = new Vector2(effectiveCamPos.x - camWidth / 2f - offscreenSpawnMargin, effectiveCamPos.y - camHeight / 2f - offscreenSpawnMargin);
        Vector2 marginMax = new Vector2(effectiveCamPos.x + camWidth / 2f + offscreenSpawnMargin, effectiveCamPos.y + camHeight / 2f + offscreenSpawnMargin);
        DrawWireRectangleGizmo(marginMin, marginMax, Color.red);
    }

    void DrawWireRectangleGizmo(Vector2 min, Vector2 max, Color color) { /* Unchanged */ Gizmos.color = color; Gizmos.DrawLine(new Vector3(min.x, min.y, 0), new Vector3(max.x, min.y, 0)); Gizmos.DrawLine(new Vector3(max.x, min.y, 0), new Vector3(max.x, max.y, 0)); Gizmos.DrawLine(new Vector3(max.x, max.y, 0), new Vector3(min.x, max.y, 0)); Gizmos.DrawLine(new Vector3(min.x, max.y, 0), new Vector3(min.x, min.y, 0)); }

} // End of class