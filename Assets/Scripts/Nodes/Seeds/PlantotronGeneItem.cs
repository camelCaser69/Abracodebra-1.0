// FILE: Assets/Scripts/Nodes/Seeds/PlantotronGeneItem.cs (UPDATED)
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
    public float dragAlpha = 0.8f;
    
    private PlayerGeneticsInventory.GeneCount geneCount;
    private PlantotronUI parentUI;
    private bool isDragging = false;
    private Vector3 originalPosition;
    private Transform originalParent;
    private CanvasGroup canvasGroup;
    
    public void Initialize(PlayerGeneticsInventory.GeneCount geneCountData, PlantotronUI ui)
    {
        geneCount = geneCountData;
        parentUI = ui;
        
        if (geneCount?.gene == null || parentUI == null)
        {
            Debug.LogError("[PlantotronGeneItem] Invalid initialization parameters!");
            return;
        }
        
        // Setup canvas group for drag transparency
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        
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
        
        // Check if dropped on sequence panel
        GameObject target = eventData.pointerCurrentRaycast.gameObject;
        if (target != null)
        {
            // Check if dropped on sequence container or sequence item
            PlantotronSequenceItem sequenceItem = target.GetComponent<PlantotronSequenceItem>();
            if (sequenceItem != null && parentUI != null && geneCount?.gene != null)
            {
                // Drop on specific sequence position
                parentUI.TryAddGeneToSequence(geneCount.gene, sequenceItem.GetIndex());
            }
            else if (target.transform.IsChildOf(parentUI.sequenceContainer) && parentUI != null && geneCount?.gene != null)
            {
                // Drop anywhere in sequence container (add to end)
                parentUI.TryAddGeneToSequence(geneCount.gene);
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
    }
}