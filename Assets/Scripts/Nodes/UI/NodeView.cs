using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class NodeView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("UI Elements")]
    public Image thumbnailImage;
    public Image backgroundImage;
    public GameObject tooltipPanel;  
    public TMP_Text tooltipText;

    // >>> ADD THIS <<<
    [Header("Node Name Display")]
    public TMP_Text nodeNameText; // Assign in your prefab

    private NodeData nodeData;
    private string nodeDescription;
    private List<NodeEffectData> nodeEffects;

    public void Initialize(NodeData data, Sprite thumbnail, Color bgColor, string description, List<NodeEffectData> effects)
    {
        nodeData = data;
        nodeDescription = description;
        nodeEffects = effects;

        if (thumbnailImage != null)
            thumbnailImage.sprite = thumbnail;

        if (backgroundImage != null)
            backgroundImage.color = bgColor;

        if (tooltipPanel != null)
            tooltipPanel.SetActive(false);

        // >>> ADD THIS <<<
        // Show the node's name from nodeData.nodeDisplayName
        if (nodeNameText != null)
            nodeNameText.text = data.nodeDisplayName;
    }

    public NodeData GetNodeData() => nodeData;

    public void OnPointerEnter(PointerEventData eventData)
    {
        // Show tooltip
        if (tooltipPanel != null && tooltipText != null)
        {
            tooltipPanel.SetActive(true);
            string tooltipStr = $"{nodeDescription}\n";
            foreach (var eff in nodeEffects)
            {
                tooltipStr += $"- {eff.effectType}: {eff.primaryValue}";
                if (eff.secondaryValue != 0)
                    tooltipStr += $" / {eff.secondaryValue}";
                tooltipStr += "\n";
            }
            tooltipText.text = tooltipStr;
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (tooltipPanel != null)
            tooltipPanel.SetActive(false);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        NodeSelectable.Select(gameObject);
    }
}
