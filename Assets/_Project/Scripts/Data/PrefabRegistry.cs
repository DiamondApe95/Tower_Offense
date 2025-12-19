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

        public List<IdPrefabPair> entries = new();

        public GameObject CreateOrFallback(string id)
        {
            if (!string.IsNullOrEmpty(id))
            {
                foreach (IdPrefabPair entry in entries)
                {
                    if (entry != null && entry.id == id && entry.prefab != null)
                    {
                        return Instantiate(entry.prefab);
                    }
                }
            }

            GameObject fallback = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            fallback.name = $"{id}_Fallback";
            return fallback;
        }

        public void RegisterFromScene()
        {
        }
    }
}
