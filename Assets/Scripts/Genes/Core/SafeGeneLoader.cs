// File: Assets/Scripts/Genes/Core/SafeGeneLoader.cs
using UnityEngine;
using Abracodabra.Genes.Services;
using Abracodabra.Genes.Runtime;

namespace Abracodabra.Genes.Core
{
    public static class SafeGeneLoader
    {
        public static GeneBase LoadGeneWithFallback(string guid, string fallbackName = null)
        {
            var library = GeneServices.Get<IGeneLibrary>();
            if (library == null)
            {
                Debug.LogError("Gene Library service not available! Cannot load gene.");
                return null;
            }

            // Try GUID first, as it's the most reliable
            var gene = library.GetGeneByGUID(guid);
            if (gene != null) return gene;

            // Try name as a fallback if the GUID fails
            if (!string.IsNullOrEmpty(fallbackName))
            {
                gene = library.GetGeneByName(fallbackName);
                if (gene != null)
                {
                    Debug.LogWarning($"Gene with GUID '{guid}' not found, but a gene with the fallback name '{fallbackName}' was loaded instead. The original asset may have been deleted or its GUID changed.");
                    return gene;
                }
            }

            // If all else fails, return the placeholder gene
            Debug.LogError($"Could not find gene with GUID '{guid}' or fallback name '{fallbackName}'. Returning placeholder.");
            return library.GetPlaceholderGene();
        }
    }
}