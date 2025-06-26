using UnityEngine;

namespace WegoSystem 
{
    public class TickConfiguration : ScriptableObject 
    {
        [SerializeField] public float ticksPerRealSecond = 2f;

        [Header("Day/Night Cycle")]
        public int ticksPerDay = 100;
        public int dayPhaseTicks = 60;
        public int nightPhaseTicks = 40;
        public int transitionTicks = 10;

        [Header("Animal Behavior")]
        public int animalHungerTickInterval = 3;
        public int animalThinkingInterval = 3;

        [Header("Movement")]
        public int movementTicksPerTile = 1;
        public int movementCooldownTicks = 0;

        // REMOVED: defaultPlantGrowthTicksPerStage - plants use seed data instead
        // REMOVED: defaultPlantMaturityCycleTicks - plants use seed data instead
        // REMOVED: maxPlanningPhaseTicks - not implemented, planning is unlimited
        // REMOVED: executionPhaseTicks - not implemented, execution phase not used
        // REMOVED: ticksPerWave - waves use day cycles instead
        // REMOVED: wavesDependOnDayCycle - waves always use day cycles

        public float GetRealSecondsPerTick() 
        {
            return ticksPerRealSecond > 0 ? 1f / ticksPerRealSecond : 0.5f;
        }

        public int ConvertSecondsToTicks(float seconds) 
        {
            return Mathf.RoundToInt(seconds * ticksPerRealSecond);
        }

        public float ConvertTicksToSeconds(int ticks) 
        {
            return ticks / ticksPerRealSecond;
        }

        public int GetDayProgress(int currentTick) 
        {
            return currentTick % ticksPerDay;
        }

        public float GetDayProgressNormalized(int currentTick) 
        {
            return (float)(currentTick % ticksPerDay) / ticksPerDay;
        }

        public void SetTicksPerSecond(float newRate) 
        {
            ticksPerRealSecond = Mathf.Max(0.1f, newRate);
        }

        void OnValidate() 
        {
            ticksPerRealSecond = Mathf.Max(0.1f, ticksPerRealSecond);
            ticksPerDay = Mathf.Max(10, ticksPerDay);
            dayPhaseTicks = Mathf.Max(1, dayPhaseTicks);
            nightPhaseTicks = Mathf.Max(1, nightPhaseTicks);
            transitionTicks = Mathf.Max(1, transitionTicks);
            animalHungerTickInterval = Mathf.Max(1, animalHungerTickInterval);
            animalThinkingInterval = Mathf.Max(1, animalThinkingInterval);
        }

        // Preset methods for quick configuration
        void ApplySlowPacedPreset() 
        {
            ticksPerRealSecond = 1f;
            ticksPerDay = 200;
            dayPhaseTicks = 120;
            nightPhaseTicks = 80;
            transitionTicks = 20;
            animalHungerTickInterval = 5;
        }

        void ApplyFastPacedPreset() 
        {
            ticksPerRealSecond = 4f;
            ticksPerDay = 50;
            dayPhaseTicks = 30;
            nightPhaseTicks = 20;
            transitionTicks = 5;
            animalHungerTickInterval = 2;
        }

        void ApplyBalancedPreset() 
        {
            ticksPerRealSecond = 2f;
            ticksPerDay = 100;
            dayPhaseTicks = 60;
            nightPhaseTicks = 40;
            transitionTicks = 10;
            animalHungerTickInterval = 3;
        }
    }
}