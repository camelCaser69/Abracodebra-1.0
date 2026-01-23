using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Collections.Generic;

namespace Abracodabra.Ecosystem.Feeding
{
    [RequireComponent(typeof(UIDocument))]
    public class FoodSelectionPopup : MonoBehaviour
    {
        public static FoodSelectionPopup Instance { get; private set; }

        [Header("Grid Configuration")]
        [SerializeField] int gridColumns = 3;
        [SerializeField] int gridRows = 3;
        [SerializeField] float slotSize = 64f;
        [SerializeField] float slotSpacing = 4f;

        [Header("Styling")]
        [SerializeField] Color backgroundColor = new Color(0.15f, 0.15f, 0.15f, 0.95f);
        [SerializeField] Color slotBackgroundColor = new Color(0.25f, 0.25f, 0.25f, 1f);
        [SerializeField] Color slotHoverColor = new Color(0.4f, 0.4f, 0.4f, 1f);
        [SerializeField] Color slotBorderColor = new Color(0.5f, 0.5f, 0.5f, 1f);
        [SerializeField] Color emptySlotBackgroundColor = new Color(0.15f, 0.15f, 0.15f, 0.5f);
        [SerializeField] Color emptySlotBorderColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
        [SerializeField] int borderRadius = 8;

        [Header("Debug")]
        [SerializeField] bool debugLog = true;

        UIDocument uiDocument;
        VisualElement rootElement;
        VisualElement popupContainer;
        VisualElement gridContainer;
        Label titleLabel;
        Label emptyLabel;

        bool isVisible = false;
        bool isInitialized = false;
        IFeedable currentTarget;
        List<FoodSlotData> currentFoods;
        Action<ConsumableData, int> onFoodSelected;
        Action onCancelled;

        List<VisualElement> slotElements = new List<VisualElement>();

        public int GridColumns => gridColumns;
        public int GridRows => gridRows;
        public int TotalSlots => gridColumns * gridRows;

        public class FoodSlotData
        {
            public ConsumableData Consumable;
            public int InventoryIndex;
            public Sprite Icon;
            public int StackCount;
        }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            SetupUIDocument();
        }

        void SetupUIDocument()
        {
            uiDocument = GetComponent<UIDocument>();

            if (uiDocument.panelSettings == null)
            {
                var existingPanelSettings = FindExistingPanelSettings();
                if (existingPanelSettings != null)
                {
                    uiDocument.panelSettings = existingPanelSettings;
                    if (debugLog) Debug.Log($"[FoodSelectionPopup] Auto-assigned PanelSettings from scene");
                }
                else
                {
                    Debug.LogError("[FoodSelectionPopup] No PanelSettings found in scene! Please assign one to any UIDocument.");
                    return;
                }
            }

            uiDocument.sortingOrder = 100;
        }

        PanelSettings FindExistingPanelSettings()
        {
            var allDocs = FindObjectsByType<UIDocument>(FindObjectsSortMode.None);
            foreach (var doc in allDocs)
            {
                if (doc != uiDocument && doc.panelSettings != null)
                {
                    return doc.panelSettings;
                }
            }
            return null;
        }

        void Start()
        {
            StartCoroutine(DelayedInit());
        }

        System.Collections.IEnumerator DelayedInit()
        {
            yield return null;
            yield return new WaitForEndOfFrame();

            if (uiDocument == null || uiDocument.rootVisualElement == null)
            {
                Debug.LogError("[FoodSelectionPopup] UIDocument not ready after delay!");
                yield break;
            }

            CreateUI();
            Hide();
            isInitialized = true;

            if (debugLog) Debug.Log("[FoodSelectionPopup] Initialization complete");
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
            if (!isVisible) return;

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Cancel();
            }

            if (Input.GetMouseButtonDown(1))
            {
                Cancel();
            }

            if (Input.GetMouseButtonDown(0) && !IsMouseOverPopup())
            {
                Cancel();
            }
        }

        void CreateUI()
        {
            rootElement = uiDocument.rootVisualElement;
            rootElement.Clear();

            rootElement.style.position = Position.Absolute;
            rootElement.style.left = 0;
            rootElement.style.top = 0;
            rootElement.style.right = 0;
            rootElement.style.bottom = 0;
            rootElement.pickingMode = PickingMode.Ignore;

            popupContainer = new VisualElement();
            popupContainer.name = "food-selection-popup";
            popupContainer.style.position = Position.Absolute;
            popupContainer.style.backgroundColor = backgroundColor;
            popupContainer.style.borderTopLeftRadius = borderRadius;
            popupContainer.style.borderTopRightRadius = borderRadius;
            popupContainer.style.borderBottomLeftRadius = borderRadius;
            popupContainer.style.borderBottomRightRadius = borderRadius;
            popupContainer.style.borderTopWidth = 2;
            popupContainer.style.borderBottomWidth = 2;
            popupContainer.style.borderLeftWidth = 2;
            popupContainer.style.borderRightWidth = 2;
            popupContainer.style.borderTopColor = new Color(0.4f, 0.4f, 0.5f, 1f);
            popupContainer.style.borderBottomColor = new Color(0.4f, 0.4f, 0.5f, 1f);
            popupContainer.style.borderLeftColor = new Color(0.4f, 0.4f, 0.5f, 1f);
            popupContainer.style.borderRightColor = new Color(0.4f, 0.4f, 0.5f, 1f);
            popupContainer.style.paddingTop = 8;
            popupContainer.style.paddingBottom = 8;
            popupContainer.style.paddingLeft = 8;
            popupContainer.style.paddingRight = 8;
            popupContainer.pickingMode = PickingMode.Position;
            popupContainer.style.display = DisplayStyle.None;
            rootElement.Add(popupContainer);

            titleLabel = new Label("Feed");
            titleLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            titleLabel.style.fontSize = 14;
            titleLabel.style.color = Color.white;
            titleLabel.style.marginBottom = 8;
            titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            popupContainer.Add(titleLabel);

            gridContainer = new VisualElement();
            gridContainer.name = "food-grid";
            gridContainer.style.flexDirection = FlexDirection.Row;
            gridContainer.style.flexWrap = Wrap.Wrap;
            gridContainer.style.width = (slotSize + slotSpacing) * gridColumns;
            popupContainer.Add(gridContainer);

            emptyLabel = new Label("No food available");
            emptyLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            emptyLabel.style.fontSize = 11;
            emptyLabel.style.color = new Color(0.6f, 0.6f, 0.6f, 1f);
            emptyLabel.style.marginTop = 4;
            emptyLabel.style.display = DisplayStyle.None;
            popupContainer.Add(emptyLabel);

            if (debugLog) Debug.Log("[FoodSelectionPopup] UI created");
        }

        void PopulateGrid(List<FoodSlotData> foods)
        {
            if (gridContainer == null) return;

            gridContainer.Clear();
            slotElements.Clear();

            int totalSlots = gridColumns * gridRows;
            int foodCount = foods?.Count ?? 0;

            for (int i = 0; i < totalSlots; i++)
            {
                VisualElement slot;

                if (i < foodCount && foods[i] != null)
                {
                    slot = CreateFoodSlot(foods[i], i);
                }
                else
                {
                    slot = CreateEmptySlot(i);
                }

                gridContainer.Add(slot);
                slotElements.Add(slot);
            }

            gridContainer.style.width = (slotSize + slotSpacing) * gridColumns;

            if (emptyLabel != null)
            {
                emptyLabel.style.display = (foodCount == 0) ? DisplayStyle.Flex : DisplayStyle.None;
            }

            if (debugLog) Debug.Log($"[FoodSelectionPopup] Populated grid: {foodCount} foods, {totalSlots - foodCount} empty slots");
        }

        VisualElement CreateEmptySlot(int index)
        {
            var slot = new VisualElement();
            slot.name = $"empty-slot-{index}";
            slot.style.width = slotSize;
            slot.style.height = slotSize;
            slot.style.marginRight = slotSpacing;
            slot.style.marginBottom = slotSpacing;
            slot.style.backgroundColor = emptySlotBackgroundColor;
            slot.style.borderTopLeftRadius = 4;
            slot.style.borderTopRightRadius = 4;
            slot.style.borderBottomLeftRadius = 4;
            slot.style.borderBottomRightRadius = 4;
            slot.style.borderTopWidth = 1;
            slot.style.borderBottomWidth = 1;
            slot.style.borderLeftWidth = 1;
            slot.style.borderRightWidth = 1;
            slot.style.borderTopColor = emptySlotBorderColor;
            slot.style.borderBottomColor = emptySlotBorderColor;
            slot.style.borderLeftColor = emptySlotBorderColor;
            slot.style.borderRightColor = emptySlotBorderColor;
            slot.style.alignItems = Align.Center;
            slot.style.justifyContent = Justify.Center;
            slot.pickingMode = PickingMode.Ignore;

            return slot;
        }

        VisualElement CreateFoodSlot(FoodSlotData foodData, int index)
        {
            var slot = new VisualElement();
            slot.name = $"food-slot-{index}";
            slot.style.width = slotSize;
            slot.style.height = slotSize;
            slot.style.marginRight = slotSpacing;
            slot.style.marginBottom = slotSpacing;
            slot.style.backgroundColor = slotBackgroundColor;
            slot.style.borderTopLeftRadius = 4;
            slot.style.borderTopRightRadius = 4;
            slot.style.borderBottomLeftRadius = 4;
            slot.style.borderBottomRightRadius = 4;
            slot.style.borderTopWidth = 1;
            slot.style.borderBottomWidth = 1;
            slot.style.borderLeftWidth = 1;
            slot.style.borderRightWidth = 1;
            slot.style.borderTopColor = slotBorderColor;
            slot.style.borderBottomColor = slotBorderColor;
            slot.style.borderLeftColor = slotBorderColor;
            slot.style.borderRightColor = slotBorderColor;
            slot.style.alignItems = Align.Center;
            slot.style.justifyContent = Justify.Center;
            slot.pickingMode = PickingMode.Position;

            if (foodData.Icon != null)
            {
                var icon = new VisualElement();
                icon.style.width = slotSize - 16;
                icon.style.height = slotSize - 16;
                icon.style.backgroundImage = new StyleBackground(foodData.Icon);
                // FIX: Use backgroundSize instead of deprecated unityBackgroundScaleMode
                icon.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Contain);
                icon.pickingMode = PickingMode.Ignore;
                slot.Add(icon);
            }
            else
            {
                var nameLabel = new Label(foodData.Consumable?.Name ?? "???");
                nameLabel.style.fontSize = 10;
                nameLabel.style.color = Color.white;
                nameLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
                nameLabel.pickingMode = PickingMode.Ignore;
                slot.Add(nameLabel);
            }

            if (foodData.StackCount > 1)
            {
                var countBadge = new Label(foodData.StackCount.ToString());
                countBadge.style.position = Position.Absolute;
                countBadge.style.right = 2;
                countBadge.style.bottom = 2;
                countBadge.style.fontSize = 10;
                countBadge.style.color = Color.white;
                countBadge.style.backgroundColor = new Color(0, 0, 0, 0.7f);
                countBadge.style.paddingLeft = 3;
                countBadge.style.paddingRight = 3;
                countBadge.style.paddingTop = 1;
                countBadge.style.paddingBottom = 1;
                countBadge.style.borderTopLeftRadius = 3;
                countBadge.style.borderTopRightRadius = 3;
                countBadge.style.borderBottomLeftRadius = 3;
                countBadge.style.borderBottomRightRadius = 3;
                countBadge.pickingMode = PickingMode.Ignore;
                slot.Add(countBadge);
            }

            slot.RegisterCallback<MouseEnterEvent>(evt => slot.style.backgroundColor = slotHoverColor);
            slot.RegisterCallback<MouseLeaveEvent>(evt => slot.style.backgroundColor = slotBackgroundColor);
            slot.RegisterCallback<ClickEvent>(evt =>
            {
                if (debugLog) Debug.Log($"[FoodSelectionPopup] Selected: {foodData.Consumable?.Name}");
                SelectFood(foodData);
            });

            slot.tooltip = $"{foodData.Consumable?.Name ?? "Unknown"}\nNutrition: {foodData.Consumable?.NutritionValue ?? 0:F0}";

            return slot;
        }

        public void Show(IFeedable target, List<FoodSlotData> foods, Action<ConsumableData, int> onSelected, Action onCancel)
        {
            if (target == null)
            {
                if (debugLog) Debug.LogWarning("[FoodSelectionPopup] Show called with null target");
                onCancel?.Invoke();
                return;
            }

            if (!isInitialized || popupContainer == null)
            {
                if (debugLog) Debug.LogWarning("[FoodSelectionPopup] Not initialized yet, retrying...");

                if (uiDocument != null && uiDocument.rootVisualElement != null)
                {
                    CreateUI();
                    isInitialized = true;
                }
                else
                {
                    Debug.LogError("[FoodSelectionPopup] Cannot initialize - UIDocument not ready");
                    onCancel?.Invoke();
                    return;
                }
            }

            currentTarget = target;
            currentFoods = foods ?? new List<FoodSlotData>();
            onFoodSelected = onSelected;
            onCancelled = onCancel;

            if (titleLabel != null)
            {
                titleLabel.text = $"Feed {target.FeedableName}";
            }

            PopulateGrid(currentFoods);
            PositionNearTarget(target);

            popupContainer.style.display = DisplayStyle.Flex;
            isVisible = true;

            if (debugLog) Debug.Log($"[FoodSelectionPopup] Showing for {target.FeedableName} with {currentFoods.Count} foods");
        }

        public void Hide()
        {
            if (popupContainer != null)
            {
                popupContainer.style.display = DisplayStyle.None;
            }
            isVisible = false;
            currentTarget = null;
            currentFoods = null;
            onFoodSelected = null;
            onCancelled = null;
        }

        public void Cancel()
        {
            if (debugLog) Debug.Log("[FoodSelectionPopup] Cancelled");
            var callback = onCancelled;
            Hide();
            callback?.Invoke();
        }

        public bool IsVisible => isVisible;

        void PositionNearTarget(IFeedable target)
        {
            if (Camera.main == null) return;

            Vector3 worldPos = target.FeedPopupAnchor;
            Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos);

            float uiX = screenPos.x;
            float uiY = Screen.height - screenPos.y;

            float popupWidth = (slotSize + slotSpacing) * gridColumns + 16;
            float popupHeight = (slotSize + slotSpacing) * gridRows + 40;

            float finalX = uiX - popupWidth / 2;
            float finalY = uiY - popupHeight - 20;

            finalX = Mathf.Clamp(finalX, 10, Screen.width - popupWidth - 10);
            finalY = Mathf.Clamp(finalY, 10, Screen.height - popupHeight - 10);

            popupContainer.style.left = finalX;
            popupContainer.style.top = finalY;
        }

        void SelectFood(FoodSlotData foodData)
        {
            var callback = onFoodSelected;
            var consumable = foodData.Consumable;
            int index = foodData.InventoryIndex;

            Hide();
            callback?.Invoke(consumable, index);
        }

        bool IsMouseOverPopup()
        {
            if (popupContainer == null || !isVisible) return false;

            Vector2 mousePos = Input.mousePosition;
            float uiY = Screen.height - mousePos.y;

            var rect = popupContainer.worldBound;
            return rect.Contains(new Vector2(mousePos.x, uiY));
        }

        /// <summary>
        /// Dynamically resize the grid (for perk scaling)
        /// </summary>
        public void SetGridSize(int columns, int rows)
        {
            gridColumns = Mathf.Max(1, columns);
            gridRows = Mathf.Max(1, rows);

            if (gridContainer != null)
            {
                gridContainer.style.width = (slotSize + slotSpacing) * gridColumns;
            }

            if (debugLog) Debug.Log($"[FoodSelectionPopup] Grid resized to {gridColumns}x{gridRows}");
        }
    }
}