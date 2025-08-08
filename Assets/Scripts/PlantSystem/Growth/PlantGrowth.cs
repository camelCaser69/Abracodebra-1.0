// REWORKED FILE: Assets/Scripts/PlantSystem/Growth/PlantGrowth.cs
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using WegoSystem;
using Abracodabra.Genes;
using Abracodabra.Genes.Templates;
using Abracodabra.Genes.Runtime;
using Abracodabra.Genes.Core; // For Fruit component

public enum PlantState
{
    Initializing,
    Growing,
    Mature
}

public class PlantGrowth : MonoBehaviour, ITickUpdateable
{
    public static readonly List<PlantGrowth> AllActivePlants = new List<PlantGrowth>();

    // --- NEW GENE SYSTEM ---
    [Header("New Gene System")]
    public SeedTemplate seedTemplate;
    // FIX: Made these public so other classes can access them
    public PlantGeneRuntimeState geneRuntimeState { get; private set; }
    public PlantSequenceExecutor sequenceExecutor { get; private set; }

    // --- MANAGERS (Now regular classes) ---
    // FIX: Made these public so other classes can access them
    public PlantCellManager CellManager { get; private set; }
    public PlantGrowthLogic GrowthLogic { get; private set; }
    public PlantEnergySystem EnergySystem { get; private set; }
    public PlantVisualManager VisualManager { get; private set; }

    // --- CORE & VISUAL REFERENCES ---
    [Header("Visual & Core References")]
    [SerializeField] private GameObject seedCellPrefab;
    [SerializeField] private GameObject stemCellPrefab;
    [SerializeField] private GameObject leafCellPrefab;
    [SerializeField] private GameObject berryCellPrefab;
    [SerializeField] public float cellSpacing = 0.08f;
    [SerializeField] private PlantShadowController shadowController;
    [SerializeField] private PlantOutlineController outlineController;
    [SerializeField] private GameObject outlinePartPrefab; // Keep this as a fallback
    [SerializeField] private bool enableOutline = true;
    

    public PlantState CurrentState { get; private set; } = PlantState.Initializing;

    void Awake()
    {
        AllActivePlants.Add(this);

        // FIX: Initialize all manager classes in Awake
        CellManager = new PlantCellManager(this, seedCellPrefab, stemCellPrefab, leafCellPrefab, berryCellPrefab, cellSpacing);
        GrowthLogic = new PlantGrowthLogic(this);
        EnergySystem = new PlantEnergySystem(this);
        VisualManager = new PlantVisualManager(this, shadowController, null, outlineController, outlinePartPrefab, enableOutline);
    }

    void OnDestroy()
    {
        AllActivePlants.Remove(this);
        var tickManager = TickManager.Instance;
        if (tickManager != null)
        {
            tickManager.UnregisterTickUpdateable(this);
        }
    }

    public void InitializeFromTemplate(SeedTemplate template)
    {
        if (template == null)
        {
            Debug.LogError($"Cannot initialize plant on '{gameObject.name}': Provided SeedTemplate is null.", this);
            Destroy(gameObject);
            return;
        }

        this.seedTemplate = template;
        geneRuntimeState = seedTemplate.CreateRuntimeState();
        
        EnergySystem.MaxEnergy = geneRuntimeState.maxEnergy; // Sync max energy

        sequenceExecutor = GetComponent<PlantSequenceExecutor>() ?? gameObject.AddComponent<PlantSequenceExecutor>();
        sequenceExecutor.plantGrowth = this;
        sequenceExecutor.InitializeWithTemplate(seedTemplate);

        CurrentState = PlantState.Growing;

        if (TickManager.Instance != null)
        {
            TickManager.Instance.RegisterTickUpdateable(this);
        }

        Debug.Log($"Plant '{gameObject.name}' initialized from template '{template.templateName}'.");
    }

    public void OnTickUpdate(int currentTick)
    {
        EnergySystem.OnTickUpdate();
    }
    
    // FIX: Added this new method for animals to call
    public void HandleBeingEaten(AnimalController eater, PlantCell eatenCell)
    {
        Debug.Log($"{eater.SpeciesName} ate cell at {eatenCell.GridCoord} on plant {name}");
        // Here you would trigger any "On Eaten" genes if you create them
    }

    // FIX: Added this method back for PlantCell to call
    public void ReportCellDestroyed(Vector2Int coord)
    {
        CellManager?.ReportCellDestroyed(coord);
    }

    // FIX: Added this method back for PlantOutlineController to use
    public GameObject GetCellGameObjectAt(Vector2Int coord)
    {
        return CellManager?.GetCellGameObjectAt(coord);
    }

    public Transform[] GetFruitSpawnPoints()
    {
        return GetComponentsInChildren<Transform>()
            .Where(t => t.CompareTag("FruitSpawn"))
            .ToArray();
    }
}