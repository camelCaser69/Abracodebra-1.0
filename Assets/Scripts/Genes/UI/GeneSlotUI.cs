using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System.Reflection;
using Abracodabra.Genes.Core;
using Abracodabra.Genes.Services;
using Abracodabra.Genes.Runtime;
using Abracodabra.UI.Genes;
using Abracodabra.Genes.Templates;

namespace Abracodabra.UI.Genes
{
    public class GeneSlotUI : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public GeneCategory acceptedCategory;
        public int slotIndex;
        public bool isLocked = false;
        public bool isDraggable = true;

        [Header("Visual Components")]
        [SerializeField] private Image slotBackground;
        [SerializeField] private GameObject emptyIndicator;
        [SerializeField] private GameObject lockedOverlay;
        [SerializeField] private GameObject executingEffect;
        [SerializeField] private ItemView itemView;

        [Header("Colors")]
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color highlightColor = Color.yellow;
        [SerializeField] private Color invalidColor = Color.red;
        [SerializeField] private Color executingColor = Color.cyan;

        public InventoryBarItem CurrentItem { get; private set; }

        private GeneSequenceUI parentSequence;
        private IGeneEventBus eventBus;

        private GameObject draggedVisual;
        private Canvas canvas;
        private bool isPointerOver = false;

        void Awake()
        {
            parentSequence = GetComponentInParent<GeneSequenceUI>();
            canvas = GetComponentInParent<Canvas>();

            if (itemView == null) itemView = GetComponentInChildren<ItemView>(true);
            if (itemView == null) Debug.LogError($"GeneSlotUI on {gameObject.name} is missing its ItemView child component.", this);
        }

        void Start()
        {
            eventBus = GeneServices.Get<IGeneEventBus>();
            UpdateVisuals();
        }

        void OnEnable()
        {
            eventBus?.Subscribe<GeneExecutedEvent>(OnGeneExecuted);
        }

        void OnDisable()
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
                        slotBackground.color = CurrentItem.GeneInstance.GetGene().geneColor.WithAlpha(0.5f);
                        break;
                    case InventoryBarItem.ItemType.Seed:
                        itemView.InitializeAsSeed(CurrentItem.SeedTemplate);
                        if (InventoryColorManager.Instance != null)
                            slotBackground.color = InventoryColorManager.Instance.GetCellColorForItem(null, CurrentItem.SeedTemplate, null);
                        break;
                    case InventoryBarItem.ItemType.Tool:
                        itemView.InitializeAsTool(CurrentItem.ToolDefinition);
                        if (InventoryColorManager.Instance != null)
                            slotBackground.color = InventoryColorManager.Instance.GetCellColorForItem(null, null, CurrentItem.ToolDefinition);
                        break;
                }
            }
            else
            {
                slotBackground.color = normalColor;
            }

            if (lockedOverlay != null) lockedOverlay.SetActive(isLocked);
            if (isPointerOver)
            {
                slotBackground.color = highlightColor;
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!isDraggable || CurrentItem == null || isLocked)
            {
                eventData.pointerDrag = null;
                return;
            }
            CreateDragVisual();
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (draggedVisual != null)
            {
                draggedVisual.transform.position = eventData.position;
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (draggedVisual != null)
            {
                Destroy(draggedVisual);
                draggedVisual = null;
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
        
        private bool IsValidDrop(GeneSlotUI source, GeneSlotUI destination)
        {
            var sourceItem = source.CurrentItem;
            var destinationItem = destination.CurrentItem;
        
            if (!IsItemValidForSlot(sourceItem, destination)) return false;
            if (!IsItemValidForSlot(destinationItem, source)) return false;
        
            return true;
        }

        private bool IsItemValidForSlot(InventoryBarItem item, GeneSlotUI slot)
        {
            if (item == null) return true;
        
            var seedEditSlotField = slot.parentSequence?.GetType().GetField("seedEditSlot", BindingFlags.NonPublic | BindingFlags.Instance);
            GeneSlotUI seedEditSlot = seedEditSlotField?.GetValue(slot.parentSequence) as GeneSlotUI;

            if (slot == seedEditSlot)
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

        // MODIFIED: This method now correctly routes passive gene updates.
        private void UpdateSlotContents(GeneSlotUI slot, InventoryBarItem newItem)
        {
            var seedEditSlotField = slot.parentSequence?.GetType().GetField("seedEditSlot", BindingFlags.NonPublic | BindingFlags.Instance);
            GeneSlotUI seedEditSlot = seedEditSlotField?.GetValue(slot.parentSequence) as GeneSlotUI;
            
            if (slot == seedEditSlot)
            {
                slot.parentSequence.LoadSeedForEditing(newItem);
            }
            else if (slot.parentSequence != null)
            {
                // This is the key fix: check the category to call the right method.
                if (slot.acceptedCategory == GeneCategory.Passive)
                {
                    slot.parentSequence.UpdatePassiveGene(slot.slotIndex, newItem);
                }
                else
                {
                    slot.parentSequence.UpdateGeneInSequence(slot.slotIndex, slot.acceptedCategory, newItem);
                }
            }
            else // This is a plain inventory slot
            {
                slot.SetItem(newItem);
            }
        }
        
        public void OnPointerEnter(PointerEventData eventData)
        {
            isPointerOver = true;
            if (isLocked || slotBackground == null) return;
            if (eventData.pointerDrag != null)
            {
                slotBackground.color = highlightColor;
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            isPointerOver = false;
            if (slotBackground != null)
            {
                UpdateVisuals();
            }
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

        IEnumerator FlashColor(Color flashColor)
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