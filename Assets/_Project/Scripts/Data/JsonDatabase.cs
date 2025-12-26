using System.Collections.Generic;
using TowerConquest.Debug;
using System.IO;
using UnityEngine;

namespace TowerConquest.Data
{
    public class JsonDatabase
    {
        public List<UnitDefinition> Units;
        public List<SpellDefinition> Spells;
        public List<TowerDefinition> Towers;
        public List<TrapDefinition> Traps;
        public List<LevelDefinition> Levels;
        public List<HeroDefinition> Heroes;
        public List<CivilizationDefinition> Civilizations;
        public List<AbilityDefinition> Abilities;
        public GlobalRulesDto GlobalRules;

        public void LoadAll()
        {
            var loader = new JsonLoader();

            string unitsPath = Path.Combine(Application.streamingAssetsPath, "Data/JSON/units.json");
            string towersPath = Path.Combine(Application.streamingAssetsPath, "Data/JSON/towers.json");
            string levelsPath = Path.Combine(Application.streamingAssetsPath, "Data/JSON/levels.json");
            string heroesPath = Path.Combine(Application.streamingAssetsPath, "Data/JSON/heroes.json");
            string civilizationsPath = Path.Combine(Application.streamingAssetsPath, "Data/JSON/civilizations.json");
            string abilitiesPath = Path.Combine(Application.streamingAssetsPath, "Data/JSON/abilities.json");

            Units = new List<UnitDefinition>();
            Spells = new List<SpellDefinition>();
            Towers = new List<TowerDefinition>();
            Traps = new List<TrapDefinition>();
            Levels = new List<LevelDefinition>();
            Heroes = new List<HeroDefinition>();
            Civilizations = new List<CivilizationDefinition>();
            Abilities = new List<AbilityDefinition>();
            GlobalRules = null;

            string unitsText = loader.LoadText(unitsPath);
            if (!string.IsNullOrWhiteSpace(unitsText))
            {
                var unitsRoot = JsonUtility.FromJson<UnitsJsonRoot>(unitsText);
                if (unitsRoot != null)
                {
                    if (unitsRoot.unit_definitions != null)
                    {
                        Units.AddRange(unitsRoot.unit_definitions);
                    }

                    if (unitsRoot.spell_definitions != null)
                    {
                        Spells.AddRange(unitsRoot.spell_definitions);
                    }
                }
            }

            string towersText = loader.LoadText(towersPath);
            if (!string.IsNullOrWhiteSpace(towersText))
            {
                var towersRoot = JsonUtility.FromJson<TowersJsonRoot>(towersText);
                if (towersRoot != null)
                {
                    if (towersRoot.tower_definitions != null)
                    {
                        Towers.AddRange(towersRoot.tower_definitions);
                    }

                    if (towersRoot.trap_definitions != null)
                    {
                        Traps.AddRange(towersRoot.trap_definitions);
                    }
                }
            }

            string levelsText = loader.LoadText(levelsPath);
            if (!string.IsNullOrWhiteSpace(levelsText))
            {
                var levelsRoot = JsonUtility.FromJson<LevelsJsonRoot>(levelsText);
                if (levelsRoot != null && levelsRoot.levels != null)
                {
                    Levels.AddRange(levelsRoot.levels);
                }

                if (levelsRoot != null)
                {
                    GlobalRules = levelsRoot.global_rules;
                }
            }

            string heroesText = loader.LoadText(heroesPath);
            if (!string.IsNullOrWhiteSpace(heroesText))
            {
                var heroesRoot = JsonUtility.FromJson<HeroesJsonRoot>(heroesText);
                if (heroesRoot != null && heroesRoot.hero_definitions != null)
                {
                    Heroes.AddRange(heroesRoot.hero_definitions);
                }
            }

            // Load Civilizations
            string civilizationsText = loader.LoadText(civilizationsPath);
            if (!string.IsNullOrWhiteSpace(civilizationsText))
            {
                var civilizationsRoot = JsonUtility.FromJson<CivilizationsJsonRoot>(civilizationsText);
                if (civilizationsRoot != null && civilizationsRoot.civilizations != null)
                {
                    Civilizations.AddRange(civilizationsRoot.civilizations);
                }
            }

            // Load Abilities
            string abilitiesText = loader.LoadText(abilitiesPath);
            if (!string.IsNullOrWhiteSpace(abilitiesText))
            {
                var abilitiesRoot = JsonUtility.FromJson<AbilitiesJsonRoot>(abilitiesText);
                if (abilitiesRoot != null && abilitiesRoot.abilities != null)
                {
                    Abilities.AddRange(abilitiesRoot.abilities);
                }
            }

            Log.Info($"JsonDatabase loaded: Units={Units.Count}, Spells={Spells.Count}, Towers={Towers.Count}, " +
                $"Traps={Traps.Count}, Levels={Levels.Count}, Heroes={Heroes.Count}, " +
                $"Civilizations={Civilizations.Count}, Abilities={Abilities.Count}.");
        }

        public UnitDefinition FindUnit(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                Log.Warning("FindUnit called with empty id.");
                return null;
            }

            for (int index = 0; index < Units.Count; index++)
            {
                if (Units[index].id == id)
                {
                    return Units[index];
                }
            }

            Log.Warning($"Unit with id '{id}' was not found.");
            return null;
        }

        /// <summary>
        /// Alias for FindUnit - for compatibility with existing code
        /// </summary>
        public UnitDefinition GetUnit(string id)
        {
            return FindUnit(id);
        }

        public SpellDefinition FindSpell(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                Log.Warning("FindSpell called with empty id.");
                return null;
            }

            for (int index = 0; index < Spells.Count; index++)
            {
                if (Spells[index].id == id)
                {
                    return Spells[index];
                }
            }

            Log.Warning($"Spell with id '{id}' was not found.");
            return null;
        }

        public TowerDefinition FindTower(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                Log.Warning("FindTower called with empty id.");
                return null;
            }

            for (int index = 0; index < Towers.Count; index++)
            {
                if (Towers[index].id == id)
                {
                    return Towers[index];
                }
            }

            Log.Warning($"Tower with id '{id}' was not found.");
            return null;
        }

        /// <summary>
        /// Alias for FindTower - for compatibility with existing code
        /// </summary>
        public TowerDefinition GetTower(string id)
        {
            return FindTower(id);
        }

        public TrapDefinition FindTrap(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                Log.Warning("FindTrap called with empty id.");
                return null;
            }

            for (int index = 0; index < Traps.Count; index++)
            {
                if (Traps[index].id == id)
                {
                    return Traps[index];
                }
            }

            Log.Warning($"Trap with id '{id}' was not found.");
            return null;
        }

        public LevelDefinition FindLevel(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                Log.Warning("FindLevel called with empty id.");
                return null;
            }

            for (int index = 0; index < Levels.Count; index++)
            {
                if (Levels[index].id == id)
                {
                    return Levels[index];
                }
            }

            Log.Warning($"Level with id '{id}' was not found.");
            return null;
        }

        public HeroDefinition FindHero(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                Log.Warning("FindHero called with empty id.");
                return null;
            }

            for (int index = 0; index < Heroes.Count; index++)
            {
                if (Heroes[index].id == id)
                {
                    return Heroes[index];
                }
            }

            Log.Warning($"Hero with id '{id}' was not found.");
            return null;
        }

        /// <summary>
        /// Alias for FindHero - for compatibility with existing code
        /// </summary>
        public HeroDefinition GetHero(string id)
        {
            return FindHero(id);
        }

        public CivilizationDefinition GetCivilization(string id)
        {
            return FindCivilization(id);
        }

        public CivilizationDefinition FindCivilization(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                Log.Warning("FindCivilization called with empty id.");
                return null;
            }

            for (int index = 0; index < Civilizations.Count; index++)
            {
                if (Civilizations[index].id == id)
                {
                    return Civilizations[index];
                }
            }

            Log.Warning($"Civilization with id '{id}' was not found.");
            return null;
        }

        public AbilityDefinition FindAbility(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                Log.Warning("FindAbility called with empty id.");
                return null;
            }

            for (int index = 0; index < Abilities.Count; index++)
            {
                if (Abilities[index].id == id)
                {
                    return Abilities[index];
                }
            }

            Log.Warning($"Ability with id '{id}' was not found.");
            return null;
        }

        public List<UnitDefinition> GetUnitsForCivilization(string civId)
        {
            var result = new List<UnitDefinition>();
            var civ = FindCivilization(civId);
            if (civ == null || civ.availableUnits == null)
            {
                return result;
            }

            foreach (string unitId in civ.availableUnits)
            {
                var unit = FindUnit(unitId);
                if (unit != null)
                {
                    result.Add(unit);
                }
            }
            return result;
        }

        public List<TowerDefinition> GetTowersForCivilization(string civId)
        {
            var result = new List<TowerDefinition>();
            var civ = FindCivilization(civId);
            if (civ == null || civ.availableTowers == null)
            {
                return result;
            }

            foreach (string towerId in civ.availableTowers)
            {
                var tower = FindTower(towerId);
                if (tower != null)
                {
                    result.Add(tower);
                }
            }
            return result;
        }

        public List<HeroDefinition> GetHeroesForCivilization(string civId)
        {
            var result = new List<HeroDefinition>();
            var civ = FindCivilization(civId);
            if (civ == null || civ.availableHeroes == null)
            {
                return result;
            }

            foreach (string heroId in civ.availableHeroes)
            {
                var hero = FindHero(heroId);
                if (hero != null)
                {
                    result.Add(hero);
                }
            }
            return result;
        }

        public CivilizationDefinition GetDefaultCivilization()
        {
            if (Civilizations != null && Civilizations.Count > 0)
            {
                // Return first unlocked civilization (unlockCost == 0)
                for (int i = 0; i < Civilizations.Count; i++)
                {
                    if (Civilizations[i].unlockCost == 0)
                    {
                        return Civilizations[i];
                    }
                }
                return Civilizations[0];
            }
            return null;
        }

        /// <summary>
        /// Get all units from the database
        /// </summary>
        public List<UnitDefinition> GetAllUnits()
        {
            return Units != null ? new List<UnitDefinition>(Units) : new List<UnitDefinition>();
        }

        /// <summary>
        /// Get all heroes from the database
        /// </summary>
        public List<HeroDefinition> GetAllHeroes()
        {
            return Heroes != null ? new List<HeroDefinition>(Heroes) : new List<HeroDefinition>();
        }

        /// <summary>
        /// Get all towers from the database
        /// </summary>
        public List<TowerDefinition> GetAllTowers()
        {
            return Towers != null ? new List<TowerDefinition>(Towers) : new List<TowerDefinition>();
        }

        /// <summary>
        /// Get all civilizations from the database
        /// </summary>
        public List<CivilizationDefinition> GetAllCivilizations()
        {
            return Civilizations != null ? new List<CivilizationDefinition>(Civilizations) : new List<CivilizationDefinition>();
        }

        /// <summary>
        /// Get all levels from the database
        /// </summary>
        public List<LevelDefinition> GetAllLevels()
        {
            if (Levels != null && Levels.Count > 0)
            {
                return new List<LevelDefinition>(Levels);
            }

            // Return default levels if none loaded
            return CreateDefaultLevels();
        }

        /// <summary>
        /// Get all traps from the database
        /// </summary>
        public List<TrapDefinition> GetAllTraps()
        {
            return Traps != null ? new List<TrapDefinition>(Traps) : new List<TrapDefinition>();
        }

        /// <summary>
        /// Get all abilities from the database
        /// </summary>
        public List<AbilityDefinition> GetAllAbilities()
        {
            return Abilities != null ? new List<AbilityDefinition>(Abilities) : new List<AbilityDefinition>();
        }

        /// <summary>
        /// Create default levels when none are loaded from JSON
        /// </summary>
        private List<LevelDefinition> CreateDefaultLevels()
        {
            var levels = new List<LevelDefinition>();

            levels.Add(new LevelDefinition
            {
                id = "lvl_01_etruria_outpost",
                display_name = "Etruria Outpost",
                description = "The first battle begins at an ancient outpost",
                startGold = 500,
                aiDifficulty = "easy",
                aiStrategy = "defensive",
                fameReward = new LevelDefinition.FameRewardDto { victory = 100, defeat = 20 }
            });

            levels.Add(new LevelDefinition
            {
                id = "lvl_02_gallic_frontier",
                display_name = "Gallic Frontier",
                description = "The Gauls have fortified the frontier",
                startGold = 500,
                aiDifficulty = "easy",
                aiStrategy = "balanced",
                fameReward = new LevelDefinition.FameRewardDto { victory = 120, defeat = 25 }
            });

            levels.Add(new LevelDefinition
            {
                id = "lvl_03_carthage_siege",
                display_name = "Carthage Siege",
                description = "Lay siege to the Carthaginian stronghold",
                startGold = 600,
                aiDifficulty = "normal",
                aiStrategy = "defensive",
                fameReward = new LevelDefinition.FameRewardDto { victory = 150, defeat = 30 }
            });

            levels.Add(new LevelDefinition
            {
                id = "lvl_04_greek_colony",
                display_name = "Greek Colony",
                description = "The Greeks defend their colony with phalanx tactics",
                startGold = 600,
                aiDifficulty = "normal",
                aiStrategy = "balanced",
                fameReward = new LevelDefinition.FameRewardDto { victory = 180, defeat = 35 }
            });

            levels.Add(new LevelDefinition
            {
                id = "lvl_05_germanic_forest",
                display_name = "Germanic Forest",
                description = "Face the fierce Germanic tribes in their home territory",
                startGold = 700,
                aiDifficulty = "hard",
                aiStrategy = "aggressive",
                fameReward = new LevelDefinition.FameRewardDto { victory = 250, defeat = 50 }
            });

            Log.Info($"[JsonDatabase] Created {levels.Count} default levels");
            return levels;
        }
    }
}
