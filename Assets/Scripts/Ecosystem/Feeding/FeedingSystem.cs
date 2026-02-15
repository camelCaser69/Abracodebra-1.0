// FILE: Assets/Scripts/Ecosystem/Feeding/FeedingSystem.cs
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using WegoSystem;
using Abracodabra.UI.Genes;
using Abracodabra.UI.Toolkit;
using Abracodabra.Ecosystem.Feeding;

namespace Abracodabra.Ecosystem.Feeding
{
    /// <summary>
    /// Central system for handling feeding interactions.
    /// Detects right-clicks on feedable entities and coordinates with the food selection UI.
    /// Works with both FoodType ScriptableObjects and consumable ItemDefinitions.
    /// </summary>
    public class FeedingSystem : MonoBehaviour
    {
        public static FeedingSystem Instance { get; private set; }

        [Header("Range Configuration")]
        [Tooltip("If true, uses TileInteractionManager's hoverRadius. If false, uses manual range.")]
        [SerializeField] private bool useGameInteractionRange = true;

        [Tooltip("Manual feeding range (only used if useGameInteractionRange is false)")]
        [SerializeField] private float manualFeedingRange = 3f;

        [Header("State Restrictions")]
        [Tooltip("If true, only allow feeding during GrowthAndThreat phase. If false, always allow.")]
        [SerializeField] private bool restrictToGrowthPhase = true;

        [Header("Player Reference")]
        [SerializeField] private Transform player;
        [SerializeField] private GardenerController gardenerController;

        [Header("Debug")]
        [SerializeField] private bool debugLog = true;
        [SerializeField] private bool verboseHoverLog = false;

        // Events
        public event Action<IFeedable> OnFeedingStarted;
        public event Action<IFeedable, ConsumableData> OnFeedingCompleted;
        public event Action OnFeedingCancelled;

        // State
        private IFeedable currentTarget;
        private IFeedable hoveredFeedable;
        private Camera mainCamera;
        private bool isSelectingFood = false;

        // Cached feedables for hover detection
        private readonly List<IFeedable> registeredFeedables = new List<IFeedable>();

        /// <summary>
        /// Get the effective feeding range (from TileInteractionManager or manual setting)
        /// </summary>
        public float EffectiveFeedingRange
        {
            get
            {
                if (useGameInteractionRange && TileInteractionManager.Instance != null)
                {
                    return TileInteractionManager.Instance.hoverRadius;
                }
                return manualFeedingRange;
            }
        }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                if (debugLog) Debug.LogWarning("[FeedingSystem] Duplicate instance destroyed");
                Destroy(gameObject);
                return;
            }
            Instance = this;
            
            if (debugLog) Debug.Log("[FeedingSystem] Awake - Instance set");
        }

        void Start()
        {
            mainCamera = Camera.main;

            if (player == null || gardenerController == null)
            {
                gardenerController = FindFirstObjectByType<GardenerController>();
                if (gardenerController != null)
                {
                    player = gardenerController.transform;
                }
            }

            if (debugLog)
            {
                Debug.Log($"[FeedingSystem] Start - Camera: {mainCamera != null}, Player: {player != null}, GardenerController: {gardenerController != null}");
            }

            // Auto-register existing feedables in scene (delayed to ensure they've initialized)
            Invoke(nameof(AutoRegisterFeedables), 0.2f);
        }

        void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        void Update()
        {
            // Always detect right-click for debugging, but only process if allowed
            bool rightClickThisFrame = Input.GetMouseButtonDown(1);

            if (rightClickThisFrame && debugLog)
            {
                Debug.Log($"[FeedingSystem] Right-click detected! isSelectingFood={isSelectingFood}, " +
                          $"registeredCount={registeredFeedables.Count}, hoveredFeedable={hoveredFeedable?.FeedableName ?? "null"}");
            }

            // If popup is open, don't process any feeding input (popup handles its own closing)
            if (FoodSelectionPopup.IsBlockingInput)
                return;

            if (isSelectingFood)
                return;

            if (restrictToGrowthPhase)
            {
                if (RunManager.Instance == null)
                {
                    if (rightClickThisFrame && debugLog)
                        Debug.Log("[FeedingSystem] RunManager.Instance is null");
                }
                else if (RunManager.Instance.CurrentState != RunState.GrowthAndThreat)
                {
                    if (rightClickThisFrame && debugLog)
                        Debug.Log($"[FeedingSystem] Wrong state: {RunManager.Instance.CurrentState} (need GrowthAndThreat)");
                    return;
                }
            }

            UpdateHoveredFeedable();

            if (rightClickThisFrame)
            {
                HandleRightClickInput();
            }

            HandleKeyboardQuickFeed();
        }

        #region Feedable Registration

        public void RegisterFeedable(IFeedable feedable)
        {
            if (feedable != null && !registeredFeedables.Contains(feedable))
            {
                registeredFeedables.Add(feedable);
                if (debugLog) Debug.Log($"[FeedingSystem] Registered feedable: {feedable.FeedableName} (total: {registeredFeedables.Count})");
            }
        }

        public void UnregisterFeedable(IFeedable feedable)
        {
            if (feedable != null)
            {
                registeredFeedables.Remove(feedable);
                if (debugLog) Debug.Log($"[FeedingSystem] Unregistered feedable: {feedable.FeedableName}");
            }
        }

        private void AutoRegisterFeedables()
        {
            // Find all IFeedable in scene
            var allMonoBehaviours = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
            int count = 0;

            foreach (var mb in allMonoBehaviours)
            {
                if (mb is IFeedable feedable && !registeredFeedables.Contains(feedable))
                {
                    registeredFeedables.Add(feedable);
                    count++;
                    if (debugLog) Debug.Log($"[FeedingSystem] Auto-registered: {feedable.FeedableName}");
                }
            }

            if (debugLog) Debug.Log($"[FeedingSystem] Auto-registration complete: {count} new feedables (total: {registeredFeedables.Count})");
        }

        /// <summary>
        /// Force re-scan for feedables (call after spawning new entities)
        /// </summary>
        public void RefreshFeedables()
        {
            AutoRegisterFeedables();
        }

        #endregion

        #region Input Handling

        void UpdateHoveredFeedable()
        {
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
                if (mainCamera == null) return;
            }

            Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            mouseWorldPos.z = 0f;

            GridPosition mouseGridPos = GridPosition.Zero;

            if (TileInteractionManager.Instance != null && TileInteractionManager.Instance.interactionGrid != null)
            {
                Vector3Int cellPos = TileInteractionManager.Instance.interactionGrid.WorldToCell(mouseWorldPos);
                mouseGridPos = new GridPosition(cellPos);
            }

            IFeedable closestFeedable = null;
            float closestDistance = float.MaxValue;

            registeredFeedables.RemoveAll(f => f == null || (f as MonoBehaviour) == null);

            foreach (var feedable in registeredFeedables)
            {
                var feedableMono = feedable as MonoBehaviour;
                if (feedableMono == null || !feedableMono.gameObject.activeInHierarchy) continue;

                GridPosition feedableGridPos = GetFeedableGridPosition(feedable);
                int manhattanDist = mouseGridPos.ManhattanDistance(feedableGridPos);
                float worldDist = Vector3.Distance(mouseWorldPos, feedable.FeedPopupAnchor);

                if (verboseHoverLog && worldDist < 3f)
                {
                    Debug.Log($"[FeedingSystem] Checking {feedable.FeedableName}: gridDist={manhattanDist}, worldDist={worldDist:F2}");
                }

                // Only detect feedable if cursor is on the EXACT same grid cell
                if (manhattanDist == 0 && worldDist < closestDistance)
                {
                    closestDistance = worldDist;
                    closestFeedable = feedable;
                }
            }

            if (hoveredFeedable != closestFeedable)
            {
                hoveredFeedable = closestFeedable;

                if (debugLog && hoveredFeedable != null)
                {
                    Debug.Log($"[FeedingSystem] Now hovering: {hoveredFeedable.FeedableName}");
                }
            }
        }

        private GridPosition GetFeedableGridPosition(IFeedable feedable)
        {
            var mono = feedable as MonoBehaviour;
            if (mono == null) return GridPosition.Zero;

            // Check for GridEntity
            var gridEntity = mono.GetComponent<GridEntity>();
            if (gridEntity != null)
            {
                return gridEntity.Position;
            }

            // Check for MultiTileEntity (like Doris)
            var multiTile = mono.GetComponent<MultiTileEntity>();
            if (multiTile != null)
            {
                return multiTile.AnchorPosition;
            }

            // Fallback: convert world position to grid
            if (TileInteractionManager.Instance != null && TileInteractionManager.Instance.interactionGrid != null)
            {
                Vector3Int cell = TileInteractionManager.Instance.interactionGrid.WorldToCell(mono.transform.position);
                return new GridPosition(cell);
            }

            return GridPosition.Zero;
        }

        private void HandleRightClickInput()
        {
            if (debugLog) 
            {
                Debug.Log($"[FeedingSystem] HandleRightClickInput - hoveredFeedable: {hoveredFeedable?.FeedableName ?? "none"}");
            }

            if (hoveredFeedable == null)
            {
                if (debugLog) Debug.Log("[FeedingSystem] No feedable under cursor - right-click ignored");
                return;
            }

            // Check range to player using grid-based distance
            if (player != null && gardenerController != null)
            {
                GridPosition playerPos = gardenerController.GetCurrentGridPosition();
                GridPosition feedablePos = GetFeedableGridPosition(hoveredFeedable);
                int gridDistance = playerPos.ManhattanDistance(feedablePos);
                int maxRange = Mathf.CeilToInt(EffectiveFeedingRange);

                if (debugLog)
                {
                    Debug.Log($"[FeedingSystem] Distance check: player at {playerPos}, target at {feedablePos}, " +
                              $"distance={gridDistance}, maxRange={maxRange}");
                }

                if (gridDistance > maxRange)
                {
                    if (debugLog) Debug.Log($"[FeedingSystem] {hoveredFeedable.FeedableName} is too far to feed (dist {gridDistance} > max {maxRange})");
                    return;
                }
            }

            // Start feeding interaction
            if (debugLog) Debug.Log($"[FeedingSystem] Starting feeding interaction with {hoveredFeedable.FeedableName}");
            StartFeedingInteraction(hoveredFeedable);
        }

        private void HandleKeyboardQuickFeed()
        {
            if (hoveredFeedable == null) return;

            // Number keys 1-9 for quick-feed from hotbar
            for (int i = 0; i < 9; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                {
                    TryQuickFeedFromHotbar(hoveredFeedable, i);
                    break;
                }
            }

            // E key for self-feeding (player only)
            if (Input.GetKeyDown(KeyCode.E))
            {
                var playerFeedable = GetPlayerFeedable();
                if (playerFeedable != null)
                {
                    if (debugLog) Debug.Log("[FeedingSystem] E pressed - opening self-feed menu");
                    StartFeedingInteraction(playerFeedable);
                }
            }
        }

        private void TryQuickFeedFromHotbar(IFeedable target, int hotbarIndex)
        {
            if (!InventoryService.IsInitialized) return;

            var hotbarItems = InventoryService.GetHotbarItems();
            if (hotbarIndex >= hotbarItems.Count) return;

            var item = hotbarItems[hotbarIndex];
            if (item == null) return;

            // Try to create consumable data from the item
            ConsumableData consumable = GetConsumableFromItem(item);
            if (consumable == null)
            {
                if (debugLog) Debug.Log($"[FeedingSystem] Slot {hotbarIndex + 1} is not a consumable");
                return;
            }

            // Check if target accepts this food
            if (!target.CanAcceptFood(consumable))
            {
                if (debugLog) Debug.Log($"[FeedingSystem] {target.FeedableName} cannot eat {consumable.Name}");
                return;
            }

            // Execute quick feed
            ExecuteFeeding(target, consumable, hotbarIndex);
        }

        #endregion

        #region Feeding Execution

        public void StartFeedingInteraction(IFeedable target)
        {
            if (target == null)
            {
                if (debugLog) Debug.LogWarning("[FeedingSystem] StartFeedingInteraction called with null target");
                return;
            }

            currentTarget = target;
            isSelectingFood = true;

            if (debugLog) Debug.Log($"[FeedingSystem] Opening food selection for {target.FeedableName}");

            OnFeedingStarted?.Invoke(target);

            var availableFoods = GetAvailableFoodsForTarget(target);

            // Show popup even with no food (popup will display empty slots)
            if (debugLog)
            {
                if (availableFoods.Count == 0)
                    Debug.Log($"[FeedingSystem] No valid foods in inventory for {target.FeedableName} - showing empty popup");
                else
                    Debug.Log($"[FeedingSystem] Showing FoodSelectionPopup with {availableFoods.Count} foods");
            }

            if (FoodSelectionPopup.Instance != null)
            {
                FoodSelectionPopup.Instance.Show(target, availableFoods, OnFoodSelected, OnFoodSelectionCancelled);
            }
            else
            {
                Debug.LogError("[FeedingSystem] FoodSelectionPopup.Instance is null! Make sure FoodSelectionPopup exists in the scene.");
                CancelFeedingInteraction();
            }
        }

        private void OnFoodSelected(ConsumableData consumable, int inventoryIndex)
        {
            if (currentTarget == null || consumable == null)
            {
                CancelFeedingInteraction();
                return;
            }

            ExecuteFeeding(currentTarget, consumable, inventoryIndex);

            isSelectingFood = false;
            currentTarget = null;
        }

        private void OnFoodSelectionCancelled()
        {
            CancelFeedingInteraction();
        }

        private void CancelFeedingInteraction()
        {
            isSelectingFood = false;
            currentTarget = null;
            OnFeedingCancelled?.Invoke();

            if (debugLog) Debug.Log("[FeedingSystem] Feeding cancelled");
        }

        private void ExecuteFeeding(IFeedable target, ConsumableData consumable, int inventoryIndex)
        {
            if (target == null || consumable == null) return;

            // Feed the target
            float satiationApplied = target.ReceiveFood(consumable, player?.gameObject);

            // Remove food from inventory
            if (inventoryIndex >= 0)
            {
                InventoryService.RemoveItemAtIndex(inventoryIndex);
            }

            // Advance tick (feeding costs time)
            if (TickManager.Instance != null)
            {
                TickManager.Instance.AdvanceTick();
            }

            if (debugLog)
            {
                Debug.Log($"[FeedingSystem] Fed {consumable.Name} to {target.FeedableName}, " +
                          $"satiation: {satiationApplied:F1}");
            }

            OnFeedingCompleted?.Invoke(target, consumable);
        }

        #endregion

        #region Helpers

        private List<FoodSelectionPopup.FoodSlotData> GetAvailableFoodsForTarget(IFeedable target)
        {
            var result = new List<FoodSelectionPopup.FoodSlotData>();

            if (!InventoryService.IsInitialized)
            {
                if (debugLog) Debug.Log("[FeedingSystem] InventoryService not initialized");
                return result;
            }

            if (debugLog) Debug.Log($"[FeedingSystem] Scanning {InventoryService.TotalSlots} inventory slots for consumables...");

            for (int i = 0; i < InventoryService.TotalSlots; i++)
            {
                var item = InventoryService.GetItemAt(i);
                if (item == null) continue;

                ConsumableData consumable = GetConsumableFromItem(item);
                if (consumable == null)
                {
                    if (debugLog && verboseHoverLog)
                    {
                        Debug.Log($"[FeedingSystem] Slot {i}: {item.ItemDefinition?.itemName ?? "unknown"} - not consumable");
                    }
                    continue;
                }

                // Check if target can eat this food
                if (!target.CanAcceptFood(consumable))
                {
                    if (debugLog) Debug.Log($"[FeedingSystem] Slot {i}: {consumable.Name} - target cannot eat");
                    continue;
                }

                if (debugLog) Debug.Log($"[FeedingSystem] Slot {i}: {consumable.Name} - VALID (nutrition={consumable.NutritionValue})");

                result.Add(new FoodSelectionPopup.FoodSlotData
                {
                    Consumable = consumable,
                    InventoryIndex = i,
                    Icon = consumable.Icon,
                    StackCount = item.StackSize
                });
            }

            if (debugLog) Debug.Log($"[FeedingSystem] Found {result.Count} valid foods for {target.FeedableName}");
            return result;
        }

        /// <summary>
        /// Extract consumable data from a UIInventoryItem.
        /// Supports both FoodType ScriptableObjects and consumable ItemDefinitions.
        /// </summary>
        private ConsumableData GetConsumableFromItem(UIInventoryItem item)
        {
            if (item == null) return null;

            // Check if original data is directly a FoodType
            if (item.OriginalData is FoodType foodType)
            {
                return new ConsumableData(foodType);
            }

            // Check if it's an ItemDefinition that's consumable
            var itemDef = item.ItemDefinition;
            if (itemDef != null && itemDef.isConsumable)
            {
                // Check if there's a ResourceInstance with dynamic properties
                if (item.ResourceInstance != null)
                {
                    return new ConsumableData(item.ResourceInstance);
                }
                
                // Fallback to base ItemDefinition
                return new ConsumableData(itemDef);
            }

            // Check if OriginalData is an ItemInstance
            if (item.OriginalData is ItemInstance itemInstance)
            {
                if (itemInstance.definition?.isConsumable == true)
                {
                    return new ConsumableData(itemInstance);
                }
            }

            return null;
        }

        private IFeedable GetPlayerFeedable()
        {
            if (player == null) return null;

            // Check if player has IFeedable component (PlayerHungerSystem)
            return player.GetComponent<IFeedable>();
        }

        public IFeedable GetHoveredFeedable()
        {
            return hoveredFeedable;
        }

        public bool IsSelectingFood => isSelectingFood;

        public int RegisteredFeedableCount => registeredFeedables.Count;

        #endregion

        #region Debug

        [ContextMenu("Log Registered Feedables")]
        public void LogRegisteredFeedables()
        {
            Debug.Log($"[FeedingSystem] === Registered Feedables ({registeredFeedables.Count}) ===");
            foreach (var feedable in registeredFeedables)
            {
                var mono = feedable as MonoBehaviour;
                string status = mono != null && mono.gameObject.activeInHierarchy ? "active" : "inactive/null";
                Debug.Log($"  - {feedable?.FeedableName ?? "null"} ({status})");
            }
        }

        [ContextMenu("Force Refresh Feedables")]
        public void ForceRefreshFeedables()
        {
            registeredFeedables.Clear();
            AutoRegisterFeedables();
        }

        [ContextMenu("Log Inventory Consumables")]
        public void LogInventoryConsumables()
        {
            if (!InventoryService.IsInitialized)
            {
                Debug.Log("[FeedingSystem] InventoryService not initialized");
                return;
            }

            Debug.Log($"[FeedingSystem] === Inventory Consumables ({InventoryService.TotalSlots} slots) ===");
            
            for (int i = 0; i < InventoryService.TotalSlots; i++)
            {
                var item = InventoryService.GetItemAt(i);
                if (item == null) continue;

                var consumable = GetConsumableFromItem(item);
                if (consumable != null)
                {
                    Debug.Log($"  Slot {i}: {consumable}");
                }
                else
                {
                    string itemName = item.ItemDefinition?.itemName ?? item.OriginalData?.GetType().Name ?? "unknown";
                    bool isConsumable = item.ItemDefinition?.isConsumable ?? false;
                    float nutrition = item.ItemDefinition?.baseNutrition ?? 0;
                    Debug.Log($"  Slot {i}: {itemName} (isConsumable={isConsumable}, nutrition={nutrition}) - NOT VALID");
                }
            }
        }

        #endregion
    }
}