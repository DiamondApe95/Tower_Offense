using System;
using System.Collections.Generic;

namespace TowerOffense.Core
{
    public static class ServiceLocator
    {
        private class Entry
        {
            public Type Type { get; }
            public object Instance { get; }

            public Entry(Type type, object instance)
            {
                Type = type;
                Instance = instance;
            }
        }

        private static readonly List<Entry> entries = new List<Entry>();

        public static void Register<T>(T instance)
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }

            Type type = typeof(T);
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].Type == type)
                {
                    entries.RemoveAt(i);
                    break;
                }
            }

            entries.Add(new Entry(type, instance));
        }

        public static T Get<T>()
        {
            if (TryGet(out T value))
            {
                return value;
            }

            throw new InvalidOperationException($"Service of type {typeof(T).Name} has not been registered.");
        }

        public static bool TryGet<T>(out T value)
        {
            Type type = typeof(T);
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].Type == type)
                {
                    value = (T)entries[i].Instance;
                    return true;
                }
            }

            value = default;
            return false;
        }
    }
}
