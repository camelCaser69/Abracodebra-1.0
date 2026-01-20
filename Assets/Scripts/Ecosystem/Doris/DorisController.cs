// FILE: Assets/Scripts/Ecosystem/Doris/DorisController.cs
using System;
using UnityEngine;
using WegoSystem;
using Abracodabra.Genes;
using Abracodabra.Ecosystem.Feeding;

namespace Abracodabra.Ecosystem
{
    public class DorisController : MonoBehaviour, IWorldInteractable, IFeedable
    {
        [Header("Configuration")]
        [SerializeField] private DorisDefinition definition;

        [Header("References")]
        [SerializeField] private MultiTileEntity multiTileEntity;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private DorisHungerSystem hungerSystem;

        [Header("Hover Feedback")]
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color hoverColor = new Color(1f, 1f, 0.8f, 1f);

        [Header("Feeding")]
        [SerializeField] private Vector3 feedPopupOffset = new Vector3(0f, 1.5f, 0f);

        [Header("Starvation Behavior")]
        [SerializeField] private int ticksSinceLastPlantEat = 0;

        [Header("Debug")]
        [SerializeField] private bool debugLog = false;

        private bool isHovered = false;
        private bool isInitialized = false;

        // Events
        public event Action<PlantGrowth> OnDorisAtePlant;
        public event Action<float> OnDorisFed;
        public event Action<ConsumableData, float> OnFed;

        // Properties
        public DorisDefinition Definition => definition;
        public DorisHungerSystem HungerSystem => hungerSystem;
        public MultiTileEntity MultiTileEntity => multiTileEntity;

        #region IWorldInteractable Implementation

        public int InteractionPriority => multiTileEntity != null
            ? multiTileEntity.InteractionPriority
            : (definition != null ? 200 : 200);

        public bool CanInteract => enabled && gameObject.activeInHierarchy;

        public Vector3 InteractionWorldPosition => transform.position;

        #endregion

        #region IFeedable Implementation

        public string FeedableName => definition != null ? definition.displayName : "Doris";

        public Vector3 FeedPopupAnchor => transform.position + feedPopupOffset;

        public bool CanAcceptFood(ConsumableData consumable)
        {
            if (consumable == null) return false;

            // Doris can eat most foods - check definition for diet restrictions
            if (definition != null && definition.acceptedFoodCategories != null 
                && definition.acceptedFoodCategories.Length > 0)
            {
                // Check if food category is in accepted list
                return definition.CanEatCategory(consumable.Category);
            }

            // Default: Doris accepts all food
            return true;
        }

        public float ReceiveFood(ConsumableData consumable, GameObject feeder)
        {
            if (consumable == null || hungerSystem == null)
            {
                return 0f;
            }

            // Calculate satiation
            float satiation = consumable.NutritionValue;

            // Apply any bonuses from definition
            if (definition != null)
            {
                satiation *= definition.GetFoodCategoryMultiplier(consumable.Category);
            }

            // Apply to hunger system
            float actualReduction = hungerSystem.Feed(satiation);

            // Play sound
            if (actualReduction > 0 && definition != null && definition.feedSound != null)
            {
                AudioSource.PlayClipAtPoint(definition.feedSound, transform.position);
            }

            // Fire events
            OnDorisFed?.Invoke(actualReduction);
            OnFed?.Invoke(consumable, actualReduction);

            if (debugLog)
            {
                Debug.Log($"[DorisController] Fed {consumable.Name} (nutrition={consumable.NutritionValue:F1}, " +
                          $"category={consumable.Category}). Actual reduction: {actualReduction:F1}");
            }

            // Reset starvation counter
            ticksSinceLastPlantEat = 0;

            return actualReduction;
        }

        #endregion

        #region Unity Lifecycle

        void Awake()
        {
            CacheComponents();
            ValidateComponents();
        }

        void Start()
        {
            Initialize();
            
            // Register with FeedingSystem
            if (FeedingSystem.Instance != null)
            {
                FeedingSystem.Instance.RegisterFeedable(this);
            }
        }

        void OnDestroy()
        {
            UnsubscribeFromEvents();
            
            // Unregister from FeedingSystem
            if (FeedingSystem.Instance != null)
            {
                FeedingSystem.Instance.UnregisterFeedable(this);
            }
        }

        void OnValidate()
        {
            if (multiTileEntity == null)
            {
                multiTileEntity = GetComponent<MultiTileEntity>();
            }
            if (hungerSystem == null)
            {
                hungerSystem = GetComponent<DorisHungerSystem>();
            }
        }

        #endregion

        #region Initialization

        void CacheComponents()
        {
            if (multiTileEntity == null)
            {
                multiTileEntity = GetComponent<MultiTileEntity>();
            }
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }
            if (hungerSystem == null)
            {
                hungerSystem = GetComponent<DorisHungerSystem>();
                if (hungerSystem == null)
                {
                    hungerSystem = gameObject.AddComponent<DorisHungerSystem>();
                    Debug.Log("[DorisController] Added DorisHungerSystem component.");
                }
            }
        }

        void ValidateComponents()
        {
            if (definition == null)
            {
                Debug.LogError("[DorisController] Missing DorisDefinition! Doris will not function properly.", this);
            }
            if (multiTileEntity == null)
            {
                Debug.LogWarning("[DorisController] Missing MultiTileEntity reference.", this);
            }
            if (spriteRenderer == null)
            {
                Debug.LogWarning("[DorisController] Missing SpriteRenderer reference.", this);
            }
        }

        void Initialize()
        {
            if (isInitialized) return;

            if (hungerSystem != null && definition != null)
            {
                hungerSystem.Initialize(definition);
            }

            if (spriteRenderer != null)
            {
                normalColor = spriteRenderer.color;
            }

            SubscribeToEvents();
            UpdateVisualState();

            isInitialized = true;

            if (debugLog)
            {
                Debug.Log("[DorisController] Initialized successfully.");
            }
        }

        void SubscribeToEvents()
        {
            if (hungerSystem != null)
            {
                hungerSystem.OnStateChanged += HandleHungerStateChanged;
                hungerSystem.OnStarvationTick += HandleStarvationTick;
                hungerSystem.OnFed += HandleHungerSystemFed;
                hungerSystem.OnHungerChanged += HandleHungerChanged;
            }
        }

        void UnsubscribeFromEvents()
        {
            if (hungerSystem != null)
            {
                hungerSystem.OnStateChanged -= HandleHungerStateChanged;
                hungerSystem.OnStarvationTick -= HandleStarvationTick;
                hungerSystem.OnFed -= HandleHungerSystemFed;
                hungerSystem.OnHungerChanged -= HandleHungerChanged;
            }
        }

        #endregion

        #region IWorldInteractable

        public bool OnInteract(GameObject interactor, ToolDefinition tool)
        {
            if (tool == null)
            {
                OnClickedWithHands(interactor);
                return true;
            }

            if (debugLog)
            {
                Debug.Log($"[DorisController] Player used tool '{tool.displayName}' on Doris");
            }

            return HandleToolInteraction(interactor, tool);
        }

        public void OnHoverEnter()
        {
            isHovered = true;
            if (spriteRenderer != null)
            {
                spriteRenderer.color = hoverColor;
            }

            if (debugLog)
            {
                Debug.Log("[DorisController] Hover entered");
            }
        }

        public void OnHoverExit()
        {
            isHovered = false;
            UpdateVisualState();

            if (debugLog)
            {
                Debug.Log("[DorisController] Hover exited");
            }
        }

        void OnClickedWithHands(GameObject interactor)
        {
            if (debugLog)
            {
                Debug.Log($"[DorisController] Player clicked Doris with bare hands");
            }

            // TODO: Open Doris UI panel (Task 9 - DorisHungerUI)
        }

        bool HandleToolInteraction(GameObject interactor, ToolDefinition tool)
        {
            if (debugLog)
            {
                Debug.Log($"[DorisController] Tool interaction with '{tool.displayName}' - not yet implemented");
            }

            return true;
        }

        #endregion

        #region Legacy Feeding (for backwards compatibility)

        /// <summary>
        /// Legacy feed method - use IFeedable.ReceiveFood for new code
        /// </summary>
        public bool FeedDoris(float nutritionValue, object foodSource = null)
        {
            if (hungerSystem == null || nutritionValue <= 0)
            {
                return false;
            }

            float actualReduction = hungerSystem.Feed(nutritionValue);

            if (actualReduction > 0)
            {
                if (definition != null && definition.feedSound != null)
                {
                    AudioSource.PlayClipAtPoint(definition.feedSound, transform.position);
                }

                OnDorisFed?.Invoke(nutritionValue);

                if (debugLog)
                {
                    Debug.Log($"[DorisController] Fed Doris with {nutritionValue:F1} nutrition. " +
                        $"Hunger reduced by {actualReduction:F1}");
                }

                return true;
            }

            return false;
        }

        #endregion

        #region Event Handlers

        void HandleHungerStateChanged(DorisHungerSystem.HungerState oldState, DorisHungerSystem.HungerState newState)
        {
            if (debugLog)
            {
                Debug.Log($"[DorisController] Hunger state changed: {oldState} -> {newState}");
            }

            UpdateVisualState();
            PlayStateChangeAudio(newState);
        }

        void HandleHungerChanged(float currentHunger, float maxHunger)
        {
            // Could update visual indicators here
        }

        void HandleHungerSystemFed(float amount)
        {
            // Reset starvation counter when fed
            ticksSinceLastPlantEat = 0;
        }

        void HandleStarvationTick()
        {
            if (definition == null) return;

            ticksSinceLastPlantEat++;

            if (ticksSinceLastPlantEat >= definition.ticksBetweenPlantEating)
            {
                ticksSinceLastPlantEat = 0;
                TryEatNearbyPlant();
            }
        }

        #endregion

        #region Plant Eating

        void TryEatNearbyPlant()
        {
            if (definition == null || multiTileEntity == null) return;
            if (GridPositionManager.Instance == null) return;

            var entitiesInRadius = GridPositionManager.Instance.GetEntitiesInRadius(
                multiTileEntity.AnchorPosition,
                definition.plantEatingRadius,
                true
            );

            PlantGrowth nearestPlant = null;
            float nearestDist = float.MaxValue;

            foreach (var entity in entitiesInRadius)
            {
                var plant = entity.GetComponent<PlantGrowth>();
                if (plant != null && (plant.CurrentState == PlantState.Growing || plant.CurrentState == PlantState.Mature))
                {
                    float dist = entity.Position.ManhattanDistance(multiTileEntity.AnchorPosition);
                    if (dist < nearestDist)
                    {
                        nearestDist = dist;
                        nearestPlant = plant;
                    }
                }
            }

            if (nearestPlant != null)
            {
                EatPlant(nearestPlant);
            }
        }

        void EatPlant(PlantGrowth plant)
        {
            if (plant == null) return;

            Debug.LogWarning($"[DorisController] Doris ate plant: {plant.gameObject.name}");

            if (definition != null && definition.eatPlantSound != null)
            {
                AudioSource.PlayClipAtPoint(definition.eatPlantSound, transform.position);
            }

            if (hungerSystem != null)
            {
                hungerSystem.OnAtePlant();
            }

            OnDorisAtePlant?.Invoke(plant);

            UnityEngine.Object.Destroy(plant.gameObject);
        }

        #endregion

        #region Visual Updates

        void UpdateVisualState()
        {
            if (hungerSystem == null || spriteRenderer == null) return;

            if (!isHovered)
            {
                switch (hungerSystem.CurrentState)
                {
                    case DorisHungerSystem.HungerState.Satisfied:
                        spriteRenderer.color = normalColor;
                        break;

                    case DorisHungerSystem.HungerState.Hungry:
                        spriteRenderer.color = Color.Lerp(normalColor, Color.yellow, 0.2f);
                        break;

                    case DorisHungerSystem.HungerState.Starving:
                        spriteRenderer.color = Color.Lerp(normalColor, Color.red, 0.3f);
                        break;
                }
            }
        }

        void PlayStateChangeAudio(DorisHungerSystem.HungerState newState)
        {
            if (definition == null) return;

            AudioClip clip = null;

            switch (newState)
            {
                case DorisHungerSystem.HungerState.Hungry:
                    clip = definition.hungrySound;
                    break;
                case DorisHungerSystem.HungerState.Starving:
                    clip = definition.hungrySound;
                    break;
            }

            if (clip != null)
            {
                AudioSource.PlayClipAtPoint(clip, transform.position);
            }
        }

        #endregion

        #region Public API

        public (float current, float max, float percent, DorisHungerSystem.HungerState state) GetHungerInfo()
        {
            if (hungerSystem == null)
            {
                return (0, 100, 0, DorisHungerSystem.HungerState.Satisfied);
            }

            return (
                hungerSystem.CurrentHunger,
                hungerSystem.MaxHunger,
                hungerSystem.HungerPercent,
                hungerSystem.CurrentState
            );
        }

        public void ResetForNewRound()
        {
            ticksSinceLastPlantEat = 0;

            if (hungerSystem != null)
            {
                hungerSystem.ResetHunger();
            }

            UpdateVisualState();

            if (debugLog)
            {
                Debug.Log("[DorisController] Reset for new round.");
            }
        }

        #endregion
    }
}