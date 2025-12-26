using System;
using System.Collections.Generic;
using TowerConquest.Core;
using TowerConquest.Data;
using TowerConquest.Gameplay.Entities;
using UnityEngine;

namespace TowerConquest.Gameplay
{
    /// <summary>
    /// Manages trap placement on path tiles
    /// Traps are placed as construction sites that require 1 builder
    /// </summary>
    public class TrapPlacementManager : MonoBehaviour
    {
        [Header("Prefabs")]
        [SerializeField] private GameObject trapConstructionSitePrefab;
        [SerializeField] private GameObject trapPrefab;

        [Header("Settings")]
        [SerializeField] private Transform trapParent;
        [SerializeField] private LayerMask pathTileLayer;
        [SerializeField] private float placementCheckRadius = 0.5f;

        [Header("Runtime")]
        [SerializeField] private List<TrapConstructionSite> activeConstructionSites = new List<TrapConstructionSite>();
        [SerializeField] private List<TrapController> activeTraps = new List<TrapController>();

        // Events
        public event Action<TrapConstructionSite> OnTrapConstructionStarted;
        public event Action<TrapController> OnTrapCompleted;
        public event Action<TrapConstructionSite> OnTrapConstructionDestroyed;

        private JsonDatabase database;
        private ConstructionManager constructionManager;

        private void Awake()
        {
            if (trapParent == null)
            {
                var go = new GameObject("Traps");
                trapParent = go.transform;
                trapParent.SetParent(transform);
            }
        }

        public void Initialize(JsonDatabase db, ConstructionManager constrManager)
        {
            database = db;
            constructionManager = constrManager;
        }

        /// <summary>
        /// Check if a trap can be placed at the given position
        /// </summary>
        public bool CanPlaceTrap(Vector3 position)
        {
            // Check if on path tile
            if (!IsOnPathTile(position))
            {
                return false;
            }

            // Allow multiple traps on same path tile
            return true;
        }

        /// <summary>
        /// Check if position is on a path tile
        /// </summary>
        public bool IsOnPathTile(Vector3 position)
        {
            // Raycast to check for path tile
            RaycastHit hit;
            if (Physics.Raycast(position + Vector3.up * 5f, Vector3.down, out hit, 10f, pathTileLayer))
            {
                return hit.collider.CompareTag("PathTile") || hit.collider.gameObject.layer == LayerMask.NameToLayer("PathTile");
            }

            // Fallback: Check by name
            Collider[] colliders = Physics.OverlapSphere(position, placementCheckRadius);
            foreach (var col in colliders)
            {
                if (col.name.Contains("Path") || col.CompareTag("PathTile"))
                {
                    return true;
                }
            }

            // If no path checking available, allow placement anywhere (for testing)
            return true;
        }

        /// <summary>
        /// Place a trap construction site
        /// </summary>
        public TrapConstructionSite PlaceTrap(string trapId, Vector3 position, GoldManager.Team team)
        {
            var trapDef = database?.FindTrap(trapId);
            if (trapDef == null)
            {
                Debug.LogError($"[TrapPlacementManager] Trap definition not found: {trapId}");
                return null;
            }

            if (!CanPlaceTrap(position))
            {
                Debug.LogWarning($"[TrapPlacementManager] Cannot place trap at {position}");
                return null;
            }

            // Create construction site
            GameObject siteObj;
            if (trapConstructionSitePrefab != null)
            {
                siteObj = Instantiate(trapConstructionSitePrefab, position, Quaternion.identity, trapParent);
            }
            else
            {
                siteObj = CreateDefaultConstructionSite(position);
            }

            siteObj.name = $"TrapSite_{trapId}_{team}";

            var site = siteObj.GetComponent<TrapConstructionSite>();
            if (site == null)
            {
                site = siteObj.AddComponent<TrapConstructionSite>();
            }

            // Traps require only 1 builder
            int requiredBuilders = 1;
            float constructionHP = trapDef.trigger?.cooldown_seconds > 0 ? 50f : 30f;

            site.Initialize(trapId, requiredBuilders, constructionHP, team);

            // Subscribe to events
            site.OnConstructionComplete += OnTrapSiteComplete;
            site.OnConstructionDestroyed += OnTrapSiteDestroyed;

            activeConstructionSites.Add(site);

            Debug.Log($"[TrapPlacementManager] Placed trap construction site for {trapId} at {position}");

            // Request builder from construction manager
            if (constructionManager != null)
            {
                constructionManager.RequestBuilderForTrap(site);
            }

            OnTrapConstructionStarted?.Invoke(site);

            return site;
        }

        private GameObject CreateDefaultConstructionSite(Vector3 position)
        {
            GameObject siteObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            siteObj.transform.position = position;
            siteObj.transform.localScale = new Vector3(1f, 0.3f, 1f);
            siteObj.transform.SetParent(trapParent);

            var renderer = siteObj.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material mat = new Material(Shader.Find("Standard"));
                mat.color = new Color(0.6f, 0.4f, 0.2f, 0.8f);
                renderer.material = mat;
            }

            return siteObj;
        }

        private void OnTrapSiteComplete(TrapConstructionSite site)
        {
            Debug.Log($"[TrapPlacementManager] Trap construction complete: {site.TrapID}");

            // Spawn the actual trap
            SpawnTrap(site);

            // Clean up
            activeConstructionSites.Remove(site);
            site.OnConstructionComplete -= OnTrapSiteComplete;
            site.OnConstructionDestroyed -= OnTrapSiteDestroyed;

            Destroy(site.gameObject);
        }

        private void OnTrapSiteDestroyed(TrapConstructionSite site)
        {
            Debug.Log($"[TrapPlacementManager] Trap construction destroyed: {site.TrapID}");

            activeConstructionSites.Remove(site);
            site.OnConstructionComplete -= OnTrapSiteComplete;
            site.OnConstructionDestroyed -= OnTrapSiteDestroyed;

            OnTrapConstructionDestroyed?.Invoke(site);
        }

        private void SpawnTrap(TrapConstructionSite site)
        {
            var trapDef = database?.FindTrap(site.TrapID);
            if (trapDef == null)
            {
                Debug.LogError($"[TrapPlacementManager] Trap definition not found: {site.TrapID}");
                return;
            }

            GameObject trapObj;
            if (trapPrefab != null)
            {
                trapObj = Instantiate(trapPrefab, site.transform.position, Quaternion.identity, trapParent);
            }
            else
            {
                trapObj = CreateDefaultTrap(site.transform.position);
            }

            trapObj.name = $"Trap_{site.TrapID}_{site.OwnerTeam}";

            var trap = trapObj.GetComponent<TrapController>();
            if (trap == null)
            {
                trap = trapObj.AddComponent<TrapController>();
            }

            Vector3 size = new Vector3(1.5f, 0.5f, 1.5f);
            trap.Initialize(site.TrapID, size);

            activeTraps.Add(trap);

            OnTrapCompleted?.Invoke(trap);
        }

        private GameObject CreateDefaultTrap(Vector3 position)
        {
            GameObject trapObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trapObj.transform.position = position;
            trapObj.transform.localScale = new Vector3(1.5f, 0.1f, 1.5f);
            trapObj.transform.SetParent(trapParent);

            var renderer = trapObj.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material mat = new Material(Shader.Find("Standard"));
                mat.color = new Color(0.5f, 0.3f, 0.1f, 1f);
                renderer.material = mat;
            }

            return trapObj;
        }

        public List<TrapConstructionSite> GetActiveSites()
        {
            return new List<TrapConstructionSite>(activeConstructionSites);
        }

        public List<TrapController> GetActiveTraps()
        {
            return new List<TrapController>(activeTraps);
        }

        private void OnDestroy()
        {
            foreach (var site in activeConstructionSites)
            {
                if (site != null)
                {
                    site.OnConstructionComplete -= OnTrapSiteComplete;
                    site.OnConstructionDestroyed -= OnTrapSiteDestroyed;
                }
            }
        }
    }
}
