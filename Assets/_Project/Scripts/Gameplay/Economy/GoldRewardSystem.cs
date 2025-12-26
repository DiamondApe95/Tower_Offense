using UnityEngine;
using TowerConquest.Data;

namespace TowerConquest.Gameplay
{
    /// <summary>
    /// Calculates gold rewards for kills and destructions
    /// </summary>
    public class GoldRewardSystem
    {
        private JsonDatabase database;

        public GoldRewardSystem(JsonDatabase db)
        {
            database = db;
        }

        /// <summary>
        /// Calculate gold reward for killing a unit
        /// </summary>
        public int CalculateUnitReward(string unitId)
        {
            var unitDef = database.GetUnit(unitId);
            if (unitDef == null)
            {
                Debug.LogWarning($"[GoldRewardSystem] Unit not found: {unitId}");
                return 0;
            }

            return unitDef.goldReward;
        }

        /// <summary>
        /// Calculate gold reward for destroying a tower
        /// </summary>
        public int CalculateTowerReward(string towerId)
        {
            var towerDef = database.GetTower(towerId);
            if (towerDef == null)
            {
                Debug.LogWarning($"[GoldRewardSystem] Tower not found: {towerId}");
                return 0;
            }

            return towerDef.goldReward;
        }

        /// <summary>
        /// Calculate gold reward for killing a hero
        /// </summary>
        public int CalculateHeroReward(string heroId)
        {
            // Heroes are typically worth more than regular units
            var heroDef = database.GetHero(heroId);
            if (heroDef == null)
            {
                Debug.LogWarning($"[GoldRewardSystem] Hero not found: {heroId}");
                return 0;
            }

            // Heroes don't have goldReward field, so calculate based on stats
            // This is a simple formula: base reward of 200 gold
            return 200;
        }

        /// <summary>
        /// Calculate combo bonus for multiple kills in short time
        /// </summary>
        public int CalculateComboBonus(int killCount, float timeWindow)
        {
            if (killCount < 2)
                return 0;

            // Bonus scaling:
            // 2-3 kills: +10 gold
            // 4-5 kills: +20 gold
            // 6+ kills: +30 gold

            if (killCount >= 6)
                return 30;
            else if (killCount >= 4)
                return 20;
            else
                return 10;
        }
    }
}
