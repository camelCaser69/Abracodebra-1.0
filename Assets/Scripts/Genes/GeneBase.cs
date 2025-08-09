using UnityEngine;
using Abracodabra.Genes.Runtime; // For RuntimeGeneInstance in context

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Abracodabra.Genes.Core
{
    public abstract class GeneBase : ScriptableObject, ITooltipDataProvider
    {
        public string geneName;
        public string description;
        public Sprite icon;
        public int tier = 1;

        public Color geneColor = Color.white;
        public GameObject effectPrefab;

        [SerializeField]
        private string _persistentGUID;
        [SerializeField]
        private int _version = 1;

#if UNITY_EDITOR
        private void OnValidate()
        {
            // This ensures the GUID is assigned and saved as soon as the asset is created or modified in the editor.
            if (string.IsNullOrEmpty(_persistentGUID))
            {
                _persistentGUID = System.Guid.NewGuid().ToString();
                EditorUtility.SetDirty(this);
            }
        }
#endif

        public string GUID
        {
            get
            {
#if UNITY_EDITOR
                // This getter remains as a fallback, ensuring a GUID is always available in the editor, even for old assets.
                if (string.IsNullOrEmpty(_persistentGUID))
                {
                    _persistentGUID = System.Guid.NewGuid().ToString();
                    EditorUtility.SetDirty(this);
                }
#endif
                return _persistentGUID;
            }
        }

        public int Version => _version;
        public abstract GeneCategory Category { get; }

        public abstract string GetTooltip(GeneTooltipContext context);
        public string GetTooltipTitle() => geneName;
        public string GetTooltipDescription() => $"Tier {tier} {Category} Gene";
        public string GetTooltipDetails(object source = null)
        {
            if (source is GeneTooltipContext context)
            {
                return GetTooltip(context);
            }
            return description;
        }

        public virtual bool CanAttachTo(GeneBase other) => false;
        public virtual void MigrateFromVersion(int oldVersion, GeneInstanceData instanceData) { }
    }

    public enum GeneCategory
    {
        Passive,
        Active,
        Modifier,
        Payload
    }

    public class GeneTooltipContext
    {
        public PlantGrowth plant; // The plant the gene is on
        public bool showAdvanced; // Show detailed stats
        public RuntimeGeneInstance instance; // The specific instance with its current values
    }
}