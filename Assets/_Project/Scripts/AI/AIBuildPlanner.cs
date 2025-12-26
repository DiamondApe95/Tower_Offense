using UnityEngine;
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
                Debug.LogWarning($"[AIBuildPlanner] Tower not found: {towerId}");
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
                Debug.Log("[AIBuildPlanner] No valid build location found");
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

            Debug.Log($"[AIBuildPlanner] Started building {towerId} at {buildPosition}");
            return true;
        }

        private Vector3 FindBuildLocation()
        {
            // Simple logic: find random position near AI base
            // TODO: Implement proper pathfinding and strategic placement

            Transform aiBase = commander.GetBase();
            if (aiBase == null)
            {
                Debug.LogWarning("[AIBuildPlanner] AI base not set");
                return Vector3.zero;
            }

            // Random offset from base
            Vector3 offset = new Vector3(
                Random.Range(-20f, 20f),
                0f,
                Random.Range(-20f, 20f)
            );

            return aiBase.position + offset;
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

                constructionManager.SpawnBuilder(spawnPos, GoldManager.Team.AI);
            }
        }
    }
}
