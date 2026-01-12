// FILE: Assets/Scripts/Ecosystem/Doris/DorisController.cs
using System;
using System.Collections.Generic;
using UnityEngine;
using WegoSystem;
using Abracodabra.Genes;

namespace Abracodabra.Ecosystem {
    /// <summary>
    /// Main controller for Doris. Handles interactions, visual state, and coordinates
    /// with DorisHungerSystem for hunger-related behaviors.
    /// </summary>
    public class DorisController : MonoBehaviour, IWorldInteractable {
        [Header("Configuration")]
        [SerializeField] private DorisDefinition definition;

        [Header("References")]
        [SerializeField] private MultiTileEntity multiTileEntity;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private DorisHungerSystem hungerSystem;

        [Header("Hover Feedback")]
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color hoverColor = new Color(1f, 1f, 0.8f, 1f);

        [Header("Starvation Behavior")]
        [SerializeField] private int ticksSinceLastPlantEat = 0;

        [Header("Debug")]
        [SerializeField] private bool debugLog = false;

        // State
        private bool isHovered = false;
        private bool isInitialized = false;

        // Events for external systems
        public event Action<PlantGrowth> OnDorisAtePlant;
        public event Action<float> OnDorisFed;

        // Properties
        public DorisDefinition Definition => definition;
        public DorisHungerSystem HungerSystem => hungerSystem;
        public MultiTileEntity MultiTileEntity => multiTileEntity;

        // IWorldInteractable implementation
        public int InteractionPriority => multiTileEntity != null 
            ? multiTileEntity.InteractionPriority 
            : (definition != null ? 200 : 200);

        public bool CanInteract => enabled && gameObject.activeInHierarchy;

        public Vector3 InteractionWorldPosition => transform.position;

        #region Unity Lifecycle

        private void Awake() {
            CacheComponents();
            ValidateComponents();
        }

        private void Start() {
            Initialize();
        }

        private void OnDestroy() {
            UnsubscribeFromEvents();
        }

        private void OnValidate() {
            if (multiTileEntity == null) {
                multiTileEntity = GetComponent<MultiTileEntity>();
            }
            if (hungerSystem == null) {
                hungerSystem = GetComponent<DorisHungerSystem>();
            }
        }

        #endregion

        #region Initialization

        private void CacheComponents() {
            if (multiTileEntity == null) {
                multiTileEntity = GetComponent<MultiTileEntity>();
            }
            if (spriteRenderer == null) {
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }
            if (hungerSystem == null) {
                hungerSystem = GetComponent<DorisHungerSystem>();
                if (hungerSystem == null) {
                    hungerSystem = gameObject.AddComponent<DorisHungerSystem>();
                    Debug.Log("[DorisController] Added DorisHungerSystem component.");
                }
            }
        }

        private void ValidateComponents() {
            if (definition == null) {
                Debug.LogError("[DorisController] Missing DorisDefinition! Doris will not function properly.", this);
            }
            if (multiTileEntity == null) {
                Debug.LogWarning("[DorisController] Missing MultiTileEntity reference.", this);
            }
            if (spriteRenderer == null) {
                Debug.LogWarning("[DorisController] Missing SpriteRenderer reference.", this);
            }
        }

        private void Initialize() {
            if (isInitialized) return;

            // Initialize hunger system with our definition
            if (hungerSystem != null && definition != null) {
                hungerSystem.Initialize(definition);
            }

            // Cache normal color
            if (spriteRenderer != null) {
                normalColor = spriteRenderer.color;
            }

            // Subscribe to hunger events
            SubscribeToEvents();

            // Update initial visual state
            UpdateVisualState();

            isInitialized = true;

            if (debugLog) {
                Debug.Log("[DorisController] Initialized successfully.");
            }
        }

        private void SubscribeToEvents() {
            if (hungerSystem != null) {
                hungerSystem.OnStateChanged += HandleHungerStateChanged;
                hungerSystem.OnStarvationTick += HandleStarvationTick;
                hungerSystem.OnFed += HandleFed;
                hungerSystem.OnHungerChanged += HandleHungerChanged;
            }
        }

        private void UnsubscribeFromEvents() {
            if (hungerSystem != null) {
                hungerSystem.OnStateChanged -= HandleHungerStateChanged;
                hungerSystem.OnStarvationTick -= HandleStarvationTick;
                hungerSystem.OnFed -= HandleFed;
                hungerSystem.OnHungerChanged -= HandleHungerChanged;
            }
        }

        #endregion

        #region IWorldInteractable Implementation

        public bool OnInteract(GameObject interactor, ToolDefinition tool) {
            if (tool == null) {
                OnClickedWithHands(interactor);
                return true;
            }

            if (debugLog) {
                Debug.Log($"[DorisController] Player used tool '{tool.displayName}' on Doris");
            }

            return HandleToolInteraction(interactor, tool);
        }

        public void OnHoverEnter() {
            isHovered = true;
            if (spriteRenderer != null) {
                spriteRenderer.color = hoverColor;
            }

            if (debugLog) {
                Debug.Log("[DorisController] Hover entered");
            }
        }

        public void OnHoverExit() {
            isHovered = false;
            if (spriteRenderer != null) {
                spriteRenderer.color = normalColor;
            }

            if (debugLog) {
                Debug.Log("[DorisController] Hover exited");
            }
        }

        #endregion

        #region Interaction Handlers

        private void OnClickedWithHands(GameObject interactor) {
            if (debugLog) {
                Debug.Log($"[DorisController] Player clicked Doris with bare hands");
            }

            // TODO: Open Doris UI panel (Task 9 - DorisHungerUI)
            // TODO: Show Doris info/status
            // TODO: Trigger dialogue if any
        }

        private bool HandleToolInteraction(GameObject interactor, ToolDefinition tool) {
            // TODO: Task 2 - Feeding Mechanism
            // Check if this is a food item being given to Doris
            // For now, just log and return true

            if (debugLog) {
                Debug.Log($"[DorisController] Tool interaction with '{tool.displayName}' - not yet implemented");
            }

            return true;
        }

        /// <summary>
        /// Feed Doris with a food item. Called by feeding mechanism.
        /// </summary>
        /// <param name="nutritionValue">Nutrition value of the food.</param>
        /// <param name="foodSource">Optional reference to the food source for tracking.</param>
        /// <returns>True if feeding was successful.</returns>
        public bool FeedDoris(float nutritionValue, object foodSource = null) {
            if (hungerSystem == null || nutritionValue <= 0) {
                return false;
            }

            float actualReduction = hungerSystem.Feed(nutritionValue);

            if (actualReduction > 0) {
                // Play feed sound
                if (definition != null && definition.feedSound != null) {
                    AudioSource.PlayClipAtPoint(definition.feedSound, transform.position);
                }

                // Fire event for tracking (used by FeedingTracker in Task 2)
                OnDorisFed?.Invoke(nutritionValue);

                if (debugLog) {
                    Debug.Log($"[DorisController] Fed Doris with {nutritionValue:F1} nutrition. " +
                              $"Hunger reduced by {actualReduction:F1}");
                }

                return true;
            }

            return false;
        }

        #endregion

        #region Hunger Event Handlers

        private void HandleHungerStateChanged(DorisHungerSystem.HungerState oldState, DorisHungerSystem.HungerState newState) {
            if (debugLog) {
                Debug.Log($"[DorisController] Hunger state changed: {oldState} -> {newState}");
            }

            UpdateVisualState();
            PlayStateChangeAudio(newState);
        }

        private void HandleHungerChanged(float currentHunger, float maxHunger) {
            // Can be used to update UI elements
            // HUD/floating bar will subscribe directly to hungerSystem.OnHungerChanged
        }

        private void HandleFed(float amount) {
            // Additional visual/audio feedback when fed could go here
        }

        private void HandleStarvationTick() {
            if (definition == null) return;

            ticksSinceLastPlantEat++;

            if (ticksSinceLastPlantEat >= definition.ticksBetweenPlantEating) {
                ticksSinceLastPlantEat = 0;
                TryEatNearbyPlant();
            }
        }

        #endregion

        #region Starvation Behavior (Task 7 prep)

        private void TryEatNearbyPlant() {
            if (definition == null || multiTileEntity == null) return;
            if (GridPositionManager.Instance == null) return;

            // Find plants in radius
            var entitiesInRadius = GridPositionManager.Instance.GetEntitiesInRadius(
                multiTileEntity.AnchorPosition,
                definition.plantEatingRadius,
                true // Use circle radius
            );

            // Find the nearest plant
            PlantGrowth nearestPlant = null;
            float nearestDist = float.MaxValue;

            foreach (var entity in entitiesInRadius) {
                var plant = entity.GetComponent<PlantGrowth>();
                // Only consider plants that are Growing or Mature (alive and edible)
                if (plant != null && (plant.CurrentState == PlantState.Growing || plant.CurrentState == PlantState.Mature)) {
                    float dist = entity.Position.ManhattanDistance(multiTileEntity.AnchorPosition);
                    if (dist < nearestDist) {
                        nearestDist = dist;
                        nearestPlant = plant;
                    }
                }
            }

            // Eat it
            if (nearestPlant != null) {
                EatPlant(nearestPlant);
            }
        }

        private void EatPlant(PlantGrowth plant) {
            if (plant == null) return;

            Debug.LogWarning($"[DorisController] Doris ate plant: {plant.gameObject.name}");

            // Play eat plant sound
            if (definition != null && definition.eatPlantSound != null) {
                AudioSource.PlayClipAtPoint(definition.eatPlantSound, transform.position);
            }

            // Notify hunger system
            if (hungerSystem != null) {
                hungerSystem.OnAtePlant();
            }

            // Fire event for external systems
            OnDorisAtePlant?.Invoke(plant);

            // Destroy the plant
            UnityEngine.Object.Destroy(plant.gameObject);
        }

        #endregion

        #region Visual State

        private void UpdateVisualState() {
            if (spriteRenderer == null || definition == null || hungerSystem == null) return;

            Sprite targetSprite = null;

            switch (hungerSystem.CurrentState) {
                case DorisHungerSystem.HungerState.Satisfied:
                    targetSprite = definition.happySprite;
                    break;
                case DorisHungerSystem.HungerState.Hungry:
                    targetSprite = definition.hungrySprite;
                    break;
                case DorisHungerSystem.HungerState.Starving:
                    targetSprite = definition.starvingSprite;
                    break;
            }

            if (targetSprite != null) {
                spriteRenderer.sprite = targetSprite;
            }
        }

        private void PlayStateChangeAudio(DorisHungerSystem.HungerState newState) {
            if (definition == null) return;

            AudioClip clip = null;

            switch (newState) {
                case DorisHungerSystem.HungerState.Hungry:
                    clip = definition.hungrySound;
                    break;
                case DorisHungerSystem.HungerState.Starving:
                    clip = definition.hungrySound; // Could be a different "angry" sound
                    break;
            }

            if (clip != null) {
                AudioSource.PlayClipAtPoint(clip, transform.position);
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Get current hunger information for UI display.
        /// </summary>
        public (float current, float max, float percent, DorisHungerSystem.HungerState state) GetHungerInfo() {
            if (hungerSystem == null) {
                return (0, 100, 0, DorisHungerSystem.HungerState.Satisfied);
            }

            return (
                hungerSystem.CurrentHunger,
                hungerSystem.MaxHunger,
                hungerSystem.HungerPercent,
                hungerSystem.CurrentState
            );
        }

        /// <summary>
        /// Reset Doris for a new round.
        /// </summary>
        public void ResetForNewRound() {
            ticksSinceLastPlantEat = 0;

            if (hungerSystem != null) {
                hungerSystem.ResetHunger();
            }

            UpdateVisualState();

            if (debugLog) {
                Debug.Log("[DorisController] Reset for new round.");
            }
        }

        #endregion
    }
}