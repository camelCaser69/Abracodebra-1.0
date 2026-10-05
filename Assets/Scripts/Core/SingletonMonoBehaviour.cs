using UnityEngine;

namespace WegoSystem
{
    // Unity 6.3 rejects [RuntimeInitializeOnLoadMethod] inside a generic class, so the quit flag lives here.
    // The flag is process-wide (every singleton shares the application quit), which matches how it was used.
    internal static class SingletonQuitState
    {
        public static bool ApplicationIsQuitting;

        // Runs when the game loads in the editor, before any scene object's Awake().
        // Resets the flag even with Domain Reloading disabled.
        #if UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ResetStaticData()
        {
            ApplicationIsQuitting = false;
        }
        #endif
    }

    public abstract class SingletonMonoBehaviour<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T _instance;
        private static readonly object _lock = new object();

        public static T Instance
        {
            get
            {
                if (SingletonQuitState.ApplicationIsQuitting)
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
            SingletonQuitState.ApplicationIsQuitting = true;
        }
    }
}
