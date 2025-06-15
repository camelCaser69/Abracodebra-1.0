using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using WegoSystem;

public enum PlantState { Initializing, Growing, GrowthComplete, Mature_Idle, Mature_Executing }

public class PlantGrowth : MonoBehaviour, ITickUpdateable
{
    // --- System Toggles ---
    [Header("System Toggles")]

    // --- Core Component Managers (Initialized in Awake) ---
    public static readonly List<PlantGrowth> AllActivePlants = new List<PlantGrowth>();
    public PlantCellManager CellManager { get; set; }
    public PlantNodeExecutor NodeExecutor { get; set; }
    public PlantGrowthLogic GrowthLogic { get; set; }
    public PlantEnergySystem EnergySystem { get; set; }
    public PlantVisualManager VisualManager { get; set; }
    public NodeGraph NodeGraph { get; set; }
    public PlantState CurrentState { get; set; } = PlantState.Initializing;

    // --- Prefab & Visual References ---
    [Header("Prefab & Visual References")]
    [SerializeField] GameObject seedCellPrefab;
    [SerializeField] GameObject stemCellPrefab;
    [SerializeField] GameObject leafCellPrefab;
    [SerializeField] GameObject berryCellPrefab;
    [SerializeField] float cellSpacing = 0.08f;

    [Header("Shadow & Outline")]
    [SerializeField] PlantShadowController shadowController;
    [SerializeField] GameObject shadowPartPrefab;
    [SerializeField] bool enableOutline = true;
    [SerializeField] PlantOutlineController outlineController;
    [SerializeField] GameObject outlinePartPrefab;

    // --- UI Display Settings ---
    [Header("UI Display Settings")]
    [SerializeField] public bool showGrowthPercentage = true; // This is the only UI setting needed now

    // --- Gameplay Settings ---
    [Header("Gameplay Settings")]
    [SerializeField] public bool allowPhotosynthesisDuringGrowth = false;


    void Awake()
    {
        InitializeComponents();
        ValidateReferences();
        AllActivePlants.Add(this);
    }

    void InitializeComponents()
    {
        CellManager = new PlantCellManager(this, seedCellPrefab, stemCellPrefab, leafCellPrefab, berryCellPrefab, cellSpacing);
        NodeExecutor = new PlantNodeExecutor(this);
        GrowthLogic = new PlantGrowthLogic(this);
        EnergySystem = new PlantEnergySystem(this);
        VisualManager = new PlantVisualManager(this, shadowController, shadowPartPrefab, outlineController, outlinePartPrefab, enableOutline);
    }

    void ValidateReferences()
    {
        bool setupValid = true;

        if (shadowController == null)
        {
            shadowController = GetComponentInChildren<PlantShadowController>(true);
            if (shadowController == null)
            {
                Debug.LogError($"PlantGrowth ERROR on '{gameObject.name}': PlantShadowController ref missing!", this);
                setupValid = false;
            }
        }

        if (shadowPartPrefab == null)
        {
            Debug.LogError($"PlantGrowth ERROR on '{gameObject.name}': Shadow Part Prefab missing!", this);
            setupValid = false;
        }

        if (enableOutline)
        {
            if (outlineController == null)
            {
                outlineController = GetComponentInChildren<PlantOutlineController>(true);
                if (outlineController == null)
                {
                    Debug.LogError($"PlantGrowth ERROR on '{gameObject.name}': PlantOutlineController ref missing but outline is enabled!", this);
                    setupValid = false;
                }
            }
            if (outlinePartPrefab == null)
            {
                Debug.LogError($"PlantGrowth ERROR on '{gameObject.name}': Outline Part Prefab missing but outline is enabled!", this);
                setupValid = false;
            }
        }

        if (seedCellPrefab == null) {
            Debug.LogError($"PlantGrowth ERROR on '{gameObject.name}': Seed Cell Prefab missing!", this);
            setupValid = false;
        }

        if (!setupValid)
        {
            enabled = false;
            return;
        }

        if (!enableOutline && outlineController != null)
        {
            outlineController.gameObject.SetActive(false);
        }
    }

    void Start() {
        // Always register with TickManager
        if (TickManager.Instance != null) {
            TickManager.Instance.RegisterTickUpdateable(this);
        }
        VisualManager.UpdateUI();
    }

    void OnDestroy()
    {
        AllActivePlants.Remove(this);
        if (TickManager.Instance != null)
        {
            TickManager.Instance.UnregisterTickUpdateable(this);
        }
        if (PlantGrowthModifierManager.Instance != null)
        {
            PlantGrowthModifierManager.Instance.UnregisterPlant(this);
        }
        CellManager?.ClearAllVisuals();
    }

    void Update() {
        // Only update visual elements - remove all real-time growth logic
        VisualManager.UpdateWegoUI();
    }

    public void OnTickUpdate(int currentTick) {
        // Remove the useWegoSystem check
        GrowthLogic.OnTickUpdate(currentTick);
        VisualManager.UpdateUI();
    }

    public void InitializeAndGrow(NodeGraph graph) {
        if (graph == null || graph.nodes == null) {
            Debug.LogError($"[{gameObject.name}] Null/empty NodeGraph provided.", gameObject);
            Destroy(gameObject);
            return;
        }

        CellManager.ClearAllVisuals();
        VisualManager.ResetDisplayState();

        NodeGraph = graph;
        CurrentState = PlantState.Initializing;
        EnergySystem.CurrentEnergy = 0f;
        CellManager.LeafDataList.Clear();

        GrowthLogic.CalculateAndApplyStats();

        GameObject spawnedSeed = CellManager.SpawnCellVisual(PlantCellType.Seed, Vector2Int.zero, null, null);
        if (spawnedSeed != null) {
            CellManager.RootCellInstance = spawnedSeed;
            RegisterWithManagers();

            if (GrowthLogic.TargetStemLength > 0) {
                CurrentState = PlantState.Growing;
                VisualManager.UpdateGrowthPercentageUI();
            }
            else {
                Debug.LogWarning($"[{gameObject.name}] Target stem length is {GrowthLogic.TargetStemLength}. Skipping visual growth phase.", gameObject);
                GrowthLogic.CompleteGrowth();
            }
        }
        else {
            Debug.LogError($"[{gameObject.name}] Failed to spawn initial seed! Aborting growth.", gameObject);
            CurrentState = PlantState.Mature_Idle;
            Destroy(gameObject, 0.1f);
        }

        VisualManager.UpdateUI();
    }

    private void RegisterWithManagers()
    {
        if (PlantGrowthModifierManager.Instance != null && TileInteractionManager.Instance != null)
        {
            Vector3Int gridPos = TileInteractionManager.Instance.WorldToCell(transform.position);
            TileDefinition currentTile = TileInteractionManager.Instance.FindWhichTileDefinitionAt(gridPos);
            PlantGrowthModifierManager.Instance.RegisterPlantTile(this, currentTile);
        }
    }

    // --- Helper Getters ---
    public bool DoesCellExistAt(Vector2Int coord) => CellManager.DoesCellExistAt(coord);
    public float GetCellSpacing() => cellSpacing;
    public GameObject GetCellGameObjectAt(Vector2Int coord) => CellManager.GetCellGameObjectAt(coord);
    public bool IsOutlineEnabled() => enableOutline;
    public float GetPoopDetectionRadius() => NodeExecutor.PoopDetectionRadius;
    public void ReportCellDestroyed(Vector2Int coord) => CellManager.ReportCellDestroyed(coord);
    public void ApplyScentDataToObject(GameObject targetObject, Dictionary<ScentDefinition, float> scentRadiusBonuses, Dictionary<ScentDefinition, float> scentStrengthBonuses)
        => NodeExecutor.ApplyScentDataToObject(targetObject, scentRadiusBonuses, scentStrengthBonuses);
}