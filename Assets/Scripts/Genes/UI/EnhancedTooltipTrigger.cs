using UnityEngine;
using UnityEngine.EventSystems;
using Abracodabra.UI.Genes;

namespace Abracodabra.UI.Tooltips
{
    [RequireComponent(typeof(GeneSlotUI))]
    public class EnhancedTooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private GeneSlotUI slotUI;
        private Coroutine showTooltipCoroutine;
        private bool isPointerOver = false;

        [SerializeField] private float hoverDelay = 0.3f;

        private void Awake()
        {
            slotUI = GetComponent<GeneSlotUI>();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            isPointerOver = true;
            if (slotUI.CurrentItem != null && slotUI.CurrentItem.IsValid())
            {
                showTooltipCoroutine = StartCoroutine(ShowTooltipAfterDelay());
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            isPointerOver = false;
            if (showTooltipCoroutine != null)
            {
                StopCoroutine(showTooltipCoroutine);
            }
            InventoryTooltipPanel.Instance?.HideTooltip();
        }

        private System.Collections.IEnumerator ShowTooltipAfterDelay()
        {
            yield return new WaitForSeconds(hoverDelay);
            if(isPointerOver) // Check if pointer is still over the slot
            {
                InventoryTooltipPanel.Instance?.ShowTooltipForItem(slotUI.CurrentItem);
            }
        }

        private void OnDisable()
        {
            if (isPointerOver)
            {
                isPointerOver = false;
                if (showTooltipCoroutine != null)
                {
                    StopCoroutine(showTooltipCoroutine);
                }
                InventoryTooltipPanel.Instance?.HideTooltip();
            }
        }
    }
}