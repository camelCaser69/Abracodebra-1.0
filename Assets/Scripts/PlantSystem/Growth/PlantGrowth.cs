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

        // Add this at the top of the class, replacing the existing cellSpacingInPixels and cellSpacing
        [Header("Plant Grid Settings")]
        [SerializeField] public float cellSizeInPixels = 6f; // Size of each plant cell in pixels
    
        public float GetCellSpacingInWorldUnits() {
            // Get the actual pixel-to-world conversion
            float pixelsPerUnit = 6f; // Default fallback
    
            if (ResolutionManager.HasInstance && ResolutionManager.Instance.CurrentPPU > 0) {
                pixelsPerUnit = ResolutionManager.Instance.CurrentPPU;
            }
    
            // Convert pixel size to world units
            return cellSizeInPixels / pixelsPerUnit;
        }

// Keep this for backward compatibility but mark as obsolete
        [System.Obsolete("Use GetCellSpacingInWorldUnits() instead")]
        public float cellSpacing {
            get { return GetCellSpacingInWorldUnits(); }
        }

// Add this property for convenience
        public float cellSpacingInPixels {
            get { return cellSizeInPixels; }
            set { cellSizeInPixels = value; }
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

        void GrowSomething() {
            int currentHeight = CellManager.cells.Count(c => c.Value == PlantCellType.Stem);
    
            if (currentHeight < maxHeight) {
                // Grow a new stem segment
                Vector2Int stemPos = new Vector2Int(0, currentHeight + 1);
                CellManager.SpawnCellVisual(PlantCellType.Stem, stemPos);
        
                // Check if we should add leaves at this height
                if (currentHeight > 0 && currentHeight % leafGap == 0) {
                    // Place leaves at the SAME height as the current stem segment
                    int leafY = currentHeight; // Not currentHeight + 1
            
                    for (int i = 0; i < leafDensity; i++) {
                        // Calculate leaf positions to avoid overlap
                        // Alternating pattern: -1, +1, -2, +2, etc.
                        int leafOffset = (i / 2) + 1;
                        int xOffset = (i % 2 == 0) ? -leafOffset : leafOffset;
                
                        var leafPos = new Vector2Int(xOffset, leafY);
                
                        // Only place leaf if position is empty
                        if (!CellManager.HasCellAt(leafPos)) {
                            var leafObj = CellManager.SpawnCellVisual(PlantCellType.Leaf, leafPos);
                            if (leafObj != null) {
                                leafObj.tag = "FruitSpawn";
                            }
                        }
                    }
                }
            }
            else {
                CurrentState = PlantState.Mature;
            }
        }
        
#if UNITY_EDITOR
        void OnDrawGizmos() {
            if (!Application.isPlaying) return;
            if (CellManager == null || CellManager.cells == null) return;
    
            float spacing = GetCellSpacingInWorldUnits();
    
            // Draw occupied cells
            foreach (var cell in CellManager.cells) {
                Vector2 cellWorldPos = (Vector2)transform.position + (Vector2)cell.Key * spacing;
        
                // Color based on cell type
                switch(cell.Value) {
                    case PlantCellType.Stem:
                        Gizmos.color = new Color(0, 1, 0, 0.5f); // Green
                        break;
                    case PlantCellType.Leaf:
                        Gizmos.color = new Color(1, 1, 0, 0.5f); // Yellow
                        break;
                    case PlantCellType.Seed:
                        Gizmos.color = new Color(1, 1, 1, 0.5f); // White
                        break;
                    case PlantCellType.Fruit:
                        Gizmos.color = new Color(1, 0, 0, 0.5f); // Red
                        break;
                    default:
                        Gizmos.color = new Color(0.5f, 0.5f, 0.5f, 0.5f); // Gray
                        break;
                }
        
                // Draw cell cube
                Gizmos.DrawCube(cellWorldPos, Vector3.one * spacing * 0.9f);
        
                // Draw cell outline
                Gizmos.color = new Color(Gizmos.color.r, Gizmos.color.g, Gizmos.color.b, 1f);
                Gizmos.DrawWireCube(cellWorldPos, Vector3.one * spacing);
            }
    
            // Draw grid reference lines
            Gizmos.color = new Color(0, 1, 0, 0.1f);
            for (int x = -5; x <= 5; x++) {
                for (int y = 0; y <= maxHeight; y++) {
                    Vector2 gridPos = (Vector2)transform.position + new Vector2(x, y) * spacing;
                    Gizmos.DrawWireCube(gridPos, Vector3.one * spacing);
                }
            }
    
            // Draw plant origin
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(transform.position, spacing * 0.25f);
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