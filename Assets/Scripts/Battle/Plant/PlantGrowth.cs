using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;

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
    [SerializeField] private GameObject berryCellPrefab;
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
    
    private FireflyManager fireflyManagerInstance;

    // --- Unity Methods ---
    private void Update() => StateMachineUpdate();
    private void OnDestroy() => StopAllCoroutines(); // Ensure cleanup
    
    private void Awake()
    {
        EnsureUIReferences();
        fireflyManagerInstance = FireflyManager.Instance; // Get the singleton instance
        if (fireflyManagerInstance == null)
        {
            // Optional: Log warning if manager doesn't exist, photosynthesis bonus won't work
            // Debug.LogWarning($"[{gameObject.name}] FireflyManager instance not found. Firefly photosynthesis bonus disabled.");
        }
    }

    // --- Public Initialization ---
    public void InitializeAndGrow(NodeGraph graph)
    {
        if (graph == null || graph.nodes == null) {
            Debug.LogError($"[{gameObject.name}] Null/empty NodeGraph provided.", gameObject); Destroy(gameObject); return;
        }
        nodeGraph = graph;
        currentState = PlantState.Initializing;
        currentEnergy = 0f;
        displayedGrowthPercentage = 0f;
        if (percentageCounterCoroutine != null) { StopCoroutine(percentageCounterCoroutine); percentageCounterCoroutine = null; }

        CalculateAndApplyStats();

        if (targetStemLength > 0) {
            StartGrowthVisuals();
        } else { // Skip visual growth
             Debug.LogWarning($"[{gameObject.name}] Target stem length is {targetStemLength}. Skipping visual growth.", gameObject);
            currentState = PlantState.Mature_Idle;
            cycleTimer = cycleCooldown;
            if (!cells.ContainsKey(Vector2Int.zero)) { SpawnCellVisual(PlantCellType.Seed, Vector2Int.zero); }
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
                if (cycleTimer <= 0f && currentEnergy >= 1f) // Basic energy check
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

    private void AccumulateEnergy()
    {
        float sunlight = WeatherManager.Instance ? WeatherManager.Instance.sunIntensity : 1f;
        int leafCount = cells.Values.Count(c => c == PlantCellType.Leaf);

        // --- Calculate Firefly Photosynthesis Bonus ---
        float fireflyBonusRate = 0f;
        if (fireflyManagerInstance != null) // Check if manager exists
        {
            int nearbyFlyCount = fireflyManagerInstance.GetNearbyFireflyCount(transform.position, fireflyManagerInstance.photosynthesisRadius);
            fireflyBonusRate = Mathf.Min(nearbyFlyCount * fireflyManagerInstance.photosynthesisIntensityPerFly,
                fireflyManagerInstance.maxPhotosynthesisBonus);
        }

        // --- Combine Photosynthesis Sources ---
        float standardPhotosynthesis = finalPhotosynthesisRate * leafCount * sunlight;
        float totalRate = standardPhotosynthesis + fireflyBonusRate; // Add the bonus

        float delta = totalRate * Time.deltaTime;
        currentEnergy = Mathf.Clamp(currentEnergy + delta, 0f, finalMaxEnergy);
    }

    private void UpdateUI()
    {
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
            // Debug.Log($"Cell at {coord} reported destroyed and removed from dictionary.");
        }
    }

    // --- Stat Calculation ---
        /// <summary>
        /// <summary>
    /// Calculates final plant stats (growth speed, max energy, etc.)
    /// based on the SUM of passive effects from the assigned NodeGraph.
    /// </summary>
    private void CalculateAndApplyStats()
    {
        if (nodeGraph == null)
        {
            Debug.LogError($"[{gameObject.name}] CalculateAndApplyStats called with null NodeGraph!");
            return;
        }

        // --- Define BASE values (Defaults if NO relevant Node Effect modifies them) ---
        // *** CRITICAL: Set bases to 0 or identity values if effects should fully define the stat ***
        float baseEnergyStorage = 0f;           // <<< FIX 1: Set base to 0. Effects will define the total.
        float basePhotosynthesisRate = 0f;      // <<< FIX 2: Set base rate to 0. Effects define total rate per leaf.
        int baseStemMin = 0;                    // Base range if no modifier
        int baseStemMax = 0;
        float baseGrowthSpeed = 0f;             // <<< FIX 3: Base time (seconds) per step. Effects ADD to this time.
        int baseLeafGap = 0;                    // <<< FIX 4: Base gap. 0 = leaves every stem. Effects ADD to this gap.
        int baseLeafPattern = 0;
        float baseGrowthRandomness = 0f;
        float baseCooldown = 0f;
        float baseCastDelay = 0f;

        // --- Accumulators for Modifiers from Node Effects ---
        float accumulatedEnergyStorage = 0f;
        float accumulatedPhotosynthesis = 0f;
        int stemLengthModifier = 0;
        float growthSpeedTimeModifier = 0f;     // How much time to ADD to baseGrowthSpeed
        int leafGapModifier = 0;
        int currentLeafPattern = baseLeafPattern;
        float growthRandomnessModifier = 0f;
        float cooldownModifier = 0f;
        float castDelayModifier = 0f;
        bool seedFound = false;

        // --- Iterate through nodes and their PASSIVE effects ---
        // Debug.Log($"[{gameObject.name}] Calculating Stats..."); // Optional: Start log
        foreach (NodeData node in nodeGraph.nodes.OrderBy(n => n.orderIndex))
        {
            if (node.effects == null) continue;
            // string nodeEffectsLog = $"Node '{node.nodeDisplayName}': "; // Optional: Log effects per node
            foreach (NodeEffectData effect in node.effects)
            {
                if (!effect.isPassive) continue; // Only process PASSIVE effects here

                // nodeEffectsLog += $" {effect.effectType}({effect.primaryValue})"; // Optional: Log effects per node

                switch (effect.effectType)
                {
                    case NodeEffectType.SeedSpawn:
                        seedFound = true;
                        break;
                    case NodeEffectType.EnergyStorage:
                        // Debug.Log($"  -> Found EnergyStorage: +{effect.primaryValue}"); // Optional: Log specific effect
                        accumulatedEnergyStorage += effect.primaryValue;
                        break;
                    case NodeEffectType.EnergyPhotosynthesis:
                        // Debug.Log($"  -> Found EnergyPhotosynthesis: +{effect.primaryValue}"); // Optional
                        accumulatedPhotosynthesis += effect.primaryValue;
                        break;
                    case NodeEffectType.StemLength:
                        stemLengthModifier += Mathf.RoundToInt(effect.primaryValue);
                        break;
                    case NodeEffectType.GrowthSpeed:
                        // Positive value = Slower (Adds Time), Negative = Faster (Subtracts Time)
                        // Debug.Log($"  -> Found GrowthSpeed Mod: {effect.primaryValue} (Time Adj)"); // Optional
                        growthSpeedTimeModifier += effect.primaryValue;
                        break;
                    case NodeEffectType.LeafGap:
                        // Debug.Log($"  -> Found LeafGap Mod: +{Mathf.RoundToInt(effect.primaryValue)}"); // Optional
                        leafGapModifier += Mathf.RoundToInt(effect.primaryValue);
                        break;
                    case NodeEffectType.LeafPattern:
                        currentLeafPattern = Mathf.RoundToInt(effect.primaryValue);
                        break;
                    case NodeEffectType.StemRandomness:
                        growthRandomnessModifier += effect.primaryValue;
                        break;
                    case NodeEffectType.Cooldown:
                        cooldownModifier += effect.primaryValue;
                        break;
                    case NodeEffectType.CastDelay:
                        castDelayModifier += effect.primaryValue;
                        break;
                }
            }
             // Debug.Log(nodeEffectsLog); // Optional: Log effects per node
        }

        // --- Final Stat Calculation & Clamping ---

        // 1. Max Energy: Base (now 0) + Accumulated. Min value of 1.
        finalMaxEnergy = Mathf.Max(1f, baseEnergyStorage + accumulatedEnergyStorage);
        // This should now directly reflect the sum of EnergyStorage effects (or 1 if sum is < 1).

        // 2. Photosynthesis Rate: Base (now 0) + Accumulated. Min 0. Rate PER LEAF.
        finalPhotosynthesisRate = Mathf.Max(0f, basePhotosynthesisRate + accumulatedPhotosynthesis);
        // This reflects the sum of EnergyPhotosynthesis effects.

        // 3. Stem Length
        int finalStemMin = Mathf.Max(1, baseStemMin + stemLengthModifier);
        int finalStemMax = Mathf.Max(finalStemMin, baseStemMax + stemLengthModifier);

        // 4. Growth Speed (Time per step): Base Time + Time Modifier. Min 0.1s.
        finalGrowthSpeed = Mathf.Max(0.1f, baseGrowthSpeed + growthSpeedTimeModifier);
        // If base=1s, effect=+0.5 -> 1.5s/step (Slower). If effect=-0.5 -> 0.5s/step (Faster).

        // 5. Leaf Gap: Base Gap (now 0) + Modifier. Min 0.
        finalLeafGap = Mathf.Max(0, baseLeafGap + leafGapModifier);
        // Gap=0 -> Leaves every stem (Modulo Check: % (0+1) == 0 -> always true)
        // Gap=1 -> Leaves every 2nd stem (Modulo Check: % (1+1) == 0 -> true for 2,4,6...)
        // Gap=2 -> Leaves every 3rd stem (Modulo Check: % (2+1) == 0 -> true for 3,6,9...)

        // 6. Other Stats
        finalLeafPattern = Mathf.Clamp(currentLeafPattern, 0, 4); // Assuming patterns 0-4
        finalGrowthRandomness = Mathf.Clamp01(baseGrowthRandomness + growthRandomnessModifier);
        cycleCooldown = Mathf.Max(0.1f, baseCooldown + cooldownModifier);
        nodeCastDelay = Mathf.Max(0.01f, baseCastDelay + castDelayModifier);

        // --- Instance Specific ---
        targetStemLength = seedFound ? Random.Range(finalStemMin, finalStemMax + 1) : 0;
        totalGrowthDuration = targetStemLength * finalGrowthSpeed;

        if (!seedFound) {
             Debug.LogWarning($"[{gameObject.name}] NodeGraph lacks SeedSpawn effect. Growth aborted.", gameObject);
        }

        // --- FINAL DEBUG LOG --- (Highly recommended until stats are confirmed correct)
         Debug.Log($"[{gameObject.name}] === STATS CALCULATED ===\n" +
                   $"  Max Energy: {finalMaxEnergy} (Base:{baseEnergyStorage} + Acc:{accumulatedEnergyStorage})\n" +
                   $"  Photo Rate (Per Leaf): {finalPhotosynthesisRate} (Base:{basePhotosynthesisRate} + Acc:{accumulatedPhotosynthesis})\n" +
                   $"  Growth Speed (Time/Step): {finalGrowthSpeed} (Base:{baseGrowthSpeed} + Mod:{growthSpeedTimeModifier})\n" +
                   $"  Leaf Gap: {finalLeafGap} (Base:{baseLeafGap} + Mod:{leafGapModifier})\n" +
                   $"  Target Length: {targetStemLength} (Range: {finalStemMin}-{finalStemMax})\n" +
                   $"  Pattern: {finalLeafPattern} | Randomness: {finalGrowthRandomness:P0}\n" +
                   $"  Cooldown: {cycleCooldown} | Cast Delay: {nodeCastDelay}\n" +
                   $"  Seed Found: {seedFound}");
        // --- END FINAL DEBUG LOG ---
    }

    // --- Visual Growth & Spawning ---
    private void StartGrowthVisuals()
    {
        // Clear existing cells (excluding UI)
        foreach (Transform child in transform) {
            if (child != transform && child.GetComponent<Canvas>() == null && (energyText == null || child != energyText.transform)) {
                Destroy(child.gameObject);
            }
        }
        cells.Clear();
        currentStemCount = 0;
        offsetRightForPattern1 = null;

        SpawnCellVisual(PlantCellType.Seed, Vector2Int.zero); // Spawn seed

        currentState = PlantState.Growing;
        if (useSmoothPercentageCounter && showGrowthPercentage) {
            percentageCounterCoroutine = StartCoroutine(PercentageCounterRoutine());
        }
        StartCoroutine(GrowRoutine());
    }

    private IEnumerator PercentageCounterRoutine()
    {
        // Smoothly updates displayed percentage UI during growth
        displayedGrowthPercentage = 0; UpdateUI();
        if (totalGrowthDuration <= 0 || percentageIncrement <= 0) yield break;
        int steps = Mathf.Max(1, 100 / percentageIncrement);
        float timePerStep = totalGrowthDuration / steps;
        for (int i = 1; i <= steps; i++) {
            yield return new WaitForSeconds(timePerStep);
            if (currentState != PlantState.Growing) break;
            displayedGrowthPercentage = Mathf.Min(i * percentageIncrement, 100f);
            UpdateUI();
        }
        if (currentState == PlantState.Growing || currentState == PlantState.Mature_Idle) {
            displayedGrowthPercentage = 100f; UpdateUI(); // Ensure 100% at end
        }
        percentageCounterCoroutine = null;
    }

    private IEnumerator GrowRoutine()
    {
        Vector2Int currentPos = Vector2Int.zero;
        int spiralDir = 1, patternCount = 0;

        while (currentState == PlantState.Growing)
        {
            // Check if target length reached *before* the wait to fix UI lag
            if (currentStemCount >= targetStemLength)
            {
                // --- GROWTH COMPLETE ---
                 // Debug.Log($"[{gameObject.name}] Growth complete ({currentStemCount}/{targetStemLength}).");
                if (percentageCounterCoroutine != null) {
                    StopCoroutine(percentageCounterCoroutine); percentageCounterCoroutine = null;
                    if (showGrowthPercentage) { displayedGrowthPercentage = 100f; UpdateUI(); } // Ensure 100% shows briefly if needed
                }
                currentState = PlantState.Mature_Idle; // Change state HERE
                cycleTimer = cycleCooldown;
                UpdateUI(); // Update UI to show energy *immediately*
                yield break; // Exit coroutine
            }

            // Wait for the growth interval
            yield return new WaitForSeconds(finalGrowthSpeed);

            // --- PERFORM GROWTH STEP --- (Only if not completed above)
            currentStemCount++;
            Vector2Int growthDir = (currentStemCount == 1) ? Vector2Int.up : GetStemDirection();
            currentPos += growthDir;

            SpawnCellVisual(PlantCellType.Stem, currentPos); // Spawns Stem and adds to dictionary

            // Leaf spawning logic
            if ((finalLeafGap >= 0) && (currentStemCount % (finalLeafGap + 1)) == 0) {
                patternCount++;
                ExecuteLeafPatternLogic(currentPos, currentPos + Vector2Int.left, currentPos + Vector2Int.right, patternCount, ref spiralDir);
            }

            // Update non-smooth UI if needed
            if (showGrowthPercentage && !useSmoothPercentageCounter) UpdateUI();
            else if (!showGrowthPercentage) UpdateUI();

        } // End while loop
    }


    private void ExecuteLeafPatternLogic(Vector2Int stemPos, Vector2Int leftBase, Vector2Int rightBase, int counter, ref int spiralDir)
    {
        // Spawns leaves based on finalLeafPattern
        switch (finalLeafPattern) {
            case 0: SpawnLeafIfEmpty(leftBase); SpawnLeafIfEmpty(rightBase); break; // Parallel
            case 1: // Offset-Parallel
                if (offsetRightForPattern1 == null) offsetRightForPattern1 = Random.value < 0.5f;
                if (offsetRightForPattern1.Value) { SpawnLeafIfEmpty(leftBase); SpawnLeafIfEmpty(rightBase + Vector2Int.up); }
                else { SpawnLeafIfEmpty(leftBase + Vector2Int.up); SpawnLeafIfEmpty(rightBase); }
                break;
            case 2: // Alternating Height
                 switch (counter % 4) { default: case 0: case 2: SpawnLeafIfEmpty(leftBase); SpawnLeafIfEmpty(rightBase); break; case 1: SpawnLeafIfEmpty(leftBase + Vector2Int.up); SpawnLeafIfEmpty(rightBase); break; case 3: SpawnLeafIfEmpty(leftBase); SpawnLeafIfEmpty(rightBase + Vector2Int.up); break; } break;
            case 3: // Double-Spiral
                 SpawnLeafIfEmpty(leftBase + new Vector2Int(0, spiralDir > 0 ? 1 : 0)); SpawnLeafIfEmpty(rightBase + new Vector2Int(0, spiralDir > 0 ? 0 : 1)); spiralDir *= -1; break;
            case 4: SpawnLeafIfEmpty(rightBase); SpawnLeafIfEmpty(rightBase + Vector2Int.up); break; // One-Sided Right Example
        }
    }

    private Vector2Int GetStemDirection()
    {
        // Returns Up, UpLeft, or UpRight based on randomness
        if (Random.value < finalGrowthRandomness) return (Random.value < 0.5f) ? Vector2Int.left + Vector2Int.up : Vector2Int.right + Vector2Int.up;
        return Vector2Int.up;
    }

    private void SpawnLeafIfEmpty(Vector2Int coords)
    {
        if (!cells.ContainsKey(coords)) SpawnCellVisual(PlantCellType.Leaf, coords);
    }

    /// <summary>
    /// Instantiates cell prefab, adds to dictionary, configures PlantCell component.
    /// </summary>
    private void SpawnCellVisual(PlantCellType cellType, Vector2Int coords)
    {
        // Check if already exists (e.g., stem collision)
        if (cells.ContainsKey(coords) && cells[coords] != cellType) {
             Debug.LogWarning($"Spawn collision at {coords}. Overwriting {cells[coords]} with {cellType}.", gameObject);
            // Optionally destroy old visual first
        } else if (cells.ContainsKey(coords)) {
            // Already exists with same type, do nothing (e.g. leaf overlap check)
            return;
        }

        GameObject prefab = null;
        switch (cellType) {
            case PlantCellType.Seed: prefab = seedCellPrefab; break;
            case PlantCellType.Stem: prefab = stemCellPrefab; break;
            case PlantCellType.Leaf: prefab = leafCellPrefab; break;
            case PlantCellType.Fruit: prefab = berryCellPrefab; break;
        }

        if (prefab != null) {
            Vector2 worldPos = (Vector2)transform.position + (Vector2)coords * cellSpacing;
            GameObject instance = Instantiate(prefab, worldPos, Quaternion.identity, transform);

            // --- Setup PlantCell Component ---
            PlantCell cellComp = instance.GetComponent<PlantCell>();
            if (cellComp == null) {
                 Debug.LogError($"Prefab '{prefab.name}' for {cellType} is MISSING the PlantCell component!", instance);
            } else {
                cellComp.ParentPlantGrowth = this; // Link back to parent
                cellComp.GridCoord = coords;       // Store its grid position
                cellComp.CellType = cellType;      // Store its type
            }
            // --- End Setup PlantCell ---

            cells[coords] = cellType; // Add/Update dictionary entry *after* potential warning

            SortableEntity sorter = instance.GetComponent<SortableEntity>() ?? instance.AddComponent<SortableEntity>();
            if (cellType != PlantCellType.Seed) sorter.SetUseParentYCoordinate(true);

            // Optional FoodItem validation
            if (cellType == PlantCellType.Fruit && (instance.GetComponent<FoodItem>() == null || instance.GetComponent<FoodItem>().foodType == null)) {
                 Debug.LogError($"Spawned Berry Prefab '{prefab.name}' at {coords} has MISSING/unassigned FoodItem!", instance);
            }
        } else {
             Debug.LogWarning($"[{gameObject.name}] No prefab assigned for {cellType}.", gameObject);
        }
    }


    // --- Mature Cycle ---
    private IEnumerator ExecuteMatureCycle()
    {
        if (nodeGraph?.nodes == null) {
            Debug.LogError($"[{gameObject.name}] NodeGraph missing!", gameObject);
            currentState = PlantState.Mature_Idle; cycleTimer = cycleCooldown; yield break;
        }

        float damageMultiplier = 1.0f;

        foreach (var node in nodeGraph.nodes.OrderBy(n => n.orderIndex)) {
            if (node.effects == null || node.effects.Count == 0) continue;

            // Calculate cost and find other active effects
            bool hasAnyActiveEffect = false;
            float nodeEnergyCost = 0f;
            List<NodeEffectData> otherActiveEffects = new List<NodeEffectData>();
            foreach (var effect in node.effects) {
                if (!effect.isPassive) {
                    hasAnyActiveEffect = true;
                    if (effect.effectType == NodeEffectType.EnergyCost) nodeEnergyCost += Mathf.Max(0f, effect.primaryValue);
                    else otherActiveEffects.Add(effect);
                }
            }

            if (hasAnyActiveEffect) {
                // Apply delay
                if (nodeCastDelay > 0.01f) yield return new WaitForSeconds(nodeCastDelay);

                // Check Energy
                if (currentEnergy >= nodeEnergyCost) {
                    // Consume energy
                    currentEnergy = Mathf.Max(0f, currentEnergy - nodeEnergyCost);

                    // Execute other effects
                    foreach (var activeEffect in otherActiveEffects) {
                        switch (activeEffect.effectType) {
                            case NodeEffectType.Output: GetComponentInChildren<OutputNodeEffect>()?.Activate(damageMultiplier); break;
                            case NodeEffectType.Damage: damageMultiplier = Mathf.Max(0.1f, damageMultiplier + activeEffect.primaryValue); break;
                            case NodeEffectType.GrowBerry: TrySpawnBerry(); break;
                                // Add other active cases
                        }
                    }
                     // Debug.Log($"Node '{node.nodeDisplayName}' activated (Cost: {nodeEnergyCost})");
                } else {
                     // Debug.Log($"Node '{node.nodeDisplayName}' skipped (Cost: {nodeEnergyCost}, Available: {currentEnergy})");
                     // Add fizzle feedback?
                }
            }
        } // End foreach node

        cycleTimer = cycleCooldown;
        currentState = PlantState.Mature_Idle;
    }

    private void TrySpawnBerry()
    {
        if (berryCellPrefab == null) { Debug.LogWarning($"[{gameObject.name}] Berry Prefab not assigned.", gameObject); return; }

        // Find empty spots below leaves or above stems
        var potentialCoords = cells
            .SelectMany(cell => {
                List<Vector2Int> candidates = new List<Vector2Int>();
                if (cell.Value == PlantCellType.Leaf) candidates.Add(cell.Key + Vector2Int.down);
                else if (cell.Value == PlantCellType.Stem) candidates.Add(cell.Key + Vector2Int.up);
                return candidates;
            })
            .Where(coord => !cells.ContainsKey(coord)) // Filter out occupied coords
            .Distinct()
            .ToList();

        if (potentialCoords.Count > 0) {
            SpawnCellVisual(PlantCellType.Fruit, potentialCoords[Random.Range(0, potentialCoords.Count)]);
        }
    }

    // --- UI Reference Helper ---
    private void EnsureUIReferences()
    {
        if (energyText) return;
        // Simplified search logic from previous versions
        energyText = GetComponentInChildren<TMP_Text>(true); // Find first active or inactive
        if (!energyText) Debug.LogWarning($"[{gameObject.name}] Energy Text (TMP_Text) not found in children.", gameObject);
    }
}