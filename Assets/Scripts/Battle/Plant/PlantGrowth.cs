// FILE: Assets/Scripts/Battle/Plant/PlantGrowth.cs
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;

// --- Enums ---

public enum PlantState { Initializing, Growing, Mature_Idle, Mature_Executing }


public class PlantGrowth : MonoBehaviour
{
    // --- Serialized Fields (FROM YOUR ORIGINAL SCRIPT) ---
    [Header("UI & Visuals")]
    [SerializeField] private TMP_Text energyText;
    [SerializeField] private GameObject seedCellPrefab;
    [SerializeField] private GameObject stemCellPrefab;
    [SerializeField] private GameObject leafCellPrefab;
    [SerializeField] private GameObject berryCellPrefab; // Used for PlantCellType.Fruit
    [SerializeField] private float cellSpacing = 8f;

    [Header("Shadow Setup")] // <<< ADDED SECTION
    [SerializeField] [Tooltip("Assign the PlantShadowController component from the child _ShadowRoot GameObject")]
    private PlantShadowController shadowController; // <<< ADDED
    [SerializeField] [Tooltip("Assign your 'PlantShadow' prefab (containing SpriteRenderer + ShadowPartController script)")]
    private GameObject shadowPartPrefab; // <<< ADDED

    [Header("Growth & UI Timing")] // (FROM YOUR ORIGINAL SCRIPT)
    [SerializeField] private bool showGrowthPercentage = true;
    [SerializeField] private bool allowPhotosynthesisDuringGrowth = false;
    [SerializeField] private bool useSmoothPercentageCounter = true;
    [SerializeField] [Range(1, 10)] private int percentageIncrement = 2;

    // --- Internal State & Data (FROM YOUR ORIGINAL SCRIPT) ---
    private NodeGraph nodeGraph;
    public PlantState currentState = PlantState.Initializing;
    public float currentEnergy = 0f;
    private Dictionary<Vector2Int, PlantCellType> cells = new Dictionary<Vector2Int, PlantCellType>();
    // <<< ADDED: List to track GameObjects for shadow cleanup >>>
    private List<GameObject> activeCellGameObjects = new List<GameObject>(); // <<< ADDED
    private Coroutine percentageCounterCoroutine;
    private FireflyManager fireflyManagerInstance;
    private GameObject rootCellInstance;

    // --- Calculated Stats (FROM YOUR ORIGINAL SCRIPT) ---
    private int targetStemLength;
    private float finalGrowthSpeed; // Represents time interval per step
    private int finalLeafGap;
    private int finalLeafPattern;
    private float finalGrowthRandomness;
    private float finalMaxEnergy;
    private float finalPhotosynthesisRate;
    private float cycleCooldown;
    private float nodeCastDelay;

    // --- Runtime Variables (FROM YOUR ORIGINAL SCRIPT) ---
    private int currentStemCount = 0;
    private float cycleTimer = 0f;
    private float displayedGrowthPercentage = 0f;
    private float totalGrowthDuration; // Used by percentage counter
    private bool? offsetRightForPattern1 = null;

    // --- Unity Methods ---
    void Awake() // Added shadow validation
    {
        // --- Shadow Setup Validation ---
        bool shadowSetupValid = true;
        if (shadowController == null) { shadowController = GetComponentInChildren<PlantShadowController>(true); if (shadowController == null) { Debug.LogError($"PlantGrowth ERROR on '{gameObject.name}': PlantShadowController ref missing!", this); shadowSetupValid = false; } else { Debug.LogWarning($"PlantGrowth on '{gameObject.name}': Found PlantShadowController dynamically.", this); } }
        if (shadowPartPrefab == null) { Debug.LogError($"PlantGrowth ERROR on '{gameObject.name}': Shadow Part Prefab missing!", this); shadowSetupValid = false; }
        if (!shadowSetupValid) { Debug.LogWarning("Shadow system may not function correctly due to missing references."); /* Don't disable the whole script */ }
        // --- End Shadow Validation ---

        // Original Awake content (finding FireflyManager)
        fireflyManagerInstance = FireflyManager.Instance;
    }

    // Original Start - removed the SpawnInitialSeed call, it's handled by InitializeAndGrow
    void Start()
    {
        // Initial UI update might be needed here or in InitializeAndGrow
        UpdateUI(); // Ensure UI reflects initial state
    }

    private void Update() => StateMachineUpdate(); // Original

    private void OnDestroy() // Added shadow cleanup iteration
    {
        StopAllCoroutines();
        if (PlantGrowthModifierManager.Instance != null) { PlantGrowthModifierManager.Instance.UnregisterPlant(this); }

        // <<< ADDED: Shadow cleanup BEFORE clearing lists >>>
        ClearAllShadows(); // Call helper to unregister all shadows
        // ----------------------------------------------

        // Clear internal state (optional, as GO is destroyed)
        cells.Clear();
        activeCellGameObjects.Clear();
    }

    // --- Public Initialization (Integrates shadow cleanup) ---
    public void InitializeAndGrow(NodeGraph graph) // Original logic + shadow cleanup
    {
        if (graph == null || graph.nodes == null) { Debug.LogError($"[{gameObject.name}] Null/empty NodeGraph provided.", gameObject); Destroy(gameObject); return; }

        // --- Reset State & Clear Existing Visuals/Shadows ---
        StopAllCoroutines();
        if (percentageCounterCoroutine != null) { StopCoroutine(percentageCounterCoroutine); percentageCounterCoroutine = null; }
        ClearAllShadows(); // <<< ADDED: Clear shadows first
        // Now clear GameObjects and state dictionaries
        foreach(GameObject cellGO in activeCellGameObjects) { if(cellGO != null) Destroy(cellGO); }
        activeCellGameObjects.Clear();
        cells.Clear();
        rootCellInstance = null; currentStemCount = 0; offsetRightForPattern1 = null;
        // ----------------------------------------------------

        nodeGraph = graph; currentState = PlantState.Initializing; currentEnergy = 0f; displayedGrowthPercentage = 0f;

        CalculateAndApplyStats();

        if (targetStemLength > 0) { // Seed found
            GameObject spawnedSeed = SpawnCellVisual(PlantCellType.Seed, Vector2Int.zero, null, null); // Spawn seed
            if (spawnedSeed != null) {
                 rootCellInstance = spawnedSeed;
                 // Register with Modifier Manager AFTER seed exists
                 if (PlantGrowthModifierManager.Instance != null && TileInteractionManager.Instance != null) { Vector3Int gridPos = TileInteractionManager.Instance.WorldToCell(transform.position); TileDefinition currentTile = TileInteractionManager.Instance.FindWhichTileDefinitionAt(gridPos); PlantGrowthModifierManager.Instance.RegisterPlantTile(this, currentTile); }
                 StartGrowthVisuals(); // Start growth process
            } else { Debug.LogError($"[{gameObject.name}] Failed spawn initial seed.", gameObject); currentState = PlantState.Mature_Idle; }
        } else { Debug.LogWarning($"[{gameObject.name}] Target stem length {targetStemLength}. Skipping growth.", gameObject); currentState = PlantState.Mature_Idle; cycleTimer = cycleCooldown; if (seedCellPrefab != null && !cells.ContainsKey(Vector2Int.zero)) { SpawnCellVisual(PlantCellType.Seed, Vector2Int.zero, null, null); } }
        UpdateUI();
    }


    // --- State Machine & Updates (Original Logic) ---
    private void StateMachineUpdate() // Original
    {
        switch (currentState) { case PlantState.Growing: if (allowPhotosynthesisDuringGrowth) AccumulateEnergy(); if (!showGrowthPercentage || !useSmoothPercentageCounter) UpdateUI(); break; case PlantState.Mature_Idle: AccumulateEnergy(); UpdateUI(); cycleTimer -= Time.deltaTime; if (cycleTimer <= 0f && currentEnergy >= 1f) { currentState = PlantState.Mature_Executing; StartCoroutine(ExecuteMatureCycle()); } break; case PlantState.Mature_Executing: AccumulateEnergy(); UpdateUI(); break; case PlantState.Initializing: break; }
    }

    // AccumulateEnergy (Original Logic)
    private void AccumulateEnergy() // Original
    {
        if (finalPhotosynthesisRate <= 0 || finalMaxEnergy <= 0) return; float sunlight = WeatherManager.Instance ? WeatherManager.Instance.sunIntensity : 1f; int leafCount = cells.Values.Count(c => c == PlantCellType.Leaf); float tileMultiplier = (PlantGrowthModifierManager.Instance != null) ? PlantGrowthModifierManager.Instance.GetEnergyRechargeMultiplier(this) : 1.0f; float fireflyBonusRate = 0f; if (fireflyManagerInstance != null) { int nearbyFlyCount = fireflyManagerInstance.GetNearbyFireflyCount(transform.position, fireflyManagerInstance.photosynthesisRadius); fireflyBonusRate = Mathf.Min(nearbyFlyCount * fireflyManagerInstance.photosynthesisIntensityPerFly, fireflyManagerInstance.maxPhotosynthesisBonus); } float standardPhotosynthesis = finalPhotosynthesisRate * leafCount * sunlight; float totalRate = (standardPhotosynthesis + fireflyBonusRate) * tileMultiplier; float delta = totalRate * Time.deltaTime; currentEnergy = Mathf.Clamp(currentEnergy + delta, 0f, finalMaxEnergy);
    }

    // UpdateUI (Original Logic)
    private void UpdateUI() // Original
    {
        if (energyText == null) return; switch (currentState) { case PlantState.Growing when showGrowthPercentage: int perc = useSmoothPercentageCounter ? Mathf.RoundToInt(displayedGrowthPercentage) : (targetStemLength <= 0 ? 100 : Mathf.RoundToInt((float)currentStemCount / targetStemLength * 100f)); energyText.text = $"{Mathf.Clamp(perc, 0, 100)}%"; break; case PlantState.Growing: case PlantState.Mature_Idle: case PlantState.Mature_Executing: energyText.text = $"{Mathf.FloorToInt(currentEnergy)}/{Mathf.FloorToInt(finalMaxEnergy)}"; break; default: energyText.text = "..."; break; }
    }

    // --- Cell Management ---
    // ReportCellDestroyed (Original logic - NOTE: Doesn't handle shadow cleanup directly)
    public void ReportCellDestroyed(Vector2Int coord) // Original
    {
         if (cells.ContainsKey(coord)) { cells.Remove(coord); }
         // We need a way to remove from activeCellGameObjects too if destroyed externally
         activeCellGameObjects.RemoveAll(go => go == null || (go.GetComponent<PlantCell>()?.GridCoord == coord));
    }

    // <<< ADDED: Explicit RemovePlantCell for controlled removal >>>
    public void RemovePlantCell(GameObject cellToRemove)
    {
        if (cellToRemove == null) return;
        // --- Shadow Integration: Unregister ---
        SpriteRenderer partRenderer = cellToRemove.GetComponentInChildren<SpriteRenderer>();
        if (shadowController != null && partRenderer != null) {
            shadowController.UnregisterPlantPart(partRenderer);
        }
        // ---------------------------------
        PlantCell cellComp = cellToRemove.GetComponent<PlantCell>();
        if (cellComp != null && cells.ContainsKey(cellComp.GridCoord)) {
            cells.Remove(cellComp.GridCoord);
        }
        activeCellGameObjects.Remove(cellToRemove);
        Destroy(cellToRemove);
    }
    // <<< END ADDED REMOVE METHOD >>>


    // --- Stat Calculation (Original Logic) ---
    private void CalculateAndApplyStats() // Original
    {
        if (nodeGraph == null) { Debug.LogError($"[{gameObject.name}] CalculateAndApplyStats called with null NodeGraph!"); return; } float baseEnergyStorage = 10f; float basePhotosynthesisRate = 0.5f; int baseStemMin = 3; int baseStemMax = 5; float baseGrowthSpeedInterval = 0.5f; int baseLeafGap = 1; int baseLeafPattern = 0; float baseGrowthRandomness = 0.1f; float baseCooldown = 5f; float baseCastDelay = 0.1f; float accumulatedEnergyStorage = 0f; float accumulatedPhotosynthesis = 0f; int stemLengthModifier = 0; float growthSpeedTimeModifier = 0f; int leafGapModifier = 0; int currentLeafPattern = baseLeafPattern; float growthRandomnessModifier = 0f; float cooldownModifier = 0f; float castDelayModifier = 0f; bool seedFound = false; foreach (NodeData node in nodeGraph.nodes.OrderBy(n => n.orderIndex)) { if (node?.effects == null) continue; foreach (NodeEffectData effect in node.effects) { if (effect == null || !effect.isPassive) continue; switch (effect.effectType) { case NodeEffectType.SeedSpawn: seedFound = true; break; case NodeEffectType.EnergyStorage: accumulatedEnergyStorage += effect.primaryValue; break; case NodeEffectType.EnergyPhotosynthesis: accumulatedPhotosynthesis += effect.primaryValue; break; case NodeEffectType.StemLength: stemLengthModifier += Mathf.RoundToInt(effect.primaryValue); break; case NodeEffectType.GrowthSpeed: growthSpeedTimeModifier += effect.primaryValue; break; case NodeEffectType.LeafGap: leafGapModifier += Mathf.RoundToInt(effect.primaryValue); break; case NodeEffectType.LeafPattern: currentLeafPattern = Mathf.Clamp(Mathf.RoundToInt(effect.primaryValue), 0, 4); break; case NodeEffectType.StemRandomness: growthRandomnessModifier += effect.primaryValue; break; case NodeEffectType.Cooldown: cooldownModifier += effect.primaryValue; break; case NodeEffectType.CastDelay: castDelayModifier += effect.primaryValue; break; } } } finalMaxEnergy = Mathf.Max(1f, baseEnergyStorage + accumulatedEnergyStorage); finalPhotosynthesisRate = Mathf.Max(0f, basePhotosynthesisRate + accumulatedPhotosynthesis); int finalStemMin = Mathf.Max(1, baseStemMin + stemLengthModifier); int finalStemMax = Mathf.Max(finalStemMin, baseStemMax + stemLengthModifier); finalGrowthSpeed = Mathf.Max(0.01f, baseGrowthSpeedInterval + growthSpeedTimeModifier); finalLeafGap = Mathf.Max(0, baseLeafGap + leafGapModifier); finalLeafPattern = currentLeafPattern; finalGrowthRandomness = Mathf.Clamp01(baseGrowthRandomness + growthRandomnessModifier); cycleCooldown = Mathf.Max(0.1f, baseCooldown + cooldownModifier); nodeCastDelay = Mathf.Max(0.01f, baseCastDelay + castDelayModifier); targetStemLength = seedFound ? Random.Range(finalStemMin, finalStemMax + 1) : 0; totalGrowthDuration = targetStemLength * finalGrowthSpeed; if (!seedFound) { Debug.LogWarning($"[{gameObject.name}] NodeGraph lacks SeedSpawn effect. Growth aborted.", gameObject); }
    }

    // --- Visual Growth & Spawning ---
    private void StartGrowthVisuals() // Original
    {
        // Don't clear cells here, InitializeAndGrow does it
        currentState = PlantState.Growing;
        if (useSmoothPercentageCounter && showGrowthPercentage) { percentageCounterCoroutine = StartCoroutine(PercentageCounterRoutine()); }
        StartCoroutine(GrowRoutine());
    }

    // PercentageCounterRoutine (Original Logic)
    private IEnumerator PercentageCounterRoutine() // Original
    {
        displayedGrowthPercentage = 0; UpdateUI(); if (totalGrowthDuration <= 0 || percentageIncrement <= 0) yield break; int steps = Mathf.Max(1, 100 / percentageIncrement); float currentProgress = 0f; while (currentState == PlantState.Growing && currentProgress < 100f) { float tileMultiplier = 1.0f; if (PlantGrowthModifierManager.Instance != null) { tileMultiplier = PlantGrowthModifierManager.Instance.GetGrowthSpeedMultiplier(this); } float adjustedTimePerStep = (totalGrowthDuration / steps) / tileMultiplier; adjustedTimePerStep = Mathf.Max(0.01f, adjustedTimePerStep); yield return new WaitForSeconds(adjustedTimePerStep); if (currentState != PlantState.Growing) break; currentProgress += percentageIncrement; displayedGrowthPercentage = Mathf.Min(currentProgress, 100f); UpdateUI(); } if (currentState == PlantState.Growing || currentState == PlantState.Mature_Idle) { displayedGrowthPercentage = 100f; UpdateUI(); } percentageCounterCoroutine = null;
    }

    // GrowRoutine (Original Logic - uses finalGrowthSpeed as interval)
    private IEnumerator GrowRoutine() // Original
    {
        Vector2Int currentPos = Vector2Int.zero; int spiralDir = 1, patternCount = 0; while (currentState == PlantState.Growing) { if (currentStemCount >= targetStemLength) { if (percentageCounterCoroutine != null) { StopCoroutine(percentageCounterCoroutine); percentageCounterCoroutine = null; if (showGrowthPercentage) { displayedGrowthPercentage = 100f; UpdateUI(); } } currentState = PlantState.Mature_Idle; cycleTimer = cycleCooldown; UpdateUI(); yield break; } float tileMultiplier = 1.0f; if (PlantGrowthModifierManager.Instance != null) { tileMultiplier = PlantGrowthModifierManager.Instance.GetGrowthSpeedMultiplier(this); } float adjustedGrowthInterval = finalGrowthSpeed / tileMultiplier; adjustedGrowthInterval = Mathf.Max(0.01f, adjustedGrowthInterval); yield return new WaitForSeconds(adjustedGrowthInterval); currentStemCount++; Vector2Int growthDir = (currentStemCount == 1 && cells.ContainsKey(Vector2Int.zero)) ? Vector2Int.up : GetStemDirection(); Vector2Int nextPos = currentPos + growthDir; GameObject spawnedStem = SpawnCellVisual(PlantCellType.Stem, nextPos, null, null); if (spawnedStem != null) { currentPos = nextPos; if ((finalLeafGap >= 0) && (currentStemCount % (finalLeafGap + 1)) == 0) { patternCount++; ExecuteLeafPatternLogic(currentPos, currentPos + Vector2Int.left, currentPos + Vector2Int.right, patternCount, ref spiralDir); } } if ((showGrowthPercentage && !useSmoothPercentageCounter) || !showGrowthPercentage) UpdateUI(); }
    }

    private void ExecuteLeafPatternLogic(Vector2Int stemPos, Vector2Int leftBase, Vector2Int rightBase, int counter, ref int spiralDir) // Original
    { /* ...unchanged logic using SpawnLeafIfEmpty... */
        switch (finalLeafPattern) { case 0: SpawnLeafIfEmpty(leftBase); SpawnLeafIfEmpty(rightBase); break; case 1: if (offsetRightForPattern1 == null) offsetRightForPattern1 = Random.value < 0.5f; if (offsetRightForPattern1.Value) { SpawnLeafIfEmpty(leftBase); SpawnLeafIfEmpty(rightBase + Vector2Int.up); } else { SpawnLeafIfEmpty(leftBase + Vector2Int.up); SpawnLeafIfEmpty(rightBase); } break; case 2: switch (counter % 4) { default: case 0: case 2: SpawnLeafIfEmpty(leftBase); SpawnLeafIfEmpty(rightBase); break; case 1: SpawnLeafIfEmpty(leftBase + Vector2Int.up); SpawnLeafIfEmpty(rightBase); break; case 3: SpawnLeafIfEmpty(leftBase); SpawnLeafIfEmpty(rightBase + Vector2Int.up); break; } break; case 3: SpawnLeafIfEmpty(leftBase + new Vector2Int(0, spiralDir > 0 ? 1 : 0)); SpawnLeafIfEmpty(rightBase + new Vector2Int(0, spiralDir > 0 ? 0 : 1)); spiralDir *= -1; break; case 4: SpawnLeafIfEmpty(rightBase); SpawnLeafIfEmpty(rightBase + Vector2Int.up); break; }
    }

    private Vector2Int GetStemDirection() // Original
    { /* ...unchanged logic... */
        if (Random.value < finalGrowthRandomness) return (Random.value < 0.5f) ? Vector2Int.left + Vector2Int.up : Vector2Int.right + Vector2Int.up; return Vector2Int.up;
    }

    private void SpawnLeafIfEmpty(Vector2Int coords) // Original
    { /* ...unchanged logic using SpawnCellVisual... */
         if (!cells.ContainsKey(coords)) SpawnCellVisual(PlantCellType.Leaf, coords, null, null);
    }

    // SpawnCellVisual (Original Logic + ADDED Shadow Registration + ADDED GO Tracking)
    private GameObject SpawnCellVisual(PlantCellType cellType, Vector2Int coords,
                                 Dictionary<ScentDefinition, float> accumulatedScentRadiusBonus = null,
                                 Dictionary<ScentDefinition, float> accumulatedScentStrengthBonus = null)
    {
        if (cells.ContainsKey(coords)) { /* Optional Warning */ return null; } // Original check
        GameObject prefab = null; switch (cellType) { case PlantCellType.Seed: prefab = seedCellPrefab; break; case PlantCellType.Stem: prefab = stemCellPrefab; break; case PlantCellType.Leaf: prefab = leafCellPrefab; break; case PlantCellType.Fruit: prefab = berryCellPrefab; break; }
        if (prefab == null) { Debug.LogWarning($"[{gameObject.name}] No prefab for {cellType}.", gameObject); return null; }
        Vector2 worldPos = (Vector2)transform.position + ((Vector2)coords * cellSpacing);
        GameObject instance = Instantiate(prefab, worldPos, Quaternion.identity, transform); instance.name = $"{gameObject.name}_{cellType}_{coords.x}_{coords.y}";
        PlantCell cellComp = instance.GetComponent<PlantCell>() ?? instance.AddComponent<PlantCell>(); cellComp.ParentPlantGrowth = this; cellComp.GridCoord = coords; cellComp.CellType = cellType;

        cells[coords] = cellType; // Original: Add type to dict
        activeCellGameObjects.Add(instance); // <<< ADDED: Track GO

        SortableEntity sorter = instance.GetComponent<SortableEntity>() ?? instance.AddComponent<SortableEntity>();
        if (cellType == PlantCellType.Seed) {
            sorter.SetUseParentYCoordinate(false);
            // ModifierManager registration moved to InitializeAndGrow
        } else {
            sorter.SetUseParentYCoordinate(true);
        }
        if (cellType == PlantCellType.Fruit && accumulatedScentRadiusBonus != null && accumulatedScentStrengthBonus != null) { ApplyScentDataToObject(instance, accumulatedScentRadiusBonus, accumulatedScentStrengthBonus); }
        if (cellType == PlantCellType.Fruit) { FoodItem foodItem = instance.GetComponent<FoodItem>(); if (foodItem == null || foodItem.foodType == null) { Debug.LogError($"Spawned Berry Prefab '{prefab.name}' missing/unassigned FoodItem!", instance); } }

        // --- Shadow Integration: Register ---
        RegisterShadowForCell(instance, cellType.ToString()); // <<< ADDED Call
        // ---------------------------------

        return instance;
    }


    // --- Mature Cycle Execution (Original Logic) ---
    private IEnumerator ExecuteMatureCycle() // Original
    {
        if (nodeGraph?.nodes == null || nodeGraph.nodes.Count == 0) { Debug.LogError($"[{gameObject.name}] NodeGraph missing or empty!", gameObject); currentState = PlantState.Mature_Idle; cycleTimer = cycleCooldown; yield break; }
        float damageMultiplier = 1.0f; Dictionary<ScentDefinition, float> accumulatedScentRadiusBonus = new Dictionary<ScentDefinition, float>(); Dictionary<ScentDefinition, float> accumulatedScentStrengthBonus = new Dictionary<ScentDefinition, float>(); float totalEnergyCostForCycle = 0f;
        // Debug.Log($"[{gameObject.name} Cycle] Starting Accumulation Phase.");
        foreach (var node in nodeGraph.nodes.OrderBy(n => n.orderIndex)) { if (node?.effects == null || node.effects.Count == 0) continue; foreach (var effect in node.effects) { if (effect == null || effect.isPassive) continue; switch (effect.effectType) { case NodeEffectType.EnergyCost: totalEnergyCostForCycle += Mathf.Max(0f, effect.primaryValue); break; case NodeEffectType.Damage: damageMultiplier = Mathf.Max(0.1f, damageMultiplier + effect.primaryValue); break; case NodeEffectType.ScentModifier: if (effect.scentDefinitionReference != null) { ScentDefinition key = effect.scentDefinitionReference; /*Debug.Log($"   - FOUND ScentModifier for '{key.name}'.");*/ if (!accumulatedScentRadiusBonus.ContainsKey(key)) accumulatedScentRadiusBonus[key] = 0f; accumulatedScentRadiusBonus[key] += effect.primaryValue; if (!accumulatedScentStrengthBonus.ContainsKey(key)) accumulatedScentStrengthBonus[key] = 0f; accumulatedScentStrengthBonus[key] += effect.secondaryValue; } else { Debug.LogWarning($"   - Node '{node.nodeDisplayName ?? "Unnamed"}' has ScentModifier effect but ScentDefinition reference is NULL."); } break; } } }
        // Debug.Log($"[{gameObject.name} Cycle] Accumulation Complete.");
        if (currentEnergy < totalEnergyCostForCycle) { /*Debug.Log($"[{gameObject.name} Cycle] Execution skipped.");*/ currentState = PlantState.Mature_Idle; cycleTimer = cycleCooldown; yield break; } currentEnergy = Mathf.Max(0f, currentEnergy - totalEnergyCostForCycle); UpdateUI(); // Debug.Log($"[{gameObject.name} Cycle] Starting Execution Phase.");
        foreach (var node in nodeGraph.nodes.OrderBy(n => n.orderIndex)) { if (node?.effects == null || node.effects.Count == 0) continue; bool hasActionEffectInNode = node.effects.Any(eff => eff != null && !eff.isPassive && eff.effectType != NodeEffectType.EnergyCost && eff.effectType != NodeEffectType.Damage && eff.effectType != NodeEffectType.ScentModifier); if (hasActionEffectInNode && nodeCastDelay > 0.01f) { yield return new WaitForSeconds(nodeCastDelay); } foreach (var effect in node.effects) { if (effect == null || effect.isPassive || effect.effectType == NodeEffectType.EnergyCost || effect.effectType == NodeEffectType.Damage || effect.effectType == NodeEffectType.ScentModifier) continue; /*Debug.Log($"   - Executing Action: {effect.effectType}");*/ switch (effect.effectType) { case NodeEffectType.Output: OutputNodeEffect outputComp = GetComponentInChildren<OutputNodeEffect>(); if (outputComp != null) { outputComp.Activate(damageMultiplier, accumulatedScentRadiusBonus, accumulatedScentStrengthBonus); } else { Debug.LogWarning($"[{gameObject.name}] Node requested Output effect, but no OutputNodeEffect component found.", this); } break; case NodeEffectType.GrowBerry: TrySpawnBerry(accumulatedScentRadiusBonus, accumulatedScentStrengthBonus); break; } } }
        cycleTimer = cycleCooldown; currentState = PlantState.Mature_Idle; // Debug.Log($"[{gameObject.name} Cycle] Execution Phase Complete.");
    }

    // TrySpawnBerry (Original Logic)
    private void TrySpawnBerry(Dictionary<ScentDefinition, float> scentRadiiBonus, Dictionary<ScentDefinition, float> scentStrengthsBonus) // Original
    { /* ...unchanged logic using SpawnCellVisual... */
        if (berryCellPrefab == null) { Debug.LogWarning($"[{gameObject.name}] Berry Prefab not assigned.", gameObject); return; } /*Debug.Log($"[{gameObject.name} Cycle] TrySpawnBerry called.");*/ var potentialCoords = cells.SelectMany(cellKvp => { Vector2Int coord = cellKvp.Key; PlantCellType cellType = cellKvp.Value; List<Vector2Int> candidates = new List<Vector2Int>(); if (cellType == PlantCellType.Leaf) candidates.Add(coord + Vector2Int.down); else if (cellType == PlantCellType.Stem) candidates.Add(coord + Vector2Int.up); return candidates; }).Where(coord => !cells.ContainsKey(coord)).Distinct().ToList(); if (potentialCoords.Count > 0) { SpawnCellVisual(PlantCellType.Fruit, potentialCoords[Random.Range(0, potentialCoords.Count)], scentRadiiBonus, scentStrengthsBonus); }
    }
    // ApplyScentDataToObject (Original Logic)
    public void ApplyScentDataToObject(GameObject targetObject, Dictionary<ScentDefinition, float> scentRadiusBonuses, Dictionary<ScentDefinition, float> scentStrengthBonuses) // Original
    { /* ...unchanged logic... */
        /*Debug.Log($"ApplyScentDataToObject called for {targetObject.name}.");*/ if (targetObject == null || EcosystemManager.Instance == null || EcosystemManager.Instance.scentLibrary == null) return; ScentDefinition strongestScentDef = null; float maxStrengthBonus = -1f; if (scentStrengthBonuses != null && scentStrengthBonuses.Count > 0) { foreach (var kvp in scentStrengthBonuses) { if (kvp.Key != null && kvp.Value > maxStrengthBonus) { maxStrengthBonus = kvp.Value; strongestScentDef = kvp.Key; } } } if (strongestScentDef != null) { /*Debug.Log($" - Strongest scent found: {strongestScentDef.name}.");*/ ScentSource scentSource = targetObject.GetComponent<ScentSource>() ?? targetObject.AddComponent<ScentSource>(); scentSource.definition = strongestScentDef; scentRadiusBonuses.TryGetValue(strongestScentDef, out float radiusBonus); scentStrengthBonuses.TryGetValue(strongestScentDef, out float strengthBonus); scentSource.radiusModifier = radiusBonus; scentSource.strengthModifier = strengthBonus; /*Debug.Log($"   - Configured ScentSource: Def={scentSource.definition?.name}, RadMod={scentSource.radiusModifier}, StrMod={scentSource.strengthModifier}, EffectiveRadius={scentSource.EffectiveRadius}");*/ if (strongestScentDef.particleEffectPrefab != null) { bool particleExists = false; foreach(Transform child in targetObject.transform){ if(child.TryGetComponent<ParticleSystem>(out _)){ particleExists = true; break; } } if (!particleExists) { Instantiate(strongestScentDef.particleEffectPrefab, targetObject.transform.position, Quaternion.identity, targetObject.transform); } } } /*else { Debug.Log(" - No strongest scent found."); }*/
    }

    // --- UI Reference Helper (Original Logic) ---
    private void EnsureUIReferences() // Original
    {
        if (energyText) return; energyText = GetComponentInChildren<TMP_Text>(true); if (!energyText) Debug.LogWarning($"[{gameObject.name}] Energy Text (TMP_Text) not found in children.", gameObject);
    }

    // --- Helper Methods for Shadow Integration ---
    // RegisterShadowForCell (Helper - unchanged from previous correct version)
    private void RegisterShadowForCell(GameObject cellInstance, string cellTypeName) // Unchanged
    {
        if (shadowController == null || shadowPartPrefab == null || cellInstance == null) return; SpriteRenderer partRenderer = cellInstance.GetComponentInChildren<SpriteRenderer>(); if (partRenderer != null) { shadowController.RegisterPlantPart(partRenderer, shadowPartPrefab); } else { Debug.LogWarning($"Plant '{gameObject.name}': Instantiated {cellTypeName} ('{cellInstance.name}') missing SpriteRenderer. No shadow created.", cellInstance); }
    }

    // <<< ADDED: Helper to clean up shadows by iterating the GO list >>>
    private void ClearAllShadows()
    {
        if (shadowController == null) return; // No controller, nothing to do
        // Debug.Log($"[{gameObject.name} ClearAllShadows] Clearing shadows for {activeCellGameObjects.Count} tracked GameObjects.");
        for (int i = activeCellGameObjects.Count - 1; i >= 0; i--) {
            GameObject cellGO = activeCellGameObjects[i];
            if (cellGO != null) { // Check if GO still exists
                SpriteRenderer partRenderer = cellGO.GetComponentInChildren<SpriteRenderer>();
                if (partRenderer != null) {
                    shadowController.UnregisterPlantPart(partRenderer); // Tell controller to clean up
                }
            }
        }
        // Note: We don't clear activeCellGameObjects here, that happens in ClearAllCells or InitializeAndGrow
    }
    // <<< END ADDED HELPER >>>

} // End of PlantGrowth class