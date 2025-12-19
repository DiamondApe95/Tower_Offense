using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TowerOffense.Data
{
    public class PrefabRegistry
    {
        private readonly Dictionary<string, Object> prefabs = new Dictionary<string, Object>();

        public void Register(string key, Object prefab)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                Debug.LogWarning("Register called with empty key.");
                return;
            }

            prefabs[key] = prefab;
        }

        public Object Get(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                Debug.LogWarning("Get called with empty key.");
                return null;
            }

            if (prefabs.TryGetValue(key, out Object prefab))
            {
                return prefab;
            }

            return null;
        }

        public GameObject CreateOrFallback(string key, out bool usedFallback)
        {
            Object prefab = Get(key);
            if (prefab is GameObject gameObjectPrefab)
            {
                usedFallback = false;
                return Object.Instantiate(gameObjectPrefab);
            }

            usedFallback = true;
            return null;
        }
    }
}
