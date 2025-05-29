// FILE: Assets/Scripts/Nodes/Seeds/PlantotronSequenceDropZone.cs (FIXED)
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class PlantotronSequenceDropZone : MonoBehaviour, IDropHandler
{
    [Header("Visual Settings")]
    public Color normalColor = new Color(0.2f, 0.8f, 0.2f, 0.2f); // Slightly visible green
    public Color highlightColor = new Color(0.2f, 1f, 0.2f, 0.5f); // Bright green when highlighted
    
    private Image backgroundImage;
    private PlantotronUI parentUI;
    private int insertIndex = -1;
    private bool isEnabled = false;
    
    void Awake()
    {
        backgroundImage = GetComponent<Image>();
        if (backgroundImage == null)
        {
            backgroundImage = gameObject.AddComponent<Image>();
        }
        
        // Set initial state - slightly visible so you can see where they are
        backgroundImage.color = normalColor;
        backgroundImage.raycastTarget = true;
        gameObject.SetActive(false); // Start disabled
    }
    
    public void Initialize(PlantotronUI ui, int index)
    {
        parentUI = ui;
        insertIndex = index;
        
        if (parentUI != null)
        {
            Debug.Log($"[PlantotronSequenceDropZone] Initialized drop zone {index}");
        }
    }
    
    public void SetEnabled(bool enabled)
    {
        isEnabled = enabled;
        gameObject.SetActive(enabled);
        
        if (!enabled)
            SetHighlight(false);
            
        Debug.Log($"[PlantotronSequenceDropZone] Zone {insertIndex} enabled: {enabled}");
    }
    
    public void SetHighlight(bool highlight)
    {
        if (backgroundImage != null)
        {
            backgroundImage.color = highlight ? highlightColor : normalColor;
            Debug.Log($"[PlantotronSequenceDropZone] Zone {insertIndex} highlighted: {highlight}");
        }
    }
    
    public bool CanAcceptDrop()
    {
        bool canAccept = isEnabled && parentUI != null && parentUI.GetCurrentSelectedSeed() != null;
        Debug.Log($"[PlantotronSequenceDropZone] Zone {insertIndex} can accept drop: {canAccept}");
        return canAccept;
    }
    
    // OLD METHOD - Keep for compatibility but make it handle both cases
    public void OnGeneDropped(NodeDefinition gene)
    {
        if (parentUI != null && CanAcceptDrop() && gene != null)
        {
            Debug.Log($"[PlantotronSequenceDropZone] Gene {gene.displayName} dropped on zone {insertIndex}");
            parentUI.TryAddGeneToSequence(gene, insertIndex);
        }
    }
    
    // FIXED: New IDropHandler implementation to handle actual drop events
    public void OnDrop(PointerEventData eventData)
    {
        Debug.Log($"[PlantotronSequenceDropZone] OnDrop triggered on zone {insertIndex}");
        
        if (!CanAcceptDrop())
        {
            Debug.Log($"[PlantotronSequenceDropZone] Zone {insertIndex} cannot accept drop");
            return;
        }
        
        GameObject draggedObject = eventData.pointerDrag;
        if (draggedObject == null)
        {
            Debug.LogWarning("[PlantotronSequenceDropZone] No dragged object found in drop event");
            return;
        }
        
        Debug.Log($"[PlantotronSequenceDropZone] Processing drop of {draggedObject.name}");
        
        // Handle drops from PlantotronGeneItem (gene inventory -> sequence)
        PlantotronGeneItem geneItem = draggedObject.GetComponent<PlantotronGeneItem>();
        if (geneItem != null)
        {
            // Get the gene from the gene item - we need to access its internal gene reference
            // Since PlantotronGeneItem doesn't expose the gene directly, we need to get it through the parent UI
            var allGeneItems = FindObjectsOfType<PlantotronGeneItem>();
            foreach (var item in allGeneItems)
            {
                if (item.gameObject == draggedObject)
                {
                    // We need to find which gene this represents by checking the inventory
                    if (PlayerGeneticsInventory.Instance != null)
                    {
                        // Try all available genes to find which one this item represents
                        foreach (var geneCount in PlayerGeneticsInventory.Instance.AvailableGenes)
                        {
                            if (geneCount.gene != null && geneCount.count > 0)
                            {
                                // Try adding this gene - TryAddGeneToSequence will handle validation
                                bool success = parentUI.TryAddGeneToSequence(geneCount.gene, insertIndex);
                                if (success)
                                {
                                    Debug.Log($"[PlantotronSequenceDropZone] Successfully added gene {geneCount.gene.displayName} at index {insertIndex}");
                                    return;
                                }
                            }
                        }
                    }
                    break;
                }
            }
            Debug.LogWarning("[PlantotronSequenceDropZone] Could not determine which gene was being dragged from inventory");
            return;
        }
        
        // Handle drops from PlantotronSequenceItem (internal sequence reordering)
        PlantotronSequenceItem sequenceItem = draggedObject.GetComponent<PlantotronSequenceItem>();
        if (sequenceItem != null)
        {
            NodeDefinition gene = sequenceItem.GetGene();
            if (gene != null)
            {
                Debug.Log($"[PlantotronSequenceDropZone] Moving sequence gene {gene.displayName} to index {insertIndex}");
                
                // For internal reordering, we need to move the gene rather than add a new one
                int fromIndex = sequenceItem.GetIndex();
                if (fromIndex != insertIndex)
                {
                    bool success = parentUI.TryMoveGeneInSequence(fromIndex, insertIndex);
                    if (success)
                    {
                        Debug.Log($"[PlantotronSequenceDropZone] Successfully moved gene from {fromIndex} to {insertIndex}");
                    }
                    else
                    {
                        Debug.LogWarning($"[PlantotronSequenceDropZone] Failed to move gene from {fromIndex} to {insertIndex}");
                    }
                }
                return;
            }
        }
        
        Debug.LogWarning($"[PlantotronSequenceDropZone] Unknown drag source: {draggedObject.name}");
    }
}