# Abracodebra 2.0 - Project Roadmap

**Last Updated:** 2025-05-29  
**Unity Version:** 6 (6000.0.39f1)

## 🎯 Core Vision

2D pixel-art ecosystem rogue-like sandbox where **plants grow from node-graphs**, **animals think and interact**, and the environment responds dynamically. Players genetically engineer plants through visual programming and discover new genetics rogue-like style.

**Inspiration:** Noita + Stardew Valley

### Key Pillars
1. **Living Ecosystem** - Dynamic AI with diet, thoughts, emergent behaviors
2. **Procedural Genetics** - Node-graph plant development with environmental effects
3. **Environmental Interaction** - Dual-grid tiles with tool modifications
4. **Genetic Discovery** - Rogue-like progression through experimentation

## 🚀 Development Phases

### ✅ PHASE 1 - FOUNDATION (COMPLETED)

#### Core Systems
- **PlantGrowth** - Time-based growth with node execution, visual cells, shadows/outlines
- **Animal AI** - Diet-based seeking, thoughts, poop fertilizer, speed zones
- **Node Editor** - Grid UI with drag-drop, auto-numbering, effect configuration
- **Dual-Grid Tiles** - Wang tiles with tool interactions, growth modifiers
- **Weather System** - Day/night with time scaling, post-processing effects
- **Visual Effects** - Dynamic shadows/outlines, water reflections, firefly attraction

#### Tool & Interaction
- ToolSwitcher with limited uses and refill mechanics
- TileInteractionManager with transformation rules
- PlantPlacementManager with validation and randomization

### 🔄 PHASE 2 - INTEGRATION (IN PROGRESS)

#### Performance & Polish
- **Scaling Optimization** - 50+ animals, 100+ plants @ 60 FPS
- **Memory Management** - Fix race conditions, cleanup systems
- **User Feedback** - Audio integration, tool effectiveness indicators
- **Edge Cases** - AI boundary fixes, state synchronization

#### System Refinement  
- Balance tuning for growth speeds and populations
- Advanced node effects (healing, area effects, conditionals)
- Enhanced animal behaviors and interactions

### 📋 PHASE 3 - CONTENT EXPANSION

#### Extended Genetics
- **Advanced Nodes** - Healing, poison, temporal effects, multi-input combinations
- **Environmental Interactions** - Weather-responsive effects, soil chemistry
- **Defensive Mechanisms** - Thorns, repellent scents, adaptive behaviors

#### Ecosystem Complexity
- **Predator-Prey** - Carnivorous animals, food chain dynamics
- **Seasonal Systems** - Migration patterns, breeding cycles
- **Resource Cycles** - Nutrient depletion, regeneration mechanics

#### Gameplay Depth
- **Objectives** - Research challenges, genetic puzzles
- **Resource Management** - Limited seeds, tool durability
- **Progressive Unlocks** - Advanced nodes through experimentation

### 📋 PHASE 4 - ROGUE-LIKE FEATURES

#### Discovery System
- **Hidden Genetics** - Experimental discovery of new node types
- **Mutation Mechanics** - Random genetic variations
- **Research Lab** - Analysis and breeding programs

#### Challenge Structure
- **Scenario System** - Pre-designed genetic puzzles
- **Environmental Constraints** - Adaptation challenges
- **Procedural Content** - Dynamic ecosystem problems

### 📋 PHASE 5 - ADVANCED SYSTEMS

#### Technical Evolution
- **ECS Conversion** - Mass simulation optimization
- **Save/Load** - Complete world state persistence
- **Networking** - Multiplayer experimentation sharing

#### Content Systems
- **Procedural Worlds** - Generated biomes and challenges
- **Advanced AI** - Machine learning behaviors
- **Tool Crafting** - Resource gathering and creation

## 🎮 Gameplay Evolution

### Current Loop (Phase 1-2)
Tab → Node design → Q/E tool switch → Plant/modify → Observe ecosystem → Iterate

### Target Loop (Phase 3-4)  
Research → Plan genetics → Gather resources → Implement → Adapt to challenges → Discover mutations

## 📊 Success Metrics

### Technical Targets
- 60 FPS with 100+ plants, 50+ animals
- < 512MB memory usage
- < 5 second load times

### Content Goals
- 50+ node effect types
- 20+ animal species with distinct behaviors  
- 15+ specialized tools
- 10+ meaningful tile types

### Engagement Metrics
- 30+ minute sessions
- 80% node type discovery rate
- High design variety
- Strong player retention

## 🔧 Technical Priorities

### Performance Architecture
- Update() loop optimization for entity scaling
- Component pooling for visual effects
- Event-driven UI updates
- Cached tile lookup systems

### Future Technical Goals
- Unity Entities migration for mass simulation
- Burst compilation for critical calculations
- Job system parallelization
- Dynamic content streaming

## 🎯 Release Strategy

### Alpha Phases
- **0.1** - Integration and performance (Current)
- **0.2** - Content expansion and balance
- **0.3** - Rogue-like features implementation
- **0.4** - Polish and community testing

### Platform Strategy
- **Primary:** Steam (Windows/Mac/Linux)
- **Secondary:** Itch.io early access
- **Future:** Potential mobile adaptation

---

**Next Review:** After Phase 2 completion