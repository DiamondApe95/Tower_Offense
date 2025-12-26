using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TowerConquest.Core;
using TowerConquest.Data;
using TowerConquest.Saving;

namespace TowerConquest.Gameplay
{
    /// <summary>
    /// Manages civilizations and their available units/towers/heroes
    /// </summary>
    public class CivilizationManager
    {
        private JsonDatabase database;
        private Dictionary<string, CivilizationDefinition> civilizations;

        public CivilizationManager(JsonDatabase db)
        {
            database = db;
            civilizations = new Dictionary<string, CivilizationDefinition>();
            LoadCivilizations();
        }

        private void LoadCivilizations()
        {
            // Load from JsonDatabase
            if (database != null && database.Civilizations != null)
            {
                foreach (var civ in database.Civilizations)
                {
                    if (civ != null && !string.IsNullOrEmpty(civ.id))
                    {
                        civilizations[civ.id] = civ;
                    }
                }
                Debug.Log($"[CivilizationManager] Loaded {civilizations.Count} civilizations from database");
            }
            else
            {
                Debug.LogWarning("[CivilizationManager] No civilizations in database, creating defaults");
                CreateDefaultCivilizations();
            }
        }

        /// <summary>
        /// Create default civilizations when none are loaded from JSON
        /// </summary>
        private void CreateDefaultCivilizations()
        {
            // Kingdom civilization
            var kingdom = new CivilizationDefinition
            {
                id = "civ_kingdom",
                name = "Kingdom",
                description = "A noble kingdom with balanced forces",
                unlockCost = 0,
                availableUnits = new string[] { "unit_swordsman", "unit_archer", "unit_knight", "unit_priest", "unit_catapult" },
                availableTowers = new string[] { "tower_watchtower", "tower_ballista", "tower_mage" },
                availableHeroes = new string[] { "hero_artus" },
                specialAbility = "ability_divine_protection"
            };
            civilizations[kingdom.id] = kingdom;

            // Horde civilization
            var horde = new CivilizationDefinition
            {
                id = "civ_horde",
                name = "Horde",
                description = "Fierce warriors with overwhelming force",
                unlockCost = 500,
                availableUnits = new string[] { "unit_warrior", "unit_wolfrider", "unit_shaman", "unit_troll", "unit_ram" },
                availableTowers = new string[] { "tower_spear", "tower_flame", "tower_totem" },
                availableHeroes = new string[] { "hero_grok" },
                specialAbility = "ability_bloodlust"
            };
            civilizations[horde.id] = horde;

            // Undead civilization
            var undead = new CivilizationDefinition
            {
                id = "civ_undead",
                name = "Undead",
                description = "Risen dead serving a dark master",
                unlockCost = 1000,
                availableUnits = new string[] { "unit_skeleton", "unit_zombie", "unit_ghost", "unit_necromancer", "unit_bone_golem" },
                availableTowers = new string[] { "tower_soul", "tower_poison", "tower_bone_spike" },
                availableHeroes = new string[] { "hero_lich" },
                specialAbility = "ability_raise_dead"
            };
            civilizations[undead.id] = undead;

            Debug.Log($"[CivilizationManager] Created {civilizations.Count} default civilizations");
        }

        public void RegisterCivilization(CivilizationDefinition civDef)
        {
            if (civilizations.ContainsKey(civDef.id))
            {
                Debug.LogWarning($"[CivilizationManager] Civilization already registered: {civDef.id}");
                return;
            }

            civilizations[civDef.id] = civDef;
            Debug.Log($"[CivilizationManager] Registered civilization: {civDef.name}");
        }

        public CivilizationDefinition GetCivilization(string id)
        {
            if (civilizations.TryGetValue(id, out var civ))
            {
                return civ;
            }

            Debug.LogWarning($"[CivilizationManager] Civilization not found: {id}");
            return null;
        }

        public List<CivilizationDefinition> GetAllCivilizations()
        {
            return civilizations.Values.ToList();
        }

        public List<CivilizationDefinition> GetUnlockedCivilizations()
        {
            var unlockedCivs = new List<CivilizationDefinition>();

            // Get player progress
            SaveManager saveManager = null;
            if (ServiceLocator.TryGet(out saveManager))
            {
                PlayerProgress progress = saveManager.GetOrCreateProgress();

                foreach (var civ in civilizations.Values)
                {
                    // Free civilizations (unlockCost == 0) are always available
                    if (civ.unlockCost == 0)
                    {
                        unlockedCivs.Add(civ);
                    }
                    // Check if civilization is unlocked in progress
                    else if (progress.unlockedCivilizationIds.Contains(civ.id))
                    {
                        unlockedCivs.Add(civ);
                    }
                }
            }
            else
            {
                // Fallback: return all free civilizations
                Debug.LogWarning("[CivilizationManager] SaveManager not found, returning only free civilizations");
                unlockedCivs = civilizations.Values.Where(c => c.unlockCost == 0).ToList();
            }

            return unlockedCivs;
        }

        /// <summary>
        /// Check if a civilization is unlocked for the player
        /// </summary>
        public bool IsCivilizationUnlocked(string civId)
        {
            var civ = GetCivilization(civId);
            if (civ == null) return false;

            // Free civilizations are always unlocked
            if (civ.unlockCost == 0) return true;

            // Check player progress
            SaveManager saveManager = null;
            if (ServiceLocator.TryGet(out saveManager))
            {
                PlayerProgress progress = saveManager.GetOrCreateProgress();
                return progress.unlockedCivilizationIds.Contains(civId);
            }

            return false;
        }

        /// <summary>
        /// Unlock a civilization with Fame
        /// </summary>
        public bool TryUnlockCivilization(string civId, out string errorMessage)
        {
            errorMessage = "";
            var civ = GetCivilization(civId);
            if (civ == null)
            {
                errorMessage = "Civilization not found";
                return false;
            }

            // Already unlocked?
            if (IsCivilizationUnlocked(civId))
            {
                errorMessage = "Already unlocked";
                return false;
            }

            // Get required systems
            SaveManager saveManager = null;
            if (!ServiceLocator.TryGet(out saveManager))
            {
                errorMessage = "SaveManager not available";
                return false;
            }

            PlayerProgress progress = saveManager.GetOrCreateProgress();

            // Check if player has enough fame
            if (progress.fame < civ.unlockCost)
            {
                errorMessage = $"Not enough Fame. Need {civ.unlockCost}, have {progress.fame}";
                return false;
            }

            // Spend fame and unlock
            progress.fame -= civ.unlockCost;
            progress.unlockedCivilizationIds.Add(civId);
            saveManager.SaveProgress(progress);

            Debug.Log($"[CivilizationManager] Unlocked civilization {civ.name} for {civ.unlockCost} Fame");
            return true;
        }

        public List<UnitDefinition> GetAvailableUnits(string civId)
        {
            var civ = GetCivilization(civId);
            if (civ == null) return new List<UnitDefinition>();

            var units = new List<UnitDefinition>();
            foreach (var unitId in civ.availableUnits)
            {
                var unitDef = database.GetUnit(unitId);
                if (unitDef != null)
                {
                    units.Add(unitDef);
                }
            }

            return units;
        }

        public List<TowerDefinition> GetAvailableTowers(string civId)
        {
            var civ = GetCivilization(civId);
            if (civ == null) return new List<TowerDefinition>();

            var towers = new List<TowerDefinition>();
            foreach (var towerId in civ.availableTowers)
            {
                var towerDef = database.GetTower(towerId);
                if (towerDef != null)
                {
                    towers.Add(towerDef);
                }
            }

            return towers;
        }

        public List<HeroDefinition> GetAvailableHeroes(string civId)
        {
            var civ = GetCivilization(civId);
            if (civ == null) return new List<HeroDefinition>();

            var heroes = new List<HeroDefinition>();
            foreach (var heroId in civ.availableHeroes)
            {
                var heroDef = database.GetHero(heroId);
                if (heroDef != null)
                {
                    heroes.Add(heroDef);
                }
            }

            return heroes;
        }

        public AbilityDefinition GetCivilizationAbility(string civId)
        {
            var civ = GetCivilization(civId);
            if (civ == null || string.IsNullOrEmpty(civ.specialAbility)) return null;

            // Load from abilities database
            return database?.FindAbility(civ.specialAbility);
        }

        /// <summary>
        /// Get the default (first unlocked) civilization
        /// </summary>
        public CivilizationDefinition GetDefaultCivilization()
        {
            // Return first with unlockCost == 0
            foreach (var civ in civilizations.Values)
            {
                if (civ.unlockCost == 0)
                {
                    return civ;
                }
            }
            // Return first if none are free
            return civilizations.Values.FirstOrDefault();
        }

    }
}
