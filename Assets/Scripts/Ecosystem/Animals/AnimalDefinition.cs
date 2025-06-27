using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewAnimalDefinition", menuName = "Ecosystem/Animal Definition")]
public class AnimalDefinition : ScriptableObject
{
    [Header("━━━━━━━━━━━━ BASIC INFO ━━━━━━━━━━━━")]
    [Space(5)]
    public string animalName = "DefaultAnimal";
    public GameObject prefab;
    
    [Header("━━━━━━━━━━━━ HEALTH & STATS ━━━━━━━━━━━━")]
    [Space(5)]
    [Tooltip("Maximum health points for this animal")]
    public float maxHealth = 10f;
    
    [Tooltip("Movement speed in grid units per tick")]
    public float movementSpeed = 2f;
    
    [Header("━━━━━━━━━━━━ DIET & HUNGER ━━━━━━━━━━━━")]
    [Space(5)]
    [Tooltip("The diet configuration for this animal species")]
    public AnimalDiet diet;
    
    [Header("━━━━━━━━━━━━ AI BEHAVIOR ━━━━━━━━━━━━")]
    [Space(5)]
    [Header("Decision Making")]
    [Tooltip("How often (in ticks) the animal makes decisions")]
    public int thinkingTickInterval = 3;
    
    [Header("Food Seeking")]
    [Tooltip("Search radius for food in grid tiles")]
    public int searchRadiusTiles = 5;
    
    [Tooltip("Distance in tiles the animal must be to eat food")]
    public int eatDistanceTiles = 1;
    
    [Tooltip("How many ticks it takes to eat food")]
    public int eatDurationTicks = 3;
    
    [Header("Wandering")]
    [Tooltip("Chance (%) to pause while wandering")]
    [Range(0, 100)]
    public int wanderPauseTickChance = 30;
    
    [Tooltip("Min ticks to move in one direction while wandering")]
    public int minWanderMoveTicks = 2;
    
    [Tooltip("Max ticks to move in one direction while wandering")]
    public int maxWanderMoveTicks = 6;
    
    [Tooltip("Min ticks to pause while wandering")]
    public int minWanderPauseTicks = 1;
    
    [Tooltip("Max ticks to pause while wandering")]
    public int maxWanderPauseTicks = 4;
    
    [Header("━━━━━━━━━━━━ DAMAGE & DEATH ━━━━━━━━━━━━")]
    [Space(5)]
    [Header("Starvation")]
    [Tooltip("Ticks between starvation damage when at max hunger")]
    public int starvationDamageTickInterval = 4;
    
    [Tooltip("Damage dealt per starvation tick")]
    public float damagePerStarvationTick = 2f;
    
    [Header("Visual Effects")]
    [Tooltip("Color to flash when taking damage")]
    public Color damageFlashColor = Color.red;
    
    [Tooltip("Duration of damage flash in ticks")]
    public int damageFlashTicks = 1;
    
    [Tooltip("Ticks for death fade animation")]
    public int deathFadeTicks = 3;
    
    [Header("━━━━━━━━━━━━ POOP MECHANICS ━━━━━━━━━━━━")]
    [Space(5)]
    [Tooltip("Minimum ticks after eating before pooping")]
    public int minPoopDelayTicks = 10;
    
    [Tooltip("Maximum ticks after eating before pooping")]
    public int maxPoopDelayTicks = 20;
    
    [Tooltip("Cooldown ticks after pooping before can poop again")]
    public int poopCooldownTicks = 2;
    
    [Tooltip("Random color variation for poop (0-1)")]
    [Range(0f, 0.5f)]
    public float poopColorVariation = 0.1f;
    
    [Header("━━━━━━━━━━━━ THOUGHT SYSTEM ━━━━━━━━━━━━")]
    [Space(5)]
    [Tooltip("Library of thought messages for this animal")]
    public AnimalThoughtLibrary thoughtLibrary;
    
    [Tooltip("Cooldown ticks between showing thoughts")]
    public int thoughtCooldownTicks = 10;
    
    [Header("━━━━━━━━━━━━ SCENT SYSTEM ━━━━━━━━━━━━")]
    [Space(5)]
    [Tooltip("Scents that attract this animal")]
    public List<ScentDefinition> attractiveScentDefinitions = new List<ScentDefinition>();
    
    [Tooltip("Scents that repel this animal")]
    public List<ScentDefinition> repellentScentDefinitions = new List<ScentDefinition>();
    
    void OnValidate()
    {
        // Basic Stats
        maxHealth = Mathf.Max(1f, maxHealth);
        movementSpeed = Mathf.Max(0.1f, movementSpeed);
        
        // AI Behavior
        thinkingTickInterval = Mathf.Max(1, thinkingTickInterval);
        searchRadiusTiles = Mathf.Max(1, searchRadiusTiles);
        eatDistanceTiles = Mathf.Max(1, eatDistanceTiles);
        eatDurationTicks = Mathf.Max(1, eatDurationTicks);
        
        // Wandering
        minWanderMoveTicks = Mathf.Max(1, minWanderMoveTicks);
        maxWanderMoveTicks = Mathf.Max(minWanderMoveTicks, maxWanderMoveTicks);
        minWanderPauseTicks = Mathf.Max(1, minWanderPauseTicks);
        maxWanderPauseTicks = Mathf.Max(minWanderPauseTicks, maxWanderPauseTicks);
        
        // Damage & Death
        starvationDamageTickInterval = Mathf.Max(1, starvationDamageTickInterval);
        damagePerStarvationTick = Mathf.Max(0.1f, damagePerStarvationTick);
        deathFadeTicks = Mathf.Max(1, deathFadeTicks);
        damageFlashTicks = Mathf.Max(1, damageFlashTicks);
        
        // Poop Mechanics
        minPoopDelayTicks = Mathf.Max(1, minPoopDelayTicks);
        maxPoopDelayTicks = Mathf.Max(minPoopDelayTicks, maxPoopDelayTicks);
        poopCooldownTicks = Mathf.Max(1, poopCooldownTicks);
        
        // Thought System
        thoughtCooldownTicks = Mathf.Max(1, thoughtCooldownTicks);
    }
}