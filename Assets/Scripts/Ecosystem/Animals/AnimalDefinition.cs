// FILE: Assets/Scripts/Ecosystem/Animals/AnimalDefinition.cs
using System.Collections.Generic;
using UnityEngine;

public class AnimalDefinition : ScriptableObject
{
    public string animalName = "DefaultAnimal";
    public GameObject prefab;

    [Header("Health & Stats")]
    public float maxHealth = 10f;

    [Header("Movement")]
    public float movementSpeed = 2f;
    public int searchRadiusTiles = 5;
    public int eatDistanceTiles = 1;

    [Header("Behavior")]
    public AnimalDiet diet;
    public int thinkingTickInterval = 3;
    public int eatDurationTicks = 3;

    [Header("Wandering")]
    public int wanderPauseTickChance = 30;
    public int minWanderPauseTicks = 1;
    public int maxWanderPauseTicks = 4;

    [Header("Needs & Damage")]
    public int starvationDamageTickInterval = 4;
    public float damagePerStarvationTick = 2f;
    public Color damageFlashColor = Color.red;
    public int damageFlashTicks = 1;
    public int deathFadeTicks = 3;

    [Header("Digestion")]
    public int minPoopDelayTicks = 10;
    public int maxPoopDelayTicks = 20;
    public int poopCooldownTicks = 2;
    public float poopColorVariation = 0.1f;

    [Header("UI & Effects")]
    public AnimalThoughtLibrary thoughtLibrary;
    public int thoughtCooldownTicks = 10;
    public List<ScentDefinition> attractiveScentDefinitions = new List<ScentDefinition>();
    public List<ScentDefinition> repellentScentDefinitions = new List<ScentDefinition>();

    [Header("Pest / Combat")]
    [Tooltip("If true, this animal will seek and attack plants during waves.")]
    public bool isPest = false;

    [Tooltip("How many ticks this pest takes to eat one leaf (before leaf durability multiplier). Lower = faster eater.")]
    public int baseEatSpeedTicks = 2;

    [Tooltip("Tiles within which a pest will start dealing damage to a plant.")]
    public float attackRangeTiles = 1.5f;

    [Header("Status Effect Immunities")]
    [Tooltip("If true, this creature is immune to the Fear payload. Use for large/boss creatures.")]
    public bool immuneToFear = false;

    void OnValidate()
    {
        maxHealth = Mathf.Max(1f, maxHealth);
        movementSpeed = Mathf.Max(0.1f, movementSpeed);

        thinkingTickInterval = Mathf.Max(1, thinkingTickInterval);
        searchRadiusTiles = Mathf.Max(1, searchRadiusTiles);
        eatDistanceTiles = Mathf.Max(1, eatDistanceTiles);
        eatDurationTicks = Mathf.Max(1, eatDurationTicks);

        minWanderPauseTicks = Mathf.Max(1, minWanderPauseTicks);
        maxWanderPauseTicks = Mathf.Max(minWanderPauseTicks, maxWanderPauseTicks);

        starvationDamageTickInterval = Mathf.Max(1, starvationDamageTickInterval);
        damagePerStarvationTick = Mathf.Max(0.1f, damagePerStarvationTick);
        deathFadeTicks = Mathf.Max(1, deathFadeTicks);
        damageFlashTicks = Mathf.Max(1, damageFlashTicks);

        minPoopDelayTicks = Mathf.Max(1, minPoopDelayTicks);
        maxPoopDelayTicks = Mathf.Max(minPoopDelayTicks, maxPoopDelayTicks);
        poopCooldownTicks = Mathf.Max(1, poopCooldownTicks);

        thoughtCooldownTicks = Mathf.Max(1, thoughtCooldownTicks);

        baseEatSpeedTicks = Mathf.Max(1, baseEatSpeedTicks);
        attackRangeTiles = Mathf.Max(0.5f, attackRangeTiles);
    }
}