using UnityEngine;
using WegoSystem;

namespace WegoSystem {
    public class TickConfiguration : ScriptableObject {
        [SerializeField] public float ticksPerRealSecond = 2f;

        public int ticksPerGameHour = 10;
        public int hoursPerDay = 24;
        public int TicksPerDay => ticksPerGameHour * hoursPerDay;

        [Header("Day/Night Cycle")]
        public int ticksPerDay = 100;  // Total ticks for one full day
        public int dayPhaseTicks = 60;
        public int nightPhaseTicks = 40;
        public int transitionTicks = 10;

        [Header("Wave System")]
        public int ticksPerWave = 50;  // How long each wave lasts in ticks
        public bool wavesDependOnDayCycle = false;  // If true, waves end with day cycles

        public int maxPlanningPhaseTicks = 0;
        public int executionPhaseTicks = 5;

        public int plantGrowthTicksPerStage = 5;

        public int animalHungerTickInterval = 3;
        public int animalThinkingInterval = 3;

        public int waveSpawnDelayTicks = 20;

        public int movementTicksPerTile = 1;
        public int movementCooldownTicks = 0;

        [SerializeField] bool useVariableTickRate = false;
        [SerializeField] AnimationCurve tickRateCurve = AnimationCurve.Linear(0, 1, 1, 1);

        public float GetRealSecondsPerTick() {
            return ticksPerRealSecond > 0 ? 1f / ticksPerRealSecond : 0.5f;
        }

        public int GetTotalDayNightCycleTicks() {
            return dayPhaseTicks + nightPhaseTicks + (transitionTicks * 2);
        }

        public int ConvertSecondsToTicks(float seconds) {
            return Mathf.RoundToInt(seconds * ticksPerRealSecond);
        }

        public float ConvertTicksToSeconds(int ticks) {
            return ticks / ticksPerRealSecond;
        }

        public int GetPhaseTickDuration(TurnPhase phase) {
            switch (phase) {
                case TurnPhase.Planning: return maxPlanningPhaseTicks;
                case TurnPhase.Execution: return executionPhaseTicks;
                default: return 1;
            }
        }

        public float GetTickRateMultiplier(int currentTick) {
            if (!useVariableTickRate) return 1f;

            float normalizedTime = (currentTick % TicksPerDay) / (float)TicksPerDay;
            return tickRateCurve.Evaluate(normalizedTime);
        }

        public void SetTicksPerSecond(float newRate) {
            ticksPerRealSecond = Mathf.Max(0.1f, newRate);
        }
        
        public int GetDayProgress(int currentTick) {
            return currentTick % ticksPerDay;
        }

        public float GetDayProgressNormalized(int currentTick) {
            return (float)(currentTick % ticksPerDay) / ticksPerDay;
        }

        void ApplySlowPacedPreset() {
            ticksPerRealSecond = 1f;
            ticksPerDay = 200;
            ticksPerWave = 100;
            dayPhaseTicks = 120;
            nightPhaseTicks = 80;
            transitionTicks = 20;
            executionPhaseTicks = 10;
            plantGrowthTicksPerStage = 8;
            animalHungerTickInterval = 5;
        }

        void ApplyFastPacedPreset() {
            ticksPerRealSecond = 4f;
            ticksPerDay = 50;
            ticksPerWave = 25;
            dayPhaseTicks = 30;
            nightPhaseTicks = 20;
            transitionTicks = 5;
            executionPhaseTicks = 3;
            plantGrowthTicksPerStage = 2;
            animalHungerTickInterval = 2;
        }

        void ApplyBalancedPreset() {
            ticksPerRealSecond = 2f;
            ticksPerDay = 100;
            ticksPerWave = 50;
            dayPhaseTicks = 60;
            nightPhaseTicks = 40;
            transitionTicks = 10;
            executionPhaseTicks = 5;
            plantGrowthTicksPerStage = 5;
            animalHungerTickInterval = 3;
        }

        void OnValidate() {
            ticksPerRealSecond = Mathf.Max(0.1f, ticksPerRealSecond);
            ticksPerDay = Mathf.Max(10, ticksPerDay);
            ticksPerWave = Mathf.Max(10, ticksPerWave);
            dayPhaseTicks = Mathf.Max(1, dayPhaseTicks);
            nightPhaseTicks = Mathf.Max(1, nightPhaseTicks);
            transitionTicks = Mathf.Max(1, transitionTicks);
            executionPhaseTicks = Mathf.Max(1, executionPhaseTicks);
            plantGrowthTicksPerStage = Mathf.Max(1, plantGrowthTicksPerStage);
            animalHungerTickInterval = Mathf.Max(1, animalHungerTickInterval);
            animalThinkingInterval = Mathf.Max(1, animalThinkingInterval);
        }
    }
}