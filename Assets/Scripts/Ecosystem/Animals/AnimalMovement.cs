using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using WegoSystem;
using Abracodabra.Genes; // Required for PlantCell

public class AnimalMovement : MonoBehaviour
{
    [SerializeField] private bool showPathfindingDebugLine = false;

    private AnimalController controller;
    private AnimalDefinition definition;
    private GridEntity gridEntity;
    private LineRenderer pathDebugLine;

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
    private float _speedAccumulator = 0f;

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
        if (!enabled || controller.IsDying || controller.Behavior.IsEating || controller.Behavior.IsPooping)
        {
            _speedAccumulator = 0f; // Reset speed if action is interrupted
            return;
        }

        if (wanderPauseTicks > 0)
        {
            wanderPauseTicks--;
            return;
        }

        _speedAccumulator += definition.movementSpeed;

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
            PlanWandering();
        }
    }

    private GameObject FindNearestFood()
    {
        if (definition.diet == null) return null;

        var entitiesInRadius = GridPositionManager.Instance.GetEntitiesInRadius(
            gridEntity.Position,
            definition.searchRadiusTiles,
            true // Use circle radius
        );

        GameObject bestFood = null;
        float bestScore = -1f;

        var foodItems = entitiesInRadius
            .Select(entity => entity.GetComponent<FoodItem>())
            .Where(foodItem => foodItem != null && foodItem.foodType != null && definition.diet.CanEat(foodItem.foodType));

        foreach (var foodItem in foodItems)
        {
            var pref = definition.diet.GetPreference(foodItem.foodType);
            if (pref == null) continue;

            GridEntity foodEntity = foodItem.GetComponent<GridEntity>();
            if (foodEntity == null) continue;

            float distance = gridEntity.Position.ManhattanDistance(foodEntity.Position);
            float score = pref.preferencePriority / (1f + distance);

            if (score > bestScore)
            {
                bestScore = score;
                bestFood = foodItem.gameObject;
            }
        }

        return bestFood;
    }

    private GridPosition GetFoodGroundPosition(GameObject food)
    {
        GridEntity foodEntity = food.GetComponent<GridEntity>();
        if (foodEntity != null && foodEntity.enabled)
        {
            return foodEntity.Position;
        }

        Debug.LogWarning($"[AnimalMovement] Food item '{food.name}' did not have an enabled GridEntity. Using world position as fallback.", food);
        return GridPositionManager.Instance.WorldToGrid(food.transform.position);
    }

    private void SetTargetFood(GameObject food)
    {
        currentTargetFood = food;
        GridPosition foodGroundPos = GetFoodGroundPosition(food);

        // NEW LOGIC: Pathfind to a tile *next to* the food, not on it.
        List<GridPosition> validEatingPositions = GetValidEatingPositions(foodGroundPos);

        GridPosition bestTarget = GridPosition.Zero;
        float shortestDistance = float.MaxValue;
        List<GridPosition> shortestPath = null;

        foreach (var pos in validEatingPositions)
        {
            if (!IsValidMove(pos)) continue;

            var path = GridPositionManager.Instance.GetPath(gridEntity.Position, pos, false);
            if (path != null && path.Count > 0)
            {
                float distance = path.Count;
                if (distance < shortestDistance)
                {
                    shortestDistance = distance;
                    bestTarget = pos;
                    shortestPath = path;
                }
            }
        }
        
        if (shortestPath != null)
        {
            currentPath = shortestPath;
            currentPathIndex = 0;
            hasPlannedAction = true;
        }
        else
        {
            // If no valid path to an eating spot is found, clear the target.
            currentTargetFood = null;
        }
    }

    private List<GridPosition> GetValidEatingPositions(GridPosition foodPos)
    {
        var positions = new List<GridPosition>();
        // Get all positions at the exact eating distance
        for (int x = -definition.eatDistanceTiles; x <= definition.eatDistanceTiles; x++)
        {
            for (int y = -definition.eatDistanceTiles; y <= definition.eatDistanceTiles; y++)
            {
                if (x == 0 && y == 0) continue; // Skip the food's own position

                // Use Manhattan distance for grid-aligned movement
                int distance = Mathf.Abs(x) + Mathf.Abs(y);
                if (distance == definition.eatDistanceTiles)
                {
                    positions.Add(foodPos + new GridPosition(x, y));
                }
            }
        }
        return positions;
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

        if (Random.Range(0, 100) < definition.wanderPauseTickChance)
        {
            // The min/max wander MOVE ticks were removed, but pause ticks remain.
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

    private void ExecutePlannedMovement()
    {
        if (!hasPlannedAction || gridEntity.IsMoving) return;

        if (currentTargetFood != null)
        {
            // NEW: Check if food still exists before acting
            if (currentTargetFood == null || !currentTargetFood.activeInHierarchy)
            {
                ClearMovementPlan();
                return;
            }

            GridPosition foodPos = GetFoodGroundPosition(currentTargetFood);
            int distance = gridEntity.Position.ManhattanDistance(foodPos);

            // MODIFIED: Use exact distance check now that we pathfind to adjacent tiles.
            if (distance == definition.eatDistanceTiles)
            {
                controller.Behavior.StartEating(currentTargetFood);
                ClearMovementPlan();
                return;
            }
        }

        int tilesToMove = Mathf.FloorToInt(_speedAccumulator);
        if (tilesToMove <= 0) return;

        int tilesMovedSuccessfully = 0;
        for (int i = 0; i < tilesToMove; i++)
        {
            if (currentPath == null || currentPathIndex >= currentPath.Count)
            {
                ClearMovementPlan();
                break;
            }

            GridPosition nextPosition = currentPath[currentPathIndex];
            if (TryMoveTo(nextPosition))
            {
                currentPathIndex++;
                tilesMovedSuccessfully++;
            }
            else
            {
                if (currentTargetFood != null)
                {
                    SetTargetFood(currentTargetFood);
                }
                else
                {
                    ClearMovementPlan();
                }
                break;
            }
        }

        _speedAccumulator -= tilesMovedSuccessfully;

        if (currentPath != null && currentPathIndex >= currentPath.Count)
        {
            ClearMovementPlan();
        }
    }

    private bool TryMoveTo(GridPosition targetPos)
    {
        if (!IsValidMove(targetPos))
            return false;

        Vector3 currentWorld = transform.position;
        Vector3 targetWorld = GridPositionManager.Instance.GridToWorld(targetPos);
        lastMoveDirection = (targetWorld - currentWorld).normalized;

        gridEntity.SetPosition(targetPos);
        return true;
    }

    private bool IsValidMove(GridPosition pos)
    {
        if (GridPositionManager.Instance == null) return false;

        if (!GridPositionManager.Instance.IsPositionValid(pos)) return false;

        // NEW: Check for plant cells at this position and forbid movement.
        var entitiesAtPos = GridPositionManager.Instance.GetEntitiesAt(pos);
        foreach (var entity in entitiesAtPos)
        {
            if (entity.GetComponent<PlantCell>() != null)
            {
                return false;
            }
        }

        if (GridPositionManager.Instance.IsPositionOccupied(pos))
        {
            // The original logic here was fine, but now it's redundant with the PlantCell check above.
            // Kept for robustness in case other occupants exist.
            if (currentTargetFood != null)
            {
                GridPosition foodPos = GetFoodGroundPosition(currentTargetFood);
                // We shouldn't be pathing onto the food anymore, but as a safeguard:
                if (pos == foodPos) return false;
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
        _speedAccumulator = 0f;
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

    private void ClearPathDebugLine()
    {
        if (pathDebugLine != null)
        {
            pathDebugLine.positionCount = 0;
        }
    }
}