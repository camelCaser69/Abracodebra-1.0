# Wego System Rework Guide
## From Real-Time to Simultaneous Turn-Based Gameplay

### Overview
This guide outlines the systematic conversion of the game from real-time to a "Wego" (We-go) turn-based system where all entities plan their moves, then execute simultaneously on each tick.

## Phase 1: Core Infrastructure (Foundation)

### 1.1 Create Global Tick Manager
**Goal**: Replace Time.deltaTime with a global tick system
- Create `TickManager` singleton
- Define configurable tick rates via ScriptableObject
- Implement tick advancement controls
- Add tick event broadcasting
- Support both `TickUpdate()` and `Update()` loops
- **Config**: TicksPerDay, TicksPerHour, PhaseTickDurations
- **Test**: Verify tick counter increments correctly

### 1.2 Create Turn Phase System
**Goal**: Establish planning vs execution phases
- Define turn phases: Planning → Execution → Resolution
- Implement phase transitions
- Add phase change events
- **Test**: Verify phase transitions work with manual controls

### 1.3 Grid-Based Position System
**Goal**: Establish discrete positions for all entities
- Create `GridPosition` struct/class
- Add grid position tracking to all moveable entities
- Implement world-to-grid conversion utilities
- **Test**: Display grid positions in debug UI

---

## Phase 2: Movement System Conversion

### 2.1 Player Movement Rework
**Goal**: Convert GardenerController to tile-based movement
- Replace Rigidbody2D.MovePosition with grid-based movement
- Create movement input queue during planning phase
- Execute movement on tick
- Add movement validation (collision detection)
- **Test**: Player moves one tile per tick

### 2.2 Animal Movement Rework  
**Goal**: Convert AnimalController to tile-based movement
- Replace continuous pathfinding with tile-based pathfinding
- Queue animal movements during planning
- Execute all animal movements simultaneously
- Handle collision resolution
- **Test**: Animals move on grid, avoid obstacles

### 2.3 Movement Animation Bridge
**Goal**: Smooth visual transitions between tiles
- Keep visual interpolation between grid positions
- Decouple logical position from visual position
- Add movement animation queuing
- **Test**: Smooth movement between tiles

---

## Phase 3: Time-Based System Conversions

### 3.1 Weather System Tick Conversion
**Goal**: Convert day/night cycles to tick-based
- Replace seconds with configurable ticks for cycle duration
- Define ticks per day/night phase in ScriptableObject
- Update transition logic
- Keep visual sun movement smooth (real-time interpolation)
- **Config**: DayTicks, NightTicks, TransitionTicks
- **Test**: Day/night cycles on tick count

### 3.2 Plant Growth Tick Conversion
**Goal**: Convert plant growth to tick-based
- Replace growth time with growth ticks
- Convert energy accumulation to per-tick
- Update growth stages to tick thresholds
- **Test**: Plants grow on tick intervals

### 3.3 Cooldown & Timer Conversions
**Goal**: Convert all timers to tick-based
- Inventory all timer usage (cooldowns, durations, etc.)
- Create tick-based timer utilities
- Convert each timer systematically
- **Test**: All cooldowns work on ticks

---

## Phase 4: Game State Management

### 4.1 Action Queue System
**Goal**: Queue all actions during planning phase
- Create universal action queue
- Define action types (move, plant, harvest, etc.)
- Implement action validation
- **Test**: Actions queue and execute properly

### 4.2 Simultaneous Resolution System
**Goal**: Resolve all actions at once
- Define resolution order (movement → actions → effects)
- Implement conflict resolution
- Handle edge cases (two entities moving to same tile)
- **Test**: Conflicts resolve predictably

### 4.3 State Rollback System (Optional)
**Goal**: Allow planning phase changes
- Implement state snapshot system
- Add rollback functionality
- Update UI to show planned vs actual state
- **Test**: Can change plans before execution

---

## Phase 5: Systems Integration

### 5.1 Wave/Spawning System
**Goal**: Convert spawning to tick-based
- Replace spawn timers with tick counts
- Update wave progression logic
- Align spawning with turn phases
- **Test**: Enemies spawn on correct ticks

### 5.2 Energy & Resource Systems
**Goal**: Convert resource generation to tick-based
- Update photosynthesis to per-tick calculation
- Convert firefly effects to tick-based (logic only)
- Update all resource accumulation
- Convert radius checks to tile patterns
- Keep firefly visual movement real-time
- **Test**: Resources accumulate correctly

### 5.3 Combat/Interaction Systems
**Goal**: Make interactions turn-based
- Queue combat actions during planning
- Resolve damage/effects simultaneously
- Update projectile systems for tick-based movement
- **Test**: Combat resolves properly

---

## Phase 6: UI/UX Adaptation

### 6.1 Turn Indicator UI
**Goal**: Show current phase and tick clearly
- Add turn phase indicator
- Show tick counter
- Add "End Turn" button for planning phase
- **Test**: UI clearly shows game state

### 6.2 Action Preview System
**Goal**: Show planned actions before execution
- Display movement paths
- Show action indicators
- Add confirmation UI
- **Test**: Players can preview their turn

### 6.3 Speed Controls
**Goal**: Control tick advancement speed
- Add pause/play for execution phase
- Add tick speed slider
- Implement auto-advance option
- **Test**: Game speed is controllable

---

## Phase 7: Polish & Edge Cases

### 7.1 Animation System Updates
**Goal**: Ensure animations work with tick system
- Adjust animation speeds for tick duration
- Handle interrupted animations
- Synchronize multiple animations
- **Test**: Animations look smooth

### 7.2 Effect System Updates
**Goal**: Convert visual effects to tick-aware
- Update particle systems
- Convert fade effects
- Handle continuous vs discrete effects
- **Test**: Effects display correctly

### 7.3 Save System Updates
**Goal**: Save tick-based state
- Update save format for tick data
- Store action queues if needed
- Handle mid-turn saves
- **Test**: Can save/load at any phase

---

## Phase 8: Real-time Visual Layer

### 8.1 Dual Update System
**Goal**: Separate tick-based logic from real-time visuals
- Implement `ITickUpdateable` interface for logic updates
- Keep `Update()` for visual-only systems
- Create `VisualUpdateManager` for coordinating
- **Test**: Idle animations play during execution phase

### 8.2 Environmental Animation System
**Goal**: Keep world feeling alive between ticks
- Idle animations (creatures breathing, swaying)
- God rays and lighting effects
- Particle systems (independent timing)
- Wind and weather effects
- **Test**: World feels alive even when "paused"

### 8.3 Radius Pattern Library
**Goal**: Standardize tile-based radius approximations
- Create `TilePattern` ScriptableObjects
- Define common patterns (Range1, Range2, Range3)
- Include Manhattan/Chebyshev/Euclidean variants
- Visual preview tool in editor
- **Test**: All radius-based systems use patterns

## Configuration System Example

### TickConfiguration ScriptableObject
```
[CreateAssetMenu(fileName = "TickConfig", menuName = "Game/Tick Configuration")]
public class TickConfiguration : ScriptableObject
{
    [Header("Base Time Units")]
    public int ticksPerRealSecond = 2;
    
    [Header("Game Time")]
    public int ticksPerGameHour = 10;
    public int hoursPerDay = 24;
    public int ticksPerDay => ticksPerGameHour * hoursPerDay;
    
    [Header("Day/Night Cycle")]
    public int dayPhaseTicks = 60;
    public int nightPhaseTicks = 40;
    public int transitionTicks = 10;
    
    [Header("Common Durations")]
    public int plantGrowthTicksPerStage = 5;
    public int animalHungerTickInterval = 3;
    public int waveSpawnDelayTicks = 20;
}
```

### TilePattern ScriptableObject
```
[CreateAssetMenu(fileName = "TilePattern", menuName = "Game/Tile Pattern")]
public class TilePattern : ScriptableObject
{
    public string patternName;
    public int radius;
    public PatternType type; // Manhattan, Chebyshev, Euclidean
    public Vector2Int[] affectedTiles; // Pre-calculated in editor
    
    // Editor tool to visualize pattern
    // Conversion helper: float radius → tile pattern
}
```

---

## Implementation Order & Dependencies

### Critical Path:
1. **TickManager** (everything depends on this)
2. **Grid Position System** (movement depends on this)
3. **Turn Phase System** (actions depend on this)
4. **Movement Conversion** (most visible change)
5. **Time-based Conversions** (systematic replacement)
6. **Action Queue** (enables true Wego gameplay)
7. **UI Updates** (makes it playable)
8. **Polish** (makes it good)

### Parallel Work Possible:
- Weather conversion can happen alongside plant growth conversion
- UI work can begin once turn phases exist
- Animation updates can happen throughout

### Testing Strategy:
- Each phase should have isolated tests
- Create a test scene with minimal systems
- Add systems incrementally
- Maintain a "tick debug mode" throughout development

---

## Key Considerations

### What Stays Real-Time:
- Visual interpolations
- UI animations
- Sound effects
- Particle effects (mostly)

### What Becomes Tick-Based:
- All gameplay logic
- Resource generation
- Movement and pathfinding
- Growth and progression
- Combat and interactions

### Potential Challenges:
1. **Input Buffering**: Handling inputs during execution phase
2. **Visual Smoothness**: Making discrete movement look good
3. **AI Adaptation**: Making AI decisions feel intelligent in turn-based context
4. **Performance**: Updating many entities simultaneously
5. **Edge Cases**: Simultaneous actions creating conflicts

### Architecture Guidelines:
- Separate logical state from visual state
- Use events extensively for tick/phase changes
- Keep tick logic centralized
- Make systems tick-aware, not tick-dependent
- Plan for variable tick rates from the start