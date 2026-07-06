# Unity Wego Game Foundation Improvement Guide

## Overview
This guide outlines critical improvements needed to solidify the project's foundation before expanding content. The focus is on completing the Wego system integration, optimizing for grid-based gameplay, and ensuring clean, scalable architecture.

---

## Priority 1: Complete Wego System Integration

### 1.1 Animal Movement System Cleanup
**Problem**: AnimalController still contains remnants of realtime movement code mixed with Wego logic.

**Tasks**:
- Remove all realtime movement code from `HandleRealtimeUpdate()` method
- Consolidate all movement logic into `OnTickUpdate()` 
- Remove unused realtime variables:
  ```csharp
  // TO REMOVE:
  float wanderStateTimer = 0f;
  float eatTimer = 0f;
  float poopTimer = 0f;
  float foodReassessmentTimer = 0f;
  ```
- Convert time-based durations to tick counts in AnimalDefinition
- Move `useWegoMovement` check to initialization only

### 1.2 Firefly Wego Integration
**Problem**: Fireflies are completely realtime, breaking consistency with other entities.

**Solution Approach**:
```
Grid Movement (Tick-based):
┌─────┬─────┬─────┐
│     │  🦟→│     │  Firefly moves between tiles on ticks
├─────┼─────┼─────┤
│     │     │     │  
└─────┴─────┴─────┘

Local Movement (Realtime):
┌─────────────────┐
│   ↗️   ↘️   ↙️   │  Within tile: smooth circular motion
│  ↙️   🦟   ↗️   │  Radius: 0.4 * tileSize
│   ↘️   ↗️   ↘️   │
└─────────────────┘
```

**Implementation Steps**:
1. Add GridEntity component to fireflies
2. Create `FireflyTickController` that decides tile movements
3. Modify `FireflyController` to:
   - Move between tiles on ticks
   - Perform local circular movement within current tile bounds
   - Respect tile boundaries for visual movement
4. Add tick-based spawn intervals to FireflyManager

---

## Priority 2: Grid-Aligned Radius Systems

### 2.1 Implement Discrete Grid Radius Calculations
**Problem**: Circular radius checks don't align with square grid tiles.

**Solution**: Implement Manhattan/Chebyshev distance for all radius checks:

```
Manhattan Distance (Diamond):     Chebyshev Distance (Square):
    2                                 2 2 2
  2 1 2                             2 1 1 2
2 1 0 1 2                         2 1 0 1 2
  2 1 2                             2 1 1 2
    2                                 2 2 2
```

**Affected Systems**:
- Animal food detection (`searchRadius`)
- Plant poop detection (`PoopDetectionRadius`)
- Firefly photosynthesis bonus
- Scent effect ranges
- Tool usage ranges

**Implementation**:
```csharp
public static List<GridPosition> GetTilesInRadius(GridPosition center, int radius, RadiusType type) {
    // type: Manhattan, Chebyshev, or Euclidean (rounded)
}
```

### 2.2 Update Debug Visualizations
- Replace `Gizmos.DrawWireSphere()` with grid-aligned visualizations
- Show actual affected tiles as highlighted squares
- Different colors for different radius types

---

## Priority 3: ScriptableObject Migration

### 3.1 Animal Property Migration
**Move from AnimalController to AnimalDefinition**:
```csharp
// AnimalDefinition.cs additions:
[Header("Movement")]
public int thinkingTickInterval = 3;
public float searchRadius = 5f;
public float eatDistance = 0.5f;
public int eatDurationTicks = 3;
public float wanderPauseChance = 0.3f;

[Header("Biological Needs")]
public int minPoopDelayTicks = 10;
public int maxPoopDelayTicks = 20;
public float poopColorVariation = 0.1f;

[Header("Behavior")]
public int thoughtCooldownTicks = 10;
public float starvationDamagePerTick = 1f;
public int deathFadeTicks = 3;
```

### 3.2 Create Species-Specific Prefab Variants
- Base AnimalController becomes truly generic
- Each species gets preset values in their AnimalDefinition
- Controller only handles execution logic

---

## Priority 4: Code Organization

### 4.1 Split Multi-Class Files
**Files to split**:
- `NodeData.cs` → Extract `InitialNodeConfig` class
- `WaveDefinition.cs` → Extract `WaveSpawnEntry` class
- `AnimalDiet.cs` → Extract `DietPreferenceSimplified` class
- `TileInteractionLibrary.cs` → Extract `ToolRefillRule` class

### 4.2 Namespace Organization
```
WegoSystem/
├── Core/           (TickManager, GridEntity, etc.)
├── Plants/         (All plant-related classes)
├── Animals/        (All animal-related classes)
├── Environment/    (Weather, Tiles, etc.)
└── UI/            (All UI controllers)
```

---

## Priority 5: Manager Consolidation

### 5.1 Create GameSettingsManager
**Consolidate scattered settings**:
```csharp
public class GameSettingsManager : MonoBehaviour {
    [Header("Debug Settings")]
    public bool globalDebugMode;
    public bool showAllGizmos;
    
    [Header("Gameplay Settings")]
    public TickConfiguration tickConfig;
    public float globalSpeedMultiplier;
    
    [Header("Visual Settings")]
    public bool enableShadows;
    public bool enableOutlines;
    // etc...
}
```

### 5.2 Unify Spawn/Placement Systems
- Merge overlapping functionality between PlantPlacementManager and NodeExecutor
- Single source of truth for "can place here?" logic

---

## Priority 6: Value Normalization for Ticks

### 6.1 Gene Effect Value Audit
**Current Issues**:
- Some effects use seconds, others use ticks
- Growth rates inconsistent with tick timing
- Energy costs not balanced for tick-based play

**Standardization**:
```csharp
// All time values in TICKS:
CooldownTicks (not CooldownSeconds)
GrowthTicksPerStage (not GrowthSpeed as float)
CastDelayTicks (not CastDelay)

// All rates as "per tick":
EnergyPerTick (not EnergyPhotosynthesis)
HungerPerTick (not hungerIncreaseRate)
```

### 6.2 Create Debug Tick Monitor
- Show current tick effects in real-time
- Validate all tick-based calculations
- Help balance gameplay values

---

## Priority 7: Performance Optimizations

### 7.1 Pooling Systems
**Implement object pools for**:
- Projectiles (plant outputs)
- Thought bubbles
- Fireflies
- Visual effects

### 7.2 Update Optimization
- Batch grid position updates
- Cache frequently accessed components
- Remove per-frame calculations in Wego mode

---

## Implementation Order

1. **Week 1**: Wego Integration (Priority 1)
   - Day 1-2: Animal movement cleanup
   - Day 3-5: Firefly system conversion

2. **Week 2**: Grid Systems (Priority 2)
   - Day 1-3: Radius calculation system
   - Day 4-5: Debug visualization updates

3. **Week 3**: Architecture (Priorities 3-4)
   - Day 1-2: ScriptableObject migration
   - Day 3-5: Code organization

4. **Week 4**: Polish (Priorities 5-7)
   - Day 1-2: Manager consolidation
   - Day 3-4: Value normalization
   - Day 5: Performance optimization

---

## Success Metrics

- [ ] No realtime movement code in Wego mode
- [ ] All radius checks use grid-aligned calculations
- [ ] One class per file
- [ ] All time values in ticks
- [ ] Centralized game settings
- [ ] <5ms tick processing time with 50+ entities

---

## Notes for Implementation

- Each task should be a separate Git commit
- Run play tests after each major change
- Document any API changes
- Update prefabs after ScriptableObject changes
- Consider creating migration tools for existing saves