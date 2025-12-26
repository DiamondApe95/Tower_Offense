using System;
using TowerConquest.Debug;
using System.Collections.Generic;
using TowerConquest.Core;
using TowerConquest.Data;
using TowerConquest.UI;
using UnityEngine;

namespace TowerConquest.Gameplay
{
    /// <summary>
    /// Manages the three build categories: Units, Towers, Traps
    /// Handles placement validation and construction initiation
    /// </summary>
    public class BuildCategoryManager : MonoBehaviour
    {
        public enum BuildCategory
        {
            Units,
            Towers,
            Traps
        }

        [Header("References")]
        [SerializeField] private ConstructionManager constructionManager;
        [SerializeField] private TrapPlacementManager trapPlacementManager;
        [SerializeField] private LiveBattleSpawnController playerSpawner;

        [Header("Placement Settings")]
        [SerializeField] private LayerMask buildTileLayer;
        [SerializeField] private LayerMask pathTileLayer;
        [SerializeField] private float placementCheckRadius = 1f;

        [Header("State")]
        [SerializeField] private BuildCategory currentCategory = BuildCategory.Units;
        [SerializeField] private string selectedBuildableId;
        [SerializeField] private bool isPlacingMode = false;

        // Events
        public event Action<BuildCategory> OnCategoryChanged;
        public event Action<string> OnBuildableSelected;
        public event Action<Vector3> OnPlacementStarted;
        public event Action<bool> OnPlacementComplete; // true = success

        private JsonDatabase database;
        private GoldManager playerGold;
        private Camera mainCamera;
        private LiveBattleLevelController levelController;

        public BuildCategory CurrentCategory => currentCategory;
        public string SelectedBuildableId => selectedBuildableId;
        public bool IsPlacingMode => isPlacingMode;

        private void Awake()
        {
            mainCamera = Camera.main;
        }

        public void Initialize(LiveBattleLevelController controller)
        {
            levelController = controller;
            database = ServiceLocator.Get<JsonDatabase>();
            playerGold = controller.PlayerGold;
            playerSpawner = controller.PlayerSpawner;

            // Find or create managers
            if (constructionManager == null)
            {
                constructionManager = FindFirstObjectByType<ConstructionManager>();
            }

            if (trapPlacementManager == null)
            {
                trapPlacementManager = FindFirstObjectByType<TrapPlacementManager>();
                if (trapPlacementManager == null)
                {
                    var go = new GameObject("TrapPlacementManager");
                    trapPlacementManager = go.AddComponent<TrapPlacementManager>();
                    trapPlacementManager.Initialize(database, constructionManager);
                }
            }
        }

        /// <summary>
        /// Switch to a different build category
        /// </summary>
        public void SetCategory(BuildCategory category)
        {
            if (currentCategory == category) return;

            currentCategory = category;
            selectedBuildableId = null;
            isPlacingMode = false;

            OnCategoryChanged?.Invoke(category);
            Log.Info($"[BuildCategoryManager] Switched to category: {category}");
        }

        /// <summary>
        /// Select a buildable item (unit, tower, or trap)
        /// </summary>
        public void SelectBuildable(string buildableId)
        {
            selectedBuildableId = buildableId;

            // For units, no placement mode needed
            if (currentCategory == BuildCategory.Units)
            {
                isPlacingMode = false;
            }
            else
            {
                isPlacingMode = true;
            }

            OnBuildableSelected?.Invoke(buildableId);
            Log.Info($"[BuildCategoryManager] Selected: {buildableId}");
        }

        /// <summary>
        /// Get the cost of the selected buildable
        /// </summary>
        public int GetSelectedCost()
        {
            if (string.IsNullOrEmpty(selectedBuildableId)) return 0;

            switch (currentCategory)
            {
                case BuildCategory.Units:
                    var unitDef = database?.FindUnit(selectedBuildableId);
                    return unitDef?.goldCost ?? 0;

                case BuildCategory.Towers:
                    var towerDef = database?.GetTower(selectedBuildableId);
                    return towerDef?.goldCost ?? 0;

                case BuildCategory.Traps:
                    var trapDef = database?.FindTrap(selectedBuildableId);
                    return trapDef?.goldCost ?? 0;

                default:
                    return 0;
            }
        }

        /// <summary>
        /// Check if the player can afford the selected buildable
        /// </summary>
        public bool CanAffordSelected()
        {
            int cost = GetSelectedCost();
            return playerGold != null && playerGold.CanAfford(cost);
        }

        /// <summary>
        /// Try to place the selected buildable at the given position
        /// </summary>
        public bool TryPlace(Vector3 position)
        {
            if (!levelController.CanPerformGameplayActions())
            {
                Log.Info("[BuildCategoryManager] Cannot place during countdown");
                return false;
            }

            if (string.IsNullOrEmpty(selectedBuildableId))
            {
                Log.Warning("[BuildCategoryManager] No buildable selected");
                return false;
            }

            int cost = GetSelectedCost();
            if (!playerGold.CanAfford(cost))
            {
                Log.Info("[BuildCategoryManager] Not enough gold");
                return false;
            }

            bool success = false;

            switch (currentCategory)
            {
                case BuildCategory.Units:
                    // Units don't need placement, they spawn at base
                    success = TrySpawnUnit();
                    break;

                case BuildCategory.Towers:
                    success = TryPlaceTower(position);
                    break;

                case BuildCategory.Traps:
                    success = TryPlaceTrap(position);
                    break;
            }

            if (success)
            {
                playerGold.SpendGold(cost);
                OnPlacementComplete?.Invoke(true);
            }
            else
            {
                OnPlacementComplete?.Invoke(false);
            }

            return success;
        }

        private bool TrySpawnUnit()
        {
            // Find the slot index for this unit in the player's deck
            var deck = levelController.PlayerDeck;
            if (deck == null) return false;

            int slotIndex = deck.SelectedUnits.IndexOf(selectedBuildableId);
            if (slotIndex < 0)
            {
                Log.Warning($"[BuildCategoryManager] Unit not in deck: {selectedBuildableId}");
                return false;
            }

            return playerSpawner.TrySpawnUnit(slotIndex);
        }

        private bool TryPlaceTower(Vector3 position)
        {
            // Check if position is on a build tile
            if (!IsOnBuildTile(position))
            {
                Log.Info("[BuildCategoryManager] Position is not on a build tile");
                return false;
            }

            // Check if there's already a tower or construction site
            if (HasExistingConstruction(position))
            {
                Log.Info("[BuildCategoryManager] Position already has a construction");
                return false;
            }

            // Place tower construction site
            var site = constructionManager.PlaceTower(selectedBuildableId, position, GoldManager.Team.Player);
            if (site != null)
            {
                // Attach progress UI
                ConstructionProgressUI.AttachTo(site);
                OnPlacementStarted?.Invoke(position);
                return true;
            }

            return false;
        }

        private bool TryPlaceTrap(Vector3 position)
        {
            // Check if position is on a path tile
            if (!IsOnPathTile(position))
            {
                Log.Info("[BuildCategoryManager] Position is not on a path tile");
                return false;
            }

            // Place trap construction site
            var site = trapPlacementManager.PlaceTrap(selectedBuildableId, position, GoldManager.Team.Player);
            if (site != null)
            {
                // Attach progress UI
                ConstructionProgressUI.AttachTo(site);
                OnPlacementStarted?.Invoke(position);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Check if a position is on a build tile
        /// </summary>
        public bool IsOnBuildTile(Vector3 position)
        {
            // Raycast check
            RaycastHit hit;
            if (Physics.Raycast(position + Vector3.up * 5f, Vector3.down, out hit, 10f, buildTileLayer))
            {
                return true;
            }

            // Collider check
            Collider[] colliders = Physics.OverlapSphere(position, placementCheckRadius, buildTileLayer);
            if (colliders.Length > 0) return true;

            // Tag check fallback
            colliders = Physics.OverlapSphere(position, placementCheckRadius);
            foreach (var col in colliders)
            {
                if (col.CompareTag("BuildTile") || col.name.Contains("BuildTile"))
                {
                    return true;
                }
            }

            // For testing, allow placement anywhere if no build tiles are defined
            return true;
        }

        /// <summary>
        /// Check if a position is on a path tile
        /// </summary>
        public bool IsOnPathTile(Vector3 position)
        {
            // Raycast check
            RaycastHit hit;
            if (Physics.Raycast(position + Vector3.up * 5f, Vector3.down, out hit, 10f, pathTileLayer))
            {
                return true;
            }

            // Collider check
            Collider[] colliders = Physics.OverlapSphere(position, placementCheckRadius, pathTileLayer);
            if (colliders.Length > 0) return true;

            // Tag check fallback
            colliders = Physics.OverlapSphere(position, placementCheckRadius);
            foreach (var col in colliders)
            {
                if (col.CompareTag("PathTile") || col.name.Contains("Path"))
                {
                    return true;
                }
            }

            // For testing, allow placement anywhere if no path tiles are defined
            return true;
        }

        /// <summary>
        /// Check if there's already a construction at the position
        /// </summary>
        public bool HasExistingConstruction(Vector3 position)
        {
            // Check for existing construction sites
            var sites = constructionManager?.GetActiveSites();
            if (sites != null)
            {
                foreach (var site in sites)
                {
                    if (Vector3.Distance(site.transform.position, position) < placementCheckRadius * 2)
                    {
                        return true;
                    }
                }
            }

            // Check for existing towers
            Collider[] colliders = Physics.OverlapSphere(position, placementCheckRadius);
            foreach (var col in colliders)
            {
                if (col.CompareTag("Tower") || col.GetComponent<Entities.TowerController>() != null)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Cancel current placement mode
        /// </summary>
        public void CancelPlacement()
        {
            isPlacingMode = false;
            selectedBuildableId = null;
            Log.Info("[BuildCategoryManager] Placement cancelled");
        }

        /// <summary>
        /// Get available buildables for the current category
        /// </summary>
        public List<string> GetAvailableBuildables()
        {
            var result = new List<string>();

            switch (currentCategory)
            {
                case BuildCategory.Units:
                    if (levelController?.PlayerDeck != null)
                    {
                        result.AddRange(levelController.PlayerDeck.SelectedUnits);
                    }
                    break;

                case BuildCategory.Towers:
                    var civId = levelController?.PlayerDeck?.CivilizationID;
                    if (!string.IsNullOrEmpty(civId))
                    {
                        var civ = database?.FindCivilization(civId);
                        if (civ?.availableTowers != null)
                        {
                            result.AddRange(civ.availableTowers);
                        }
                    }
                    break;

                case BuildCategory.Traps:
                    // Get all available traps from database
                    var traps = database?.GetAllTraps();
                    if (traps != null)
                    {
                        foreach (var trap in traps)
                        {
                            result.Add(trap.id);
                        }
                    }
                    break;
            }

            return result;
        }

        private void Update()
        {
            // Handle placement input when in placing mode
            if (!isPlacingMode || !levelController.CanPerformGameplayActions()) return;

            // Right-click to cancel
            if (Input.GetMouseButtonDown(1))
            {
                CancelPlacement();
                return;
            }

            // Left-click to place
            if (Input.GetMouseButtonDown(0))
            {
                Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;

                if (Physics.Raycast(ray, out hit, 1000f))
                {
                    TryPlace(hit.point);
                }
            }
        }
    }
}
