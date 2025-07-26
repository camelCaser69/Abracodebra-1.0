// Assets/Scripts/PlantSystem/UI/ItemView.cs

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ItemView : MonoBehaviour, IPointerDownHandler
{
    [SerializeField] private Image thumbnailImage;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Sprite fallbackThumbnail; // New field for a fallback icon

    private NodeData _nodeData;
    private NodeDefinition _nodeDefinition;
    private ToolDefinition _toolDefinition;

    private NodeEditorGridController _sequenceGridControllerRef;
    private NodeCell _parentCell;
    private Color _originalBackgroundColor;
    private TooltipTrigger _tooltipTrigger;
    private DisplayType _displayType;

    public enum DisplayType { None, Node, Tool }

    public void Initialize(NodeData data, NodeDefinition definition, NodeEditorGridController sequenceController)
    {
        _displayType = DisplayType.Node;
        _nodeData = data;
        _nodeDefinition = definition;
        _sequenceGridControllerRef = sequenceController;

        if (_nodeData == null || _nodeDefinition == null)
        {
            Debug.LogError($"[ItemView Initialize Node] NodeData or NodeDefinition is null for {gameObject.name}. Disabling.", gameObject);
            gameObject.SetActive(false);
            return;
        }

        if (!_nodeData.IsSeed())
        {
            _nodeData.ClearStoredSequence();
        }

        SetupVisuals();
    }

    public void Initialize(NodeData data, ToolDefinition toolDef)
    {
        _displayType = DisplayType.Tool;
        _nodeData = data; // This is the wrapper NodeData for the tool
        _toolDefinition = toolDef;

        if (_toolDefinition == null)
        {
            Debug.LogError($"[ItemView Initialize Tool] ToolDefinition is null for {gameObject.name}. Disabling.", gameObject);
            gameObject.SetActive(false);
            return;
        }

        SetupVisuals();
    }

    private void SetupVisuals()
    {
        UpdateParentCellReference();
        _tooltipTrigger = GetComponent<TooltipTrigger>() ?? gameObject.AddComponent<TooltipTrigger>();

        float globalScaleFactor = 1f;
        float raycastPaddingValue = 0f;
        if (InventoryGridController.Instance != null)
        {
            globalScaleFactor = InventoryGridController.Instance.NodeGlobalImageScale;
            raycastPaddingValue = InventoryGridController.Instance.NodeImageRaycastPadding;
        }

        Vector4 raycastPaddingVector = new Vector4(raycastPaddingValue, raycastPaddingValue, raycastPaddingValue, raycastPaddingValue);

        if (thumbnailImage != null)
        {
            // --- MODIFIED LOGIC START ---
            Sprite spriteToShow = (_displayType == DisplayType.Node) ? _nodeDefinition.thumbnail : _toolDefinition.icon;

            // If the intended sprite is null, try to use the fallback.
            if (spriteToShow == null)
            {
                spriteToShow = fallbackThumbnail;
                if (_displayType == DisplayType.Node)
                {
                    Debug.LogWarning($"[ItemView] NodeDefinition '{_nodeDefinition.displayName}' is missing a thumbnail. Using fallback.", this);
                }
            }
            
            thumbnailImage.sprite = spriteToShow;
            thumbnailImage.color = (_displayType == DisplayType.Node) ? _nodeDefinition.thumbnailTintColor : _toolDefinition.iconTint;
            thumbnailImage.rectTransform.localScale = new Vector3(globalScaleFactor, globalScaleFactor, 1f);
            thumbnailImage.enabled = (thumbnailImage.sprite != null); // Now this is less likely to be false
            // --- MODIFIED LOGIC END ---

            thumbnailImage.raycastTarget = true;
            thumbnailImage.raycastPadding = raycastPaddingVector;
        }

        if (backgroundImage != null)
        {
            if (InventoryColorManager.Instance != null)
            {
                _originalBackgroundColor = InventoryColorManager.Instance.GetCellColorForItem(_nodeData, _nodeDefinition, _toolDefinition);
            }
            else
            {
                _originalBackgroundColor = (_displayType == DisplayType.Node) ? _nodeDefinition.backgroundColor : new Color(0.5f, 0.5f, 0.5f, 1f);
            }

            backgroundImage.color = _originalBackgroundColor;
            backgroundImage.enabled = true;
            backgroundImage.raycastTarget = true;
            backgroundImage.raycastPadding = raycastPaddingVector;
        }
    }

    public NodeData GetNodeData() => _nodeData;
    public NodeDefinition GetNodeDefinition() => _nodeDefinition;
    public ToolDefinition GetToolDefinition() => _toolDefinition;
    public NodeCell GetParentCell() => _parentCell;
    public DisplayType GetDisplayType() => _displayType;

    public void UpdateParentCellReference()
    {
        _parentCell = GetComponentInParent<NodeCell>();
    }

    public void Highlight()
    {
        if (backgroundImage != null && _sequenceGridControllerRef != null && _parentCell != null && !_parentCell.IsInventoryCell && _displayType == DisplayType.Node)
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

    public void OnPointerDown(PointerEventData eventData)
    {
        if (_parentCell == null) UpdateParentCellReference();

        if (_parentCell != null && eventData.button == PointerEventData.InputButton.Left)
        {
            if (!_parentCell.IsInventoryCell && !_parentCell.IsSeedSlot && _displayType == DisplayType.Node)
            {
                NodeCell.SelectCell(_parentCell);
            }
        }
    }
}