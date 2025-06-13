using UnityEngine;
using WegoSystem;

namespace WegoSystem {
    [CreateAssetMenu(fileName = "TickConfiguration", menuName = "Wego/Tick Configuration")]
    public class TickConfiguration : ScriptableObject {
        [Header("Core Timing")]
        [SerializeField] public float ticksPerRealSecond = 2f;
        
        [Header("Day/Night Cycle")]
        public int ticksPerDay = 100;
        public int dayPhaseTicks = 60;
        public int nightPhaseTicks = 40;
        public int transitionTicks = 10;
        
        [Header("Wave System")]
        public int ticksPerWave = 50;
        public bool wavesDependOnDayCycle = false;
        
        [Header("Turn Phases")]
        public int maxPlanningPhaseTicks = 0; // 0 = unlimited
        public int executionPhaseTicks = 5;
        
        [Header("Entity Updates")]
        public int animalHungerTickInterval = 3;
        public int animalThinkingInterval = 3;
        
        [Header("Plant Growth")]
        public int defaultPlantGrowthTicksPerStage = 5;
        public int defaultPlantMaturityCycleTicks = 20;
        
        [Header("Movement")]
        public int movementTicksPerTile = 1;
        public int movementCooldownTicks = 0;
        
        // Helper methods
        public float GetRealSecondsPerTick() {
            return ticksPerRealSecond > 0 ? 1f / ticksPerRealSecond : 0.5f;
        }
        
        public int ConvertSecondsToTicks(float seconds) {
            return Mathf.RoundToInt(seconds * ticksPerRealSecond);
        }
        
        public float ConvertTicksToSeconds(int ticks) {
            return ticks / ticksPerRealSecond;
        }
        
        public int GetDayProgress(int currentTick) {
            return currentTick % ticksPerDay;
        }
        
        public float GetDayProgressNormalized(int currentTick) {
            return (float)(currentTick % ticksPerDay) / ticksPerDay;
        }
        
        public void SetTicksPerSecond(float newRate) {
            ticksPerRealSecond = Mathf.Max(0.1f, newRate);
        }
        
        // Validation
        void OnValidate() {
            ticksPerRealSecond = Mathf.Max(0.1f, ticksPerRealSecond);
            ticksPerDay = Mathf.Max(10, ticksPerDay);
            ticksPerWave = Mathf.Max(10, ticksPerWave);
            dayPhaseTicks = Mathf.Max(1, dayPhaseTicks);
            nightPhaseTicks = Mathf.Max(1, nightPhaseTicks);
            transitionTicks = Mathf.Max(1, transitionTicks);
            executionPhaseTicks = Mathf.Max(1, executionPhaseTicks);
            animalHungerTickInterval = Mathf.Max(1, animalHungerTickInterval);
            animalThinkingInterval = Mathf.Max(1, animalThinkingInterval);
            defaultPlantGrowthTicksPerStage = Mathf.Max(1, defaultPlantGrowthTicksPerStage);
            defaultPlantMaturityCycleTicks = Mathf.Max(1, defaultPlantMaturityCycleTicks);
        }
        
        // Preset configurations
        [ContextMenu("Apply Slow Pace Preset")]
        void ApplySlowPacedPreset() {
            ticksPerRealSecond = 1f;
            ticksPerDay = 200;
            ticksPerWave = 100;
            dayPhaseTicks = 120;
            nightPhaseTicks = 80;
            transitionTicks = 20;
            executionPhaseTicks = 10;
            defaultPlantGrowthTicksPerStage = 8;
            animalHungerTickInterval = 5;
        }
        
        [ContextMenu("Apply Fast Pace Preset")]
        void ApplyFastPacedPreset() {
            ticksPerRealSecond = 4f;
            ticksPerDay = 50;
            ticksPerWave = 25;
            dayPhaseTicks = 30;
            nightPhaseTicks = 20;
            transitionTicks = 5;
            executionPhaseTicks = 3;
            defaultPlantGrowthTicksPerStage = 2;
            animalHungerTickInterval = 2;
        }
        
        [ContextMenu("Apply Balanced Preset")]
        void ApplyBalancedPreset() {
            ticksPerRealSecond = 2f;
            ticksPerDay = 100;
            ticksPerWave = 50;
            dayPhaseTicks = 60;
            nightPhaseTicks = 40;
            transitionTicks = 10;
            executionPhaseTicks = 5;
            defaultPlantGrowthTicksPerStage = 5;
            animalHungerTickInterval = 3;
        }
    }
}