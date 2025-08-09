// File: Assets/Scripts/Genes/Runtime/RuntimeGeneInstance.cs
using System;
using System.Collections.Generic;
using UnityEngine;
using Abracodabra.Genes.Core;

namespace Abracodabra.Genes.Runtime
{
    [Serializable]
    public class RuntimeGeneInstance : ISerializationCallbackReceiver
    {
        [SerializeField] private string geneGUID;
        [SerializeField] private string geneName; // Fallback for missing genes
        [SerializeField] private GeneInstanceData instanceData;

        [NonSerialized] private GeneBase cachedGene;

        public RuntimeGeneInstance(GeneBase sourceGene)
        {
            if (sourceGene == null)
            {
                Debug.LogError("Cannot create RuntimeGeneInstance from a null sourceGene!");
                return;
            }

            geneGUID = sourceGene.GUID;
            geneName = sourceGene.geneName;
            instanceData = new GeneInstanceData();
            cachedGene = sourceGene;
        }

        public T GetGene<T>() where T : GeneBase
        {
            if (cachedGene == null)
                LoadGene();
            return cachedGene as T;
        }

        public GeneBase GetGene()
        {
            if (cachedGene == null)
                LoadGene();
            return cachedGene;
        }

        private void LoadGene()
        {
            cachedGene = SafeGeneLoader.LoadGeneWithFallback(geneGUID, geneName);
            if (cachedGene != null)
            {
                if (instanceData.version < cachedGene.Version)
                {
                    cachedGene.MigrateFromVersion(instanceData.version, instanceData);
                    instanceData.version = cachedGene.Version;
                }
            }
        }

        public float GetValue(string key, float defaultValue = 0f)
        {
            return instanceData.GetValue(key, defaultValue);
        }

        public void SetValue(string key, float value)
        {
            instanceData.SetValue(key, value);
        }

        public void ModifyValue(string key, float delta)
        {
            instanceData.ModifyValue(key, delta);
        }

        #region Serialization Callbacks
        // FIX: Added serialization methods
        public void OnBeforeSerialize()
        {
            // Save the current gene reference to ensure it persists
            if (cachedGene != null)
            {
                geneGUID = cachedGene.GUID;
                geneName = cachedGene.geneName;
        
                // Update version if needed
                if (instanceData.version < cachedGene.Version)
                {
                    instanceData.version = cachedGene.Version;
                }
            }
        }

        public void OnAfterDeserialize()
        {
            // Clear cached reference to force reload
            cachedGene = null;
        }
        #endregion
    }

    [Serializable]
    public class GeneInstanceData
    {
        public int version = 1;
        public Dictionary<string, float> values = new Dictionary<string, float>();
        public int stackCount = 1;
        public float powerMultiplier = 1f;

        public float GetValue(string key, float defaultValue = 0f)
        {
            return values.TryGetValue(key, out float value) ? value : defaultValue;
        }

        public void SetValue(string key, float value)
        {
            values[key] = value;
        }

        public void ModifyValue(string key, float delta)
        {
            if (values.ContainsKey(key))
                values[key] += delta;
            else
                values[key] = delta;
        }
    }
}