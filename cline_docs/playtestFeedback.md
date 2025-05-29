# Playtest Feedback Log

**Last Updated:** 2025-05-29  
**Build:** 1.0.0

## 🎮 Latest Session

**Focus:** Core Systems Integration Testing

### Key Observations
1. **Plant Growth** - Node genetics working, time-based growth natural, tile modifiers meaningful
2. **Animal AI** - Diet behavior engaging, thought bubbles add personality, population needs balancing
3. **Tools** - Q/E switching responsive, transformations clear, refills work but need feedback
4. **Node Editor** - Tab toggle convenient, drag-drop solid, can be overwhelming initially

## 📊 System Feedback

### Plant Systems
✅ **Working Well**
- NodeGraph → PlantGrowth pipeline robust
- Growth progression satisfying
- Shadow/outline effects enhance visibility
- Poop fertilizer leaf regrowth works

🔄 **Needs Improvement**
- Growth speed balancing across tiles
- Plant placement randomization confusing
- Large populations cause frame drops

⏳ **Planned**
- Cross-breeding genetics
- More node effect types
- Plant lifecycle stages

### Ecosystem
✅ **Working Well**
- Natural AI decision-making
- Scent system emergent behaviors
- Day/night behavior changes
- Wave spawning population control

🔄 **Needs Improvement**
- Pathfinding near screen edges
- Population balance between species
- Animal clustering issues

⏳ **Planned**
- Predator-prey relationships
- Seasonal migration
- Territorial behaviors

### Tools & Interaction
✅ **Working Well**
- Intuitive tool switching
- Logical transformation rules
- Usage limits prevent overuse
- Refill mechanics at water

🔄 **Needs Improvement**
- Visual feedback for effectiveness
- Range indicators needed
- Better remaining uses display

⏳ **Planned**
- Tool upgrade system
- Specialized tools
- Crafting mechanics

### Node Editor
✅ **Working Well**
- Organized grid layout
- Natural drag-and-drop
- Comprehensive configuration
- Auto-numbered creation

🔄 **Needs Improvement**
- Learning curve steep
- Effect preview needed
- Tooltip clarity

⏳ **Planned**
- Node templates
- Visual effect preview
- Validation warnings

## 🎯 Performance Metrics

### Current Results
- **60 FPS:** ✅ 20 animals, 30 plants
- **45 FPS:** ⚠️ 50 animals, 50 plants  
- **Load Time:** ✅ ~2 seconds
- **Memory:** ✅ ~180MB
- **Draw Calls:** ✅ ~35

### Bottlenecks
1. Animal AI Update() loops scale poorly
2. Plant visual effects in LateUpdate()
3. Too many individual plant cell GameObjects
4. Real-time scent visualization

## 🐛 Known Issues

### High Priority
1. Plant cell destruction race conditions
2. Animal thought bubble memory leaks
3. Frame drops with 50+ entities

### Medium Priority
1. AI boundary pathfinding
2. Tool feedback missing
3. Node editor dropdown positioning

### Low Priority
1. Visual transition abruptness
2. No audio feedback
3. UI scaling issues

## 💡 Player Suggestions

### Most Requested
1. Save/load system for plant designs
2. Audio feedback for interactions
3. Tutorial for node editor

### Quality of Life
1. Direct tool hotkeys (1-9)
2. Plant inspection (click to see genetics)
3. Time speed controls

## 📈 Tracked Metrics

### Growth System
- Plants per session: 15-25
- Popular combinations: Seed + Leaf + Berry
- Completion rate: 85%

### Animal System  
- Animals per area: 8-12
- Population stability good with waves
- High interaction near food sources

### Player Engagement
- Session duration: 15-30 minutes
- Tool usage: Hoe 60%, WateringCan 25%, SeedPouch 15%
- Node editor discovery: 100%, advanced effects 40%

## 🔄 Iteration Goals

### Short Term (2 weeks)
1. Fix race conditions and memory leaks
2. Optimize Update() loops
3. Basic audio feedback
4. Tool effectiveness indicators

### Medium Term (1 month)
1. Save/load implementation
2. Tutorial system
3. 100+ entity optimization
4. New node effects

### Long Term (3 months)
1. Advanced gameplay features
2. Content expansion
3. Polish phase
4. Sharing features

---

**Next Update:** After major playtest sessions