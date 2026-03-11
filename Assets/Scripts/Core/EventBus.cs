using System;
using System.Collections.Generic;

namespace KeepItTogether.Core
{
    /// <summary>
    /// Lightweight, type-safe, thread-safe pub/sub event bus for decoupled cross-system communication.
    /// </summary>
    /// <example>
    /// EventBus.Subscribe&lt;WaveStartedEvent&gt;(OnWaveStarted);
    /// EventBus.Publish(new WaveStartedEvent { WaveNumber = 5 });
    /// EventBus.Unsubscribe&lt;WaveStartedEvent&gt;(OnWaveStarted);
    /// </example>
    public static class EventBus
    {
        private static readonly Dictionary<Type, List<Delegate>> _subscribers = new Dictionary<Type, List<Delegate>>();
        private static readonly object _lock = new object();

        /// <summary>
        /// Subscribe a callback to events of type <typeparamref name="T"/>.
        /// </summary>
        public static void Subscribe<T>(Action<T> callback)
        {
            if (callback == null) throw new ArgumentNullException(nameof(callback));

            lock (_lock)
            {
                var type = typeof(T);
                if (!_subscribers.TryGetValue(type, out var list))
                {
                    list = new List<Delegate>();
                    _subscribers[type] = list;
                }
                list.Add(callback);
            }
        }

        /// <summary>
        /// Publish an event of type <typeparamref name="T"/> to all subscribers.
        /// </summary>
        public static void Publish<T>(T eventData)
        {
            List<Delegate> snapshot;

            lock (_lock)
            {
                var type = typeof(T);
                if (!_subscribers.TryGetValue(type, out var list) || list.Count == 0)
                    return;

                snapshot = new List<Delegate>(list);
            }

            foreach (var handler in snapshot)
            {
                if (handler is Action<T> callback)
                {
                    callback.Invoke(eventData);
                }
            }
        }

        /// <summary>
        /// Unsubscribe a specific callback from events of type <typeparamref name="T"/>.
        /// </summary>
        public static void Unsubscribe<T>(Action<T> callback)
        {
            if (callback == null) throw new ArgumentNullException(nameof(callback));

            lock (_lock)
            {
                var type = typeof(T);
                if (_subscribers.TryGetValue(type, out var list))
                {
                    list.Remove(callback);
                }
            }
        }

        /// <summary>
        /// Remove all subscriptions for event type <typeparamref name="T"/>.
        /// </summary>
        public static void ClearAll<T>()
        {
            lock (_lock)
            {
                _subscribers.Remove(typeof(T));
            }
        }

        /// <summary>
        /// Remove all subscriptions for every event type.
        /// </summary>
        public static void Clear()
        {
            lock (_lock)
            {
                _subscribers.Clear();
            }
        }
    }
}
