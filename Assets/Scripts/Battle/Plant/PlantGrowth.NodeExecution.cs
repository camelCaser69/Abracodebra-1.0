// FILE: Assets/Scripts/Battle/Plant/PlantGrowth.NodeExecution.cs
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public partial class PlantGrowth : MonoBehaviour
{
    // ------------------------------------------------
    // --- NODE EXECUTION METHODS ---
    // ------------------------------------------------

    private IEnumerator ExecuteMatureCycle()
    {
        if (nodeGraph?.nodes == null || nodeGraph.nodes.Count == 0) {
             Debug.LogError($"[{gameObject.name}] NodeGraph missing or empty!", gameObject);
             currentState = PlantState.Mature_Idle;
             cycleTimer = cycleCooldown; // Reset timer even on error
             yield break;
        }

        // --- Accumulation Phase ---
        float damageMultiplier = 1.0f;
        Dictionary<ScentDefinition, float> accumulatedScentRadiusBonus = new Dictionary<ScentDefinition, float>();
        Dictionary<ScentDefinition, float> accumulatedScentStrengthBonus = new Dictionary<ScentDefinition, float>();
        float totalEnergyCostForCycle = 0f;

        // Process all nodes to accumulate effects
        foreach (var node in nodeGraph.nodes.OrderBy(n => n.orderIndex))
        {
            if (node?.effects == null || node.effects.Count == 0) continue;

            foreach (var effect in node.effects)
            {
                 if (effect == null || effect.isPassive) continue; // Skip passive effects

                 // Accumulate specific non-passive effects
                 switch (effect.effectType)
                 {
                    case NodeEffectType.EnergyCost:
                         totalEnergyCostForCycle += Mathf.Max(0f, effect.primaryValue);
                         break;
                    case NodeEffectType.Damage:
                         damageMultiplier = Mathf.Max(0.1f, damageMultiplier + effect.primaryValue);
                         break;
                    case NodeEffectType.ScentModifier:
                        if (effect.scentDefinitionReference != null)
                        {
                             ScentDefinition key = effect.scentDefinitionReference;
                             // Use TryGetValue for cleaner addition
                             accumulatedScentRadiusBonus.TryGetValue(key, out float currentRad);
                             accumulatedScentRadiusBonus[key] = currentRad + effect.primaryValue;

                             accumulatedScentStrengthBonus.TryGetValue(key, out float currentStr);
                             accumulatedScentStrengthBonus[key] = currentStr + effect.secondaryValue;
                        }
                        else {
                             Debug.LogWarning($"Node '{node.nodeDisplayName ?? "Unnamed"}' has ScentModifier effect but ScentDefinition reference is NULL.");
                        }
                        break;
                    // Other accumulation effects could go here
                 }
            }
        }

        // --- Execution Phase ---
        // Check Energy Cost *before* executing actions
        if (currentEnergy < totalEnergyCostForCycle) {
             if(Debug.isDebugBuild) Debug.Log($"[{gameObject.name}] Not enough energy ({currentEnergy}/{totalEnergyCostForCycle}) for mature cycle.");
             currentState = PlantState.Mature_Idle;
             cycleTimer = cycleCooldown; // Reset timer
             yield break; // Exit if not enough energy
        }

        // Spend energy
        currentEnergy = Mathf.Max(0f, currentEnergy - totalEnergyCostForCycle);
        UpdateUI(); // Update UI after spending energy

        // Execute node effects in order
        foreach (var node in nodeGraph.nodes.OrderBy(n => n.orderIndex))
        {
            if (node?.effects == null || node.effects.Count == 0) continue;

            // Check if this node contains any *active* effects that require a delay
            bool hasActionEffectInNode = node.effects.Any(eff => eff != null && !eff.isPassive &&
                                            eff.effectType != NodeEffectType.EnergyCost && // Don't delay for cost
                                            eff.effectType != NodeEffectType.Damage &&   // Don't delay for damage mod
                                            eff.effectType != NodeEffectType.ScentModifier); // Don't delay for scent mod

            // Apply delay BEFORE processing effects of this node if applicable
            if (hasActionEffectInNode && nodeCastDelay > 0.01f) {
                 yield return new WaitForSeconds(nodeCastDelay);
            }

            // Execute individual active effects
            foreach (var effect in node.effects)
            {
                 // Skip passive and accumulation-only effects during execution
                 if (effect == null || effect.isPassive ||
                     effect.effectType == NodeEffectType.EnergyCost ||
                     effect.effectType == NodeEffectType.Damage ||
                     effect.effectType == NodeEffectType.ScentModifier) continue;

                 // Execute the actual active effect
                 switch (effect.effectType) {
                     case NodeEffectType.Output:
                        // Find component on THIS plant root or its children
                        OutputNodeEffect outputComp = GetComponentInChildren<OutputNodeEffect>();
                        if (outputComp != null) {
                             outputComp.Activate(damageMultiplier, accumulatedScentRadiusBonus, accumulatedScentStrengthBonus);
                        } else {
                             Debug.LogWarning($"[{gameObject.name}] Node requested Output effect, but no OutputNodeEffect component found.", this);
                        }
                         break;
                     case NodeEffectType.GrowBerry:
                         TrySpawnBerry(accumulatedScentRadiusBonus, accumulatedScentStrengthBonus);
                         break;
                     // Add other ACTIVE effect cases here (e.g., Heal, StatusEffect)
                 }
            }
        }

        // --- Cycle Complete ---
        // Reset timer and state after executing all nodes
        cycleTimer = cycleCooldown;
        currentState = PlantState.Mature_Idle;
    }

    // --- TrySpawnBerry - Tries to create a berry on the plant ---
    private void TrySpawnBerry(Dictionary<ScentDefinition, float> scentRadiiBonus, Dictionary<ScentDefinition, float> scentStrengthsBonus)
    {
        if (berryCellPrefab == null) {
            Debug.LogWarning($"[{gameObject.name}] Berry Prefab not assigned. Cannot spawn berry.", gameObject);
            return;
        }

        // Find potential coordinates adjacent to existing stems/leaves where berries can grow
        // Consider only coords adjacent to stems for typical berry growth
        var potentialCoords = cells
            .Where(cellKvp => cellKvp.Value == PlantCellType.Stem || cellKvp.Value == PlantCellType.Seed) // Only grow off Stem/Seed
            .SelectMany(cellKvp => {
                Vector2Int coord = cellKvp.Key;
                // Define potential relative offsets for berries (e.g., above, left, right)
                Vector2Int[] berryOffsets = { Vector2Int.up, Vector2Int.left, Vector2Int.right };
                List<Vector2Int> candidates = new List<Vector2Int>();
                foreach(var offset in berryOffsets) {
                    candidates.Add(coord + offset);
                }
                return candidates;
            })
            .Where(coord => !cells.ContainsKey(coord)) // Ensure the target coordinate is empty
            .Distinct() // Avoid duplicates if multiple stems border the same empty cell
            .ToList();

        if (potentialCoords.Count > 0) {
            // Choose a random empty valid spot
            Vector2Int chosenCoord = potentialCoords[Random.Range(0, potentialCoords.Count)];
            // Spawn the berry visual, passing accumulated scent data
            GameObject berryGO = SpawnCellVisual(PlantCellType.Fruit, chosenCoord, scentRadiiBonus, scentStrengthsBonus);
             if (berryGO == null) {
                  Debug.LogError($"[{gameObject.name}] Failed to spawn berry visual at {chosenCoord}, SpawnCellVisual returned null.");
             }
        } else {
             if(Debug.isDebugBuild) Debug.Log($"[{gameObject.name}] No valid empty adjacent locations found to spawn a berry.");
        }
    }


    // --- ApplyScentDataToObject - Applies scent to an object like a berry or projectile ---
    public void ApplyScentDataToObject(GameObject targetObject, Dictionary<ScentDefinition, float> scentRadiusBonuses, Dictionary<ScentDefinition, float> scentStrengthBonuses)
    {
        // (Function body remains the same as before)
        if (targetObject == null) {
            Debug.LogError("ApplyScentDataToObject: targetObject is null.");
            return;
        }

        if (EcosystemManager.Instance == null) {
            Debug.LogError("ApplyScentDataToObject: EcosystemManager instance not found.");
            return;
        }

        if (EcosystemManager.Instance.scentLibrary == null) {
            Debug.LogWarning("ApplyScentDataToObject: Scent Library not assigned in EcosystemManager.");
            return;
        }

        // Find the ScentDefinition with the highest accumulated strength bonus
        ScentDefinition strongestScentDef = null;
        float maxStrengthBonus = -1f; // Use -1 to correctly handle 0 bonus values

        if (scentStrengthBonuses != null && scentStrengthBonuses.Count > 0) {
            foreach (var kvp in scentStrengthBonuses) {
                if (kvp.Key != null && kvp.Value > maxStrengthBonus) {
                    maxStrengthBonus = kvp.Value;
                    strongestScentDef = kvp.Key;
                }
            }
        } else {
             // if (Debug.isDebugBuild) Debug.Log($"[{gameObject.name} ApplyScentToObject] No scent strength bonuses provided.");
        }

        // Apply the strongest scent found (if any)
        if (strongestScentDef != null) {
             // if (Debug.isDebugBuild) Debug.Log($"[{gameObject.name} ApplyScentToObject] Applying strongest scent '{strongestScentDef.displayName}' to {targetObject.name} (Str Bonus: {maxStrengthBonus})");

            ScentSource scentSource = targetObject.GetComponent<ScentSource>();
            if (scentSource == null) {
                scentSource = targetObject.AddComponent<ScentSource>();
            }

            // Configure the ScentSource component
            scentSource.definition = strongestScentDef;

            // Retrieve the corresponding bonuses (defaulting to 0 if not found)
            scentRadiusBonuses.TryGetValue(strongestScentDef, out float radiusBonus);
            scentSource.radiusModifier = radiusBonus;
            scentSource.strengthModifier = maxStrengthBonus; // Apply the max strength found

            // Instantiate particle effect if defined and not already present
            if (strongestScentDef.particleEffectPrefab != null) {
                bool particleExists = false;
                // Check immediate children for an existing particle system to avoid duplicates
                foreach(Transform child in targetObject.transform){
                    if(child.TryGetComponent<ParticleSystem>(out _)){
                        particleExists = true;
                        break;
                    }
                }
                if (!particleExists) {
                     // Instantiate under the target object
                    Instantiate(strongestScentDef.particleEffectPrefab, targetObject.transform.position, Quaternion.identity, targetObject.transform);
                }
            }
        } else {
            // if (Debug.isDebugBuild) Debug.Log($"[{gameObject.name} ApplyScentToObject] No strongest scent found to apply to {targetObject.name}.");
        }
    }

} // End PARTIAL Class