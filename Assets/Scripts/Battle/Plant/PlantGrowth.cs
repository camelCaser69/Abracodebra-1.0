// FILE: Assets/Scripts/Battle/Plant/PlantGrowth.cs
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;

// --- Enums ---
public enum PlantCellType { Seed, Stem, Leaf, Flower, Fruit }
public enum PlantState { Initializing, Growing, Mature_Idle, Mature_Executing }


public class PlantGrowth : MonoBehaviour
{
    // --- Serialized Fields ---
    [Header("UI & Visuals")]
    [SerializeField] private TMP_Text energyText;
    [SerializeField] private GameObject seedCellPrefab;
    [SerializeField] private GameObject stemCellPrefab;
    [SerializeField] private GameObject leafCellPrefab;
    [SerializeField] private GameObject berryCellPrefab; // Used for PlantCellType.Fruit
    [SerializeField] private float cellSpacing = 8f;

    [Header("Growth & UI Timing")]
    [SerializeField] private bool showGrowthPercentage = true;
    [SerializeField] private bool allowPhotosynthesisDuringGrowth = false;
    [SerializeField] private bool useSmoothPercentageCounter = true;
    [SerializeField] [Range(1, 10)] private int percentageIncrement = 2;

    // --- Internal State & Data ---
    private NodeGraph nodeGraph;
    public PlantState currentState = PlantState.Initializing;
    public float currentEnergy = 0f;
    private Dictionary<Vector2Int, PlantCellType> cells = new Dictionary<Vector2Int, PlantCellType>();
    private Coroutine percentageCounterCoroutine;
    private FireflyManager fireflyManagerInstance; // Cache the reference

    // --- Calculated Stats ---
    private int targetStemLength;
    private float finalGrowthSpeed;
    private int finalLeafGap;
    private int finalLeafPattern;
    private float finalGrowthRandomness;
    private float finalMaxEnergy;
    private float finalPhotosynthesisRate;
    private float cycleCooldown;
    private float nodeCastDelay;

    // --- Runtime Variables ---
    private int currentStemCount = 0;
    private float cycleTimer = 0f;
    private float displayedGrowthPercentage = 0f;
    private float totalGrowthDuration;
    private bool? offsetRightForPattern1 = null;

    // --- Unity Methods ---
    private void Awake() // Modified
    {
        EnsureUIReferences();
        fireflyManagerInstance = FireflyManager.Instance; // Get the singleton instance
        if (fireflyManagerInstance == null)
        {
             // Optional: Log warning if manager doesn't exist, photosynthesis bonus won't work
             // Debug.LogWarning($"[{gameObject.name}] FireflyManager instance not found. Firefly photosynthesis bonus disabled.");
        }
    }

    private void Update() => StateMachineUpdate();

    private void OnDestroy()
    {
        StopAllCoroutines(); // Ensure cleanup
    
        // Unregister with the PlantGrowthModifierManager if it exists
        if (PlantGrowthModifierManager.Instance != null)
        {
            PlantGrowthModifierManager.Instance.UnregisterPlant(this);
        }
    }

    // --- Public Initialization ---
    public void InitializeAndGrow(NodeGraph graph)
    {
        if (graph == null || graph.nodes == null) {
            Debug.LogError($"[{gameObject.name}] Null/empty NodeGraph provided.", gameObject); Destroy(gameObject); return;
        }

        // <<< ADDED DETAILED GRAPH INSPECTION LOG >>>
        Debug.Log($"InitializeAndGrow received graph with {graph.nodes.Count} nodes.");
        for(int i = 0; i < graph.nodes.Count; i++)
        {
            var node = graph.nodes[i];
             if(node == null) {
                Debug.LogWarning($" - Node at index {i} is NULL.");
                continue;
             }
            Debug.Log($" - Node '{node.nodeDisplayName ?? "NO NAME"}' (Index: {node.orderIndex}) has {node.effects?.Count ?? 0} effects:");
            if (node.effects != null)
            {
                for(int j = 0; j < node.effects.Count; j++)
                {
                    var effect = node.effects[j];
                     if(effect == null) {
                        Debug.LogWarning($"   - Effect at index {j} is NULL.");
                        continue;
                     }
                     // Log details of each effect
                     Debug.Log($"   - Type: {effect.effectType}, Passive: {effect.isPassive}, ScentRef: {(effect.scentDefinitionReference != null ? effect.scentDefinitionReference.name : "NULL")}, RadiusBonus(Val1): {effect.primaryValue}, StrengthBonus(Val2): {effect.secondaryValue}");
                }
            }
        }
        // <<< END DETAILED GRAPH INSPECTION LOG >>>


        nodeGraph = graph; // Store the graph reference
        currentState = PlantState.Initializing;
        currentEnergy = 0f;
        displayedGrowthPercentage = 0f;
        if (percentageCounterCoroutine != null) { StopCoroutine(percentageCounterCoroutine); percentageCounterCoroutine = null; }

        CalculateAndApplyStats(); // Calculate based on the stored nodeGraph

        if (targetStemLength > 0) {
            StartGrowthVisuals();
        } else {
             Debug.LogWarning($"[{gameObject.name}] Target stem length is {targetStemLength}. Skipping visual growth.", gameObject);
            currentState = PlantState.Mature_Idle;
            cycleTimer = cycleCooldown;
            if (!cells.ContainsKey(Vector2Int.zero)) {
                 SpawnCellVisual(PlantCellType.Seed, Vector2Int.zero, null, null);
             }
        }
        UpdateUI();
    }


    // --- State Machine & Updates ---
    private void StateMachineUpdate()
    {
        switch (currentState)
        {
            case PlantState.Growing:
                if (allowPhotosynthesisDuringGrowth) AccumulateEnergy();
                if (!showGrowthPercentage || !useSmoothPercentageCounter) UpdateUI();
                break;
            case PlantState.Mature_Idle:
                AccumulateEnergy();
                UpdateUI();
                cycleTimer -= Time.deltaTime;
                // Ensure energy is checked against the cost dynamically if needed,
                // or just use a minimal threshold to start the cycle attempt.
                // Checking against total cost happens INSIDE ExecuteMatureCycle now.
                if (cycleTimer <= 0f && currentEnergy >= 1f) // Low threshold to attempt cycle
                {
                    currentState = PlantState.Mature_Executing;
                    StartCoroutine(ExecuteMatureCycle());
                }
                break;
            case PlantState.Mature_Executing:
                AccumulateEnergy();
                UpdateUI();
                break;
        }
    }

    // AccumulateEnergy (No changes needed from previous version)
    private void AccumulateEnergy()
    {
        float sunlight = WeatherManager.Instance ? WeatherManager.Instance.sunIntensity : 1f;
        int leafCount = cells.Values.Count(c => c == PlantCellType.Leaf);
    
        // Get the tile-based energy multiplier
        float tileMultiplier = 1.0f;
        if (PlantGrowthModifierManager.Instance != null)
        {
            tileMultiplier = PlantGrowthModifierManager.Instance.GetEnergyRechargeMultiplier(this);
        }
    
        float fireflyBonusRate = 0f;
        if (fireflyManagerInstance != null)
        {
            int nearbyFlyCount = fireflyManagerInstance.GetNearbyFireflyCount(transform.position, fireflyManagerInstance.photosynthesisRadius);
            fireflyBonusRate = Mathf.Min(nearbyFlyCount * fireflyManagerInstance.photosynthesisIntensityPerFly,
                fireflyManagerInstance.maxPhotosynthesisBonus);
        }
    
        float standardPhotosynthesis = finalPhotosynthesisRate * leafCount * sunlight;
        float totalRate = (standardPhotosynthesis + fireflyBonusRate) * tileMultiplier; // Apply tile multiplier
        float delta = totalRate * Time.deltaTime;
        currentEnergy = Mathf.Clamp(currentEnergy + delta, 0f, finalMaxEnergy);
    }


    private void UpdateUI()
    {
        // (No changes needed)
        if (energyText == null) return;
        switch (currentState) {
            case PlantState.Growing when showGrowthPercentage:
                int perc = useSmoothPercentageCounter ? Mathf.RoundToInt(displayedGrowthPercentage) :
                           (targetStemLength <= 0 ? 100 : Mathf.RoundToInt((float)currentStemCount / targetStemLength * 100f));
                energyText.text = $"{Mathf.Clamp(perc, 0, 100)}%";
                break;
            case PlantState.Growing: // Not showing percentage, show energy
            case PlantState.Mature_Idle:
            case PlantState.Mature_Executing:
                energyText.text = $"{Mathf.FloorToInt(currentEnergy)}/{Mathf.FloorToInt(finalMaxEnergy)}";
                break;
            default: energyText.text = "..."; break;
        }
    }

    // --- Cell Management ---
    /// <summary>
    /// Called by PlantCell component when its GameObject is destroyed.
    /// Removes the cell from the internal dictionary.
    /// </summary>
    public void ReportCellDestroyed(Vector2Int coord)
    {
         if (cells.ContainsKey(coord))
         {
             cells.Remove(coord);
         }
    }

    // --- Stat Calculation ---
    private void CalculateAndApplyStats()
    {
        // (No changes needed - Scent is not passive)
        if (nodeGraph == null) { Debug.LogError($"[{gameObject.name}] CalculateAndApplyStats called with null NodeGraph!"); return; }
        float baseEnergyStorage = 0f; float basePhotosynthesisRate = 0f; int baseStemMin = 0; int baseStemMax = 0; float baseGrowthSpeed = 0f;
        int baseLeafGap = 0; int baseLeafPattern = 0; float baseGrowthRandomness = 0f; float baseCooldown = 0f; float baseCastDelay = 0f;
        float accumulatedEnergyStorage = 0f; float accumulatedPhotosynthesis = 0f; int stemLengthModifier = 0; float growthSpeedTimeModifier = 0f;
        int leafGapModifier = 0; int currentLeafPattern = baseLeafPattern; float growthRandomnessModifier = 0f; float cooldownModifier = 0f;
        float castDelayModifier = 0f; bool seedFound = false;
        foreach (NodeData node in nodeGraph.nodes.OrderBy(n => n.orderIndex)) {
            if (node.effects == null) continue;
            foreach (NodeEffectData effect in node.effects) {
                if (!effect.isPassive) continue;
                switch (effect.effectType) {
                    case NodeEffectType.SeedSpawn: seedFound = true; break;
                    case NodeEffectType.EnergyStorage: accumulatedEnergyStorage += effect.primaryValue; break;
                    case NodeEffectType.EnergyPhotosynthesis: accumulatedPhotosynthesis += effect.primaryValue; break;
                    case NodeEffectType.StemLength: stemLengthModifier += Mathf.RoundToInt(effect.primaryValue); break;
                    case NodeEffectType.GrowthSpeed: growthSpeedTimeModifier += effect.primaryValue; break;
                    case NodeEffectType.LeafGap: leafGapModifier += Mathf.RoundToInt(effect.primaryValue); break;
                    case NodeEffectType.LeafPattern: currentLeafPattern = Mathf.RoundToInt(effect.primaryValue); break;
                    case NodeEffectType.StemRandomness: growthRandomnessModifier += effect.primaryValue; break;
                    case NodeEffectType.Cooldown: cooldownModifier += effect.primaryValue; break;
                    case NodeEffectType.CastDelay: castDelayModifier += effect.primaryValue; break;
                }
            }
        }
        finalMaxEnergy = Mathf.Max(1f, baseEnergyStorage + accumulatedEnergyStorage);
        finalPhotosynthesisRate = Mathf.Max(0f, basePhotosynthesisRate + accumulatedPhotosynthesis);
        int finalStemMin = Mathf.Max(1, baseStemMin + stemLengthModifier); int finalStemMax = Mathf.Max(finalStemMin, baseStemMax + stemLengthModifier);
        finalGrowthSpeed = Mathf.Max(0.1f, baseGrowthSpeed + growthSpeedTimeModifier); finalLeafGap = Mathf.Max(0, baseLeafGap + leafGapModifier);
        finalLeafPattern = Mathf.Clamp(currentLeafPattern, 0, 4); finalGrowthRandomness = Mathf.Clamp01(baseGrowthRandomness + growthRandomnessModifier);
        cycleCooldown = Mathf.Max(0.1f, baseCooldown + cooldownModifier); nodeCastDelay = Mathf.Max(0.01f, baseCastDelay + castDelayModifier);
        targetStemLength = seedFound ? Random.Range(finalStemMin, finalStemMax + 1) : 0; totalGrowthDuration = targetStemLength * finalGrowthSpeed;
        if (!seedFound) { Debug.LogWarning($"[{gameObject.name}] NodeGraph lacks SeedSpawn effect. Growth aborted.", gameObject); }
    }

    // --- Visual Growth & Spawning ---
    private void StartGrowthVisuals()
    {
        // (No changes needed)
         foreach (Transform child in transform) { if (child.GetComponent<PlantCell>() != null) { Destroy(child.gameObject); } }
         cells.Clear(); currentStemCount = 0; offsetRightForPattern1 = null;
         SpawnCellVisual(PlantCellType.Seed, Vector2Int.zero, null, null);
         currentState = PlantState.Growing;
         if (useSmoothPercentageCounter && showGrowthPercentage) { percentageCounterCoroutine = StartCoroutine(PercentageCounterRoutine()); }
         StartCoroutine(GrowRoutine());
    }

    private IEnumerator PercentageCounterRoutine()
    {
        displayedGrowthPercentage = 0; 
        UpdateUI(); 
    
        if (totalGrowthDuration <= 0 || percentageIncrement <= 0) yield break;
    
        int steps = Mathf.Max(1, 100 / percentageIncrement); 
    
        // Keep track of progress
        float currentProgress = 0f;
    
        while (currentState == PlantState.Growing && currentProgress < 100f)
        {
            // Get the tile-based growth speed multiplier - this is the same as in GrowRoutine
            float tileMultiplier = 1.0f;
            if (PlantGrowthModifierManager.Instance != null)
            {
                tileMultiplier = PlantGrowthModifierManager.Instance.GetGrowthSpeedMultiplier(this);
            }
        
            // Calculate how much time to wait before the next percentage update
            // We adjust time based on the same tile multiplier
            float adjustedTimePerStep = (totalGrowthDuration / steps) / tileMultiplier;
        
            yield return new WaitForSeconds(adjustedTimePerStep);
        
            if (currentState != PlantState.Growing) break;
        
            // Increase percentage and update UI
            currentProgress += percentageIncrement;
            displayedGrowthPercentage = Mathf.Min(currentProgress, 100f);
            UpdateUI();
        }
    
        if (currentState == PlantState.Growing || currentState == PlantState.Mature_Idle)
        { 
            displayedGrowthPercentage = 100f; 
            UpdateUI(); 
        }
    
        percentageCounterCoroutine = null;
    }

    private IEnumerator GrowRoutine()
    {
        Vector2Int currentPos = Vector2Int.zero; 
        int spiralDir = 1, patternCount = 0;
    
        while (currentState == PlantState.Growing) 
        {
            if (currentStemCount >= targetStemLength) 
            {
                if (percentageCounterCoroutine != null) 
                { 
                    StopCoroutine(percentageCounterCoroutine); 
                    percentageCounterCoroutine = null; 
                    if (showGrowthPercentage) 
                    { 
                        displayedGrowthPercentage = 100f; 
                        UpdateUI(); 
                    } 
                }
                currentState = PlantState.Mature_Idle; 
                cycleTimer = cycleCooldown; 
                UpdateUI(); 
                yield break;
            }
        
            // Get the tile-based growth speed multiplier
            float tileMultiplier = 1.0f;
            if (PlantGrowthModifierManager.Instance != null)
            {
                tileMultiplier = PlantGrowthModifierManager.Instance.GetGrowthSpeedMultiplier(this);
            }
        
            // Apply tile multiplier to growth speed
            float adjustedGrowthSpeed = finalGrowthSpeed / tileMultiplier; // Divide because higher value = slower growth
        
            yield return new WaitForSeconds(adjustedGrowthSpeed);
        
            currentStemCount++; 
            Vector2Int growthDir = (currentStemCount == 1) ? Vector2Int.up : GetStemDirection(); 
            currentPos += growthDir;
            SpawnCellVisual(PlantCellType.Stem, currentPos, null, null);
        
            if ((finalLeafGap >= 0) && (currentStemCount % (finalLeafGap + 1)) == 0) 
            { 
                patternCount++; 
                ExecuteLeafPatternLogic(currentPos, currentPos + Vector2Int.left, currentPos + Vector2Int.right, patternCount, ref spiralDir); 
            }
        
            if ((showGrowthPercentage && !useSmoothPercentageCounter) || !showGrowthPercentage) 
                UpdateUI();
        }
    }

    private void ExecuteLeafPatternLogic(Vector2Int stemPos, Vector2Int leftBase, Vector2Int rightBase, int counter, ref int spiralDir)
    {
        // (No changes needed)
         switch (finalLeafPattern) {
             case 0: SpawnLeafIfEmpty(leftBase); SpawnLeafIfEmpty(rightBase); break;
             case 1: if (offsetRightForPattern1 == null) offsetRightForPattern1 = Random.value < 0.5f; if (offsetRightForPattern1.Value) { SpawnLeafIfEmpty(leftBase); SpawnLeafIfEmpty(rightBase + Vector2Int.up); } else { SpawnLeafIfEmpty(leftBase + Vector2Int.up); SpawnLeafIfEmpty(rightBase); } break;
             case 2: switch (counter % 4) { default: case 0: case 2: SpawnLeafIfEmpty(leftBase); SpawnLeafIfEmpty(rightBase); break; case 1: SpawnLeafIfEmpty(leftBase + Vector2Int.up); SpawnLeafIfEmpty(rightBase); break; case 3: SpawnLeafIfEmpty(leftBase); SpawnLeafIfEmpty(rightBase + Vector2Int.up); break; } break;
             case 3: SpawnLeafIfEmpty(leftBase + new Vector2Int(0, spiralDir > 0 ? 1 : 0)); SpawnLeafIfEmpty(rightBase + new Vector2Int(0, spiralDir > 0 ? 0 : 1)); spiralDir *= -1; break;
             case 4: SpawnLeafIfEmpty(rightBase); SpawnLeafIfEmpty(rightBase + Vector2Int.up); break;
         }
    }

    private Vector2Int GetStemDirection()
    {
        // (No changes needed)
        if (Random.value < finalGrowthRandomness) return (Random.value < 0.5f) ? Vector2Int.left + Vector2Int.up : Vector2Int.right + Vector2Int.up;
        return Vector2Int.up;
    }

    private void SpawnLeafIfEmpty(Vector2Int coords)
    {
        // (No changes needed)
        if (!cells.ContainsKey(coords)) SpawnCellVisual(PlantCellType.Leaf, coords, null, null);
    }

    // SpawnCellVisual (No changes needed from previous 'complete' version)
    private void SpawnCellVisual(PlantCellType cellType, Vector2Int coords,
                                 Dictionary<ScentDefinition, float> accumulatedScentRadiusBonus = null,
                                 Dictionary<ScentDefinition, float> accumulatedScentStrengthBonus = null)
    {
        // (No changes needed)
        if (cells.ContainsKey(coords)) { if(cells[coords] != cellType) { Debug.LogWarning($"Spawn collision at {coords}. Overwriting {cells[coords]} with {cellType}.", gameObject); } else { return; } }
        GameObject prefab = null; switch (cellType) { case PlantCellType.Seed: prefab = seedCellPrefab; break; case PlantCellType.Stem: prefab = stemCellPrefab; break; case PlantCellType.Leaf: prefab = leafCellPrefab; break; case PlantCellType.Fruit: prefab = berryCellPrefab; break; }
        if (prefab != null) {
            Vector2 worldPos = (Vector2)transform.position + (Vector2)coords * cellSpacing; GameObject instance = Instantiate(prefab, worldPos, Quaternion.identity, transform);
            PlantCell cellComp = instance.GetComponent<PlantCell>(); if (cellComp == null) { Debug.LogError($"Prefab '{prefab.name}' for {cellType} missing PlantCell! Adding one.", instance); cellComp = instance.AddComponent<PlantCell>(); }
            cellComp.ParentPlantGrowth = this; cellComp.GridCoord = coords; cellComp.CellType = cellType;
            cells[coords] = cellType;
            SortableEntity sorter = instance.GetComponent<SortableEntity>() ?? instance.AddComponent<SortableEntity>(); if (cellType != PlantCellType.Seed) sorter.SetUseParentYCoordinate(true);
            if (cellType == PlantCellType.Fruit && accumulatedScentRadiusBonus != null && accumulatedScentStrengthBonus != null) { ApplyScentDataToObject(instance, accumulatedScentRadiusBonus, accumulatedScentStrengthBonus); }
            if (cellType == PlantCellType.Fruit && (instance.GetComponent<FoodItem>() == null || instance.GetComponent<FoodItem>().foodType == null)) { Debug.LogError($"Spawned Berry Prefab '{prefab.name}' at {coords} missing/unassigned FoodItem!", instance); }
        } else { Debug.LogWarning($"[{gameObject.name}] No prefab assigned for {cellType}.", gameObject); }
    }


    // --- Mature Cycle Execution (with Debug Logs) ---
    private IEnumerator ExecuteMatureCycle()
    {
        if (nodeGraph?.nodes == null || nodeGraph.nodes.Count == 0) {
             Debug.LogError($"[{gameObject.name}] NodeGraph missing or empty!", gameObject);
             currentState = PlantState.Mature_Idle; cycleTimer = cycleCooldown; yield break;
        }

        // --- Accumulation Phase ---
        float damageMultiplier = 1.0f;
        Dictionary<ScentDefinition, float> accumulatedScentRadiusBonus = new Dictionary<ScentDefinition, float>();
        Dictionary<ScentDefinition, float> accumulatedScentStrengthBonus = new Dictionary<ScentDefinition, float>();
        float totalEnergyCostForCycle = 0f;

        Debug.Log($"[{gameObject.name} Cycle] Starting Accumulation Phase for {nodeGraph.nodes.Count} nodes."); // Log start

        foreach (var node in nodeGraph.nodes.OrderBy(n => n.orderIndex))
        {
            if (node.effects == null || node.effects.Count == 0) continue;

            // Debug.Log($" - Checking Node '{node.nodeDisplayName}'"); // Optional per-node log

            foreach (var effect in node.effects)
            {
                 // Skip passive effects during the mature cycle accumulation
                 if(effect.isPassive) {
                     // if(effect.effectType == NodeEffectType.ScentModifier) Debug.Log($"   - Skipping ScentModifier because it's marked PASSIVE."); // Log if passive
                     continue;
                 }

                 // Debug.Log($"   - Processing Effect Type: {effect.effectType}"); // Optional per-effect log

                 switch (effect.effectType)
                 {
                    case NodeEffectType.EnergyCost:
                         totalEnergyCostForCycle += Mathf.Max(0f, effect.primaryValue);
                         break;
                    case NodeEffectType.Damage:
                         damageMultiplier = Mathf.Max(0.1f, damageMultiplier + effect.primaryValue);
                         break;
                    case NodeEffectType.ScentModifier:
                        // Check if the reference is valid
                        if (effect.scentDefinitionReference != null)
                        {
                             ScentDefinition key = effect.scentDefinitionReference;
                             // <<< ADDED SPECIFIC SCENTMODIFIER LOG >>>
                             Debug.Log($"   - FOUND ScentModifier for '{key.name}'. Passive={effect.isPassive}, RadiusBonus={effect.primaryValue}, StrengthBonus={effect.secondaryValue}");

                             // Add radius bonus
                            if (!accumulatedScentRadiusBonus.ContainsKey(key)) accumulatedScentRadiusBonus[key] = 0f;
                            accumulatedScentRadiusBonus[key] += effect.primaryValue;

                            // Add strength bonus
                            if (!accumulatedScentStrengthBonus.ContainsKey(key)) accumulatedScentStrengthBonus[key] = 0f;
                            accumulatedScentStrengthBonus[key] += effect.secondaryValue;
                        }
                        else {
                             // <<< ADDED NULL REFERENCE LOG >>>
                             Debug.LogWarning($"   - Node '{node.nodeDisplayName ?? "Unnamed"}' has ScentModifier effect but ScentDefinition reference is NULL.");
                        }
                        break;
                    // Other non-action, non-passive effects accumulate here if needed
                 }
            }
        }

        // Log results of accumulation
        Debug.Log($"[{gameObject.name} Cycle] Accumulation Complete. " +
                  $"Total Cost: {totalEnergyCostForCycle}, DamageMult: {damageMultiplier}, " +
                  $"Scent Radius Bonuses: {accumulatedScentRadiusBonus.Count}, " +
                  $"Scent Strength Bonuses: {accumulatedScentStrengthBonus.Count}");
        // Optional detailed log
        // foreach(var kvp in accumulatedScentStrengthBonus) { Debug.Log($"    -> {kvp.Key.name}: StrBonus={kvp.Value}"); }


        // --- Execution Phase ---
        if (currentEnergy < totalEnergyCostForCycle) {
             Debug.Log($"[{gameObject.name} Cycle] Execution skipped. Cost {totalEnergyCostForCycle} > Available {currentEnergy}");
             currentState = PlantState.Mature_Idle; cycleTimer = cycleCooldown; yield break;
        }
        currentEnergy = Mathf.Max(0f, currentEnergy - totalEnergyCostForCycle);
        UpdateUI();
        Debug.Log($"[{gameObject.name} Cycle] Starting Execution Phase. Remaining Energy: {currentEnergy}");

        foreach (var node in nodeGraph.nodes.OrderBy(n => n.orderIndex))
        {
            if (node.effects == null || node.effects.Count == 0) continue;

            bool hasActionEffectInNode = node.effects.Any(eff => !eff.isPassive &&
                                            eff.effectType != NodeEffectType.EnergyCost &&
                                            eff.effectType != NodeEffectType.Damage &&
                                            eff.effectType != NodeEffectType.ScentModifier);
            if (hasActionEffectInNode && nodeCastDelay > 0.01f) {
                 yield return new WaitForSeconds(nodeCastDelay);
            }

            foreach (var effect in node.effects)
            {
                 // Skip non-action effects here
                 if(effect.isPassive || effect.effectType == NodeEffectType.EnergyCost || effect.effectType == NodeEffectType.Damage || effect.effectType == NodeEffectType.ScentModifier) continue;

                 // Debug.Log($"   - Executing Action: {effect.effectType} from Node '{node.nodeDisplayName}'"); // Optional per-action log

                 switch (effect.effectType) {
                     case NodeEffectType.Output:
                        OutputNodeEffect outputComp = GetComponentInChildren<OutputNodeEffect>();
                        if (outputComp != null) {
                             outputComp.Activate(damageMultiplier, accumulatedScentRadiusBonus, accumulatedScentStrengthBonus);
                        } else { Debug.LogWarning($"[{gameObject.name}] Node requested Output effect, but no OutputNodeEffect component found.", this); }
                         break;
                     case NodeEffectType.GrowBerry:
                         TrySpawnBerry(accumulatedScentRadiusBonus, accumulatedScentStrengthBonus);
                         break;
                     // Other actions...
                 }
            }
        }

        cycleTimer = cycleCooldown;
        currentState = PlantState.Mature_Idle;
        Debug.Log($"[{gameObject.name} Cycle] Execution Phase Complete.");
    }


    // TrySpawnBerry (with Debug Log)
    private void TrySpawnBerry(Dictionary<ScentDefinition, float> scentRadiiBonus, Dictionary<ScentDefinition, float> scentStrengthsBonus)
    {
        if (berryCellPrefab == null) { Debug.LogWarning($"[{gameObject.name}] Berry Prefab not assigned.", gameObject); return; }
        // <<< ADDED LOG from previous step >>>
        Debug.Log($"[{gameObject.name} Cycle] TrySpawnBerry called. Passing {scentStrengthsBonus?.Count ?? 0} scent strength entries.");

        var potentialCoords = cells
            .SelectMany(cell => {
                List<Vector2Int> candidates = new List<Vector2Int>();
                if (cell.Value == PlantCellType.Leaf) candidates.Add(cell.Key + Vector2Int.down);
                else if (cell.Value == PlantCellType.Stem) candidates.Add(cell.Key + Vector2Int.up);
                return candidates;
            })
            .Where(coord => !cells.ContainsKey(coord))
            .Distinct()
            .ToList();

        if (potentialCoords.Count > 0) {
            SpawnCellVisual(PlantCellType.Fruit, potentialCoords[Random.Range(0, potentialCoords.Count)], scentRadiiBonus, scentStrengthsBonus);
        }
    }

    // ApplyScentDataToObject (with Debug Logs)
    public void ApplyScentDataToObject(GameObject targetObject, Dictionary<ScentDefinition, float> scentRadiusBonuses, Dictionary<ScentDefinition, float> scentStrengthBonuses)
    {
        // <<< ADDED LOG from previous step >>>
        Debug.Log($"ApplyScentDataToObject called for {targetObject.name}. StrengthBonus Dict has {scentStrengthBonuses?.Count ?? 0} entries.");

        if (targetObject == null) { Debug.LogError("ApplyScentDataToObject: targetObject is null."); return; }
        if (EcosystemManager.Instance == null) { Debug.LogError("ApplyScentDataToObject: EcosystemManager instance not found."); return; }
        if (EcosystemManager.Instance.scentLibrary == null) { Debug.LogWarning("ApplyScentDataToObject: Scent Library not assigned in EcosystemManager."); return; }


        ScentDefinition strongestScentDef = null;
        float maxStrengthBonus = -1f;

        if (scentStrengthBonuses != null && scentStrengthBonuses.Count > 0) {
            foreach (var kvp in scentStrengthBonuses) {
                if (kvp.Key != null && kvp.Value > maxStrengthBonus) {
                    maxStrengthBonus = kvp.Value;
                    strongestScentDef = kvp.Key;
                }
            }
        }

        if (strongestScentDef != null) {
            // <<< ADDED LOG from previous step >>>
            Debug.Log($" - Strongest scent found: {strongestScentDef.name}. Getting/Adding ScentSource component...");
            ScentSource scentSource = targetObject.GetComponent<ScentSource>();
            if (scentSource == null) {
                Debug.Log("   - ScentSource missing, adding component.");
                scentSource = targetObject.AddComponent<ScentSource>();
            } else {
                 Debug.Log("   - ScentSource already exists, configuring.");
                 if (scentSource.definition != null && scentSource.definition != strongestScentDef) {
                     Debug.LogWarning($"   - Overwriting existing scent '{scentSource.definition.name}' with '{strongestScentDef.name}' on {targetObject.name}");
                 }
            }

            scentSource.definition = strongestScentDef;
            scentRadiusBonuses.TryGetValue(strongestScentDef, out float radiusBonus);
            scentStrengthBonuses.TryGetValue(strongestScentDef, out float strengthBonus); // Strength bonus is maxStrengthBonus
            scentSource.radiusModifier = radiusBonus;
            scentSource.strengthModifier = strengthBonus;

            // <<< ADDED LOG from previous step >>>
            Debug.Log($"   - Configured ScentSource: Def={scentSource.definition?.name}, RadMod={scentSource.radiusModifier}, StrMod={scentSource.strengthModifier}, EffectiveRadius={scentSource.EffectiveRadius}");

            if (strongestScentDef.particleEffectPrefab != null) {
                bool particleExists = false; foreach(Transform child in targetObject.transform){ if(child.TryGetComponent<ParticleSystem>(out _)){ particleExists = true; break; } }
                if (!particleExists) { Instantiate(strongestScentDef.particleEffectPrefab, targetObject.transform.position, Quaternion.identity, targetObject.transform); }
            }
        } else {
             // <<< ADDED LOG from previous step >>>
             Debug.Log(" - No strongest scent found (maxStrengthBonus was <= -1 or dictionary empty/null). No ScentSource added/configured.");
        }
    }


    // --- UI Reference Helper ---
    private void EnsureUIReferences()
    {
        // (No changes needed)
        if (energyText) return;
        energyText = GetComponentInChildren<TMP_Text>(true);
        if (!energyText) Debug.LogWarning($"[{gameObject.name}] Energy Text (TMP_Text) not found in children.", gameObject);
    }
}