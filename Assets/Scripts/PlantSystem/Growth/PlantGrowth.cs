// FILE: Assets/Scripts/PlantSystem/Growth/PlantGrowth.cs
using System;
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

namespace Abracodabra.Genes
{
    public enum PlantState
    {
        Initializing,
        Growing,
        Mature,
        Withering,
        Dead
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

        [Header("Rendering Configuration")]
        [SerializeField] public float desiredPixelsPerCell = 1f;

        public float GetCellWorldSpacing()
        {
            if (ResolutionManager.HasInstance && ResolutionManager.Instance.CurrentPPU > 0)
            {
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

        [HideInInspector] public float leafDurabilityMultiplier = 1f;
        [HideInInspector] public float thornDamage = 0f;
        [HideInInspector] public float leafRegrowthRate = 0f;

        int witheringTicksRemaining = 0;
        const int WITHERING_DURATION = 3;

        int regrowthTickCounter = 0;

        List<Vector2Int> destroyedLeafPositions = new List<Vector2Int>();

        public int DestroyedLeafCount => destroyedLeafPositions.Count;

        public event Action<PlantGrowth, Vector2Int> OnLeafConsumed;

        public int MaxLeafCount => CellManager != null ? CellManager.LeafDataList.Count : 0;
        public int ActiveLeafCount => CellManager != null ? CellManager.GetActiveLeafCount() : 0;

        static readonly Color WitheringTint = new Color(0.6f, 0.4f, 0.2f);
        Dictionary<SpriteRenderer, Color> preWitheringColors;

        [HideInInspector] public float baseGrowthChance;
        [HideInInspector] public int minHeight;
        [HideInInspector] public int maxHeight;
        [HideInInspector] public int leafDensity;
        [HideInInspector] public int leafGap;

        public PlantState CurrentState { get; set; } = PlantState.Initializing;

        HashSet<Vector2Int> activeFruitPositions = new HashSet<Vector2Int>();
        IDeterministicRandom _deterministicRandom;

        // Track growth progress per archetype
        int _growthStep = 0;

        void Awake()
        {
            AllActivePlants.Add(this);

            CellManager = new PlantCellManager(this, seedCellPrefab, stemCellPrefab, leafCellPrefab, berryCellPrefab, leafFoodType);
            GrowthLogic = new PlantGrowthLogic(this);
            EnergySystem = new PlantEnergySystem(this);
            VisualManager = new PlantVisualManager(this, shadowController, null, outlineController, outlinePartPrefab, enableOutline);

            _deterministicRandom = GeneServices.Get<IDeterministicRandom>();
            if (_deterministicRandom == null)
            {
                Debug.LogError($"[{nameof(PlantGrowth)}] could not retrieve IDeterministicRandom service! Growth will be non-deterministic.", this);
            }

            var worldUI = GetComponent<PlantWorldUI>();
            if (worldUI == null)
            {
                worldUI = gameObject.AddComponent<PlantWorldUI>();
            }
            worldUI.Initialize(this);
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
            EnergySystem.CurrentEnergy = geneRuntimeState.template.startingEnergy;
            EnergySystem.BaseEnergyPerLeaf = seedTemplate.energyRegenRate;

            sequenceExecutor.InitializeWithTemplate(this.geneRuntimeState);

            CellManager.SpawnCellVisual(PlantCellType.Seed, Vector2Int.zero);

            CurrentState = PlantState.Initializing;

            destroyedLeafPositions.Clear();
            witheringTicksRemaining = 0;
            regrowthTickCounter = 0;
            preWitheringColors = null;
            _growthStep = 0;

            StartCoroutine(DelayedGrowthStart());

            if (TickManager.Instance != null)
            {
                TickManager.Instance.RegisterTickUpdateable(this);
            }

            Debug.Log($"Plant '{gameObject.name}' initialized from template '{seedTemplate.templateName}' (Archetype: {seedTemplate.archetype}). State: {CurrentState}");
        }

        IEnumerator DelayedGrowthStart()
        {
            yield return new WaitForSeconds(0.5f);
            CurrentState = PlantState.Growing;
            Debug.Log($"Plant '{gameObject.name}' transitioned to Growing state");
        }

        public void OnTickUpdate(int currentTick)
        {
            // Regrowth runs BEFORE withering early-return so a withering plant can save itself
            if (leafRegrowthRate > 0 && destroyedLeafPositions.Count > 0)
            {
                regrowthTickCounter++;
                if (regrowthTickCounter >= Mathf.CeilToInt(leafRegrowthRate))
                {
                    RegrowLeaf();
                    regrowthTickCounter = 0;
                }
            }

            if (CurrentState == PlantState.Withering)
            {
                witheringTicksRemaining--;
                Debug.Log($"[PlantGrowth] '{name}' withering… {witheringTicksRemaining} ticks remaining.");

                if (witheringTicksRemaining <= 0)
                {
                    if (ActiveLeafCount <= 0)
                    {
                        Die();
                        return;
                    }
                    else
                    {
                        ExitWithering();
                        Debug.Log($"[PlantGrowth] '{name}' survived withering! Leaves regrew in time.");
                    }
                }
                return;
            }

            if (CurrentState == PlantState.Mature || rechargeEnergyDuringGrowth)
            {
                EnergySystem.OnTickUpdate();
            }

            if (sequenceExecutor != null)
            {
                sequenceExecutor.OnTickUpdate(currentTick);
            }

            if (VisualManager != null)
            {
                VisualManager.UpdateUI();
            }

            if (CurrentState == PlantState.Growing)
            {
                float randomValue = (_deterministicRandom != null) ?
                    _deterministicRandom.Range(0f, 1f) : UnityEngine.Random.value;
                if (randomValue < baseGrowthChance * growthSpeedMultiplier)
                {
                    GrowSomething();
                }
            }
        }

        void EnterWithering()
        {
            CurrentState = PlantState.Withering;
            witheringTicksRemaining = WITHERING_DURATION;

            var renderers = GetComponentsInChildren<SpriteRenderer>();
            preWitheringColors = new Dictionary<SpriteRenderer, Color>(renderers.Length);
            foreach (var r in renderers)
            {
                if (r != null)
                {
                    preWitheringColors[r] = r.color;
                    r.color = new Color(WitheringTint.r, WitheringTint.g, WitheringTint.b, r.color.a);
                }
            }

            Debug.Log($"[PlantGrowth] '{name}' has 0 leaves — entering Withering ({WITHERING_DURATION} ticks until death).");
        }

        void ExitWithering()
        {
            CurrentState = PlantState.Mature;
            witheringTicksRemaining = 0;

            if (preWitheringColors != null)
            {
                foreach (var kvp in preWitheringColors)
                {
                    if (kvp.Key != null)
                    {
                        kvp.Key.color = kvp.Value;
                    }
                }
                preWitheringColors = null;
            }
        }

        void Die()
        {
            CurrentState = PlantState.Dead;
            Debug.Log($"[PlantGrowth] '{name}' has died.");
        }

        public bool DestroyRandomLeaf(string source = "unknown")
        {
            var activeLeaves = CellManager.LeafDataList.Where(l => l.IsActive).ToList();
            if (activeLeaves.Count == 0) return false;

            var target = activeLeaves[UnityEngine.Random.Range(0, activeLeaves.Count)];
            Vector2Int coord = target.GridCoord;

            GameObject cellObj = CellManager.GetCellGameObjectAt(coord);
            Vector3 vfxPos = cellObj != null
                ? cellObj.transform.position
                : transform.position + new Vector3(coord.x * cellSpacing, coord.y * cellSpacing, 0);

            CellManager.ReportCellDestroyed(coord);

            if (!destroyedLeafPositions.Contains(coord))
            {
                destroyedLeafPositions.Add(coord);
            }

            OnLeafConsumed?.Invoke(this, coord);

            StartCoroutine(DamageFlash());
            FloatingCombatText.Spawn(vfxPos, "-1 Leaf", new Color(0.4f, 0.8f, 0.2f));

            Debug.Log($"[PlantGrowth] '{name}' lost a leaf from {source}. Remaining: {ActiveLeafCount}");

            CheckLeafVitality();
            return true;
        }

        public bool RegrowLeaf()
        {
            if (destroyedLeafPositions.Count == 0) return false;

            Vector2Int regrowCoord = destroyedLeafPositions[0];
            destroyedLeafPositions.RemoveAt(0);

            if (CellManager.HasCellAt(regrowCoord)) return false;

            GameObject newLeaf = CellManager.SpawnCellVisual(PlantCellType.Leaf, regrowCoord);

            Vector3 vfxPos = newLeaf != null
                ? newLeaf.transform.position
                : transform.position + (Vector3)(Vector2)regrowCoord * cellSpacing;
            FloatingCombatText.Spawn(vfxPos + Vector3.up * 0.1f, "+1 Leaf", Color.green);

            if (newLeaf != null)
            {
                StartCoroutine(LeafRegrowAnimation(newLeaf.transform));
            }

            CheckLeafVitality();

            Debug.Log($"[PlantGrowth] '{name}' regrew a leaf at {regrowCoord}. Active: {ActiveLeafCount}");
            return true;
        }

        void CheckLeafVitality()
        {
            int activeLeaves = ActiveLeafCount;

            if (activeLeaves > 0)
            {
                if (CurrentState == PlantState.Withering)
                {
                    ExitWithering();
                    Debug.Log($"[PlantGrowth] '{name}' saved from withering! A leaf regrew.");
                }
            }
            else if (activeLeaves <= 0 && CurrentState == PlantState.Mature)
            {
                EnterWithering();
            }
        }

        IEnumerator LeafRegrowAnimation(Transform leafTransform)
        {
            if (leafTransform == null) yield break;

            Vector3 targetScale = leafTransform.localScale;
            leafTransform.localScale = targetScale * 0.3f;

            float elapsed = 0f;
            float duration = 0.3f;

            while (elapsed < duration)
            {
                if (leafTransform == null) yield break;
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                leafTransform.localScale = Vector3.Lerp(targetScale * 0.3f, targetScale, t);
                yield return null;
            }

            if (leafTransform != null)
            {
                leafTransform.localScale = targetScale;
            }
        }

        // ═══════════════════════════════════════════════════════════════
        //  GROWTH — Archetype dispatcher + per-archetype methods
        // ═══════════════════════════════════════════════════════════════

        void GrowSomething()
        {
            PlantArchetype arch = seedTemplate != null ? seedTemplate.archetype : PlantArchetype.Standard;

            switch (arch)
            {
                case PlantArchetype.Grass:
                    GrowGrass();
                    break;
                case PlantArchetype.Canopy:
                    GrowCanopy();
                    break;
                case PlantArchetype.Bush:
                    GrowBush();
                    break;
                default:
                    GrowStandard();
                    break;
            }
        }

        /// <summary>
        /// Original growth pattern: vertical stem + symmetric leaves every leafGap stems.
        /// </summary>
        void GrowStandard()
        {
            int oldHeight = CellManager.cells.Count(c => c.Value == PlantCellType.Stem);

            if (oldHeight < maxHeight)
            {
                int newHeight = oldHeight + 1;
                Vector2Int stemPos = new Vector2Int(0, newHeight);
                CellManager.SpawnCellVisual(PlantCellType.Stem, stemPos);

                if (newHeight > 0 && newHeight % leafGap == 0)
                {
                    int leafY = newHeight;
                    for (int i = 0; i < leafDensity; i++)
                    {
                        int leafOffset = (i / 2) + 1;
                        int xOffset = (i % 2 == 0) ? -leafOffset : leafOffset;
                        var leafPos = new Vector2Int(xOffset, leafY);
                        if (!CellManager.HasCellAt(leafPos))
                        {
                            CellManager.SpawnCellVisual(PlantCellType.Leaf, leafPos);
                        }
                    }
                }
            }
            else
            {
                CurrentState = PlantState.Mature;
            }
        }

        /// <summary>
        /// Grass archetype: all leaves, no stem. Blades fan out from ground level.
        /// Highest leaf count → highest energy generation → highest vulnerability.
        /// Uses maxHeight as the number of growth "waves" and leafDensity as blades per wave.
        /// </summary>
        void GrowGrass()
        {
            // Total waves = maxHeight, blades per wave = leafDensity
            int totalWaves = maxHeight;

            if (_growthStep < totalWaves)
            {
                int wave = _growthStep;

                // Each wave fans out blades at increasing spread.
                // wave 0 → y=1, tight cluster; wave N → y=N+1, wider spread
                int bladeY = wave + 1;
                float spreadFactor = 0.6f + (wave * 0.4f);

                for (int i = 0; i < leafDensity; i++)
                {
                    // Alternate left/right, increasing offset
                    int leafOffset = (i / 2) + 1;
                    int xOffset = (i % 2 == 0) ? -leafOffset : leafOffset;

                    // Apply spread factor to X; vary Y slightly for a natural look
                    int finalX = Mathf.RoundToInt(xOffset * spreadFactor);
                    int finalY = bladeY + (i % 2); // slight Y stagger

                    var leafPos = new Vector2Int(finalX, finalY);
                    if (!CellManager.HasCellAt(leafPos))
                    {
                        CellManager.SpawnCellVisual(PlantCellType.Leaf, leafPos);
                    }
                }

                _growthStep++;
            }
            else
            {
                CurrentState = PlantState.Mature;
            }
        }

        /// <summary>
        /// Canopy archetype: tall bare trunk, leaf crown concentrated at the top.
        /// Slow to mature. Once grown, leaves are far from ground — pests must reach them.
        /// Uses maxHeight for trunk height, leafDensity for crown size.
        /// </summary>
        void GrowCanopy()
        {
            int currentStemCount = CellManager.cells.Count(c => c.Value == PlantCellType.Stem);
            int trunkHeight = maxHeight; // Full trunk before any leaves

            if (currentStemCount < trunkHeight)
            {
                // Phase 1: grow the trunk (one stem per tick)
                int newHeight = currentStemCount + 1;
                Vector2Int stemPos = new Vector2Int(0, newHeight);
                CellManager.SpawnCellVisual(PlantCellType.Stem, stemPos);
            }
            else if (_growthStep == 0)
            {
                // Phase 2: spawn the leaf crown in one burst at the top
                int crownBaseY = trunkHeight;
                int crownLayers = Mathf.Max(2, leafDensity);

                for (int layer = 0; layer < crownLayers; layer++)
                {
                    int y = crownBaseY + layer + 1;
                    // Each layer gets wider toward the middle, then narrows — diamond/oval shape
                    int halfMiddle = crownLayers / 2;
                    int distFromMiddle = Mathf.Abs(layer - halfMiddle);
                    int layerWidth = Mathf.Max(1, halfMiddle - distFromMiddle + 1);

                    for (int x = -layerWidth; x <= layerWidth; x++)
                    {
                        var leafPos = new Vector2Int(x, y);
                        if (!CellManager.HasCellAt(leafPos))
                        {
                            CellManager.SpawnCellVisual(PlantCellType.Leaf, leafPos);
                        }
                    }
                }

                _growthStep = 1;
                CurrentState = PlantState.Mature;
            }
        }

        /// <summary>
        /// Bush archetype: short trunk + dense, wide leaf cluster.
        /// Round/bushy shape filling horizontal space. The "hedge wall."
        /// Uses minHeight as trunk height, maxHeight and leafDensity for bush volume.
        /// </summary>
        void GrowBush()
        {
            int currentStemCount = CellManager.cells.Count(c => c.Value == PlantCellType.Stem);
            int trunkHeight = Mathf.Max(1, minHeight); // Short trunk

            if (currentStemCount < trunkHeight)
            {
                // Phase 1: short trunk
                int newHeight = currentStemCount + 1;
                Vector2Int stemPos = new Vector2Int(0, newHeight);
                CellManager.SpawnCellVisual(PlantCellType.Stem, stemPos);
            }
            else
            {
                // Phase 2: grow one ring of bush per tick
                int bushLayers = maxHeight - trunkHeight;
                if (bushLayers <= 0) bushLayers = 3;

                if (_growthStep < bushLayers)
                {
                    int layer = _growthStep;
                    int baseY = trunkHeight;

                    // Elliptical bush: wider than tall
                    int y = baseY + layer + 1;
                    // Width peaks in the middle layers
                    int halfLayers = bushLayers / 2;
                    int distFromMiddle = Mathf.Abs(layer - halfLayers);
                    int layerWidth = Mathf.Max(1, leafDensity - distFromMiddle);

                    for (int x = -layerWidth; x <= layerWidth; x++)
                    {
                        var leafPos = new Vector2Int(x, y);
                        if (!CellManager.HasCellAt(leafPos))
                        {
                            CellManager.SpawnCellVisual(PlantCellType.Leaf, leafPos);
                        }
                    }

                    _growthStep++;
                }
                else
                {
                    CurrentState = PlantState.Mature;
                }
            }
        }

        // ═══════════════════════════════════════════════════════════════
        //  Pest eating
        // ═══════════════════════════════════════════════════════════════

        public void HandleBeingEaten(AnimalController eater, PlantCell eatenCell)
        {
            Debug.Log($"{eater.SpeciesName} ate cell at {eatenCell.GridCoord} on plant {name}");
            CellManager?.ReportCellDestroyed(eatenCell.GridCoord);

            if (eatenCell.CellType == PlantCellType.Leaf)
            {
                if (!destroyedLeafPositions.Contains(eatenCell.GridCoord))
                {
                    destroyedLeafPositions.Add(eatenCell.GridCoord);
                }
                OnLeafConsumed?.Invoke(this, eatenCell.GridCoord);

                if (thornDamage > 0 && eater != null)
                {
                    eater.TakeDamage(thornDamage);
                    FloatingCombatText.Spawn(
                        eater.transform.position + Vector3.up * 0.3f,
                        $"-{thornDamage:F0}",
                        new Color(0.2f, 0.8f, 0.2f)
                    );
                    Debug.Log($"[Thorns] Pest '{eater.SpeciesName}' took {thornDamage} thorn damage from eating leaf on '{name}'");
                }

                CheckLeafVitality();
            }
        }

        // ═══════════════════════════════════════════════════════════════
        //  Fruit management
        // ═══════════════════════════════════════════════════════════════

        public void RegisterFruitPosition(Vector2Int coord)
        {
            if (!activeFruitPositions.Contains(coord))
            {
                activeFruitPositions.Add(coord);
            }
        }

        public void ReportCellDestroyed(Vector2Int coord)
        {
            if (CellManager.cells.TryGetValue(coord, out var cellType) && cellType == PlantCellType.Fruit)
            {
                activeFruitPositions.Remove(coord);
            }
            CellManager?.ReportCellDestroyed(coord);
        }

        public GameObject GetCellGameObjectAt(Vector2Int coord)
        {
            return CellManager?.GetCellGameObjectAt(coord);
        }

        public Transform[] GetFruitSpawnPoints()
        {
            List<Transform> spawnPoints = new List<Transform>();
            List<Vector2Int> sourcePositions = GetFruitSourcePositions();
            HashSet<Vector2Int> emptyPositions = FindEmptyPositionsAround(sourcePositions);
            var availablePositions = emptyPositions.Where(pos => !activeFruitPositions.Contains(pos)).ToList();

            foreach (Vector2Int emptyPos in availablePositions)
            {
                GameObject tempSpawnPoint = CreateTemporarySpawnPoint(emptyPos);
                if (tempSpawnPoint != null)
                {
                    spawnPoints.Add(tempSpawnPoint.transform);
                }
            }

            if (spawnPoints.Count > 0)
            {
                StartCoroutine(CleanupTemporarySpawnPoints(spawnPoints, 0.1f));
            }

            return spawnPoints.ToArray();
        }

        List<Vector2Int> GetFruitSourcePositions()
        {
            List<Vector2Int> sourcePositions = new List<Vector2Int>();
            foreach (var kvp in CellManager.cells)
            {
                if (kvp.Value == PlantCellType.Stem) sourcePositions.Add(kvp.Key);
            }
            if (allowFruitsAroundLeaves)
            {
                foreach (var kvp in CellManager.cells)
                {
                    if (kvp.Value == PlantCellType.Leaf) sourcePositions.Add(kvp.Key);
                }
            }
            return sourcePositions;
        }

        HashSet<Vector2Int> FindEmptyPositionsAround(List<Vector2Int> sourcePositions)
        {
            HashSet<Vector2Int> emptyPositions = new HashSet<Vector2Int>();
            foreach (Vector2Int sourcePos in sourcePositions)
            {
                for (int x = -fruitSearchRadius; x <= fruitSearchRadius; x++)
                {
                    for (int y = -fruitSearchRadius; y <= fruitSearchRadius; y++)
                    {
                        if (x == 0 && y == 0) continue;
                        Vector2Int checkPos = sourcePos + new Vector2Int(x, y);
                        if (!CellManager.HasCellAt(checkPos))
                        {
                            float distance = Vector2Int.Distance(sourcePos, checkPos);
                            if (distance <= fruitSearchRadius)
                            {
                                emptyPositions.Add(checkPos);
                            }
                        }
                    }
                }
            }
            return emptyPositions;
        }

        GameObject CreateTemporarySpawnPoint(Vector2Int gridPos)
        {
            float spacing = GetCellWorldSpacing();
            Vector3 worldPos = transform.position + new Vector3(gridPos.x * spacing, gridPos.y * spacing, 0);
            GameObject spawnPoint = new GameObject($"TempFruitSpawnPoint_{gridPos.x}_{gridPos.y}");
            spawnPoint.transform.position = worldPos;
            spawnPoint.transform.SetParent(transform);
            spawnPoint.AddComponent<TemporaryFruitSpawnMarker>();
            return spawnPoint;
        }

        IEnumerator CleanupTemporarySpawnPoints(List<Transform> spawnPoints, float delay)
        {
            yield return new WaitForSeconds(delay);
            foreach (Transform spawnPoint in spawnPoints)
            {
                if (spawnPoint != null && spawnPoint.GetComponent<TemporaryFruitSpawnMarker>() != null)
                {
                    Destroy(spawnPoint.gameObject);
                }
            }
        }

        public List<HarvestedItem> HarvestAllFruits()
        {
            var harvestedItems = new List<HarvestedItem>();
            if (CellManager == null) return harvestedItems;

            var fruitGameObjects = new List<GameObject>();
            foreach (var cell in CellManager.cells)
            {
                if (cell.Value == PlantCellType.Fruit)
                {
                    var fruitGO = GetCellGameObjectAt(cell.Key);
                    if (fruitGO != null && fruitGO.GetComponent<HarvestableTag>() != null)
                    {
                        fruitGameObjects.Add(fruitGO);
                    }
                }
            }

            if (fruitGameObjects.Count == 0) return harvestedItems;

            foreach (var fruitGO in fruitGameObjects)
            {
                var fruitComponent = fruitGO.GetComponent<Fruit>();
                if (fruitComponent != null && fruitComponent.RepresentingItemDefinition != null)
                {
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

        [System.Obsolete("Use DestroyRandomLeaf instead. Kept for migration safety.")]
        public void TakeDamage(float amount)
        {
            if (CurrentState == PlantState.Dead) return;
            DestroyRandomLeaf("legacy:TakeDamage");
        }

        IEnumerator DamageFlash()
        {
            var renderers = GetComponentsInChildren<SpriteRenderer>();
            var originalColors = new Color[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
            {
                originalColors[i] = renderers[i].color;
                renderers[i].color = Color.red;
            }
            yield return new WaitForSeconds(0.15f);
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                    renderers[i].color = originalColors[i];
            }
        }
    }
}