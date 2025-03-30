using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.Text;

[RequireComponent(typeof(RectTransform))]
public class NodeView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler
{
    // --- Fields remain the same ---
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
    private NodeEditorGridController _controller;
    private NodeCell _parentCell;
    private Color _originalBackgroundColor;

    // --- Initialize, UpdateParentCellReference, Getters, Highlight, Unhighlight remain the same ---
     public void Initialize(NodeData data, NodeDefinition definition, NodeEditorGridController controller)
    { /* ... as before ... */
         _nodeData = data; _nodeDefinition = definition; _controller = controller;
         UpdateParentCellReference();
         if (_nodeData == null || _nodeDefinition == null || _controller == null || _parentCell == null) { gameObject.SetActive(false); return; }
         if (thumbnailImage != null) { thumbnailImage.sprite = _nodeDefinition.thumbnail; thumbnailImage.color = _nodeDefinition.thumbnailTintColor; thumbnailImage.rectTransform.localScale = _controller.NodeImageScale; thumbnailImage.enabled = (thumbnailImage.sprite != null); if (!thumbnailImage.raycastTarget) thumbnailImage.raycastTarget = true; }
         if (backgroundImage != null) { _originalBackgroundColor = _nodeDefinition.backgroundColor; backgroundImage.color = _originalBackgroundColor; backgroundImage.enabled = true; if (!backgroundImage.raycastTarget) backgroundImage.raycastTarget = true; }
         if (tooltipPanel != null) tooltipPanel.SetActive(false);
         if (nodeNameText != null) { nodeNameText.text = _nodeData.nodeDisplayName; nodeNameText.gameObject.SetActive(displayNodeName); }
    }
     public void UpdateParentCellReference() { _parentCell = GetComponentInParent<NodeCell>(); /* ... null check ... */ }
     public NodeData GetNodeData() => _nodeData;
     public NodeDefinition GetNodeDefinition() => _nodeDefinition;
     public NodeCell GetParentCell() => _parentCell;
     public void Highlight() { if (backgroundImage != null && _controller != null) backgroundImage.color = _controller.SelectedNodeBackgroundColor; }
     public void Unhighlight() { if (backgroundImage != null) backgroundImage.color = _originalBackgroundColor; }


    // --- Tooltip Handling with Logging ---
    public void OnPointerEnter(PointerEventData eventData)
    {
        // --- Add Log ---
        Debug.Log($"[NodeView OnPointerEnter] Fired on {gameObject.name}. Tooltip Panel assigned: {tooltipPanel != null}", gameObject);

        // Ensure the CanvasGroup allows raycasts IF it exists
        CanvasGroup cg = GetComponent<CanvasGroup>();
        if (cg != null && cg.blocksRaycasts == false)
        {
             Debug.LogWarning($"--> PointerEnter detected but CanvasGroup is blocking raycasts! Drag state might be stuck?", gameObject);
             // Optionally force it: cg.blocksRaycasts = true;
        }


        if (tooltipPanel != null && tooltipText != null && _nodeDefinition != null && _nodeData != null)
        {
            // --- Add Log ---
            // Debug.Log("--> Activating Tooltip Panel");
            tooltipPanel.SetActive(true);
            tooltipText.text = BuildTooltipString();
        }
         else if (tooltipPanel == null) {
             // Debug.LogWarning("--> Tooltip Panel is not assigned in Inspector.", gameObject);
         }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // --- Add Log ---
        // Debug.Log($"[NodeView OnPointerExit] Fired on {gameObject.name}", gameObject);

        if (tooltipPanel != null)
        {
             // Debug.Log("--> Deactivating Tooltip Panel");
            tooltipPanel.SetActive(false);
        }
    }

    // --- Pointer Down remains the same ---
    public void OnPointerDown(PointerEventData eventData) { /* ... as before ... */
         // Debug.Log($"[NodeView OnPointerDown] Fired on: {gameObject.name} in Cell: {_parentCell?.CellIndex} | Button: {eventData.button}", gameObject);
        if (eventData.button == PointerEventData.InputButton.Left) {
            if (_parentCell != null) {
                 // Debug.Log($"--> Left Click Confirmed. Requesting SelectCell for {_parentCell.CellIndex}");
                NodeCell.SelectCell(_parentCell);
            } else { UpdateParentCellReference(); if (_parentCell != null) NodeCell.SelectCell(_parentCell); else Debug.LogError("[NodeView] ParentCell null on PointerDown!", gameObject); }
        }
    }

    // --- BuildTooltipString remains the same ---
    private string BuildTooltipString() { /* ... as before ... */
         StringBuilder sb = new StringBuilder(); sb.Append("<b>").Append(_nodeData.nodeDisplayName).Append("</b>\n"); if (!string.IsNullOrEmpty(_nodeDefinition.description)) sb.Append(_nodeDefinition.description).Append("\n"); if (_nodeData.effects != null && _nodeData.effects.Count > 0) { sb.Append("<i>Effects:</i>\n"); foreach (var eff in _nodeData.effects) { sb.Append("- ").Append(eff.effectType.ToString()).Append(": "); sb.Append(eff.primaryValue.ToString("G3")); if (eff.secondaryValue != 0) sb.Append(" / ").Append(eff.secondaryValue.ToString("G3")); sb.Append("\n"); } } return sb.ToString().TrimEnd();
     }
}