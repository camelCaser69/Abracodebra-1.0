using UnityEngine;
using System.Linq;
using WegoSystem;
using Abracodabra.Genes.Templates;
using Abracodabra.Genes.Runtime;
using Abracodabra.Genes.Core;
using Abracodabra.Genes.Services;
using System.Collections.Generic;

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

        public SeedTemplate seedTemplate { get; set; }
        public PlantGeneRuntimeState geneRuntimeState { get; set; }
        public PlantSequenceExecutor sequenceExecutor { get; set; }

        public PlantCellManager CellManager { get; set; }
        public PlantGrowthLogic GrowthLogic { get; set; }
        public PlantEnergySystem EnergySystem { get; set; }
        public PlantVisualManager VisualManager { get; set; }

        [Header("Visuals & Prefabs")]
        [SerializeField] 
        [Tooltip("Size of each plant cell in pixels (e.g., 6 = 6x6 pixel cells)")]
        [Range(1, 16)] // Reasonable range for pixel sizes
        public float cellSpacing = 6f; // Changed from 0.08 to 6 (pixels)
        [SerializeField] private GameObject seedCellPrefab;
        [SerializeField] private GameObject stemCellPrefab;
        [SerializeField] private GameObject leafCellPrefab;
        [SerializeField] private GameObject berryCellPrefab;

        [Header("Controllers")]
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

        [Header("Base Growth Parameters")]
        public float baseGrowthChance;
        public int minHeight;
        public int maxHeight;
        public int leafDensity;
        public int leafGap;

        public PlantState CurrentState { get; set; } = PlantState.Initializing;

        private IDeterministicRandom _deterministicRandom;

        void Awake() {
            AllActivePlants.Add(this);
    
            // Cell spacing should be in pixels, then converted to world units when needed
            // The inspector value (e.g., 6) represents the pixel size of each plant cell
            float cellSpacingInPixels = cellSpacing; // This is now treated as pixels (e.g., 6 pixels)
    
            // Convert pixel spacing to world units for use in positioning
            if (ResolutionManager.HasInstance && ResolutionManager.Instance.CurrentPPU > 0) {
                cellSpacing = cellSpacingInPixels / ResolutionManager.Instance.CurrentPPU;
                Debug.Log($"[{nameof(PlantGrowth)}] Cell spacing set to {cellSpacing} world units ({cellSpacingInPixels} pixels at {ResolutionManager.Instance.CurrentPPU} PPU)");
            } else {
                // Fallback: assume default PPU if ResolutionManager not available
                float defaultPPU = 6f;
                cellSpacing = cellSpacingInPixels / defaultPPU;
                Debug.LogWarning($"[{nameof(PlantGrowth)}] ResolutionManager not available, using default PPU of {defaultPPU}. Cell spacing: {cellSpacing} world units");
            }
    
            CellManager = new PlantCellManager(this, seedCellPrefab, stemCellPrefab, leafCellPrefab, berryCellPrefab, cellSpacing, leafFoodType);
            GrowthLogic = new PlantGrowthLogic(this);
            EnergySystem = new PlantEnergySystem(this);
            VisualManager = new PlantVisualManager(this, shadowController, null, outlineController, outlinePartPrefab, enableOutline);

            _deterministicRandom = GeneServices.Get<IDeterministicRandom>();
            if (_deterministicRandom == null) {
                Debug.LogError($"[{nameof(PlantGrowth)}] could not retrieve IDeterministicRandom service! Growth will be non-deterministic.", this);
            }
        }

        private void OnDestroy()
        {
            AllActivePlants.Remove(this);
            var tickManager = TickManager.Instance;
            if (tickManager != null)
            {
                tickManager.UnregisterTickUpdateable(this);
            }
        }

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

            // Apply base stats from template
            this.baseGrowthChance = seedTemplate.baseGrowthChance;
            this.minHeight = seedTemplate.minHeight;
            this.maxHeight = seedTemplate.maxHeight;
            this.leafDensity = seedTemplate.leafDensity;
            this.leafGap = seedTemplate.leafGap;

            sequenceExecutor = GetComponent<PlantSequenceExecutor>();
            if (sequenceExecutor == null)
            {
                sequenceExecutor = gameObject.AddComponent<PlantSequenceExecutor>();
            }
            sequenceExecutor.plantGrowth = this;

            // Calculate passive gene effects
            GrowthLogic.CalculateAndApplyPassiveStats();

            // Setup systems with final stats
            EnergySystem.MaxEnergy = geneRuntimeState.template.maxEnergy * energyStorageMultiplier;
            EnergySystem.CurrentEnergy = EnergySystem.MaxEnergy;
            EnergySystem.BaseEnergyPerLeaf = seedTemplate.energyRegenRate;

            // Start the gene sequence executor
            sequenceExecutor.runtimeState = this.geneRuntimeState;
            sequenceExecutor.StartExecution();

            // Initial state
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

            if (CurrentState == PlantState.Growing)
            {
                float randomValue = (_deterministicRandom != null) ? _deterministicRandom.Range(0f, 1f) : Random.value;
                if (randomValue < baseGrowthChance * growthSpeedMultiplier)
                {
                    GrowSomething();
                }
            }
        }

        void GrowSomething() {
            int currentHeight = CellManager.cells.Count(c => c.Value == PlantCellType.Stem);
            if (currentHeight < maxHeight) {
                // Spawn stem at center column
                Vector2Int stemPos = new Vector2Int(0, currentHeight + 1);
                CellManager.SpawnCellVisual(PlantCellType.Stem, stemPos);

                // Check if we should spawn leaves at this height
                if (currentHeight > 0 && currentHeight % leafGap == 0) {
                    // Spawn leaves to the sides of the stem, not on top of it
                    for (int i = 0; i < leafDensity; i++) {
                        // Alternate between left and right, with increasing distance
                        int leafIndex = i + 1;
                        int xOffset = (i % 2 == 0) ? -leafIndex : leafIndex;
                        var leafPos = new Vector2Int(xOffset, currentHeight + 1);
                
                        // Only spawn if position is not occupied
                        if (!CellManager.HasCellAt(leafPos)) {
                            var leafObj = CellManager.SpawnCellVisual(PlantCellType.Leaf, leafPos);
                            if (leafObj != null) {
                                leafObj.tag = "FruitSpawn";
                            }
                        }
                    }
                }
            } else {
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