using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;

public enum PlantCellType
{
    Seed,
    Stem,
    Leaf,
    Flower,
    Fruit
}

public enum PlantState
{
    Initializing,
    Growing,
    Mature_Idle,
    Mature_Executing
}

public class PlantGrowth : MonoBehaviour
{
    [Header("Node Graph Data")]
    private NodeGraph nodeGraph;

    [Header("UI Display Options")]
    [Tooltip("If enabled, shows growth percentage during growth phase instead of energy")]
    [SerializeField] private bool showGrowthPercentage = true;
    
    [Tooltip("If enabled, plant will accumulate energy during growth phase")]
    [SerializeField] private bool allowPhotosynthesisDuringGrowth = false;
    
    [Tooltip("If enabled, shows a smooth percentage counter independent of actual growth")]
    [SerializeField] private bool useSmoothPercentageCounter = true;
    
    [Tooltip("How much to increment the percentage counter each step (%)")]
    [SerializeField] [Range(1, 10)] private int percentageIncrement = 2;

    [Header("Calculated Runtime Stats")]
    // Growth Stats
    private int finalStemMinLength = 3;
    private int finalStemMaxLength = 6;
    private float finalGrowthSpeed = 1f;
    private int finalLeafGap = 1;
    private int finalLeafPattern = 0;
    private float finalGrowthRandomness = 0f;
    // Energy Stats
    private float finalMaxEnergy = 10f;
    private float finalPhotosynthesisRate = 1f;
    // Mature Cycle Timing Stats
    private float cycleCooldown = 5.0f;
    private float nodeCastDelay = 0.1f;

    [Header("Current State")]
    public PlantState currentState = PlantState.Initializing;
    public float currentEnergy = 0f;
    private float cycleTimer = 0f;
    
    // For smooth percentage counter
    private float displayedGrowthPercentage = 0f;
    private Coroutine percentageCounterCoroutine;
    private float totalGrowthDuration;

    [Header("Growth Visuals & Logic")]
    [SerializeField] private GameObject seedCellPrefab;
    [SerializeField] private GameObject stemCellPrefab;
    [SerializeField] private GameObject leafCellPrefab;
    [SerializeField] private float cellSpacing = 8f;
    [SerializeField] private TMP_Text energyText;

    private Dictionary<Vector2Int, PlantCellType> cells = new Dictionary<Vector2Int, PlantCellType>();
    private int currentStemCount = 0;
    private int targetStemLength = 0;
    private bool? offsetRightForPattern1 = null;

    private void Awake()
    {
        EnsureUIReferences();
    }

    private void EnsureUIReferences()
    {
        if (energyText != null) return;

        // Try to find by name first
        Transform textTrans = transform.Find("Txt_Energy");
        
        // Try common parent paths if not found directly
        if (textTrans == null) textTrans = transform.Find("Canvas/Txt_Energy");
        if (textTrans == null) textTrans = transform.Find("UI/Txt_Energy");
        
        // If found by name, get the component
        if (textTrans != null)
        {
            energyText = textTrans.GetComponent<TMP_Text>();
            if (energyText != null)
            {
                Debug.Log($"[PlantGrowth] Found Energy Text by name: {textTrans.name}", gameObject);
                return;
            }
        }
        
        // If not found by name, try to find by type
        TMP_Text[] allTexts = GetComponentsInChildren<TMP_Text>(true);
        if (allTexts.Length > 0)
        {
            // Try to find one that might be for energy
            foreach (var text in allTexts)
            {
                string lowerName = text.name.ToLower();
                if (lowerName.Contains("energy") || lowerName.Contains("txt_e"))
                {
                    energyText = text;
                    Debug.Log($"[PlantGrowth] Found Energy Text by component search: {text.name}", gameObject);
                    return;
                }
            }
            
            // If no good match, just use the first one
            energyText = allTexts[0];
            Debug.Log($"[PlantGrowth] Using first TMP_Text found: {energyText.name}", gameObject);
        }
        else
        {
            Debug.LogWarning($"[PlantGrowth] No TMP_Text components found on {gameObject.name}. Energy display will not work.", gameObject);
        }
    }

    public void InitializeAndGrow(NodeGraph graph)
    {
        EnsureUIReferences();
        
        if (graph == null || graph.nodes == null)
        {
            Debug.LogError("[PlantGrowth] Cannot initialize with a null NodeGraph!", gameObject);
            Destroy(gameObject);
            return;
        }
        this.nodeGraph = graph;
        currentState = PlantState.Initializing;
        currentEnergy = 0f; // Reset energy at initialization
        
        // Reset percentage counter
        displayedGrowthPercentage = 0f;
        
        // Stop any existing percentage counter
        if (percentageCounterCoroutine != null)
        {
            StopCoroutine(percentageCounterCoroutine);
            percentageCounterCoroutine = null;
        }

        CalculateAndApplyStats();

        if (targetStemLength > 0)
        {
             StartGrowthVisuals();
        }
        else
        {
            Debug.LogWarning("[PlantGrowth] Target stem length calculated as 0 or less. Plant will not grow.", gameObject);
            currentState = PlantState.Mature_Idle;
            cycleTimer = cycleCooldown;
        }
        
        // Initialize UI based on the current state
        UpdateUI();
    }

    private void CalculateAndApplyStats()
    {
        if (nodeGraph == null) return;

        // --- Default Values ---
        float accumulatedEnergyStorage = 0f;
        float accumulatedPhotosynthesis = 0f;
        int baseStemMin = 3;
        int baseStemMax = 6;
        float baseGrowthSpeed = 1f;
        int baseLeafGap = 1;
        int baseLeafPattern = 0;
        float baseGrowthRandomness = 0f;
        float baseCooldown = 5.0f;
        float baseCastDelay = 0.1f;
        // Modifiers
        int stemLengthModifier = 0;
        float growthSpeedModifier = 0f;
        int leafGapModifier = 0;
        float growthRandomnessModifier = 0f;
        float cooldownModifier = 0f;
        float castDelayModifier = 0f;

        bool seedFound = false;

        foreach (NodeData node in nodeGraph.nodes.OrderBy(n => n.orderIndex))
        {
            if (node.effects == null) continue;

            foreach (NodeEffectData effect in node.effects)
            {
                if (!effect.isPassive) continue;

                switch (effect.effectType)
                {
                    case NodeEffectType.SeedSpawn:
                        seedFound = true;
                        break;
                    case NodeEffectType.EnergyStorage:
                        accumulatedEnergyStorage += effect.primaryValue;
                        break;
                    case NodeEffectType.EnergyPhotosynthesis:
                        accumulatedPhotosynthesis += effect.primaryValue;
                        break;
                    case NodeEffectType.StemLength:
                        stemLengthModifier += Mathf.RoundToInt(effect.primaryValue);
                        break;
                    case NodeEffectType.GrowthSpeed:
                        growthSpeedModifier += effect.primaryValue;
                        break;
                    case NodeEffectType.LeafGap:
                        leafGapModifier += Mathf.RoundToInt(effect.primaryValue);
                        break;
                    case NodeEffectType.LeafPattern:
                        baseLeafPattern = Mathf.RoundToInt(effect.primaryValue);
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
        }

        finalMaxEnergy = Mathf.Max(1f, accumulatedEnergyStorage);
        finalPhotosynthesisRate = Mathf.Max(0f, accumulatedPhotosynthesis);

        finalStemMinLength = Mathf.Max(1, baseStemMin + stemLengthModifier);
        finalStemMaxLength = Mathf.Max(finalStemMinLength, baseStemMax + stemLengthModifier);

        finalGrowthSpeed = Mathf.Max(0.1f, baseGrowthSpeed + growthSpeedModifier);
        finalLeafGap = Mathf.Max(0, baseLeafGap + leafGapModifier);
        finalLeafPattern = Mathf.Clamp(baseLeafPattern, 0, 4);
        finalGrowthRandomness = Mathf.Clamp01(baseGrowthRandomness + growthRandomnessModifier);

        cycleCooldown = Mathf.Max(0.1f, baseCooldown + cooldownModifier);
        nodeCastDelay = Mathf.Max(0.01f, baseCastDelay + castDelayModifier);

        targetStemLength = Random.Range(finalStemMinLength, finalStemMaxLength + 1);

        // Calculate total growth duration
        totalGrowthDuration = finalGrowthSpeed * targetStemLength;

        if (!seedFound)
        {
             Debug.LogWarning("[PlantGrowth] Node chain lacks a passive SeedSpawn effect. Plant will not grow.", gameObject);
             targetStemLength = 0;
        }
    }

    private void StartGrowthVisuals()
    {
        foreach (Transform child in transform) { 
            if (child.GetComponent<Canvas>() != null || child.GetComponent<TMP_Text>() != null) continue;
            if (energyText != null && energyText.transform == child) continue;
            
            Destroy(child.gameObject); 
        }
        cells.Clear();
        currentStemCount = 0;
        offsetRightForPattern1 = null;

        cells[Vector2Int.zero] = PlantCellType.Seed;
        SpawnCellVisual(PlantCellType.Seed, Vector2Int.zero);

        SortableEntity[] cellEntities = GetComponentsInChildren<SortableEntity>();
        foreach (var cellEntity in cellEntities) {
            if (cellEntity.transform != transform) cellEntity.SetUseParentYCoordinate(true);
        }

        currentState = PlantState.Growing;
        
        // Start the smooth percentage counter if enabled
        if (useSmoothPercentageCounter && showGrowthPercentage)
        {
            percentageCounterCoroutine = StartCoroutine(PercentageCounterRoutine());
        }
        
        // Start the actual growth routine
        StartCoroutine(GrowRoutine());
    }

    private IEnumerator PercentageCounterRoutine()
    {
        displayedGrowthPercentage = 0;
        UpdateUI();
        
        // Calculate time per percentage increment
        int totalSteps = 100 / percentageIncrement;
        float timePerStep = totalGrowthDuration / totalSteps;
        
        for (int step = 1; step <= totalSteps; step++)
        {
            // Wait the calculated time
            yield return new WaitForSeconds(timePerStep);
            
            // Check if we're still in growing state
            if (currentState != PlantState.Growing)
                break;
            
            // Update the percentage
            displayedGrowthPercentage = step * percentageIncrement;
            displayedGrowthPercentage = Mathf.Min(displayedGrowthPercentage, 100f); // Cap at 100%
            
            // Update the UI
            UpdateUI();
        }
        
        // Ensure we end at exactly 100%
        if (currentState == PlantState.Growing)
        {
            displayedGrowthPercentage = 100f;
            UpdateUI();
        }
    }

    private void Update()
    {
        switch (currentState)
        {
            case PlantState.Initializing:
                // Waiting for InitializeAndGrow call
                break;

            case PlantState.Growing:
                // Optionally accumulate energy during growth based on the flag
                if (allowPhotosynthesisDuringGrowth)
                {
                    AccumulateEnergy();
                    
                    // Only update UI if percentage is not shown (since percentage updates are handled by the counter)
                    if (!showGrowthPercentage)
                    {
                        UpdateUI();
                    }
                }
                break;

            case PlantState.Mature_Idle:
                // Always accumulate energy when mature
                AccumulateEnergy();
                UpdateUI();
                
                cycleTimer -= Time.deltaTime;
                if (cycleTimer <= 0f)
                {
                    currentState = PlantState.Mature_Executing;
                    StartCoroutine(ExecuteMatureCycle());
                }
                break;

            case PlantState.Mature_Executing:
                // Always accumulate energy during execution too
                AccumulateEnergy();
                UpdateUI();
                break;
        }
    }

    // Separated energy accumulation from UI updates
    private void AccumulateEnergy()
    {
        float sunlight = (WeatherManager.Instance != null) ? WeatherManager.Instance.sunIntensity : 1f;
        float leafCount = cells.Values.Count(c => c == PlantCellType.Leaf);
        float deltaPhotosynthesis = finalPhotosynthesisRate * leafCount * sunlight * Time.deltaTime;
        currentEnergy = Mathf.Clamp(currentEnergy + deltaPhotosynthesis, 0f, finalMaxEnergy);
    }

    private void UpdateUI()
    {
        if (energyText == null) EnsureUIReferences();
        if (energyText == null) return;
        
        switch (currentState)
        {
            case PlantState.Growing:
                if (showGrowthPercentage)
                {
                    // During growth phase, show the fixed-step counter if enabled, otherwise show real percentage
                    if (useSmoothPercentageCounter)
                    {
                        // Use the smooth counter value
                        int displayPercentage = Mathf.RoundToInt(displayedGrowthPercentage);
                        energyText.text = $"{displayPercentage}%";
                    }
                    else
                    {
                        // Calculate actual percentage
                        int percentage = targetStemLength <= 0 ? 0 : 
                            Mathf.RoundToInt((float)currentStemCount / targetStemLength * 100f);
                        energyText.text = $"{percentage}%";
                    }
                }
                else
                {
                    // Show energy instead of percentage if that's the preference
                    energyText.text = $"{Mathf.Floor(currentEnergy)}/{Mathf.Floor(finalMaxEnergy)}";
                }
                break;
                
            case PlantState.Mature_Idle:
            case PlantState.Mature_Executing:
                // After growth is complete, always show energy level
                energyText.text = $"{Mathf.Floor(currentEnergy)}/{Mathf.Floor(finalMaxEnergy)}";
                break;
                
            default:
                // Initializing or other states
                energyText.text = "...";
                break;
        }
    }

    private IEnumerator ExecuteMatureCycle()
    {
        if (nodeGraph == null || nodeGraph.nodes == null)
        {
             Debug.LogError($"[{gameObject.name}] NodeGraph is missing during mature cycle execution!", gameObject);
             currentState = PlantState.Mature_Idle;
             cycleTimer = cycleCooldown;
             yield break;
        }

        float damageMultiplier = 1.0f;

        var sortedNodes = nodeGraph.nodes.OrderBy(n => n.orderIndex).ToList();

        foreach (NodeData node in sortedNodes)
        {
             if (node.effects == null) continue;

             bool hasActiveEffect = node.effects.Any(eff => !eff.isPassive);

             if (hasActiveEffect)
             {
                 if (nodeCastDelay > 0)
                 {
                     yield return new WaitForSeconds(nodeCastDelay);
                 }

                 foreach (NodeEffectData effect in node.effects)
                 {
                     if (effect.isPassive) continue;

                     switch (effect.effectType)
                     {
                         case NodeEffectType.Output:
                             OutputNodeEffect outputEffect = GetComponentInChildren<OutputNodeEffect>();
                             if(outputEffect != null) {
                                outputEffect.Activate(damageMultiplier);
                             } else {
                                Debug.LogWarning($"[PlantGrowth] Output effect found on node '{node.nodeDisplayName}' but no OutputNodeEffect component found on plant '{gameObject.name}' or its children.", gameObject);
                             }
                             break;
                         case NodeEffectType.Damage:
                             damageMultiplier += effect.primaryValue;
                             break;
                     }
                 }
             }
        }

        cycleTimer = cycleCooldown;
        currentState = PlantState.Mature_Idle;
    }

    private IEnumerator GrowRoutine()
    {
        Vector2Int currentPos = Vector2Int.zero;
        int spiralDirection = 1;
        int patternCounter = 0;

        while (currentState == PlantState.Growing)
        {
            yield return new WaitForSeconds(finalGrowthSpeed);

            if (currentStemCount < targetStemLength)
            {
                currentStemCount++;
                Vector2Int dir = (currentStemCount == 1) ? Vector2Int.up : GetStemDirection();
                currentPos += dir;

                if (!cells.ContainsKey(currentPos)) {
                    cells[currentPos] = PlantCellType.Stem;
                    SpawnCellVisual(PlantCellType.Stem, currentPos);
                }

                if ((finalLeafGap >= 0) && (currentStemCount % (finalLeafGap + 1)) == 0)
                {
                    Vector2Int baseLeftPos = currentPos + Vector2Int.left;
                    Vector2Int baseRightPos = currentPos + Vector2Int.right;
                    patternCounter++;

                    ExecuteLeafPatternLogic(currentPos, baseLeftPos, baseRightPos, patternCounter, ref spiralDirection);
                }
                
                // Only update UI if we're showing actual percentage or energy
                if (showGrowthPercentage && !useSmoothPercentageCounter)
                {
                    UpdateUI();
                }
                else if (!showGrowthPercentage)
                {
                    UpdateUI();
                }
            }
            else
            {
                Debug.Log($"[{gameObject.name}] Growth complete. Transitioning to Mature_Idle state.");
                
                // Stop the percentage counter coroutine if it's running
                if (percentageCounterCoroutine != null)
                {
                    StopCoroutine(percentageCounterCoroutine);
                    percentageCounterCoroutine = null;
                }
                
                // Make sure percentage shows 100% at the end
                if (showGrowthPercentage)
                {
                    displayedGrowthPercentage = 100f;
                    UpdateUI();
                }
                
                currentState = PlantState.Mature_Idle;
                cycleTimer = cycleCooldown;
                
                // Final UI update to show energy instead of growth percentage
                UpdateUI();
                yield break;
            }
        }
    }

    private void ExecuteLeafPatternLogic(Vector2Int currentPos, Vector2Int baseLeftPos, Vector2Int baseRightPos, int patternCounter, ref int spiralDirection)
    {
        switch (finalLeafPattern)
        {
            case 0: // Parallel
                SpawnLeafIfEmpty(baseLeftPos); SpawnLeafIfEmpty(baseRightPos); break;
            case 1: // Offset-Parallel
                if (offsetRightForPattern1 == null) offsetRightForPattern1 = (Random.value < 0.5f);
                Vector2Int raisedLeft = baseLeftPos + Vector2Int.up;
                Vector2Int raisedRight = baseRightPos + Vector2Int.up;
                if (offsetRightForPattern1.Value) { SpawnLeafIfEmpty(baseLeftPos); SpawnLeafIfEmpty(raisedRight); }
                else { SpawnLeafIfEmpty(raisedLeft); SpawnLeafIfEmpty(baseRightPos); }
                break;
            case 2: // Alternating
                 Vector2Int leftPos, rightPos;
                 switch (patternCounter % 4) {
                     case 1: leftPos = baseLeftPos + Vector2Int.up; rightPos = baseRightPos; break;
                     case 2: leftPos = baseLeftPos; rightPos = baseRightPos; break;
                     case 3: leftPos = baseLeftPos; rightPos = baseRightPos + Vector2Int.up; break;
                     case 0: default: leftPos = baseLeftPos; rightPos = baseRightPos; break;
                 }
                 SpawnLeafIfEmpty(leftPos); SpawnLeafIfEmpty(rightPos);
                 break;
            case 3: // Double-Spiral
                 Vector2Int leftSpiral = baseLeftPos + new Vector2Int(0, spiralDirection > 0 ? 1 : 0);
                 Vector2Int rightSpiral = baseRightPos + new Vector2Int(0, spiralDirection > 0 ? 0 : 1);
                 SpawnLeafIfEmpty(leftSpiral); SpawnLeafIfEmpty(rightSpiral);
                 spiralDirection *= -1; // Toggle spiral
                 break;
            case 4: // One-Sided (Right side example)
                 SpawnLeafIfEmpty(baseRightPos); SpawnLeafIfEmpty(baseRightPos + Vector2Int.up); break;
            default: // Fallback
                 SpawnLeafIfEmpty(baseLeftPos); SpawnLeafIfEmpty(baseRightPos); break;
        }
    }

    private Vector2Int GetStemDirection()
    {
        Vector2Int up = Vector2Int.up;
        Vector2Int leftDiag = new Vector2Int(-1, 1);
        Vector2Int rightDiag = new Vector2Int(1, 1);

        float r = finalGrowthRandomness;
        if (Random.value < (1f - r)) return up;
        else return (Random.value < 0.5f) ? leftDiag : rightDiag;
    }

    private void SpawnLeafIfEmpty(Vector2Int coords)
    {
        if (!cells.ContainsKey(coords))
        {
            cells[coords] = PlantCellType.Leaf;
            SpawnCellVisual(PlantCellType.Leaf, coords);
        }
    }

    private void SpawnCellVisual(PlantCellType cellType, Vector2Int coords)
    {
        Vector2 worldPos = (Vector2)transform.position + (Vector2)coords * cellSpacing;
        GameObject prefabToUse = null;
        switch (cellType) {
            case PlantCellType.Seed: prefabToUse = seedCellPrefab; break;
            case PlantCellType.Stem: prefabToUse = stemCellPrefab; break;
            case PlantCellType.Leaf: prefabToUse = leafCellPrefab; break;
        }
        if (prefabToUse != null) {
            GameObject cellInstance = Instantiate(prefabToUse, worldPos, Quaternion.identity, transform);
            SortableEntity sortableEntity = cellInstance.GetComponent<SortableEntity>() ?? cellInstance.AddComponent<SortableEntity>();
            if (cellType != PlantCellType.Seed) {
                sortableEntity.SetUseParentYCoordinate(true);
            }
        } else Debug.LogWarning($"[PlantGrowth] No prefab assigned for cell type {cellType}");
    }
    
    private void OnDestroy()
    {
        // Ensure coroutines are stopped when object is destroyed
        if (percentageCounterCoroutine != null)
        {
            StopCoroutine(percentageCounterCoroutine);
            percentageCounterCoroutine = null;
        }
    }
}