﻿// Assets/Scripts/PlantSystem/Growth/PlantGrowth.cs

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using WegoSystem;

public enum PlantState
{
    Initializing,
    Growing,
    GrowthComplete,
    Mature_Idle,
    Mature_Executing
}

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

    List<NodeDefinition> storedDefinitions = new List<NodeDefinition>();

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
                GrowthLogic.OnTickUpdate(currentTick);
                UpdateRadiusVisualizations();
                break;

            case PlantState.Mature_Executing:
                EnergySystem.AccumulateEnergyTick();
                GrowthLogic.OnTickUpdate(currentTick);
                break;
        }

        VisualManager.UpdateWegoUI();
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

    public List<NodeDefinition> Harvest()
    {
        var harvestedDefs = new List<NodeDefinition>();
        var harvestableBerries = new List<GameObject>();

        // Step 1: Find all physical, harvestable items on this plant.
        var tags = GetComponentsInChildren<HarvestableTag>();
        foreach (var tag in tags)
        {
            var cell = tag.GetComponent<PlantCell>();
            if (cell != null && cell.CellType == PlantCellType.Fruit)
            {
                harvestableBerries.Add(tag.gameObject);
            }
        }

        if (harvestableBerries.Count == 0)
        {
            Debug.LogWarning($"[PlantGrowth] Harvest called on '{gameObject.name}', but no fruit with HarvestableTag found.", gameObject);
            return harvestedDefs;
        }

        if (NodeGraph?.nodes == null)
        {
            Debug.LogError($"[PlantGrowth] Cannot determine harvest item because NodeGraph is null.", gameObject);
            return harvestedDefs;
        }

        // Step 2: Find the single gene in the plant's DNA that is responsible for growing berries.
        var berryNode = NodeGraph.nodes.FirstOrDefault(n => n != null && n.effects.Any(e => e != null && e.effectType == NodeEffectType.GrowBerry));

        if (berryNode == null)
        {
            Debug.LogError($"[PlantGrowth] Found harvestable fruit tags, but no NodeData with a 'GrowBerry' effect exists in the plant's NodeGraph. Cannot determine what to give the player.", gameObject);
            return harvestedDefs;
        }

        string matchingNodeDefinitionName = berryNode.definitionName;
        NodeDefinition harvestableDefinition = null;

        // Step 3: Use the name from that gene to find the master NodeDefinition asset.
        if (!string.IsNullOrEmpty(matchingNodeDefinitionName))
        {
            var library = NodeEditorGridController.Instance?.DefinitionLibrary;
            if (library?.definitions != null)
            {
                harvestableDefinition = library.definitions.FirstOrDefault(d => d != null && d.name == matchingNodeDefinitionName);
            }

            if (harvestableDefinition == null)
            {
                 var allDefs = Resources.LoadAll<NodeDefinition>("");
                 harvestableDefinition = allDefs.FirstOrDefault(d => d != null && d.name == matchingNodeDefinitionName);
            }
        }

        if (harvestableDefinition == null)
        {
            Debug.LogError($"[PlantGrowth] Could not find NodeDefinition asset with name '{matchingNodeDefinitionName ?? "NULL"}'. The 'GrowBerry' gene is present, but its source asset is missing from the library or Resources.", gameObject);
            return harvestedDefs;
        }
        
        // Step 4: Add one definition to the list for each physical berry found and destroy the berry.
        Debug.Log($"[PlantGrowth] Using definition '{harvestableDefinition.displayName}' for harvest.");

        foreach (var berryGO in harvestableBerries)
        {
            var cell = berryGO.GetComponent<PlantCell>();
            harvestedDefs.Add(harvestableDefinition);
            if (cell != null)
            {
                ReportCellDestroyed(cell.GridCoord);
            }
            Destroy(berryGO);
        }

        Debug.Log($"[PlantGrowth] Harvested {harvestedDefs.Count} berries from {gameObject.name}");
        return harvestedDefs;
    }

    public void StoreOriginalDefinitions(List<NodeDefinition> definitions)
    {
        storedDefinitions = new List<NodeDefinition>(definitions);
    }

    public float GetCellSpacing() => cellSpacing;
    public bool IsOutlineEnabled() => enableOutline;
    public GameObject GetCellGameObjectAt(Vector2Int coord) => CellManager.GetCellGameObjectAt(coord);
    public GameObject GetOutlinePartPrefab()
    {
        if (outlineController != null && outlineController.outlinePartPrefab != null)
        {
            return outlineController.outlinePartPrefab;
        }
        return outlinePartPrefab;
    }
    public float GetPoopDetectionRadius()
    {
        if (NodeGraph?.nodes == null) return 0f;
        foreach (var node in NodeGraph.nodes)
        {
            if (node?.effects == null) continue;
            var poopEffect = node.effects.FirstOrDefault(e => e.effectType == NodeEffectType.PoopAbsorption);
            if (poopEffect != null)
            {
                return poopEffect.primaryValue;
            }
        }
        return 0f;
    }
    public bool DoesCellExistAt(Vector2Int coord) => CellManager.DoesCellExistAt(coord);
    public void ReportCellDestroyed(Vector2Int coord) => CellManager.ReportCellDestroyed(coord);
    public void ApplyScentDataToObject(GameObject targetObject, Dictionary<ScentDefinition, float> scentRadiusBonuses, Dictionary<ScentDefinition, float> scentStrengthBonuses) => NodeExecutor.ApplyScentDataToObject(targetObject, scentRadiusBonuses, scentStrengthBonuses);
}