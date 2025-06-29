using System.Collections.Generic;
using UnityEngine;
using WegoSystem;

public class AnimalMovement : MonoBehaviour
{
    [SerializeField] bool showPathfindingDebugLine = false;

    AnimalController controller;
    AnimalDefinition definition;
    GridEntity gridEntity;
    LineRenderer pathDebugLine;

    List<GridPosition> currentPath = new List<GridPosition>();
    int currentPathIndex = 0;
    GameObject currentTargetFood = null;
    bool hasPlannedAction = false;
    int wanderPauseTicks = 0;
    int lastThinkTick = 0;

    bool isSeekingScreenCenter = false;
    Vector2 screenCenterTarget;
    Vector2 minBounds;
    Vector2 maxBounds;

    Vector2 lastMoveDirection;

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

        if (currentTick - lastThinkTick >= definition.thinkingTickInterval)
        {
            MakeMovementDecision();
            lastThinkTick = currentTick;
        }

        if (hasPlannedAction && !gridEntity.IsMoving)
        {
            ExecutePlannedMovement();
        }
    }

    public void UpdateVisuals()
    {
        UpdatePathDebugLine();
    }

    void MakeMovementDecision()
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

    void PlanFoodSeeking()
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
            PlanWandering();
        }
    }

    GameObject FindNearestFood()
    {
        if (definition.diet == null) return null;

        GameObject foodFromGrid = FindFoodInGrid();
        if (foodFromGrid != null) return foodFromGrid;

        return FindFoodByCollider();
    }

    GameObject FindFoodInGrid()
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

                GridPosition foodGroundPos = GetFoodGroundPosition(entity.gameObject);
                float distance = gridEntity.Position.ManhattanDistance(foodGroundPos);

                float score = pref.preferencePriority / (1f + distance);

                if (score > bestScore)
                {
                    bestScore = score;
                    bestFood = entity.gameObject;
                }
            }
        }

        return bestFood;
    }

    GameObject FindFoodByCollider()
    {
        Vector3 worldPos = transform.position;
        float tileSize = GridPositionManager.Instance.GetTilemapGrid()?.cellSize.x ?? 1f;
        float searchRadius = definition.searchRadiusTiles * tileSize;

        Collider2D[] colliders = Physics2D.OverlapCircleAll(worldPos, searchRadius);

        GameObject bestFood = null;
        float bestScore = -1f;

        foreach (var collider in colliders)
        {
            if (collider.gameObject == this.gameObject) continue;

            FoodItem foodItem = collider.GetComponent<FoodItem>();
            if (foodItem != null && foodItem.foodType != null && definition.diet.CanEat(foodItem.foodType))
            {
                var pref = definition.diet.GetPreference(foodItem.foodType);
                if (pref == null) continue;

                float distance = Vector3.Distance(worldPos, collider.transform.position);
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

    GridPosition GetFoodGroundPosition(GameObject food)
    {
        GridEntity foodEntity = food.GetComponent<GridEntity>();
        if (foodEntity != null)
        {
            return foodEntity.Position;
        }

        Vector3 foodWorldPos = food.transform.position;
        return GridPositionManager.Instance.WorldToGrid(foodWorldPos);
    }

    void SetTargetFood(GameObject food)
    {
        currentTargetFood = food;
        GridPosition foodGroundPos = GetFoodGroundPosition(food);

        List<GridPosition> path = GridPositionManager.Instance.GetPath(gridEntity.Position, foodGroundPos, false);
        if (path != null && path.Count > 0)
        {
            currentPath = path;
            currentPathIndex = 0;
            hasPlannedAction = true;
        }
        else
        {
            currentTargetFood = null;
        }
    }

    void PlanWandering()
    {
        if (GridDebugVisualizer.Instance != null)
        {
            GridDebugVisualizer.Instance.HideContinuousRadius(controller);
        }
        ClearPathDebugLine();

        currentPath.Clear();
        currentTargetFood = null;

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

        for (int i = 0; i < directions.Length; i++)
        {
            int randomIndex = Random.Range(i, directions.Length);
            GridPosition temp = directions[i];
            directions[i] = directions[randomIndex];
            directions[randomIndex] = temp;
        }

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

        hasPlannedAction = false;
    }

    void ExecutePlannedMovement()
    {
        if (!hasPlannedAction || gridEntity.IsMoving) return;

        if (currentTargetFood != null)
        {
            GridPosition foodPos = GetFoodGroundPosition(currentTargetFood);
            int distance = gridEntity.Position.ManhattanDistance(foodPos);

            if (distance <= definition.eatDistanceTiles)
            {
                controller.Behavior.StartEating(currentTargetFood);
                ClearMovementPlan();
                return;
            }
        }

        if (currentPath != null && currentPath.Count > 0 && currentPathIndex < currentPath.Count)
        {
            GridPosition nextPosition = currentPath[currentPathIndex];

            if (TryMoveTo(nextPosition))
            {
                currentPathIndex++;

                if (currentPathIndex >= currentPath.Count)
                {
                    ClearMovementPlan();
                }
            }
            else
            {
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
            hasPlannedAction = false;
        }
    }

    bool TryMoveTo(GridPosition targetPos)
    {
        if (!IsValidMove(targetPos))
            return false;

        Vector3 currentWorld = transform.position;
        Vector3 targetWorld = GridPositionManager.Instance.GridToWorld(targetPos);
        lastMoveDirection = (targetWorld - currentWorld).normalized;

        gridEntity.SetPosition(targetPos);
        return true;
    }

    bool IsValidMove(GridPosition pos)
    {
        if (GridPositionManager.Instance == null) return false;

        if (!GridPositionManager.Instance.IsPositionValid(pos)) return false;

        if (GridPositionManager.Instance.IsPositionOccupied(pos))
        {
            if (currentTargetFood != null)
            {
                GridPosition foodPos = GetFoodGroundPosition(currentTargetFood);
                if (pos == foodPos) return true;
            }
            return false;
        }

        return true;
    }

    void HandleScreenCenterSeeking()
    {
        Vector2 currentPos = transform.position;
        bool centerWithinBounds = currentPos.x >= minBounds.x && currentPos.x <= maxBounds.x &&
                                 currentPos.y >= minBounds.y && currentPos.y <= maxBounds.y;

        if (centerWithinBounds)
        {
            isSeekingScreenCenter = false;
            hasPlannedAction = false;
        }
        else
        {
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

    void SetupDebugLineRenderer()
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

    void UpdatePathDebugLine()
    {
        if (!showPathfindingDebugLine || pathDebugLine == null || currentPath == null || currentPath.Count == 0)
        {
            if (pathDebugLine != null) pathDebugLine.positionCount = 0;
            return;
        }

        List<Vector3> positions = new List<Vector3>();

        Vector3 groundPosition = GridPositionManager.Instance.GridToWorld(gridEntity.Position);
        positions.Add(groundPosition);

        for (int i = currentPathIndex; i < currentPath.Count; i++)
        {
            Vector3 worldPos = GridPositionManager.Instance.GridToWorld(currentPath[i]);
            positions.Add(worldPos);
        }

        pathDebugLine.positionCount = positions.Count;
        pathDebugLine.SetPositions(positions.ToArray());
    }

    void ClearPathDebugLine()
    {
        if (pathDebugLine != null)
        {
            pathDebugLine.positionCount = 0;
        }
    }
}