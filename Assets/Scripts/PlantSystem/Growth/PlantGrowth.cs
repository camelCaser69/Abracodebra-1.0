using System.Collections.Generic;
using UnityEngine;
using WegoSystem;

public enum PlantState { Initializing, Growing, GrowthComplete, Mature_Idle, Mature_Executing }

public class PlantGrowth : MonoBehaviour, ITickUpdateable
{
    public static readonly List<PlantGrowth> AllActivePlants = new List<PlantGrowth>();

    public PlantCellManager CellManager { get; set; }
    public PlantNodeExecutor NodeExecutor { get; set; }
    public PlantGrowthLogic GrowthLogic { get; set; }
    public PlantEnergySystem EnergySystem { get; set; }
    public PlantVisualManager VisualManager { get; set; }
    public NodeGraph NodeGraph { get; set; }
    public PlantState CurrentState { get; set; } = PlantState.Initializing;

    [SerializeField] GameObject seedCellPrefab;
    [SerializeField] GameObject stemCellPrefab;
    [SerializeField] GameObject leafCellPrefab;
    [SerializeField] GameObject berryCellPrefab;
    [SerializeField] float cellSpacing = 0.08f;

    [SerializeField] PlantShadowController shadowController;
    [SerializeField] GameObject shadowPartPrefab;
    [SerializeField] bool enableOutline = true;
    [SerializeField] PlantOutlineController outlineController;
    [SerializeField] GameObject outlinePartPrefab; // Added missing field

    [SerializeField] public bool showGrowthPercentage = true;
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
        
        // Get the outline part prefab from the controller if available
        GameObject outlinePrefabToUse = null;
        if (outlineController != null && outlineController.outlinePartPrefab != null)
        {
            outlinePrefabToUse = outlineController.outlinePartPrefab;
        }
        else
        {
            outlinePrefabToUse = outlinePartPrefab;
        }
        
        VisualManager = new PlantVisualManager(this, shadowController, shadowPartPrefab, outlineController, outlinePrefabToUse, enableOutline);
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

            // Check if we can get outline part prefab from either source
            GameObject availableOutlinePrefab = GetOutlinePartPrefab();
            if (availableOutlinePrefab == null)
            {
                Debug.LogError($"PlantGrowth ERROR on '{gameObject.name}': Outline Part Prefab missing but outline is enabled!", this);
                setupValid = false;
            }
        }

        if (seedCellPrefab == null)
        {
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

    void Start()
    {
        if (TickManager.Instance != null)
        {
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

        if (GridDebugVisualizer.Instance != null)
        {
            GridDebugVisualizer.Instance.HideContinuousRadius(this);
        }

        CellManager?.ClearAllVisuals();
    }

    void Update()
    {
        VisualManager.UpdateWegoUI();
    }

    public void OnTickUpdate(int currentTick)
    {
        if (!enabled) return;

        switch (CurrentState)
        {
            case PlantState.Initializing:
                break;

            case PlantState.Growing:
                GrowthLogic.OnTickUpdate(currentTick);
                if (allowPhotosynthesisDuringGrowth)
                {
                    EnergySystem.AccumulateEnergyTick();
                }
                break;

            case PlantState.GrowthComplete:
                TransitionToMature();
                break;

            case PlantState.Mature_Idle:
                EnergySystem.AccumulateEnergyTick();
                NodeExecutor.ExecuteMatureCycleTick();
                UpdateRadiusVisualizations(); // Update visualizations during mature state
                break;

            case PlantState.Mature_Executing:
                EnergySystem.AccumulateEnergyTick();
                break;
        }

        VisualManager.UpdateUI();
    }

    void UpdateRadiusVisualizations()
    {
        if (GridDebugVisualizer.Instance != null)
        {
            GridEntity gridEntity = GetComponent<GridEntity>();
            if (gridEntity != null)
            {
                float poopRadius = GetPoopDetectionRadius();
                if (poopRadius > 0.01f)
                {
                    int radiusTiles = Mathf.RoundToInt(poopRadius);
                    GridDebugVisualizer.Instance.VisualizePlantPoopRadius(this, gridEntity.Position, radiusTiles);
                }
                else
                {
                    GridDebugVisualizer.Instance.HideContinuousRadius(this);
                }
            }
        }
    }

    public void InitializeAndGrow(NodeGraph graph)
    {
        if (graph == null || graph.nodes == null || graph.nodes.Count == 0)
        {
            Debug.LogError($"[{gameObject.name}] Null/empty NodeGraph provided. Aborting growth.", gameObject);
            CurrentState = PlantState.Mature_Idle;
            Destroy(gameObject, 0.1f);
            return;
        }

        CellManager.ClearAllVisuals();
        VisualManager.ResetDisplayState();

        NodeGraph = graph;
        CurrentState = PlantState.Initializing;
        EnergySystem.CurrentEnergy = 0f;
        CellManager.LeafDataList.Clear();

        GrowthLogic.CalculateAndApplyStats();
        NodeExecutor.ProcessPassiveEffects(NodeGraph);

        GameObject spawnedSeed = CellManager.CreateSeedCell(Vector2Int.zero);
        if (spawnedSeed != null)
        {
            CellManager.RootCellInstance = spawnedSeed;
            RegisterWithManagers();

            if (GrowthLogic.TargetStemLength > 0)
            {
                CurrentState = PlantState.Growing;
                VisualManager.UpdateGrowthPercentageUI();
            }
            else
            {
                Debug.LogWarning($"[{gameObject.name}] Target stem length is {GrowthLogic.TargetStemLength}. Skipping visual growth phase.", gameObject);
                GrowthLogic.CompleteGrowth();
            }

            VisualManager.UpdateShadow();
            VisualManager.UpdateOutline();
        }
        else
        {
            Debug.LogError($"[{gameObject.name}] Failed to spawn initial seed! Aborting growth.", gameObject);
            CurrentState = PlantState.Mature_Idle;
            Destroy(gameObject, 0.1f);
        }
    }

    void TransitionToMature()
    {
        CurrentState = PlantState.Mature_Idle;

        if (GrowthLogic.GrowthTicksPerStage <= 1)
        {
            Debug.Log($"[{gameObject.name}] Growth completed instantly. Skipping visual growth phase.", gameObject);
            GrowthLogic.CompleteGrowth();
        }
        else
        {
            Debug.Log($"[{gameObject.name}] Growth completed! Transitioning to mature state.");
        }
    }

    void RegisterWithManagers()
    {
        if (PlantGrowthModifierManager.Instance != null && TileInteractionManager.Instance != null)
        {
            Vector3Int gridPos = TileInteractionManager.Instance.WorldToCell(transform.position);
            TileDefinition currentTile = TileInteractionManager.Instance.FindWhichTileDefinitionAt(gridPos);
            PlantGrowthModifierManager.Instance.RegisterPlantTile(this, currentTile);
        }
    }

    public float GetCellSpacing() => cellSpacing;

    public bool IsOutlineEnabled() => enableOutline;

    public GameObject GetCellGameObjectAt(Vector2Int coord) => CellManager.GetCellGameObjectAt(coord);

    // FIXED: Use proper property to access the outlinePartPrefab
    public GameObject GetOutlinePartPrefab()
    {
        // Try to get from outline controller first, fallback to direct reference
        if (outlineController != null && outlineController.outlinePartPrefab != null)
        {
            return outlineController.outlinePartPrefab;
        }
        return outlinePartPrefab;
    }

    public bool DoesCellExistAt(Vector2Int coord) => CellManager.DoesCellExistAt(coord);

    public float GetPoopDetectionRadius() => NodeExecutor.PoopDetectionRadius;
    public void ReportCellDestroyed(Vector2Int coord) => CellManager.ReportCellDestroyed(coord);
    public void ApplyScentDataToObject(GameObject targetObject, Dictionary<ScentDefinition, float> scentRadiusBonuses, Dictionary<ScentDefinition, float> scentStrengthBonuses)
        => NodeExecutor.ApplyScentDataToObject(targetObject, scentRadiusBonuses, scentStrengthBonuses);
}