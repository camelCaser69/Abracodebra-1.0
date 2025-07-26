// Assets/Scripts/Ecosystem/Animals/AnimalBehavior.cs
using System.Collections.Generic;
using UnityEngine;
using WegoSystem;

public class AnimalBehavior : MonoBehaviour
{
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

    public void Initialize(AnimalController controller, AnimalDefinition definition)
    {
        this.controller = controller;
        this.definition = definition;

        hasPooped = true;
        poopDelayTick = 0;
        currentPoopCooldownTick = 0;

        Debug.Log($"[AnimalBehavior] Initialized for {controller.SpeciesName}. Poop delay range: {definition.minPoopDelayTicks}-{definition.maxPoopDelayTicks} ticks");
    }

    public void OnTickUpdate(int currentTick)
    {
        if (isEating)
        {
            eatRemainingTicks--;
            if (eatRemainingTicks <= 0)
            {
                FinishEating();
            }
        }

        if (poopDelayTick > 0)
        {
            poopDelayTick--;
            if (poopDelayTick == 0)
            {
                Debug.Log($"[AnimalBehavior] {controller.SpeciesName} poop delay timer reached 0");
            }
        }

        if (currentPoopCooldownTick > 0)
        {
            currentPoopCooldownTick--;
        }

        if (!hasPooped && poopDelayTick <= 0 && currentPoopCooldownTick <= 0 && CanAct)
        {
            Debug.Log($"[AnimalBehavior] {controller.SpeciesName} ready to poop! hasPooped={hasPooped}, delay={poopDelayTick}, cooldown={currentPoopCooldownTick}, canAct={CanAct}");
            TryPoop();
        }
    }

    public void StartEating(GameObject food)
    {
        if (food == null || !CanAct) return;

        FoodItem foodItem = food.GetComponent<FoodItem>();
        if (foodItem == null || foodItem.foodType == null || !definition.diet.CanEat(foodItem.foodType))
        {
            Debug.LogWarning($"[AnimalBehavior] {controller.SpeciesName} cannot eat this food!");
            return;
        }

        controller.Movement.ClearMovementPlan();

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
            return;
        }

        FoodItem foodItem = currentEatingTarget.GetComponent<FoodItem>();
        if (foodItem != null)
        {
            controller.Needs.Eat(foodItem);

            // <<< NEW LOGIC: Check if the eaten item was part of a plant and trigger its effects. >>>
            PlantCell plantCell = currentEatingTarget.GetComponent<PlantCell>();
            if (plantCell != null && plantCell.ParentPlantGrowth != null)
            {
                // This is the crucial link. The animal tells the plant it has been eaten.
                plantCell.ParentPlantGrowth.TriggerEatCast(controller);

                if (Debug.isDebugBuild)
                {
                    Debug.Log($"[AnimalBehavior] Eating a plant cell. Manually reporting destruction of cell at {plantCell.GridCoord} to plant '{plantCell.ParentPlantGrowth.name}' before Destroy() is called.");
                }
                plantCell.ParentPlantGrowth.ReportCellDestroyed(plantCell.GridCoord);
            }

            Destroy(currentEatingTarget);

            hasPooped = false;
            poopDelayTick = Random.Range(definition.minPoopDelayTicks, definition.maxPoopDelayTicks);

            Debug.Log($"[AnimalBehavior] {controller.SpeciesName} finished eating. Will poop in {poopDelayTick} ticks");
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

        Debug.Log($"[AnimalBehavior] {controller.SpeciesName} pooped!");
    }

    private void SpawnPoop()
    {
        if (poopPrefabs == null || poopPrefabs.Count == 0)
        {
            Debug.LogWarning($"[AnimalBehavior] No poop prefabs assigned for {controller.SpeciesName}");
            return;
        }

        int index = Random.Range(0, poopPrefabs.Count);
        GameObject prefab = poopPrefabs[index];
        if (prefab == null) return;

        Transform spawnTransform = poopSpawnPoint != null ? poopSpawnPoint : transform;
        GameObject poopObj = Instantiate(prefab, spawnTransform.position, Quaternion.identity);

        SpriteRenderer sr = poopObj.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.flipX = Random.value > 0.5f;

            Color c = sr.color;
            float v = definition.poopColorVariation;
            sr.color = new Color(
                Mathf.Clamp01(c.r + Random.Range(-v, v)),
                Mathf.Clamp01(c.g + Random.Range(-v, v)),
                Mathf.Clamp01(c.b + Random.Range(-v, v)),
                c.a
            );
        }

        PoopController pc = poopObj.GetComponent<PoopController>();
        if (pc == null)
        {
            pc = poopObj.AddComponent<PoopController>();
        }

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