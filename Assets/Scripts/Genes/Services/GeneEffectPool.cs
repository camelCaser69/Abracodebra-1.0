// File: Assets/Scripts/Genes/Services/GeneEffectPool.cs
using System.Collections.Generic;
using UnityEngine;

namespace Abracodabra.Genes.Services
{
    public class GeneEffectPool : MonoBehaviour, IGeneEffectPool
    {
        private static GeneEffectPool _instance;
        public static GeneEffectPool Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("GeneEffectPool");
                    _instance = go.AddComponent<GeneEffectPool>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        private Dictionary<GameObject, Queue<GameObject>> pools = new Dictionary<GameObject, Queue<GameObject>>();
        private Transform poolContainer;

        void Awake()
        {
            poolContainer = new GameObject("PooledEffects").transform;
            poolContainer.SetParent(transform);
        }

        public GameObject GetEffect(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            if (prefab == null) return null;

            if (!pools.ContainsKey(prefab))
                pools[prefab] = new Queue<GameObject>();

            GameObject effect;
            if (pools[prefab].Count > 0)
            {
                effect = pools[prefab].Dequeue();
                effect.transform.SetPositionAndRotation(position, rotation);
                effect.SetActive(true);
            }
            else
            {
                effect = Instantiate(prefab, position, rotation);
                var poolable = effect.AddComponent<PoolableEffect>();
                poolable.sourcePrefab = prefab;
                poolable.pool = this;
            }

            return effect;
        }

        public void ReturnEffect(GameObject effect, GameObject sourcePrefab)
        {
            if (effect == null || sourcePrefab == null) return;

            effect.SetActive(false);
            effect.transform.SetParent(poolContainer);

            if (!pools.ContainsKey(sourcePrefab))
                pools[sourcePrefab] = new Queue<GameObject>();

            pools[sourcePrefab].Enqueue(effect);
        }

        public void PrewarmPool(GameObject prefab, int count)
        {
            if (prefab == null || count <= 0) return;
            var list = new List<GameObject>();
            for (int i = 0; i < count; i++)
            {
                var obj = GetEffect(prefab, Vector3.zero, Quaternion.identity);
                list.Add(obj);
            }

            foreach (var obj in list)
            {
                var poolable = obj.GetComponent<PoolableEffect>();
                if (poolable != null)
                    ReturnEffect(obj, poolable.sourcePrefab);
            }
        }
    }

    /// <summary>
    /// A helper component added to pooled objects to manage their lifecycle.
    /// </summary>
    public class PoolableEffect : MonoBehaviour
    {
        public GameObject sourcePrefab;
        public GeneEffectPool pool;
        public float lifetime = 2f;

        void OnEnable()
        {
            if (lifetime > 0)
                Invoke(nameof(ReturnToPool), lifetime);
        }

        void OnDisable()
        {
            CancelInvoke();
        }

        public void ReturnToPool()
        {
            if (pool != null && sourcePrefab != null)
                pool.ReturnEffect(gameObject, sourcePrefab);
        }
    }
}