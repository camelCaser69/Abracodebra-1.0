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
        [SerializeField] public float cellSpacingInPixels = 6f; // Exposed to inspector for easy tweaking

        public float cellSpacing {
            get {
                // Always use a fixed world unit size that matches our pixel grid
                // This ensures consistent spacing regardless of PPU changes
                return cellSpacingInPixels / 6f; // 6 PPU is our base reference
            }
        }

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

        private void Awake()
        {
            AllActivePlants.Add(this);

            // The calculation is now handled by the cellSpacing property, so Awake is simpler.
            CellManager = new PlantCellManager(this, seedCellPrefab, stemCellPrefab, leafCellPrefab, berryCellPrefab, leafFoodType);
            GrowthLogic = new PlantGrowthLogic(this);
            EnergySystem = new PlantEnergySystem(this);
            VisualManager = new PlantVisualManager(this, shadowController, null, outlineController, outlinePartPrefab, enableOutline);

            _deterministicRandom = GeneServices.Get<IDeterministicRandom>();
            if (_deterministicRandom == null)
            {
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

        private void GrowSomething()
        {
            int currentHeight = CellManager.cells.Count(c => c.Value == PlantCellType.Stem);
            if (currentHeight < maxHeight)
            {
                // Spawn stem at center column
                Vector2Int stemPos = new Vector2Int(0, currentHeight + 1);
                CellManager.SpawnCellVisual(PlantCellType.Stem, stemPos);

                if (currentHeight > 0 && currentHeight % leafGap == 0) {
                    // Calculate leaf positions to ensure no overlap
                    // Leaves should be placed at the same height as the stem, but offset horizontally
                    int leafY = currentHeight; // Same height as current stem, not +1
    
                    for (int i = 0; i < leafDensity; i++) {
                        // Alternate left and right, starting from position 1
                        int xOffset = (i % 2 == 0) ? -(i/2 + 1) : (i/2 + 1);
                        var leafPos = new Vector2Int(xOffset, leafY);

                        // Check if position is available (no overlap)
                        if (!CellManager.HasCellAt(leafPos)) {
                            var leafObj = CellManager.SpawnCellVisual(PlantCellType.Leaf, leafPos);
                            if (leafObj != null) {
                                leafObj.tag = "FruitSpawn";
                            }
                        }
                    }
                }
            }
            else
            {
                CurrentState = PlantState.Mature;
            }
        }
        
#if UNITY_EDITOR
        void OnDrawGizmos() {
            if (!Application.isPlaying) return;
    
            // Draw the plant's cell grid for debugging
            Gizmos.color = Color.green;
            float worldUnitsPerCell = cellSpacingInPixels / 6f;
    
            // Draw occupied cells
            if (CellManager != null && CellManager.cells != null) {
                foreach (var cell in CellManager.cells) {
                    Vector2 cellWorldPos = (Vector2)transform.position + (Vector2)cell.Key * worldUnitsPerCell;
            
                    // Draw cell boundary
                    Gizmos.color = cell.Value == PlantCellType.Stem ? Color.green : 
                        cell.Value == PlantCellType.Leaf ? Color.yellow :
                        cell.Value == PlantCellType.Seed ? Color.white : Color.red;
            
                    Gizmos.DrawWireCube(cellWorldPos, Vector3.one * worldUnitsPerCell * 0.9f);
                }
            }
    
            // Draw grid lines for reference
            Gizmos.color = new Color(0, 1, 0, 0.2f);
            for (int x = -5; x <= 5; x++) {
                for (int y = -1; y <= maxHeight + 1; y++) {
                    Vector2 gridPos = (Vector2)transform.position + new Vector2(x, y) * worldUnitsPerCell;
                    Gizmos.DrawWireCube(gridPos, Vector3.one * worldUnitsPerCell);
                }
            }
        }
#endif

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