using UnityEngine;
using UnityEngine.EventSystems;

public class TooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private TooltipType tooltipType = TooltipType.Auto;

    public enum TooltipType
    {
        Auto,
        Node,
        Tool,
    }

    private ItemView itemView;
    private NodeCell nodeCell;
    private bool isShowingTooltip = false;

    void Awake()
    {
        itemView = GetComponent<ItemView>();
        nodeCell = GetComponentInParent<NodeCell>();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (UniversalTooltipManager.Instance == null || isShowingTooltip) return;
        ShowTooltip();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (UniversalTooltipManager.Instance == null || !isShowingTooltip) return;
        HideTooltip();
    }

    private void ShowTooltip()
    {
        switch (tooltipType)
        {
            case TooltipType.Auto: ShowAutoTooltip(); break;
            case TooltipType.Node: ShowNodeTooltip(); break;
            case TooltipType.Tool: ShowToolTooltip(); break;
        }
        isShowingTooltip = true;
    }

    private void ShowAutoTooltip()
    {
        // Prioritize the ItemView on this component first
        if (itemView != null)
        {
            if (itemView.GetToolDefinition() != null)
            {
                UniversalTooltipManager.Instance.ShowToolTooltip(itemView.GetToolDefinition());
                return;
            }
            if (itemView.GetNodeDefinition() != null)
            {
                UniversalTooltipManager.Instance.ShowNodeTooltip(itemView.GetNodeData(), itemView.GetNodeDefinition());
                return;
            }
        }
        
        // Fallback to checking the parent cell's data
        if (nodeCell != null)
        {
            if (nodeCell.GetToolDefinition() != null)
            {
                UniversalTooltipManager.Instance.ShowToolTooltip(nodeCell.GetToolDefinition());
            }
            else if (nodeCell.GetNodeDefinition() != null)
            {
                UniversalTooltipManager.Instance.ShowNodeTooltip(nodeCell.GetNodeData(), nodeCell.GetNodeDefinition());
            }
        }
    }

    private void ShowNodeTooltip()
    {
        NodeData data = itemView?.GetNodeData() ?? nodeCell?.GetNodeData();
        NodeDefinition def = itemView?.GetNodeDefinition() ?? nodeCell?.GetNodeDefinition();

        if (data != null && def != null)
        {
            UniversalTooltipManager.Instance.ShowNodeTooltip(data, def);
        }
    }

    private void ShowToolTooltip()
    {
        ToolDefinition def = itemView?.GetToolDefinition() ?? nodeCell?.GetToolDefinition();

        if (def != null)
        {
            UniversalTooltipManager.Instance.ShowToolTooltip(def);
        }
    }

    private void HideTooltip()
    {
        UniversalTooltipManager.Instance?.HideTooltip();
        isShowingTooltip = false;
    }

    void OnDisable()
    {
        if (isShowingTooltip) HideTooltip();
    }
}