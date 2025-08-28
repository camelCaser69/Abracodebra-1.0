using System.Collections.Generic;
using UnityEngine;
using WegoSystem;
using Abracodabra.Genes.Templates;
using Abracodabra.Genes.Runtime;
using Abracodabra.Genes.Core;
using Abracodabra.Genes.Services;
using System.Linq;

namespace Abracodabra.Genes {
    public enum PlantState {
        Initializing,
        Growing,
        Mature
    }

    public class PlantGrowth : MonoBehaviour, ITickUpdateable {
        public static readonly List<PlantGrowth> AllActivePlants = new List<PlantGrowth>();

        public SeedTemplate seedTemplate { get; set; }
        public PlantGeneRuntimeState geneRuntimeState { get; set; }
        public PlantSequenceExecutor sequenceExecutor { get; set; }

        public PlantCellManager CellManager { get; set; }
        public PlantGrowthLogic GrowthLogic { get; set; }
        public PlantEnergySystem EnergySystem { get; set; }
        public PlantVisualManager VisualManager { get; set; }

        // ===== FIXED SPACING SYSTEM =====
        [Header("Plant Grid Settings")]
        [SerializeField] public float desiredPixelsPerCell = 1f; // Each cell = 1 "fake pixel"
        
        // This property calculates the correct world spacing based on current PPU
        public float GetCellWorldSpacing() {
            if (ResolutionManager.HasInstance && ResolutionManager.Instance.CurrentPPU > 0) {
                return desiredPixelsPerCell / ResolutionManager.Instance.CurrentPPU;
            }
            return desiredPixelsPerCell / 16f; // Fallback: 1/16 world unit per cell
        }
        
        // Backward compatibility properties
        public float cellSpacingInPixels => desiredPixelsPerCell;
        public float cellSpacing => GetCellWorldSpacing();
        // ===== END SPACING SYSTEM =====

        [SerializeField] GameObject seedCellPrefab;
        [SerializeField] GameObject stemCellPrefab;
        [SerializeField] GameObject leafCellPrefab;
        [SerializeField] GameObject berryCellPrefab;

        [SerializeField] PlantShadowController shadowController;
        [SerializeField] PlantOutlineController outlineController;
        [SerializeField] GameObject outlinePartPrefab;
        [SerializeField] bool enableOutline = true;
        [SerializeField] FoodType leafFoodType;

        public float growthSpeedMultiplier = 1f;
        public float energyGenerationMultiplier = 1f;
        public float energyStorageMultiplier = 1f;
        public float fruitYieldMultiplier = 1f;

        public float baseGrowthChance;
        public int minHeight;
        public int maxHeight;
        public int leafDensity;
        public int leafGap;

        public PlantState CurrentState { get; set; } = PlantState.Initializing;

        IDeterministicRandom _deterministicRandom;

        void Awake() {
            AllActivePlants.Add(this);

            CellManager = new PlantCellManager(this, seedCellPrefab, stemCellPrefab, leafCellPrefab, berryCellPrefab, leafFoodType);
            GrowthLogic = new PlantGrowthLogic(this);
            EnergySystem = new PlantEnergySystem(this);
            VisualManager = new PlantVisualManager(this, shadowController, null, outlineController, outlinePartPrefab, enableOutline);

            _deterministicRandom = GeneServices.Get<IDeterministicRandom>();
            if (_deterministicRandom == null) {
                Debug.LogError($"[{nameof(PlantGrowth)}] could not retrieve IDeterministicRandom service! Growth will be non-deterministic.", this);
            }
        }

        void OnDestroy() {
            AllActivePlants.Remove(this);
            var tickManager = TickManager.Instance;
            if (tickManager != null) {
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

    GrowthLogic.CalculateAndApplyPassiveStats();

    EnergySystem.MaxEnergy = geneRuntimeState.template.maxEnergy * energyStorageMultiplier;
    EnergySystem.CurrentEnergy = EnergySystem.MaxEnergy;
    EnergySystem.BaseEnergyPerLeaf = seedTemplate.energyRegenRate;

    sequenceExecutor.runtimeState = this.geneRuntimeState;
    sequenceExecutor.StartExecution();

    CellManager.SpawnCellVisual(PlantCellType.Seed, Vector2Int.zero);
    
    // Keep in Initializing state briefly to prevent immediate growth
    CurrentState = PlantState.Initializing;
    
    // Start a coroutine to transition to Growing state after a short delay
    StartCoroutine(DelayedGrowthStart());

    if (TickManager.Instance != null)
    {
        TickManager.Instance.RegisterTickUpdateable(this);
    }

    Debug.Log($"Plant '{gameObject.name}' initialized from template '{seedTemplate.templateName}'. State: {CurrentState}");
}

        private System.Collections.IEnumerator DelayedGrowthStart()
        {
            yield return new WaitForSeconds(0.5f);
            CurrentState = PlantState.Growing;
            Debug.Log($"Plant '{gameObject.name}' transitioned to Growing state");
        }

        public void OnTickUpdate(int currentTick)
        {
            EnergySystem.OnTickUpdate();
    
            // Update the visual display on each tick
            if (VisualManager != null)
            {
                VisualManager.UpdateUI();
            }

            if (CurrentState == PlantState.Growing)
            {
                float randomValue = (_deterministicRandom != null) ? 
                    _deterministicRandom.Range(0f, 1f) : Random.value;
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
                Vector2Int stemPos = new Vector2Int(0, currentHeight + 1);
                CellManager.SpawnCellVisual(PlantCellType.Stem, stemPos);

                if (currentHeight > 0 && currentHeight % leafGap == 0)
                {
                    int leafY = currentHeight;

                    for (int i = 0; i < leafDensity; i++)
                    {
                        int leafOffset = (i / 2) + 1;
                        int xOffset = (i % 2 == 0) ? -leafOffset : leafOffset;

                        var leafPos = new Vector2Int(xOffset, leafY);

                        if (!CellManager.HasCellAt(leafPos))
                        {
                            var leafObj = CellManager.SpawnCellVisual(PlantCellType.Leaf, leafPos);
                            if (leafObj != null)
                            {
                                // Add a component to mark fruit spawn points instead of using tags
                                leafObj.AddComponent<FruitSpawnPoint>();
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

        public void HandleBeingEaten(AnimalController eater, PlantCell eatenCell) {
            Debug.Log($"{eater.SpeciesName} ate cell at {eatenCell.GridCoord} on plant {name}");
        }

        public void ReportCellDestroyed(Vector2Int coord) {
            CellManager?.ReportCellDestroyed(coord);
        }

        public GameObject GetCellGameObjectAt(Vector2Int coord) {
            return CellManager?.GetCellGameObjectAt(coord);
        }

        public Transform[] GetFruitSpawnPoints()
        {
            // Look for FruitSpawnPoint components instead of tags
            var spawnPoints = GetComponentsInChildren<FruitSpawnPoint>();
            return spawnPoints.Select(sp => sp.transform).ToArray();
        }

#if UNITY_EDITOR
        void OnDrawGizmos() {
            if (!Application.isPlaying) return;
            if (CellManager == null || CellManager.cells == null) return;

            float spacing = GetCellWorldSpacing();

            // Draw plant cells
            foreach (var cell in CellManager.cells) {
                Vector2 cellWorldPos = (Vector2)transform.position + (Vector2)cell.Key * spacing;

                // Color code by cell type
                switch(cell.Value) {
                    case PlantCellType.Stem:
                        Gizmos.color = new Color(0, 1, 0, 0.5f);
                        break;
                    case PlantCellType.Leaf:
                        Gizmos.color = new Color(1, 1, 0, 0.5f);
                        break;
                    case PlantCellType.Seed:
                        Gizmos.color = new Color(1, 1, 1, 0.5f);
                        break;
                    case PlantCellType.Fruit:
                        Gizmos.color = new Color(1, 0, 0, 0.5f);
                        break;
                }

                Gizmos.DrawCube(cellWorldPos, Vector3.one * spacing * 0.9f);
                Gizmos.color = new Color(Gizmos.color.r, Gizmos.color.g, Gizmos.color.b, 1f);
                Gizmos.DrawWireCube(cellWorldPos, Vector3.one * spacing);
            }

            // Draw tile grid overlay for alignment checking
            Gizmos.color = new Color(0, 1, 1, 0.2f); // Cyan for tile grid
            Vector3Int tilePos = GridPositionManager.Instance?.WorldToGrid(transform.position).ToVector3Int() ?? Vector3Int.zero;
            Vector3 tileWorldPos = GridPositionManager.Instance?.GridToWorld(new GridPosition(tilePos)) ?? transform.position;
            Gizmos.DrawWireCube(tileWorldPos, Vector3.one * 1f); // 1 world unit = 1 tile

            // Show debug info
            UnityEditor.Handles.Label(
                transform.position + Vector3.up * (maxHeight + 1) * spacing,
                $"Cell Spacing: {spacing:F4} wu\nPPU: {(ResolutionManager.HasInstance ? ResolutionManager.Instance.CurrentPPU.ToString() : "Unknown")}\nTile Aligned: {Mathf.Approximately(spacing, 1f)}"
            );
        }
#endif
    }
}