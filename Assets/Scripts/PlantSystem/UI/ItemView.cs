using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ItemView : MonoBehaviour, IPointerDownHandler
{
    #region Serialized Fields
    [Header("UI References")]
    [SerializeField] private Image thumbnailImage;
    [SerializeField] private Image backgroundImage;
    #endregion

    #region Private State
    // Item Data
    private NodeData _nodeData;
    private NodeDefinition _nodeDefinition;
    private ToolDefinition _toolDefinition;

    // Context & State
    private NodeEditorGridController _sequenceGridControllerRef;
    private NodeCell _parentCell;
    private Color _originalBackgroundColor;
    private TooltipTrigger _tooltipTrigger;
    private DisplayType _displayType;

    // --- FIXED: The enum must be public to be used by a public method ---
    public enum DisplayType { None, Node, Tool }
    #endregion

    #region Initialization
    /// <summary>
    /// Initializes the view to display a Node.
    /// </summary>
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

        // A node inside a sequence cannot itself be a seed or contain a sequence.
        if (!_nodeData.IsSeed())
        {
            _nodeData.ClearStoredSequence();
        }

        SetupVisuals();
    }

    /// <summary>
    /// Initializes the view to display a Tool.
    /// </summary>
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

    void SetupVisuals() {
        UpdateParentCellReference();
        _tooltipTrigger = GetComponent<TooltipTrigger>() ?? gameObject.AddComponent<TooltipTrigger>();

        float globalScaleFactor = 1f;
        float raycastPaddingValue = 0f;
        if (InventoryGridController.Instance != null) {
            globalScaleFactor = InventoryGridController.Instance.NodeGlobalImageScale;
            raycastPaddingValue = InventoryGridController.Instance.NodeImageRaycastPadding;
        }

        Vector4 raycastPaddingVector = new Vector4(raycastPaddingValue, raycastPaddingValue, raycastPaddingValue, raycastPaddingValue);

        if (thumbnailImage != null) {
            thumbnailImage.sprite = (_displayType == DisplayType.Node) ? _nodeDefinition.thumbnail : _toolDefinition.icon;
            thumbnailImage.color = (_displayType == DisplayType.Node) ? _nodeDefinition.thumbnailTintColor : _toolDefinition.iconTint;
            thumbnailImage.rectTransform.localScale = new Vector3(globalScaleFactor, globalScaleFactor, 1f);
            thumbnailImage.enabled = (thumbnailImage.sprite != null);
            thumbnailImage.raycastTarget = true;
            thumbnailImage.raycastPadding = raycastPaddingVector;
        }

        if (backgroundImage != null) {
            // Use InventoryColorManager if available, otherwise fall back to definition colors
            if (InventoryColorManager.Instance != null) {
                _originalBackgroundColor = InventoryColorManager.Instance.GetCellColorForItem(_nodeData, _nodeDefinition, _toolDefinition);
            }
            else {
                // Fallback to original behavior
                _originalBackgroundColor = (_displayType == DisplayType.Node) ? _nodeDefinition.backgroundColor : new Color(0.5f, 0.5f, 0.5f, 1f);
            }
        
            backgroundImage.color = _originalBackgroundColor;
            backgroundImage.enabled = true;
            backgroundImage.raycastTarget = true;
            backgroundImage.raycastPadding = raycastPaddingVector;
        }
    }
    #endregion
    
    #region Public Methods & Properties
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
    #endregion

    #region Event Handlers
    public void OnPointerDown(PointerEventData eventData)
    {
        if (_parentCell == null) UpdateParentCellReference();

        if (_parentCell != null && eventData.button == PointerEventData.InputButton.Left)
        {
            // Selection logic only applies to Nodes in the sequence editor
            if (!_parentCell.IsInventoryCell && !_parentCell.IsSeedSlot && _displayType == DisplayType.Node)
            {
                NodeCell.SelectCell(_parentCell);
            }
        }
    }
    #endregion
}