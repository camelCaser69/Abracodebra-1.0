Looking at your current scripts and requirements, I'll create a comprehensive guide for Phases 4 and 5, updated for your current implementation state.

# Wego System Phase 4-5 Implementation Guide

## Critical Issues to Address First

### Issue 1: Player Movement Not Working (HIGHEST PRIORITY)
**Problem**: Player movement is currently broken because `PlayerActionManager.ExecutePlayerMove()` is trying to use a queuing system during planning phase, but the actual movement execution during execution phase isn't implemented.

**Root Cause Analysis**:
- `GardenerController.HandleWegoInput()` correctly detects input and calls `PlayerActionManager.ExecutePlayerMove()`
- `PlayerActionManager` queues the move during planning phase but never actually processes it
- The queue is stored in `GardenerController` but there's no system to execute queued moves when execution phase begins

### Issue 2: Wave Duration Configuration Missing
**Problem**: Waves don't have configurable tick durations - they're hardcoded to day cycles.

### Issue 3: Planning Phase Between Waves Not Showing
**Problem**: The transition between waves doesn't properly return to planning phase.

---

## Phase 4: Core Mechanic Fixes

### 4.1 Fix Player Movement System (CRITICAL - Do First)

#### 4.1.1 Create Movement Execution in GardenerController
```csharp
// In GardenerController.cs, modify OnTickUpdate:
public void OnTickUpdate(int currentTick) {
    if (!useWegoMovement || isPlanting) return;
    
    // Only process moves during execution phase
    if (TurnPhaseManager.Instance?.CurrentPhase == TurnPhase.Execution) {
        if (plannedMoves.Count > 0) {
            GridPosition nextMove = plannedMoves.Dequeue();
            
            if (gridEntity != null && GridPositionManager.Instance != null) {
                if (GridPositionManager.Instance.IsPositionValid(nextMove) &&
                    !GridPositionManager.Instance.IsPositionOccupied(nextMove)) {
                    
                    // Calculate tick cost before moving
                    Vector3 worldPos = GridPositionManager.Instance.GridToWorld(nextMove);
                    int moveCost = PlayerActionManager.Instance.GetMovementTickCost(worldPos);
                    
                    // Execute the move
                    gridEntity.SetPosition(nextMove);
                    currentTargetPosition = nextMove;
                    
                    // Advance ticks based on cost
                    TickManager.Instance.AdvanceMultipleTicks(moveCost);
                }
            }
        }
    }
    
    hasMoveQueued = plannedMoves.Count > 0;
}
```

#### 4.1.2 Fix PlayerActionManager Queue System
```csharp
// In PlayerActionManager.cs, modify ExecutePlayerMove:
public bool ExecutePlayerMove(GardenerController gardener, GridPosition from, GridPosition to) {
    if (gardener == null) {
        OnActionFailed?.Invoke("No gardener controller");
        return false;
    }
    
    if (!ValidateMovement(from, to)) {
        OnActionFailed?.Invoke("Invalid movement");
        return false;
    }
    
    // During planning phase, just queue the move
    if (TurnPhaseManager.Instance?.IsInPlanningPhase == true) {
        gardener.QueueMovement(to);
        if (debugMode) Debug.Log($"[PlayerActionManager] Queued move to {to}");
        OnActionExecuted?.Invoke(PlayerActionType.Move, true);
        return true;
    }
    
    // During execution phase, moves are handled by GardenerController.OnTickUpdate
    // This method shouldn't be called during execution phase
    if (debugMode) Debug.Log("[PlayerActionManager] Move requested outside planning phase");
    return false;
}
```

#### 4.1.3 Clear Movement Queue on Phase Changes
```csharp
// In GardenerController.cs, modify OnPhaseChanged:
void OnPhaseChanged(TurnPhase oldPhase, TurnPhase newPhase) {
    if (newPhase == TurnPhase.Planning) {
        // Clear any remaining moves when returning to planning
        plannedMoves.Clear();
        hasMoveQueued = false;
        
        // Ensure we're snapped to our current position
        if (gridEntity != null) {
            currentTargetPosition = gridEntity.Position;
        }
    }
}
```

### 4.2 Add Configurable Day/Wave Durations

#### 4.2.1 Extend TickConfiguration
```csharp
// In TickConfiguration.cs, add:
[Header("Day/Night Cycle")]
public int ticksPerDay = 100;  // Total ticks for one full day

[Header("Wave System")]
public int ticksPerWave = 50;  // How long each wave lasts in ticks
public bool wavesDependOnDayCycle = false;  // If true, waves end with day cycles

public int GetDayProgress(int currentTick) {
    return currentTick % ticksPerDay;
}

public float GetDayProgressNormalized(int currentTick) {
    return (float)(currentTick % ticksPerDay) / ticksPerDay;
}
```

#### 4.2.2 Update WeatherManager to Use Tick-Based Days
```csharp
// In WeatherManager.cs, modify OnTickUpdate:
public void OnTickUpdate(int currentTick) {
    if (!useWegoSystem || !dayNightCycleEnabled || IsPaused) return;
    
    // Calculate day progress from ticks
    float dayProgress = TickManager.Instance.Config.GetDayProgressNormalized(currentTick);
    
    // Map progress to cycle phases
    if (dayProgress < 0.4f) {
        currentPhase = CyclePhase.Day;
        sunIntensity = 1f;
    } else if (dayProgress < 0.5f) {
        currentPhase = CyclePhase.TransitionToNight;
        float transitionProgress = (dayProgress - 0.4f) / 0.1f;
        sunIntensity = Mathf.Lerp(1f, 0f, transitionCurve.Evaluate(transitionProgress));
    } else if (dayProgress < 0.9f) {
        currentPhase = CyclePhase.Night;
        sunIntensity = 0f;
    } else {
        currentPhase = CyclePhase.TransitionToDay;
        float transitionProgress = (dayProgress - 0.9f) / 0.1f;
        sunIntensity = Mathf.Lerp(0f, 1f, transitionCurve.Evaluate(transitionProgress));
    }
    
    UpdateFadeSprite();
}
```

#### 4.2.3 Update WaveManager for Tick-Based Waves
```csharp
// In WaveManager.cs, add:
private int waveStartTick = 0;
private int GetWaveDurationTicks() {
    var config = TickManager.Instance?.Config;
    if (config == null) return 50;
    
    return config.wavesDependOnDayCycle ? 
        config.ticksPerDay * waveDurationInDayCycles : 
        config.ticksPerWave;
}

// Modify StartWaveForRound:
public void StartWaveForRound(int roundNumber) {
    // ... existing validation code ...
    
    waveStartTick = TickManager.Instance.CurrentTick;
    dayCyclesRemainingForThisWaveDef = 0; // Not using day cycles anymore
    
    // ... rest of method
}

// Add tick-based wave completion check:
public void CheckWaveCompletion(int currentTick) {
    if (currentActiveWaveDef == null) return;
    
    int ticksElapsed = currentTick - waveStartTick;
    int waveDuration = GetWaveDurationTicks();
    
    if (ticksElapsed >= waveDuration) {
        Debug.Log($"[WaveManager] Wave '{currentActiveWaveDef.waveName}' completed after {ticksElapsed} ticks");
        EndCurrentWave();
    }
}
```

### 4.3 Fix Planning Phase Transitions

#### 4.3.1 Ensure Planning Phase Shows Between Waves
```csharp
// In WaveManager.cs:
private void EndCurrentWave() {
    Debug.Log($"[WaveManager] Ending wave '{currentActiveWaveDef?.waveName}'");
    
    StopCurrentWaveSpawning();
    currentActiveWaveDef = null;
    SetInternalState(InternalWaveState.Idle);
    
    // Force transition to planning phase
    if (RunManager.Instance != null) {
        RunManager.Instance.StartNewPlanningPhase();
    }
    
    // Ensure turn phase manager is in planning
    if (TurnPhaseManager.Instance != null && 
        TurnPhaseManager.Instance.CurrentPhase != TurnPhase.Planning) {
        TurnPhaseManager.Instance.TransitionToPhase(TurnPhase.Planning);
    }
}
```

#### 4.3.2 Update RunManager State Transitions
```csharp
// In RunManager.cs, modify StartNewRound:
void StartNewRound() {
    currentRoundNumber++;
    Debug.Log($"[RunManager] Starting new round: {currentRoundNumber}");
    
    // Reset wave manager
    waveManager?.ResetForNewRound();
    
    // Ensure we're in planning state
    SetState(RunState.Planning);
    
    // Force turn phase to planning
    if (useWegoSystem && TurnPhaseManager.Instance != null) {
        TurnPhaseManager.Instance.TransitionToPhase(TurnPhase.Planning);
    }
    
    OnRoundChanged?.Invoke(currentRoundNumber);
}
```

### 4.4 Rework SlowdownZone to Tick-Based

#### 4.4.1 Update SlowdownZone Component
```csharp
// SlowdownZone is already set up for tick costs, but we need to ensure it's used
// The implementation in the scripts looks correct, just needs testing
```

#### 4.4.2 Verify GetMovementTickCost in PlayerActionManager
```csharp
// This is already implemented correctly in PlayerActionManager.GetMovementTickCost()
// Just ensure SlowdownZones are properly placed in the scene
```

---

## Phase 5: System Integration & Polish

### 5.1 Action Validation System

#### 5.1.1 Enhance ActionValidator
```csharp
// Create new file: ActionValidator.cs
public static class ActionValidator {
    public static bool CanMove(GridPosition from, GridPosition to) {
        // Check distance (must be adjacent)
        if (from.ManhattanDistance(to) != 1) return false;
        
        // Check bounds
        if (!GridPositionManager.Instance.IsPositionValid(to)) return false;
        
        // Check occupancy
        if (GridPositionManager.Instance.IsPositionOccupied(to)) return false;
        
        return true;
    }
    
    public static bool CanUseTool(ToolDefinition tool, Vector3Int target) {
        if (tool == null) return false;
        
        // Check if tool has uses remaining
        if (tool.limitedUses && ToolSwitcher.Instance != null) {
            if (ToolSwitcher.Instance.CurrentRemainingUses <= 0) return false;
        }
        
        // Check range (if player reference available)
        if (TileInteractionManager.Instance != null) {
            var player = TileInteractionManager.Instance.player;
            if (player != null) {
                float distance = Vector2.Distance(
                    player.position, 
                    TileInteractionManager.Instance.interactionGrid.GetCellCenterWorld(target)
                );
                if (distance > TileInteractionManager.Instance.hoverRadius) return false;
            }
        }
        
        return true;
    }
    
    public static bool CanPlantSeed(InventoryBarItem seedItem, Vector3Int target) {
        if (seedItem == null || !seedItem.IsSeed()) return false;
        
        // Check if position is occupied
        if (PlantPlacementManager.Instance?.IsPositionOccupied(target) ?? true) return false;
        
        // Check if tile is valid for planting
        var tileDef = TileInteractionManager.Instance?.FindWhichTileDefinitionAt(target);
        if (!PlantPlacementManager.Instance?.IsTileValidForPlanting(tileDef) ?? true) return false;
        
        return true;
    }
}
```

### 5.2 Turn Order Resolution

#### 5.2.1 Create TurnOrderManager
```csharp
// Create new file: TurnOrderManager.cs
public class TurnOrderManager : MonoBehaviour {
    public static TurnOrderManager Instance { get; private set; }
    
    [System.Serializable]
    public enum EntityType {
        Player = 0,
        Animal = 1,
        Plant = 2,
        Weather = 3,
        Effect = 4,
        System = 5
    }
    
    [SerializeField] private List<EntityType> executionOrder = new List<EntityType> {
        EntityType.Player,
        EntityType.Animal,
        EntityType.Plant,
        EntityType.Weather,
        EntityType.Effect,
        EntityType.System
    };
    
    private Dictionary<EntityType, List<ITickUpdateable>> entityGroups;
    
    void Awake() {
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        entityGroups = new Dictionary<EntityType, List<ITickUpdateable>>();
        foreach (EntityType type in System.Enum.GetValues(typeof(EntityType))) {
            entityGroups[type] = new List<ITickUpdateable>();
        }
    }
    
    public void RegisterEntity(ITickUpdateable entity, EntityType type) {
        if (!entityGroups[type].Contains(entity)) {
            entityGroups[type].Add(entity);
        }
    }
    
    public void UnregisterEntity(ITickUpdateable entity, EntityType type) {
        entityGroups[type].Remove(entity);
    }
    
    public void ProcessTick(int currentTick) {
        foreach (var entityType in executionOrder) {
            var entities = new List<ITickUpdateable>(entityGroups[entityType]);
            foreach (var entity in entities) {
                try {
                    entity.OnTickUpdate(currentTick);
                } catch (System.Exception e) {
                    Debug.LogError($"[TurnOrderManager] Error processing {entityType}: {e.Message}");
                }
            }
        }
    }
}
```

### 5.3 Save System Adaptation

#### 5.3.1 Create Save Data Structure
```csharp
// Create new file: WegoSaveData.cs
[System.Serializable]
public class WegoSaveData {
    public int currentTick;
    public int currentRound;
    public RunState runState;
    public TurnPhase turnPhase;
    
    // Player data
    public GridPosition playerPosition;
    public List<GridPosition> playerQueuedMoves;
    
    // Entity positions
    public List<EntitySaveData> savedEntities;
    
    // Plant data
    public List<PlantSaveData> savedPlants;
    
    // Wave data
    public int currentWaveIndex;
    public int waveStartTick;
    
    [System.Serializable]
    public class EntitySaveData {
        public string prefabName;
        public GridPosition position;
        public string entityType;
        public string customData; // JSON serialized custom data
    }
    
    [System.Serializable]
    public class PlantSaveData {
        public GridPosition position;
        public string nodeGraphJson;
        public int currentGrowthStage;
        public float currentEnergy;
    }
}
```

### 5.4 UI Improvements

#### 5.4.1 Add Tick Cost Preview
```csharp
// In PlayerTileInteractor.cs or a new UITickCostPreview.cs:
public class UITickCostPreview : MonoBehaviour {
    [SerializeField] private GameObject costPreviewPrefab;
    [SerializeField] private TextMeshProUGUI costText;
    
    void Update() {
        if (TurnPhaseManager.Instance?.IsInPlanningPhase != true) {
            costPreviewPrefab?.SetActive(false);
            return;
        }
        
        // Get mouse grid position
        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector3Int gridPos = TileInteractionManager.Instance.WorldToCell(mouseWorld);
        
        // Calculate potential move cost
        var gardener = FindObjectOfType<GardenerController>();
        if (gardener != null) {
            GridPosition from = gardener.GetCurrentGridPosition();
            GridPosition to = new GridPosition(gridPos.x, gridPos.y);
            
            if (ActionValidator.CanMove(from, to)) {
                int cost = PlayerActionManager.Instance.GetMovementTickCost(
                    GridPositionManager.Instance.GridToWorld(to)
                );
                
                costPreviewPrefab?.SetActive(true);
                costPreviewPrefab.transform.position = GridPositionManager.Instance.GridToWorld(to);
                if (costText != null) costText.text = cost.ToString();
            } else {
                costPreviewPrefab?.SetActive(false);
            }
        }
    }
}
```

### 5.5 Testing Checklist

#### Core Functionality Tests:
1. **Player Movement**
   - [ ] Player can queue moves during planning phase
   - [ ] Moves execute during execution phase
   - [ ] Each move costs correct number of ticks
   - [ ] Movement blocked by obstacles
   - [ ] SlowdownZones add extra tick cost

2. **Wave System**
   - [ ] Waves last exactly `ticksPerWave` ticks
   - [ ] Planning phase shows between waves
   - [ ] Enemies spawn at correct times
   - [ ] Wave completion triggers properly

3. **Day/Night Cycle**
   - [ ] Full day lasts exactly `ticksPerDay` ticks
   - [ ] Transitions happen at correct percentages
   - [ ] Visual effects sync with tick-based time

4. **Turn Phases**
   - [ ] Planning phase allows action queuing
   - [ ] Execution phase processes all queued actions
   - [ ] Phase transitions are clear to player
   - [ ] No actions possible during execution

5. **Plant Growth**
   - [ ] Plants grow based on ticks not real time
   - [ ] Growth stages advance predictably
   - [ ] Energy accumulation is tick-based

6. **Animal Behavior**
   - [ ] Animals think every N ticks
   - [ ] Movement is grid-based
   - [ ] Hunger increases by ticks
   - [ ] No real-time behavior

---

## Implementation Priority Order

### CRITICAL - Week 1
1. **Fix Player Movement** (4.1) - Game is unplayable without this
2. **Configure Day/Wave Durations** (4.2) - Required for testing
3. **Fix Planning Phase Transitions** (4.3) - Core game loop

### HIGH - Week 2  
4. **Turn Order Resolution** (5.2) - Ensures predictable gameplay
5. **Action Validation** (5.1) - Prevents invalid states
6. **UI Tick Cost Preview** (5.4.1) - Critical for player understanding

### MEDIUM - Week 3
7. **SlowdownZone Verification** (4.4) - Already mostly implemented
8. **Save System** (5.3) - Can be added after core is stable
9. **Testing & Polish** (5.5) - Ongoing

### LOW - Week 4+
10. **Advanced UI Features** - Nice to have
11. **Additional Tick Mechanics** - Future content

---

## Common Pitfalls to Avoid

1. **Don't Mix Real-Time and Tick-Based**: Ensure ALL gameplay systems use ticks
2. **Clear the Queues**: Always clear action queues when phases change
3. **Validate Before Execute**: Check if actions are valid before processing
4. **Tick Cost Transparency**: Always show players how many ticks actions will cost
5. **Save State Carefully**: The tick-based state is more complex than real-time

## Debug Commands to Add

```csharp
// Add to UIManager or create DebugCommands.cs:
void Update() {
    #if UNITY_EDITOR || DEVELOPMENT_BUILD
    if (Input.GetKeyDown(KeyCode.F1)) {
        Debug.Log($"Current Tick: {TickManager.Instance.CurrentTick}");
        Debug.Log($"Current Phase: {TurnPhaseManager.Instance.CurrentPhase}");
        Debug.Log($"Player Queued Moves: {FindObjectOfType<GardenerController>()?.GetQueuedMoveCount() ?? 0}");
    }
    
    if (Input.GetKeyDown(KeyCode.F2)) {
        TickManager.Instance.AdvanceMultipleTicks(10);
    }
    
    if (Input.GetKeyDown(KeyCode.F3)) {
        TurnPhaseManager.Instance.ForcePhase(TurnPhase.Planning);
    }
    #endif
}
```

This guide should provide clear, actionable steps to complete the Wego system implementation. The most critical issue is fixing player movement, which should be addressed first before moving on to the other improvements.