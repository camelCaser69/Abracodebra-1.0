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
        [Tooltip("The fixed number of slots to display in the bar.")]
        [SerializeField] private int barSlotCount = 8;
        [Tooltip("The prefab for a single inventory slot (must have a GeneSlotUI component).")]
        [SerializeField] private GameObject inventorySlotPrefab;

        [Header("Component References")]
        [SerializeField] private InventoryGridController inventoryGridController;
        // NOTE: This should be the child object that has the Image, GridLayoutGroup, and ContentSizeFitter
        [SerializeField] private Transform cellContainer;
        [SerializeField] private GameObject selectionHighlight;

        private List<GeneSlotUI> barSlots = new List<GeneSlotUI>();
        private int selectedSlot = 0;
        private Coroutine updateHighlightCoroutine;
        private RectTransform cellContainerRect; // Store a reference to the RectTransform

        public InventoryBarItem SelectedItem { get; private set; }
        public event System.Action<InventoryBarItem> OnSelectionChanged;

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
            if (inventoryGridController == null) Debug.LogError($"[{nameof(InventoryBarController)}] InventoryGridController not assigned!", this);
            if (inventorySlotPrefab == null) Debug.LogError($"[{nameof(InventoryBarController)}] Inventory Slot Prefab not assigned!", this);
            if (cellContainer == null) Debug.LogError($"[{nameof(InventoryBarController)}] Cell Container not assigned!", this);
            else cellContainerRect = cellContainer.GetComponent<RectTransform>();

            inventoryGridController.OnInventoryChanged += HandleInventoryChanged;
            
            CreateBarSlots();
            
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

        private void CreateBarSlots()
        {
            foreach (Transform child in cellContainer)
            {
                Destroy(child.gameObject);
            }
            barSlots.Clear();

            for (int i = 0; i < barSlotCount; i++)
            {
                GameObject slotGO = Instantiate(inventorySlotPrefab, cellContainer);
                GeneSlotUI slotUI = slotGO.GetComponent<GeneSlotUI>();
                if (slotUI != null)
                {
                    slotUI.isDraggable = false;
                    barSlots.Add(slotUI);
                }
            }

            // --- THIS IS THE FIX ---
            // We force the rebuild on the cellContainer, which has all the layout components.
            if (cellContainerRect != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(cellContainerRect);
            }
        }
        
        private void UpdateBarDisplay()
        {
            if (inventoryGridController == null) return;
            
            List<InventoryBarItem> items = inventoryGridController.GetFirstNItems(barSlotCount);

            for (int i = 0; i < barSlotCount; i++)
            {
                if (i < barSlots.Count && i < items.Count)
                {
                    barSlots[i].SetItem(items[i]);
                }
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
            SelectSlot(selectedSlot);
        }

        private void HandleNumberKeyInput()
        {
            for (int i = 1; i <= 9; i++)
            {
                if (i <= barSlotCount && Input.GetKeyDown(KeyCode.Alpha0 + i)) { SelectSlot(i - 1); return; }
            }
            if (barSlotCount >= 10 && Input.GetKeyDown(KeyCode.Alpha0)) { SelectSlot(9); }
        }

        public void SelectSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= barSlots.Count)
            {
                return;
            }
            
            selectedSlot = slotIndex;
            SelectedItem = barSlots[slotIndex].CurrentItem;
            
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
                if (selectedSlot < 0 || selectedSlot >= barSlots.Count)
                {
                    selectionHighlight.SetActive(false);
                    yield break;
                }

                bool itemIsValid = SelectedItem != null && SelectedItem.IsValid();
                selectionHighlight.SetActive(itemIsValid);

                if (itemIsValid)
                {
                    selectionHighlight.transform.position = barSlots[selectedSlot].transform.position;
                }
            }
            updateHighlightCoroutine = null;
        }

        public void SelectSlotByIndex(int slotIndex)
        {
            int targetSlot = Mathf.Clamp(slotIndex, 0, barSlotCount - 1);
            SelectSlot(targetSlot);
        }
    }
}