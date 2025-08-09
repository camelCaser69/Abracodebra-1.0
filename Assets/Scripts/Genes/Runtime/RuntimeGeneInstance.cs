// File: Assets/Scripts/Genes/Runtime/RuntimeGeneInstance.cs
using System;
using System.Collections.Generic;
using UnityEngine;
using Abracodabra.Genes.Core;

namespace Abracodabra.Genes.Runtime
{
    /// <summary>
    /// Represents a single, unique instance of a GeneBase asset. This class holds any
    /// runtime-specific data or modifications for that gene instance.
    /// </summary>
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
            if (cachedGene != null && instanceData != null)
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

        #region Serialization
        public void OnBeforeSerialize()
        {
            if (cachedGene != null)
            {
                geneGUID = cachedGene.GUID;
                geneName = cachedGene.geneName;

                if (instanceData != null && instanceData.version < cachedGene.Version)
                {
                    instanceData.version = cachedGene.Version;
                }
            }
        }

        public void OnAfterDeserialize()
        {
            // Clear the cache, forcing a reload via SafeGeneLoader next time GetGene() is called.
            // This ensures we always have the correct, up-to-date gene asset.
            cachedGene = null;
        }
        #endregion
    }

    /// <summary>
    /// Holds the serializable data for a RuntimeGeneInstance.
    /// Implements ISerializationCallbackReceiver to handle the serialization of a dictionary,
    /// which Unity cannot do by default. It converts the dictionary to two lists before serialization
    /// and back to a dictionary after deserialization.
    /// </summary>
    [Serializable]
    public class GeneInstanceData : ISerializationCallbackReceiver
    {
        public int version = 1;

        // Serialized fields
        [SerializeField] private List<string> _keys = new List<string>();
        [SerializeField] private List<float> _values = new List<float>();

        // Runtime dictionary (fast lookups)
        [NonSerialized] private Dictionary<string, float> _runtimeValues = new Dictionary<string, float>();
        
        public int stackCount = 1;
        public float powerMultiplier = 1f;

        public float GetValue(string key, float defaultValue = 0f)
        {
            return _runtimeValues.TryGetValue(key, out float value) ? value : defaultValue;
        }

        public void SetValue(string key, float value)
        {
            _runtimeValues[key] = value;
        }

        public void ModifyValue(string key, float delta)
        {
            if (_runtimeValues.TryGetValue(key, out float currentValue))
            {
                _runtimeValues[key] = currentValue + delta;
            }
            else
            {
                _runtimeValues[key] = delta;
            }
        }

        public void OnBeforeSerialize()
        {
            _keys.Clear();
            _values.Clear();

            if (_runtimeValues == null)
            {
                return;
            }

            foreach (var kvp in _runtimeValues)
            {
                _keys.Add(kvp.Key);
                _values.Add(kvp.Value);
            }
        }

        public void OnAfterDeserialize()
        {
            _runtimeValues = new Dictionary<string, float>();

            if (_keys != null && _values != null && _keys.Count == _values.Count)
            {
                for (int i = 0; i < _keys.Count; i++)
                {
                    // Guard against duplicate or null keys which could happen during serialization
                    if (!string.IsNullOrEmpty(_keys[i]) && !_runtimeValues.ContainsKey(_keys[i]))
                    {
                        _runtimeValues.Add(_keys[i], _values[i]);
                    }
                }
            }
        }
    }
}