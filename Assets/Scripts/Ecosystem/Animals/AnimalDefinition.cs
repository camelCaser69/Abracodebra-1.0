using System.Collections.Generic;
using UnityEngine;

public class AnimalDefinition : ScriptableObject 
{
    public string animalName = "DefaultAnimal";
    public GameObject prefab;

    public float maxHealth = 10f;
    public float movementSpeed = 2f; // Grid units per tick

    public int thinkingTickInterval = 3;
    public int wanderPauseTickChance = 30; // Percentage
    public int minWanderMoveTicks = 2;
    public int maxWanderMoveTicks = 6;
    public int minWanderPauseTicks = 1;
    public int maxWanderPauseTicks = 4;

    public int searchRadiusTiles = 5; // In grid tiles
    public int eatDistanceTiles = 1; // Must be adjacent
    public int eatDurationTicks = 3;

    public int thoughtCooldownTicks = 10;

    public int starvationDamageTickInterval = 4;
    public float damagePerStarvationTick = 2f;
    public int deathFadeTicks = 3;
    public Color damageFlashColor = Color.red;
    public int damageFlashTicks = 1;

    public int minPoopDelayTicks = 10;
    public int maxPoopDelayTicks = 20;
    public int poopCooldownTicks = 2;
    public float poopColorVariation = 0.1f;

    public AnimalDiet diet;
    
    // Added: Reference to thought library for this animal type
    public AnimalThoughtLibrary thoughtLibrary;

    public List<ScentDefinition> attractiveScentDefinitions = new List<ScentDefinition>();
    public List<ScentDefinition> repellentScentDefinitions = new List<ScentDefinition>();

    void OnValidate() 
    {
        thinkingTickInterval = Mathf.Max(1, thinkingTickInterval);
        searchRadiusTiles = Mathf.Max(1, searchRadiusTiles);
        eatDistanceTiles = Mathf.Max(1, eatDistanceTiles);
        eatDurationTicks = Mathf.Max(1, eatDurationTicks);
        thoughtCooldownTicks = Mathf.Max(1, thoughtCooldownTicks);
        starvationDamageTickInterval = Mathf.Max(1, starvationDamageTickInterval);
        deathFadeTicks = Mathf.Max(1, deathFadeTicks);
        damageFlashTicks = Mathf.Max(1, damageFlashTicks);
        minPoopDelayTicks = Mathf.Max(1, minPoopDelayTicks);
        maxPoopDelayTicks = Mathf.Max(minPoopDelayTicks, maxPoopDelayTicks);
        poopCooldownTicks = Mathf.Max(1, poopCooldownTicks);
        minWanderMoveTicks = Mathf.Max(1, minWanderMoveTicks);
        maxWanderMoveTicks = Mathf.Max(minWanderMoveTicks, maxWanderMoveTicks);
        minWanderPauseTicks = Mathf.Max(1, minWanderPauseTicks);
        maxWanderPauseTicks = Mathf.Max(minWanderPauseTicks, maxWanderPauseTicks);
        wanderPauseTickChance = Mathf.Clamp(wanderPauseTickChance, 0, 100);
    }
}