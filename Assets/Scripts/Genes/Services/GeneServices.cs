// REWORKED FILE: Assets/Scripts/Genes/Services/GeneServices.cs
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Abracodabra.Genes.Core;

namespace Abracodabra.Genes.Services
{
    public static class GeneServices
    {
        private static Dictionary<Type, object> services = new Dictionary<Type, object>();
        private static bool isInitialized = false;

        public static void Initialize()
        {
            if (isInitialized) return;

            Register<IGeneEventBus>(new GeneEventBus());
            Register<IDeterministicRandom>(new DeterministicRandom(DateTime.Now.Millisecond));

            isInitialized = true;
            Debug.Log("Core Gene Services initialized (EventBus, Random).");
        }

        public static void Register<T>(T service) where T : class
        {
            services[typeof(T)] = service;
        }

        public static T Get<T>() where T : class
        {
            if (!isInitialized)
            {
                Debug.LogError("GeneServices not initialized! Call Initialize() first.");
                return null;
            }

            // Fallback for GeneLibrary if it wasn't registered in time
            if (typeof(T) == typeof(IGeneLibrary) && !services.ContainsKey(typeof(T)))
            {
                Debug.LogWarning("IGeneLibrary service was requested before it was registered. Attempting to find and register it now.");
                var library = GeneLibrary.Instance ?? Resources.FindObjectsOfTypeAll<GeneLibrary>().FirstOrDefault();
                if (library != null)
                {
                    if (GeneLibrary.Instance == null)
                    {
                        library.SetActiveInstance(); // Ensures the lookups are built
                    }
                    Register<IGeneLibrary>(library);
                    Debug.Log("Successfully found and registered IGeneLibrary service on-demand.");
                }
                else
                {
                    Debug.LogError("CRITICAL: Could not find any GeneLibrary asset to register as a fallback service!");
                }
            }

            if (services.TryGetValue(typeof(T), out object service))
                return (T)service;

            Debug.LogError($"Service {typeof(T)} not registered!");
            return null;
        }

        public static void Reset()
        {
            services.Clear();
            isInitialized = false;
        }
    }

    public interface IGeneLibrary
    {
        GeneBase GetGeneByGUID(string guid);
        GeneBase GetGeneByName(string name);
        GeneBase GetPlaceholderGene();
    }

    public interface IGeneEventBus
    {
        void Subscribe<T>(Action<T> handler) where T : class;
        void Unsubscribe<T>(Action<T> handler) where T : class;
        void Publish<T>(T message) where T : class;
    }

    public interface IGeneEffectPool
    {
        GameObject GetEffect(GameObject prefab, Vector3 position, Quaternion rotation);
        void ReturnEffect(GameObject effect, GameObject sourcePrefab);
    }

    public interface IDeterministicRandom
    {
        float Range(float min, float max);
        int Range(int min, int max);
        void SetSeed(int seed);
    }
}