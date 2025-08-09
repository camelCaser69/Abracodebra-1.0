// REWORKED FILE: Assets/Scripts/UI/Genes/GeneSlotUI.cs
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using Abracodabra.Genes.Core;
using Abracodabra.Genes.Services;

namespace Abracodabra.UI.Genes
{
    // This now represents a generic inventory/sequence slot, not just for genes.
    public class GeneSlotUI : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public GeneCategory acceptedCategory; // Still useful for sequence slots
        public int slotIndex;
        public bool isLocked = false;
        public bool isDraggable = true;

        [Header("Visuals")]
        [SerializeField] private Image slotBackground;
        [SerializeField] private GameObject emptyIndicator;
        [SerializeField] private GameObject lockedOverlay;
        [SerializeField] private GameObject invalidDropOverlay;
        [SerializeField] private GameObject executingEffect;
        [SerializeField] private ItemView itemView; // The view that shows the item's icon

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

        private void Awake()
        {
            parentSequence = GetComponentInParent<GeneSequenceUI>();
            canvas = GetComponentInParent<Canvas>();

            // The ItemView should be a child of this slot object.
            if (itemView == null)
            {
                itemView = GetComponentInChildren<ItemView>();
            }
            if (itemView == null)
            {
                Debug.LogError($"GeneSlotUI on {gameObject.name} is missing its ItemView child component.", this);
            }
        }

        private void Start()
        {
            eventBus = GeneServices.Get<IGeneEventBus>();
            if (eventBus == null)
            {
                Debug.LogError("GeneEventBus service not found!", this);
            }
        }

        #region Event Handling
        // Subscribing in OnEnable and unsubscribing in OnDisable is the standard,
        // robust pattern for handling events on MonoBehaviours that can be
        // toggled on and off. This prevents memory leaks and duplicate subscriptions.
        private void OnEnable()
        {
            eventBus?.Subscribe<GeneExecutedEvent>(OnGeneExecuted);
        }

        private void OnDisable()
        {
            eventBus?.Unsubscribe<GeneExecutedEvent>(OnGeneExecuted);
        }
        #endregion

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
            bool isEmpty = CurrentItem == null || !CurrentItem.IsValid();

            if (emptyIndicator != null) emptyIndicator.SetActive(isEmpty);
            if (itemView != null) itemView.gameObject.SetActive(!isEmpty);

            if (!isEmpty)
            {
                // Let the ItemView handle its own initialization
                switch(CurrentItem.Type)
                {
                    case InventoryBarItem.ItemType.Gene:
                        itemView.InitializeAsGene(CurrentItem.GeneInstance);
                        if (slotBackground != null) slotBackground.color = CurrentItem.GeneInstance.GetGene().geneColor.WithAlpha(0.5f);
                        break;
                    case InventoryBarItem.ItemType.Seed:
                        itemView.InitializeAsSeed(CurrentItem.SeedTemplate);
                        if (slotBackground != null) slotBackground.color = InventoryColorManager.Instance.GetCellColorForItem(null, CurrentItem.SeedTemplate, null);
                        break;
                    case InventoryBarItem.ItemType.Tool:
                        itemView.InitializeAsTool(CurrentItem.ToolDefinition);
                        if (slotBackground != null) slotBackground.color = InventoryColorManager.Instance.GetCellColorForItem(null, null, CurrentItem.ToolDefinition);
                        break;
                }
            }
            else
            {
                 if (slotBackground != null) slotBackground.color = normalColor;
            }

            if (lockedOverlay != null) lockedOverlay.SetActive(isLocked);
        }

        #region Drag and Drop
        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!isDraggable || CurrentItem == null || isLocked)
            {
                eventData.pointerDrag = null; // Prevent drag
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

            var draggedItem = sourceSlot.CurrentItem;
            if (draggedItem == null) return;
            
            // For a gene, check if it can be attached to this slot's active gene
            if (acceptedCategory == GeneCategory.Modifier || acceptedCategory == GeneCategory.Payload)
            {
                var activeGene = parentSequence?.GetActiveGeneForRow(slotIndex);
                if (activeGene == null || !draggedItem.GeneInstance.GetGene().CanAttachTo(activeGene))
                {
                    ShowInvalidDropFeedback();
                    return;
                }
            }
            
            // Swap items
            var previousItemInThisSlot = this.CurrentItem;
            SetItem(draggedItem);
            sourceSlot.SetItem(previousItemInThisSlot);
        }
        #endregion

        #region Pointer Events
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (isLocked || slotBackground == null || eventData.pointerDrag == null) return;
            
            // TODO: Add proper validation logic here if needed
            slotBackground.color = highlightColor;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            // Reset color, UpdateVisuals will set it correctly based on content
            if (slotBackground != null) slotBackground.color = normalColor; 
            UpdateVisuals();
            if (invalidDropOverlay != null) invalidDropOverlay.SetActive(false);
        }
        #endregion

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

        #region Execution Feedback
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
        #endregion
    }
}