using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using WegoSystem;

public class AnimalMovement : MonoBehaviour
{
    #region Standard Unity Using Statements
    // These are commonly used and can be added back for clarity.
    // using System;
    // using System.Collections;
    #endregion

    [Header("Debugging")]
    [SerializeField] private bool showPathfindingDebugLine = false;

    // --- Component References ---
    private AnimalController controller;
    private AnimalDefinition definition;
    private GridEntity gridEntity;
    private LineRenderer pathDebugLine;

    // --- State ---
    private List<GridPosition> currentPath = new List<GridPosition>();
    private int currentPathIndex = 0;
    private GameObject currentTargetFood = null;
    private bool hasPlannedAction = false;
    private int wanderPauseTicks = 0;
    private int lastThinkTick = 0;

    private bool isSeekingScreenCenter = false;
    private Vector2 screenCenterTarget;
    private Vector2 minBounds;
    private Vector2 maxBounds;

    private Vector2 lastMoveDirection;

    public bool HasTarget => currentTargetFood != null || hasPlannedAction;
    public GameObject CurrentTargetFood => currentTargetFood;

    public void Initialize(AnimalController controller, AnimalDefinition definition)
    {
        this.controller = controller;
        this.definition = definition;
        this.gridEntity = controller.GridEntity;

        SetupDebugLineRenderer();
    }

    public void OnTickUpdate(int currentTick)
    {
        if (!enabled || controller.IsDying || controller.Behavior.IsEating || controller.Behavior.IsPooping) return;

        if (wanderPauseTicks > 0)
        {
            wanderPauseTicks--;
            return;
        }

        // Think about what to do next
        if (currentTick - lastThinkTick >= definition.thinkingTickInterval)
        {
            MakeMovementDecision();
            lastThinkTick = currentTick;
        }

        // If we have a plan and we're not moving, execute the next step
        if (hasPlannedAction && !gridEntity.IsMoving)
        {
            ExecutePlannedMovement();
        }
    }

    public void UpdateVisuals()
    {
        UpdatePathDebugLine();
    }

    private void MakeMovementDecision()
    {
        if (gridEntity.IsMoving) return;

        if (isSeekingScreenCenter)
        {
            HandleScreenCenterSeeking();
            return;
        }

        if (controller.Needs.IsHungry)
        {
            PlanFoodSeeking();
        }
        else
        {
            PlanWandering();
        }
    }

    private void PlanFoodSeeking()
    {
        if (GridDebugVisualizer.Instance != null && Debug.isDebugBuild)
        {
            GridDebugVisualizer.Instance.VisualizeAnimalSearchRadius(controller, gridEntity.Position, definition.searchRadiusTiles);
        }

        if (controller.CanShowThought())
        {
            controller.ShowThought(ThoughtTrigger.Hungry);
        }

        GameObject nearestFood = FindNearestFood();
        if (nearestFood != null)
        {
            SetTargetFood(nearestFood);
        }
        else
        {
            // No food found, just wander
            PlanWandering();
        }
    }

    private GameObject FindNearestFood()
    {
        if (definition.diet == null) return null;

        // Try the more optimized grid-based search first
        GameObject foodFromGrid = FindFoodInGrid();
        if (foodFromGrid != null) return foodFromGrid;

        // Fallback to collider-based search
        return FindFoodByCollider();
    }

    private GameObject FindFoodInGrid()
    {
        var entitiesInRadius = GridPositionManager.Instance.GetEntitiesInRadius(
            gridEntity.Position,
            definition.searchRadiusTiles,
            true
        );

        GameObject bestFood = null;
        float bestScore = -1f;

        foreach (var entity in entitiesInRadius)
        {
            if (entity == null || entity.gameObject == this.gameObject) continue;

            FoodItem foodItem = entity.GetComponent<FoodItem>();
            if (foodItem != null && foodItem.foodType != null && definition.diet.CanEat(foodItem.foodType))
            {
                var pref = definition.diet.GetPreference(foodItem.foodType);
                if (pref == null) continue;

                // --- FIX: Use the helper to get the food's *actual* ground position for distance calculation.
                GridPosition foodGroundPos = GetFoodGroundPosition(entity.gameObject);
                float distance = gridEntity.Position.ManhattanDistance(foodGroundPos);

                float score = pref.preferencePriority / (1f + distance);

                if (score > bestScore)
                {
                    bestScore = score;
                    bestFood = entity.gameObject; // We still return the specific part to eat.
                }
            }
        }

        return bestFood;
    }

    private GameObject FindFoodByCollider()
    {
        Vector3 worldPos = transform.position;
        float worldRadius = definition.searchRadiusTiles * GridPositionManager.Instance.GetTilemapGrid().cellSize.x;

        Collider2D[] colliders = Physics2D.OverlapCircleAll(worldPos, worldRadius);

        GameObject bestFood = null;
        float bestScore = -1f;

        foreach (var collider in colliders)
        {
            if (collider == null || collider.gameObject == this.gameObject) continue;

            // Ignore poop
            if (collider.GetComponent<PoopController>() != null) continue;

            FoodItem foodItem = collider.GetComponent<FoodItem>();
            if (foodItem != null && foodItem.foodType != null && definition.diet.CanEat(foodItem.foodType))
            {
                var pref = definition.diet.GetPreference(foodItem.foodType);
                if (pref == null) continue;

                // --- FIX: Use the helper to get the food's ground position and calculate distance based on the grid
                GridPosition foodGroundPos = GetFoodGroundPosition(collider.gameObject);
                float distance = gridEntity.Position.ManhattanDistance(foodGroundPos);
                
                float score = pref.preferencePriority / (1f + distance);

                if (score > bestScore)
                {
                    bestScore = score;
                    bestFood = collider.gameObject;
                }
            }
        }

        return bestFood;
    }

    /// <summary>
    /// Gets the root grid position of a food object, correctly handling plants.
    /// This is the key to ensuring animals navigate to the base of a plant, not an offset part like a leaf.
    /// </summary>
    private GridPosition GetFoodGroundPosition(GameObject food)
    {
        if (food == null) return GridPosition.Zero;

        // The most reliable way to find a plant's root is to look for the PlantGrowth component in its hierarchy.
        // GetComponentInParent will check the object itself and then its parents.
        PlantGrowth plantRoot = food.GetComponentInParent<PlantGrowth>();

        if (plantRoot != null)
        {
            // If we found a PlantGrowth root, its GridEntity holds the true ground position.
            GridEntity plantGridEntity = plantRoot.GetComponent<GridEntity>();
            if (plantGridEntity != null)
            {
                return plantGridEntity.Position;
            }
        }

        // If it's not part of a plant, try to get its own GridEntity.
        GridEntity foodGridEntity = food.GetComponent<GridEntity>();
        if (foodGridEntity != null)
        {
            return foodGridEntity.Position;
        }

        // Fallback: if no GridEntity is found, calculate from world position.
        return GridPositionManager.Instance.WorldToGrid(food.transform.position);
    }


    private void SetTargetFood(GameObject food)
    {
        currentTargetFood = food;

        // --- FIX: Use the helper to get the correct ground-level grid position for the food
        GridPosition foodGridPos = GetFoodGroundPosition(food);

        currentPath = GridPositionManager.Instance.GetPath(gridEntity.Position, foodGridPos, false);
        currentPathIndex = 0;

        if (currentPath.Count > 0)
        {
            hasPlannedAction = true;
        }
        else
        {
            currentPath.Clear();
            hasPlannedAction = false;
        }
    }

    private void PlanWandering()
    {
        if (GridDebugVisualizer.Instance != null)
        {
            GridDebugVisualizer.Instance.HideContinuousRadius(controller);
        }
        ClearPathDebugLine();

        currentPath.Clear();
        currentTargetFood = null;

        // Chance to just pause for a bit
        if (Random.Range(0, 100) < definition.wanderPauseTickChance)
        {
            wanderPauseTicks = Random.Range(definition.minWanderPauseTicks, definition.maxWanderPauseTicks);
            hasPlannedAction = false;
            return;
        }

        GridPosition currentPos = gridEntity.Position;
        GridPosition[] directions = {
            GridPosition.Up, GridPosition.Down,
            GridPosition.Left, GridPosition.Right
        };

        // Shuffle directions to avoid bias
        for (int i = 0; i < directions.Length; i++)
        {
            int randomIndex = Random.Range(i, directions.Length);
            GridPosition temp = directions[i];
            directions[i] = directions[randomIndex];
            directions[randomIndex] = temp;
        }

        // Try to move in a random valid direction
        foreach (var dir in directions)
        {
            GridPosition targetPos = currentPos + dir;
            if (IsValidMove(targetPos))
            {
                currentPath.Clear();
                currentPath.Add(targetPos);
                currentPathIndex = 0;
                hasPlannedAction = true;
                return;
            }
        }

        // No valid moves found
        hasPlannedAction = false;
    }

    private void ExecutePlannedMovement()
    {
        if (!hasPlannedAction || gridEntity.IsMoving) return;

        // If we are close enough to our food target, eat it
        if (currentTargetFood != null)
        {
            // --- FIX: Use the helper to get the correct ground position for distance checking
            GridPosition foodPos = GetFoodGroundPosition(currentTargetFood);
            int distance = gridEntity.Position.ManhattanDistance(foodPos);

            if (distance <= definition.eatDistanceTiles)
            {
                controller.Behavior.StartEating(currentTargetFood);
                ClearMovementPlan();
                return;
            }
        }

        // Otherwise, continue moving along the path
        if (currentPath != null && currentPath.Count > 0 && currentPathIndex < currentPath.Count)
        {
            GridPosition nextPosition = currentPath[currentPathIndex];

            if (TryMoveTo(nextPosition))
            {
                currentPathIndex++;

                // If we've reached the end of the path, clear the plan
                if (currentPathIndex >= currentPath.Count)
                {
                    ClearMovementPlan();
                }
            }
            else
            {
                // If we can't move to the next position, re-plan
                if (currentTargetFood != null)
                {
                    SetTargetFood(currentTargetFood); // Re-path to the same food
                }
                else
                {
                    ClearMovementPlan(); // Or just clear if it was a wander
                }
            }
        }
        else
        {
            // No path, or path is complete
            hasPlannedAction = false;
        }
    }

    private bool TryMoveTo(GridPosition targetPos)
    {
        if (!IsValidMove(targetPos)) return false;

        Vector3 currentWorld = transform.position;
        Vector3 targetWorld = GridPositionManager.Instance.GridToWorld(targetPos);
        lastMoveDirection = (targetWorld - currentWorld).normalized;

        gridEntity.SetPosition(targetPos);
        return true;
    }

    private bool IsValidMove(GridPosition pos)
    {
        if (GridPositionManager.Instance == null) return false;

        // Is the position within the grid bounds?
        if (!GridPositionManager.Instance.IsPositionValid(pos)) return false;

        // Is the tile occupied by something we can't move into?
        if (GridPositionManager.Instance.IsPositionOccupied(pos))
        {
            // Exception: we can move "into" our food target's tile to eat it
            if (currentTargetFood != null)
            {
                GridPosition foodPos = GetFoodGroundPosition(currentTargetFood);
                if (pos == foodPos) return true;
            }
            return false;
        }

        return true;
    }

    private void HandleScreenCenterSeeking()
    {
        Vector2 currentPos = transform.position;
        bool centerWithinBounds = currentPos.x >= minBounds.x && currentPos.x <= maxBounds.x &&
                                 currentPos.y >= minBounds.y && currentPos.y <= maxBounds.y;

        if (centerWithinBounds)
        {
            // We've reached the screen, stop seeking
            isSeekingScreenCenter = false;
            hasPlannedAction = false;
        }
        else
        {
            // Still offscreen, plan a path to the center
            GridPosition targetGridPos = GridPositionManager.Instance.WorldToGrid(screenCenterTarget);
            currentPath = GridPositionManager.Instance.GetPath(gridEntity.Position, targetGridPos, false);
            currentPathIndex = 0;
            hasPlannedAction = currentPath.Count > 0;
        }
    }

    public void SetSeekingScreenCenter(Vector2 target, Vector2 minBounds, Vector2 maxBounds)
    {
        isSeekingScreenCenter = true;
        screenCenterTarget = target;
        this.minBounds = minBounds;
        this.maxBounds = maxBounds;
    }

    public void StopAllMovement()
    {
        ClearMovementPlan();
        wanderPauseTicks = 0;
        isSeekingScreenCenter = false;
    }

    public void ClearMovementPlan()
    {
        currentPath.Clear();
        currentPathIndex = 0;
        currentTargetFood = null;
        hasPlannedAction = false;
        ClearPathDebugLine();

        if (GridDebugVisualizer.Instance != null)
        {
            GridDebugVisualizer.Instance.HideContinuousRadius(controller);
        }
    }

    public Vector2 GetLastMoveDirection()
    {
        return lastMoveDirection;
    }

    #region Debugging
    private void SetupDebugLineRenderer()
    {
        if (!showPathfindingDebugLine) return;

        GameObject lineObj = new GameObject("PathDebugLine");
        lineObj.transform.SetParent(transform);
        pathDebugLine = lineObj.AddComponent<LineRenderer>();
        pathDebugLine.startWidth = 0.05f;
        pathDebugLine.endWidth = 0.05f;
        pathDebugLine.material = new Material(Shader.Find("Sprites/Default"));
        pathDebugLine.startColor = Color.yellow;
        pathDebugLine.endColor = Color.red;
        pathDebugLine.sortingOrder = 100;
    }

    private void UpdatePathDebugLine()
    {
        if (!showPathfindingDebugLine || pathDebugLine == null || currentPath == null || currentPath.Count == 0)
        {
            if (pathDebugLine != null) pathDebugLine.positionCount = 0;
            return;
        }

        List<Vector3> positions = new List<Vector3>();
        
        // Start the line from the animal's current ground position
        Vector3 groundPosition = GridPositionManager.Instance.GridToWorld(gridEntity.Position);
        positions.Add(groundPosition);

        // Add the rest of the path waypoints
        for (int i = currentPathIndex; i < currentPath.Count; i++)
        {
            Vector3 worldPos = GridPositionManager.Instance.GridToWorld(currentPath[i]);
            positions.Add(worldPos);
        }

        pathDebugLine.positionCount = positions.Count;
        pathDebugLine.SetPositions(positions.ToArray());
    }

    private void ClearPathDebugLine()
    {
        if (pathDebugLine != null)
        {
            pathDebugLine.positionCount = 0;
        }
    }
    #endregion
}