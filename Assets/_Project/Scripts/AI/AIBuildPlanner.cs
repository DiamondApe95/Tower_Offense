using UnityEngine;
using TowerConquest.Debug;
using TowerConquest.Gameplay;
using TowerConquest.Data;

namespace TowerConquest.AI
{
    /// <summary>
    /// Plans and executes tower construction for AI
    /// </summary>
    public class AIBuildPlanner
    {
        private AICommander commander;
        private GoldManager goldManager;
        private ConstructionManager constructionManager;
        private JsonDatabase database;

        private CostSystem costSystem;

        public void Initialize(AICommander cmd, GoldManager gold, ConstructionManager construction, JsonDatabase db)
        {
            commander = cmd;
            goldManager = gold;
            constructionManager = construction;
            database = db;

            costSystem = new CostSystem(database);
        }

        /// <summary>
        /// Attempt to build a tower
        /// </summary>
        public bool TryBuildTower(string towerId)
        {
            var towerDef = database.GetTower(towerId);
            if (towerDef == null)
            {
                Log.Warning($"[AIBuildPlanner] Tower not found: {towerId}");
                return false;
            }

            // Check if we can afford it
            if (!costSystem.CanAffordTower(goldManager, towerId))
            {
                return false;
            }

            // Find a valid build location
            Vector3 buildPosition = FindBuildLocation();
            if (buildPosition == Vector3.zero)
            {
                Log.Info("[AIBuildPlanner] No valid build location found");
                return false;
            }

            // Purchase tower
            if (!costSystem.TryPurchaseTower(goldManager, towerId))
            {
                return false;
            }

            // Place construction site
            var site = constructionManager.PlaceTower(towerId, buildPosition, GoldManager.Team.AI);
            if (site == null)
            {
                // Refund gold if placement failed
                goldManager.AddGold(towerDef.goldCost);
                return false;
            }

            // Spawn builders
            SpawnBuilders(site, buildPosition);

            Log.Info($"[AIBuildPlanner] Started building {towerId} at {buildPosition}");
            return true;
        }

        private Vector3 FindBuildLocation()
        {
            // Simple logic: find random position near AI base
            // TODO: Implement proper pathfinding and strategic placement

            Transform aiBase = commander.GetBase();
            if (aiBase == null)
            {
                Log.Warning("[AIBuildPlanner] AI base not set");
                return Vector3.zero;
            }

            // Try multiple times to find a valid position within map boundaries
            int maxAttempts = 10;
            for (int i = 0; i < maxAttempts; i++)
            {
                // Random offset from base (reduced range for better chances)
                Vector3 offset = new Vector3(
                    Random.Range(-15f, 15f),
                    0f,
                    Random.Range(-15f, 15f)
                );

                Vector3 candidatePosition = aiBase.position + offset;

                // Validate position is within map boundaries
                if (MapBoundary.Instance != null)
                {
                    if (MapBoundary.Instance.IsWithinSafeBounds(candidatePosition))
                    {
                        return candidatePosition;
                    }
                }
                else
                {
                    // No boundary system found, return candidate (backward compatibility)
                    return candidatePosition;
                }
            }

            // If all attempts failed, return a clamped position near the base
            if (MapBoundary.Instance != null)
            {
                return MapBoundary.Instance.ClampToSafeBounds(aiBase.position);
            }

            return aiBase.position;
        }

        private void SpawnBuilders(ConstructionSite site, Vector3 buildPosition)
        {
            if (site == null) return;

            // Spawn required number of builders
            for (int i = 0; i < site.RequiredBuilders; i++)
            {
                // Spawn builder near AI base
                Vector3 spawnPos = commander.GetBase().position + Random.insideUnitSphere * 5f;
                spawnPos.y = 0f;

                // Clamp to map boundaries
                if (MapBoundary.Instance != null)
                {
                    spawnPos = MapBoundary.Instance.ClampToSafeBounds(spawnPos);
                }

                constructionManager.SpawnBuilder(spawnPos, GoldManager.Team.AI);
            }
        }
    }
}
