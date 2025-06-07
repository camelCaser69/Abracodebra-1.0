using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq; // Keep for OrderBy

public partial class PlantGrowth : MonoBehaviour
{
    // ------------------------------------------------
    // --- TIME-BASED GROWTH SYSTEM ---                // <<< NEW SYSTEM
    // ------------------------------------------------

    // Helper class to store pre-calculated growth steps (same as before)
    private class GrowthStep
    {
        public PlantCellType CellType;
        public Vector2Int Position;
        public int StemIndex; // Track which stem this belongs to (for percentage)
    }

    // --- GrowthCoroutine_TimeBased ---              // <<< FIXED COROUTINE
    private IEnumerator GrowthCoroutine_TimeBased()
    {
        if (targetStemLength <= 0)
        {
            currentState = PlantState.GrowthComplete;
            growthCoroutine = null;
            yield break;
        }

        List<GrowthStep> growthPlan = PreCalculateGrowthPlan();
        
        // Reset all growth tracking variables
        totalPlannedSteps = growthPlan.Count;
        stepsCompleted = 0;
        actualGrowthProgress = 0f;
        
        if (totalPlannedSteps == 0)
        {
            currentState = PlantState.GrowthComplete;
            growthCoroutine = null;
            yield break;
        }

        // --- Count stem steps for better time estimation ---
        int stemStepsCount = growthPlan.Count(step => step.CellType == PlantCellType.Stem);
        if (stemStepsCount == 0) stemStepsCount = 1; // Safety check

        // --- Initialize Time Variables ---
        currentGrowthElapsedTime = 0f; // Reset elapsed time for this growth cycle
        float baseTimePerStep = finalGrowthSpeed;
        if (baseTimePerStep <= 0.001f) baseTimePerStep = 0.01f;

        // --- Calculate Initial Estimated Total Time (for continuous mode) ---
        float initialTileMultiplier = 1.0f;
        if (PlantGrowthModifierManager.Instance != null)
        {
            initialTileMultiplier = PlantGrowthModifierManager.Instance.GetGrowthSpeedMultiplier(this);
            initialTileMultiplier = Mathf.Clamp(initialTileMultiplier, 0.1f, 10.0f);
        }
        float initialEffectiveTimePerStep = baseTimePerStep / initialTileMultiplier;
        if (initialEffectiveTimePerStep < 0.001f) initialEffectiveTimePerStep = 0.001f;
        
        // --- Use stem count for time estimate, not total steps ---
        estimatedTotalGrowthTime = stemStepsCount * initialEffectiveTimePerStep;
        // Ensure estimated time is at least a small positive value
        if (estimatedTotalGrowthTime < 0.01f) estimatedTotalGrowthTime = 0.01f;

        float lastUpdateTime = Time.time;
        float updateProgressInterval = 0.1f; // Update progress at least every 0.1 seconds

        // --- Track actual progress toward next step ---
        float progressTowardNextStep = 0f;

        // --- Growth Loop ---
        while (stepsCompleted < totalPlannedSteps && currentState == PlantState.Growing)
        {
            // 1. Get Current Frame's Delta Time
            float frameDeltaTime = Time.deltaTime;
            currentGrowthElapsedTime += frameDeltaTime;

            // 2. Get Current Tile Modifier
            float currentTileMultiplier = 1.0f;
            if (PlantGrowthModifierManager.Instance != null)
            {
                currentTileMultiplier = PlantGrowthModifierManager.Instance.GetGrowthSpeedMultiplier(this);
                currentTileMultiplier = Mathf.Clamp(currentTileMultiplier, 0.1f, 10.0f);
            }

            // 3. Calculate current effective time per step
            float currentEffectiveTimePerStep = baseTimePerStep / currentTileMultiplier;
            if (currentEffectiveTimePerStep < 0.001f) currentEffectiveTimePerStep = 0.001f;

            // 4. FIXED: Calculate progress rate - how quickly we accumulate progress relative to base rate
            //    When growth speed increases, we accumulate progress faster proportionally
            float progressRate = initialEffectiveTimePerStep / currentEffectiveTimePerStep;
            
            // 5. Add progress toward next step based on adjusted progress rate
            progressTowardNextStep += frameDeltaTime * progressRate;
            
            // 6. Calculate partial progress (0-1) toward next step for UI smoothness
            actualGrowthProgress = Mathf.Clamp01(progressTowardNextStep / initialEffectiveTimePerStep);
            
            // 7. Check if we've accumulated enough progress for one or more steps
            int stepsToProcessThisFrame = 0;
            
            while (progressTowardNextStep >= initialEffectiveTimePerStep && stepsCompleted < totalPlannedSteps) 
            {
                stepsToProcessThisFrame++;
                progressTowardNextStep -= initialEffectiveTimePerStep;
                
                // Limit steps per frame to prevent lag
                if (stepsToProcessThisFrame >= 3) break;
            }

            // 8. Process the steps
            if (stepsToProcessThisFrame > 0)
            {
                for (int i = 0; i < stepsToProcessThisFrame; i++)
                {
                    int currentPlanIndex = stepsCompleted;
                    if (currentPlanIndex >= totalPlannedSteps) break;

                    GrowthStep step = growthPlan[currentPlanIndex];
                    GameObject spawnedCell = SpawnCellVisual(step.CellType, step.Position, null, null);

                    // Update stem count for discrete percentage display
                    if (step.CellType == PlantCellType.Stem)
                    {
                        currentStemCount = step.StemIndex;
                        if (!continuousIncrement) // Only update UI from here if discrete
                        {
                             UpdateGrowthPercentageUI();
                        }
                    }
                    
                    stepsCompleted++;
                }
                
                lastUpdateTime = Time.time;
                
                // Update partial progress after steps
                actualGrowthProgress = Mathf.Clamp01(progressTowardNextStep / initialEffectiveTimePerStep);
            }
            
            // 9. Update UI based on time interval regardless of steps processed
            if (Time.time - lastUpdateTime > updateProgressInterval)
            {
                if (continuousIncrement)
                {
                    UpdateGrowthPercentageUI();
                }
                lastUpdateTime = Time.time;
            }

            // 10. Yield
            yield return null;
        }

        // Final update to ensure we reach 100%
        currentState = PlantState.GrowthComplete;
        stepsCompleted = totalPlannedSteps;
        actualGrowthProgress = 1.0f;
        growthCoroutine = null;
    }


    // --- PreCalculateGrowthPlan ---                     // <<< NEW HELPER
    /// <summary>
    /// Generates the full sequence of stem and leaf placements based on calculated stats.
    /// </summary>
    /// <returns>A list of GrowthStep objects defining the growth sequence.</returns>
    private List<GrowthStep> PreCalculateGrowthPlan()
    {
        List<GrowthStep> plan = new List<GrowthStep>();
        Vector2Int currentPos = Vector2Int.zero; // Start relative to the seed
        int spiralDir = 1; // Used for spiral pattern
        int patternCount = 0; // Used for alternating patterns

        // Simulate the growth stem by stem
        for (int stemIndex = 1; stemIndex <= targetStemLength; stemIndex++)
        {
            // Determine growth direction for this stem
            Vector2Int growthDir = GetStemDirection(); // Use the randomness calculated in stats
            Vector2Int nextStemPos = currentPos + growthDir;

            // Add stem step to the plan
            plan.Add(new GrowthStep {
                CellType = PlantCellType.Stem,
                Position = nextStemPos,
                StemIndex = stemIndex
            });

            currentPos = nextStemPos; // Update position for leaf calculation

            // Check if leaves should be added for this stem segment
            if ((finalLeafGap >= 0) && (stemIndex % (finalLeafGap + 1)) == 0)
            {
                patternCount++;

                // Base positions for leaves relative to the *new* stem position
                Vector2Int leftBase = currentPos + Vector2Int.left;
                Vector2Int rightBase = currentPos + Vector2Int.right;

                // Calculate leaf positions based on the chosen pattern
                List<Vector2Int> leafPositions = CalculateLeafPositions(
                    currentPos, // Current stem position
                    leftBase,   // Potential left leaf position
                    rightBase,  // Potential right leaf position
                    patternCount, // Counter for alternating patterns
                    ref spiralDir // Ref for spiral direction state
                );

                // Add leaf steps to the plan
                foreach (Vector2Int leafPos in leafPositions)
                {
                    plan.Add(new GrowthStep {
                        CellType = PlantCellType.Leaf,
                        Position = leafPos,
                        StemIndex = stemIndex // Associate leaf with the stem it grew from
                    });
                }
            }
        }
        
        // Track all leaf positions for potential regrowth
        foreach (GrowthStep step in plan)
        {
            if (step.CellType == PlantCellType.Leaf)
            {
                leafDataList.Add(new LeafData(step.Position, true));
            }
        }
        
        return plan;
    }


    // --- GetStemDirection - Determines the next stem growth direction ---
    // (Logic remains the same, uses finalGrowthRandomness)
    private Vector2Int GetStemDirection()
    {
        // Use pre-calculated randomness factor
        if (Random.value < finalGrowthRandomness) // Check against the final calculated value
        {
            // Randomly choose left-up or right-up diagonal
            return (Random.value < 0.5f) ? (Vector2Int.up + Vector2Int.left) : (Vector2Int.up + Vector2Int.right);
        }
        // Default to straight up
        return Vector2Int.up;
    }

    // --- CalculateLeafPositions - Calculates leaf positions based on pattern ---
    // (Logic remains the same, uses finalLeafPattern)
    private List<Vector2Int> CalculateLeafPositions(
        Vector2Int stemPos, Vector2Int leftBase, Vector2Int rightBase, int counter, ref int spiralDir)
    {
        List<Vector2Int> leafPositions = new List<Vector2Int>();

        switch (finalLeafPattern) // Use the final calculated pattern
        {
            case 0: // Parallel leaves
                leafPositions.Add(leftBase);
                leafPositions.Add(rightBase);
                break;

            case 1: // Alternating Offset leaves
                 // Initialize random offset ONCE per plant growth instance
                if (offsetRightForPattern1 == null)
                    offsetRightForPattern1 = Random.value < 0.5f;

                if (offsetRightForPattern1.Value) { // Offset right leaf up
                    leafPositions.Add(leftBase);
                    leafPositions.Add(rightBase + Vector2Int.up);
                } else { // Offset left leaf up
                    leafPositions.Add(leftBase + Vector2Int.up);
                    leafPositions.Add(rightBase);
                }
                break;

            case 2: // Alternating Parallel/Offset combination
                switch (counter % 4) {
                    case 0: // Treat 0 like 2 for parallel
                    case 2: // Parallel
                        leafPositions.Add(leftBase);
                        leafPositions.Add(rightBase);
                        break;
                    case 1: // Offset left up
                        leafPositions.Add(leftBase + Vector2Int.up);
                        leafPositions.Add(rightBase);
                        break;
                    case 3: // Offset right up
                        leafPositions.Add(leftBase);
                        leafPositions.Add(rightBase + Vector2Int.up);
                        break;
                }
                break;

            case 3: // Spiral leaves
                // Place one leaf normally, offset the other based on spiral direction
                if (spiralDir > 0) { // e.g., Right leaf is higher
                    leafPositions.Add(leftBase);
                    leafPositions.Add(rightBase + Vector2Int.up);
                } else { // e.g., Left leaf is higher
                    leafPositions.Add(leftBase + Vector2Int.up);
                    leafPositions.Add(rightBase);
                }
                spiralDir *= -1; // Flip direction for next time
                break;

            case 4: // Example: Symmetric double leaves on both sides
                leafPositions.Add(leftBase);
                leafPositions.Add(leftBase + Vector2Int.up);
                leafPositions.Add(rightBase);
                leafPositions.Add(rightBase + Vector2Int.up);
                break;

             default: // Fallback to parallel
                 leafPositions.Add(leftBase);
                 leafPositions.Add(rightBase);
                 break;
        }

        return leafPositions;
    }

} // End PARTIAL Class