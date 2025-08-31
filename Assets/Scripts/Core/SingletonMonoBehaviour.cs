using UnityEngine;

namespace WegoSystem
{
    public abstract class SingletonMonoBehaviour<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T _instance;
        private static readonly object _lock = new object();
        private static bool _applicationIsQuitting = false;

        public static T Instance
        {
            get
            {
                if (_applicationIsQuitting)
                {
                    return null;
                }

                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = FindFirstObjectByType<T>();

                        if (_instance == null)
                        {
                            Debug.LogError($"[Singleton] CRITICAL: An instance of '{typeof(T).Name}' is needed in the scene, but none was found. " +
                                           "Ensure a GameObject with this component exists and is active in your scene.");
                        }
                    }
                    return _instance;
                }
            }
        }

        public static bool HasInstance => _instance != null;
        
        // --- THIS IS THE FIX ---
        // This special attribute tells Unity to run this static method when the game loads in the editor,
        // before any scene objects have their Awake() methods called.
        // This ensures our flag is correctly reset, even with Domain Reloading disabled.
        #if UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ResetStaticData()
        {
            _applicationIsQuitting = false;
        }
        #endif


        protected virtual void Awake()
        {
            if (_instance == null)
            {
                _instance = this as T;

                // Make this a root object to prevent DontDestroyOnLoad issues with parenting
                transform.SetParent(null); 
                DontDestroyOnLoad(gameObject);
            }
            else if (_instance != this)
            {
                Debug.LogWarning($"[Singleton] Another instance of '{typeof(T).Name}' already exists. Destroying duplicate on '{gameObject.name}'.", gameObject);
                Destroy(gameObject);
                return;
            }

            OnAwake();
        }

        protected virtual void OnAwake() { }

        protected virtual void OnApplicationQuit()
        {
            _applicationIsQuitting = true;
        }
    }
}