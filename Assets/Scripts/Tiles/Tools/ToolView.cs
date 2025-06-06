// FILE: Assets/Scripts/UI/ToolView.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

[RequireComponent(typeof(RectTransform))]
public class ToolView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler
{
    [Header("UI Elements (Assign in Prefab)")]
    [SerializeField] private Image thumbnailImage;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private GameObject tooltipPanel;
    [SerializeField] private TMP_Text tooltipText;
    [SerializeField] private TMP_Text nodeNameText;

    private NodeData _nodeData;
    private ToolDefinition _toolDefinition;
    private Color _originalBackgroundColor;

        
    public void Initialize(NodeData data, ToolDefinition toolDef)
{
    _nodeData = data;
    _toolDefinition = toolDef;

    if (_toolDefinition == null)
    {
        Debug.LogError($"[ToolView Initialize] ToolDefinition is null for {gameObject.name}. Disabling.", gameObject);
        gameObject.SetActive(false);
        return;
    }

    // Get global scale and padding from InventoryGridController
    float globalScaleFactor = 1f;
    float raycastPaddingValue = 0f;

    if (InventoryGridController.Instance != null)
    {
        globalScaleFactor = InventoryGridController.Instance.NodeGlobalImageScale;
        raycastPaddingValue = InventoryGridController.Instance.NodeImageRaycastPadding;
    }

    Vector4 raycastPaddingVector = new Vector4(raycastPaddingValue, raycastPaddingValue, raycastPaddingValue, raycastPaddingValue);

    // SIMPLIFIED: Only set up assigned components (from your prefab)
    if (thumbnailImage != null)
    {
        thumbnailImage.sprite = _toolDefinition.icon;
        thumbnailImage.color = _toolDefinition.iconTint;
        thumbnailImage.rectTransform.localScale = new Vector3(globalScaleFactor, globalScaleFactor, 1f);
        thumbnailImage.enabled = (_toolDefinition.icon != null);
        thumbnailImage.raycastTarget = true;
        thumbnailImage.raycastPadding = raycastPaddingVector;
    }
    else
    {
        Debug.LogWarning($"[ToolView {gameObject.name}] ThumbnailImage not assigned in prefab!", gameObject);
    }

    if (backgroundImage != null)
    {
        _originalBackgroundColor = backgroundImage.color; // Use existing color from prefab
        backgroundImage.enabled = true;
        backgroundImage.raycastTarget = true;
        backgroundImage.raycastPadding = raycastPaddingVector;
    }

    if (tooltipPanel != null) 
        tooltipPanel.SetActive(false);

    if (nodeNameText != null)
    {
        nodeNameText.text = _toolDefinition.displayName;
        nodeNameText.gameObject.SetActive(false); // Usually hidden for tools
    }
}

    public NodeData GetNodeData() => _nodeData;
    public ToolDefinition GetToolDefinition() => _toolDefinition;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (tooltipPanel != null && tooltipText != null && _toolDefinition != null)
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
        // Tools in inventory don't get selected like nodes do
        // Selection is handled by the inventory bar
    }

    private string BuildTooltipString()
    {
        if (_toolDefinition == null) return "Invalid Tool";

        string tooltip = $"<b>{_toolDefinition.displayName}</b>\n";
        tooltip += $"Tool Type: {_toolDefinition.toolType}\n";
        
        if (_toolDefinition.limitedUses)
        {
            tooltip += $"Uses: {_toolDefinition.initialUses}\n";
        }
        else
        {
            tooltip += "Uses: Unlimited\n";
        }

        return tooltip.TrimEnd();
    }
}