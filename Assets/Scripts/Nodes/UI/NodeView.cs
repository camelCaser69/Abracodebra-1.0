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
    private NodeEditorGridController _sequenceGridControllerRef; // Controller if this view is in the main sequence
    private NodeCell _parentCell; // The cell this view is currently in
    private Color _originalBackgroundColor;

    public void Initialize(NodeData data, NodeDefinition definition, NodeEditorGridController sequenceController)
    {
        _nodeData = data;
        _nodeDefinition = definition;
        _sequenceGridControllerRef = sequenceController; // This will be null if it's an inventory item

        UpdateParentCellReference(); // Call early

        if (_nodeData == null || _nodeDefinition == null)
        {
            Debug.LogError($"[NodeView Initialize] NodeData or NodeDefinition is null for {gameObject.name}. Disabling.", gameObject);
            gameObject.SetActive(false);
            return;
        }

        // Thumbnail Image Setup
        if (thumbnailImage != null)
        {
            thumbnailImage.sprite = _nodeDefinition.thumbnail;
            thumbnailImage.color = _nodeDefinition.thumbnailTintColor;

            Vector3 imageScale = Vector3.one; // Default to (1,1,1)
            if (InventoryGridController.Instance != null)
            {
                // Prioritize scale from InventoryGridController if it exists, making it global
                imageScale = InventoryGridController.Instance.InventoryNodeImageScale;
            }
            else
            {
                // Fallback if InventoryGridController isn't available (e.g., testing scene without it)
                Debug.LogWarning("[NodeView] InventoryGridController.Instance not found during Initialize. Using default image scale (1,1,1).", gameObject);
            }
            thumbnailImage.rectTransform.localScale = imageScale;

            thumbnailImage.enabled = (_nodeDefinition.thumbnail != null);
            if (!thumbnailImage.raycastTarget) thumbnailImage.raycastTarget = true; // Ensure clickable
        }
        else
        {
            Debug.LogWarning($"[NodeView {gameObject.name}] ThumbnailImage is not assigned in prefab.", gameObject);
        }


        // Background Image Setup
        if (backgroundImage != null)
        {
            _originalBackgroundColor = _nodeDefinition.backgroundColor;
            backgroundImage.color = _originalBackgroundColor;
            backgroundImage.enabled = true;
            if (!backgroundImage.raycastTarget) backgroundImage.raycastTarget = true; // Ensure clickable
        }
        else
        {
            Debug.LogWarning($"[NodeView {gameObject.name}] BackgroundImage is not assigned in prefab.", gameObject);
        }

        // Tooltip Setup
        if (tooltipPanel != null) tooltipPanel.SetActive(false);

        // Node Name Text Setup
        if (nodeNameText != null)
        {
            nodeNameText.text = _nodeData.nodeDisplayName;
            nodeNameText.gameObject.SetActive(displayNodeName);
        }
    }

    public void UpdateParentCellReference()
    {
        _parentCell = GetComponentInParent<NodeCell>();
        // No warning here as it can be null during drag's reparenting
    }

    public NodeData GetNodeData() => _nodeData;
    public NodeDefinition GetNodeDefinition() => _nodeDefinition;
    public NodeCell GetParentCell() => _parentCell;

    public void Highlight()
    {
        // Only highlight if it's a sequence node and has the necessary references
        if (backgroundImage != null && _sequenceGridControllerRef != null && _parentCell != null && !_parentCell.IsInventoryCell)
        {
            backgroundImage.color = _sequenceGridControllerRef.SelectedNodeBackgroundColor;
        }
    }

    public void Unhighlight()
    {
        if (backgroundImage != null)
        {
            backgroundImage.color = _originalBackgroundColor; // Revert to its definition's color
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
        if (_parentCell == null) UpdateParentCellReference(); // Attempt to get parent if null

        if (_parentCell != null && eventData.button == PointerEventData.InputButton.Left)
        {
            if (!_parentCell.IsInventoryCell) // Only select actual sequence nodes
            {
                NodeCell.SelectCell(_parentCell);
            }
            // Left-clicking inventory items will still allow drag to start due to NodeDraggable
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

        // Determine if effects should be shown (i.e., it's a "real" node in the sequence)
        bool showEffects = false;
        if (_parentCell != null && !_parentCell.IsInventoryCell) // If it's in a sequence cell
        {
            showEffects = true;
        }
        else if (_parentCell == null && _sequenceGridControllerRef != null) // If it's being initialized for sequence but not yet in a cell
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
                    (eff.effectType == NodeEffectType.ScentModifier && eff.scentDefinitionReference != null) || /* Add other types that use secondaryValue even if 0 */
                    (eff.effectType == NodeEffectType.PoopFertilizer) ) // PoopFertilizer uses secondary for energy
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
        return sb.ToString().TrimEnd();
    }
}