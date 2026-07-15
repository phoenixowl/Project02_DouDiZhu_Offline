// EventBus.cs
using System;
using System.Collections.Generic;
namespace DouDiZhu.Logic.Events
{
    public static class EventBus
    {
        private static readonly Dictionary<Type, Delegate> events = new();

        public static void Subscribe<T>(Action<T> callback)
        {
            if (!events.ContainsKey(typeof(T)))
                events[typeof(T)] = null;

            events[typeof(T)] = (Action<T>)events[typeof(T)] + callback;
        }

        public static void Unsubscribe<T>(Action<T> callback)
        {
            if (!events.ContainsKey(typeof(T))) return;

            events[typeof(T)] = (Action<T>)events[typeof(T)] - callback;
        }

        public static void Emit<T>(T signal)
        {
            if (events.TryGetValue(typeof(T), out var del))
                ((Action<T>)del)?.Invoke(signal);
        }
    }
}