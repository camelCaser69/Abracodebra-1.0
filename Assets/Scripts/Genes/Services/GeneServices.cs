// REWORKED FILE: Assets/Scripts/Genes/Services/GeneServices.cs
using System;
using System.Collections.Generic;
using UnityEngine;
using Abracodabra.Genes.Core; // FIX: Added missing using statement for GeneBase

namespace Abracodabra.Genes.Services
{
    public static class GeneServices
    {
        private static Dictionary<Type, object> services = new Dictionary<Type, object>();
        private static bool isInitialized = false;

        public static void Initialize()
        {
            if (isInitialized) return;

            // FIX: Only register services that don't depend on a scene asset.
            // The GeneLibrary will be registered by its own loader.
            Register<IGeneEventBus>(new GeneEventBus());
            Register<IDeterministicRandom>(new DeterministicRandom(DateTime.Now.Millisecond));
            // GeneEffectPool is a MonoBehaviour, so it will be registered by its loader/instance.

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

    #region Service Interfaces
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
    #endregion
}