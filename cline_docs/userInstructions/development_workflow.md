# Development Workflow Guide

**Last Updated:** 2025-05-29  
**Project:** Abracodebra 2.0

## 🚀 Getting Started

1. Clone the repository
2. Open project in Unity 6 (6000.0.39f1)
3. Wait for initial asset import
4. Open MainScene.unity

## 🔄 Daily Development Cycle

### 1. Project Sync
```bash
# Pull latest changes
git pull origin main

# Update packages if needed
# Unity Package Manager will prompt if updates required
```

### 2. Scene Setup
1. Open MainScene.unity
2. Ensure PixelPerfectCamera is active
3. Check Post Processing volume is enabled
4. Verify Input System is in correct mode

### 3. Development Loop
1. Make code changes
2. Test in Play mode
3. Use Unity Test Runner for unit tests
4. Commit changes with clear messages

## 🛠️ Common Tasks

### Adding New Plant Nodes
1. Open Node Graph Editor (Tab key)
2. Right-click > Create New Node
3. Configure node properties
4. Add to NodeDefinitionLibrary
5. Test node behavior in play mode

### Creating Animal Behaviors
1. Create new ScriptableObject in Animals/
2. Configure diet preferences
3. Set up thought patterns
4. Add behavior scripts
5. Test AI responses

### Modifying Tile System
1. Update Wang tile rules
2. Configure tile interactions
3. Set up visual states
4. Test with different tools
5. Verify ecosystem responses

## 🎮 Testing Protocol

### Quick Tests
1. Enter Play mode
2. Plant test specimens
3. Observe growth patterns
4. Check animal interactions
5. Verify tool functionality

### Thorough Testing
1. Run all unit tests
2. Profile performance
3. Test edge cases
4. Verify cross-system interactions
5. Check memory usage

## 🐛 Debug Tools

### Unity Tools
1. Frame Debugger: Visual effect issues
2. Profiler: Performance bottlenecks
3. Console: Error tracking
4. Scene view: Spatial debugging

### Custom Tools
1. Node Graph Debugger
2. Ecosystem State Viewer
3. Tile System Inspector
4. Plant Growth Visualizer

## 📦 Building

### Development Build
```bash
1. File > Build Settings
2. Select Windows/Mac/Linux
3. Enable Development Build
4. Choose Build Location
5. Click Build
```

### Release Build
```bash
1. File > Build Settings
2. Disable Development Build
3. Enable IL2CPP
4. Strip Engine Code
5. Build and Zip
```

## 🔍 Code Review Guidelines

### Before Submitting
1. Run code cleanup
2. Update documentation
3. Test all changes
4. Check performance impact
5. Verify style compliance

### Review Process
1. Create pull request
2. Add test evidence
3. Document changes
4. Request reviews
5. Address feedback

## 📝 Documentation Updates

### When to Update
- New features added
- Systems modified
- APIs changed
- Workflows updated
- Dependencies changed

### What to Update
1. XML documentation
2. README files
3. API documentation
4. User guides
5. Architecture diagrams

## 🚨 Common Issues

### Scene Issues
- Missing references: Check prefab connections
- Broken materials: Reimport assets
- Camera glitches: Reset PixelPerfectCamera
- Input problems: Verify Input System setup

### Code Issues
- Null references: Check initialization
- Performance spikes: Profile specific systems
- Memory leaks: Monitor allocations
- Physics glitches: Check collision matrices

## ⚡ Performance Tips

### Runtime Optimization
1. Batch similar operations
2. Use object pooling
3. Minimize garbage collection
4. Cache frequently used values
5. Profile critical paths

### Development Optimization
1. Use assembly definitions
2. Enable incremental compilation
3. Optimize asset imports
4. Use appropriate quality settings
5. Enable fast play mode

---

**Next Update:** When workflows or tools significantly change
