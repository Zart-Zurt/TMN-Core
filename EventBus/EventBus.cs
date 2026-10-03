using System;
using System.Collections.Generic;

namespace Core.Events
{
    public static class EventBus
    {
        private static readonly Dictionary<Type, List<Delegate>> _subscribers = new();
        private static readonly Stack<List<Delegate>> _bufferPool = new();
        private static readonly object _lock = new();

        public static void Subscribe<T>(Action<T> listener)
        {
            var eventType = typeof(T);

            lock (_lock)
            {
                if (!_subscribers.TryGetValue(eventType, out var listeners))
                {
                    listeners = new List<Delegate>();
                    _subscribers[eventType] = listeners;
                }

                if (!listeners.Contains(listener))
                {
                    listeners.Add(listener);
                }
            }
        }

        public static void Unsubscribe<T>(Action<T> listener)
        {
            var eventType = typeof(T);

            lock (_lock)
            {
                if (_subscribers.TryGetValue(eventType, out var listeners))
                {
                    listeners.Remove(listener);

                    if (listeners.Count == 0)
                    {
                        _subscribers.Remove(eventType);
                    }
                }
            }
        }

        public static void Raise<T>(T eventArgs)
        {
            var eventType = typeof(T);
            List<Delegate> buffer;

            lock (_lock)
            {
                if (!_subscribers.TryGetValue(eventType, out var listeners) || listeners.Count == 0)
                {
                    return;
                }

                buffer = _bufferPool.Count > 0 ? _bufferPool.Pop() : new List<Delegate>(listeners.Count);
                buffer.AddRange(listeners);
            }

            try
            {
                for (var i = 0; i < buffer.Count; i++)
                {
                    (buffer[i] as Action<T>)?.Invoke(eventArgs);
                }
            }
            finally
            {
                lock (_lock)
                {
                    buffer.Clear();
                    _bufferPool.Push(buffer);
                }
            }
        }

        public static void Clear()
        {
            lock (_lock)
            {
                _subscribers.Clear();
                _bufferPool.Clear();
            }
        }
    }
}