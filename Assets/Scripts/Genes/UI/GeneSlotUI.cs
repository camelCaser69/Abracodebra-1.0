// REWORKED FILE: Assets/Scripts/UI/Genes/GeneSlotUI.cs
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections; // FIX: Added missing using statement for IEnumerator
using Abracodabra.Genes.Services;
using Abracodabra.Genes.Core;
using Abracodabra.Genes.Runtime;

namespace Abracodabra.UI.Genes
{
    // ... (rest of the file is identical to the one I sent previously, but with the IEnumerator fix)
    public class GeneSlotUI : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Header("Slot Configuration")]
        public GeneCategory acceptedCategory;
        public int slotIndex;
        public bool isLocked = false;
        public bool isDraggable = true;

        [Header("Visual Elements")]
        public Image slotBackground;
        public Image geneIcon;
        public TMPro.TextMeshProUGUI geneName;
        public GameObject emptyIndicator;
        public GameObject lockedOverlay;
        public GameObject invalidDropOverlay;
        public GameObject executingEffect;

        [Header("Colors")]
        public Color normalColor = Color.white;
        public Color highlightColor = Color.yellow;
        public Color invalidColor = Color.red;
        public Color executingColor = Color.cyan;

        // Runtime data
        private RuntimeGeneInstance currentInstance;
        private GeneSequenceUI parentSequence;
        private IGeneEventBus eventBus;

        // Drag & Drop state
        private GameObject draggedVisual;
        private Canvas canvas;

        void Awake()
        {
            parentSequence = GetComponentInParent<GeneSequenceUI>();
            canvas = GetComponentInParent<Canvas>();
        }
        
        void Start()
        {
            // Services might not be ready in Awake
            eventBus = GeneServices.Get<IGeneEventBus>();
            if (eventBus == null)
            {
                Debug.LogError("GeneEventBus service not found!", this);
            }
        }
        
        // ADD THIS METHOD TO THE EXISTING GeneSlotUI.cs SCRIPT
        void OnDestroy()
        {
            // Unsubscribe from events to prevent memory leaks
            eventBus?.Unsubscribe<GeneExecutedEvent>(OnGeneExecuted);
        }

        void OnEnable()
        {
            eventBus?.Subscribe<GeneExecutedEvent>(OnGeneExecuted);
        }

        void OnDisable()
        {
            eventBus?.Unsubscribe<GeneExecutedEvent>(OnGeneExecuted);
        }

        public void SetGeneInstance(RuntimeGeneInstance instance)
        {
            currentInstance = instance;
            UpdateVisuals();

            // Notify parent sequence if an Active gene was changed
            if (acceptedCategory == GeneCategory.Active && parentSequence != null)
            {
                parentSequence.OnActiveGeneChanged(slotIndex, instance?.GetGene<ActiveGene>());
            }
        }

        public RuntimeGeneInstance GetGeneInstance() => currentInstance;

        public void ClearSlot()
        {
            SetGeneInstance(null);
        }

        private void UpdateVisuals()
        {
            bool isEmpty = currentInstance == null;

            if(emptyIndicator != null) emptyIndicator.SetActive(isEmpty);
            if(geneIcon != null) geneIcon.gameObject.SetActive(!isEmpty);
            if(geneName != null) geneName.gameObject.SetActive(!isEmpty);

            if (!isEmpty)
            {
                var gene = currentInstance.GetGene();
                if (gene != null)
                {
                    if (geneIcon != null) geneIcon.sprite = gene.icon;
                    if (geneName != null) geneName.text = gene.geneName;
                    if (slotBackground != null) slotBackground.color = gene.geneColor;
                }
            }
            else
            {
                if (slotBackground != null) slotBackground.color = normalColor;
            }

            if(lockedOverlay != null) lockedOverlay.SetActive(isLocked);
        }

        #region Drag & Drop
        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!isDraggable || currentInstance == null || isLocked)
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

            var draggedInstance = sourceSlot.GetGeneInstance();
            if (draggedInstance == null) return;

            if (!CanAcceptGene(draggedInstance.GetGene()))
            {
                ShowInvalidDropFeedback();
                return;
            }

            // Swap the gene instances between slots
            var previousInstanceInThisSlot = currentInstance;
            SetGeneInstance(draggedInstance);
            sourceSlot.SetGeneInstance(previousInstanceInThisSlot);
        }

        private bool CanAcceptGene(GeneBase gene)
        {
            if (gene == null) return false;
            if (gene.Category != acceptedCategory) return false;

            if (acceptedCategory == GeneCategory.Modifier || acceptedCategory == GeneCategory.Payload)
            {
                var parentActive = parentSequence?.GetActiveGeneForRow(slotIndex);
                if (parentActive == null) return false;
                return gene.CanAttachTo(parentActive);
            }

            return true;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (isLocked || slotBackground == null) return;

            if (eventData.pointerDrag != null)
            {
                var sourceSlot = eventData.pointerDrag.GetComponent<GeneSlotUI>();
                if (sourceSlot != null)
                {
                    var gene = sourceSlot.GetGeneInstance()?.GetGene();
                    bool canAccept = gene != null && CanAcceptGene(gene);
                    slotBackground.color = canAccept ? highlightColor : invalidColor;
                    if (invalidDropOverlay != null) invalidDropOverlay.SetActive(!canAccept);
                }
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            UpdateVisuals();
            if (invalidDropOverlay != null) invalidDropOverlay.SetActive(false);
        }
        #endregion

        private void CreateDragVisual()
        {
            if (canvas == null) return;
            draggedVisual = new GameObject("DragVisual");
            draggedVisual.transform.SetParent(canvas.transform, false);
            draggedVisual.transform.SetAsLastSibling();

            var image = draggedVisual.AddComponent<Image>();
            image.sprite = geneIcon.sprite;
            image.color = new Color(1, 1, 1, 0.7f);
            image.raycastTarget = false;

            var rect = draggedVisual.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(64, 64); // Or match source rect
        }

        private void ShowInvalidDropFeedback()
        {
            StartCoroutine(FlashColor(invalidColor));
        }

        // FIX: Replaced incorrect FlashInvalid method with this correct coroutine
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
            if (currentInstance != null && currentInstance.GetGene()?.GUID == evt.Gene.GUID)
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