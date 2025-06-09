using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using WegoSystem;

public enum PlantState { Initializing, Growing, GrowthComplete, Mature_Idle, Mature_Executing }

public partial class PlantGrowth : MonoBehaviour, ITickUpdateable
{
    #region Variables
    
    [Header("System")]
    [SerializeField] bool useWegoSystem = true;

    public static readonly List<PlantGrowth> AllActivePlants = new List<PlantGrowth>();

    [Header("UI")]
    [SerializeField] TMP_Text energyText;
    [SerializeField] bool showGrowthPercentage = true;
    [SerializeField, Range(1, 25)] int percentageIncrement = 5;
    [SerializeField] bool continuousIncrement = false;

    [Header("Prefabs & Visuals")]
    [SerializeField] GameObject seedCellPrefab;
    [SerializeField] GameObject stemCellPrefab;
    [SerializeField] GameObject leafCellPrefab;
    [SerializeField] GameObject berryCellPrefab;
    [SerializeField] float cellSpacing = 0.08f;
    [SerializeField] PlantShadowController shadowController;
    [SerializeField] GameObject shadowPartPrefab;
    [SerializeField] bool enableOutline = true;
    [SerializeField] PlantOutlineController outlineController;
    [SerializeField] GameObject outlinePartPrefab;
    
    [Header("Growth Settings")]
    [SerializeField] bool allowPhotosynthesisDuringGrowth = false;

    // --- Private State ---
    private NodeGraph nodeGraph;
    public PlantState currentState = PlantState.Initializing;
    public float currentEnergy = 0f;
    private Dictionary<Vector2Int, PlantCellType> cells = new Dictionary<Vector2Int, PlantCellType>();
    private List<GameObject> activeCellGameObjects = new List<GameObject>();
    private FireflyManager fireflyManagerInstance;
    private GameObject rootCellInstance;

    // Wego System Growth
    private float growthProgress = 0f;
    private int maturityCycleTick = 0;
    private int currentStemStage = 0;

    // Real-time Fallback Growth
    private Coroutine growthCoroutine;
    private bool isGrowthCompletionHandled = false;

    // Shared State
    private float poopDetectionRadius = 0f;
    private float poopEnergyBonus = 0f;
    private List<LeafData> leafDataList = new List<LeafData>();

    // Calculated Stats from Nodes
    private int targetStemLength;
    private float finalGrowthSpeed;
    private int finalLeafGap;
    private int finalLeafPattern;
    private float finalGrowthRandomness;
    private float finalMaxEnergy;
    private float finalPhotosynthesisRate;
    private int maturityCycleTicks;
    private float nodeCastDelay;

    // --- Real-time Fallback Private State ---
    private int currentStemCount = 0;
    private float cycleTimer = 0f;
    private int displayedGrowthPercentage = -1;
    private bool? offsetRightForPattern1 = null;
    private float estimatedTotalGrowthTime = 1f;
    private float actualGrowthProgress = 0f;
    private int stepsCompleted = 0;
    private int totalPlannedSteps = 0;

    #endregion

    #region Unity Lifecycle

    void Awake()
    {
        bool setupValid = true;
        if (shadowController == null) {
            shadowController = GetComponentInChildren<PlantShadowController>(true);
            if (shadowController == null) { Debug.LogError($"PlantGrowth ERROR on '{gameObject.name}': PlantShadowController ref missing!", this); setupValid = false; }
            else { Debug.LogWarning($"PlantGrowth on '{gameObject.name}': Found PlantShadowController dynamically.", this); }
        }
        if (shadowPartPrefab == null) { Debug.LogError($"PlantGrowth ERROR on '{gameObject.name}': Shadow Part Prefab missing!", this); setupValid = false; }
        if (enableOutline) {
            if (outlineController == null) {
                outlineController = GetComponentInChildren<PlantOutlineController>(true);
                if (outlineController == null) { Debug.LogError($"PlantGrowth ERROR on '{gameObject.name}': PlantOutlineController ref missing but outline is enabled!", this); setupValid = false; }
                else { Debug.LogWarning($"PlantGrowth on '{gameObject.name}': Found PlantOutlineController dynamically.", this); }
            }
            if (outlinePartPrefab == null) { Debug.LogError($"PlantGrowth ERROR on '{gameObject.name}': Outline Part Prefab missing but outline is enabled!", this); setupValid = false; }
        }
        if (seedCellPrefab == null) { Debug.LogError($"PlantGrowth ERROR on '{gameObject.name}': Seed Cell Prefab missing!", this); setupValid = false; }
        if (energyText == null) Debug.LogWarning($"PlantGrowth on '{gameObject.name}': Energy Text (TMP_Text) missing.", this);
        if (!setupValid) { enabled = false; return; }
        if (!enableOutline && outlineController != null) { outlineController.gameObject.SetActive(false); }
        fireflyManagerInstance = FireflyManager.Instance;
        EnsureUIReferences();
        AllActivePlants.Add(this);
    }

    void OnDestroy()
    {
        AllActivePlants.Remove(this);
        if (TickManager.Instance != null) { TickManager.Instance.UnregisterTickUpdateable(this); }
        StopAllCoroutines();
        growthCoroutine = null;
        if (PlantGrowthModifierManager.Instance != null) { PlantGrowthModifierManager.Instance.UnregisterPlant(this); }
        ClearAllVisuals();
    }

    void Start()
    {
        if (useWegoSystem && TickManager.Instance != null) {
            TickManager.Instance.RegisterTickUpdateable(this);
        }
        UpdateUI();
    }
    
    void Update()
    {
        if (useWegoSystem) {
            if (showGrowthPercentage && currentState == PlantState.Growing) { UpdateGrowthPercentageUI(); }
            return;
        }
        switch (currentState) {
            case PlantState.Growing:
                if (allowPhotosynthesisDuringGrowth) AccumulateEnergy();
                if (showGrowthPercentage && continuousIncrement) { UpdateGrowthPercentageUI(); }
                break;
            case PlantState.GrowthComplete:
                if (!isGrowthCompletionHandled) {
                    isGrowthCompletionHandled = true;
                    if (showGrowthPercentage && targetStemLength > 0) { UpdateGrowthPercentageUI(true); }
                    float rtCooldown = maturityCycleTicks * (TickManager.Instance?.Config.GetRealSecondsPerTick() ?? 0.5f);
                    cycleTimer = rtCooldown; 
                    currentState = PlantState.Mature_Idle;
                    UpdateUI();
                }
                break;
            case PlantState.Mature_Idle:
                AccumulateEnergy();
                UpdateUI();
                cycleTimer -= Time.deltaTime;
                if (cycleTimer <= 0f && currentEnergy >= 1f) { currentState = PlantState.Mature_Executing; StartCoroutine(ExecuteMatureCycle()); }
                break;
            case PlantState.Mature_Executing:
                AccumulateEnergy();
                UpdateUI();
                break;
        }
    }
    
    #endregion
    
    #region Initialization and Stat Calculation

    public void InitializeAndGrow(NodeGraph graph) {
        if (graph == null || graph.nodes == null) { Debug.LogError($"[{gameObject.name}] Null/empty NodeGraph provided.", gameObject); Destroy(gameObject); return; }
        if (growthCoroutine != null) { StopCoroutine(growthCoroutine); growthCoroutine = null; }
        ClearAllVisuals();
        rootCellInstance = null;
        isGrowthCompletionHandled = false;
        displayedGrowthPercentage = -1;
        growthProgress = 0f;
        maturityCycleTick = 0;
        currentStemStage = 0;
        currentStemCount = 0;
        offsetRightForPattern1 = null;
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
                UpdateGrowthPercentageUI(); 
                if (!useWegoSystem) { growthCoroutine = StartCoroutine(GrowthCoroutine_Time_Based()); }
            } else {
                Debug.LogWarning($"[{gameObject.name}] Target stem length is {targetStemLength}. Skipping visual growth phase.", gameObject);
                CompleteGrowth();
            }
        } else {
            Debug.LogError($"[{gameObject.name}] Failed to spawn initial seed! Aborting growth.", gameObject);
            currentState = PlantState.Mature_Idle;
            Destroy(gameObject, 0.1f);
        }
        UpdateUI();
    }
    
    private void CalculateAndApplyStats()
    {
        if (nodeGraph == null) {
            Debug.LogError($"[{gameObject.name}] CalculateAndApplyStats called with null NodeGraph!");
            return;
        }

        float baseEnergyStorage = 10f;
        float basePhotosynthesisRate = 0.5f;
        int baseStemMin = 3;
        int baseStemMax = 5;
        float baseGrowthSpeedRate = 0.2f;
        int baseLeafGap = 1;
        int baseLeafPattern = 0;
        float baseGrowthRandomness = 0.1f;
        int baseCooldownTicks = 20;
        float baseCastDelay = 0.1f;
        float accumulatedEnergyStorage = 0f;
        float accumulatedPhotosynthesis = 0f;
        int stemLengthModifier = 0;
        float growthSpeedRateModifier = 0f;
        int leafGapModifier = 0;
        int currentLeafPattern = baseLeafPattern;
        float growthRandomnessModifier = 0f;
        int cooldownTicksModifier = 0;
        float castDelayModifier = 0f;
        bool seedFound = false;
        poopDetectionRadius = 0f;
        poopEnergyBonus = 0f;

        foreach (NodeData node in nodeGraph.nodes.OrderBy(n => n.orderIndex)) {
            if (node?.effects == null) continue;
            foreach (var effect in node.effects) {
                if (effect == null || !effect.isPassive) continue;
                switch (effect.effectType) {
                    case NodeEffectType.SeedSpawn: seedFound = true; break;
                    case NodeEffectType.EnergyStorage: accumulatedEnergyStorage += effect.primaryValue; break;
                    case NodeEffectType.EnergyPhotosynthesis: accumulatedPhotosynthesis += effect.primaryValue; break;
                    case NodeEffectType.StemLength: stemLengthModifier += Mathf.RoundToInt(effect.primaryValue); break;
                    case NodeEffectType.GrowthSpeed: growthSpeedRateModifier += effect.primaryValue; break;
                    case NodeEffectType.LeafGap: leafGapModifier += Mathf.RoundToInt(effect.primaryValue); break;
                    case NodeEffectType.LeafPattern: currentLeafPattern = Mathf.Clamp(Mathf.RoundToInt(effect.primaryValue), 0, 4); break;
                    case NodeEffectType.StemRandomness: growthRandomnessModifier += effect.primaryValue; break;
                    case NodeEffectType.Cooldown: cooldownTicksModifier += Mathf.RoundToInt(effect.primaryValue); break;
                    case NodeEffectType.CastDelay: castDelayModifier += effect.primaryValue; break;
                    case NodeEffectType.PoopFertilizer: poopDetectionRadius = Mathf.Max(0f, effect.primaryValue); poopEnergyBonus = Mathf.Max(0f, effect.secondaryValue); break;
                }
            }
        }

        finalMaxEnergy = Mathf.Max(1f, baseEnergyStorage + accumulatedEnergyStorage);
        finalPhotosynthesisRate = Mathf.Max(0f, basePhotosynthesisRate + accumulatedPhotosynthesis);
        int finalStemMin = Mathf.Max(1, baseStemMin + stemLengthModifier);
        int finalStemMax = Mathf.Max(finalStemMin, baseStemMax + stemLengthModifier);
        finalGrowthSpeed = Mathf.Max(0.01f, baseGrowthSpeedRate + growthSpeedRateModifier);
        finalLeafGap = Mathf.Max(0, baseLeafGap + leafGapModifier);
        finalLeafPattern = currentLeafPattern;
        finalGrowthRandomness = Mathf.Clamp01(baseGrowthRandomness + growthRandomnessModifier);
        maturityCycleTicks = Mathf.Max(1, baseCooldownTicks + cooldownTicksModifier);
        nodeCastDelay = Mathf.Max(0.01f, baseCastDelay + castDelayModifier);
        targetStemLength = seedFound ? Random.Range(finalStemMin, finalStemMax + 1) : 0;

        if (!seedFound) {
            Debug.LogWarning($"[{gameObject.name}] NodeGraph lacks SeedSpawn effect. Growth aborted.", gameObject);
        }
    }
    
    #endregion
    
    #region UI and Visuals

    private void UpdateUI() {
        if (energyText == null) return;
        if (showGrowthPercentage && (currentState == PlantState.Growing || (currentState == PlantState.GrowthComplete && !isGrowthCompletionHandled)))
        {
             // UpdateGrowthPercentageUI is called to handle this display
        }
        else {
            energyText.text = $"{Mathf.FloorToInt(currentEnergy)}/{Mathf.FloorToInt(finalMaxEnergy)}";
        }
    }
    
    private void UpdateGrowthPercentageUI(bool forceComplete = false)
    {
        if (!showGrowthPercentage || energyText == null) return;
        float rawPercentageFloat;
        if (forceComplete || (currentState == PlantState.GrowthComplete && isGrowthCompletionHandled)) { rawPercentageFloat = 100f; }
        else if (useWegoSystem) {
            if (targetStemLength <= 0) { rawPercentageFloat = 100f; }
            else {
                float partialStepProgress = growthProgress * (100f / targetStemLength);
                rawPercentageFloat = ((float)currentStemStage / targetStemLength) * 100f + partialStepProgress;
            }
        }
        else if (continuousIncrement) {
            if (totalPlannedSteps > 0) {
                rawPercentageFloat = ((float)stepsCompleted / totalPlannedSteps) * 100f;
                if (actualGrowthProgress > 0f && stepsCompleted < totalPlannedSteps) {
                    float stepSize = 100f / totalPlannedSteps;
                    float partialStepProgress = actualGrowthProgress * stepSize;
                    rawPercentageFloat = (stepsCompleted * stepSize) + partialStepProgress;
                }
            } else { rawPercentageFloat = (currentState == PlantState.Growing) ? 0f : 100f; }
        } else {
            if (targetStemLength <= 0) { rawPercentageFloat = 100f; }
            else { rawPercentageFloat = Mathf.Clamp(((float)currentStemCount / targetStemLength) * 100f, 0f, 100f); }
        }
        rawPercentageFloat = Mathf.Clamp(rawPercentageFloat, 0f, 100f);
        int targetDisplayValue;
        if (percentageIncrement <= 1) { targetDisplayValue = Mathf.FloorToInt(rawPercentageFloat); }
        else { targetDisplayValue = Mathf.RoundToInt(rawPercentageFloat / percentageIncrement) * percentageIncrement; }
        targetDisplayValue = Mathf.Min(targetDisplayValue, 100);
        if (targetDisplayValue == 100 && currentState == PlantState.Growing && !forceComplete) { targetDisplayValue = 99; }
        if (targetDisplayValue != displayedGrowthPercentage) {
            displayedGrowthPercentage = targetDisplayValue;
            energyText.text = $"{displayedGrowthPercentage}%";
        }
    }
    
    private void EnsureUIReferences() {
        if (energyText) return;
        energyText = GetComponentInChildren<TMP_Text>(true);
        if (!energyText) { Debug.LogWarning($"[{gameObject.name}] Energy Text (TMP_Text) UI reference not assigned and not found in children.", gameObject); }
    }
    
    #endregion
    
    #region Public Accessors & Modifiers

    public bool DoesCellExistAt(Vector2Int coord) { return cells.ContainsKey(coord); }
    public float GetCellSpacing() { return this.cellSpacing; }
    public GameObject GetCellGameObjectAt(Vector2Int coord) { return activeCellGameObjects.FirstOrDefault(go => go != null && go.GetComponent<PlantCell>()?.GridCoord == coord); }
    public bool IsOutlineEnabled() { return enableOutline; }
    public float GetPoopDetectionRadius() { return poopDetectionRadius; }
    public void SetWegoSystem(bool enabled) {
        bool wasEnabled = useWegoSystem;
        useWegoSystem = enabled;
        if (enabled && !wasEnabled && TickManager.Instance != null) {
            TickManager.Instance.RegisterTickUpdateable(this);
            if (currentState == PlantState.Growing && growthCoroutine != null) { StopCoroutine(growthCoroutine); growthCoroutine = null; }
        } else if (!enabled && wasEnabled && TickManager.Instance != null) {
            TickManager.Instance.UnregisterTickUpdateable(this);
            if (currentState == PlantState.Growing) { growthCoroutine = StartCoroutine(GrowthCoroutine_Time_Based()); }
        }
    }
    
    #endregion
}