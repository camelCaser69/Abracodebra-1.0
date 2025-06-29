using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using WegoSystem;

public class PlantNodeExecutor
{
    readonly PlantGrowth plant;

    public float PoopDetectionRadius { get; private set; }
    public float PoopEnergyBonus { get; private set; }

    public PlantNodeExecutor(PlantGrowth plant)
    {
        this.plant = plant;
    }

    public void ProcessPassiveEffects(NodeGraph nodeGraph)
    {
        PoopDetectionRadius = 0f;
        PoopEnergyBonus = 0f;

        foreach (NodeData node in nodeGraph.nodes.OrderBy(n => n.orderIndex))
        {
            if (node?.effects == null) continue;

            foreach (var effect in node.effects)
            {
                if (effect == null || !effect.isPassive) continue;

                switch (effect.effectType)
                {
                    case NodeEffectType.PoopAbsorption:
                        PoopDetectionRadius = Mathf.Max(0f, effect.primaryValue);
                        PoopEnergyBonus = Mathf.Max(0f, effect.secondaryValue);
                        break;
                }
            }
        }
    }

    public void ExecuteMatureCycleTick()
    {
        if (plant.NodeGraph?.nodes == null || plant.NodeGraph.nodes.Count == 0)
        {
            Debug.LogError($"[{plant.gameObject.name}] NodeGraph missing or empty!");
            return;
        }

        float damageMultiplier = 1.0f;
        var accumulatedScentRadiusBonus = new Dictionary<ScentDefinition, float>();
        var accumulatedScentStrengthBonus = new Dictionary<ScentDefinition, float>();
        float totalEnergyCostForCycle = 0f;

        foreach (var node in plant.NodeGraph.nodes.OrderBy(n => n.orderIndex))
        {
            if (node?.effects == null) continue;

            foreach (var effect in node.effects)
            {
                if (effect == null || effect.isPassive) continue;

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

                            if (!accumulatedScentRadiusBonus.ContainsKey(key))
                                accumulatedScentRadiusBonus[key] = 0f;
                            accumulatedScentRadiusBonus[key] += effect.primaryValue;

                            if (!accumulatedScentStrengthBonus.ContainsKey(key))
                                accumulatedScentStrengthBonus[key] = 0f;
                            accumulatedScentStrengthBonus[key] += effect.secondaryValue;
                        }
                        break;
                }
            }
        }

        // Check for poop absorption if the plant has this ability
        if (PoopDetectionRadius > 0f)
        {
            CheckForPoopAndAbsorb();
        }

        if (plant.EnergySystem.CurrentEnergy < totalEnergyCostForCycle)
        {
            if (Debug.isDebugBuild)
            {
                Debug.Log($"[{plant.gameObject.name}] Not enough energy ({plant.EnergySystem.CurrentEnergy}/{totalEnergyCostForCycle}) for mature cycle.");
            }
            return;
        }

        plant.EnergySystem.SpendEnergy(totalEnergyCostForCycle);

        // Apply scent effects to plant if any
        if (accumulatedScentRadiusBonus.Count > 0 || accumulatedScentStrengthBonus.Count > 0)
        {
            ApplyScentDataToObject(plant.gameObject, accumulatedScentRadiusBonus, accumulatedScentStrengthBonus);
        }
    }

    void CheckForPoopAndAbsorb()
    {
        if (PoopDetectionRadius <= 0f) return;

        GridEntity plantGrid = plant.GetComponent<GridEntity>();
        if (plantGrid == null) return;

        int radiusTiles = Mathf.RoundToInt(PoopDetectionRadius);
        var tilesInRadius = GridRadiusUtility.GetTilesInCircle(plantGrid.Position, radiusTiles);

        foreach (var tile in tilesInRadius)
        {
            var entitiesAtTile = GridPositionManager.Instance.GetEntitiesAt(tile);
            foreach (var entity in entitiesAtTile)
            {
                PoopController poop = entity.GetComponent<PoopController>();
                if (poop != null)
                {
                    // Absorb the poop and gain energy
                    if (PoopEnergyBonus > 0f)
                    {
                        plant.EnergySystem.AddEnergy(PoopEnergyBonus);
                        if (Debug.isDebugBuild)
                        {
                            Debug.Log($"[{plant.gameObject.name}] Absorbed poop and gained {PoopEnergyBonus} energy!");
                        }
                    }
                    
                    // Destroy the poop
                    Object.Destroy(poop.gameObject);
                    break; // Only absorb one poop per tile per cycle
                }
            }
        }
    }

    public void ApplyScentDataToObject(GameObject targetObject, Dictionary<ScentDefinition, float> scentRadiusBonuses, Dictionary<ScentDefinition, float> scentStrengthBonuses)
    {
        if (targetObject == null) return;

        foreach (var kvp in scentRadiusBonuses)
        {
            ScentDefinition scentDef = kvp.Key;
            float radiusBonus = kvp.Value;
            float strengthBonus = scentStrengthBonuses.ContainsKey(scentDef) ? scentStrengthBonuses[scentDef] : 0f;

            // Try to find existing scent source with this definition
            ScentSource existingSource = targetObject.GetComponentsInChildren<ScentSource>()
                .FirstOrDefault(s => s.Definition == scentDef);

            if (existingSource != null)
            {
                // Modify existing scent source
                existingSource.ApplyModifiers(radiusBonus, strengthBonus);
            }
            else
            {
                // Create new scent source
                GameObject scentSourceObj = new GameObject($"ScentSource_{scentDef.displayName}");
                scentSourceObj.transform.SetParent(targetObject.transform);
                scentSourceObj.transform.localPosition = Vector3.zero;

                ScentSource newSource = scentSourceObj.AddComponent<ScentSource>();
                SetScentSourceDefinition(newSource, scentDef);
                newSource.SetRadiusModifier(radiusBonus);
                newSource.SetStrengthModifier(strengthBonus);
            }
        }
    }

    // Helper method to set scent source definition
    void SetScentSourceDefinition(ScentSource source, ScentDefinition definition)
    {
        source.SetDefinition(definition);
    }
}