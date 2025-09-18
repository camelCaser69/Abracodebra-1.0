using UnityEngine;

namespace Abracodabra.UI.Tooltips
{
    public class TooltipSystemManager : MonoBehaviour
    {
        [Header("Tooltip Panel Prefabs")]
        [SerializeField] private GameObject inventoryTooltipPanelPrefab;
        [SerializeField] private GameObject seedEditorTooltipPanelPrefab;

        [Header("Parent Canvases")]
        [SerializeField] private Transform mainUICanvas;

        void Awake()
        {
            // Instantiate inventory tooltip panel if it doesn't exist
            if (InventoryTooltipPanel.Instance == null && inventoryTooltipPanelPrefab != null)
            {
                Instantiate(inventoryTooltipPanelPrefab, mainUICanvas);
            }
            
            // Instantiate seed editor tooltip panel if it doesn't exist
            if (SeedEditorTooltipPanel.Instance == null && seedEditorTooltipPanelPrefab != null)
            {
                Instantiate(seedEditorTooltipPanelPrefab, mainUICanvas);
            }
        }
    }
}