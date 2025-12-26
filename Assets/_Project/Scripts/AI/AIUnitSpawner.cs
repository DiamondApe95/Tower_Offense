using UnityEngine;
using TowerConquest.Gameplay;
using TowerConquest.Data;

namespace TowerConquest.AI
{
    /// <summary>
    /// Handles unit spawning for AI
    /// </summary>
    public class AIUnitSpawner
    {
        private AICommander commander;
        private GoldManager goldManager;
        private JsonDatabase database;
        private Transform aiBase;

        private CostSystem costSystem;
        private SpawnController spawnController;

        public void Initialize(AICommander cmd, GoldManager gold, JsonDatabase db, Transform baseTransform)
        {
            commander = cmd;
            goldManager = gold;
            database = db;
            aiBase = baseTransform;

            costSystem = new CostSystem(database);
        }

        public void SetSpawnController(SpawnController spawner)
        {
            spawnController = spawner;
        }

        /// <summary>
        /// Attempt to spawn a unit
        /// </summary>
        public bool TrySpawnUnit(string unitId)
        {
            var unitDef = database.GetUnit(unitId);
            if (unitDef == null)
            {
                Debug.LogWarning($"[AIUnitSpawner] Unit not found: {unitId}");
                return false;
            }

            // Check if we can afford it
            if (!costSystem.CanAffordUnit(goldManager, unitId))
            {
                return false;
            }

            // Purchase unit
            if (!costSystem.TryPurchaseUnit(goldManager, unitId))
            {
                return false;
            }

            // Spawn unit at AI base
            SpawnUnit(unitId);

            Debug.Log($"[AIUnitSpawner] Spawned {unitId}");
            return true;
        }

        private void SpawnUnit(string unitId)
        {
            // TODO: Integrate with SpawnController
            // For now, just log

            if (spawnController != null)
            {
                // Use spawn controller to spawn unit
                // spawnController.SpawnUnit(unitId, aiBase.position, GoldManager.Team.AI);
            }
            else
            {
                Debug.Log($"[AIUnitSpawner] Would spawn {unitId} at {aiBase.position}");
            }
        }
    }
}
