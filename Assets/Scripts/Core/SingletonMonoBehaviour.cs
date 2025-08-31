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
                    // If the application is quitting, don't try to find or create anything.
                    return null;
                }

                if (_instance != null)
                {
                    return _instance;
                }

                // Search for an existing instance in the scene.
                _instance = FindFirstObjectByType<T>();

                // FIX: REMOVED THE DANGEROUS AUTO-CREATION LOGIC.
                // If no instance is found, it's a setup error. We will now log a critical error
                // instead of creating a blank, broken instance silently.
                if (_instance == null)
                {
                    Debug.LogError($"[Singleton] CRITICAL: An instance of '{typeof(T).Name}' is needed in the scene, but none was found. " +
                                   "Ensure a GameObject with this component exists and is active in your scene.");
                }
                return _instance;
            }
        }

        public static bool HasInstance => _instance != null;

        protected virtual void Awake()
        {
            _applicationIsQuitting = false;
            
            if (_instance == null)
            {
                // If this is the first instance, make it the singleton.
                _instance = this as T;
                
                // Optional: Make it persist across scenes. If you don't want this, you can remove these two lines.
                transform.SetParent(null);
                DontDestroyOnLoad(gameObject);
            }
            else if (_instance != this)
            {
                // If an instance already exists, this one is a duplicate, so destroy it.
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
            _instance = null;
        }
    }
}