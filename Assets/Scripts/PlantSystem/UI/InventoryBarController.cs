using UnityEngine;
using System.Collections.Generic;
using Abracodabra.UI.Genes;

namespace Abracodabra.UI.Genes // Assuming this is the correct namespace based on context
{
    public class InventoryBarController : MonoBehaviour
    {
        public static InventoryBarController Instance { get; private set; }

        // This is now a maximum limit, not a fixed count.
        [SerializeField] private int maxSlots = 10;
        [SerializeField] private InventoryGridController inventoryGridController;
        [SerializeField] private Transform cellContainer;
        [SerializeField] private GameObject selectionHighlight;
        [SerializeField] private GameObject inventoryItemViewPrefab;

        // We no longer need to store the slots, as they are created dynamically.
        private List<GameObject> activeItemSlots = new List<GameObject>();
        private int selectedSlot = 0;

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
            if (inventoryGridController != null)
            {
                inventoryGridController.OnInventoryChanged += HandleInventoryChanged;
            }
            else
            {
                Debug.LogError($"[{nameof(InventoryBarController)}] InventoryGridController not assigned!", this);
            }

            if (inventoryItemViewPrefab == null)
            {
                Debug.LogError($"[{nameof(InventoryBarController)}] InventoryItemViewPrefab not assigned!", this);
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
            // Clear existing slots first
            foreach (var slot in activeItemSlots)
            {
                Destroy(slot);
            }
            activeItemSlots.Clear();

            if (inventoryGridController == null || inventoryItemViewPrefab == null) return;
            
            var allItems = inventoryGridController.GetAllItems();
            int itemsToDisplay = Mathf.Min(allItems.Count, maxSlots);

            for (int i = 0; i < itemsToDisplay; i++)
            {
                var item = allItems[i];
                if (item == null || !item.IsValid()) continue;

                // Create a slot ONLY for valid items
                GameObject itemViewGO = Instantiate(inventoryItemViewPrefab, cellContainer);
                var itemView = itemViewGO.GetComponentInChildren<ItemView>();
                
                if (itemView == null)
                {
                    Debug.LogError($"The assigned InventoryItemViewPrefab is missing the ItemView component on itself or its children!", inventoryItemViewPrefab);
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
            // Clamp selected slot to the number of actual items
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
            // Can't select a slot that doesn't exist
            if (slotIndex < 0 || slotIndex >= activeItemSlots.Count)
            {
                // If trying to select an empty slot beyond the current items, deselect everything
                SelectedItem = null;
                UpdateSelection();
                return;
            }
            
            selectedSlot = slotIndex;
            var slot = activeItemSlots[slotIndex];
            var itemComponent = slot.GetComponentInChildren<InventoryBarItemComponent>();
            if (itemComponent != null)
            {
                SelectedItem = itemComponent.item;
            }
            else
            {
                SelectedItem = null;
            }
            
            UpdateSelection();
        }

        private void UpdateSelection()
        {
            if (selectionHighlight != null)
            {
                bool itemIsValid = SelectedItem != null && SelectedItem.IsValid() && selectedSlot < activeItemSlots.Count;
                selectionHighlight.SetActive(itemIsValid);

                if (itemIsValid)
                {
                    selectionHighlight.transform.position = activeItemSlots[selectedSlot].transform.position;
                }
            }
            OnSelectionChanged?.Invoke(SelectedItem);
        }

        public void SelectSlotByIndex(int slotIndex)
        {
            int targetSlot = Mathf.Clamp(slotIndex, 0, maxSlots - 1);
            SelectSlot(targetSlot);
        }
    }
}