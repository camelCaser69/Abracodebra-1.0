// FILE: Assets/Scripts/Nodes/Seeds/PlantotronGeneItem.cs (FIXED)
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class PlantotronGeneItem : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("UI References")]
    public TMP_Text geneNameText;
    public TMP_Text geneCountText;
    public Image geneIcon;
    public Button addButton;
    public Image backgroundImage;
    
    [Header("Visual Settings")]
    public Color normalColor = Color.white;
    public Color hoverColor = Color.yellow;
    public Color dragColor = new Color(1f, 0.8f, 0.8f, 1f);
    
    [Header("Drag Settings")]
    public float dragAlpha = 0.6f;
    
    private PlayerGeneticsInventory.GeneCount geneCount;
    private PlantotronUI parentUI;
    private bool isDragging = false;
    private Vector3 originalPosition;
    private Transform originalParent;
    private CanvasGroup canvasGroup;
    private Canvas rootCanvas;
    private RectTransform rectTransform;
    
    // Drag visual clone
    private GameObject dragClone;
    private PlantotronSequenceDropZone currentDropZone;
    
    // FIXED: Add public accessor for the gene
    public NodeDefinition GetGene()
    {
        return geneCount?.gene;
    }
    
    public void Initialize(PlayerGeneticsInventory.GeneCount geneCountData, PlantotronUI ui)
    {
        geneCount = geneCountData;
        parentUI = ui;
        
        if (geneCount?.gene == null || parentUI == null)
        {
            Debug.LogError("[PlantotronGeneItem] Invalid initialization parameters!");
            return;
        }
        
        // Setup components
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        
        rootCanvas = GetComponentInParent<Canvas>();
        if (rootCanvas != null)
            rootCanvas = rootCanvas.rootCanvas;
        
        // Setup UI elements
        if (geneNameText != null)
            geneNameText.text = geneCount.gene.displayName;
            
        if (geneCountText != null)
        {
            geneCountText.text = $"x{geneCount.count}";
        }
        
        if (geneIcon != null)
        {
            geneIcon.sprite = geneCount.gene.thumbnail;
            geneIcon.color = geneCount.gene.thumbnailTintColor;
        }
        
        // Setup add button
        if (addButton != null)
        {
            addButton.onClick.RemoveAllListeners();
            addButton.onClick.AddListener(OnAddButtonClicked);
            addButton.interactable = geneCount.count > 0;
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
        if (!isDragging && backgroundImage != null)
            backgroundImage.color = hoverColor;
            
        if (parentUI != null && geneCount?.gene != null)
            parentUI.ShowGeneDetails(geneCount.gene);
    }
    
    public void OnPointerExit(PointerEventData eventData)
    {
        if (!isDragging && backgroundImage != null)
            backgroundImage.color = normalColor;
    }
    
    public void OnPointerClick(PointerEventData eventData)
    {
        if (parentUI != null && geneCount?.gene != null)
            parentUI.OnGeneClicked(geneCount.gene);
    }
    
    private void OnAddButtonClicked()
    {
        if (parentUI != null && geneCount?.gene != null && geneCount.count > 0)
        {
            parentUI.TryAddGeneToSequence(geneCount.gene);
        }
    }
    
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (geneCount?.count <= 0) return; // Can't drag if no genes available
    
        isDragging = true;
    
        // Visual feedback
        if (backgroundImage != null)
            backgroundImage.color = dragColor;
        if (canvasGroup != null)
            canvasGroup.alpha = dragAlpha;
    
        // Create drag clone for better visual feedback
        CreateDragClone();
    
        // Enable drop zones through parent UI
        if (parentUI != null)
        {
            parentUI.EnableDropZones(true);
            Debug.Log("[PlantotronGeneItem] Enabled drop zones for drag");
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
        
        // Check for drop zones
        CheckDropZones(eventData);
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
    
        // Disable drop zones through parent UI
        if (parentUI != null)
        {
            parentUI.EnableDropZones(false);
            Debug.Log("[PlantotronGeneItem] Disabled drop zones after drag");
        }
    
        // FIXED: Handle drop using Unity's drop system instead of manual detection
        // The drop will be handled by PlantotronSequenceDropZone.OnDrop
        if (currentDropZone != null)
        {
            currentDropZone.SetHighlight(false);
            currentDropZone = null;
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
    
    private void CheckDropZones(PointerEventData eventData)
    {
        // Clear current drop zone highlighting
        if (currentDropZone != null)
        {
            currentDropZone.SetHighlight(false);
            currentDropZone = null;
        }
        
        // Check for new drop zone
        var results = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);
        
        foreach (var result in results)
        {
            PlantotronSequenceDropZone dropZone = result.gameObject.GetComponent<PlantotronSequenceDropZone>();
            if (dropZone != null && dropZone.CanAcceptDrop())
            {
                currentDropZone = dropZone;
                dropZone.SetHighlight(true);
                break;
            }
        }
    }
    
    // Update the display when gene count changes
    public void UpdateDisplay()
    {
        if (geneCountText != null && geneCount != null)
        {
            geneCountText.text = $"x{geneCount.count}";
        }
        
        if (addButton != null && geneCount != null)
        {
            addButton.interactable = geneCount.count > 0;
        }
    }
    
    void OnDestroy()
    {
        if (addButton != null)
            addButton.onClick.RemoveAllListeners();
        
        if (dragClone != null)
            Destroy(dragClone);
    }
}