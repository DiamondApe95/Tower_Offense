using System.Collections.Generic;
using UnityEngine;
using TowerConquest.Data;
using TowerConquest.Core;

namespace TowerConquest.Gameplay
{
    /// <summary>
    /// Manages all construction sites and builder assignment
    /// </summary>
    public class ConstructionManager : MonoBehaviour
    {
        [Header("Prefabs")]
        [SerializeField] private GameObject constructionSitePrefab;
        [SerializeField] private GameObject builderPrefab;

        [Header("Settings")]
        [SerializeField] private Transform constructionParent;
        [SerializeField] private Transform builderParent;

        [Header("Runtime")]
        [SerializeField] private List<ConstructionSite> activeSites = new List<ConstructionSite>();
        [SerializeField] private List<BuilderController> availableBuilders = new List<BuilderController>();
        [SerializeField] private List<BuilderController> assignedBuilders = new List<BuilderController>();

        private JsonDatabase database;
        private PrefabRegistry prefabRegistry;

        private void Awake()
        {
            if (constructionParent == null)
            {
                var go = new GameObject("ConstructionSites");
                constructionParent = go.transform;
                constructionParent.SetParent(transform);
            }

            if (builderParent == null)
            {
                var go = new GameObject("Builders");
                builderParent = go.transform;
                builderParent.SetParent(transform);
            }
        }

        public void Initialize(JsonDatabase db, PrefabRegistry registry)
        {
            database = db;
            prefabRegistry = registry;
        }

        /// <summary>
        /// Place a tower construction site
        /// </summary>
        public ConstructionSite PlaceTower(string towerId, Vector3 position, GoldManager.Team team)
        {
            var towerDef = database.GetTower(towerId);
            if (towerDef == null)
            {
                Debug.LogError($"[ConstructionManager] Tower definition not found: {towerId}");
                return null;
            }

            // Create construction site
            GameObject sitePrefab = constructionSitePrefab;
            // TODO: Load tower-specific construction site prefab if available
            // if (!string.IsNullOrEmpty(towerDef.constructionSitePrefabPath))
            //     sitePrefab = prefabRegistry.LoadPrefab(towerDef.constructionSitePrefabPath);

            if (sitePrefab == null)
            {
                Debug.LogError("[ConstructionManager] Construction site prefab not assigned");
                return null;
            }

            GameObject siteObj = Instantiate(sitePrefab, position, Quaternion.identity, constructionParent);
            siteObj.name = $"ConstructionSite_{towerId}_{team}";

            var site = siteObj.GetComponent<ConstructionSite>();
            if (site == null)
            {
                site = siteObj.AddComponent<ConstructionSite>();
            }

            // Initialize with tower data
            float constructionHP = towerDef.baseStats != null ? towerDef.baseStats.constructionHP : 100f;
            if (constructionHP <= 0) constructionHP = 100f; // Default

            site.Initialize(towerId, towerDef.requiredBuilders, constructionHP, team);

            // Listen to events
            site.OnConstructionComplete += OnConstructionComplete;
            site.OnConstructionDestroyed += OnConstructionDestroyed;

            activeSites.Add(site);

            Debug.Log($"[ConstructionManager] Placed construction site for {towerId} at {position}");

            // Assign builders automatically
            AssignBuilders(team);

            return site;
        }

        /// <summary>
        /// Spawn a builder unit
        /// </summary>
        public BuilderController SpawnBuilder(Vector3 position, GoldManager.Team team)
        {
            if (builderPrefab == null)
            {
                Debug.LogError("[ConstructionManager] Builder prefab not assigned");
                return null;
            }

            GameObject builderObj = Instantiate(builderPrefab, position, Quaternion.identity, builderParent);
            builderObj.name = $"Builder_{team}_{availableBuilders.Count}";

            var builder = builderObj.GetComponent<BuilderController>();
            if (builder == null)
            {
                builder = builderObj.AddComponent<BuilderController>();
            }

            builder.Initialize(team);
            availableBuilders.Add(builder);

            Debug.Log($"[ConstructionManager] Spawned builder for team {team}");

            // Try to assign to a site immediately
            AssignBuilders(team);

            return builder;
        }

        /// <summary>
        /// Assign available builders to construction sites
        /// </summary>
        private void AssignBuilders(GoldManager.Team team)
        {
            // Get all unassigned builders for this team
            var unassignedBuilders = availableBuilders.FindAll(b => !b.IsAssigned && b.OwnerTeam == team);

            // Get all incomplete sites for this team
            var incompleteSites = activeSites.FindAll(s => !s.IsComplete && s.OwnerTeam == team);

            foreach (var site in incompleteSites)
            {
                int buildersNeeded = site.RequiredBuilders - site.CurrentBuilders;

                for (int i = 0; i < buildersNeeded && unassignedBuilders.Count > 0; i++)
                {
                    var builder = unassignedBuilders[0];
                    unassignedBuilders.RemoveAt(0);

                    builder.AssignToSite(site);
                    assignedBuilders.Add(builder);
                    availableBuilders.Remove(builder);

                    Debug.Log($"[ConstructionManager] Assigned builder to {site.TowerID}");
                }

                if (unassignedBuilders.Count == 0)
                    break;
            }
        }

        private void OnConstructionComplete(ConstructionSite site)
        {
            Debug.Log($"[ConstructionManager] Construction complete: {site.TowerID}");

            // Spawn the actual tower
            SpawnCompletedTower(site);

            // Remove from active sites
            activeSites.Remove(site);

            // Unsubscribe
            site.OnConstructionComplete -= OnConstructionComplete;
            site.OnConstructionDestroyed -= OnConstructionDestroyed;

            // Destroy the construction site
            Destroy(site.gameObject);
        }

        private void OnConstructionDestroyed(ConstructionSite site)
        {
            Debug.Log($"[ConstructionManager] Construction destroyed: {site.TowerID}");

            // Remove from active sites
            activeSites.Remove(site);

            // Unsubscribe
            site.OnConstructionComplete -= OnConstructionComplete;
            site.OnConstructionDestroyed -= OnConstructionDestroyed;

            // TODO: Partial gold refund?
        }

        private void SpawnCompletedTower(ConstructionSite site)
        {
            var towerDef = database.GetTower(site.TowerID);
            if (towerDef == null)
            {
                Debug.LogError($"[ConstructionManager] Tower definition not found: {site.TowerID}");
                return;
            }

            // TODO: Load tower prefab and spawn
            // For now, just log
            Debug.Log($"[ConstructionManager] Would spawn tower: {site.TowerID} at {site.transform.position}");

            // GameObject towerPrefab = prefabRegistry.LoadPrefab(towerDef.prefabPath);
            // if (towerPrefab != null)
            // {
            //     GameObject tower = Instantiate(towerPrefab, site.transform.position, site.transform.rotation);
            //     // Initialize tower
            // }
        }

        /// <summary>
        /// Get all active construction sites
        /// </summary>
        public List<ConstructionSite> GetActiveSites()
        {
            return new List<ConstructionSite>(activeSites);
        }

        /// <summary>
        /// Get construction sites for a specific team
        /// </summary>
        public List<ConstructionSite> GetSitesForTeam(GoldManager.Team team)
        {
            return activeSites.FindAll(s => s.OwnerTeam == team);
        }

        private void OnDestroy()
        {
            // Cleanup subscriptions
            foreach (var site in activeSites)
            {
                if (site != null)
                {
                    site.OnConstructionComplete -= OnConstructionComplete;
                    site.OnConstructionDestroyed -= OnConstructionDestroyed;
                }
            }
        }
    }
}
