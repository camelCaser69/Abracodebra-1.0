using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

[RequireComponent(typeof(RectTransform))]
public class ToolView : MonoBehaviour, IPointerDownHandler
{
    [Header("UI Elements (Assign in Prefab)")]
    [SerializeField] private Image thumbnailImage;
    [SerializeField] private Image backgroundImage;
    // REMOVED: tooltipPanel and tooltipText
    [SerializeField] private TMP_Text nodeNameText;

    private NodeData _nodeData;
    private ToolDefinition _toolDefinition;
    private Color _originalBackgroundColor;
    private TooltipTrigger tooltipTrigger; // NEW

    public void Initialize(NodeData data, ToolDefinition toolDef)
    {
        _nodeData = data;
        _toolDefinition = toolDef;

        if (_nodeData != null)
        {
            _nodeData.storedSequence = null;
            _nodeData.ClearStoredSequence();
        
            if (_nodeData.effects != null)
            {
                _nodeData.effects.RemoveAll(e => e != null && e.effectType == NodeEffectType.SeedSpawn);
            }
        }

        if (_toolDefinition == null)
        {
            Debug.LogError($"[ToolView Initialize] ToolDefinition is null for {gameObject.name}. Disabling.", gameObject);
            gameObject.SetActive(false);
            return;
        }
        tooltipTrigger = GetComponent<TooltipTrigger>();
       if (tooltipTrigger == null)
       {
           tooltipTrigger = gameObject.AddComponent<TooltipTrigger>();
       }

       float globalScaleFactor = 1f;
       float raycastPaddingValue = 0f;

       if (InventoryGridController.Instance != null)
       {
           globalScaleFactor = InventoryGridController.Instance.NodeGlobalImageScale;
           raycastPaddingValue = InventoryGridController.Instance.NodeImageRaycastPadding;
       }

       Vector4 raycastPaddingVector = new Vector4(raycastPaddingValue, raycastPaddingValue, raycastPaddingValue, raycastPaddingValue);

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
           _originalBackgroundColor = backgroundImage.color; 
           backgroundImage.enabled = true;
           backgroundImage.raycastTarget = true;
           backgroundImage.raycastPadding = raycastPaddingVector;
       }

       if (nodeNameText != null)
       {
           nodeNameText.text = _toolDefinition.displayName;
           nodeNameText.gameObject.SetActive(false); 
       }
   }

   public NodeData GetNodeData() => _nodeData;
   public ToolDefinition GetToolDefinition() => _toolDefinition;

   // REMOVED: OnPointerEnter and OnPointerExit methods

   public void OnPointerDown(PointerEventData eventData)
   {
       // Tools in inventory don't get selected like nodes do
   }

   // REMOVED: BuildTooltipString method
}