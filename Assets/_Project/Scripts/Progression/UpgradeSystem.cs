using System.Collections.Generic;
using UnityEngine;
using TowerConquest.Data;

namespace TowerConquest.Progression
{
    /// <summary>
    /// Manages upgrades for units, heroes, and towers
    /// </summary>
    public class UpgradeSystem
    {
        private Dictionary<string, int> unitLevels = new Dictionary<string, int>();
        private Dictionary<string, int> heroLevels = new Dictionary<string, int>();
        private Dictionary<string, int> towerLevels = new Dictionary<string, int>();

        private JsonDatabase database;
        private FameManager fameManager;

        public UpgradeSystem(JsonDatabase db, FameManager fame)
        {
            database = db;
            fameManager = fame;
            LoadUpgrades();
        }

        /// <summary>
        /// Get current level of a unit
        /// </summary>
        public int GetUnitLevel(string unitId)
        {
            return unitLevels.ContainsKey(unitId) ? unitLevels[unitId] : 1;
        }

        /// <summary>
        /// Get current level of a hero
        /// </summary>
        public int GetHeroLevel(string heroId)
        {
            return heroLevels.ContainsKey(heroId) ? heroLevels[heroId] : 1;
        }

        /// <summary>
        /// Get upgrade cost for unit
        /// </summary>
        public int GetUnitUpgradeCost(string unitId)
        {
            var unitDef = database.GetUnit(unitId);
            if (unitDef == null || unitDef.upgradeLevels == null) return 0;

            int currentLevel = GetUnitLevel(unitId);
            if (currentLevel >= unitDef.upgradeLevels.Length) return 0; // Max level

            return unitDef.upgradeLevels[currentLevel].fameCost;
        }

        /// <summary>
        /// Get upgrade cost for hero
        /// </summary>
        public int GetHeroUpgradeCost(string heroId)
        {
            var heroDef = database.GetHero(heroId);
            if (heroDef == null || heroDef.upgradeLevels == null) return 0;

            int currentLevel = GetHeroLevel(heroId);
            if (currentLevel >= heroDef.upgradeLevels.Length) return 0; // Max level

            return heroDef.upgradeLevels[currentLevel].fameCost;
        }

        /// <summary>
        /// Upgrade a unit
        /// </summary>
        public bool UpgradeUnit(string unitId)
        {
            int cost = GetUnitUpgradeCost(unitId);
            if (cost == 0)
            {
                Debug.LogWarning($"[UpgradeSystem] Unit {unitId} is already max level");
                return false;
            }

            if (!fameManager.SpendFame(cost))
            {
                return false;
            }

            int currentLevel = GetUnitLevel(unitId);
            unitLevels[unitId] = currentLevel + 1;

            Debug.Log($"[UpgradeSystem] Upgraded {unitId} to level {unitLevels[unitId]}");
            SaveUpgrades();
            return true;
        }

        /// <summary>
        /// Upgrade a hero
        /// </summary>
        public bool UpgradeHero(string heroId)
        {
            int cost = GetHeroUpgradeCost(heroId);
            if (cost == 0)
            {
                Debug.LogWarning($"[UpgradeSystem] Hero {heroId} is already max level");
                return false;
            }

            if (!fameManager.SpendFame(cost))
            {
                return false;
            }

            int currentLevel = GetHeroLevel(heroId);
            heroLevels[heroId] = currentLevel + 1;

            Debug.Log($"[UpgradeSystem] Upgraded hero {heroId} to level {heroLevels[heroId]}");
            SaveUpgrades();
            return true;
        }

        /// <summary>
        /// Get stat bonuses for a unit based on its level
        /// </summary>
        public UpgradeLevel GetUnitUpgradeBonus(string unitId)
        {
            var unitDef = database.GetUnit(unitId);
            if (unitDef == null || unitDef.upgradeLevels == null) return null;

            int level = GetUnitLevel(unitId);
            if (level <= 0 || level > unitDef.upgradeLevels.Length) return null;

            return unitDef.upgradeLevels[level - 1];
        }

        /// <summary>
        /// Get stat bonuses for a hero based on its level
        /// </summary>
        public UpgradeLevel GetHeroUpgradeBonus(string heroId)
        {
            var heroDef = database.GetHero(heroId);
            if (heroDef == null || heroDef.upgradeLevels == null) return null;

            int level = GetHeroLevel(heroId);
            if (level <= 0 || level > heroDef.upgradeLevels.Length) return null;

            return heroDef.upgradeLevels[level - 1];
        }

        private void LoadUpgrades()
        {
            // TODO: Load from SaveManager/PlayerProgress
            string unitLevelsJson = PlayerPrefs.GetString("UnitLevels", "{}");
            string heroLevelsJson = PlayerPrefs.GetString("HeroLevels", "{}");

            // Parse JSON
            // For now, leave empty (all start at level 1)
            Debug.Log("[UpgradeSystem] Loaded upgrades");
        }

        private void SaveUpgrades()
        {
            // TODO: Save via SaveManager/PlayerProgress
            // For now, use PlayerPrefs
            // string unitLevelsJson = JsonUtility.ToJson(unitLevels);
            // PlayerPrefs.SetString("UnitLevels", unitLevelsJson);
            PlayerPrefs.Save();
        }
    }
}
