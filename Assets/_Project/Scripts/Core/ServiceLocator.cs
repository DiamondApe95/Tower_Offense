using System;
using System.Collections.Generic;

namespace TowerConquest.Core
{
    public static class ServiceLocator
    {
        private class Entry
        {
            public Type Type;
            public object Instance;

            public Entry(Type type, object instance)
            {
                Type = type;
                Instance = instance;
            }
        }

        private static readonly List<Entry> Entries = new List<Entry>();

        public static void Register<T>(T instance)
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }

            Type type = typeof(T);
            for (int index = 0; index < Entries.Count; index++)
            {
                if (Entries[index].Type == type)
                {
                    Entries[index].Instance = instance;
                    return;
                }
            }

            Entries.Add(new Entry(type, instance));
        }

        public static T Get<T>()
        {
            if (TryGet(out T value))
            {
                return value;
            }

            throw new InvalidOperationException($"Service of type {typeof(T).Name} is not registered.");
        }

        public static bool TryGet<T>(out T value)
        {
            Type type = typeof(T);
            for (int index = 0; index < Entries.Count; index++)
            {
                if (Entries[index].Type == type)
                {
                    if (Entries[index].Instance is T typedValue)
                    {
                        value = typedValue;
                        return true;
                    }
                }
            }

            value = default;
            return false;
        }
    }
}
