using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "AnimalDefinition", menuName = "Ecosystem/Animal Definition")]
public class AnimalDefinition : ScriptableObject {
    [Header("Basic Info")]
    public string animalName = "DefaultAnimal";
    public GameObject prefab;
    
    [Header("Stats")]
    public float maxHealth = 10f;
    public float movementSpeed = 2f; // Grid units per tick
    
    [Header("Movement - Ticks")]
    public int thinkingTickInterval = 3;
    public int wanderPauseTickChance = 30; // Percentage
    public int minWanderMoveTicks = 2;
    public int maxWanderMoveTicks = 6;
    public int minWanderPauseTicks = 1;
    public int maxWanderPauseTicks = 4;
    
    [Header("Detection")]
    public int searchRadiusTiles = 5; // In grid tiles
    public int eatDistanceTiles = 1; // Must be adjacent
    
    [Header("Actions - Ticks")]
    public int eatDurationTicks = 3;
    public int thoughtCooldownTicks = 10;
    public int starvationDamageTickInterval = 4;
    public float damagePerStarvationTick = 2f;
    public int deathFadeTicks = 3;
    
    [Header("Biological Needs - Ticks")]
    public int minPoopDelayTicks = 10;
    public int maxPoopDelayTicks = 20;
    public int poopCooldownTicks = 2;
    
    [Header("Visual")]
    public float poopColorVariation = 0.1f;
    public Color damageFlashColor = Color.red;
    public int damageFlashTicks = 1;
    
    [Header("Diet")]
    public AnimalDiet diet;
    
    [Header("Scent Interactions")]
    public List<ScentDefinition> attractiveScentDefinitions = new List<ScentDefinition>();
    public List<ScentDefinition> repellentScentDefinitions = new List<ScentDefinition>();
}