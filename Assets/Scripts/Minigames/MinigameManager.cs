using System;
using System.Collections.Generic;
using UnityEngine;
using WegoSystem;

namespace Abracodabra.Minigames {
    
    /// <summary>
    /// Central manager for the minigame system.
    /// Handles triggering minigames, processing results, and applying rewards.
    /// Supports deferred actions (action happens after minigame completes).
    /// </summary>
    public class MinigameManager : MonoBehaviour {
        
        public static MinigameManager Instance { get; private set; }

        [Header("Minigame Configurations")]
        [Tooltip("Configuration for planting minigame")]
        [SerializeField] TimingCircleConfig plantingConfig;
        
        [Header("Reward Configuration")]
        [Tooltip("Tool definition for watering (used as planting bonus)")]
        [SerializeField] ToolDefinition wateringCanTool;
        
        [Header("Settings")]
        [Tooltip("If true, minigames are enabled globally")]
        [SerializeField] bool minigamesEnabled = true;
        
        [Tooltip("If true, player can skip minigames by pressing Escape")]
        [SerializeField] bool allowSkip = true;
        
        [Header("Debug")]
        [SerializeField] bool showDebug = false;

        // Currently active minigame (only one at a time)
        TimingCircleMinigame activeMinigame;
        MinigameCompletedCallback pendingCallback;
        MinigameTrigger currentTrigger;
        Vector3Int currentGridPosition;
        
        // Deferred action data (for planting after minigame)
        Action pendingAction;
        Action<MinigameResult> pendingRewardAction;

        // Events for external systems to hook into
        public event Action<MinigameTrigger, Vector3Int> OnMinigameStarted;
        public event Action<MinigameResult> OnMinigameCompleted;

        // Track which triggers have minigames enabled (for future perk system)
        readonly HashSet<MinigameTrigger> enabledTriggers = new HashSet<MinigameTrigger>();

        public bool MinigamesEnabled => minigamesEnabled;
        public bool IsMinigameActive => activeMinigame != null;

        void Awake() {
            if (Instance != null && Instance != this) {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // By default, enable planting minigame
            EnableTrigger(MinigameTrigger.Planting);
        }

        void Update() {
            if (allowSkip && IsMinigameActive && Input.GetKeyDown(KeyCode.Escape)) {
                SkipCurrentMinigame();
            }
        }

        /// <summary>
        /// Enable minigames for a specific trigger type.
        /// </summary>
        public void EnableTrigger(MinigameTrigger trigger) {
            enabledTriggers.Add(trigger);
            if (showDebug) Debug.Log($"[MinigameManager] Enabled minigame for trigger: {trigger}");
        }

        /// <summary>
        /// Disable minigames for a specific trigger type.
        /// </summary>
        public void DisableTrigger(MinigameTrigger trigger) {
            enabledTriggers.Remove(trigger);
            if (showDebug) Debug.Log($"[MinigameManager] Disabled minigame for trigger: {trigger}");
        }

        /// <summary>
        /// Check if minigames are enabled for a trigger type.
        /// </summary>
        public bool IsTriggerEnabled(MinigameTrigger trigger) {
            return minigamesEnabled && enabledTriggers.Contains(trigger);
        }

        /// <summary>
        /// Trigger a minigame with a deferred action.
        /// The deferredAction is executed AFTER the minigame completes (regardless of result).
        /// The rewardAction is executed only on success, after deferredAction.
        /// Returns true if minigame was started.
        /// </summary>
        public bool TryTriggerMinigameWithDeferredAction(
            MinigameTrigger trigger, 
            Vector3Int gridPosition, 
            Vector3 worldPosition,
            Action deferredAction,
            Action<MinigameResult> rewardAction = null,
            MinigameCompletedCallback onComplete = null) 
        {
            if (!IsTriggerEnabled(trigger)) {
                if (showDebug) Debug.Log($"[MinigameManager] Minigame not enabled for trigger: {trigger}");
                return false;
            }

            if (IsMinigameActive) {
                if (showDebug) Debug.LogWarning("[MinigameManager] Cannot start minigame - another is already active");
                return false;
            }

            TimingCircleConfig config = GetConfigForTrigger(trigger);
            if (config == null) {
                if (showDebug) Debug.LogWarning($"[MinigameManager] No config found for trigger: {trigger}");
                return false;
            }

            // Store context
            currentTrigger = trigger;
            currentGridPosition = gridPosition;
            pendingCallback = onComplete;
            pendingAction = deferredAction;
            pendingRewardAction = rewardAction;

            // Create and start the minigame
            StartTimingCircleMinigame(config, worldPosition);

            OnMinigameStarted?.Invoke(trigger, gridPosition);
            
            if (showDebug) Debug.Log($"[MinigameManager] Started {trigger} minigame at {gridPosition} (with deferred action)");
            
            return true;
        }

        /// <summary>
        /// Attempt to trigger a minigame for the given action (legacy method without deferred action).
        /// Returns true if a minigame was started.
        /// </summary>
        public bool TryTriggerMinigame(MinigameTrigger trigger, Vector3Int gridPosition, Vector3 worldPosition, MinigameCompletedCallback onComplete = null) {
            return TryTriggerMinigameWithDeferredAction(trigger, gridPosition, worldPosition, null, null, onComplete);
        }

        TimingCircleConfig GetConfigForTrigger(MinigameTrigger trigger) {
            switch (trigger) {
                case MinigameTrigger.Planting:
                    return plantingConfig;
                default:
                    return null;
            }
        }

        void StartTimingCircleMinigame(TimingCircleConfig config, Vector3 worldPosition) {
            GameObject minigameGO = new GameObject($"TimingCircleMinigame_{Time.frameCount}");
            minigameGO.transform.position = worldPosition;

            activeMinigame = minigameGO.AddComponent<TimingCircleMinigame>();
            activeMinigame.Initialize(config, OnMinigameFinished);
            activeMinigame.StartMinigame();
        }

        void OnMinigameFinished(MinigameResult result) {
            // Enrich result with context
            result.Trigger = currentTrigger;
            result.GridPosition = currentGridPosition;

            if (showDebug) {
                Debug.Log($"[MinigameManager] Minigame finished: {result.Tier} (Accuracy: {result.Accuracy:P0})");
            }

            // Execute deferred action first (e.g., plant the seed)
            if (pendingAction != null) {
                pendingAction.Invoke();
                if (showDebug) Debug.Log("[MinigameManager] Executed deferred action");
            }

            // Apply rewards based on result (only if custom reward action not provided)
            if (pendingRewardAction != null) {
                pendingRewardAction.Invoke(result);
            } else {
                ApplyRewards(result);
            }

            // Notify external listeners
            OnMinigameCompleted?.Invoke(result);

            // Call pending callback
            pendingCallback?.Invoke(result);

            // Cleanup
            if (activeMinigame != null) {
                Destroy(activeMinigame.gameObject);
                activeMinigame = null;
            }
            pendingCallback = null;
            pendingAction = null;
            pendingRewardAction = null;
            currentTrigger = MinigameTrigger.None;
        }

        void ApplyRewards(MinigameResult result) {
            if (!result.IsSuccess) return;

            switch (result.Trigger) {
                case MinigameTrigger.Planting:
                    ApplyPlantingReward(result);
                    break;
            }
        }

        void ApplyPlantingReward(MinigameResult result) {
            if (wateringCanTool == null) {
                Debug.LogWarning("[MinigameManager] WateringCan tool not assigned - cannot apply planting bonus");
                return;
            }

            if (TileInteractionManager.Instance != null) {
                bool applied = TileInteractionManager.Instance.ApplyToolAtPosition(wateringCanTool, result.GridPosition);
                
                if (showDebug) {
                    string bonus = result.IsPerfect ? "PERFECT! " : "";
                    Debug.Log($"[MinigameManager] {bonus}Planting bonus applied: Watering at {result.GridPosition} = {applied}");
                }
            }
        }

        void SkipCurrentMinigame() {
            if (activeMinigame != null) {
                activeMinigame.Skip();
            }
        }

        /// <summary>
        /// Force cancel any active minigame
        /// </summary>
        public void CancelActiveMinigame() {
            if (activeMinigame != null) {
                Destroy(activeMinigame.gameObject);
                activeMinigame = null;
                pendingCallback = null;
                pendingAction = null;
                pendingRewardAction = null;
                currentTrigger = MinigameTrigger.None;
                
                if (showDebug) Debug.Log("[MinigameManager] Active minigame cancelled");
            }
        }

        void OnDestroy() {
            if (Instance == this) {
                Instance = null;
            }
            CancelActiveMinigame();
        }
    }
}