using System;
using TowerConquest.Debug;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TowerConquest.Data;
using TowerConquest.Core;
using TowerConquest.Gameplay.Entities;

namespace TowerConquest.Gameplay
{
    /// <summary>
    /// Manages all construction sites and builder assignment
    /// Automatically spawns builders when construction sites are placed
    /// </summary>
    public class ConstructionManager : MonoBehaviour
    {
        [Header("Prefabs")]
        [SerializeField] private GameObject constructionSitePrefab;
        [SerializeField] private GameObject builderPrefab;

        [Header("Settings")]
        [SerializeField] private Transform constructionParent;
        [SerializeField] private Transform builderParent;

        [Header("Builder Spawning")]
        [Tooltip("Time between spawning builders for a tower (3 builders)")]
        [SerializeField] private float builderSpawnInterval = 2f;
        [Tooltip("Number of builders required for towers")]
        [SerializeField] private int towerBuilderCount = 3;
        [Tooltip("Number of builders required for traps")]
        #pragma warning disable 0414 // Field assigned but never used (reserved for future configuration)
        [SerializeField] private int trapBuilderCount = 1;
        #pragma warning restore 0414

        [Header("Spawn Points")]
        [SerializeField] private Transform playerBuilderSpawnPoint;
        [SerializeField] private Transform aiBuilderSpawnPoint;

        [Header("Runtime")]
        [SerializeField] private List<ConstructionSite> activeSites = new List<ConstructionSite>();
        [SerializeField] private List<TrapConstructionSite> activeTrapSites = new List<TrapConstructionSite>();
        [SerializeField] private List<BuilderController> availableBuilders = new List<BuilderController>();
        [SerializeField] private List<BuilderController> assignedBuilders = new List<BuilderController>();

        // Events
        public event Action<ConstructionSite> OnTowerConstructionStarted;
        public event Action<ConstructionSite> OnTowerConstructionComplete;
        public event Action<TrapConstructionSite> OnTrapConstructionComplete;

        private JsonDatabase database;
        private PrefabRegistry prefabRegistry;
        private BaseController playerBase;
        private BaseController enemyBase;

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

            // Find bases for builder spawn points
            FindBases();
        }

        private void FindBases()
        {
            var bases = FindObjectsByType<BaseController>(FindObjectsSortMode.None);
            foreach (var baseCtrl in bases)
            {
                if (baseCtrl.CompareTag("PlayerBase") || baseCtrl.gameObject.name.Contains("Player"))
                {
                    playerBase = baseCtrl;
                    if (playerBuilderSpawnPoint == null)
                    {
                        playerBuilderSpawnPoint = baseCtrl.transform;
                    }
                }
                else if (baseCtrl.CompareTag("EnemyBase") || baseCtrl.gameObject.name.Contains("Enemy"))
                {
                    enemyBase = baseCtrl;
                    if (aiBuilderSpawnPoint == null)
                    {
                        aiBuilderSpawnPoint = baseCtrl.transform;
                    }
                }
            }
        }

        /// <summary>
        /// Get the spawn point for builders of a specific team
        /// </summary>
        private Vector3 GetBuilderSpawnPoint(GoldManager.Team team)
        {
            if (team == GoldManager.Team.Player)
            {
                return playerBuilderSpawnPoint != null ? playerBuilderSpawnPoint.position : Vector3.zero;
            }
            else
            {
                return aiBuilderSpawnPoint != null ? aiBuilderSpawnPoint.position : Vector3.zero;
            }
        }

        /// <summary>
        /// Place a tower construction site and automatically spawn builders
        /// </summary>
        public ConstructionSite PlaceTower(string towerId, Vector3 position, GoldManager.Team team)
        {
            var towerDef = database?.GetTower(towerId);
            if (towerDef == null)
            {
                Log.Error($"[ConstructionManager] Tower definition not found: {towerId}");
                return null;
            }

            // Create construction site
            GameObject siteObj;
            if (constructionSitePrefab != null)
            {
                siteObj = Instantiate(constructionSitePrefab, position, Quaternion.identity, constructionParent);
            }
            else
            {
                siteObj = CreateDefaultConstructionSite(position);
            }

            siteObj.name = $"ConstructionSite_{towerId}_{team}";

            var site = siteObj.GetComponent<ConstructionSite>();
            if (site == null)
            {
                site = siteObj.AddComponent<ConstructionSite>();
            }

            // Initialize with tower data
            float constructionHP = towerDef.baseStats != null ? towerDef.baseStats.constructionHP : 100f;
            if (constructionHP <= 0) constructionHP = 100f;

            int requiredBuilders = towerDef.requiredBuilders > 0 ? towerDef.requiredBuilders : towerBuilderCount;
            site.Initialize(towerId, requiredBuilders, constructionHP, team);

            // Listen to events
            site.OnConstructionComplete += OnConstructionComplete;
            site.OnConstructionDestroyed += OnConstructionDestroyed;

            activeSites.Add(site);

            Log.Info($"[ConstructionManager] Placed construction site for {towerId} at {position}");

            // Automatically spawn builders for this site
            StartCoroutine(SpawnBuildersForSite(site, requiredBuilders, team));

            OnTowerConstructionStarted?.Invoke(site);

            return site;
        }

        /// <summary>
        /// Create a default construction site object
        /// </summary>
        private GameObject CreateDefaultConstructionSite(Vector3 position)
        {
            GameObject siteObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            siteObj.transform.position = position;
            siteObj.transform.localScale = new Vector3(2f, 1f, 2f);
            siteObj.transform.SetParent(constructionParent);

            var renderer = siteObj.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material mat = new Material(Shader.Find("Standard"));
                mat.color = new Color(0.5f, 0.35f, 0.2f, 0.9f);
                renderer.material = mat;
            }

            return siteObj;
        }

        /// <summary>
        /// Automatically spawn builders for a construction site with delay
        /// </summary>
        private IEnumerator SpawnBuildersForSite(ConstructionSite site, int count, GoldManager.Team team)
        {
            Vector3 spawnPos = GetBuilderSpawnPoint(team);

            for (int i = 0; i < count; i++)
            {
                if (site == null || site.IsComplete)
                {
                    yield break;
                }

                var builder = SpawnBuilderForSite(spawnPos, team, site);
                if (builder != null)
                {
                    Log.Info($"[ConstructionManager] Spawned builder {i + 1}/{count} for {site.TowerID}");
                }

                // Wait before spawning next builder (except for last one)
                if (i < count - 1)
                {
                    yield return new WaitForSeconds(builderSpawnInterval);
                }
            }
        }

        /// <summary>
        /// Spawn a builder and assign it directly to a site
        /// </summary>
        private BuilderController SpawnBuilderForSite(Vector3 position, GoldManager.Team team, ConstructionSite site)
        {
            GameObject builderObj;
            if (builderPrefab != null)
            {
                builderObj = Instantiate(builderPrefab, position, Quaternion.identity, builderParent);
            }
            else
            {
                builderObj = CreateDefaultBuilder(position);
            }

            builderObj.name = $"Builder_{team}_{assignedBuilders.Count}";

            var builder = builderObj.GetComponent<BuilderController>();
            if (builder == null)
            {
                builder = builderObj.AddComponent<BuilderController>();
            }

            builder.Initialize(team);
            builder.AssignToSite(site);
            assignedBuilders.Add(builder);

            return builder;
        }

        /// <summary>
        /// Create a default builder object
        /// </summary>
        private GameObject CreateDefaultBuilder(Vector3 position)
        {
            GameObject builderObj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            builderObj.transform.position = position;
            builderObj.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
            builderObj.transform.SetParent(builderParent);

            var renderer = builderObj.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material mat = new Material(Shader.Find("Standard"));
                mat.color = new Color(0.8f, 0.6f, 0.2f, 1f);
                renderer.material = mat;
            }

            // Add NavMeshAgent for pathfinding
            var agent = builderObj.AddComponent<UnityEngine.AI.NavMeshAgent>();
            agent.speed = 5f;
            agent.stoppingDistance = 1.5f;

            return builderObj;
        }

        /// <summary>
        /// Request a builder for a trap construction site
        /// </summary>
        public void RequestBuilderForTrap(TrapConstructionSite trapSite)
        {
            if (trapSite == null) return;

            activeTrapSites.Add(trapSite);
            trapSite.OnConstructionComplete += OnTrapComplete;
            trapSite.OnConstructionDestroyed += OnTrapDestroyed;

            // Spawn a single builder for the trap
            Vector3 spawnPos = GetBuilderSpawnPoint(trapSite.OwnerTeam);
            var builder = SpawnBuilderForTrap(spawnPos, trapSite.OwnerTeam, trapSite);

            Log.Info($"[ConstructionManager] Spawned builder for trap {trapSite.TrapID}");
        }

        private BuilderController SpawnBuilderForTrap(Vector3 position, GoldManager.Team team, TrapConstructionSite trapSite)
        {
            GameObject builderObj;
            if (builderPrefab != null)
            {
                builderObj = Instantiate(builderPrefab, position, Quaternion.identity, builderParent);
            }
            else
            {
                builderObj = CreateDefaultBuilder(position);
            }

            builderObj.name = $"TrapBuilder_{team}_{assignedBuilders.Count}";

            var builder = builderObj.GetComponent<BuilderController>();
            if (builder == null)
            {
                builder = builderObj.AddComponent<BuilderController>();
            }

            builder.Initialize(team);
            builder.AssignToTrapSite(trapSite);
            assignedBuilders.Add(builder);

            return builder;
        }

        private void OnTrapComplete(TrapConstructionSite site)
        {
            activeTrapSites.Remove(site);
            site.OnConstructionComplete -= OnTrapComplete;
            site.OnConstructionDestroyed -= OnTrapDestroyed;

            OnTrapConstructionComplete?.Invoke(site);
        }

        private void OnTrapDestroyed(TrapConstructionSite site)
        {
            activeTrapSites.Remove(site);
            site.OnConstructionComplete -= OnTrapComplete;
            site.OnConstructionDestroyed -= OnTrapDestroyed;
        }

        /// <summary>
        /// Spawn a builder unit
        /// </summary>
        public BuilderController SpawnBuilder(Vector3 position, GoldManager.Team team)
        {
            if (builderPrefab == null)
            {
                Log.Error("[ConstructionManager] Builder prefab not assigned");
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

            Log.Info($"[ConstructionManager] Spawned builder for team {team}");

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

                    Log.Info($"[ConstructionManager] Assigned builder to {site.TowerID}");
                }

                if (unassignedBuilders.Count == 0)
                    break;
            }
        }

        private void OnConstructionComplete(ConstructionSite site)
        {
            Log.Info($"[ConstructionManager] Construction complete: {site.TowerID}");

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
            Log.Info($"[ConstructionManager] Construction destroyed: {site.TowerID}");

            // Remove from active sites
            activeSites.Remove(site);

            // Unsubscribe
            site.OnConstructionComplete -= OnConstructionComplete;
            site.OnConstructionDestroyed -= OnConstructionDestroyed;

            // Partial gold refund (50% of tower cost)
            var towerDef = database?.GetTower(site.TowerID);
            if (towerDef != null && towerDef.goldCost > 0)
            {
                int refundAmount = Mathf.RoundToInt(towerDef.goldCost * 0.5f);
                var goldManagers = FindObjectsByType<GoldManager>(FindObjectsSortMode.None);
                foreach (var gm in goldManagers)
                {
                    if (gm.OwnerTeam == site.OwnerTeam)
                    {
                        gm.AddGold(refundAmount);
                        Log.Info($"[ConstructionManager] Refunded {refundAmount} gold for destroyed construction site");
                        break;
                    }
                }
            }
        }

        private void SpawnCompletedTower(ConstructionSite site)
        {
            var towerDef = database.GetTower(site.TowerID);
            if (towerDef == null)
            {
                Log.Error($"[ConstructionManager] Tower definition not found: {site.TowerID}");
                return;
            }

            GameObject towerObj = null;

            // Try to load from prefab registry first
            if (prefabRegistry != null)
            {
                towerObj = prefabRegistry.CreateOrFallback(site.TowerID);
            }

            // Fallback: Create default tower object
            if (towerObj == null)
            {
                towerObj = CreateDefaultTower(towerDef);
            }

            // Position the tower
            towerObj.transform.position = site.transform.position;
            towerObj.transform.rotation = site.transform.rotation;
            towerObj.name = $"Tower_{site.TowerID}_{site.OwnerTeam}";

            // Setup TowerController
            var towerController = towerObj.GetComponent<TowerController>();
            if (towerController == null)
            {
                towerController = towerObj.AddComponent<TowerController>();
            }

            // Initialize tower with definition data
            towerController.towerId = site.TowerID;
            towerController.buildCost = towerDef.goldCost;
            towerController.ownerTeam = site.OwnerTeam;

            if (towerDef.baseAttack != null)
            {
                towerController.range = towerDef.baseAttack.range > 0 ? towerDef.baseAttack.range : 6f;
                towerController.damage = towerDef.baseAttack.base_damage > 0 ? towerDef.baseAttack.base_damage : 20f;
                towerController.attacksPerSecond = towerDef.baseAttack.attacks_per_second > 0 ? towerDef.baseAttack.attacks_per_second : 1f;
            }

            // Note: effects are in tiers, not directly on TowerDefinition
            // if (towerDef.tiers != null && towerDef.tiers.Length > 0)
            // {
            //     towerController.effects = towerDef.tiers[0].effects;
            // }

            // Recalculate DPS
            towerController.UpdateDpsCache();

            // Set team layer
            int layer = site.OwnerTeam == GoldManager.Team.Player
                ? LayerMask.NameToLayer("PlayerTower")
                : LayerMask.NameToLayer("EnemyTower");
            if (layer < 0)
            {
                layer = site.OwnerTeam == GoldManager.Team.Player
                    ? LayerMask.NameToLayer("PlayerUnit")
                    : LayerMask.NameToLayer("Enemy");
            }
            if (layer >= 0)
            {
                SetLayerRecursively(towerObj, layer);
            }

            // Set team tag
            towerObj.tag = site.OwnerTeam == GoldManager.Team.Player ? "PlayerTower" : "EnemyTower";

            Log.Info($"[ConstructionManager] Spawned tower: {site.TowerID} at {site.transform.position}");

            OnTowerConstructionComplete?.Invoke(site);
        }

        /// <summary>
        /// Create a default tower object when no prefab is available
        /// </summary>
        private GameObject CreateDefaultTower(TowerDefinition towerDef)
        {
            GameObject towerObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            towerObj.transform.localScale = new Vector3(1.5f, 2f, 1.5f);

            var renderer = towerObj.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material mat = new Material(Shader.Find("Standard"));
                // Color based on tower type
                mat.color = new Color(0.4f, 0.4f, 0.6f, 1f); // Stone gray-blue
                renderer.material = mat;
            }

            // Add a simple turret head
            GameObject turretHead = GameObject.CreatePrimitive(PrimitiveType.Cube);
            turretHead.name = "TurretHead";
            turretHead.transform.SetParent(towerObj.transform);
            turretHead.transform.localPosition = new Vector3(0f, 0.6f, 0f);
            turretHead.transform.localScale = new Vector3(0.6f, 0.4f, 0.6f);

            var headRenderer = turretHead.GetComponent<Renderer>();
            if (headRenderer != null)
            {
                Material headMat = new Material(Shader.Find("Standard"));
                headMat.color = new Color(0.3f, 0.3f, 0.4f, 1f);
                headRenderer.material = headMat;
            }

            // Remove collider from turret head to avoid double collisions
            var headCollider = turretHead.GetComponent<Collider>();
            if (headCollider != null)
            {
                Destroy(headCollider);
            }

            return towerObj;
        }

        /// <summary>
        /// Recursively set layer on object and all children
        /// </summary>
        private void SetLayerRecursively(GameObject obj, int layer)
        {
            obj.layer = layer;
            foreach (Transform child in obj.transform)
            {
                SetLayerRecursively(child.gameObject, layer);
            }
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
