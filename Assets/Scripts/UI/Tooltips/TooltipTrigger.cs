// Reworked File: Assets/Scripts/UI/Tooltips/TooltipTrigger.cs
using UnityEngine;
using UnityEngine.EventSystems;
using Abracodabra.UI.Genes; // For ItemView
using Abracodabra.Genes.Core; // For GeneTooltipContext

public class TooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private ItemView _itemView;
    private bool _isShowingTooltip = false;

    void Awake()
    {
        _itemView = GetComponent<ItemView>();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (UniversalTooltipManager.Instance == null || _isShowingTooltip || _itemView == null) return;
        ShowTooltip();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (UniversalTooltipManager.Instance == null || !_isShowingTooltip) return;
        HideTooltip();
    }

    void ShowTooltip()
    {
        ITooltipDataProvider provider = null;
        GeneTooltipContext context = new GeneTooltipContext();

        if (_itemView.GetGene() != null)
        {
            provider = _itemView.GetGene();
            context.instance = _itemView.GetRuntimeInstance();
        }
        else if (_itemView.GetToolDefinition() != null)
        {
            provider = _itemView.GetToolDefinition();
        }
        else if (_itemView.GetSeedTemplate() != null)
        {
            // Assuming SeedTemplate will implement ITooltipDataProvider
            // provider = _itemView.GetSeedTemplate(); 
        }

        if (provider != null)
        {
            UniversalTooltipManager.Instance.ShowTooltip(provider, transform, context);
            _isShowingTooltip = true;
        }
    }

    void HideTooltip()
    {
        UniversalTooltipManager.Instance?.HideTooltip();
        _isShowingTooltip = false;
    }

    void OnDisable()
    {
        if (_isShowingTooltip)
        {
            HideTooltip();
        }
    }
}