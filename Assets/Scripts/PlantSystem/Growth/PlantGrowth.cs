using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;
using WegoSystem;

public enum PlantState { Initializing, Growing, GrowthComplete, Mature_Idle, Mature_Executing }

public partial class PlantGrowth : MonoBehaviour, ITickUpdateable {
    [Header("Wego System")]
    [SerializeField] bool useWegoSystem = true;
    [SerializeField] int growthTicksPerStage = 5;
    [SerializeField] int maturityCycleTicks = 10;

    public static readonly List<PlantGrowth> AllActivePlants = new List<PlantGrowth>();

    [SerializeField] TMP_Text energyText;

    [SerializeField] GameObject seedCellPrefab;
    [SerializeField] GameObject stemCellPrefab;
    [SerializeField] GameObject leafCellPrefab;
    [SerializeField] GameObject berryCellPrefab; // Used for PlantCellType.Fruit
    [SerializeField] float cellSpacing = 0.08f;

    [SerializeField] PlantShadowController shadowController;
    [SerializeField] GameObject shadowPartPrefab;

    [SerializeField] bool enableOutline = true;
    [SerializeField] PlantOutlineController outlineController;
    [SerializeField] GameObject outlinePartPrefab;

    [SerializeField] bool showGrowthPercentage = true;
    [SerializeField] bool allowPhotosynthesisDuringGrowth = false;
    [SerializeField, Range(1, 25)] int percentageIncrement = 5;
    [SerializeField] bool continuousIncrement = false;

    NodeGraph nodeGraph;
    public PlantState currentState = PlantState.Initializing;
    public float currentEnergy = 0f;
    Dictionary<Vector2Int, PlantCellType> cells = new Dictionary<Vector2Int, PlantCellType>();
    List<GameObject> activeCellGameObjects = new List<GameObject>();
    FireflyManager fireflyManagerInstance;
    GameObject rootCellInstance;
    
    // Wego system variables
    int currentGrowthTick = 0;
    int maturityCycleTick = 0;
    int currentStemStage = 0;
    int leafSpawnTick = 0;
    
    // Real-time fallback
    Coroutine growthCoroutine;
    bool isGrowthCompletionHandled = false;

    float poopDetectionRadius = 0f;
    float poopEnergyBonus = 0f;
    List<LeafData> leafDataList = new List<LeafData>();

    int targetStemLength;
    float finalGrowthSpeed;
    int finalLeafGap;
    int finalLeafPattern;
    float finalGrowthRandomness;
    float finalMaxEnergy;
    float finalPhotosynthesisRate;
    float cycleCooldown;
    float nodeCastDelay;

    int currentStemCount = 0;
    float cycleTimer = 0f;
    int displayedGrowthPercentage = -1;
    bool? offsetRightForPattern1 = null;
    float currentGrowthElapsedTime = 0f;
    float estimatedTotalGrowthTime = 1f;
    float actualGrowthProgress = 0f;
    int stepsCompleted = 0;
    int totalPlannedSteps = 0;

    void Awake() {
        bool setupValid = true;
        if (shadowController == null) { 
            shadowController = GetComponentInChildren<PlantShadowController>(true); 
            if (shadowController == null) { 
                Debug.LogError($"PlantGrowth ERROR on '{gameObject.name}': PlantShadowController ref missing!", this); 
                setupValid = false; 
            } else { 
                Debug.LogWarning($"PlantGrowth on '{gameObject.name}': Found PlantShadowController dynamically.", this); 
            } 
        }
        
        if (shadowPartPrefab == null) { 
            Debug.LogError($"PlantGrowth ERROR on '{gameObject.name}': Shadow Part Prefab missing!", this); 
            setupValid = false; 
        }
        
        if (enableOutline) { 
            if (outlineController == null) { 
                outlineController = GetComponentInChildren<PlantOutlineController>(true); 
                if (outlineController == null) { 
                    Debug.LogError($"PlantGrowth ERROR on '{gameObject.name}': PlantOutlineController ref missing but outline is enabled!", this); 
                    setupValid = false; 
                } else { 
                    Debug.LogWarning($"PlantGrowth on '{gameObject.name}': Found PlantOutlineController dynamically.", this); 
                } 
            } 
            if (outlinePartPrefab == null) { 
                Debug.LogError($"PlantGrowth ERROR on '{gameObject.name}': Outline Part Prefab missing but outline is enabled!", this); 
                setupValid = false; 
            } 
        }
        
        if (seedCellPrefab == null) { 
            Debug.LogError($"PlantGrowth ERROR on '{gameObject.name}': Seed Cell Prefab missing!", this); 
            setupValid = false; 
        } 
        
        if (energyText == null) 
            Debug.LogWarning($"PlantGrowth on '{gameObject.name}': Energy Text (TMP_Text) missing.", this);
        
        if (!setupValid) { 
            enabled = false; 
            return; 
        }
        
        if (!enableOutline && outlineController != null) { 
            outlineController.gameObject.SetActive(false); 
        }

        fireflyManagerInstance = FireflyManager.Instance;
        EnsureUIReferences();

        AllActivePlants.Add(this); // Register instance
    }

    void OnDestroy() {
        AllActivePlants.Remove(this); // Unregister instance

        if (TickManager.Instance != null) {
            TickManager.Instance.UnregisterTickUpdateable(this);
        }

        StopAllCoroutines();
        growthCoroutine = null;

        if (PlantGrowthModifierManager.Instance != null) {
            PlantGrowthModifierManager.Instance.UnregisterPlant(this);
        }

        ClearAllVisuals();
    }

    void Start() {
        if (useWegoSystem && TickManager.Instance != null) {
            TickManager.Instance.RegisterTickUpdateable(this);
        }
        UpdateUI();
    }

    public void OnTickUpdate(int currentTick) {
        if (!useWegoSystem) return;

        switch (currentState) {
            case PlantState.Growing:
                HandleWegoGrowth();
                if (allowPhotosynthesisDuringGrowth) AccumulateEnergyTick();
                break;

            case PlantState.Mature_Idle:
                AccumulateEnergyTick();
                maturityCycleTick++;
                if (maturityCycleTick >= maturityCycleTicks && currentEnergy >= 1f) {
                    currentState = PlantState.Mature_Executing;
                    ExecuteMatureCycleTick();
                    maturityCycleTick = 0;
                }
                break;

            case PlantState.Mature_Executing:
                AccumulateEnergyTick();
                // Mature cycle execution happens instantly in tick-based mode
                currentState = PlantState.Mature_Idle;
                break;
        }

        UpdateUI();
    }

    void Update() {
        if (useWegoSystem) {
            // Only handle real-time visual updates in Wego mode
            if (showGrowthPercentage && continuousIncrement && currentState == PlantState.Growing) {
                UpdateGrowthPercentageUI();
            }
            return;
        }

        // Real-time fallback mode
        switch (currentState) {
            case PlantState.Growing:
                if (allowPhotosynthesisDuringGrowth)
                    AccumulateEnergy();

                if (showGrowthPercentage && continuousIncrement) {
                    UpdateGrowthPercentageUI();
                }
                break;

            case PlantState.GrowthComplete:
                if (!isGrowthCompletionHandled) {
                    isGrowthCompletionHandled = true;
                    if (showGrowthPercentage && targetStemLength > 0) {
                        UpdateGrowthPercentageUI(true);
                    }
                    currentState = PlantState.Mature_Idle;
                    cycleTimer = cycleCooldown;
                    UpdateUI();
                }
                break;

            case PlantState.Mature_Idle:
                AccumulateEnergy();
                UpdateUI();
                cycleTimer -= Time.deltaTime;
                if (cycleTimer <= 0f && currentEnergy >= 1f) {
                    currentState = PlantState.Mature_Executing;
                    StartCoroutine(ExecuteMatureCycle());
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

    public void InitializeAndGrow(NodeGraph graph) {
        if (graph == null || graph.nodes == null) { 
            Debug.LogError($"[{gameObject.name}] Null/empty NodeGraph provided.", gameObject); 
            Destroy(gameObject); 
            return; 
        }
        
        if (growthCoroutine != null) { 
            StopCoroutine(growthCoroutine); 
            growthCoroutine = null; 
        }
        
        ClearAllVisuals();
        rootCellInstance = null; 
        currentStemCount = 0; 
        offsetRightForPattern1 = null; 
        isGrowthCompletionHandled = false; 
        displayedGrowthPercentage = -1;
        
        // Reset Wego variables
        currentGrowthTick = 0;
        maturityCycleTick = 0;
        currentStemStage = 0;
        leafSpawnTick = 0;
        
        // Reset real-time variables
        currentGrowthElapsedTime = 0f;
        estimatedTotalGrowthTime = 1f;
        stepsCompleted = 0;
        totalPlannedSteps = 0;
        actualGrowthProgress = 0f;
        
        nodeGraph = graph; 
        currentState = PlantState.Initializing; 
        currentEnergy = 0f;
        leafDataList.Clear();
        
        CalculateAndApplyStats();
        
        GameObject spawnedSeed = SpawnCellVisual(PlantCellType.Seed, Vector2Int.zero, null, null);
        if (spawnedSeed != null) {
            rootCellInstance = spawnedSeed;
            
            if (PlantGrowthModifierManager.Instance != null && TileInteractionManager.Instance != null) { 
                Vector3Int gridPos = TileInteractionManager.Instance.WorldToCell(transform.position); 
                TileDefinition currentTile = TileInteractionManager.Instance.FindWhichTileDefinitionAt(gridPos); 
                PlantGrowthModifierManager.Instance.RegisterPlantTile(this, currentTile); 
            }
            
            if (targetStemLength > 0) {
                currentState = PlantState.Growing;
                UpdateGrowthPercentageUI(); // Initial UI update (0%)
                
                if (!useWegoSystem) {
                    growthCoroutine = StartCoroutine(GrowthCoroutine_TimeBased());
                }
            } else {
                Debug.LogWarning($"[{gameObject.name}] Target stem length is {targetStemLength}. Skipping visual growth phase.", gameObject);
                currentState = PlantState.GrowthComplete; 
                isGrowthCompletionHandled = false; 
                UpdateUI();
            }
        } else { 
            Debug.LogError($"[{gameObject.name}] Failed to spawn initial seed! Aborting growth.", gameObject); 
            currentState = PlantState.Mature_Idle; 
            Destroy(gameObject, 0.1f); 
        }
        
        UpdateUI();
    }

    void UpdateUI() {
        if (energyText == null) return;

        if (showGrowthPercentage && (currentState == PlantState.Growing || currentState == PlantState.GrowthComplete)) {
            // Growth percentage is handled by UpdateGrowthPercentageUI
        }
        else {
            // Show energy
            energyText.text = $"{Mathf.FloorToInt(currentEnergy)}/{Mathf.FloorToInt(finalMaxEnergy)}";
        }
    }

    void EnsureUIReferences() {
        if (energyText) return; 
        energyText = GetComponentInChildren<TMP_Text>(true); 
        if (!energyText) { 
            Debug.LogWarning($"[{gameObject.name}] Energy Text (TMP_Text) UI reference not assigned in Inspector and not found in children.", gameObject); 
        }
    }

    // Public accessors and utility methods
    public bool DoesCellExistAt(Vector2Int coord) { return cells.ContainsKey(coord); }
    public float GetCellSpacing() { return this.cellSpacing; }
    public GameObject GetCellGameObjectAt(Vector2Int coord) { return activeCellGameObjects.FirstOrDefault(go => go != null && go.GetComponent<PlantCell>()?.GridCoord == coord); }
    public bool IsOutlineEnabled() { return enableOutline; }
    public float GetPoopDetectionRadius() { return poopDetectionRadius; }

    // Wego-specific methods
    public void SetWegoSystem(bool enabled) {
        bool wasEnabled = useWegoSystem;
        useWegoSystem = enabled;

        if (enabled && !wasEnabled && TickManager.Instance != null) {
            TickManager.Instance.RegisterTickUpdateable(this);
            
            // Convert current growth progress to tick-based
            if (currentState == PlantState.Growing && TickManager.Instance.Config != null) {
                var config = TickManager.Instance.Config;
                growthTicksPerStage = config.plantGrowthTicksPerStage;
                
                // Stop real-time growth
                if (growthCoroutine != null) {
                    StopCoroutine(growthCoroutine);
                    growthCoroutine = null;
                }
            }
        } else if (!enabled && wasEnabled && TickManager.Instance != null) {
            TickManager.Instance.UnregisterTickUpdateable(this);
            
            // Convert to real-time growth if currently growing
            if (currentState == PlantState.Growing) {
                growthCoroutine = StartCoroutine(GrowthCoroutine_TimeBased());
            }
        }
    }

    public PlantState GetCurrentState() {
        return currentState;
    }

    public int GetCurrentGrowthTick() {
        return currentGrowthTick;
    }

    public int GetGrowthTicksPerStage() {
        return growthTicksPerStage;
    }

    public int GetCurrentStemStage() {
        return currentStemStage;
    }

    public int GetTargetStemLength() {
        return targetStemLength;
    }

    public void ForceCompleteGrowth() {
        if (Application.isEditor || Debug.isDebugBuild) {
            currentStemStage = targetStemLength;
            CompleteGrowth();
        }
    }
}