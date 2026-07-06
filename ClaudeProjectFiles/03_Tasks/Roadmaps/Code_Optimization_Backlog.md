# Unity Project Code Optimization Guide

## Overview
This document outlines recommended code optimizations for your Unity project, organized by priority and implementation difficulty. Each task is designed to be completed and tested independently.

---

## MANDATORY OPTIMIZATIONS (Easy Wins)

### 1. Remove Unused Combat System Scripts
**Difficulty:** ⭐ (Very Easy)
**Impact:** Reduces project clutter and compilation time

**Files to Delete:**
- `Assets/Scripts/UI/Combat/StatusEffect.cs`
- `Assets/Scripts/UI/Combat/BurningStatusEffect.cs`
- `Assets/Scripts/UI/Combat/SpellProjectile.cs`

**Steps:**
1. Search entire project for references to `StatusEffect`, `BurningStatusEffect`, `SpellProjectile`
2. Confirm no references exist
3. Delete the three files
4. **Test:** Ensure project compiles without errors

---

### 2. Remove Unused UI Scripts
**Difficulty:** ⭐ (Very Easy)
**Impact:** Reduces project clutter

**Files to Delete:**
- `Assets/Scripts/PlantSystem/UI/NodeSelectable.cs` (replaced by NodeCell selection)
- `Assets/Scripts/PlantSystem/UI/DeselectOnClickOutside.cs` (no references found)

**Steps:**
1. Verify no GameObject in scenes has these components attached
2. Delete both files
3. **Test:** Ensure node selection still works in the plant editor

---

### 3. Consolidate Speed Multiplier Logic
**Difficulty:** ⭐⭐ (Easy)
**Impact:** Reduces code duplication, easier maintenance

Create a shared component for speed modification:

**New File:** `Assets/Scripts/Core/SpeedModifiable.cs`
```csharp
using UnityEngine;
using System.Collections.Generic;

public class SpeedModifiable : MonoBehaviour {
    [SerializeField] protected float baseSpeed = 5f;
    [SerializeField] protected float currentSpeed;
    
    List<float> activeSpeedMultipliers = new List<float>();
    
    protected virtual void Awake() {
        currentSpeed = baseSpeed;
    }
    
    public void ApplySpeedMultiplier(float multiplier) {
        if (!activeSpeedMultipliers.Contains(multiplier)) {
            activeSpeedMultipliers.Add(multiplier);
            UpdateSpeed();
        }
    }
    
    public void RemoveSpeedMultiplier(float multiplier) {
        if (activeSpeedMultipliers.Remove(multiplier)) {
            UpdateSpeed();
        }
    }
    
    void UpdateSpeed() {
        float lowestMultiplier = 1.0f;
        foreach (float mult in activeSpeedMultipliers) {
            if (mult < lowestMultiplier) lowestMultiplier = mult;
        }
        currentSpeed = baseSpeed * lowestMultiplier;
        OnSpeedChanged(currentSpeed);
    }
    
    protected virtual void OnSpeedChanged(float newSpeed) { }
}
```

**Update AnimalController.cs:**
- Inherit from SpeedModifiable
- Remove duplicate speed multiplier code
- Override OnSpeedChanged to update movement speed

**Update GardenerController.cs:**
- Inherit from SpeedModifiable
- Remove duplicate speed multiplier code
- Use currentSpeed directly in FixedUpdate

**Test:** Verify slowdown zones still work for both animals and player

---

### 4. Optimize Debug.Log Calls
**Difficulty:** ⭐⭐ (Easy)
**Impact:** Better performance in builds

**Files to Update:** All scripts with Debug.Log calls

**Pattern to Apply:**
```csharp
// Replace:
Debug.Log("message");

// With:
if (Debug.isDebugBuild) Debug.Log("message");
```

**Priority Files:**
- `PlantGrowth.cs` and its partials
- `FaunaManager.cs`
- `AnimalController.cs`

**Test:** Ensure debug messages appear in editor but not in builds

---

### 5. Cache Component References
**Difficulty:** ⭐⭐ (Easy)  
**Impact:** Better performance in Update loops

**Files to Update:**
- `ShadowPartController.cs` - Cache transform references
- `OutlinePartController.cs` - Cache transform references
- `WaterReflection.cs` - Cache frequently accessed components

**Example Pattern:**
```csharp
// Add to class:
Transform cachedTransform;
SpriteRenderer cachedRenderer;

// In Awake/Start:
cachedTransform = transform;
cachedRenderer = GetComponent<SpriteRenderer>();

// Use cached references instead of properties
```

**Test:** Verify shadows, outlines, and reflections still work correctly

---

### 6. Remove Empty Methods
**Difficulty:** ⭐ (Very Easy)
**Impact:** Cleaner code

**Methods to Remove:**
- Empty `Awake()` methods that only exist without any logic
- Empty `Start()` methods
- Unused event handler stubs

**Test:** Ensure project compiles

---

### 7. Simplify NodeData Serialization
**Difficulty:** ⭐⭐⭐ (Medium)
**Impact:** Simpler code, easier to maintain

**Current Issue:** Complex recursive CleanForSerialization with depth limiting

**Suggested Approach:**
```csharp
public void OnBeforeSerialize() {
    if (_isContainedInSequence || !IsPotentialSeedContainer()) {
        _storedSequence = null;
    }
}

public void OnAfterDeserialize() {
    _isContainedInSequence = false;
    if (!IsPotentialSeedContainer()) {
        _storedSequence = null;
    }
}
```

**Test:** Ensure seed storage/loading still works in inventory and editor

---

## OPTIONAL OPTIMIZATIONS

### 8. Consolidate Visual Update Patterns
**Difficulty:** ⭐⭐⭐ (Medium)
**Impact:** DRY principle, easier maintenance

Create a base class for visual synchronization:
```csharp
public abstract class VisualSynchronizer : MonoBehaviour {
    protected abstract void SyncVisuals();
    protected abstract bool ShouldUpdate();
    
    protected virtual void LateUpdate() {
        if (ShouldUpdate()) SyncVisuals();
    }
}
```

Apply to: `ShadowPartController`, `OutlinePartController`, `WaterReflection`

---

### 9. Optimize FloraManager Circle Updates
**Difficulty:** ⭐⭐ (Easy)
**Impact:** Better performance with many plants

Replace `FindObjectsByType` in Update with cached plant list:
- Maintain a static list of active PlantGrowth instances
- Register/unregister in Awake/OnDestroy
- Iterate cached list instead of finding objects

---

### 10. Simplify WaterReflection Override System
**Difficulty:** ⭐⭐⭐ (Medium)
**Impact:** Simpler code, easier to understand

Current system is over-engineered. Consider:
- Remove OverrideSettings class
- Use simple bool flags for each override
- Inline ResolveSettings logic

---

### 11. Create Constants for Magic Numbers
**Difficulty:** ⭐ (Very Easy)
**Impact:** Better maintainability

Examples:
- `0.01f` threshold checks → `const float EPSILON = 0.01f`
- Animation state names → string constants
- Layer names → cached layer IDs

---

### 12. Consolidate Tooltip Logic
**Difficulty:** ⭐⭐ (Easy)
**Impact:** Less code duplication

- Merge similar BuildNodeDetails/BuildToolDetails patterns
- Create common interface for tooltip data
- Simplify TooltipTrigger detection logic

---

### 13. Use Object Pooling for Frequent Instantiations
**Difficulty:** ⭐⭐⭐⭐ (Hard)
**Impact:** Better performance, less GC

Consider for:
- Fireflies
- Animal spawning
- UI elements (thought bubbles)
- Plant cells

---

## Testing Checklist After Each Change

1. **Compilation:** Project compiles without errors
2. **Play Mode:** Can enter play mode without exceptions
3. **Core Features:**
   - [ ] Plant growth system works
   - [ ] Node editor functions properly
   - [ ] Animals spawn and behave correctly
   - [ ] Inventory system works
   - [ ] Visual effects (shadows, outlines) display correctly
   - [ ] Day/night cycle functions
   - [ ] Tile interactions work

## Implementation Order

1. Start with file deletions (Tasks 1-2)
2. Implement simple patterns (Tasks 3-6)
3. Test thoroughly
4. Move to medium difficulty tasks if desired
5. Consider optional optimizations based on performance needs

## Notes

- Each task is independent - you can skip any that seem risky
- Always backup before making changes
- Use version control to track changes
- Test in both editor and builds
- Profile before/after for performance-critical changes