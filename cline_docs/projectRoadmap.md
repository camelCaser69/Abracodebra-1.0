# Abracodebra 2.0 - Project Roadmap

**Last Updated:** 2025-05-29  
**Unity Version:** 6 (6000.0.39f1)  
**Project Type:** 2D Ecosystem Rogue-like Sandbox

## 🎯 Game Pillars

### Core Vision
Abracodebra 2.0 is a 2D pixel-art ecosystem rogue-like sandbox where **plants grow procedurally from node-graphs**, **animals wander, eat, think and poop**, and the environment reacts via day/night cycles, scents, and tile interactions. Players genetically engineer plants by editing gene nodes and obtaining/inventing gene sequences in a rogue-like fashion.

**Inspiration:** Blend of Noita and Stardew Valley

### Key Pillars
1. **Living Ecosystem** - Dynamic fauna AI with hunger, thoughts, pathfinding-free wandering
2. **Procedural Plant Growth** - Node-graph driven plant development with genetic engineering
3. **Environmental Interaction** - Dual-grid Wang-tile system with tile modifiers and scent systems
4. **Rogue-like Progression** - Gene sequence discovery and plant breeding mechanics

## 🚀 Current Milestones

### ✅ COMPLETED
- **Core Plant Growth System** - PlantGrowth with node-graph execution
- **Dual-Grid Tilemap Integration** - Wang-tile ground system with tile interactions
- **Basic Ecosystem** - Animal AI, scent system, day/night cycle
- **Node Editor UI** - Tab-based node graph editing with drag-drop
- **Tool System** - Gardening tools (Hoe, Watering Can, Seed Pouch)
- **Visual Effects** - Plant shadows, outlines, water reflections, firefly system
- **Weather System** - Day/night transitions with post-processing effects

### 🔄 IN PROGRESS
- **Documentation Setup** - Establishing Cline workflow documentation
- **System Integration** - Ensuring all systems work cohesively

### 📋 UPCOMING PRIORITIES
1. **Plant Genetics Expansion** - More node types and complex interactions
2. **Rogue-like Elements** - Gene discovery mechanics, progression systems
3. **Enhanced AI** - More complex animal behaviors and interactions
4. **Performance Optimization** - ECS conversion for mass plant instances
5. **Save/Load System** - JSON serialization for node graphs and world state

## 🗂️ Done Archive

### Phase 1 - Foundation (Completed)
- Basic plant growth with stem/leaf patterns
- Animal spawning and basic AI behaviors
- Tile interaction system with tool switching
- Node graph UI with effect configuration
- Shadow and outline visual systems

### Phase 2 - Systems Integration (Completed)
- Scent system with radius visualization
- Poop fertilizer mechanics with leaf regrowth
- Weather manager with day/night transitions
- Plant placement management with tile validation
- Tool refill mechanics and usage limits

## 🎮 Current Gameplay Loop

1. **Plant Creation** - Use node editor (Tab key) to design plant genetics
2. **Seed Planting** - Switch to Seed Pouch tool and plant on valid tiles
3. **Growth Observation** - Watch plants grow based on node configurations
4. **Ecosystem Interaction** - Animals spawn, seek food, produce fertilizer
5. **Environmental Management** - Use tools to modify tiles and support growth
6. **Cycle Progression** - Day/night affects photosynthesis and animal behavior

## 🔧 Technical Debt & Improvements

### High Priority
- Unit testing framework for core systems
- Performance profiling for large plant populations
- Memory optimization for visual effects

### Medium Priority
- Event-queue system for scent/thought decoupling
- Strongly-typed scent storage for Burst/ECS readiness
- Enhanced error handling and validation

### Low Priority
- Advanced shader effects for visual polish
- Audio system integration
- Localization framework

---

**Next Review:** When starting new major features or after significant system changes
