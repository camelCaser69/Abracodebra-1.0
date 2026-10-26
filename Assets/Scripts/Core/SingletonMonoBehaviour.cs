using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

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

    /// <summary>
    /// B3: a restart reloads the scene, but the singletons below are DontDestroyOnLoad. Left alone they
    /// outlive the reload and destroy the reloaded scene's fresh copies as duplicates (and so do plain
    /// static-Instance components on the same GameObjects, e.g. GridDebugVisualizer), so the old run's
    /// round, tick and state carry on. TearDownForRestart destroys them at once (the reloaded scene
    /// awakes before a deferred Destroy would run); the reload then starts like a fresh Play.
    /// Until the new scene is loaded a missing singleton is expected, not CRITICAL.
    /// </summary>
    public static class SingletonRuntime
    {
        static readonly List<Action> releasers = new List<Action>();

        public static bool IsRestarting { get; private set; }

        internal static void Register(Action release)
        {
            releasers.Add(release);
        }

        /// <summary>Destroys every persistent singleton now. Call right before SceneManager.LoadScene.</summary>
        public static void TearDownForRestart()
        {
            IsRestarting = true;
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            SceneManager.sceneLoaded += HandleSceneLoaded;

            var pending = new List<Action>(releasers);
            releasers.Clear();
            foreach (var release in pending)
            {
                release();
            }
        }

        static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            IsRestarting = false;
        }

        #if UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ResetStaticData()
        {
            releasers.Clear();
            IsRestarting = false;
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

                        // B3: between a restart's teardown and the reloaded scene nothing is there yet.
                        if (_instance == null && !SingletonRuntime.IsRestarting)
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
                SingletonRuntime.Register(ReleaseForRestart);
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

        // B3: called by SingletonRuntime.TearDownForRestart. Immediate, see SingletonRuntime.
        void ReleaseForRestart()
        {
            if (_instance == this)
            {
                _instance = null;
            }
            if (this != null)
            {
                DestroyImmediate(gameObject);
            }
        }
    }
}
