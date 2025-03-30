using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.Text; // For StringBuilder

// Requires RectTransform, Image, maybe CanvasGroup
[RequireComponent(typeof(RectTransform))]
public class NodeView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler // Removed IPointerClickHandler for now
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
    [SerializeField] private bool displayNodeName = false; // Default to false unless explicitly designed for

    // Runtime Data
    private NodeData _nodeData;
    private NodeDefinition _nodeDefinition;
    private NodeEditorGridController _controller; // Reference to controller for visual settings

    /// <summary>
    /// Initializes the NodeView with its data, definition, and controller reference.
    /// Applies visual settings from the controller and definition.
    /// </summary>
    public void Initialize(NodeData data, NodeDefinition definition, NodeEditorGridController controller)
    {
        _nodeData = data;
        _nodeDefinition = definition;
        _controller = controller;

        if (_nodeData == null || _nodeDefinition == null || _controller == null)
        {
            Debug.LogError("[NodeView] Initialization failed due to null data, definition, or controller.");
            gameObject.SetActive(false); // Disable if initialization fails
            return;
        }

        // --- Apply Visuals ---

        // Thumbnail Image
        if (thumbnailImage != null)
        {
            thumbnailImage.sprite = _nodeDefinition.thumbnail; // Use thumbnail from definition
            thumbnailImage.color = _controller.NodeImageColor; // Use color from controller config
            thumbnailImage.rectTransform.localScale = _controller.NodeImageScale; // Use scale from controller config
            // Ensure image is enabled if a sprite exists
            thumbnailImage.enabled = (thumbnailImage.sprite != null);
        }
        else
        {
            Debug.LogWarning($"[NodeView] Thumbnail Image is not assigned in the prefab for {gameObject.name}");
        }

        // Background Image / Color
        if (backgroundImage != null)
        {
            backgroundImage.color = _nodeDefinition.backgroundColor; // Use background color from definition
            backgroundImage.enabled = true; // Ensure background is visible
        }

        // Tooltip Panel
        if (tooltipPanel != null)
        {
            tooltipPanel.SetActive(false); // Start with tooltip hidden
        }

        // Node Name Text
        if (nodeNameText != null)
        {
            nodeNameText.text = _nodeData.nodeDisplayName; // Use display name from data/definition
            nodeNameText.gameObject.SetActive(displayNodeName); // Set visibility based on config
        }
    }

    /// <summary>
    /// Returns the NodeData associated with this view.
    /// </summary>
    public NodeData GetNodeData() => _nodeData;

    /// <summary>
    /// Returns the NodeDefinition associated with this view.
    /// </summary>
    public NodeDefinition GetNodeDefinition() => _nodeDefinition;


    // --- Tooltip Handling ---

    public void OnPointerEnter(PointerEventData eventData)
    {
        // Show tooltip only if panel and text components are assigned
        if (tooltipPanel != null && tooltipText != null && _nodeDefinition != null && _nodeData != null)
        {
            tooltipPanel.SetActive(true);
            tooltipText.text = BuildTooltipString();

            // Optional: Adjust tooltip position if needed (e.g., follow mouse or fixed offset)
            // RectTransform tooltipRect = tooltipPanel.GetComponent<RectTransform>();
            // Position adjustment logic here...
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // Hide tooltip
        if (tooltipPanel != null)
        {
            tooltipPanel.SetActive(false);
        }
    }

    /// <summary>
    /// Constructs the formatted string for the tooltip.
    /// </summary>
    private string BuildTooltipString()
    {
        StringBuilder sb = new StringBuilder();

        // Node Name (Bold)
        sb.Append("<b>").Append(_nodeData.nodeDisplayName).Append("</b>\n");

        // Description
        if (!string.IsNullOrEmpty(_nodeDefinition.description))
        {
            sb.Append(_nodeDefinition.description).Append("\n");
        }

        // Effects (if any)
        if (_nodeData.effects != null && _nodeData.effects.Count > 0)
        {
            sb.Append("<i>Effects:</i>\n"); // Italic header for effects
            foreach (var eff in _nodeData.effects)
            {
                sb.Append("- ").Append(eff.effectType.ToString()).Append(": ");
                sb.Append(eff.primaryValue.ToString("G3")); // Format number nicely
                if (eff.secondaryValue != 0) // Show secondary value only if non-zero
                {
                    sb.Append(" / ").Append(eff.secondaryValue.ToString("G3"));
                }
                sb.Append("\n");
            }
        }

        return sb.ToString().TrimEnd(); // Trim trailing newline
    }
}