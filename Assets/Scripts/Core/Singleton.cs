using UnityEngine;

namespace KeepItTogether.Core
{
    /// <summary>
    /// Generic singleton base class for MonoBehaviours.
    /// Provides a thread-safe <see cref="Instance"/> property with DontDestroyOnLoad semantics.
    /// </summary>
    public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T _instance;
        private static readonly object _lock = new object();
        private static bool _isQuitting;

        /// <summary>
        /// The singleton instance. Returns null if the application is quitting.
        /// </summary>
        public static T Instance
        {
            get
            {
                if (_isQuitting)
                {
                    Debug.LogWarning($"[Singleton] Instance of {typeof(T)} requested after OnDestroy. Returning null.");
                    return null;
                }

                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = FindObjectOfType<T>();

                        if (_instance == null)
                        {
                            var singletonObject = new GameObject($"[{typeof(T).Name}]");
                            _instance = singletonObject.AddComponent<T>();
                            DontDestroyOnLoad(singletonObject);
                        }
                    }

                    return _instance;
                }
            }
        }

        /// <summary>
        /// True if a singleton instance currently exists.
        /// </summary>
        public static bool HasInstance => _instance != null;

        protected virtual void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this as T;
            DontDestroyOnLoad(gameObject);
        }

        protected virtual void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        protected virtual void OnApplicationQuit()
        {
            _isQuitting = true;
        }
    }
}
