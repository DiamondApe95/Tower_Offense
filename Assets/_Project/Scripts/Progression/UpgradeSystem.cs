using System.Collections.Generic;
using TowerConquest.Debug;
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
                Log.Warning($"[UpgradeSystem] Unit {unitId} is already max level");
                return false;
            }

            if (!fameManager.SpendFame(cost))
            {
                return false;
            }

            int currentLevel = GetUnitLevel(unitId);
            unitLevels[unitId] = currentLevel + 1;

            Log.Info($"[UpgradeSystem] Upgraded {unitId} to level {unitLevels[unitId]}");
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
                Log.Warning($"[UpgradeSystem] Hero {heroId} is already max level");
                return false;
            }

            if (!fameManager.SpendFame(cost))
            {
                return false;
            }

            int currentLevel = GetHeroLevel(heroId);
            heroLevels[heroId] = currentLevel + 1;

            Log.Info($"[UpgradeSystem] Upgraded hero {heroId} to level {heroLevels[heroId]}");
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
            // Load unit levels
            string unitLevelsJson = PlayerPrefs.GetString("UnitLevels", "");
            if (!string.IsNullOrEmpty(unitLevelsJson))
            {
                try
                {
                    var wrapper = JsonUtility.FromJson<UpgradeLevelsWrapper>(unitLevelsJson);
                    if (wrapper != null && wrapper.entries != null)
                    {
                        foreach (var entry in wrapper.entries)
                        {
                            if (!string.IsNullOrEmpty(entry.id))
                            {
                                unitLevels[entry.id] = entry.level;
                            }
                        }
                    }
                }
                catch (System.Exception e)
                {
                    Log.Warning($"[UpgradeSystem] Failed to parse unit levels: {e.Message}");
                }
            }

            // Load hero levels
            string heroLevelsJson = PlayerPrefs.GetString("HeroLevels", "");
            if (!string.IsNullOrEmpty(heroLevelsJson))
            {
                try
                {
                    var wrapper = JsonUtility.FromJson<UpgradeLevelsWrapper>(heroLevelsJson);
                    if (wrapper != null && wrapper.entries != null)
                    {
                        foreach (var entry in wrapper.entries)
                        {
                            if (!string.IsNullOrEmpty(entry.id))
                            {
                                heroLevels[entry.id] = entry.level;
                            }
                        }
                    }
                }
                catch (System.Exception e)
                {
                    Log.Warning($"[UpgradeSystem] Failed to parse hero levels: {e.Message}");
                }
            }

            // Load tower levels
            string towerLevelsJson = PlayerPrefs.GetString("TowerLevels", "");
            if (!string.IsNullOrEmpty(towerLevelsJson))
            {
                try
                {
                    var wrapper = JsonUtility.FromJson<UpgradeLevelsWrapper>(towerLevelsJson);
                    if (wrapper != null && wrapper.entries != null)
                    {
                        foreach (var entry in wrapper.entries)
                        {
                            if (!string.IsNullOrEmpty(entry.id))
                            {
                                towerLevels[entry.id] = entry.level;
                            }
                        }
                    }
                }
                catch (System.Exception e)
                {
                    Log.Warning($"[UpgradeSystem] Failed to parse tower levels: {e.Message}");
                }
            }

            Log.Info($"[UpgradeSystem] Loaded upgrades: {unitLevels.Count} units, {heroLevels.Count} heroes, {towerLevels.Count} towers");
        }

        private void SaveUpgrades()
        {
            // Save unit levels
            var unitWrapper = new UpgradeLevelsWrapper();
            foreach (var kvp in unitLevels)
            {
                unitWrapper.entries.Add(new UpgradeLevelEntry { id = kvp.Key, level = kvp.Value });
            }
            PlayerPrefs.SetString("UnitLevels", JsonUtility.ToJson(unitWrapper));

            // Save hero levels
            var heroWrapper = new UpgradeLevelsWrapper();
            foreach (var kvp in heroLevels)
            {
                heroWrapper.entries.Add(new UpgradeLevelEntry { id = kvp.Key, level = kvp.Value });
            }
            PlayerPrefs.SetString("HeroLevels", JsonUtility.ToJson(heroWrapper));

            // Save tower levels
            var towerWrapper = new UpgradeLevelsWrapper();
            foreach (var kvp in towerLevels)
            {
                towerWrapper.entries.Add(new UpgradeLevelEntry { id = kvp.Key, level = kvp.Value });
            }
            PlayerPrefs.SetString("TowerLevels", JsonUtility.ToJson(towerWrapper));

            PlayerPrefs.Save();
            Log.Info("[UpgradeSystem] Saved upgrades");
        }

        /// <summary>
        /// Get current level of a tower
        /// </summary>
        public int GetTowerLevel(string towerId)
        {
            return towerLevels.ContainsKey(towerId) ? towerLevels[towerId] : 1;
        }

        /// <summary>
        /// Upgrade a tower
        /// </summary>
        public bool UpgradeTower(string towerId, int cost)
        {
            if (!fameManager.SpendFame(cost))
            {
                return false;
            }

            int currentLevel = GetTowerLevel(towerId);
            towerLevels[towerId] = currentLevel + 1;

            Log.Info($"[UpgradeSystem] Upgraded tower {towerId} to level {towerLevels[towerId]}");
            SaveUpgrades();
            return true;
        }

        /// <summary>
        /// Reset all upgrades (for testing)
        /// </summary>
        public void ResetAllUpgrades()
        {
            unitLevels.Clear();
            heroLevels.Clear();
            towerLevels.Clear();
            SaveUpgrades();
            Log.Info("[UpgradeSystem] Reset all upgrades");
        }
    }

    /// <summary>
    /// Wrapper class for serializing upgrade levels to JSON
    /// </summary>
    [System.Serializable]
    public class UpgradeLevelsWrapper
    {
        public System.Collections.Generic.List<UpgradeLevelEntry> entries = new System.Collections.Generic.List<UpgradeLevelEntry>();
    }

    /// <summary>
    /// Single upgrade level entry for serialization
    /// </summary>
    [System.Serializable]
    public class UpgradeLevelEntry
    {
        public string id;
        public int level;
    }
}
