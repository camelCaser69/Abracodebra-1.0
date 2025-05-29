// FILE: Assets/Scripts/Nodes/Seeds/PlantotronGeneSequenceItem.cs (FIXED)
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class PlantotronSequenceItem : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, 
    IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
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
    public float dragAlpha = 0.6f;
    
    private NodeDefinition gene;
    private int sequenceIndex;
    private PlantotronUI parentUI;
    private bool isDragging = false;
    private bool isDropTarget = false;
    private Vector3 originalPosition;
    private Transform originalParent;
    private CanvasGroup canvasGroup;
    private Canvas rootCanvas;
    private GameObject dragClone;
    private PlantotronSequenceItem currentDropTarget;
    
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
        
        // Setup components
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        
        rootCanvas = GetComponentInParent<Canvas>();
        if (rootCanvas != null)
            rootCanvas = rootCanvas.rootCanvas;
        
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
        
        // Visual feedback
        if (backgroundImage != null)
            backgroundImage.color = dragColor;
        if (canvasGroup != null)
            canvasGroup.alpha = dragAlpha;
        
        // Create drag clone
        CreateDragClone();
        
        // Make this item appear on top during drag
        transform.SetAsLastSibling();
        
        // FIXED: Enable drop zones for internal sequence dragging
        if (parentUI != null)
        {
            parentUI.EnableDropZones(true);
            Debug.Log("[PlantotronSequenceItem] Enabled drop zones for internal sequence drag");
        }
    }
    
    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging || dragClone == null) return;
        
        // Move drag clone to follow cursor
        Vector2 localPoint;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rootCanvas.transform as RectTransform, 
            eventData.position, 
            rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : rootCanvas.worldCamera, 
            out localPoint))
        {
            dragClone.transform.localPosition = localPoint;
        }
        
        // Check for drop targets
        CheckDropTargets(eventData);
    }
    
    public void OnEndDrag(PointerEventData eventData)
    {
        isDragging = false;
        
        // Reset visual state
        if (backgroundImage != null)
            backgroundImage.color = normalColor;
        if (canvasGroup != null)
            canvasGroup.alpha = 1f;
        
        // Clean up drag clone
        if (dragClone != null)
        {
            Destroy(dragClone);
            dragClone = null;
        }
        
        // Reset position and parent
        transform.position = originalPosition;
        transform.SetParent(originalParent);
        
        // FIXED: Disable drop zones after dragging
        if (parentUI != null)
        {
            parentUI.EnableDropZones(false);
            Debug.Log("[PlantotronSequenceItem] Disabled drop zones after internal sequence drag");
        }
        
        // Handle drop
        if (currentDropTarget != null && currentDropTarget != this)
        {
            // Swap positions in the gene sequence
            if (parentUI != null)
                parentUI.TryMoveGeneInSequence(this.sequenceIndex, currentDropTarget.sequenceIndex);
            
            currentDropTarget.SetDropTarget(false);
            currentDropTarget = null;
        }
    }
    
    private void CreateDragClone()
    {
        if (rootCanvas == null) return;
        
        // Create a visual clone
        dragClone = Instantiate(gameObject, rootCanvas.transform);
        dragClone.name = gameObject.name + "_DragClone";
        
        // Remove interactive components from clone
        Button[] buttons = dragClone.GetComponentsInChildren<Button>();
        foreach (var btn in buttons)
            btn.interactable = false;
        
        // Make it slightly transparent
        CanvasGroup cloneGroup = dragClone.GetComponent<CanvasGroup>();
        if (cloneGroup == null)
            cloneGroup = dragClone.AddComponent<CanvasGroup>();
        cloneGroup.alpha = 0.8f;
        cloneGroup.blocksRaycasts = false;
        
        // Position at cursor
        dragClone.transform.SetAsLastSibling();
    }
    
    private void CheckDropTargets(PointerEventData eventData)
    {
        // Clear current drop target highlighting
        if (currentDropTarget != null)
        {
            currentDropTarget.SetDropTarget(false);
            currentDropTarget = null;
        }
        
        // Check for new drop target
        var results = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);
        
        foreach (var result in results)
        {
            PlantotronSequenceItem sequenceItem = result.gameObject.GetComponent<PlantotronSequenceItem>();
            if (sequenceItem != null && sequenceItem != this)
            {
                currentDropTarget = sequenceItem;
                sequenceItem.SetDropTarget(true);
                break;
            }
        }
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
        
        if (dragClone != null)
            Destroy(dragClone);
    }
}