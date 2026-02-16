using WegoSystem;
using Abracodabra.UI.Genes;
using Abracodabra.UI.Toolkit;
using Abracodabra.Ecosystem.Feeding;
using Abracodabra.Genes.Components; // ✅ ADDED: For FruitConsumptionHandler
using System.Collections.Generic;
using UnityEngine;
using System;

namespace Abracodabra.Ecosystem.Feeding {
    public class FeedingSystem : MonoBehaviour {
        public static FeedingSystem Instance { get; set; }

        [Header("Range Configuration")]
        [Tooltip("If true, uses TileInteractionManager's hoverRadius. If false, uses manual range.")]
        [SerializeField] bool useGameInteractionRange = true;

        [Tooltip("Manual feeding range (only used if useGameInteractionRange is false)")]
        [SerializeField] float manualFeedingRange = 3f;

        [Header("State Restrictions")]
        [Tooltip("If true, only allow feeding during GrowthAndThreat phase. If false, always allow.")]
        [SerializeField] bool restrictToGrowthPhase = true;

        [Header("Player Reference")]
        [SerializeField] Transform player;
        [SerializeField] GardenerController gardenerController;

        [Header("Debug")]
        [SerializeField] bool debugLog = true;
        [SerializeField] bool verboseHoverLog = false;

        public event Action<IFeedable> OnFeedingStarted;
        public event Action<IFeedable, ConsumableData> OnFeedingCompleted;
        public event Action OnFeedingCancelled;

        IFeedable currentTarget;
        IFeedable hoveredFeedable;
        Camera mainCamera;
        bool isSelectingFood = false;

        readonly List<IFeedable> registeredFeedables = new List<IFeedable>();

        public float EffectiveFeedingRange {
            get {
                if (useGameInteractionRange && TileInteractionManager.Instance != null) {
                    return TileInteractionManager.Instance.hoverRadius;
                }
                return manualFeedingRange;
            }
        }

        void Awake() {
            if (Instance != null && Instance != this) {
                if (debugLog) Debug.LogWarning("[FeedingSystem] Duplicate instance destroyed");
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (debugLog) Debug.Log("[FeedingSystem] Awake - Instance set");
        }

        void Start() {
            mainCamera = Camera.main;

            if (player == null || gardenerController == null) {
                gardenerController = FindFirstObjectByType<GardenerController>();
                if (gardenerController != null) {
                    player = gardenerController.transform;
                }
            }

            if (debugLog) {
                Debug.Log($"[FeedingSystem] Start - Camera: {mainCamera != null}, Player: {player != null}, GardenerController: {gardenerController != null}");
            }

            Invoke(nameof(AutoRegisterFeedables), 0.2f);
        }

        void OnDestroy() {
            if (Instance == this) {
                Instance = null;
            }
        }

        void Update() {
            // Always detect right-click for debugging, but only process if allowed
            bool rightClickThisFrame = Input.GetMouseButtonDown(1);

            if (rightClickThisFrame && debugLog) {
                Debug.Log($"[FeedingSystem] Right-click detected! isSelectingFood={isSelectingFood}, " +
                          $"registeredCount={registeredFeedables.Count}, hoveredFeedable={hoveredFeedable?.FeedableName ?? "null"}");
            }

            if (FoodSelectionPopup.IsBlockingInput)
                return;

            if (isSelectingFood)
                return;

            if (restrictToGrowthPhase) {
                if (RunManager.Instance == null) {
                    if (rightClickThisFrame && debugLog)
                        Debug.Log("[FeedingSystem] RunManager.Instance is null");
                }
                else if (RunManager.Instance.CurrentState != RunState.GrowthAndThreat) {
                    if (rightClickThisFrame && debugLog)
                        Debug.Log($"[FeedingSystem] Wrong state: {RunManager.Instance.CurrentState} (need GrowthAndThreat)");
                    return;
                }
            }

            UpdateHoveredFeedable();

            if (rightClickThisFrame) {
                HandleRightClickInput();
            }

            HandleKeyboardQuickFeed();
        }

        public void RegisterFeedable(IFeedable feedable) {
            if (feedable != null && !registeredFeedables.Contains(feedable)) {
                registeredFeedables.Add(feedable);
                if (debugLog) Debug.Log($"[FeedingSystem] Registered feedable: {feedable.FeedableName} (total: {registeredFeedables.Count})");
            }
        }

        public void UnregisterFeedable(IFeedable feedable) {
            if (feedable != null) {
                registeredFeedables.Remove(feedable);
                if (debugLog) Debug.Log($"[FeedingSystem] Unregistered feedable: {feedable.FeedableName}");
            }
        }

        void AutoRegisterFeedables() {
            var allMonoBehaviours = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
            int count = 0;

            foreach (var mb in allMonoBehaviours) {
                if (mb is IFeedable feedable && !registeredFeedables.Contains(feedable)) {
                    registeredFeedables.Add(feedable);
                    count++;
                    if (debugLog) Debug.Log($"[FeedingSystem] Auto-registered: {feedable.FeedableName}");
                }
            }

            if (debugLog) Debug.Log($"[FeedingSystem] Auto-registration complete: {count} new feedables (total: {registeredFeedables.Count})");
        }

        public void RefreshFeedables() {
            AutoRegisterFeedables();
        }

        void UpdateHoveredFeedable() {
            if (mainCamera == null) {
                mainCamera = Camera.main;
                if (mainCamera == null) return;
            }

            Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            mouseWorldPos.z = 0f;

            GridPosition mouseGridPos = GridPosition.Zero;

            if (TileInteractionManager.Instance != null && TileInteractionManager.Instance.interactionGrid != null) {
                Vector3Int cellPos = TileInteractionManager.Instance.interactionGrid.WorldToCell(mouseWorldPos);
                mouseGridPos = new GridPosition(cellPos);
            }

            IFeedable closestFeedable = null;
            float closestDistance = float.MaxValue;

            registeredFeedables.RemoveAll(f => f == null || (f as MonoBehaviour) == null);

            foreach (var feedable in registeredFeedables) {
                var feedableMono = feedable as MonoBehaviour;
                if (feedableMono == null || !feedableMono.gameObject.activeInHierarchy) continue;

                GridPosition feedableGridPos = GetFeedableGridPosition(feedable);
                int manhattanDist = mouseGridPos.ManhattanDistance(feedableGridPos);
                float worldDist = Vector3.Distance(mouseWorldPos, feedable.FeedPopupAnchor);

                if (verboseHoverLog && worldDist < 3f) {
                    Debug.Log($"[FeedingSystem] Checking {feedable.FeedableName}: gridDist={manhattanDist}, worldDist={worldDist:F2}");
                }

                if (manhattanDist == 0 && worldDist < closestDistance) {
                    closestDistance = worldDist;
                    closestFeedable = feedable;
                }
            }

            if (hoveredFeedable != closestFeedable) {
                hoveredFeedable = closestFeedable;

                if (debugLog && hoveredFeedable != null) {
                    Debug.Log($"[FeedingSystem] Now hovering: {hoveredFeedable.FeedableName}");
                }
            }
        }

        GridPosition GetFeedableGridPosition(IFeedable feedable) {
            var mono = feedable as MonoBehaviour;
            if (mono == null) return GridPosition.Zero;

            var gridEntity = mono.GetComponent<GridEntity>();
            if (gridEntity != null) {
                return gridEntity.Position;
            }

            var multiTile = mono.GetComponent<MultiTileEntity>();
            if (multiTile != null) {
                return multiTile.AnchorPosition;
            }

            if (TileInteractionManager.Instance != null && TileInteractionManager.Instance.interactionGrid != null) {
                Vector3Int cell = TileInteractionManager.Instance.interactionGrid.WorldToCell(mono.transform.position);
                return new GridPosition(cell);
            }

            return GridPosition.Zero;
        }

        void HandleRightClickInput() {
            if (debugLog) {
                Debug.Log($"[FeedingSystem] HandleRightClickInput - hoveredFeedable: {hoveredFeedable?.FeedableName ?? "none"}");
            }

            if (hoveredFeedable == null) {
                if (debugLog) Debug.Log("[FeedingSystem] No feedable under cursor - right-click ignored");
                return;
            }

            if (player != null && gardenerController != null) {
                GridPosition playerPos = gardenerController.GetCurrentGridPosition();
                GridPosition feedablePos = GetFeedableGridPosition(hoveredFeedable);
                int gridDistance = playerPos.ManhattanDistance(feedablePos);
                int maxRange = Mathf.CeilToInt(EffectiveFeedingRange);

                if (debugLog) {
                    Debug.Log($"[FeedingSystem] Distance check: player at {playerPos}, target at {feedablePos}, " +
                              $"distance={gridDistance}, maxRange={maxRange}");
                }

                if (gridDistance > maxRange) {
                    if (debugLog) Debug.Log($"[FeedingSystem] {hoveredFeedable.FeedableName} is too far to feed (dist {gridDistance} > max {maxRange})");
                    return;
                }
            }

            if (debugLog) Debug.Log($"[FeedingSystem] Starting feeding interaction with {hoveredFeedable.FeedableName}");
            StartFeedingInteraction(hoveredFeedable);
        }

        void HandleKeyboardQuickFeed() {
            if (hoveredFeedable == null) return;

            for (int i = 0; i < 9; i++) {
                if (Input.GetKeyDown(KeyCode.Alpha1 + i)) {
                    TryQuickFeedFromHotbar(hoveredFeedable, i);
                    break;
                }
            }

            if (Input.GetKeyDown(KeyCode.E)) {
                var playerFeedable = GetPlayerFeedable();
                if (playerFeedable != null) {
                    if (debugLog) Debug.Log("[FeedingSystem] E pressed - opening self-feed menu");
                    StartFeedingInteraction(playerFeedable);
                }
            }
        }

        void TryQuickFeedFromHotbar(IFeedable target, int hotbarIndex) {
            if (!InventoryService.IsInitialized) return;

            var hotbarItems = InventoryService.GetHotbarItems();
            if (hotbarIndex >= hotbarItems.Count) return;

            var item = hotbarItems[hotbarIndex];
            if (item == null) return;

            ConsumableData consumable = GetConsumableFromItem(item);
            if (consumable == null) {
                if (debugLog) Debug.Log($"[FeedingSystem] Slot {hotbarIndex + 1} is not a consumable");
                return;
            }

            if (!target.CanAcceptFood(consumable)) {
                if (debugLog) Debug.Log($"[FeedingSystem] {target.FeedableName} cannot eat {consumable.Name}");
                return;
            }

            ExecuteFeeding(target, consumable, hotbarIndex);
        }

        public void StartFeedingInteraction(IFeedable target) {
            if (target == null) {
                if (debugLog) Debug.LogWarning("[FeedingSystem] StartFeedingInteraction called with null target");
                return;
            }

            currentTarget = target;
            isSelectingFood = true;

            if (debugLog) Debug.Log($"[FeedingSystem] Opening food selection for {target.FeedableName}");

            OnFeedingStarted?.Invoke(target);

            var availableFoods = GetAvailableFoodsForTarget(target);

            if (debugLog) {
                if (availableFoods.Count == 0)
                    Debug.Log($"[FeedingSystem] No valid foods in inventory for {target.FeedableName} - showing empty popup");
                else
                    Debug.Log($"[FeedingSystem] Showing FoodSelectionPopup with {availableFoods.Count} foods");
            }

            if (FoodSelectionPopup.Instance != null) {
                FoodSelectionPopup.Instance.Show(target, availableFoods, OnFoodSelected, OnFoodSelectionCancelled);
            }
            else {
                Debug.LogError("[FeedingSystem] FoodSelectionPopup.Instance is null! Make sure FoodSelectionPopup exists in the scene.");
                CancelFeedingInteraction();
            }
        }

        void OnFoodSelected(ConsumableData consumable, int inventoryIndex) {
            if (currentTarget == null || consumable == null) {
                CancelFeedingInteraction();
                return;
            }

            ExecuteFeeding(currentTarget, consumable, inventoryIndex);

            isSelectingFood = false;
            currentTarget = null;
        }

        void OnFoodSelectionCancelled() {
            CancelFeedingInteraction();
        }

        void CancelFeedingInteraction() {
            isSelectingFood = false;
            currentTarget = null;
            OnFeedingCancelled?.Invoke();

            if (debugLog) Debug.Log("[FeedingSystem] Feeding cancelled");
        }

        void ExecuteFeeding(IFeedable target, ConsumableData consumable, int inventoryIndex) {
            if (target == null || consumable == null) return;

            float satiationApplied = target.ReceiveFood(consumable, player?.gameObject);

            // ✅ ADDED: Execute payloads attached to the food
            if (consumable.Payloads != null && consumable.Payloads.Count > 0) {
                if (target is MonoBehaviour targetMono) {
                    FruitConsumptionHandler.Consume(consumable.Payloads, targetMono.gameObject, null);
                }
            }

            if (inventoryIndex >= 0) {
                InventoryService.RemoveItemAtIndex(inventoryIndex);
            }

            if (TickManager.Instance != null) {
                TickManager.Instance.AdvanceTick();
            }

            if (debugLog) {
                Debug.Log($"[FeedingSystem] Fed {consumable.Name} to {target.FeedableName}, " +
                          $"satiation: {satiationApplied:F1}");
            }

            OnFeedingCompleted?.Invoke(target, consumable);
        }

        List<FoodSelectionPopup.FoodSlotData> GetAvailableFoodsForTarget(IFeedable target) {
            var result = new List<FoodSelectionPopup.FoodSlotData>();

            if (!InventoryService.IsInitialized) {
                if (debugLog) Debug.Log("[FeedingSystem] InventoryService not initialized");
                return result;
            }

            if (debugLog) Debug.Log($"[FeedingSystem] Scanning {InventoryService.TotalSlots} inventory slots for consumables...");

            for (int i = 0; i < InventoryService.TotalSlots; i++) {
                var item = InventoryService.GetItemAt(i);
                if (item == null) continue;

                ConsumableData consumable = GetConsumableFromItem(item);
                if (consumable == null) {
                    if (debugLog && verboseHoverLog) {
                        Debug.Log($"[FeedingSystem] Slot {i}: {item.ItemDefinition?.itemName ?? "unknown"} - not consumable");
                    }
                    continue;
                }

                if (!target.CanAcceptFood(consumable)) {
                    if (debugLog) Debug.Log($"[FeedingSystem] Slot {i}: {consumable.Name} - target cannot eat");
                    continue;
                }

                if (debugLog) Debug.Log($"[FeedingSystem] Slot {i}: {consumable.Name} - VALID (nutrition={consumable.NutritionValue})");

                result.Add(new FoodSelectionPopup.FoodSlotData {
                    Consumable = consumable,
                    InventoryIndex = i,
                    Icon = consumable.Icon,
                    StackCount = item.StackSize
                });
            }

            if (debugLog) Debug.Log($"[FeedingSystem] Found {result.Count} valid foods for {target.FeedableName}");
            return result;
        }

        ConsumableData GetConsumableFromItem(UIInventoryItem item) {
            if (item == null) return null;

            if (item.OriginalData is FoodType foodType) {
                return new ConsumableData(foodType);
            }

            var itemDef = item.ItemDefinition;
            if (itemDef != null && itemDef.isConsumable) {
                if (item.ResourceInstance != null) {
                    return new ConsumableData(item.ResourceInstance);
                }

                return new ConsumableData(itemDef);
            }

            if (item.OriginalData is ItemInstance itemInstance) {
                if (itemInstance.definition?.isConsumable == true) {
                    return new ConsumableData(itemInstance);
                }
            }

            return null;
        }

        IFeedable GetPlayerFeedable() {
            if (player == null) return null;

            return player.GetComponent<IFeedable>();
        }

        public IFeedable GetHoveredFeedable() {
            return hoveredFeedable;
        }

        public bool IsSelectingFood => isSelectingFood;

        public int RegisteredFeedableCount => registeredFeedables.Count;

        public void LogRegisteredFeedables() {
            Debug.Log($"[FeedingSystem] === Registered Feedables ({registeredFeedables.Count}) ===");
            foreach (var feedable in registeredFeedables) {
                var mono = feedable as MonoBehaviour;
                string status = mono != null && mono.gameObject.activeInHierarchy ? "active" : "inactive/null";
                Debug.Log($"  - {feedable?.FeedableName ?? "null"} ({status})");
            }
        }

        public void ForceRefreshFeedables() {
            registeredFeedables.Clear();
            AutoRegisterFeedables();
        }

        public void LogInventoryConsumables() {
            if (!InventoryService.IsInitialized) {
                Debug.Log("[FeedingSystem] InventoryService not initialized");
                return;
            }

            Debug.Log($"[FeedingSystem] === Inventory Consumables ({InventoryService.TotalSlots} slots) ===");

            for (int i = 0; i < InventoryService.TotalSlots; i++) {
                var item = InventoryService.GetItemAt(i);
                if (item == null) continue;

                var consumable = GetConsumableFromItem(item);
                if (consumable != null) {
                    Debug.Log($"  Slot {i}: {consumable}");
                }
                else {
                    string itemName = item.ItemDefinition?.itemName ?? item.OriginalData?.GetType().Name ?? "unknown";
                    bool isConsumable = item.ItemDefinition?.isConsumable ?? false;
                    float nutrition = item.ItemDefinition?.baseNutrition ?? 0;
                    Debug.Log($"  Slot {i}: {itemName} (isConsumable={isConsumable}, nutrition={nutrition}) - NOT VALID");
                }
            }
        }

    }
}