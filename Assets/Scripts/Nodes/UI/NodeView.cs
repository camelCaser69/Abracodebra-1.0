// FILE: Assets/Scripts/Nodes/UI/NodeView.cs (MODIFIED)
// Remove the old tooltip-related code and add TooltipTrigger

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

[RequireComponent(typeof(RectTransform))]
public class NodeView : MonoBehaviour, IPointerDownHandler
{
    [Header("UI Elements (Assign in Prefab)")]
    [SerializeField] private Image thumbnailImage;
    [SerializeField] private Image backgroundImage;

    private NodeData _nodeData;
    private NodeDefinition _nodeDefinition;
    private NodeEditorGridController _sequenceGridControllerRef; 
    private NodeCell _parentCell; 
    private Color _originalBackgroundColor;
    private TooltipTrigger tooltipTrigger; // NEW

    public void Initialize(NodeData data, NodeDefinition definition, NodeEditorGridController sequenceController)
    {
        _nodeData = data;
        _nodeDefinition = definition;
        _sequenceGridControllerRef = sequenceController; 

        UpdateParentCellReference(); 

        if (_nodeData == null || _nodeDefinition == null)
        {
            Debug.LogError($"[NodeView Initialize] NodeData or NodeDefinition is null for {gameObject.name}. Disabling.", gameObject);
            gameObject.SetActive(false);
            return;
        }

        if (!_nodeData.IsSeed())
        {
            _nodeData.ClearStoredSequence();
        }

        // Setup tooltip trigger
        tooltipTrigger = GetComponent<TooltipTrigger>();
        if (tooltipTrigger == null)
        {
            tooltipTrigger = gameObject.AddComponent<TooltipTrigger>();
        }

        float globalScaleFactor = 1f;
        float raycastPaddingValue = 0f;

        if (InventoryGridController.Instance != null)
        {
            globalScaleFactor = InventoryGridController.Instance.NodeGlobalImageScale;
            raycastPaddingValue = InventoryGridController.Instance.NodeImageRaycastPadding;
        }
        else
        {
            Debug.LogWarning("[NodeView] InventoryGridController.Instance not found during Initialize. Using default image scale (1) and padding (0).", gameObject);
        }
        Vector4 raycastPaddingVector = new Vector4(raycastPaddingValue, raycastPaddingValue, raycastPaddingValue, raycastPaddingValue);

        if (thumbnailImage != null)
        {
            thumbnailImage.sprite = _nodeDefinition.thumbnail;
            thumbnailImage.color = _nodeDefinition.thumbnailTintColor;
            thumbnailImage.rectTransform.localScale = new Vector3(globalScaleFactor, globalScaleFactor, 1f);
            thumbnailImage.enabled = (_nodeDefinition.thumbnail != null);
            thumbnailImage.raycastTarget = true; 
            thumbnailImage.raycastPadding = raycastPaddingVector; 
        }
        else
        {
            Debug.LogWarning($"[NodeView {gameObject.name}] ThumbnailImage is not assigned in prefab.", gameObject);
        }

        if (backgroundImage != null) 
        {
            _originalBackgroundColor = _nodeDefinition.backgroundColor;
            backgroundImage.color = _originalBackgroundColor;
            backgroundImage.enabled = true;
            backgroundImage.raycastTarget = true; 
            backgroundImage.raycastPadding = raycastPaddingVector; 
        }
        else
        {
            Debug.LogWarning($"[NodeView {gameObject.name}] BackgroundImage for NodeView itself is not assigned in prefab.", gameObject);
        }
    }

    public void UpdateParentCellReference()
    {
        _parentCell = GetComponentInParent<NodeCell>();
    }

    public NodeData GetNodeData() => _nodeData;
    public NodeDefinition GetNodeDefinition() => _nodeDefinition;
    public NodeCell GetParentCell() => _parentCell;

    public void Highlight()
    {
        if (backgroundImage != null && _sequenceGridControllerRef != null && _parentCell != null && !_parentCell.IsInventoryCell)
        {
            backgroundImage.color = _sequenceGridControllerRef.SelectedNodeBackgroundColor;
        }
    }

    public void Unhighlight()
    {
        if (backgroundImage != null)
        {
            backgroundImage.color = _originalBackgroundColor; 
        }
    }

    // REMOVED: OnPointerEnter and OnPointerExit methods

    public void OnPointerDown(PointerEventData eventData)
    {
        if (_parentCell == null) UpdateParentCellReference(); 

        if (_parentCell != null && eventData.button == PointerEventData.InputButton.Left)
        {
            if (!_parentCell.IsInventoryCell && !_parentCell.IsSeedSlot) 
            {
                NodeCell.SelectCell(_parentCell);
            }
        }
    }

    // REMOVED: BuildTooltipString method
}