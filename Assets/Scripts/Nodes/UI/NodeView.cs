// FILE: Assets/Scripts/Nodes/UI/NodeView.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System.Text;

[RequireComponent(typeof(RectTransform))]
public class NodeView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler
{
    [Header("UI Elements (Assign in Prefab)")]
    [SerializeField] private Image thumbnailImage;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private GameObject tooltipPanel;
    [SerializeField] private TMP_Text tooltipText;
    [SerializeField] private TMP_Text nodeNameText;

    [Header("Configuration")]
    [SerializeField] private bool displayNodeName = false;

    private NodeData _nodeData;
    private NodeDefinition _nodeDefinition;
    private NodeEditorGridController _sequenceGridControllerRef; 
    private NodeCell _parentCell; 
    private Color _originalBackgroundColor;

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

        // CRITICAL: If this NodeData is for a node element (not a seed container),
        // its storedSequence MUST be null to prevent serialization cycles.
        // If it IS a seed, its storedSequence should be handled by EnsureSeedSequenceInitialized elsewhere.
        if (!_nodeData.IsSeed())
        {
            _nodeData.ClearStoredSequence();
        }
        // For seed items, _nodeData.storedSequence should already be initialized (e.g., when added to inventory).
        // We don't call EnsureSeedSequenceInitialized here because this NodeView might be for an element
        // within a sequence editor, where IsSeed() would be false.

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

        if (tooltipPanel != null) tooltipPanel.SetActive(false);

        if (nodeNameText != null)
        {
            nodeNameText.text = _nodeData.nodeDisplayName;
            nodeNameText.gameObject.SetActive(displayNodeName);
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

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (tooltipPanel != null && tooltipText != null && _nodeDefinition != null && _nodeData != null)
        {
            tooltipPanel.SetActive(true);
            tooltipText.text = BuildTooltipString();
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (tooltipPanel != null)
        {
            tooltipPanel.SetActive(false);
        }
    }

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

    private string BuildTooltipString()
    {
        if (_nodeData == null || _nodeDefinition == null) return "Invalid Node Data";

        StringBuilder sb = new StringBuilder();
        sb.Append("<b>").Append(_nodeData.nodeDisplayName ?? "Unnamed Node").Append("</b>\n");

        if (!string.IsNullOrEmpty(_nodeDefinition.description))
        {
            sb.Append(_nodeDefinition.description).Append("\n");
        }

        bool showEffects = false;
        // Show effects if it's in the sequence editor or being initialized for it
        if ((_parentCell != null && !_parentCell.IsInventoryCell && !_parentCell.IsSeedSlot) || 
            (_parentCell == null && _sequenceGridControllerRef != null))
        {
            showEffects = true;
        }


        if (showEffects && _nodeData.effects != null && _nodeData.effects.Count > 0)
        {
            sb.Append("<i>Effects:</i>\n");
            foreach (var eff in _nodeData.effects)
            {
                if (eff == null) continue;
                sb.Append("- ").Append(eff.effectType.ToString()).Append(": ");
                sb.Append(eff.primaryValue.ToString("G3"));
                if (eff.secondaryValue != 0 || 
                    (eff.effectType == NodeEffectType.ScentModifier && eff.scentDefinitionReference != null) || 
                    (eff.effectType == NodeEffectType.PoopFertilizer) ) 
                {
                    sb.Append(" / ").Append(eff.secondaryValue.ToString("G3"));
                }
                if (eff.effectType == NodeEffectType.ScentModifier && eff.scentDefinitionReference != null)
                {
                    sb.Append(" (").Append(eff.scentDefinitionReference.displayName).Append(")");
                }
                sb.Append("\n");
            }
        }
        else if (_nodeData.IsSeed() && _nodeData.storedSequence != null) // Tooltip for seeds in inventory (or seed slot)
        {
            sb.Append("<i>Sequence:</i> ");
            if (_nodeData.storedSequence.nodes != null && _nodeData.storedSequence.nodes.Count > 0)
            {
                sb.Append(_nodeData.storedSequence.nodes.Count).Append(" nodes\n");
            }
            else
            {
                sb.Append("Empty\n");
            }
        }
        return sb.ToString().TrimEnd();
    }
}