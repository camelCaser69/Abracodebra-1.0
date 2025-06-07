using UnityEngine;
using UnityEngine.EventSystems;

public class TooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private ItemView _itemView;
    private NodeCell _nodeCell;
    private bool _isShowingTooltip = false;

    private void Awake()
    {
        // Cache references to potential data sources
        _itemView = GetComponent<ItemView>();
        _nodeCell = GetComponentInParent<NodeCell>();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (UniversalTooltipManager.Instance == null || _isShowingTooltip) return;
        
        ShowTooltip();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (UniversalTooltipManager.Instance == null || !_isShowingTooltip) return;

        HideTooltip();
    }

    private void ShowTooltip()
    {
        ITooltipDataProvider provider = null;
        object sourceData = null; // For passing NodeData to a NodeDefinition provider

        // Prioritize ItemView if it exists
        if (_itemView != null)
        {
            if (_itemView.GetToolDefinition() != null)
            {
                provider = _itemView.GetToolDefinition();
            }
            else if (_itemView.GetNodeDefinition() != null)
            {
                provider = _itemView.GetNodeDefinition();
                sourceData = _itemView.GetNodeData();
            }
        }
        // Fallback to NodeCell if no ItemView or ItemView has no provider
        else if (_nodeCell != null)
        {
            if (_nodeCell.GetToolDefinition() != null)
            {
                provider = _nodeCell.GetToolDefinition();
            }
            else if (_nodeCell.GetNodeDefinition() != null)
            {
                provider = _nodeCell.GetNodeDefinition();
                sourceData = _nodeCell.GetNodeData();
            }
        }
        
        if (provider != null)
        {
            UniversalTooltipManager.Instance.ShowTooltip(provider, transform, sourceData);
            _isShowingTooltip = true;
        }
    }

    private void HideTooltip()
    {
        UniversalTooltipManager.Instance?.HideTooltip();
        _isShowingTooltip = false;
    }

    private void OnDisable()
    {
        if (_isShowingTooltip)
        {
            HideTooltip();
        }
    }
}