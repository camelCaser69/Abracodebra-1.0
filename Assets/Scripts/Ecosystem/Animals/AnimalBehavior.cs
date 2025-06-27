using System.Collections.Generic;
using UnityEngine;
using WegoSystem;

public class AnimalBehavior : MonoBehaviour
{
    [Header("Spawning Points")]
    [SerializeField] Transform poopSpawnPoint;
    [SerializeField] List<GameObject> poopPrefabs;
    
    // References
    private AnimalController controller;
    private AnimalDefinition definition;
    
    // State
    private bool isEating = false;
    private bool isPooping = false;
    private bool hasPooped = true;
    private GameObject currentEatingTarget = null;
    
    // Tick counters
    private int eatRemainingTicks = 0;
    private int poopDelayTick = 0;
    private int currentPoopCooldownTick = 0;
    
    // Properties
    public bool IsEating => isEating;
    public bool IsPooping => isPooping;
    public bool CanAct => !isEating && !isPooping && !controller.IsDying;
    
    public void Initialize(AnimalController controller, AnimalDefinition definition)
    {
        this.controller = controller;
        this.definition = definition;
    }
    
    public void OnTickUpdate(int currentTick)
    {
        // Update eating
        if (isEating)
        {
            eatRemainingTicks--;
            if (eatRemainingTicks <= 0)
            {
                FinishEating();
            }
        }
        
        // Update poop timers
        if (poopDelayTick > 0)
        {
            poopDelayTick--;
        }
        
        if (currentPoopCooldownTick > 0)
        {
            currentPoopCooldownTick--;
        }
        
        // Try to poop if ready
        if (!hasPooped && poopDelayTick <= 0 && currentPoopCooldownTick <= 0 && CanAct)
        {
            TryPoop();
        }
    }
    
    public void StartEating(GameObject food)
    {
        if (food == null || !CanAct) return;
        
        // Validate food
        FoodItem foodItem = food.GetComponent<FoodItem>();
        if (foodItem == null || foodItem.foodType == null || !definition.diet.CanEat(foodItem.foodType))
        {
            Debug.LogWarning($"[AnimalBehavior] {controller.SpeciesName} cannot eat this food!");
            return;
        }
        
        // Clear movement
        controller.Movement.ClearMovementPlan();
        
        // Start eating
        isEating = true;
        currentEatingTarget = food;
        eatRemainingTicks = definition.eatDurationTicks;
        
        if (controller.CanShowThought())
        {
            controller.ShowThought(ThoughtTrigger.Eating);
        }
        
        Debug.Log($"[AnimalBehavior] {controller.SpeciesName} started eating {foodItem.foodType.foodName}");
    }
    
    private void FinishEating()
    {
        isEating = false;
        
        if (currentEatingTarget == null)
        {
            currentEatingTarget = null;
            return;
        }
        
        // Get food component and eat
        FoodItem foodItem = currentEatingTarget.GetComponent<FoodItem>();
        if (foodItem != null)
        {
            controller.Needs.Eat(foodItem);
            
            // Destroy the food
            Destroy(currentEatingTarget);
            
            // Set up poop timer
            hasPooped = false;
            poopDelayTick = Random.Range(definition.minPoopDelayTicks, definition.maxPoopDelayTicks);
            
            Debug.Log($"[AnimalBehavior] {controller.SpeciesName} finished eating");
        }
        
        currentEatingTarget = null;
    }
    
    private void TryPoop()
    {
        if (!CanAct) return;
        
        isPooping = true;
        currentPoopCooldownTick = definition.poopCooldownTicks;
        
        SpawnPoop();
        
        hasPooped = true;
        isPooping = false;
        
        if (controller.CanShowThought())
        {
            controller.ShowThought(ThoughtTrigger.Pooping);
        }
    }
    
    private void SpawnPoop()
    {
        if (poopPrefabs == null || poopPrefabs.Count == 0)
        {
            Debug.LogWarning($"[AnimalBehavior] No poop prefabs assigned for {controller.SpeciesName}");
            return;
        }
        
        // Select random poop prefab
        int index = Random.Range(0, poopPrefabs.Count);
        GameObject prefab = poopPrefabs[index];
        if (prefab == null) return;
        
        // Determine spawn position
        Transform spawnTransform = poopSpawnPoint != null ? poopSpawnPoint : transform;
        GameObject poopObj = Instantiate(prefab, spawnTransform.position, Quaternion.identity);
        
        // Apply visual variations
        SpriteRenderer sr = poopObj.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            // Random flip
            sr.flipX = Random.value > 0.5f;
            
            // Color variation
            Color c = sr.color;
            float v = definition.poopColorVariation;
            sr.color = new Color(
                Mathf.Clamp01(c.r + Random.Range(-v, v)),
                Mathf.Clamp01(c.g + Random.Range(-v, v)),
                Mathf.Clamp01(c.b + Random.Range(-v, v)),
                c.a
            );
        }
        
        // Initialize poop controller
        PoopController pc = poopObj.GetComponent<PoopController>();
        if (pc == null)
        {
            pc = poopObj.AddComponent<PoopController>();
        }
        
        // Snap poop to grid
        if (GridPositionManager.Instance != null)
        {
            GridPositionManager.Instance.SnapEntityToGrid(poopObj);
        }
        
        Debug.Log($"[AnimalBehavior] {controller.SpeciesName} pooped at {poopObj.transform.position}");
    }
    
    public void CancelCurrentAction()
    {
        if (isEating)
        {
            isEating = false;
            eatRemainingTicks = 0;
            currentEatingTarget = null;
        }
        
        if (isPooping)
        {
            isPooping = false;
        }
    }
}