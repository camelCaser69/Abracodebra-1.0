# 05_Playtest_Feedback_And_Known_Issues.md

**Synthesized:** 2025-05-31
**Project:** Gene Garden Survivor
**Basis:** Playtest Build 1.0.0 (Pre-Sprint 0, Sandbox)

Logs feedback from earlier sandbox playtests and known issues. Will be updated post-Sprint 0.

## 🎮 Playtest Summary (Build 1.0.0 Sandbox)

*   **Plant Growth:** Node genetics functional, growth natural, tile modifiers impactful.
*   **Animal AI:** Diet behavior engaging, thoughts added personality. Population balancing needed.
*   **Tools:** Switching responsive, transformations clear. Refills worked but lacked feedback.
*   **Node Editor:** Tab toggle good, drag-drop solid. Potentially overwhelming initially.

## 📊 System Feedback (Build 1.0.0 Sandbox)

### Plant Systems
*   👍 NodeGraph→PlantGrowth robust. Visual growth satisfying. Shadow/outline good. Poop fertilizer worked.
*   🔄 Balance growth speeds. Plant placement randomization confusing. Frame drops with large populations.
*   ⏳ **Sprint 0 Impact:** "Leaf = Life" & combat genes will alter plant dynamics.

### Ecosystem (Animals, Scents, Fireflies)
*   👍 Animal food seeking natural. Scent system emergent. Day/night activity changes correct. Wave spawning controlled population.
*   🔄 Animal pathfinding near bounds (clustering/stuck). Species population balance.
*   ⏳ **Sprint 0 Impact:** Threats are now explicit waves. Animal AI needs plant/leaf targeting.

### Tools & Tile Interaction
*   👍 Tool switching intuitive. Transformations logical. Usage limits/refills functional.
*   🔄 Lack of immediate visual/audio feedback for tool use/failure. No range indicators. Better remaining uses UI.
*   ⏳ **Sprint 0 Impact:** Player class special tools new interaction layer.

### Node Editor UI
*   👍 Grid layout organized. Drag-drop robust. Comprehensive config. Auto-numbering helpful.
*   🔄 Steep learning curve. Effect/synergy preview needed. Tooltip clarity. Dropdown usability on small screens.
*   ⏳ **Sprint 0 Impact:** Node Editor central to Planning phase; usability critical.

## 🎯 Performance (Build 1.0.0 Sandbox)
*   **60 FPS:** ~20 animals, ~30 plants.
*   **~45 FPS:** ~50 animals, ~50 plants.
*   **Load Time:** ~2s. **Memory (Editor):** ~180MB. **Draw Calls:** ~35.

**Bottlenecks (Pre-Optimization):**
1.  `AnimalController.Update()` loops.
2.  Plant visual effects `LateUpdate()` (shadows/outlines).
3.  Many `PlantCell` GameObjects.
4.  Real-time scent/firefly line visualizations (if many enabled).

## 🐛 Known Issues (Pre-Sprint 0)
### High Priority
1.  Plant cell destruction race conditions.
2.  Animal thought bubble memory leaks (verify).
3.  Frame drops >50 entities.

### Medium Priority
1.  AI boundary pathfinding issues.
2.  Missing tool use feedback.
3.  Node editor dropdown positioning.

### Low Priority
1.  Abrupt visual transitions.
2.  Lack of audio feedback.
3.  Minor UI scaling issues.
4.  Plant placement imprecision (randomization).

## 💡 Player Suggestions (Sandbox)
*   **Most Requested:** Save/load gene designs. Audio feedback. Node editor tutorial.
*   **QoL:** Direct tool hotkeys. Plant inspection UI. Game speed controls (for sandbox mode).

## 🔄 Iteration Goals (Sprint 0 Focus & Beyond)
### Short Term (Sprint 0-1)
1.  Implement `RunManager` & new game loop.
2.  Fix critical race conditions/leaks.
3.  Profile/optimize `AnimalController.Update()`, plant VFX.
4.  Basic UI feedback for new phases.
5.  Adapt `WaveManager`/`WeatherManager` for `RunManager` control.

### Medium Term (Post Sprint 1-2)
*   Initial audio. Better tool indicators. Refine AI boundary behavior. Optimize for 100+ entities.

### Long Term
*   Save/load gene designs. Interactive tutorial. Visual polish.