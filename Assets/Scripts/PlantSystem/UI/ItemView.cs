// Reworked File: Assets/Scripts/PlantSystem/UI/ItemView.cs
using UnityEngine;
using UnityEngine.UI;
using Abracodabra.Genes.Core;
using Abracodabra.Genes.Templates;
using Abracodabra.Genes.Runtime;

namespace Abracodabra.UI.Genes
{
    public class ItemView : MonoBehaviour
    {
        [SerializeField] private Image thumbnailImage;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Sprite fallbackThumbnail;

        private GeneBase _gene;
        private RuntimeGeneInstance _runtimeInstance;
        private ToolDefinition _toolDefinition;
        private SeedTemplate _seedTemplate;

        private Color _originalBackgroundColor;

        public void InitializeAsGene(RuntimeGeneInstance instance)
        {
            _runtimeInstance = instance;
            _gene = instance.GetGene();
            _toolDefinition = null;
            _seedTemplate = null;
            SetupVisuals();
            gameObject.SetActive(true);
        }

        public void InitializeAsTool(ToolDefinition toolDef)
        {
            _runtimeInstance = null;
            _gene = null;
            _toolDefinition = toolDef;
            _seedTemplate = null;
            SetupVisuals();
            gameObject.SetActive(true);
        }

        public void InitializeAsSeed(SeedTemplate seed)
        {
            _runtimeInstance = null;
            _gene = null;
            _toolDefinition = null;
            _seedTemplate = seed;
            SetupVisuals();
            gameObject.SetActive(true);
        }

        private void SetupVisuals()
        {
            Sprite spriteToShow = fallbackThumbnail;
            Color tintColor = Color.white;
            _originalBackgroundColor = Color.gray;

            if (_gene != null)
            {
                spriteToShow = _gene.icon ?? fallbackThumbnail;
                tintColor = _gene.geneColor;
                _originalBackgroundColor = _gene.geneColor.WithAlpha(0.5f);
            }
            else if (_toolDefinition != null)
            {
                spriteToShow = _toolDefinition.icon ?? fallbackThumbnail;
                tintColor = _toolDefinition.iconTint;
                _originalBackgroundColor = InventoryColorManager.Instance.GetCellColorForItem(null, null, _toolDefinition);
            }
            else if (_seedTemplate != null)
            {
                spriteToShow = _seedTemplate.icon ?? fallbackThumbnail;
                tintColor = Color.white;
                _originalBackgroundColor = InventoryColorManager.Instance.GetCellColorForItem(null, _seedTemplate, null);
            }

            if (thumbnailImage != null)
            {
                thumbnailImage.sprite = spriteToShow;
                thumbnailImage.color = tintColor;
                thumbnailImage.enabled = (thumbnailImage.sprite != null);
            }

            if (backgroundImage != null)
            {
                backgroundImage.color = _originalBackgroundColor;
            }
        }
        
        public void Clear()
        {
            _gene = null;
            _runtimeInstance = null;
            _toolDefinition = null;
            _seedTemplate = null;
            gameObject.SetActive(false);
        }

        public GeneBase GetGene() => _gene;
        public RuntimeGeneInstance GetRuntimeInstance() => _runtimeInstance;
        public ToolDefinition GetToolDefinition() => _toolDefinition;
        public SeedTemplate GetSeedTemplate() => _seedTemplate;
    }
}