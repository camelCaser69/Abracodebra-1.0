// Assets/Scripts/Core/SingletonMonoBehaviour.cs
using UnityEngine;

/// <summary>
/// An abstract base class for creating a thread-safe, lazy-initialized singleton MonoBehaviour.
/// If an instance is not found in the scene, a new one will be created automatically.
/// </summary>
/// <typeparam name="T">The type of the singleton.</typeparam>
public abstract class SingletonMonoBehaviour<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T _instance;
    private static readonly object _lock = new object();

    public static T Instance
    {
        get
        {
            // Lock to ensure thread safety, especially in multi-threaded scenarios.
            lock (_lock)
            {
                // If the instance is null, try to find it in the scene.
                if (_instance == null)
                {
                    // Use the modern, non-obsolete FindFirstObjectByType.
                    _instance = FindFirstObjectByType<T>();

                    // If it's still null and we're in play mode, create a new GameObject for it.
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

    /// <summary>
    /// This is the standard Unity Awake method that handles the singleton pattern.
    /// It ensures that only one instance of the singleton exists.
    /// If another instance already exists, this one is destroyed.
    /// </summary>
    protected virtual void Awake()
    {
        if (_instance == null)
        {
            // If no instance exists yet, this one becomes the singleton instance.
            _instance = this as T;
            DontDestroyOnLoad(gameObject); // Make it persistent across scenes
        }
        else if (_instance != this)
        {
            // If an instance already exists and it's not this one, destroy this one.
            Debug.LogWarning($"[Singleton] Another instance of '{typeof(T).Name}' already exists. Destroying duplicate on '{gameObject.name}'.", gameObject);
            Destroy(gameObject);
            return;
        }

        // Call the virtual OnAwake method for derived classes to implement their own Awake logic.
        OnAwake();
    }

    /// <summary>
    /// This virtual method is called from Awake() after the singleton instance has been confirmed.
    /// Derived classes should override this method instead of Awake() for their initialization logic.
    /// </summary>
    protected virtual void OnAwake() { }
}