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

    [Header("Node Name Display")]
    public TMP_Text nodeNameText;
    [Tooltip("If false, the node name text will be hidden.")]
    public bool displayNodeName = true;

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
        if (nodeNameText != null)
        {
            nodeNameText.text = data.nodeDisplayName;
            nodeNameText.gameObject.SetActive(displayNodeName);
        }
    }

    public NodeData GetNodeData() => nodeData;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (tooltipPanel != null && tooltipText != null)
        {
            tooltipPanel.SetActive(true);
            string tip = $"{nodeData.nodeDisplayName}\n{nodeDescription}\nEffects:\n";
            foreach (var eff in nodeEffects)
            {
                tip += $"- {eff.effectType}: {eff.primaryValue}";
                if (eff.secondaryValue != 0)
                    tip += $" / {eff.secondaryValue}";
                tip += "\n";
            }
            tooltipText.text = tip;
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (tooltipPanel != null)
            tooltipPanel.SetActive(false);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // Implement selection logic here if needed.
    }
}
