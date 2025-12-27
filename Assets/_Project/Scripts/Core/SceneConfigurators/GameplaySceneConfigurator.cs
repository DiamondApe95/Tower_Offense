using System.Collections.Generic;
using UnityEngine;
using TowerConquest.Debug;
using TowerConquest.Data;

namespace TowerConquest.Core
{
    /// <summary>
    /// Automatically configures the Gameplay scene on load.
    /// Registers all prefabs and ensures proper setup.
    /// </summary>
    public class GameplaySceneConfigurator : MonoBehaviour
    {
        [Header("Auto-Register Prefabs")]
        [Tooltip("Automatically register all prefabs from the Prefab folder")]
        public bool autoRegisterPrefabs = true;

        [Header("Prefab Folder")]
        public string prefabFolder = "Assets/_Project/Prefab";

        [Header("Manual Prefab Assignments")]
        [Tooltip("Towers to register")]
        public List<PrefabEntry> towerPrefabs = new List<PrefabEntry>();

        [Tooltip("Units to register")]
        public List<PrefabEntry> unitPrefabs = new List<PrefabEntry>();

        [Tooltip("Heroes to register")]
        public List<PrefabEntry> heroPrefabs = new List<PrefabEntry>();

        [System.Serializable]
        public class PrefabEntry
        {
            public string id;
            public GameObject prefab;
        }

        private PrefabRegistry prefabRegistry;

        private void Awake()
        {
            ConfigureScene();
        }

        [ContextMenu("Configure Scene")]
        public void ConfigureScene()
        {
            Log.Info("GameplaySceneConfigurator: Configuring Gameplay scene...");

            EnsurePrefabRegistry();
            RegisterAllPrefabs();

            Log.Info("GameplaySceneConfigurator: Configuration complete.");
        }

        private void EnsurePrefabRegistry()
        {
            prefabRegistry = FindFirstObjectByType<PrefabRegistry>();

            if (prefabRegistry == null)
            {
                var go = new GameObject("PrefabRegistry");
                prefabRegistry = go.AddComponent<PrefabRegistry>();
                Log.Info("GameplaySceneConfigurator: Created PrefabRegistry.");
            }

            // Register with ServiceLocator
            ServiceLocator.Register(prefabRegistry);
        }

        private void RegisterAllPrefabs()
        {
#if UNITY_EDITOR
            if (autoRegisterPrefabs)
            {
                AutoRegisterFromFolder();
            }
#endif

            // Register manual entries
            foreach (var entry in towerPrefabs)
            {
                if (!string.IsNullOrEmpty(entry.id) && entry.prefab != null)
                {
                    prefabRegistry.RegisterPrefab(entry.id, entry.prefab);
                }
            }

            foreach (var entry in unitPrefabs)
            {
                if (!string.IsNullOrEmpty(entry.id) && entry.prefab != null)
                {
                    prefabRegistry.RegisterPrefab(entry.id, entry.prefab);
                }
            }

            foreach (var entry in heroPrefabs)
            {
                if (!string.IsNullOrEmpty(entry.id) && entry.prefab != null)
                {
                    prefabRegistry.RegisterPrefab(entry.id, entry.prefab);
                }
            }
        }

#if UNITY_EDITOR
        private void AutoRegisterFromFolder()
        {
            if (string.IsNullOrEmpty(prefabFolder)) return;

            string[] guids = UnityEditor.AssetDatabase.FindAssets("t:Prefab", new[] { prefabFolder });

            int count = 0;
            foreach (string guid in guids)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);

                if (prefab != null)
                {
                    // Determine ID based on prefab name
                    string prefabName = prefab.name;
                    string id = DeterminePrefabId(prefabName);

                    if (!prefabRegistry.HasPrefab(id))
                    {
                        prefabRegistry.RegisterPrefab(id, prefab);
                        count++;
                    }
                }
            }

            Log.Info($"GameplaySceneConfigurator: Auto-registered {count} prefabs from {prefabFolder}.");
        }

        private string DeterminePrefabId(string prefabName)
        {
            // Convert prefab names to IDs
            // Tower_Archery -> tower_archery
            // Unit_Archer -> unit_archer
            // Hero_Centurion -> hero_centurion

            string id = prefabName.ToLower();

            // Handle special cases
            if (prefabName.StartsWith("Tower_"))
            {
                id = "tower_" + prefabName.Substring(6).ToLower();
            }
            else if (prefabName.StartsWith("Unit_"))
            {
                id = "unit_" + prefabName.Substring(5).ToLower();
            }
            else if (prefabName.StartsWith("Hero_"))
            {
                id = "hero_" + prefabName.Substring(5).ToLower();
            }
            else if (prefabName.StartsWith("Prefab_"))
            {
                // Handle base prefabs: Prefab_Roman_Base -> base_roman
                string remainder = prefabName.Substring(7).ToLower();
                if (remainder.Contains("base"))
                {
                    id = "base_" + remainder.Replace("_base", "");
                }
                else
                {
                    id = remainder;
                }
            }
            else if (prefabName.StartsWith("Projectile_"))
            {
                id = "projectile_" + prefabName.Substring(11).ToLower();
            }

            return id;
        }
#endif
    }
}
