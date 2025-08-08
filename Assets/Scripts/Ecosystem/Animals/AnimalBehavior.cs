// REWORKED FILE: Assets/Scripts/Ecosystem/Animals/AnimalBehavior.cs
using UnityEngine;
using System.Collections.Generic;
using WegoSystem;

public class AnimalBehavior : MonoBehaviour
{
    // ... (Fields are the same)
    [SerializeField] private Transform poopSpawnPoint;
    [SerializeField] private List<GameObject> poopPrefabs;
    private AnimalController controller;
    private AnimalDefinition definition;
    private bool isEating = false;
    private bool isPooping = false;
    private bool hasPooped = true;
    private GameObject currentEatingTarget = null;
    private int eatRemainingTicks = 0;
    private int poopDelayTick = 0;
    private int currentPoopCooldownTick = 0;
    public bool IsEating => isEating;
    public bool IsPooping => isPooping;
    public bool CanAct => !isEating && !isPooping && !controller.IsDying;

    // ... (Initialize, OnTickUpdate, StartEating are the same)
    public void Initialize(AnimalController controller, AnimalDefinition definition) { this.controller = controller; this.definition = definition; hasPooped = true; }
    public void OnTickUpdate(int currentTick) { if (isEating) { eatRemainingTicks--; if (eatRemainingTicks <= 0) { FinishEating(); } } if (poopDelayTick > 0) { poopDelayTick--; } if (currentPoopCooldownTick > 0) { currentPoopCooldownTick--; } if (!hasPooped && poopDelayTick <= 0 && currentPoopCooldownTick <= 0 && CanAct) { TryPoop(); } }
    public void StartEating(GameObject food) { if (food == null || !CanAct) return; FoodItem foodItem = food.GetComponent<FoodItem>(); if (foodItem == null || foodItem.foodType == null || !definition.diet.CanEat(foodItem.foodType)) { return; } controller.Movement.ClearMovementPlan(); isEating = true; currentEatingTarget = food; eatRemainingTicks = definition.eatDurationTicks; if (controller.CanShowThought()) { controller.ShowThought(ThoughtTrigger.Eating); } }

    void FinishEating()
    {
        isEating = false;
        if (currentEatingTarget == null) return;

        FoodItem foodItem = currentEatingTarget.GetComponent<FoodItem>();
        if (foodItem != null)
        {
            controller.Needs.Eat(foodItem);

            var plantCell = currentEatingTarget.GetComponent<PlantCell>();
            if (plantCell != null && plantCell.ParentPlantGrowth != null)
            {
                // FIX: Instead of calling specific methods, we notify the plant it was eaten.
                // The plant's internal gene system (executor) will handle any "On Eaten" triggers.
                plantCell.ParentPlantGrowth.HandleBeingEaten(this.controller, plantCell);
            }

            Destroy(currentEatingTarget);

            hasPooped = false;
            poopDelayTick = Random.Range(definition.minPoopDelayTicks, definition.maxPoopDelayTicks);
        }

        currentEatingTarget = null;
    }

    // ... (Rest of the file is the same)
    private void TryPoop() { if (!CanAct) return; isPooping = true; currentPoopCooldownTick = definition.poopCooldownTicks; SpawnPoop(); hasPooped = true; isPooping = false; if (controller.CanShowThought()) { controller.ShowThought(ThoughtTrigger.Pooping); } }
    private void SpawnPoop() { if (poopPrefabs == null || poopPrefabs.Count == 0) return; int index = Random.Range(0, poopPrefabs.Count); GameObject prefab = poopPrefabs[index]; if (prefab == null) return; Transform spawnTransform = poopSpawnPoint != null ? poopSpawnPoint : transform; GameObject poopObj = Instantiate(prefab, spawnTransform.position, Quaternion.identity); if (GridPositionManager.Instance != null) { GridPositionManager.Instance.SnapEntityToGrid(poopObj); } }
    public void CancelCurrentAction() { isEating = false; eatRemainingTicks = 0; currentEatingTarget = null; isPooping = false; }
}