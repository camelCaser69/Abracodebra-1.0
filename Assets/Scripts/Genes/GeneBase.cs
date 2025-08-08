// File: Assets/Scripts/Genes/Core/GeneBase.cs
using UnityEngine;
using System;
using Abracodabra.Genes.Runtime; // For RuntimeGeneInstance in context

namespace Abracodabra.Genes.Core
{
    /// <summary>
    /// The abstract base class for all gene ScriptableObjects.
    /// Defines common properties and a persistent GUID for reliable referencing.
    /// </summary>
    public abstract class GeneBase : ScriptableObject, ITooltipDataProvider
    {
        [Header("Basic Info")]
        public string geneName;
        [TextArea(3, 5)]
        public string description;
        public Sprite icon;
        public int tier = 1;

        [Header("Visual")]
        public Color geneColor = Color.white;
        public GameObject effectPrefab;

        [Header("System")]
        [SerializeField, HideInInspector]
        private string _persistentGUID;
        [SerializeField]
        private int _version = 1;

        public string GUID
        {
            get
            {
#if UNITY_EDITOR
                if (string.IsNullOrEmpty(_persistentGUID))
                {
                    _persistentGUID = System.Guid.NewGuid().ToString();
                    UnityEditor.EditorUtility.SetDirty(this);
                }
#endif
                return _persistentGUID;
            }
        }

        public int Version => _version;
        public abstract GeneCategory Category { get; }

        // Interface Implementation
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

    /// <summary>
    /// Context object passed to GetTooltip to provide more detailed information.
    /// </summary>
    public class GeneTooltipContext
    {
        public PlantGrowth plant; // The plant the gene is on
        public bool showAdvanced; // Show detailed stats
        public RuntimeGeneInstance instance; // The specific instance with its current values
    }
}