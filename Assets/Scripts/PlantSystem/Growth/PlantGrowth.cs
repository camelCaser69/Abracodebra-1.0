// REWORKED FILE: Assets/Scripts/PlantSystem/Growth/PlantGrowth.cs
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using WegoSystem;
using Abracodabra.Genes;
using Abracodabra.Genes.Templates;
using Abracodabra.Genes.Runtime;
using Abracodabra.Genes.Core;

namespace Abracodabra.Genes
{
    public enum PlantState
    {
        Initializing,
        Growing,
        Mature
    }

    public class PlantGrowth : MonoBehaviour, ITickUpdateable
    {
        public static readonly List<PlantGrowth> AllActivePlants = new List<PlantGrowth>();

        [Header("Configuration")]
        public SeedTemplate seedTemplate;
        public PlantGeneRuntimeState geneRuntimeState { get; private set; }
        public PlantSequenceExecutor sequenceExecutor { get; private set; }

        [Header("Systems")]
        public PlantCellManager CellManager { get; private set; }
        public PlantGrowthLogic GrowthLogic { get; private set; }
        public PlantEnergySystem EnergySystem { get; private set; }
        public PlantVisualManager VisualManager { get; private set; }

        [Header("Plant Configuration")]
        [SerializeField] public float cellSpacing = 0.08f;
        [SerializeField] GameObject seedCellPrefab;
        [SerializeField] GameObject stemCellPrefab;
        [SerializeField] GameObject leafCellPrefab;
        [SerializeField] GameObject berryCellPrefab;
        [SerializeField] PlantShadowController shadowController;
        [SerializeField] PlantOutlineController outlineController;
        [SerializeField] GameObject outlinePartPrefab;
        [SerializeField] bool enableOutline = true;

        [Header("Stat Modifiers (Modified by Passive Genes)")]
        public float growthSpeedMultiplier = 1f;
        public float energyGenerationMultiplier = 1f;
        public float energyStorageMultiplier = 1f;
        public float fruitYieldMultiplier = 1f;
        public float poopAbsorptionRadius = 3f;
        public float poopAbsorptionEfficiency = 1f;
        public int minHeight = 3;
        public int maxHeight = 5;
        public int leafDensity = 2;
        public int leafGap = 1;

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
            geneRuntimeState = template.CreateRuntimeState();
            
            // Initialize energy system with template values
            EnergySystem.MaxEnergy = geneRuntimeState.maxEnergy * energyStorageMultiplier;
            EnergySystem.CurrentEnergy = EnergySystem.MaxEnergy;
            
            // Initialize growth logic with template values
            GrowthLogic.PhotosynthesisEfficiencyPerLeaf = template.energyRegenRate * energyGenerationMultiplier;
            GrowthLogic.TargetStemLength = minHeight; // Will be modified by passive genes
            
            // Setup sequence executor
            sequenceExecutor = GetComponent<PlantSequenceExecutor>();
            if (sequenceExecutor == null)
            {
                sequenceExecutor = gameObject.AddComponent<PlantSequenceExecutor>();
            }

            sequenceExecutor.plantGrowth = this;
            sequenceExecutor.InitializeWithTemplate(template);

            // Apply passive genes (this will modify our stat multipliers)
            GrowthLogic.CalculateAndApplyPassiveStats();
            
            // Spawn initial seed cell
            CellManager.SpawnCellVisual(PlantCellType.Seed, Vector2Int.zero);
            
            // Start growing!
            CurrentState = PlantState.Growing;

            if (TickManager.Instance != null)
            {
                TickManager.Instance.RegisterTickUpdateable(this);
            }

            Debug.Log($"Plant '{gameObject.name}' initialized from template '{template.templateName}'. State: {CurrentState}");
        }

        public void OnTickUpdate(int currentTick)
        {
            EnergySystem.OnTickUpdate();
            
            // Simulate growth (simplified for now)
            if (CurrentState == PlantState.Growing && CellManager.cells.Count < maxHeight * 2)
            {
                // Simple growth logic - this would be more complex in real game
                if (Random.value < 0.1f * growthSpeedMultiplier)
                {
                    GrowSomething();
                }
            }
        }
        
        void GrowSomething()
        {
            // Simplified growth - just add a stem or leaf
            int currentHeight = CellManager.cells.Count(c => c.Value == PlantCellType.Stem);
            if (currentHeight < maxHeight)
            {
                CellManager.SpawnCellVisual(PlantCellType.Stem, new Vector2Int(0, currentHeight + 1));
                
                // Add leaves around stem based on leaf density
                if (currentHeight % leafGap == 0)
                {
                    for (int i = 0; i < leafDensity; i++)
                    {
                        int xOffset = (i % 2 == 0) ? -1 : 1;
                        var leafPos = new Vector2Int(xOffset, currentHeight + 1);
                        var leafObj = CellManager.SpawnCellVisual(PlantCellType.Leaf, leafPos);
                        
                        // Tag leaves as fruit spawn points
                        if (leafObj != null)
                        {
                            leafObj.tag = "FruitSpawn";
                        }
                    }
                }
            }
            else
            {
                CurrentState = PlantState.Mature;
            }
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
}