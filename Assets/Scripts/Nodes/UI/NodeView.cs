using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.Text;

[RequireComponent(typeof(RectTransform))]
public class NodeView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("UI Elements (Assign in Prefab)")]
    [Tooltip("Image component to display the NodeDefinition's thumbnail.")]
    [SerializeField] private Image thumbnailImage;
    [Tooltip("Image component for the node's background color/frame.")]
    [SerializeField] private Image backgroundImage;
    [Tooltip("Optional: GameObject used as the tooltip panel.")]
    [SerializeField] private GameObject tooltipPanel;
    [Tooltip("Optional: TextMeshProUGUI for displaying tooltip info.")]
    [SerializeField] private TMP_Text tooltipText;
    [Tooltip("Optional: TextMeshProUGUI to display the node's name below/on it.")]
    [SerializeField] private TMP_Text nodeNameText;

    [Header("Configuration")]
    [Tooltip("If true, the node name text will be shown.")]
    [SerializeField] private bool displayNodeName = false;

    // Runtime Data
    private NodeData _nodeData;
    private NodeDefinition _nodeDefinition;
    private NodeEditorGridController _controller;
    private NodeCell _parentCell;
    private Color _originalBackgroundColor; // Store the default background color

    public void Initialize(NodeData data, NodeDefinition definition, NodeEditorGridController controller)
    {
        _nodeData = data;
        _nodeDefinition = definition;
        _controller = controller;
        _parentCell = GetComponentInParent<NodeCell>();

        if (_nodeData == null || _nodeDefinition == null || _controller == null || _parentCell == null)
        {
            Debug.LogError($"[NodeView] Initialization failed: Null data, definition, controller, or parentCell. Name: {gameObject.name}", gameObject);
            gameObject.SetActive(false);
            return;
        }

        // --- Apply Visuals ---
        if (thumbnailImage != null)
        {
            thumbnailImage.sprite = _nodeDefinition.thumbnail;
            thumbnailImage.color = _nodeDefinition.thumbnailTintColor;
            thumbnailImage.rectTransform.localScale = _controller.NodeImageScale;
            thumbnailImage.enabled = (thumbnailImage.sprite != null);
        }
        else Debug.LogWarning($"[NodeView] Thumbnail Image not assigned in prefab: {gameObject.name}", gameObject);

        if (backgroundImage != null)
        {
            // **CRITICAL:** Store the original background color defined in the NodeDefinition
            _originalBackgroundColor = _nodeDefinition.backgroundColor;
            // **DEBUG LOG:** Verify the stored color immediately after assignment
            // Debug.Log($"[NodeView Initialize] Stored Original BG Color for {definition.displayName}: {_originalBackgroundColor}", gameObject);

            backgroundImage.color = _originalBackgroundColor; // Set initial color from stored value
            backgroundImage.enabled = true;
        }
        else Debug.LogWarning($"[NodeView] Background Image not assigned in prefab: {gameObject.name}", gameObject);

        if (tooltipPanel != null) tooltipPanel.SetActive(false);

        if (nodeNameText != null)
        {
            nodeNameText.text = _nodeData.nodeDisplayName;
            nodeNameText.gameObject.SetActive(displayNodeName);
        }
    }

    public NodeData GetNodeData() => _nodeData;
    public NodeDefinition GetNodeDefinition() => _nodeDefinition;
    public NodeCell GetParentCell() => _parentCell;

    // --- Selection Highlighting Methods ---

    public void Highlight()
    {
        if (backgroundImage != null && _controller != null)
        {
            // Debug.Log($"Highlighting NodeView in Cell {_parentCell?.CellIndex}. Current Color: {backgroundImage.color}, Setting to: {_controller.SelectedNodeBackgroundColor}", gameObject);
            backgroundImage.color = _controller.SelectedNodeBackgroundColor;
        }
    }

    public void Unhighlight()
    {
        if (backgroundImage != null)
        {
             // **DEBUG LOG:** Check the original color value *before* applying it
            // Debug.Log($"Unhighlighting NodeView in Cell {_parentCell?.CellIndex}. Current Color: {backgroundImage.color}, Resetting to Original: {_originalBackgroundColor}", gameObject);

            // Use the stored original color. If it's black here, the storing failed or it got overwritten.
            backgroundImage.color = _originalBackgroundColor;
        }
         else {
             // Debug.LogWarning($"[NodeView Unhighlight] Background Image is null for {gameObject.name}", gameObject);
         }
    }

    // --- Tooltip Handling ---
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
        if (tooltipPanel != null) tooltipPanel.SetActive(false);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            if (_parentCell != null)
            {
                // Debug.Log($"NodeView Clicked in Cell {_parentCell.CellIndex}. Requesting SelectCell.", gameObject);
                NodeCell.SelectCell(_parentCell);
            }
            else
            {
                Debug.LogError("[NodeView] ParentCell reference is null on click!", gameObject);
            }
        }
    }

    private string BuildTooltipString()
    {
        StringBuilder sb = new StringBuilder();
        sb.Append("<b>").Append(_nodeData.nodeDisplayName).Append("</b>\n");
        if (!string.IsNullOrEmpty(_nodeDefinition.description)) sb.Append(_nodeDefinition.description).Append("\n");
        if (_nodeData.effects != null && _nodeData.effects.Count > 0)
        {
            sb.Append("<i>Effects:</i>\n");
            foreach (var eff in _nodeData.effects)
            {
                sb.Append("- ").Append(eff.effectType.ToString()).Append(": ");
                sb.Append(eff.primaryValue.ToString("G3"));
                if (eff.secondaryValue != 0) sb.Append(" / ").Append(eff.secondaryValue.ToString("G3"));
                sb.Append("\n");
            }
        }
        return sb.ToString().TrimEnd();
    }
}