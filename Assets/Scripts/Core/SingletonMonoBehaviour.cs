// Assets/Scripts/Core/SingletonMonoBehaviour.cs
using UnityEngine;

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
                // If the application is quitting, don't create a new instance.
                // This prevents the "zombie singleton" issue.
                Debug.LogWarning($"[Singleton] Instance '{typeof(T).Name}' already destroyed on application quit. Won't create again - returning null.");
                return null;
            }

            lock (_lock)
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<T>();

                    if (_instance == null && Application.isPlaying)
                    {
                        Debug.LogWarning($"[Singleton] Instance of '{typeof(T).Name}' not found. A new one will be created.");
                        GameObject singletonObject = new GameObject();
                        _instance = singletonObject.AddComponent<T>();
                        singletonObject.name = $"[Singleton] {typeof(T).Name}";
                    }
                }
                return _instance;
            }
        }
    }

    protected virtual void Awake()
    {
        if (_instance == null)
        {
            _instance = this as T;

            // Un-parent the singleton to make it a root object.
            // This is required for DontDestroyOnLoad to work correctly.
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

    /// <summary>
    /// When the application quits, we set a flag to prevent the singleton
    /// from being re-created if it's accessed from an OnDestroy method.
    /// </summary>
    protected virtual void OnApplicationQuit()
    {
        _applicationIsQuitting = true;
    }
}