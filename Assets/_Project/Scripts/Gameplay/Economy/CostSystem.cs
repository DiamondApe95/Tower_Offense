using UnityEngine;
using TowerConquest.Data;

namespace TowerConquest.Gameplay
{
    /// <summary>
    /// Handles cost calculations for units, towers, and abilities
    /// </summary>
    public class CostSystem
    {
        private JsonDatabase database;

        public CostSystem(JsonDatabase db)
        {
            database = db;
        }

        /// <summary>
        /// Get the gold cost to spawn a unit
        /// </summary>
        public int GetUnitCost(string unitId)
        {
            var unitDef = database.GetUnit(unitId);
            if (unitDef == null)
            {
                Debug.LogWarning($"[CostSystem] Unit not found: {unitId}");
                return 0;
            }

            return unitDef.goldCost;
        }

        /// <summary>
        /// Get the gold cost to build a tower
        /// </summary>
        public int GetTowerCost(string towerId)
        {
            var towerDef = database.GetTower(towerId);
            if (towerDef == null)
            {
                Debug.LogWarning($"[CostSystem] Tower not found: {towerId}");
                return 0;
            }

            return towerDef.goldCost;
        }

        /// <summary>
        /// Check if gold manager can afford a unit
        /// </summary>
        public bool CanAffordUnit(GoldManager goldManager, string unitId)
        {
            int cost = GetUnitCost(unitId);
            return goldManager.CanAfford(cost);
        }

        /// <summary>
        /// Check if gold manager can afford a tower
        /// </summary>
        public bool CanAffordTower(GoldManager goldManager, string towerId)
        {
            int cost = GetTowerCost(towerId);
            return goldManager.CanAfford(cost);
        }

        /// <summary>
        /// Attempt to purchase a unit
        /// </summary>
        public bool TryPurchaseUnit(GoldManager goldManager, string unitId)
        {
            int cost = GetUnitCost(unitId);
            if (goldManager.SpendGold(cost))
            {
                Debug.Log($"[CostSystem] Purchased unit '{unitId}' for {cost} gold");
                return true;
            }
            return false;
        }

        /// <summary>
        /// Attempt to purchase a tower
        /// </summary>
        public bool TryPurchaseTower(GoldManager goldManager, string towerId)
        {
            int cost = GetTowerCost(towerId);
            if (goldManager.SpendGold(cost))
            {
                Debug.Log($"[CostSystem] Purchased tower '{towerId}' for {cost} gold");
                return true;
            }
            return false;
        }
    }
}
