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

