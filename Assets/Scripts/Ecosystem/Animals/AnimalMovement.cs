using System.Collections.Generic;
using UnityEngine;
using WegoSystem;
using System.Linq;

public class AnimalMovement : MonoBehaviour
{
    [Header("Debug")]
    [SerializeField] private bool showPathfindingDebugLine = false;
    
    // References
    private AnimalController controller;
    private AnimalDefinition definition;
    private GridEntity gridEntity;
    private LineRenderer pathDebugLine;
    
    // Movement State
    private List<GridPosition> currentPath = new List<GridPosition>();
    private int currentPathIndex = 0;
    private GameObject currentTargetFood = null;
    private bool hasPlannedAction = false;
    private int wanderPauseTicks = 0;
    private int lastThinkTick = 0;
    
    // Screen Center Seeking
    private bool isSeekingScreenCenter = false;
    private Vector2 screenCenterTarget;
    private Vector2 minBounds;
    private Vector2 maxBounds;
    
    // Movement tracking
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
        
        // Decrement wander pause
        if (wanderPauseTicks > 0)
        {
            wanderPauseTicks--;
            return;
        }
        
        // Make decision periodically
        if (currentTick - lastThinkTick >= definition.thinkingTickInterval)
        {
            MakeMovementDecision();
            lastThinkTick = currentTick;
        }
        
        // Execute movement
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
        
        // Priority 1: Screen center seeking
        if (isSeekingScreenCenter)
        {
            HandleScreenCenterSeeking();
            return;
        }
        
        // Priority 2: Food seeking when hungry
        if (controller.Needs.IsHungry)
        {
            PlanFoodSeeking();
        }
        // Priority 3: Wander
        else
        {
            PlanWandering();
        }
    }
    
    private void PlanFoodSeeking()
    {
        // Show search radius in debug mode
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
            // No food found, wander instead
            PlanWandering();
        }
    }
    
    private GameObject FindNearestFood()
    {
        if (definition.diet == null) return null;
        
        // Method 1: Search for GridEntities with FoodItem components
        GameObject foodFromGrid = FindFoodInGrid();
        if (foodFromGrid != null) return foodFromGrid;
        
        // Method 2: Direct collider search as fallback
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
                
                float distance = entity.Position.ManhattanDistance(gridEntity.Position);
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
    
    private GameObject FindFoodByCollider()
    {
        // Get world position and search radius
        Vector3 worldPos = transform.position;
        float worldRadius = definition.searchRadiusTiles * GridPositionManager.Instance.GetTilemapGrid().cellSize.x;
        
        // Find all colliders in radius
        Collider2D[] colliders = Physics2D.OverlapCircleAll(worldPos, worldRadius);
        
        GameObject bestFood = null;
        float bestScore = -1f;
        
        foreach (var collider in colliders)
        {
            if (collider == null || collider.gameObject == this.gameObject) continue;
            
            // Skip poop
            if (collider.GetComponent<PoopController>() != null) continue;
            
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
    
    private void SetTargetFood(GameObject food)
    {
        currentTargetFood = food;
        
        // Get food's grid position
        GridPosition foodGridPos;
        GridEntity foodGridEntity = food.GetComponent<GridEntity>();
        
        if (foodGridEntity != null)
        {
            foodGridPos = foodGridEntity.Position;
        }
        else
        {
            foodGridPos = GridPositionManager.Instance.WorldToGrid(food.transform.position);
        }
        
        // Generate path to food
        currentPath = GridPositionManager.Instance.GetPath(gridEntity.Position, foodGridPos, false);
        currentPathIndex = 0;
        
        if (currentPath.Count > 0)
        {
            hasPlannedAction = true;
        }
        else
        {
            // No path found, try direct movement
            currentPath.Clear();
            hasPlannedAction = false;
        }
    }
    
    private void PlanWandering()
    {
        // Hide debug visuals
        if (GridDebugVisualizer.Instance != null)
        {
            GridDebugVisualizer.Instance.HideContinuousRadius(controller);
        }
        ClearPathDebugLine();
        
        currentPath.Clear();
        currentTargetFood = null;
        
        // Random pause chance
        if (Random.Range(0, 100) < definition.wanderPauseTickChance)
        {
            wanderPauseTicks = Random.Range(definition.minWanderPauseTicks, definition.maxWanderPauseTicks);
            hasPlannedAction = false;
            return;
        }
        
        // Pick random adjacent tile
        GridPosition currentPos = gridEntity.Position;
        GridPosition[] directions = {
            GridPosition.Up, GridPosition.Down,
            GridPosition.Left, GridPosition.Right
        };
        
        // Shuffle directions
        for (int i = 0; i < directions.Length; i++)
        {
            int randomIndex = Random.Range(i, directions.Length);
            GridPosition temp = directions[i];
            directions[i] = directions[randomIndex];
            directions[randomIndex] = temp;
        }
        
        // Try each direction
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
        
        // Check if we're close enough to eat
        if (currentTargetFood != null)
        {
            GridPosition foodPos = GetFoodGridPosition(currentTargetFood);
            int distance = gridEntity.Position.ManhattanDistance(foodPos);
            
            if (distance <= definition.eatDistanceTiles)
            {
                controller.Behavior.StartEating(currentTargetFood);
                ClearMovementPlan();
                return;
            }
        }
        
        // Follow path
        if (currentPath != null && currentPath.Count > 0 && currentPathIndex < currentPath.Count)
        {
            GridPosition nextPosition = currentPath[currentPathIndex];
            
            // Try to move to next position
            if (TryMoveTo(nextPosition))
            {
                currentPathIndex++;
                
                // Check if path complete
                if (currentPathIndex >= currentPath.Count)
                {
                    ClearMovementPlan();
                }
            }
            else
            {
                // Path blocked, recalculate
                if (currentTargetFood != null)
                {
                    SetTargetFood(currentTargetFood);
                }
                else
                {
                    ClearMovementPlan();
                }
            }
        }
        else
        {
            hasPlannedAction = false;
        }
    }
    
    private bool TryMoveTo(GridPosition targetPos)
    {
        if (!IsValidMove(targetPos)) return false;
        
        // Calculate direction for visual feedback
        Vector3 currentWorld = transform.position;
        Vector3 targetWorld = GridPositionManager.Instance.GridToWorld(targetPos);
        lastMoveDirection = (targetWorld - currentWorld).normalized;
        
        // Move
        gridEntity.SetPosition(targetPos);
        return true;
    }
    
    private bool IsValidMove(GridPosition pos)
    {
        if (GridPositionManager.Instance == null) return false;
        
        // Check if position is valid
        if (!GridPositionManager.Instance.IsPositionValid(pos)) return false;
        
        // Check if occupied (unless it's our target food)
        if (GridPositionManager.Instance.IsPositionOccupied(pos))
        {
            if (currentTargetFood != null)
            {
                GridPosition foodPos = GetFoodGridPosition(currentTargetFood);
                if (pos == foodPos) return true;
            }
            return false;
        }
        
        return true;
    }
    
    private GridPosition GetFoodGridPosition(GameObject food)
    {
        if (food == null) return GridPosition.Zero;
        
        GridEntity foodGridEntity = food.GetComponent<GridEntity>();
        if (foodGridEntity != null)
        {
            return foodGridEntity.Position;
        }
        else
        {
            return GridPositionManager.Instance.WorldToGrid(food.transform.position);
        }
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
    
    #region Debug Visualization
    
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
        positions.Add(transform.position);
        
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