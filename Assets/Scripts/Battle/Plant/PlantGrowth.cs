// FILE: Assets/Scripts/Battle/Plant/PlantGrowth.cs
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;

// --- Enums ---
public enum PlantState { Initializing, Growing, GrowthComplete, Mature_Idle, Mature_Executing }

public partial class PlantGrowth : MonoBehaviour
{
    // ------------------------------------------------
    // --- SERIALIZED FIELDS ---
    // ------------------------------------------------

    [Header("UI & Visuals")]
    [SerializeField] private TMP_Text energyText;
    [SerializeField] private GameObject seedCellPrefab;
    [SerializeField] private GameObject stemCellPrefab;
    [SerializeField] private GameObject leafCellPrefab;
    [SerializeField] private GameObject berryCellPrefab; // Used for PlantCellType.Fruit
    [SerializeField] private float cellSpacing = 0.08f;

    [Header("Shadow Setup")]
    [SerializeField] [Tooltip("Assign the PlantShadowController component from the child _ShadowRoot GameObject")]
    private PlantShadowController shadowController;
    [SerializeField] [Tooltip("Assign your 'PlantShadow' prefab (GO + SpriteRenderer + ShadowPartController script)")]
    private GameObject shadowPartPrefab;

    [Header("Outline Setup")]
    [SerializeField] [Tooltip("Enable or disable plant outline visualization")]
    private bool enableOutline = true;
    [SerializeField] [Tooltip("Assign the PlantOutlineController component from the child _OutlineRoot GameObject")]
    private PlantOutlineController outlineController;
    [SerializeField] [Tooltip("Assign your outline part prefab (GO + SpriteRenderer + OutlinePartController script)")]
    private GameObject outlinePartPrefab;

    [Header("Growth & UI Timing")]
    [SerializeField] private bool showGrowthPercentage = true;
    [SerializeField] private bool allowPhotosynthesisDuringGrowth = false;
    [SerializeField] [Tooltip("Percentage UI updates only on these increments (e.g., 5 shows 0, 5, 10...).")]
    [Range(1, 25)] private int percentageIncrement = 5;
    // --- NEW FIELD ---
    [SerializeField] [Tooltip("If true, percentage display approximates smooth progress based on time. If false, it reflects discrete stem cell additions.")]
    private bool continuousIncrement = false;
    // -----------------

    // ------------------------------------------------
    // --- INTERNAL STATE & DATA ---
    // ------------------------------------------------

    private NodeGraph nodeGraph;
    public PlantState currentState = PlantState.Initializing;
    public float currentEnergy = 0f;
    private Dictionary<Vector2Int, PlantCellType> cells = new Dictionary<Vector2Int, PlantCellType>();
    private List<GameObject> activeCellGameObjects = new List<GameObject>();
    private FireflyManager fireflyManagerInstance;
    private GameObject rootCellInstance;
    private Coroutine growthCoroutine;
    private bool isGrowthCompletionHandled = false;

    // ------------------------------------------------
    // --- CALCULATED STATS ---
    // ------------------------------------------------

    private int targetStemLength;
    private float finalGrowthSpeed; // Represents time interval per step
    private int finalLeafGap;
    private int finalLeafPattern;
    private float finalGrowthRandomness;
    private float finalMaxEnergy;
    private float finalPhotosynthesisRate;
    private float cycleCooldown;
    private float nodeCastDelay;

    // ------------------------------------------------
    // --- RUNTIME VARIABLES ---
    // ------------------------------------------------

    private int currentStemCount = 0;
    private float cycleTimer = 0f;
    private int displayedGrowthPercentage = -1;
    private bool? offsetRightForPattern1 = null;
    // --- NEW FIELDS for Continuous Mode ---
    private float currentGrowthElapsedTime = 0f;
    private float estimatedTotalGrowthTime = 1f; // Default to 1 to avoid division by zero
    // --- NEW FIELDS for better growth tracking ---
    private float actualGrowthProgress = 0f; // The normalized progress value (0-1) representing true growth completion
    private int stepsCompleted = 0; // Track how many steps have been completed
    private int totalPlannedSteps = 0; // Total number of steps in the growth plan

    // ------------------------------------------------
    // --- UNITY LIFECYCLE METHODS ---
    // ------------------------------------------------

    void Awake()
    {
        // --- Critical Setup Check ---
        bool setupValid = true;
        if (shadowController == null) { shadowController = GetComponentInChildren<PlantShadowController>(true); if (shadowController == null) { Debug.LogError($"PlantGrowth ERROR on '{gameObject.name}': PlantShadowController ref missing!", this); setupValid = false; } else { Debug.LogWarning($"PlantGrowth on '{gameObject.name}': Found PlantShadowController dynamically.", this); } }
        if (shadowPartPrefab == null) { Debug.LogError($"PlantGrowth ERROR on '{gameObject.name}': Shadow Part Prefab missing!", this); setupValid = false; }
        if (enableOutline) { if (outlineController == null) { outlineController = GetComponentInChildren<PlantOutlineController>(true); if (outlineController == null) { Debug.LogError($"PlantGrowth ERROR on '{gameObject.name}': PlantOutlineController ref missing but outline is enabled!", this); setupValid = false; } else { Debug.LogWarning($"PlantGrowth on '{gameObject.name}': Found PlantOutlineController dynamically.", this); } } if (outlinePartPrefab == null) { Debug.LogError($"PlantGrowth ERROR on '{gameObject.name}': Outline Part Prefab missing but outline is enabled!", this); setupValid = false; } }
        if (seedCellPrefab == null) { Debug.LogError($"PlantGrowth ERROR on '{gameObject.name}': Seed Cell Prefab missing!", this); setupValid = false; } if (energyText == null) Debug.LogWarning($"PlantGrowth on '{gameObject.name}': Energy Text (TMP_Text) missing.", this);
        if (!setupValid) { enabled = false; return; }
        if (!enableOutline && outlineController != null) { outlineController.gameObject.SetActive(false); }
        fireflyManagerInstance = FireflyManager.Instance;
        EnsureUIReferences();
    }

    void Start()
    {
        UpdateUI();
    }

    private void Update()
    {
        // Handle frame-dependent UI updates and state transitions
        switch (currentState)
        {
            case PlantState.Growing:
                if (allowPhotosynthesisDuringGrowth)
                    AccumulateEnergy();

                // --- Update percentage UI every frame ONLY if in continuous mode ---
                if (showGrowthPercentage && continuousIncrement)
                {
                    UpdateGrowthPercentageUI();
                }
                // (Discrete mode updates are handled within the coroutine)
                break;

            case PlantState.GrowthComplete:
                if (!isGrowthCompletionHandled)
                {
                    isGrowthCompletionHandled = true;
                    if (showGrowthPercentage && targetStemLength > 0)
                    {
                        UpdateGrowthPercentageUI(true); // Force 100% display
                    }
                    currentState = PlantState.Mature_Idle;
                    cycleTimer = cycleCooldown;
                    UpdateUI(); // Update energy text if needed
                }
                break;

            case PlantState.Mature_Idle:
                AccumulateEnergy();
                UpdateUI();
                cycleTimer -= Time.deltaTime;
                if (cycleTimer <= 0f && currentEnergy >= 1f)
                {
                    currentState = PlantState.Mature_Executing;
                    StartCoroutine(ExecuteMatureCycle()); // Assumes ExecuteMatureCycle is in another partial file
                }
                break;

            case PlantState.Mature_Executing:
                AccumulateEnergy();
                UpdateUI();
                break;

            case PlantState.Initializing:
                break;
        }
    }

    private void OnDestroy()
    {
        StopAllCoroutines(); growthCoroutine = null;
        if (PlantGrowthModifierManager.Instance != null) { PlantGrowthModifierManager.Instance.UnregisterPlant(this); }
        ClearAllVisuals(); // Assumes ClearAllVisuals is in another partial file
    }

    // ------------------------------------------------
    // --- PUBLIC INITIALIZATION ---
    // ------------------------------------------------

    public void InitializeAndGrow(NodeGraph graph)
    {
        if (graph == null || graph.nodes == null) { Debug.LogError($"[{gameObject.name}] Null/empty NodeGraph provided.", gameObject); Destroy(gameObject); return; }
        if (growthCoroutine != null) { StopCoroutine(growthCoroutine); growthCoroutine = null; }
        ClearAllVisuals();
        rootCellInstance = null; currentStemCount = 0; offsetRightForPattern1 = null; isGrowthCompletionHandled = false; displayedGrowthPercentage = -1;
        currentGrowthElapsedTime = 0f; // <-- RESET
        estimatedTotalGrowthTime = 1f; // <-- RESET to default
        stepsCompleted = 0; // <-- RESET new variable
        totalPlannedSteps = 0; // <-- RESET new variable
        actualGrowthProgress = 0f; // <-- RESET new variable
        nodeGraph = graph; currentState = PlantState.Initializing; currentEnergy = 0f;
        CalculateAndApplyStats();
        GameObject spawnedSeed = SpawnCellVisual(PlantCellType.Seed, Vector2Int.zero, null, null);
        if (spawnedSeed != null) {
            rootCellInstance = spawnedSeed;
            if (PlantGrowthModifierManager.Instance != null && TileInteractionManager.Instance != null) { Vector3Int gridPos = TileInteractionManager.Instance.WorldToCell(transform.position); TileDefinition currentTile = TileInteractionManager.Instance.FindWhichTileDefinitionAt(gridPos); PlantGrowthModifierManager.Instance.RegisterPlantTile(this, currentTile); }
            if (targetStemLength > 0) {
                currentState = PlantState.Growing;
                UpdateGrowthPercentageUI(); // Initial UI update (0%)
                growthCoroutine = StartCoroutine(GrowthCoroutine_TimeBased());
            } else {
                Debug.LogWarning($"[{gameObject.name}] Target stem length is {targetStemLength}. Skipping visual growth phase.", gameObject);
                currentState = PlantState.GrowthComplete; isGrowthCompletionHandled = false; UpdateUI();
            }
        } else { Debug.LogError($"[{gameObject.name}] Failed to spawn initial seed! Aborting growth.", gameObject); currentState = PlantState.Mature_Idle; Destroy(gameObject, 0.1f); }
        UpdateUI();
    }

    // ------------------------------------------------
    // --- ENERGY & UI ---
    // ------------------------------------------------

    private void AccumulateEnergy()
    {
        if (finalPhotosynthesisRate <= 0 || finalMaxEnergy <= 0) return; float sunlight = WeatherManager.Instance ? WeatherManager.Instance.sunIntensity : 1f; int leafCount = cells.Values.Count(c => c == PlantCellType.Leaf); float tileMultiplier = (PlantGrowthModifierManager.Instance != null) ? PlantGrowthModifierManager.Instance.GetEnergyRechargeMultiplier(this) : 1.0f; float fireflyBonusRate = 0f; if (fireflyManagerInstance != null) { int nearbyFlyCount = fireflyManagerInstance.GetNearbyFireflyCount(transform.position, fireflyManagerInstance.photosynthesisRadius); fireflyBonusRate = Mathf.Min(nearbyFlyCount * fireflyManagerInstance.photosynthesisIntensityPerFly, fireflyManagerInstance.maxPhotosynthesisBonus); } float standardPhotosynthesis = finalPhotosynthesisRate * leafCount * sunlight; float totalRate = (standardPhotosynthesis + fireflyBonusRate) * tileMultiplier; float delta = totalRate * Time.deltaTime; currentEnergy = Mathf.Clamp(currentEnergy + delta, 0f, finalMaxEnergy);
    }

    // --- FIXED: UpdateGrowthPercentageUI with Continuous Mode Logic ---
    /// <summary>
    /// Calculates the target display percentage based on chosen mode (discrete/continuous)
    /// and updates the UI text ONLY if the snapped value has changed.
    /// </summary>
    /// <param name="forceComplete">If true, forces the display to 100%.</param>
    private void UpdateGrowthPercentageUI(bool forceComplete = false)
    {
        if (!showGrowthPercentage || energyText == null) return;

        float rawPercentageFloat = 0f; // The calculated percentage before snapping

        if (forceComplete || currentState == PlantState.GrowthComplete)
        {
            rawPercentageFloat = 100f;
        }
        // --- Check Continuous Flag Here ---
        else if (continuousIncrement) // --- CONTINUOUS MODE ---
        {
            // NEW: Use actual growth progress instead of elapsed time
            if (totalPlannedSteps > 0) 
            {
                // Calculate percentage based on steps completed
                rawPercentageFloat = ((float)stepsCompleted / totalPlannedSteps) * 100f;
                
                // Add partial progress toward next step based on actual growth progress
                if (actualGrowthProgress > 0f && stepsCompleted < totalPlannedSteps)
                {
                    float stepSize = 100f / totalPlannedSteps;
                    float partialStepProgress = actualGrowthProgress * stepSize;
                    rawPercentageFloat = (stepsCompleted * stepSize) + partialStepProgress;
                }
            }
            else
            {
                // Fallback if no planned steps (shouldn't happen)
                rawPercentageFloat = (currentState == PlantState.Growing) ? 0f : 100f;
            }
        }
        else // --- DISCRETE MODE ---
        {
            // Calculate based on current stem count vs target
            if (targetStemLength <= 0)
            {
                rawPercentageFloat = 0f; // No stems to grow
            }
            else
            {
                rawPercentageFloat = Mathf.Clamp(((float)currentStemCount / targetStemLength) * 100f, 0f, 100f);
            }
        }

        // --- Snap the calculated percentage (Applies to BOTH modes) ---
        int targetDisplayValue; // The final value to show (snapped)
        if (percentageIncrement <= 1)
        {
            targetDisplayValue = Mathf.FloorToInt(rawPercentageFloat);
        }
        else
        {
            // Use proper rounding to closest increment rather than floor
            targetDisplayValue = Mathf.RoundToInt(rawPercentageFloat / percentageIncrement) * percentageIncrement;
        }
        targetDisplayValue = Mathf.Min(targetDisplayValue, 100); // Clamp final value
        
        // Ensure we don't show 100% until growth is complete, unless forced
        if (targetDisplayValue == 100 && currentState == PlantState.Growing && !forceComplete)
        {
            targetDisplayValue = 95; // Cap at 95% until actually complete
        }

        // --- Update TextMeshPro only if the snapped value changed ---
        if (targetDisplayValue != displayedGrowthPercentage)
        {
            displayedGrowthPercentage = targetDisplayValue;
            energyText.text = $"{displayedGrowthPercentage}%";
        }
    }


    // --- UpdateUI (Consolidated) ---
    private void UpdateUI()
    {
        if (energyText == null) return;

        // If showing percentage AND in a state where it's relevant (Growing or just completed)
        // Let UpdateGrowthPercentageUI handle it (called from Update or completion logic)
        if (showGrowthPercentage && (currentState == PlantState.Growing || currentState == PlantState.GrowthComplete))
        {
           // If not using continuous increment, the discrete update happens in the coroutine.
           // If using continuous, it happens in Update(). If complete, it happens in Update().
           // No need to directly modify text here for percentage display.
        }
        else // Otherwise (Idle, Executing, or not showing percentage), show Energy
        {
            energyText.text = $"{Mathf.FloorToInt(currentEnergy)}/{Mathf.FloorToInt(finalMaxEnergy)}";
        }
    }

    // ------------------------------------------------
    // --- UI REFERENCE HELPER ---
    // ------------------------------------------------

    private void EnsureUIReferences()
    {
        if (energyText) return; energyText = GetComponentInChildren<TMP_Text>(true); if (!energyText) { Debug.LogWarning($"[{gameObject.name}] Energy Text (TMP_Text) UI reference not assigned in Inspector and not found in children.", gameObject); }
    }

    // -----------------------------------------------
    // --- PUBLIC ACCESSORS FOR OUTLINES ---
    // -----------------------------------------------

    public bool DoesCellExistAt(Vector2Int coord) { return cells.ContainsKey(coord); }
    public float GetCellSpacing() { return this.cellSpacing; }
    public GameObject GetCellGameObjectAt(Vector2Int coord) { return activeCellGameObjects.FirstOrDefault(go => go != null && go.GetComponent<PlantCell>()?.GridCoord == coord); }
    public bool IsOutlineEnabled() { return enableOutline; }

    // --- PARTIAL CLASS METHODS (Assumed in other files) ---
    // Define these methods in the corresponding partial class files:
    // In PlantGrowth.Cell.cs: ReportCellDestroyed, RemovePlantCell, ClearAllVisuals, SpawnCellVisual, CalculateAndApplyStats, RegisterShadowForCell
    // In PlantGrowth.Growth.cs: GrowthStep class, GrowthCoroutine_TimeBased, PreCalculateGrowthPlan, GetStemDirection, CalculateLeafPositions
    // In PlantGrowth.NodeExecution.cs: ExecuteMatureCycle, TrySpawnBerry, ApplyScentDataToObject

} // End PARTIAL Class definition