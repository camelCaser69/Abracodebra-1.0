using System;
using UnityEngine;

namespace Abracodabra.Minigames {
    
    /// <summary>
    /// Types of minigames available in the system.
    /// Expand this enum as new minigames are added.
    /// </summary>
    public enum MinigameType {
        None,
        TimingCircle,       // Wartales-style shrinking circle
        // Future types:
        // StopTheBar,      // Golf-style moving marker
        // ReactionTest,    // Hades-style flash reaction
        // RhythmTap,       // Beat-matching
    }

    /// <summary>
    /// What action triggered the minigame.
    /// Used to determine rewards and context.
    /// </summary>
    public enum MinigameTrigger {
        None,
        Planting,
        Watering,
        Harvesting,
        Tilling,
        // Future triggers as needed
    }

    /// <summary>
    /// Quality tier of minigame result.
    /// </summary>
    public enum MinigameResultTier {
        Miss,       // Didn't click in time or clicked outside zone
        Good,       // Clicked in outer success zone
        Perfect,    // Clicked in inner "perfect" zone
        Skipped,    // Player skipped/cancelled the minigame
    }

    /// <summary>
    /// Result data from a completed minigame.
    /// </summary>
    [Serializable]
    public struct MinigameResult {
        public MinigameResultTier Tier;
        public float Accuracy;          // 0-1, how close to perfect center
        public float TimeRemaining;     // How much time was left when clicked
        public MinigameTrigger Trigger; // What triggered this minigame
        public Vector3Int GridPosition; // Where the action occurred

        public bool IsSuccess => Tier == MinigameResultTier.Good || Tier == MinigameResultTier.Perfect;
        public bool IsPerfect => Tier == MinigameResultTier.Perfect;

        public static MinigameResult Miss(MinigameTrigger trigger, Vector3Int gridPos) {
            return new MinigameResult {
                Tier = MinigameResultTier.Miss,
                Accuracy = 0f,
                TimeRemaining = 0f,
                Trigger = trigger,
                GridPosition = gridPos
            };
        }

        public static MinigameResult Skipped(MinigameTrigger trigger, Vector3Int gridPos) {
            return new MinigameResult {
                Tier = MinigameResultTier.Skipped,
                Accuracy = 0f,
                TimeRemaining = 0f,
                Trigger = trigger,
                GridPosition = gridPos
            };
        }
    }

    /// <summary>
    /// Callback signature for minigame completion.
    /// </summary>
    public delegate void MinigameCompletedCallback(MinigameResult result);
}
