using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using Abracodabra.UI.Genes;

namespace Abracodabra.UI.Genes
{
    public class InventoryBarController : MonoBehaviour
    {
        public static InventoryBarController Instance { get; private set; }

        [Header("Configuration")]
        [SerializeField] private int maxSlots = 10;
        [SerializeField] private GameObject inventoryItemViewPrefab;

        [Header("Component References")]
        [SerializeField] private InventoryGridController inventoryGridController;
        [SerializeField] private RectTransform inventoryBarPanelRect; // The parent RectTransform for rebuilding
        [SerializeField] private Transform cellContainer; // <-- The CRUCIAL reference, now restored
        [SerializeField] private GameObject selectionHighlight;

        private List<GameObject> activeItemSlots = new List<GameObject>();
        private int selectedSlot = 0;
        private Coroutine updateHighlightCoroutine;

        public InventoryBarItem SelectedItem { get; private set; }
        public event System.Action<InventoryBarItem> OnSelectionChanged;

        public class InventoryBarItemComponent : MonoBehaviour { public InventoryBarItem item; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            selectedSlot = 0;
        }

        private void Start()
        {
            // --- VALIDATION ---
            if (inventoryGridController == null) Debug.LogError($"[{nameof(InventoryBarController)}] InventoryGridController not assigned!", this);
            if (inventoryItemViewPrefab == null) Debug.LogError($"[{nameof(InventoryBarController)}] InventoryItemViewPrefab not assigned!", this);
            if (inventoryBarPanelRect == null) Debug.LogError($"[{nameof(InventoryBarController)}] Inventory Bar Panel Rect not assigned!", this);
            if (cellContainer == null) Debug.LogError($"[{nameof(InventoryBarController)}] Cell Container not assigned!", this);

            inventoryGridController.OnInventoryChanged += HandleInventoryChanged;
            
            if (selectionHighlight != null)
            {
                selectionHighlight.SetActive(false);
            }

            gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (inventoryGridController != null)
            {
                inventoryGridController.OnInventoryChanged -= HandleInventoryChanged;
            }
        }

        private void UpdateBarDisplay()
        {
            foreach (var slot in activeItemSlots)
            {
                Destroy(slot);
            }
            activeItemSlots.Clear();

            if (inventoryGridController == null || inventoryItemViewPrefab == null || cellContainer == null) return;
            
            var allItems = inventoryGridController.GetAllItems();
            int itemsToDisplay = Mathf.Min(allItems.Count, maxSlots);

            for (int i = 0; i < itemsToDisplay; i++)
            {
                var item = allItems[i];
                if (item == null || !item.IsValid()) continue;
                
                // --- THIS IS THE FIX ---
                // Instantiate items as children of the correct 'cellContainer' transform.
                GameObject itemViewGO = Instantiate(inventoryItemViewPrefab, cellContainer);
                var itemView = itemViewGO.GetComponentInChildren<ItemView>();
                
                if (itemView == null)
                {
                    Debug.LogError($"The assigned InventoryItemViewPrefab is missing the ItemView component!", itemViewGO);
                    Destroy(itemViewGO);
                    continue;
                }
                
                itemViewGO.SetActive(true);

                switch (item.Type)
                {
                    case InventoryBarItem.ItemType.Gene: itemView.InitializeAsGene(item.GeneInstance); break;
                    case InventoryBarItem.ItemType.Seed: itemView.InitializeAsSeed(item.SeedTemplate); break;
                    case InventoryBarItem.ItemType.Tool: itemView.InitializeAsTool(item.ToolDefinition); break;
                }

                var barItemComponent = itemViewGO.AddComponent<InventoryBarItemComponent>();
                barItemComponent.item = item;
                
                activeItemSlots.Add(itemViewGO);
            }
            
            if (inventoryBarPanelRect != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(inventoryBarPanelRect);
            }
        }

        private void HandleInventoryChanged()
        {
            if (gameObject.activeInHierarchy)
            {
                RefreshBar();
            }
        }

        private void Update()
        {
            if (!gameObject.activeInHierarchy) return;
            HandleNumberKeyInput();
        }

        public void ShowBar()
        {
            gameObject.SetActive(true);
            RefreshBar();
            SelectSlotByIndex(0);
        }

        public void HideBar()
        {
            gameObject.SetActive(false);
            if (selectionHighlight != null)
            {
                selectionHighlight.SetActive(false);
            }
        }

        private void RefreshBar()
        {
            UpdateBarDisplay();
            SelectSlot(Mathf.Clamp(selectedSlot, 0, activeItemSlots.Count - 1));
        }

        private void HandleNumberKeyInput()
        {
            for (int i = 1; i <= 9; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha0 + i)) { SelectSlot(i - 1); return; }
            }
            if (Input.GetKeyDown(KeyCode.Alpha0)) { SelectSlot(9); }
        }

        public void SelectSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= activeItemSlots.Count)
            {
                SelectedItem = null;
                UpdateSelection();
                return;
            }
            
            selectedSlot = slotIndex;
            var slot = activeItemSlots[slotIndex];
            var itemComponent = slot.GetComponentInChildren<InventoryBarItemComponent>();
            SelectedItem = itemComponent?.item;
            
            UpdateSelection();
        }

        private void UpdateSelection()
        {
            if (updateHighlightCoroutine != null)
            {
                StopCoroutine(updateHighlightCoroutine);
            }
            updateHighlightCoroutine = StartCoroutine(UpdateSelectionVisualsAfterFrame());

            OnSelectionChanged?.Invoke(SelectedItem);
        }

        private IEnumerator UpdateSelectionVisualsAfterFrame()
        {
            yield return null;

            if (selectionHighlight != null)
            {
                bool itemIsValid = SelectedItem != null && SelectedItem.IsValid() && selectedSlot < activeItemSlots.Count;
                selectionHighlight.SetActive(itemIsValid);

                if (itemIsValid)
                {
                    selectionHighlight.transform.position = activeItemSlots[selectedSlot].transform.position;
                }
            }
            updateHighlightCoroutine = null;
        }

        public void SelectSlotByIndex(int slotIndex)
        {
            int targetSlot = Mathf.Clamp(slotIndex, 0, maxSlots - 1);
            SelectSlot(targetSlot);
        }
    }
}