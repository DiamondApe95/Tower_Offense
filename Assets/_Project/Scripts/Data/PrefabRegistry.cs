using System;
using System.Collections.Generic;
using UnityEngine;

namespace TowerConquest.Data
{
    public class PrefabRegistry : MonoBehaviour
    {
        [Serializable]
        public class IdPrefabPair
        {
            public string id;
            public GameObject prefab;
        }

        public List<IdPrefabPair> entries = new List<IdPrefabPair>();

        public GameObject GetPrefab(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return null;
            }

            foreach (IdPrefabPair entry in entries)
            {
                if (entry != null && entry.id == id && entry.prefab != null)
                {
                    return entry.prefab;
                }
            }

            return null;
        }

        public GameObject CreateOrFallback(string id)
        {
            GameObject prefab = GetPrefab(id);
            if (prefab != null)
            {
                return Instantiate(prefab);
            }

            GameObject fallback = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            fallback.name = $"{id}_Fallback";
            return fallback;
        }

        public bool HasPrefab(string id)
        {
            return GetPrefab(id) != null;
        }

        public void RegisterFromScene()
        {
        }
    }
}
