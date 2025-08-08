// REWORKED FILE: Assets/Scripts/Genes/GeneLibrary.cs
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Abracodabra.Genes.Services;
using Abracodabra.Genes.Core;

namespace Abracodabra.Genes
{
    [CreateAssetMenu(fileName = "GeneLibrary", menuName = "Abracodabra/Gene Library")]
    public class GeneLibrary : ScriptableObject, IGeneLibrary
    {
        private static GeneLibrary _instance;
        public static GeneLibrary Instance
        {
            get
            {
                if (_instance == null)
                {
                    // FIX: This is now the ONLY way the instance is loaded.
                    // It ensures consistency by always loading from the same path.
                    _instance = Resources.Load<GeneLibrary>("GeneLibrary");
                    if (_instance == null)
                    {
                        Debug.LogError("FATAL: GeneLibrary asset not found at 'Assets/Resources/GeneLibrary.asset'!");
                    }
                }
                return _instance;
            }
        }

        [Header("Gene Collections")]
        public List<PassiveGene> passiveGenes = new List<PassiveGene>();
        public List<ActiveGene> activeGenes = new List<ActiveGene>();
        public List<ModifierGene> modifierGenes = new List<ModifierGene>();
        public List<PayloadGene> payloadGenes = new List<PayloadGene>();

        [Header("Starter & System Genes")]
        public List<GeneBase> starterGenes = new List<GeneBase>();
        public PlaceholderGene placeholderGene;

        // Caches for fast runtime access
        private Dictionary<string, GeneBase> _guidLookup;
        private Dictionary<string, GeneBase> _nameLookup;

        // OnEnable is called when the ScriptableObject is loaded.
        void OnEnable()
        {
            BuildLookupCaches();
        }

        void BuildLookupCaches()
        {
            _guidLookup = new Dictionary<string, GeneBase>();
            _nameLookup = new Dictionary<string, GeneBase>();

            foreach (var gene in GetAllGenes())
            {
                if (gene == null) continue;

                if (!string.IsNullOrEmpty(gene.GUID) && !_guidLookup.ContainsKey(gene.GUID))
                    _guidLookup[gene.GUID] = gene;

                if (!string.IsNullOrEmpty(gene.geneName) && !_nameLookup.ContainsKey(gene.geneName))
                    _nameLookup[gene.geneName] = gene;
            }
        }

        public IEnumerable<GeneBase> GetAllGenes()
        {
            foreach (var g in passiveGenes) if (g != null) yield return g;
            foreach (var g in activeGenes) if (g != null) yield return g;
            foreach (var g in modifierGenes) if (g != null) yield return g;
            foreach (var g in payloadGenes) if (g != null) yield return g;
        }

        public GeneBase GetGeneByGUID(string guid)
        {
            if (string.IsNullOrEmpty(guid)) return null;
            if (_guidLookup == null) BuildLookupCaches(); // Safety check
            return _guidLookup.TryGetValue(guid, out var gene) ? gene : null;
        }

        public GeneBase GetGeneByName(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            if (_nameLookup == null) BuildLookupCaches(); // Safety check
            return _nameLookup.TryGetValue(name, out var gene) ? gene : null;
        }

        public GeneBase GetPlaceholderGene()
        {
            if (placeholderGene == null)
            {
                Debug.LogError("PlaceholderGene is not assigned in the GeneLibrary asset!");
                placeholderGene = ScriptableObject.CreateInstance<PlaceholderGene>();
                placeholderGene.name = "RUNTIME_PLACEHOLDER";
            }
            return placeholderGene;
        }

        public List<GeneBase> GetGenesOfCategory(GeneCategory category)
        {
            switch (category)
            {
                case GeneCategory.Passive: return passiveGenes.Cast<GeneBase>().ToList();
                case GeneCategory.Active: return activeGenes.Cast<GeneBase>().ToList();
                case GeneCategory.Modifier: return modifierGenes.Cast<GeneBase>().ToList();
                case GeneCategory.Payload: return payloadGenes.Cast<GeneBase>().ToList();
                default: return new List<GeneBase>();
            }
        }

#if UNITY_EDITOR
        [ContextMenu("Auto-populate Library from Project")]
        void AutoPopulate()
        {
            passiveGenes.Clear();
            activeGenes.Clear();
            modifierGenes.Clear();
            payloadGenes.Clear();

            string[] guids = UnityEditor.AssetDatabase.FindAssets("t:GeneBase");
            foreach (string guid in guids)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                GeneBase gene = UnityEditor.AssetDatabase.LoadAssetAtPath<GeneBase>(path);

                if (gene is PlaceholderGene) continue;

                if (gene is PassiveGene p)
                    passiveGenes.Add(p);
                else if (gene is ActiveGene a)
                    activeGenes.Add(a);
                else if (gene is ModifierGene m)
                    modifierGenes.Add(m);
                else if (gene is PayloadGene pay)
                    payloadGenes.Add(pay);
            }

            UnityEditor.EditorUtility.SetDirty(this);
            Debug.Log($"Auto-populated library: {passiveGenes.Count} passive, {activeGenes.Count} active, {modifierGenes.Count} modifier, {payloadGenes.Count} payload genes.");
        }
#endif
    }
}