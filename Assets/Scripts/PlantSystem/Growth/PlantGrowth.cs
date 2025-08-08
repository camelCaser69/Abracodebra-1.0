// REWORKED FILE: Assets/Scripts/PlantSystem/Growth/PlantGrowth.cs
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using WegoSystem;
using Abracodabra.Genes;
using Abracodabra.Genes.Templates;
using Abracodabra.Genes.Runtime;
using Abracodabra.Genes.Core;

// FIX: Added the missing enum definition here, in the global namespace.
public enum PlantState
{
    Initializing,
    Growing,
    Mature
}

public class PlantGrowth : MonoBehaviour, ITickUpdateable
{
    // ... (rest of the script is exactly the same as the previous version)
    public static readonly List<PlantGrowth> AllActivePlants = new List<PlantGrowth>();

    // --- NEW GENE SYSTEM ---
    [Header("New Gene System")]
    public SeedTemplate seedTemplate;
    public PlantGeneRuntimeState geneRuntimeState { get; private set; }
    public PlantSequenceExecutor sequenceExecutor { get; private set; }

    // --- MANAGERS ---
    public PlantCellManager CellManager { get; private set; }
    public PlantGrowthLogic GrowthLogic { get; private set; }
    public PlantEnergySystem EnergySystem { get; private set; }
    public PlantVisualManager VisualManager { get; private set; }

    // --- CORE & VISUAL REFERENCES ---
    [Header("Visual & Core References")]
    [SerializeField] public float cellSpacing = 0.08f;
    [SerializeField] private GameObject seedCellPrefab;
    [SerializeField] private GameObject stemCellPrefab;
    [SerializeField] private GameObject leafCellPrefab;
    [SerializeField] private GameObject berryCellPrefab;
    [SerializeField] private PlantShadowController shadowController;
    [SerializeField] private PlantOutlineController outlineController;
    [SerializeField] private GameObject outlinePartPrefab;
    [SerializeField] private bool enableOutline = true;

    public PlantState CurrentState { get; private set; } = PlantState.Initializing;

    void Awake()
    {
        AllActivePlants.Add(this);
        CellManager = new PlantCellManager(this, seedCellPrefab, stemCellPrefab, leafCellPrefab, berryCellPrefab, cellSpacing);
        GrowthLogic = new PlantGrowthLogic(this);
        EnergySystem = new PlantEnergySystem(this);
        VisualManager = new PlantVisualManager(this, shadowController, null, outlineController, outlinePartPrefab, enableOutline);
    }

    void OnDestroy()
    {
        AllActivePlants.Remove(this);
        var tickManager = TickManager.Instance;
        if (tickManager != null) { tickManager.UnregisterTickUpdateable(this); }
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
        EnergySystem.MaxEnergy = geneRuntimeState.maxEnergy;
        
        sequenceExecutor = GetComponent<PlantSequenceExecutor>();
        if (sequenceExecutor == null)
        {
            sequenceExecutor = gameObject.AddComponent<PlantSequenceExecutor>();
        }
        
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
    
    public void HandleBeingEaten(AnimalController eater, PlantCell eatenCell)
    {
        Debug.Log($"{eater.SpeciesName} ate cell at {eatenCell.GridCoord} on plant {name}");
    }

    public void ReportCellDestroyed(Vector2Int coord)
    {
        CellManager?.ReportCellDestroyed(coord);
    }
    
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