// FILE: Assets/Scripts/Nodes/Seeds/PlantotronSequenceItem.cs (NEW)
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class PlantotronSequenceItem : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, 
    IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler, IPointerClickHandler
{
    [Header("UI References")]
    public TMP_Text geneNameText;
    public TMP_Text sequenceIndexText;
    public Image geneIcon;
    public Button removeButton;
    public Image backgroundImage;
    
    [Header("Visual Settings")]
    public Color normalColor = new Color(0.9f, 0.9f, 0.9f, 1f);
    public Color hoverColor = Color.yellow;
    public Color dragColor = new Color(1f, 0.8f, 0.8f, 1f);
    public Color dropTargetColor = new Color(0.8f, 1f, 0.8f, 1f);
    
    [Header("Drag Settings")]
    public float dragAlpha = 0.8f;
    
    private NodeDefinition gene;
    private int sequenceIndex;
    private PlantotronUI parentUI;
    private bool isDragging = false;
    private bool isDropTarget = false;
    private Vector3 originalPosition;
    private Transform originalParent;
    private CanvasGroup canvasGroup;
    
    public void Initialize(NodeDefinition geneDefinition, int index, PlantotronUI ui)
    {
        gene = geneDefinition;
        sequenceIndex = index;
        parentUI = ui;
        
        if (gene == null || parentUI == null)
        {
            Debug.LogError("[PlantotronSequenceItem] Invalid initialization parameters!");
            return;
        }
        
        // Setup canvas group for drag transparency
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        
        // Setup UI elements
        if (geneNameText != null)
            geneNameText.text = gene.displayName;
            
        if (sequenceIndexText != null)
            sequenceIndexText.text = $"{index + 1}.";
            
        if (geneIcon != null)
        {
            geneIcon.sprite = gene.thumbnail;
            geneIcon.color = gene.thumbnailTintColor;
        }
        
        // Setup remove button
        if (removeButton != null)
        {
            removeButton.onClick.RemoveAllListeners();
            removeButton.onClick.AddListener(OnRemoveButtonClicked);
        }
        
        // Set initial background color
        if (backgroundImage != null)
            backgroundImage.color = normalColor;
            
        // Store original transform info
        originalPosition = transform.position;
        originalParent = transform.parent;
    }
    
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!isDragging && !isDropTarget && backgroundImage != null)
            backgroundImage.color = hoverColor;
            
        if (parentUI != null && gene != null)
            parentUI.ShowGeneDetails(gene);
    }
    
    public void OnPointerExit(PointerEventData eventData)
    {
        if (!isDragging && !isDropTarget && backgroundImage != null)
            backgroundImage.color = normalColor;
    }
    
    public void OnPointerClick(PointerEventData eventData)
    {
        if (parentUI != null && gene != null)
            parentUI.ShowGeneDetails(gene);
    }
    
    private void OnRemoveButtonClicked()
    {
        if (parentUI != null)
            parentUI.TryRemoveGeneFromSequence(sequenceIndex);
    }
    
    public void OnBeginDrag(PointerEventData eventData)
    {
        isDragging = true;
        if (backgroundImage != null)
            backgroundImage.color = dragColor;
            
        // Make semi-transparent during drag
        if (canvasGroup != null)
            canvasGroup.alpha = dragAlpha;
            
        // Make this item appear on top
        transform.SetAsLastSibling();
    }
    
    public void OnDrag(PointerEventData eventData)
    {
        if (isDragging)
        {
            // Follow mouse/finger during drag
            transform.position = eventData.position;
        }
    }
    
    public void OnEndDrag(PointerEventData eventData)
    {
        isDragging = false;
        if (backgroundImage != null)
            backgroundImage.color = normalColor;
            
        // Restore transparency
        if (canvasGroup != null)
            canvasGroup.alpha = 1f;
            
        // Reset position and parent
        transform.position = originalPosition;
        transform.SetParent(originalParent);
    }
    
    public void OnDrop(PointerEventData eventData)
    {
        // Handle drop from other sequence items (reordering)
        GameObject draggedObject = eventData.pointerDrag;
        if (draggedObject != null && parentUI != null)
        {
            PlantotronSequenceItem draggedItem = draggedObject.GetComponent<PlantotronSequenceItem>();
            if (draggedItem != null && draggedItem != this)
            {
                // Swap positions in the gene sequence
                parentUI.TryMoveGeneInSequence(draggedItem.sequenceIndex, this.sequenceIndex);
            }
            else
            {
                // Handle drop from gene panel
                PlantotronGeneItem geneItem = draggedObject.GetComponent<PlantotronGeneItem>();
                if (geneItem != null)
                {
                    // This is handled by the gene item's OnEndDrag
                }
            }
        }
        
        // Reset drop target visual
        SetDropTarget(false);
    }
    
    public void SetDropTarget(bool isTarget)
    {
        isDropTarget = isTarget;
        if (backgroundImage != null)
        {
            if (isTarget)
                backgroundImage.color = dropTargetColor;
            else if (!isDragging)
                backgroundImage.color = normalColor;
        }
    }
    
    // Public method to update the sequence index and display
    public void UpdateIndex(int newIndex)
    {
        sequenceIndex = newIndex;
        if (sequenceIndexText != null)
            sequenceIndexText.text = $"{newIndex + 1}.";
            
        // Update original position for drag reset
        originalPosition = transform.position;
    }
    
    public int GetIndex()
    {
        return sequenceIndex;
    }
    
    public NodeDefinition GetGene()
    {
        return gene;
    }
    
    void OnDestroy()
    {
        if (removeButton != null)
            removeButton.onClick.RemoveAllListeners();
    }
}