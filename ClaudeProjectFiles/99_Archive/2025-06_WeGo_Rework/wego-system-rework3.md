# Wego System Completion Guide
## From Time-Based to Player-Action-Driven Turn-Based Gameplay

### Overview
This guide completes the Wego system implementation, transforming it into a true player-action-driven turn-based game where time only advances when the player acts (similar to Stoneshard).

---

## Phase 1: Player-Driven Tick System (CRITICAL)

### 1.1 Remove Auto-Advance from TickManager
**Goal**: Ticks only advance when player takes action, never automatically
- Modify `TickManager.Update()` to remove all time-based tick accumulation
- Remove `tickAccumulator`, `autoAdvanceTicks`, and time-based logic
- Keep manual `AdvanceTick()` method
- **Implementation Approach**:
  ```csharp
  // In TickManager.cs Update():
  void Update() {
      // REMOVE all tickAccumulator logic
      // Keep only debug key handling if needed
  }
  ```
- **Test**: Verify time stands still without player input

### 1.2 Create PlayerActionManager
**Goal**: Centralize all player actions that trigger ticks
- Create new `PlayerActionManager` singleton
- Define action types: Move, Plant, Water, Harvest, UseTool
- Each successful action calls `TickManager.Instance.AdvanceTick()`
- **Implementation Structure**:
  ```csharp
  public class PlayerActionManager : MonoBehaviour {
      public static PlayerActionManager Instance;
      
      public void ExecutePlayerMove(GridPosition from, GridPosition to) {
          // Validate move
          // Execute move
          // Trigger tick ONLY if move successful
          if (moveSuccessful) {
              TickManager.Instance.AdvanceTick();
          }
      }
      
      public void ExecutePlayerAction(PlayerActionType type, GridPosition target) {
          // Similar pattern for all actions
      }
  }
  ```
- **Test**: Each player action advances exactly 1 tick

### 1.3 Modify GardenerController Input System
**Goal**: Route all player inputs through PlayerActionManager
- Replace direct movement with action requests
- Remove continuous movement in `Update()`
- Convert to discrete grid-based actions
- **Key Changes**:
  ```csharp
  // In HandleWegoInput():
  if (input.sqrMagnitude > 0.01f) {
      GridPosition targetPos = // calculate target
      PlayerActionManager.Instance.ExecutePlayerMove(currentPos, targetPos);
      // Remove direct movement logic
  }
  ```
- **Test**: Player can only move one tile per input

### 1.4 Integrate Tool/Seed Actions
**Goal**: Make all interactions tick-advancing
- Modify `PlayerTileInteractor.HandleLeftClick()`
- Route through PlayerActionManager
- **Implementation**:
  ```csharp
  // Instead of direct tool application:
  PlayerActionManager.Instance.ExecutePlayerAction(
      PlayerActionType.UseTool, 
      cellPos,
      selected.ToolDefinition
  );
  ```
- **Test**: Using tools advances tick

---

## Phase 2: Complete Grid Movement System

### 2.1 Force Grid Snapping on Start
**Goal**: All entities start on valid grid positions
- Add to all entity `Start()` methods:
  ```csharp
  void Start() {
      // Snap to nearest grid position
      GridPosition nearestGrid = GridPositionManager.Instance.WorldToGrid(transform.position);
      transform.position = GridPositionManager.Instance.GridToWorld(nearestGrid);
      
      // Initialize GridEntity if needed
      if (gridEntity == null) {
          gridEntity = GetComponent<GridEntity>() ?? gameObject.AddComponent<GridEntity>();
      }
      gridEntity.SetPosition(nearestGrid, instant: true);
  }
  ```
- Apply to: GardenerController, AnimalController, all spawned entities
- **Test**: All entities appear exactly on grid tiles

### 2.2 Remove Real-Time Movement
**Goal**: Eliminate all continuous movement code
- In `GardenerController`:
  - Remove `FixedUpdate()` entirely
  - Remove `Rigidbody2D` references
  - Remove `movement` vector and real-time logic
- In `AnimalController`:
  - Remove real-time wander/seek logic
  - Convert to pure grid-based decisions
- **Test**: No entity moves between grid positions

### 2.3 Standardize Grid-Based Pathfinding
**Goal**: All movement uses grid paths
- Implement simple A* or Manhattan pathfinding in GridPositionManager
- Animals plan paths during their thinking phase
- **Implementation Hint**:
  ```csharp
  public List<GridPosition> GetPath(GridPosition start, GridPosition end) {
      // Use Manhattan distance
      // Check occupancy for each step
      // Return list of positions to move through
  }
  ```
- **Test**: Animals navigate around obstacles

### 2.4 Visual Movement Interpolation
**Goal**: Smooth visual transitions without affecting logic
- Keep visual interpolation in GridEntity
- Ensure it's purely cosmetic
- **Critical**: Visual position != logical position
- **Test**: Entities slide smoothly between tiles

---

## Phase 3: System Cleanup & Removal

### 3.1 Remove Speed-Based Systems
**Goal**: Eliminate all speed modifiers and related code
- Delete `SpeedModifiable` base class
- Remove speed inheritance from GardenerController, AnimalController
- Remove `baseSpeed`, `currentSpeed`, speed multipliers
- Delete speed-related UI if any
- **Test**: No speed references remain

### 3.2 Simplify Turn Phase System
**Goal**: Remove unnecessary complexity
- Consider removing TurnPhaseManager entirely OR
- Simplify to just Planning/Execution (no Resolution)
- Player actions directly trigger execution
- **Suggested Approach**:
  ```csharp
  // Simplified flow:
  // 1. Player acts
  // 2. Tick advances
  // 3. All entities execute their planned actions
  // 4. Back to waiting for player
  ```

### 3.3 Remove Time-Based Weather
**Goal**: Weather changes on tick count only
- Remove all `Time.deltaTime` usage in WeatherManager
- Base transitions purely on tick thresholds
- **Test**: Day/night cycles advance by player actions

### 3.4 Cleanup Obsolete Scripts
**Goal**: Remove scripts that no longer serve purpose
- Consider removing:
  - RunManager (if turn phases simplified)
  - Real-time animation controllers
  - Continuous effect systems
- **Test**: Game runs without removed scripts

---

## Phase 4: Mechanic Reworks

### 4.1 Rework SlowdownZone to Tick-Skip
**Goal**: Zones cause tick penalties instead of speed reduction
- Replace speed multiplier with tick cost
- **New Implementation**:
  ```csharp
  public class TickCostZone : MonoBehaviour {
      public int additionalTickCost = 1; // Moving here costs 2 ticks instead of 1
      
      void OnTriggerEnter2D(Collider2D other) {
          if (other.TryGetComponent<GridEntity>(out var entity)) {
              // Mark entity for additional tick consumption
              entity.NextMoveCost = 1 + additionalTickCost;
          }
      }
  }
  ```
- **Test**: Water tiles cost 2 ticks to cross

### 4.2 Convert Growth to Pure Tick-Based
**Goal**: Remove all growth timing complexities
- Plants grow X ticks per stage, period
- No interpolation needed
- Energy accumulates per tick
- **Simplification**:
  ```csharp
  void OnTickUpdate(int tick) {
      ticksSinceLastGrowth++;
      if (ticksSinceLastGrowth >= ticksPerGrowthStage) {
          GrowNextStage();
          ticksSinceLastGrowth = 0;
      }
  }
  ```
- **Test**: Plants grow predictably

### 4.3 Simplify Animal Behavior
**Goal**: Animals act predictably on ticks
- Remove complex timing
- Think every N ticks
- Execute one action per tick
- **Pattern**:
  ```csharp
  void OnTickUpdate(int tick) {
      if (tick % thinkInterval == 0) PlanNextAction();
      if (hasPlannedAction) ExecuteAction();
  }
  ```
- **Test**: Animals behave predictably

### 4.4 Wave Spawning on Player Actions
**Goal**: Waves progress by player action count
- Count player actions instead of time
- Spawn enemies after N player actions
- **Test**: Enemies appear after specific action counts

---

## Phase 5: Critical Implementation Details

### 5.1 Action Validation System
**Goal**: Ensure only valid actions advance time
```csharp
public class ActionValidator {
    public bool CanMove(GridPosition from, GridPosition to) {
        // Check distance (must be adjacent)
        // Check occupancy
        // Check bounds
        return isValid;
    }
    
    public bool CanUseTool(ToolDefinition tool, GridPosition target) {
        // Check tool has uses
        // Check target is valid
        // Check range
        return isValid;
    }
}
```

### 5.2 Turn Order Resolution
**Goal**: Define clear execution order when tick advances
```csharp
void ProcessTick() {
    // 1. Player action completes
    // 2. Animals move
    // 3. Plants grow
    // 4. Weather updates
    // 5. Effects process
    // 6. Check win/lose conditions
}
```

### 5.3 Save System Adaptation
**Goal**: Save tick-based state
- Save current tick number
- Save all grid positions
- Save planned actions
- Remove time-based data

---

## Phase 6: Testing Strategy

### 6.1 Core Functionality Tests
1. **Tick Advancement**: Only player actions advance ticks
2. **Grid Movement**: All entities stay on grid
3. **Action Cost**: Different actions cost correct ticks
4. **No Time Pressure**: Game waits indefinitely for input

### 6.2 Integration Tests
1. **Full Game Loop**: Plant seed → wait → water → wait → harvest
2. **Combat**: Enemy spawns → player moves → enemy moves
3. **Resource Management**: Energy/water consumed per action

---

## Phase 7: Polish & Optional Enhancements

### 7.1 UI Improvements
- Show tick cost preview before actions
- Display "Thinking..." indicators during execution
- Action queue visualization

### 7.2 Advanced Tick Mechanics
- Multi-tick actions (channeled abilities)
- Tick cost modifiers (tired = 2 ticks per move)
- Bonus actions (doesn't consume tick)

### 7.3 Tutorial Integration
- Explain tick-based mechanics
- Show how time only moves with actions
- Emphasize strategic planning

---

## Implementation Priority Order

1. **CRITICAL - Do First**
   - Player-driven tick system (Phase 1)
   - Grid snapping (Phase 2.1)
   - Remove real-time movement (Phase 2.2)

2. **HIGH - Core Functionality**
   - Complete grid movement (Phase 2.3-2.4)
   - Remove speed systems (Phase 3.1)
   - Rework slowdown zones (Phase 4.1)

3. **MEDIUM - Cleanup**
   - Simplify systems (Phase 3.2-3.4)
   - Convert mechanics (Phase 4.2-4.4)

4. **LOW - Polish**
   - UI improvements
   - Advanced mechanics
   - Tutorial

---

## Key Success Metrics

- ✓ Time never advances without player input
- ✓ All movement is grid-based
- ✓ No references to speed or deltaTime in gameplay
- ✓ Clear tick cost for all actions
- ✓ Predictable, strategic gameplay