// FILE: Assets/Scripts/PlantSystem/Growth/PlantGrowth.cs
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Abracodabra.Genes.Components;
using WegoSystem;
using Abracodabra.Genes.Templates;
using Abracodabra.Genes.Runtime;
using Abracodabra.Genes.Core;
using Abracodabra.Genes.Implementations;
using Abracodabra.Genes.Services;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Abracodabra.Genes {
    public enum PlantState {
        Initializing,
        Growing,
        Mature,
        Dead
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

        [Header("Rendering Configuration")]
        [SerializeField] public float desiredPixelsPerCell = 1f;

        public float GetCellWorldSpacing() {
            if (ResolutionManager.HasInstance && ResolutionManager.Instance.CurrentPPU > 0) {
                return desiredPixelsPerCell / ResolutionManager.Instance.CurrentPPU;
            }
            return desiredPixelsPerCell / 16f;
        }

        public float cellSpacingInPixels => desiredPixelsPerCell;
        public float cellSpacing => GetCellWorldSpacing();

        [Header("Cell Prefabs")]
        [SerializeField] GameObject seedCellPrefab;
        [SerializeField] GameObject stemCellPrefab;
        [SerializeField] GameObject leafCellPrefab;
        [SerializeField] GameObject berryCellPrefab;

        [Header("Visual Controllers")]
        [SerializeField] PlantShadowController shadowController;
        [SerializeField] PlantOutlineController outlineController;
        [SerializeField] GameObject outlinePartPrefab;
        [SerializeField] bool enableOutline = true;
        [SerializeField] FoodType leafFoodType;

        [Header("Fruit Spawning Logic")]
        [SerializeField] bool allowFruitsAroundLeaves = false;
        [SerializeField] int fruitSearchRadius = 2;

        [Header("Energy Logic")]
        [SerializeField] public bool rechargeEnergyDuringGrowth = false;

        [HideInInspector] public float growthSpeedMultiplier = 1f;
        [HideInInspector] public float energyGenerationMultiplier = 1f;
        [HideInInspector] public float energyStorageMultiplier = 1f;
        [HideInInspector] public float fruitYieldMultiplier = 1f;
        [HideInInspector] public float defenseMultiplier = 1f;

        [Header("Plant HP")]
        public float maxHP = 50f;
        [HideInInspector] public float currentHP;

        [HideInInspector] public float baseGrowthChance;
        [HideInInspector] public int minHeight;
        [HideInInspector] public int maxHeight;
        [HideInInspector] public int leafDensity;
        [HideInInspector] public int leafGap;

        public PlantState CurrentState { get; set; } = PlantState.Initializing;

        HashSet<Vector2Int> activeFruitPositions = new HashSet<Vector2Int>();
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

        public void InitializeWithState(PlantGeneRuntimeState state) {
            if (state == null || state.template == null) {
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
            if (sequenceExecutor == null) {
                sequenceExecutor = gameObject.AddComponent<PlantSequenceExecutor>();
            }
            sequenceExecutor.plantGrowth = this;

            GrowthLogic.CalculateAndApplyPassiveStats();

            EnergySystem.MaxEnergy = geneRuntimeState.template.maxEnergy * energyStorageMultiplier;
            EnergySystem.CurrentEnergy = geneRuntimeState.template.startingEnergy;
            EnergySystem.BaseEnergyPerLeaf = seedTemplate.energyRegenRate;

            sequenceExecutor.InitializeWithTemplate(this.geneRuntimeState);

            CellManager.SpawnCellVisual(PlantCellType.Seed, Vector2Int.zero);

            CurrentState = PlantState.Initializing;
            currentHP = maxHP;

            StartCoroutine(DelayedGrowthStart());

            if (TickManager.Instance != null) {
                TickManager.Instance.RegisterTickUpdateable(this);
            }

            Debug.Log($"Plant '{gameObject.name}' initialized from template '{seedTemplate.templateName}'. State: {CurrentState}");
        }

        IEnumerator DelayedGrowthStart() {
            yield return new WaitForSeconds(0.5f);
            CurrentState = PlantState.Growing;
            Debug.Log($"Plant '{gameObject.name}' transitioned to Growing state");
        }

        public void OnTickUpdate(int currentTick) {
            if (CurrentState == PlantState.Mature || rechargeEnergyDuringGrowth) {
                EnergySystem.OnTickUpdate();
            }

            if (sequenceExecutor != null) {
                sequenceExecutor.OnTickUpdate(currentTick);
            }

            if (VisualManager != null) {
                VisualManager.UpdateUI();
            }

            if (CurrentState == PlantState.Growing) {
                float randomValue = (_deterministicRandom != null) ?
                    _deterministicRandom.Range(0f, 1f) : Random.value;
                if (randomValue < baseGrowthChance * growthSpeedMultiplier) {
                    GrowSomething();
                }
            }
        }

        void GrowSomething() {
            int oldHeight = CellManager.cells.Count(c => c.Value == PlantCellType.Stem);

            if (oldHeight < maxHeight) {
                int newHeight = oldHeight + 1;
                Vector2Int stemPos = new Vector2Int(0, newHeight);
                CellManager.SpawnCellVisual(PlantCellType.Stem, stemPos);

                if (newHeight > 0 && newHeight % leafGap == 0) {
                    int leafY = newHeight;

                    for (int i = 0; i < leafDensity; i++) {
                        int leafOffset = (i / 2) + 1;
                        int xOffset = (i % 2 == 0) ? -leafOffset : leafOffset;

                        var leafPos = new Vector2Int(xOffset, leafY);

                        if (!CellManager.HasCellAt(leafPos)) {
                            CellManager.SpawnCellVisual(PlantCellType.Leaf, leafPos);
                        }
                    }
                }
            }
            else {
                CurrentState = PlantState.Mature;
            }
        }

        public void HandleBeingEaten(AnimalController eater, PlantCell eatenCell) {
            Debug.Log($"{eater.SpeciesName} ate cell at {eatenCell.GridCoord} on plant {name}");
            CellManager?.ReportCellDestroyed(eatenCell.GridCoord);
        }

        public void RegisterFruitPosition(Vector2Int coord) {
            if (!activeFruitPositions.Contains(coord)) {
                activeFruitPositions.Add(coord);
            }
        }

        public void ReportCellDestroyed(Vector2Int coord) {
            if (CellManager.cells.TryGetValue(coord, out var cellType) && cellType == PlantCellType.Fruit) {
                activeFruitPositions.Remove(coord);
            }
            CellManager?.ReportCellDestroyed(coord);
        }

        public GameObject GetCellGameObjectAt(Vector2Int coord) {
            return CellManager?.GetCellGameObjectAt(coord);
        }

        public Transform[] GetFruitSpawnPoints() {
            List<Transform> spawnPoints = new List<Transform>();
            List<Vector2Int> sourcePositions = GetFruitSourcePositions();
            HashSet<Vector2Int> emptyPositions = FindEmptyPositionsAround(sourcePositions);

            var availablePositions = emptyPositions.Where(pos => !activeFruitPositions.Contains(pos)).ToList();

            foreach (Vector2Int emptyPos in availablePositions) {
                GameObject tempSpawnPoint = CreateTemporarySpawnPoint(emptyPos);
                if (tempSpawnPoint != null) {
                    spawnPoints.Add(tempSpawnPoint.transform);
                }
            }

            if (spawnPoints.Count > 0) {
                StartCoroutine(CleanupTemporarySpawnPoints(spawnPoints, 0.1f));
            }

            return spawnPoints.ToArray();
        }

        List<Vector2Int> GetFruitSourcePositions() {
            List<Vector2Int> sourcePositions = new List<Vector2Int>();

            foreach (var kvp in CellManager.cells) {
                if (kvp.Value == PlantCellType.Stem) {
                    sourcePositions.Add(kvp.Key);
                }
            }

            if (allowFruitsAroundLeaves) {
                foreach (var kvp in CellManager.cells) {
                    if (kvp.Value == PlantCellType.Leaf) {
                        sourcePositions.Add(kvp.Key);
                    }
                }
            }

            return sourcePositions;
        }

        HashSet<Vector2Int> FindEmptyPositionsAround(List<Vector2Int> sourcePositions) {
            HashSet<Vector2Int> emptyPositions = new HashSet<Vector2Int>();

            foreach (Vector2Int sourcePos in sourcePositions) {
                for (int x = -fruitSearchRadius; x <= fruitSearchRadius; x++) {
                    for (int y = -fruitSearchRadius; y <= fruitSearchRadius; y++) {
                        if (x == 0 && y == 0) continue;

                        Vector2Int checkPos = sourcePos + new Vector2Int(x, y);

                        if (!CellManager.HasCellAt(checkPos)) {
                            float distance = Vector2Int.Distance(sourcePos, checkPos);
                            if (distance <= fruitSearchRadius) {
                                emptyPositions.Add(checkPos);
                            }
                        }
                    }
                }
            }

            return emptyPositions;
        }

        GameObject CreateTemporarySpawnPoint(Vector2Int gridPos) {
            float spacing = GetCellWorldSpacing();
            Vector3 worldPos = transform.position + new Vector3(gridPos.x * spacing, gridPos.y * spacing, 0);

            GameObject spawnPoint = new GameObject($"TempFruitSpawnPoint_{gridPos.x}_{gridPos.y}");
            spawnPoint.transform.position = worldPos;
            spawnPoint.transform.SetParent(transform);

            spawnPoint.AddComponent<TemporaryFruitSpawnMarker>();

            return spawnPoint;
        }

        IEnumerator CleanupTemporarySpawnPoints(List<Transform> spawnPoints, float delay) {
            yield return new WaitForSeconds(delay);

            foreach (Transform spawnPoint in spawnPoints) {
                if (spawnPoint != null && spawnPoint.GetComponent<TemporaryFruitSpawnMarker>() != null) {
                    Destroy(spawnPoint.gameObject);
                }
            }
        }

        public List<HarvestedItem> HarvestAllFruits() {
            var harvestedItems = new List<HarvestedItem>();
            if (CellManager == null) return harvestedItems;

            var fruitGameObjects = new List<GameObject>();
            foreach (var cell in CellManager.cells) {
                if (cell.Value == PlantCellType.Fruit) {
                    var fruitGO = GetCellGameObjectAt(cell.Key);
                    if (fruitGO != null && fruitGO.GetComponent<HarvestableTag>() != null) {
                        fruitGameObjects.Add(fruitGO);
                    }
                }
            }

            if (fruitGameObjects.Count == 0) {
                return harvestedItems;
            }

            foreach (var fruitGO in fruitGameObjects) {
                var fruitComponent = fruitGO.GetComponent<Fruit>();
                if (fruitComponent != null && fruitComponent.RepresentingItemDefinition != null) {
                    harvestedItems.Add(new HarvestedItem(
                        fruitComponent.RepresentingItemDefinition,
                        fruitComponent.DynamicProperties,
                        fruitComponent.PayloadGeneInstances
                    ));
                }

                Destroy(fruitGO);
            }

            Debug.Log($"[{name}] Harvested {harvestedItems.Count} items.");
            return harvestedItems;
        }

        public void TakeDamage(float amount) {
            if (CurrentState == PlantState.Dead) return;

            // Apply defense reduction: defenseMultiplier 1.3 = 30% damage reduction
            float reduction = Mathf.Clamp01(defenseMultiplier - 1f);
            float finalDamage = amount * (1f - reduction);

            currentHP -= finalDamage;
            currentHP = Mathf.Max(0f, currentHP);

            // Visual: brief red flash
            StartCoroutine(DamageFlash());

            if (currentHP <= 0f) {
                Die();
            }
        }

        private System.Collections.IEnumerator DamageFlash() {
            var renderers = GetComponentsInChildren<SpriteRenderer>();
            var originalColors = new Color[renderers.Length];

            for (int i = 0; i < renderers.Length; i++) {
                originalColors[i] = renderers[i].color;
                renderers[i].color = Color.red;
            }

            yield return new WaitForSeconds(0.15f);

            for (int i = 0; i < renderers.Length; i++) {
                if (renderers[i] != null)
                    renderers[i].color = originalColors[i];
            }
        }

        public void Die() {
            if (CurrentState == PlantState.Dead) return;

            CurrentState = PlantState.Dead;
            Debug.Log($"[PlantGrowth] Plant '{gameObject.name}' has died!");

            // Unregister from tick system
            if (TickManager.Instance != null) {
                TickManager.Instance.UnregisterTickUpdateable(this);
            }

            AllActivePlants.Remove(this);

            // Death visual: shrink and fade
            StartCoroutine(DeathAnimation());
        }

        private System.Collections.IEnumerator DeathAnimation() {
            float duration = 0.5f;
            float elapsed = 0f;
            Vector3 startScale = transform.localScale;

            var renderers = GetComponentsInChildren<SpriteRenderer>();

            while (elapsed < duration) {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                // Shrink Y scale
                transform.localScale = new Vector3(
                    startScale.x,
                    Mathf.Lerp(startScale.y, 0f, t),
                    startScale.z
                );

                // Fade out
                foreach (var r in renderers) {
                    if (r != null) {
                        Color c = r.color;
                        c.a = Mathf.Lerp(1f, 0f, t);
                        r.color = c;
                    }
                }

                yield return null;
            }

            Destroy(gameObject);
        }

        #if UNITY_EDITOR
        void OnDrawGizmos() {
            if (!Application.isPlaying) return;
            if (CellManager == null || CellManager.cells == null) return;

            float spacing = GetCellWorldSpacing();

            foreach (var cell in CellManager.cells) {
                Vector2 cellWorldPos = (Vector2)transform.position + (Vector2)cell.Key * spacing;

                switch (cell.Value) {
                    case PlantCellType.Stem: Gizmos.color = new Color(0, 1, 0, 0.5f); break;
                    case PlantCellType.Leaf: Gizmos.color = new Color(1, 1, 0, 0.5f); break;
                    case PlantCellType.Seed: Gizmos.color = new Color(1, 1, 1, 0.5f); break;
                    case PlantCellType.Fruit: Gizmos.color = new Color(1, 0, 0, 0.5f); break;
                }

                Gizmos.DrawCube(cellWorldPos, Vector3.one * spacing * 0.9f);
                Gizmos.color = new Color(Gizmos.color.r, Gizmos.color.g, Gizmos.color.b, 1f);
                Gizmos.DrawWireCube(cellWorldPos, Vector3.one * spacing);
            }

            Gizmos.color = new Color(0, 1, 1, 0.2f);
            Vector3Int tilePos = GridPositionManager.Instance?.WorldToGrid(transform.position).ToVector3Int() ?? Vector3Int.zero;
            Vector3 tileWorldPos = GridPositionManager.Instance?.GridToWorld(new GridPosition(tilePos)) ?? transform.position;
            Gizmos.DrawWireCube(tileWorldPos, Vector3.one * 1f);

            Handles.Label(
                transform.position + Vector3.up * (maxHeight + 1) * spacing,
                $"Cell Spacing: {spacing:F4} wu\nPPU: {(ResolutionManager.HasInstance ? ResolutionManager.Instance.CurrentPPU.ToString() : "Unknown")}\nTile Aligned: {Mathf.Approximately(spacing, 1f)}\nEnergy: {(EnergySystem != null ? EnergySystem.CurrentEnergy : 0):F0}"
            );
        }

        void OnDrawGizmosSelected() {
            if (!Application.isPlaying || CellManager == null) return;

            List<Vector2Int> sourcePositions = GetFruitSourcePositions();
            HashSet<Vector2Int> emptyPositions = FindEmptyPositionsAround(sourcePositions);

            float spacing = GetCellWorldSpacing();

            Gizmos.color = Color.green;
            foreach (Vector2Int sourcePos in sourcePositions) {
                Vector3 worldPos = transform.position + new Vector3(sourcePos.x * spacing, sourcePos.y * spacing, 0);
                Gizmos.DrawWireSphere(worldPos, spacing * 0.2f);
            }

            Gizmos.color = Color.cyan;
            foreach (Vector2Int emptyPos in emptyPositions) {
                Vector3 worldPos = transform.position + new Vector3(emptyPos.x * spacing, emptyPos.y * spacing, 0);
                Gizmos.DrawWireCube(worldPos, Vector3.one * spacing * 0.5f);
            }
        }
        #endif
    }
}