using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Abracodabra.UI.Genes;
using Abracodabra.Genes.Templates;

namespace Abracodabra.UI.Tooltips
{
    public class InventoryTooltipPanel : MonoBehaviour
    {
        public static InventoryTooltipPanel Instance { get; private set; }

        [Header("UI References")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Image itemIcon;
        [SerializeField] private TextMeshProUGUI itemNameText;
        [SerializeField] private TextMeshProUGUI qualityText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private TextMeshProUGUI keyStat1Text; // Max Energy
        [SerializeField] private TextMeshProUGUI keyStat2Text; // Growth Chance
        [SerializeField] private TextMeshProUGUI keyStat3Text; // Cycle Cost

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            if(panelRoot != null) panelRoot.SetActive(false);
        }

        public void ShowTooltipForItem(InventoryBarItem item)
        {
            if (item == null || !item.IsValid() || panelRoot == null)
            {
                HideTooltip();
                return;
            }

            switch (item.Type)
            {
                case InventoryBarItem.ItemType.Seed:
                    DisplaySeedTooltip(item);
                    break;
                case InventoryBarItem.ItemType.Gene:
                case InventoryBarItem.ItemType.Tool:
                case InventoryBarItem.ItemType.Resource:
                    DisplayGenericTooltip(item);
                    break;
            }
            
            panelRoot.SetActive(true);
        }

        public void HideTooltip()
        {
            if(panelRoot != null) panelRoot.SetActive(false);
        }

        private void DisplaySeedTooltip(InventoryBarItem item)
        {
            var data = SeedTooltipData.CreateFromSeed(item.SeedTemplate, item.SeedRuntimeState);
            if (data == null)
            {
                HideTooltip();
                return;
            }

            itemIcon.sprite = item.GetIcon();
            itemNameText.text = data.seedName;
            descriptionText.text = $"<i>{item.SeedTemplate.description}</i>";
            
            qualityText.text = SeedQualityCalculator.GetQualityDescription(data.qualityTier);
            qualityText.color = SeedQualityCalculator.GetQualityColor(data.qualityTier);
            
            keyStat1Text.text = $"<sprite=energy> {item.SeedTemplate.maxEnergy * data.energyStorageMultiplier:F0} E";
            keyStat2Text.text = $"<sprite=growth> {item.SeedTemplate.baseGrowthChance * data.growthSpeedMultiplier:P0}/tick";
            keyStat3Text.text = $"<sprite=cycle> {data.totalModifiedEnergyCost:F0} E/cycle";
        }
        
        private void DisplayGenericTooltip(InventoryBarItem item)
        {
            itemIcon.sprite = item.GetIcon();
            itemNameText.text = item.GetDisplayName();
            
            // Simplified view for non-seeds
            qualityText.text = item.Type.ToString();
            qualityText.color = Color.grey;

            string desc = "";
             switch (item.Type)
             {
                case InventoryBarItem.ItemType.Gene:
                    desc = item.GeneInstance?.GetGene()?.description ?? "";
                    break;
                case InventoryBarItem.ItemType.Tool:
                    desc = item.ToolDefinition?.GetTooltipDetails() ?? "";
                    break;
                case InventoryBarItem.ItemType.Resource:
                     desc = item.ItemInstance?.definition?.description ?? "";
                     break;
             }
            descriptionText.text = $"<i>{desc}</i>";

            keyStat1Text.text = "";
            keyStat2Text.text = "";
            keyStat3Text.text = "";
        }
    }
}