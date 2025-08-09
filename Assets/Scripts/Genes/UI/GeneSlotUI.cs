using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using Abracodabra.Genes.Core;
using Abracodabra.Genes.Services;
using Abracodabra.Genes.Runtime;
using Abracodabra.UI.Genes;
using Abracodabra.Genes.Templates;
// The incorrect 'using Abracodabra.Core;' line has been removed.

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
        [SerializeField] private GameObject invalidDropOverlay;
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

        void Awake()
        {
            parentSequence = GetComponentInParent<GeneSequenceUI>();
            canvas = GetComponentInParent<Canvas>();

            if (itemView == null)
            {
                itemView = GetComponentInChildren<ItemView>();
            }
            if (itemView == null)
            {
                Debug.LogError($"GeneSlotUI on {gameObject.name} is missing its ItemView child component.", this);
            }
        }

        void Start()
        {
            eventBus = GeneServices.Get<IGeneEventBus>();
            // No error needed if null, just a potential feature loss
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
            bool isEmpty = CurrentItem == null || !CurrentItem.IsValid();

            if (emptyIndicator != null) emptyIndicator.SetActive(isEmpty);
            if (itemView != null) itemView.gameObject.SetActive(!isEmpty);

            if (!isEmpty)
            {
                switch (CurrentItem.Type)
                {
                    case InventoryBarItem.ItemType.Gene:
                        itemView.InitializeAsGene(CurrentItem.GeneInstance);
                        if (slotBackground != null) slotBackground.color = CurrentItem.GeneInstance.GetGene().geneColor.WithAlpha(0.5f);
                        break;
                    case InventoryBarItem.ItemType.Seed:
                        itemView.InitializeAsSeed(CurrentItem.SeedTemplate);
                        if (slotBackground != null && InventoryColorManager.Instance != null) slotBackground.color = InventoryColorManager.Instance.GetCellColorForItem(null, CurrentItem.SeedTemplate, null);
                        break;
                    case InventoryBarItem.ItemType.Tool:
                        itemView.InitializeAsTool(CurrentItem.ToolDefinition);
                        if (slotBackground != null && InventoryColorManager.Instance != null) slotBackground.color = InventoryColorManager.Instance.GetCellColorForItem(null, null, CurrentItem.ToolDefinition);
                        break;
                }
            }
            else
            {
                if (slotBackground != null) slotBackground.color = normalColor;
            }

            if (lockedOverlay != null) lockedOverlay.SetActive(isLocked);
        }

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
            if (draggedItem == null || draggedItem.Type != InventoryBarItem.ItemType.Gene)
            {
                ShowInvalidDropFeedback();
                return;
            }
            
            // Validate category
            var draggedGene = draggedItem.GeneInstance.GetGene();
            if (draggedGene.Category != acceptedCategory)
            {
                 ShowInvalidDropFeedback();
                 return;
            }

            // Validate attachment for modifiers/payloads
            if (acceptedCategory == GeneCategory.Modifier || acceptedCategory == GeneCategory.Payload)
            {
                var activeGene = parentSequence?.GetActiveGeneForRow(slotIndex);
                if (activeGene == null || !draggedGene.CanAttachTo(activeGene))
                {
                    ShowInvalidDropFeedback();
                    return;
                }
            }

            // If we are in a sequence builder, notify the parent to update the data model
            if (parentSequence != null)
            {
                parentSequence.UpdateGeneInSequence(slotIndex, acceptedCategory, draggedItem);
                
                // The source slot should now contain what was in this slot before the drop
                // This logic might need to be expanded if you want to drag *from* the inventory grid
                // For now, we assume swaps happen within the sequence UI
                sourceSlot.parentSequence.UpdateGeneInSequence(sourceSlot.slotIndex, sourceSlot.acceptedCategory, this.CurrentItem);
            }
            else // Otherwise, perform a simple swap (for inventory grid)
            {
                var previousItemInThisSlot = this.CurrentItem;
                SetItem(draggedItem);
                sourceSlot.SetItem(previousItemInThisSlot);
            }
        }


        public void OnPointerEnter(PointerEventData eventData)
        {
            if (isLocked || slotBackground == null || eventData.pointerDrag == null) return;

            slotBackground.color = highlightColor;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (slotBackground != null) slotBackground.color = normalColor;
            UpdateVisuals();
            if (invalidDropOverlay != null) invalidDropOverlay.SetActive(false);
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