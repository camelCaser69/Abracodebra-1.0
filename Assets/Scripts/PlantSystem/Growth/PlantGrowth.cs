using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using WegoSystem;
using Abracodabra.Genes.Templates;
using Abracodabra.Genes.Runtime;
using Abracodabra.Genes.Core;
using Abracodabra.Genes.Services;

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

        public SeedTemplate seedTemplate { get; private set; } // Can still be useful for reference
        public PlantGeneRuntimeState geneRuntimeState { get; set; }
        public PlantSequenceExecutor sequenceExecutor { get; private set; }

        public PlantCellManager CellManager { get; private set; }
        public PlantGrowthLogic GrowthLogic { get; private set; }
        public PlantEnergySystem EnergySystem { get; private set; }
        public PlantVisualManager VisualManager { get; private set; }

        [Header("Cell Configuration")]
        [SerializeField] public float cellSpacing = 0.08f;
        [SerializeField] private GameObject seedCellPrefab;
        [SerializeField] private GameObject stemCellPrefab;
        [SerializeField] private GameObject leafCellPrefab;
        [SerializeField] private GameObject berryCellPrefab;

        [Header("Visuals")]
        [SerializeField] private PlantShadowController shadowController;
        [SerializeField] private PlantOutlineController outlineController;
        [SerializeField] private GameObject outlinePartPrefab;
        [SerializeField] private bool enableOutline = true;
        [SerializeField] private FoodType leafFoodType;

        [Header("Passive Stat Multipliers")]
        public float growthSpeedMultiplier = 1f;
        public float energyGenerationMultiplier = 1f;
        public float energyStorageMultiplier = 1f;
        public float fruitYieldMultiplier = 1f;
        
        [Header("Growth Parameters")]
        public float poopAbsorptionRadius = 3f;
        public float poopAbsorptionEfficiency = 1f;
        public int minHeight = 3;
        public int maxHeight = 5;
        public int leafDensity = 2;
        public int leafGap = 1;

        public PlantState CurrentState { get; set; } = PlantState.Initializing;

        private IDeterministicRandom _deterministicRandom;

        void Awake()
        {
            AllActivePlants.Add(this);
            CellManager = new PlantCellManager(this, seedCellPrefab, stemCellPrefab, leafCellPrefab, berryCellPrefab, cellSpacing, leafFoodType);
            GrowthLogic = new PlantGrowthLogic(this);
            EnergySystem = new PlantEnergySystem(this);
            VisualManager = new PlantVisualManager(this, shadowController, null, outlineController, outlinePartPrefab, enableOutline);

            _deterministicRandom = GeneServices.Get<IDeterministicRandom>();
            if (_deterministicRandom == null)
            {
                Debug.LogError($"[{nameof(PlantGrowth)}] could not retrieve IDeterministicRandom service! Growth will be non-deterministic.", this);
            }
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

        // MODIFIED: This method now accepts the fully-configured state.
        public void InitializeWithState(PlantGeneRuntimeState state)
        {
            if (state == null || state.template == null)
            {
                Debug.LogError($"Cannot initialize plant on '{gameObject.name}': Provided state or its template is null.", this);
                Destroy(gameObject);
                return;
            }

            this.seedTemplate = state.template;
            this.geneRuntimeState = state;

            sequenceExecutor = GetComponent<PlantSequenceExecutor>();
            if (sequenceExecutor == null)
            {
                sequenceExecutor = gameObject.AddComponent<PlantSequenceExecutor>();
            }
            sequenceExecutor.plantGrowth = this;

            GrowthLogic.CalculateAndApplyPassiveStats();

            EnergySystem.MaxEnergy = geneRuntimeState.template.maxEnergy * energyStorageMultiplier;
            EnergySystem.CurrentEnergy = EnergySystem.MaxEnergy;
            GrowthLogic.PhotosynthesisEfficiencyPerLeaf = seedTemplate.energyRegenRate * energyGenerationMultiplier;
            
            // The executor now gets the state directly, not a template to create a state from.
            sequenceExecutor.runtimeState = this.geneRuntimeState;
            sequenceExecutor.StartExecution();

            CellManager.SpawnCellVisual(PlantCellType.Seed, Vector2Int.zero);
            CurrentState = PlantState.Growing;

            if (TickManager.Instance != null)
            {
                TickManager.Instance.RegisterTickUpdateable(this);
            }

            Debug.Log($"Plant '{gameObject.name}' initialized from template '{seedTemplate.templateName}'. State: {CurrentState}");
        }

        public void OnTickUpdate(int currentTick)
        {
            EnergySystem.OnTickUpdate();

            if (CurrentState == PlantState.Growing && CellManager.cells.Count < maxHeight * 2)
            {
                float randomValue = (_deterministicRandom != null) ? _deterministicRandom.Range(0f, 1f) : Random.value;
                if (randomValue < 0.1f * growthSpeedMultiplier)
                {
                    GrowSomething();
                }
            }
        }

        void GrowSomething()
        {
            // FIX: The lambda expression should directly compare the dictionary's value (c.Value) to the enum.
            int currentHeight = CellManager.cells.Count(c => c.Value == PlantCellType.Stem);
            if (currentHeight < maxHeight)
            {
                CellManager.SpawnCellVisual(PlantCellType.Stem, new Vector2Int(0, currentHeight + 1));

                if (currentHeight % leafGap == 0)
                {
                    for (int i = 0; i < leafDensity; i++)
                    {
                        int xOffset = (i % 2 == 0) ? -1 : 1;
                        var leafPos = new Vector2Int(xOffset, currentHeight + 1);
                        var leafObj = CellManager.SpawnCellVisual(PlantCellType.Leaf, leafPos);

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