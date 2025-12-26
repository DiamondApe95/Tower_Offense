using System;
using TowerConquest.Debug;
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

        [Serializable]
        public class CategoryPrefabs
        {
            public string category;
            public List<IdPrefabPair> prefabs = new List<IdPrefabPair>();
        }

        [Header("Prefab Entries")]
        public List<IdPrefabPair> entries = new List<IdPrefabPair>();

        [Header("Categorized Prefabs")]
        public List<CategoryPrefabs> categories = new List<CategoryPrefabs>();

        [Header("Fallback Settings")]
        public bool createFallbackPrimitives = true;
        public Color unitFallbackColor = Color.blue;
        public Color towerFallbackColor = Color.green;
        public Color heroFallbackColor = Color.yellow;
        public Color projectileFallbackColor = Color.red;

        private Dictionary<string, GameObject> cachedPrefabs = new Dictionary<string, GameObject>();
        private bool cacheBuilt = false;

        private void Awake()
        {
            BuildCache();
        }

        public void BuildCache()
        {
            if (cacheBuilt) return;

            cachedPrefabs.Clear();

            // Add entries from main list
            foreach (IdPrefabPair entry in entries)
            {
                if (entry != null && !string.IsNullOrEmpty(entry.id) && entry.prefab != null)
                {
                    if (!cachedPrefabs.ContainsKey(entry.id))
                    {
                        cachedPrefabs[entry.id] = entry.prefab;
                    }
                }
            }

            // Add entries from categories
            foreach (var category in categories)
            {
                if (category?.prefabs == null) continue;
                foreach (var entry in category.prefabs)
                {
                    if (entry != null && !string.IsNullOrEmpty(entry.id) && entry.prefab != null)
                    {
                        if (!cachedPrefabs.ContainsKey(entry.id))
                        {
                            cachedPrefabs[entry.id] = entry.prefab;
                        }
                    }
                }
            }

            cacheBuilt = true;
            Log.Info($"[PrefabRegistry] Cache built with {cachedPrefabs.Count} prefabs.");
        }

        public GameObject GetPrefab(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return null;
            }

            if (!cacheBuilt)
            {
                BuildCache();
            }

            if (cachedPrefabs.TryGetValue(id, out GameObject prefab))
            {
                return prefab;
            }

            // Fallback to linear search for uncached entries
            foreach (IdPrefabPair entry in entries)
            {
                if (entry != null && entry.id == id && entry.prefab != null)
                {
                    cachedPrefabs[id] = entry.prefab;
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

            if (!createFallbackPrimitives)
            {
                Log.Warning($"[PrefabRegistry] Prefab not found for id: {id}");
                return null;
            }

            return CreateFallbackPrefab(id);
        }

        public GameObject CreateFallbackPrefab(string id)
        {
            PrimitiveType primitiveType = PrimitiveType.Capsule;
            Color color = Color.white;
            Vector3 scale = Vector3.one;

            // Determine type based on id prefix
            if (id.StartsWith("unit_"))
            {
                primitiveType = PrimitiveType.Capsule;
                color = unitFallbackColor;
                scale = new Vector3(0.5f, 0.5f, 0.5f);
            }
            else if (id.StartsWith("tower_"))
            {
                primitiveType = PrimitiveType.Cube;
                color = towerFallbackColor;
                scale = new Vector3(0.8f, 1.5f, 0.8f);
            }
            else if (id.StartsWith("hero_"))
            {
                primitiveType = PrimitiveType.Capsule;
                color = heroFallbackColor;
                scale = new Vector3(0.7f, 0.7f, 0.7f);
            }
            else if (id.StartsWith("projectile_"))
            {
                primitiveType = PrimitiveType.Sphere;
                color = projectileFallbackColor;
                scale = new Vector3(0.2f, 0.2f, 0.2f);
            }
            else if (id.StartsWith("base_"))
            {
                primitiveType = PrimitiveType.Cube;
                color = Color.magenta;
                scale = new Vector3(2f, 2f, 2f);
            }

            GameObject fallback = GameObject.CreatePrimitive(primitiveType);
            fallback.name = $"{id}_Fallback";
            fallback.transform.localScale = scale;

            // Set color
            Renderer renderer = fallback.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material mat = new Material(Shader.Find("Standard"));
                mat.color = color;
                renderer.material = mat;
            }

            Log.Info($"[PrefabRegistry] Created fallback for: {id}");
            return fallback;
        }

        public bool HasPrefab(string id)
        {
            return GetPrefab(id) != null;
        }

        public void RegisterPrefab(string id, GameObject prefab)
        {
            if (string.IsNullOrEmpty(id) || prefab == null)
            {
                return;
            }

            // Add to entries list
            var entry = new IdPrefabPair { id = id, prefab = prefab };
            entries.Add(entry);

            // Update cache
            cachedPrefabs[id] = prefab;
            Log.Info($"[PrefabRegistry] Registered prefab: {id}");
        }

        public void RegisterCategory(string categoryName, List<IdPrefabPair> prefabs)
        {
            var category = new CategoryPrefabs
            {
                category = categoryName,
                prefabs = prefabs
            };
            categories.Add(category);

            // Update cache
            foreach (var entry in prefabs)
            {
                if (entry != null && !string.IsNullOrEmpty(entry.id) && entry.prefab != null)
                {
                    cachedPrefabs[entry.id] = entry.prefab;
                }
            }

            Log.Info($"[PrefabRegistry] Registered category '{categoryName}' with {prefabs.Count} prefabs.");
        }

        public void ClearCache()
        {
            cachedPrefabs.Clear();
            cacheBuilt = false;
        }

        public List<string> GetAllRegisteredIds()
        {
            if (!cacheBuilt)
            {
                BuildCache();
            }
            return new List<string>(cachedPrefabs.Keys);
        }

        public void RegisterFromScene()
        {
            // Find all GameObjects with PrefabIdentifier component
            var identifiers = FindObjectsByType<PrefabIdentifier>(FindObjectsSortMode.None);
            foreach (var identifier in identifiers)
            {
                if (!string.IsNullOrEmpty(identifier.prefabId))
                {
                    RegisterPrefab(identifier.prefabId, identifier.gameObject);
                }
            }
            Log.Info($"[PrefabRegistry] Registered {identifiers.Length} prefabs from scene.");
        }

#if UNITY_EDITOR
        [ContextMenu("Auto-Register From Prefab Folder")]
        public void AutoRegisterFromFolder()
        {
            string prefabFolder = "Assets/Prefab";
            string[] guids = UnityEditor.AssetDatabase.FindAssets("t:Prefab", new[] { prefabFolder });

            int count = 0;
            foreach (string guid in guids)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null)
                {
                    string id = prefab.name.ToLower().Replace(" ", "_");

                    // Check if already registered
                    bool exists = false;
                    foreach (var entry in entries)
                    {
                        if (entry.id == id)
                        {
                            exists = true;
                            break;
                        }
                    }

                    if (!exists)
                    {
                        entries.Add(new IdPrefabPair { id = id, prefab = prefab });
                        count++;
                    }
                }
            }

            cacheBuilt = false;
            BuildCache();
            Log.Info($"[PrefabRegistry] Auto-registered {count} prefabs from {prefabFolder}.");
        }
#endif
    }

    /// <summary>
    /// Component to identify prefabs for auto-registration
    /// </summary>
    public class PrefabIdentifier : MonoBehaviour
    {
        public string prefabId;
    }
}
