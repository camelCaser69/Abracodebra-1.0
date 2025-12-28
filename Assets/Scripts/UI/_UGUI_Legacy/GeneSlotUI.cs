using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using Abracodabra.Genes.Core;
using Abracodabra.Genes.Services;
using Abracodabra.Genes.Runtime;
using Abracodabra.Genes.Templates;

// Note: Add 'using' statements for ItemInstance/ItemDefinition if they are in namespaces.

namespace Abracodabra.UI.Genes
{
    public class GeneSlotUI : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public GeneCategory acceptedCategory;
        public int slotIndex;
        public bool isLocked = false;
        public bool isDraggable = true;

        [SerializeField] private Image slotBackground;
        [SerializeField] private GameObject emptyIndicator;
        [SerializeField] private GameObject lockedOverlay;
        [SerializeField] private GameObject executingEffect;
        [SerializeField] private ItemView itemView;

        [SerializeField] private Color normalColor = new Color(0.2f, 0.2f, 0.2f, 0.5f);
        [SerializeField] private Color highlightColor = new Color(1f, 0.9f, 0.4f, 0.5f);
        [SerializeField] private Color invalidColor = new Color(1f, 0.3f, 0.3f, 0.5f);
        [SerializeField] private Color executingColor = new Color(0.4f, 0.8f, 1f, 0.5f);


        public InventoryBarItem CurrentItem { get; set; }
        private GeneSequenceUI parentSequence;
        private IGeneEventBus eventBus;
        private GameObject draggedVisual;
        private Canvas canvas;
        private bool isPointerOver = false;

        private void Awake()
        {
            parentSequence = GetComponentInParent<GeneSequenceUI>();
            canvas = GetComponentInParent<Canvas>();
            if (itemView == null) itemView = GetComponentInChildren<ItemView>(true);
            if (itemView == null) Debug.LogError($"GeneSlotUI on {gameObject.name} is missing its ItemView child component.", this);
        }

        private void Start()
        {
            eventBus = GeneServices.Get<IGeneEventBus>();
            UpdateVisuals();
        }

        private void OnEnable()
        {
            eventBus?.Subscribe<GeneExecutedEvent>(OnGeneExecuted);
        }

        private void OnDisable()
        {
            eventBus?.Unsubscribe<GeneExecutedEvent>(OnGeneExecuted);
        }

        public void SetItem(InventoryBarItem item)
        {
            CurrentItem = item;
            UpdateVisuals();
        }

        public void ClearSlot()
        {
            SetItem(null);
        }

        private void UpdateVisuals()
        {
            if (itemView == null || slotBackground == null) return;
            bool isEmpty = CurrentItem == null || !CurrentItem.IsValid();
            if (emptyIndicator != null) emptyIndicator.SetActive(isEmpty);
            itemView.gameObject.SetActive(!isEmpty);

            if (!isEmpty)
            {
                switch (CurrentItem.Type)
                {
                    case InventoryBarItem.ItemType.Gene:
                        itemView.InitializeAsGene(CurrentItem.GeneInstance);
                        slotBackground.color = InventoryColorManager.Instance.GetCellColorForItem(CurrentItem.GeneInstance.GetGene(), null, null, null);
                        break;
                    case InventoryBarItem.ItemType.Seed:
                        itemView.InitializeAsSeed(CurrentItem.SeedTemplate);
                        slotBackground.color = InventoryColorManager.Instance.GetCellColorForItem(null, CurrentItem.SeedTemplate, null, null);
                        break;
                    case InventoryBarItem.ItemType.Tool:
                        itemView.InitializeAsTool(CurrentItem.ToolDefinition);
                        slotBackground.color = InventoryColorManager.Instance.GetCellColorForItem(null, null, CurrentItem.ToolDefinition, null);
                        break;
                    case InventoryBarItem.ItemType.Resource: // NEW CASE
                        itemView.InitializeAsItem(CurrentItem.ItemInstance);
                        slotBackground.color = InventoryColorManager.Instance.GetCellColorForItem(null, null, null, CurrentItem.ItemInstance.definition);
                        break;
                }
            }
            else
            {
                slotBackground.color = normalColor;
            }

            if (lockedOverlay != null) lockedOverlay.SetActive(isLocked);
            if (isPointerOver && !isLocked)
            {
                slotBackground.color = highlightColor;
            }
        }


        public void OnDrop(PointerEventData eventData)
        {
            if (isLocked) return;
            GeneSlotUI sourceSlot = eventData.pointerDrag?.GetComponent<GeneSlotUI>();
            if (sourceSlot == null || sourceSlot == this) return;

            if (!IsValidDrop(sourceSlot, this))
            {
                ShowInvalidDropFeedback();
                return;
            }

            var itemFromSource = sourceSlot.CurrentItem;
            var itemFromDestination = this.CurrentItem;

            UpdateSlotContents(sourceSlot, itemFromDestination);
            UpdateSlotContents(this, itemFromSource);
        }

        private bool IsItemValidForSlot(InventoryBarItem item, GeneSlotUI slot)
        {
            if (item == null) return true;

            // Prevent dropping Resources into gene sequence slots
            if (item.Type == InventoryBarItem.ItemType.Resource && slot.parentSequence != null)
            {
                return false;
            }

            if (slot.acceptedCategory == GeneCategory.Seed)
            {
                return item.Type == InventoryBarItem.ItemType.Seed;
            }

            if (slot.parentSequence != null)
            {
                if (item.Type != InventoryBarItem.ItemType.Gene) return false;
                var gene = item.GeneInstance.GetGene();
                if (gene.Category != slot.acceptedCategory) return false;

                if (slot.acceptedCategory == GeneCategory.Modifier || slot.acceptedCategory == GeneCategory.Payload)
                {
                    var activeGene = slot.parentSequence.GetActiveGeneForRow(slot.slotIndex);
                    if (activeGene == null || !gene.CanAttachTo(activeGene)) return false;
                }
            }
            return true;
        }


        private bool IsValidDrop(GeneSlotUI source, GeneSlotUI destination)
        {
            var sourceItem = source.CurrentItem;
            var destinationItem = destination.CurrentItem;
            return IsItemValidForSlot(sourceItem, destination) && IsItemValidForSlot(destinationItem, source);
        }

        private void UpdateSlotContents(GeneSlotUI slot, InventoryBarItem newItem)
        {
            if (slot.acceptedCategory == GeneCategory.Seed && slot.parentSequence != null)
            {
                slot.parentSequence.LoadSeedForEditing(newItem);
            }
            else if (slot.parentSequence != null)
            {
                slot.parentSequence.UpdateDataForSlot(slot.slotIndex, slot.acceptedCategory, newItem);
            }
            else
            {
                slot.SetItem(newItem);
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            isPointerOver = true;
            UpdateVisuals();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            isPointerOver = false;
            UpdateVisuals();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!isDraggable || CurrentItem == null || isLocked) { eventData.pointerDrag = null; return; }
            CreateDragVisual();
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (draggedVisual != null) draggedVisual.transform.position = eventData.position;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (draggedVisual != null) Destroy(draggedVisual);
        }

        private void CreateDragVisual()
        {
            if (canvas == null || CurrentItem == null) return;
            draggedVisual = new GameObject("DragVisual");
            draggedVisual.transform.SetParent(canvas.transform, false);
            draggedVisual.transform.SetAsLastSibling();
            var image = draggedVisual.AddComponent<Image>();
            image.sprite = CurrentItem.GetIcon();
            image.color = new Color(1, 1, 1, 0.7f);
            image.raycastTarget = false;
            var rect = draggedVisual.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(64, 64);
        }

        private void ShowInvalidDropFeedback()
        {
            StartCoroutine(FlashColor(invalidColor));
        }

        private IEnumerator FlashColor(Color flashColor)
        {
            if (slotBackground == null) yield break;
            Color originalColor = slotBackground.color;
            slotBackground.color = flashColor;
            yield return new WaitForSeconds(0.3f);
            slotBackground.color = originalColor;
            UpdateVisuals();
        }

        private void OnGeneExecuted(GeneExecutedEvent evt)
        {
            if (CurrentItem != null && CurrentItem.Type == InventoryBarItem.ItemType.Gene && CurrentItem.GeneInstance.GetGene()?.GUID == evt.Gene.GUID)
            {
                ShowExecuting();
            }
        }

        public void ShowExecuting()
        {
            if (executingEffect != null) executingEffect.SetActive(true);
            if (slotBackground != null) slotBackground.color = executingColor;
            Invoke(nameof(HideExecuting), 0.5f);
        }

        public void HideExecuting()
        {
            if (executingEffect != null) executingEffect.SetActive(false);
            UpdateVisuals();
        }
    }
}