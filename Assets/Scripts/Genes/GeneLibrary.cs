using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Abracodabra.Genes.Services;
using Abracodabra.Genes.Core;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Abracodabra.Genes
{
    public class GeneLibrary : ScriptableObject, IGeneLibrary
    {
        public static GeneLibrary Instance { get; set; }

        public void SetActiveInstance()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("An existing GeneLibrary instance was already active. It is being overwritten.", this);
            }
            Instance = this;
            Initialize();
        }

        public List<PassiveGene> passiveGenes = new List<PassiveGene>();
        public List<ActiveGene> activeGenes = new List<ActiveGene>();
        public List<ModifierGene> modifierGenes = new List<ModifierGene>();
        public List<PayloadGene> payloadGenes = new List<PayloadGene>();

        public List<GeneBase> starterGenes = new List<GeneBase>();
        public PlaceholderGene placeholderGene;

        private Dictionary<string, GeneBase> _guidLookup;
        private Dictionary<string, GeneBase> _nameLookup;

        private void Initialize()
        {
            BuildLookupCaches();
        }

        private void BuildLookupCaches()
        {
            _guidLookup = new Dictionary<string, GeneBase>();
            _nameLookup = new Dictionary<string, GeneBase>();

            foreach (var gene in GetAllGenes())
            {
                if (gene == null) continue;
                if (!string.IsNullOrEmpty(gene.GUID))
                {
                    if (!_guidLookup.ContainsKey(gene.GUID))
                    {
                        _guidLookup[gene.GUID] = gene;
                    }
                    else
                    {
                        Debug.LogWarning($"Gene Library: Duplicate GUID '{gene.GUID}' detected. The gene '{gene.name}' will be ignored by GUID lookup. The existing entry is '{_guidLookup[gene.GUID].name}'.", gene);
                    }
                }
                if (!string.IsNullOrEmpty(gene.geneName))
                {
                    if (!_nameLookup.ContainsKey(gene.geneName))
                    {
                        _nameLookup[gene.geneName] = gene;
                    }
                    else
                    {
                        Debug.LogWarning($"Gene Library: Duplicate gene name '{gene.geneName}' detected. The gene '{gene.name}' will be ignored by name lookup. The existing entry is '{_nameLookup[gene.geneName].name}'.", gene);
                    }
                }
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
            if (_guidLookup == null) BuildLookupCaches();
            return _guidLookup.TryGetValue(guid, out var gene) ? gene : null;
        }

        public GeneBase GetGeneByName(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            if (_nameLookup == null) BuildLookupCaches();
            return _nameLookup.TryGetValue(name, out var gene) ? gene : null;
        }

        public GeneBase GetPlaceholderGene()
        {
            if (placeholderGene == null)
            {
                Debug.LogError("PlaceholderGene is not assigned in the GeneLibrary asset! The system may be unstable. Please assign it in the editor.", this);
                // Create a temporary instance as a last resort to prevent crashes.
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
        private void OnValidate()
        {
            // This ensures a placeholder gene is always assigned in the editor.
            if (placeholderGene == null)
            {
                string[] guids = AssetDatabase.FindAssets("t:PlaceholderGene");
                if (guids.Length > 0)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    placeholderGene = AssetDatabase.LoadAssetAtPath<PlaceholderGene>(path);
                    Debug.LogWarning("GeneLibrary's PlaceholderGene was unassigned. Auto-assigned from project assets.", this);
                    EditorUtility.SetDirty(this);
                }
                else
                {
                    Debug.LogError("Could not find a 'PlaceholderGene' asset in the project. Please create one.", this);
                }
            }
        }

        private void AutoPopulate()
        {
            passiveGenes.Clear();
            activeGenes.Clear();
            modifierGenes.Clear();
            payloadGenes.Clear();

            string[] guids = AssetDatabase.FindAssets("t:GeneBase");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GeneBase gene = AssetDatabase.LoadAssetAtPath<GeneBase>(path);

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
            
            // Sort lists alphabetically for consistency
            passiveGenes = passiveGenes.OrderBy(g => g.name).ToList();
            activeGenes = activeGenes.OrderBy(g => g.name).ToList();
            modifierGenes = modifierGenes.OrderBy(g => g.name).ToList();
            payloadGenes = payloadGenes.OrderBy(g => g.name).ToList();
            
            EditorUtility.SetDirty(this);
            Debug.Log($"Auto-populated library: {passiveGenes.Count} passive, {activeGenes.Count} active, {modifierGenes.Count} modifier, {payloadGenes.Count} payload genes.");
        }
#endif
    }
}