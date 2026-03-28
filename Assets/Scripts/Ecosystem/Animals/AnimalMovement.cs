// FILE: Assets/Scripts/Ecosystem/Animals/AnimalMovement.cs
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using WegoSystem;
using Abracodabra.Genes;

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
    PlantGrowth currentTargetPlant = null;
    bool hasPlannedAction = false;
    int wanderPauseTicks = 0;
    int lastThinkTick = 0;

    bool isSeekingScreenCenter = false;
    Vector2 screenCenterTarget;
    Vector2 minBounds;
    Vector2 maxBounds;

    Vector2 lastMoveDirection;
    float _speedAccumulator = 0f;

    // ═══════════════════════════════════════════════════════
    //  FEAR / FLEE STATE
    // ═══════════════════════════════════════════════════════
    bool isFleeing = false;
    Vector3 fleeSourcePosition;

    public bool HasTarget => currentTargetFood != null || currentTargetPlant != null || hasPlannedAction;
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
            _speedAccumulator = 0f;
            return;
        }

        if (wanderPauseTicks > 0)
        {
            wanderPauseTicks--;
            return;
        }

        int slowPenalty = controller.StatusManager != null ? controller.StatusManager.AdditionalMoveTicks : 0;
        float effectiveSpeed = definition.movementSpeed / (1f + slowPenalty);
        _speedAccumulator += effectiveSpeed;

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

        // Fear fleeing takes absolute priority
        if (isFleeing)
        {
            PlanFleeing();
            return;
        }

        if (isSeekingScreenCenter)
        {
            HandleScreenCenterSeeking();
            return;
        }

        if (definition.isPest)
        {
            PlanPlantTargeting();
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

    // ═══════════════════════════════════════════════════════
    //  FEAR / FLEE — Public API
    // ═══════════════════════════════════════════════════════

    /// <summary>
    /// Start fleeing away from the given source position.
    /// Called by AnimalController.ApplyFear().
    /// </summary>
    public void StartFleeing(Vector3 sourcePosition)
    {
        isFleeing = true;
        fleeSourcePosition = sourcePosition;

        // Immediately plan a flee path
        ClearMovementPlan();
        PlanFleeing();
    }

    /// <summary>
    /// Update the fear source position (e.g., when fear is refreshed from a new source).
    /// </summary>
    public void UpdateFleeSource(Vector3 sourcePosition)
    {
        fleeSourcePosition = sourcePosition;
    }

    /// <summary>
    /// Stop fleeing. Called when fear expires.
    /// </summary>
    public void StopFleeing()
    {
        isFleeing = false;
        ClearMovementPlan();
    }

    /// <summary>
    /// Plan movement directly away from the fear source.
    /// Picks the best cardinal direction that moves away from the source.
    /// </summary>
    void PlanFleeing()
    {
        GridPosition currentPos = gridEntity.Position;
        Vector3 currentWorld = GridPositionManager.Instance.GridToWorld(currentPos);

        // Direction away from fear source
        Vector2 fleeDir = ((Vector2)currentWorld - (Vector2)fleeSourcePosition).normalized;

        // If we're right on top of the source, pick a random direction
        if (fleeDir.sqrMagnitude < 0.01f)
        {
            fleeDir = new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized;
        }

        // Score each cardinal direction by how well it aligns with the flee direction
        GridPosition[] directions = {
            GridPosition.Up, GridPosition.Down,
            GridPosition.Left, GridPosition.Right
        };

        float[] scores = new float[4];
        Vector2[] dirVecs = {
            Vector2.up, Vector2.down,
            Vector2.left, Vector2.right
        };

        for (int i = 0; i < 4; i++)
        {
            scores[i] = Vector2.Dot(fleeDir, dirVecs[i]);
        }

        // Sort by score descending — prefer directions most aligned with flee vector
        var ranked = Enumerable.Range(0, 4).OrderByDescending(i => scores[i]).ToArray();

        foreach (int idx in ranked)
        {
            GridPosition targetPos = currentPos + directions[idx];

            bool valid = definition.isPest ? IsValidMoveForPest(targetPos) : IsValidMove(targetPos);
            if (valid)
            {
                currentPath.Clear();
                currentPath.Add(targetPos);
                currentPathIndex = 0;
                hasPlannedAction = true;
                return;
            }
        }

        // Can't flee anywhere — stay put (cornered)
        hasPlannedAction = false;
    }

    // ═══════════════════════════════════════════════════════
    //  PEST PLANT TARGETING
    // ═══════════════════════════════════════════════════════

    void PlanPlantTargeting()
    {
        if (currentTargetPlant != null && (currentTargetPlant == null || currentTargetPlant.CurrentState == PlantState.Dead))
        {
            currentTargetPlant = null;
            ClearMovementPlan();
        }

        if (currentTargetPlant == null)
        {
            currentTargetPlant = FindNearestMaturePlant();
        }

        if (currentTargetPlant == null)
        {
            PlanWandering();
            return;
        }

        GridPosition plantPos = GetPlantGridPosition(currentTargetPlant);
        GridPosition bestTarget = GridPosition.Zero;
        List<GridPosition> shortestPath = null;
        float shortestDist = float.MaxValue;

        GridPosition[] neighbors = {
            plantPos + GridPosition.Up,
            plantPos + GridPosition.Down,
            plantPos + GridPosition.Left,
            plantPos + GridPosition.Right
        };

        foreach (var pos in neighbors)
        {
            if (!IsValidMoveForPest(pos)) continue;

            if (gridEntity.Position == pos)
            {
                hasPlannedAction = false;
                currentPath.Clear();
                return;
            }

            var path = GridPositionManager.Instance.GetPath(gridEntity.Position, pos, false);
            if (path != null && path.Count > 0 && path.Count < shortestDist)
            {
                shortestDist = path.Count;
                bestTarget = pos;
                shortestPath = path;
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
            PlanWandering();
        }
    }

    PlantGrowth FindNearestMaturePlant()
    {
        PlantGrowth nearest = null;
        float nearestDist = float.MaxValue;

        foreach (var plant in PlantGrowth.AllActivePlants)
        {
            if (plant == null) continue;
            if (plant.CurrentState != PlantState.Mature) continue;

            float dist = Vector3.Distance(transform.position, plant.transform.position);
            if (dist < nearestDist)
            {
                nearestDist = dist;
                nearest = plant;
            }
        }

        return nearest;
    }

    GridPosition GetPlantGridPosition(PlantGrowth plant)
    {
        var ge = plant.GetComponent<GridEntity>();
        if (ge != null) return ge.Position;
        return GridPositionManager.Instance.WorldToGrid(plant.transform.position);
    }

    bool IsValidMoveForPest(GridPosition pos)
    {
        if (GridPositionManager.Instance == null) return false;
        if (!GridPositionManager.Instance.IsPositionValid(pos)) return false;
        if (GridPositionManager.Instance.IsMovementBlockedAt(pos)) return false;
        return true;
    }

    // ═══════════════════════════════════════════════════════
    //  FOOD SEEKING
    // ═══════════════════════════════════════════════════════

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

        var entitiesInRadius = GridPositionManager.Instance.GetEntitiesInRadius(
            gridEntity.Position,
            definition.searchRadiusTiles,
            true
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

    GridPosition GetFoodGroundPosition(GameObject food)
    {
        GridEntity foodEntity = food.GetComponent<GridEntity>();
        if (foodEntity != null && foodEntity.enabled)
        {
            return foodEntity.Position;
        }

        Debug.LogWarning($"[AnimalMovement] Food item '{food.name}' did not have an enabled GridEntity. Using world position as fallback.", food);
        return GridPositionManager.Instance.WorldToGrid(food.transform.position);
    }

    void SetTargetFood(GameObject food)
    {
        currentTargetFood = food;
        GridPosition foodGroundPos = GetFoodGroundPosition(food);

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
            currentTargetFood = null;
        }
    }

    List<GridPosition> GetValidEatingPositions(GridPosition foodPos)
    {
        var positions = new List<GridPosition>();
        for (int x = -definition.eatDistanceTiles; x <= definition.eatDistanceTiles; x++)
        {
            for (int y = -definition.eatDistanceTiles; y <= definition.eatDistanceTiles; y++)
            {
                if (x == 0 && y == 0) continue;

                int distance = Mathf.Abs(x) + Mathf.Abs(y);
                if (distance == definition.eatDistanceTiles)
                {
                    positions.Add(foodPos + new GridPosition(x, y));
                }
            }
        }
        return positions;
    }

    // ═══════════════════════════════════════════════════════
    //  WANDERING
    // ═══════════════════════════════════════════════════════

    void PlanWandering()
    {
        if (GridDebugVisualizer.Instance != null)
        {
            GridDebugVisualizer.Instance.HideContinuousRadius(controller);
        }
        ClearPathDebugLine();

        currentPath.Clear();
        currentTargetFood = null;
        currentTargetPlant = null;

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

    // ═══════════════════════════════════════════════════════
    //  MOVEMENT EXECUTION
    // ═══════════════════════════════════════════════════════

    void ExecutePlannedMovement()
    {
        if (!hasPlannedAction || gridEntity.IsMoving) return;

        if (currentTargetFood != null)
        {
            if (!currentTargetFood.activeInHierarchy)
            {
                ClearMovementPlan();
                return;
            }

            GridPosition foodPos = GetFoodGroundPosition(currentTargetFood);
            int distance = gridEntity.Position.ManhattanDistance(foodPos);

            if (distance == definition.eatDistanceTiles)
            {
                controller.Behavior.StartEating(currentTargetFood);
                ClearMovementPlan();
                return;
            }
        }

        if (definition.isPest && currentTargetPlant != null)
        {
            GridPosition plantPos = GetPlantGridPosition(currentTargetPlant);
            int dist = gridEntity.Position.ManhattanDistance(plantPos);
            if (dist <= 1)
            {
                hasPlannedAction = false;
                currentPath.Clear();
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

    bool TryMoveTo(GridPosition targetPos)
    {
        bool valid = definition.isPest ? IsValidMoveForPest(targetPos) : IsValidMove(targetPos);
        if (!valid) return false;

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

        var entitiesAtPos = GridPositionManager.Instance.GetEntitiesAt(pos);
        foreach (var entity in entitiesAtPos)
        {
            if (entity.GetComponent<PlantCell>() != null)
            {
                return false;
            }
        }

        if (GridPositionManager.Instance.IsMovementBlockedAt(pos))
        {
            if (currentTargetFood != null)
            {
                GridPosition foodPos = GetFoodGroundPosition(currentTargetFood);
                if (pos == foodPos) return false;
            }
            return false;
        }

        return true;
    }

    // ═══════════════════════════════════════════════════════
    //  SCREEN CENTER SEEKING
    // ═══════════════════════════════════════════════════════

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

    // ═══════════════════════════════════════════════════════
    //  UTILITIES
    // ═══════════════════════════════════════════════════════

    public void StopAllMovement()
    {
        ClearMovementPlan();
        wanderPauseTicks = 0;
        isSeekingScreenCenter = false;
        isFleeing = false;
    }

    public void ClearMovementPlan()
    {
        currentPath.Clear();
        currentPathIndex = 0;
        currentTargetFood = null;
        currentTargetPlant = null;
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

    // ═══════════════════════════════════════════════════════
    //  DEBUG LINE RENDERER
    // ═══════════════════════════════════════════════════════

    void SetupDebugLineRenderer()
    {
        if (!showPathfindingDebugLine) return;

        pathDebugLine = GetComponent<LineRenderer>();
        if (pathDebugLine == null)
        {
            pathDebugLine = gameObject.AddComponent<LineRenderer>();
        }

        pathDebugLine.startWidth = 0.05f;
        pathDebugLine.endWidth = 0.05f;
        pathDebugLine.material = new Material(Shader.Find("Sprites/Default"));
        pathDebugLine.startColor = Color.yellow;
        pathDebugLine.endColor = Color.red;
        pathDebugLine.positionCount = 0;
        pathDebugLine.sortingOrder = 100;
    }

    void UpdatePathDebugLine()
    {
        if (pathDebugLine == null || !showPathfindingDebugLine) return;

        if (currentPath == null || currentPath.Count == 0 || currentPathIndex >= currentPath.Count)
        {
            pathDebugLine.positionCount = 0;
            return;
        }

        int remainingPoints = currentPath.Count - currentPathIndex;
        pathDebugLine.positionCount = remainingPoints + 1;

        pathDebugLine.SetPosition(0, transform.position);

        for (int i = 0; i < remainingPoints; i++)
        {
            Vector3 worldPos = GridPositionManager.Instance.GridToWorld(currentPath[currentPathIndex + i]);
            pathDebugLine.SetPosition(i + 1, worldPos);
        }
    }

    void ClearPathDebugLine()
    {
        if (pathDebugLine != null)
        {
            pathDebugLine.positionCount = 0;
        }
    }
}