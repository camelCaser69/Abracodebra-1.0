// FILE: Assets/Scripts/UI/TooltipTrigger.cs (SIMPLIFIED)
using UnityEngine;
using UnityEngine.EventSystems;

public class TooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Tooltip Type")]
    [SerializeField] private TooltipType tooltipType = TooltipType.Auto;
    
    public enum TooltipType
    {
        Auto,      // Detect from components
        Node,      // Force node tooltip
        Tool,      // Force tool tooltip
    }
    
    private NodeView nodeView;
    private ToolView toolView;
    private NodeCell nodeCell;
    private bool isShowingTooltip = false;

    void Awake()
    {
        // Cache references
        nodeView = GetComponent<NodeView>();
        toolView = GetComponent<ToolView>();
        nodeCell = GetComponentInParent<NodeCell>();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (UniversalTooltipManager.Instance == null) return;
        
        // Don't show if already showing
        if (isShowingTooltip) return;
        
        ShowTooltip();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (UniversalTooltipManager.Instance == null) return;
        
        HideTooltip();
    }

    private void ShowTooltip()
    {
        switch (tooltipType)
        {
            case TooltipType.Auto:
                ShowAutoTooltip();
                break;
            case TooltipType.Node:
                ShowNodeTooltip();
                break;
            case TooltipType.Tool:
                ShowToolTooltip();
                break;
        }
        
        isShowingTooltip = true;
    }

    private void ShowAutoTooltip()
    {
        // Try to get from cell first (for inventory bar display items)
        if (nodeCell != null)
        {
            if (nodeCell.GetToolDefinition() != null)
            {
                UniversalTooltipManager.Instance.ShowToolTooltip(nodeCell.GetToolDefinition());
                return;
            }
            else if (nodeCell.GetNodeData() != null && nodeCell.GetNodeView() != null)
            {
                var nodeData = nodeCell.GetNodeData();
                var nodeDef = nodeCell.GetNodeView().GetNodeDefinition();
                if (nodeDef != null)
                {
                    UniversalTooltipManager.Instance.ShowNodeTooltip(nodeData, nodeDef);
                    return;
                }
            }
        }
        
        // Try direct components
        if (toolView != null)
        {
            UniversalTooltipManager.Instance.ShowToolTooltip(toolView.GetToolDefinition());
        }
        else if (nodeView != null)
        {
            UniversalTooltipManager.Instance.ShowNodeTooltip(
                nodeView.GetNodeData(), 
                nodeView.GetNodeDefinition()
            );
        }
    }

    private void ShowNodeTooltip()
    {
        NodeData data = null;
        NodeDefinition def = null;
        
        if (nodeView != null)
        {
            data = nodeView.GetNodeData();
            def = nodeView.GetNodeDefinition();
        }
        else if (nodeCell != null)
        {
            data = nodeCell.GetNodeData();
            if (nodeCell.GetNodeView() != null)
                def = nodeCell.GetNodeView().GetNodeDefinition();
        }
        
        if (data != null && def != null)
        {
            UniversalTooltipManager.Instance.ShowNodeTooltip(data, def);
        }
    }

    private void ShowToolTooltip()
    {
        ToolDefinition def = null;
        
        if (toolView != null)
        {
            def = toolView.GetToolDefinition();
        }
        else if (nodeCell != null)
        {
            def = nodeCell.GetToolDefinition();
        }
        
        if (def != null)
        {
            UniversalTooltipManager.Instance.ShowToolTooltip(def);
        }
    }

    private void HideTooltip()
    {
        if (!isShowingTooltip) return;
        
        UniversalTooltipManager.Instance.HideTooltip();
        isShowingTooltip = false;
    }

    void OnDisable()
    {
        if (isShowingTooltip)
        {
            HideTooltip();
        }
    }

    void OnDestroy()
    {
        if (isShowingTooltip)
        {
            HideTooltip();
        }
    }
}