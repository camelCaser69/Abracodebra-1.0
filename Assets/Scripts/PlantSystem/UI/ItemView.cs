// Reworked File: Assets/Scripts/PlantSystem/UI/ItemView.cs
using UnityEngine;
using UnityEngine.UI;
using Abracodabra.Genes.Core;
using Abracodabra.Genes.Templates;
using Abracodabra.Genes.Runtime;

namespace Abracodabra.UI.Genes
{
    /// <summary>
    /// A component that holds the visual and data references for an item in a UI slot.
    /// It works in conjunction with a GeneSlotUI.
    /// </summary>
    public class ItemView : MonoBehaviour
    {
        [Header("Visual Elements")]
        [SerializeField] private Image thumbnailImage;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Sprite fallbackThumbnail;

        // Data References
        private GeneBase _gene;
        private RuntimeGeneInstance _runtimeInstance;
        private ToolDefinition _toolDefinition;
        private SeedTemplate _seedTemplate;

        private GeneSlotUI _parentSlot;
        private Color _originalBackgroundColor;

        void Awake()
        {
            _parentSlot = GetComponent<GeneSlotUI>();
        }

        public void InitializeAsGene(RuntimeGeneInstance instance)
        {
            _runtimeInstance = instance;
            _gene = instance.GetGene();
            _toolDefinition = null;
            _seedTemplate = null;
            SetupVisuals();
        }

        public void InitializeAsTool(ToolDefinition toolDef)
        {
            _runtimeInstance = null;
            _gene = null;
            _toolDefinition = toolDef;
            _seedTemplate = null;
            SetupVisuals();
        }

        public void InitializeAsSeed(SeedTemplate seed)
        {
            _runtimeInstance = null;
            _gene = null;
            _toolDefinition = null;
            _seedTemplate = seed;
            SetupVisuals();
        }

        private void SetupVisuals()
        {
            // Determine what this ItemView represents
            Sprite spriteToShow = fallbackThumbnail;
            Color tintColor = Color.white;
            _originalBackgroundColor = Color.gray;

            if (_gene != null)
            {
                spriteToShow = _gene.icon ?? fallbackThumbnail;
                tintColor = _gene.geneColor;
                _originalBackgroundColor = _gene.geneColor;
            }
            else if (_toolDefinition != null)
            {
                spriteToShow = _toolDefinition.icon ?? fallbackThumbnail;
                tintColor = _toolDefinition.iconTint;
                _originalBackgroundColor = Color.gray; // Placeholder for tool color
            }
            else if (_seedTemplate != null)
            {
                spriteToShow = _seedTemplate.icon ?? fallbackThumbnail;
                tintColor = Color.white;
                _originalBackgroundColor = Color.green; // Placeholder for seed color
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
        
        // Data Accessors
        public GeneBase GetGene() => _gene;
        public RuntimeGeneInstance GetRuntimeInstance() => _runtimeInstance;
        public ToolDefinition GetToolDefinition() => _toolDefinition;
        public SeedTemplate GetSeedTemplate() => _seedTemplate;
    }
}