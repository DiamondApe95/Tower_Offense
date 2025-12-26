using System.Collections.Generic;
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

            UnityEngine.Debug.Log($"JsonDatabase loaded: Units={Units.Count}, Spells={Spells.Count}, Towers={Towers.Count}, " +
                $"Traps={Traps.Count}, Levels={Levels.Count}, Heroes={Heroes.Count}, " +
                $"Civilizations={Civilizations.Count}, Abilities={Abilities.Count}.");
        }

        public UnitDefinition FindUnit(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                UnityEngine.Debug.LogWarning("FindUnit called with empty id.");
                return null;
            }

            for (int index = 0; index < Units.Count; index++)
            {
                if (Units[index].id == id)
                {
                    return Units[index];
                }
            }

            UnityEngine.Debug.LogWarning($"Unit with id '{id}' was not found.");
            return null;
        }

        public SpellDefinition FindSpell(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                UnityEngine.Debug.LogWarning("FindSpell called with empty id.");
                return null;
            }

            for (int index = 0; index < Spells.Count; index++)
            {
                if (Spells[index].id == id)
                {
                    return Spells[index];
                }
            }

            UnityEngine.Debug.LogWarning($"Spell with id '{id}' was not found.");
            return null;
        }

        public TowerDefinition FindTower(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                UnityEngine.Debug.LogWarning("FindTower called with empty id.");
                return null;
            }

            for (int index = 0; index < Towers.Count; index++)
            {
                if (Towers[index].id == id)
                {
                    return Towers[index];
                }
            }

            UnityEngine.Debug.LogWarning($"Tower with id '{id}' was not found.");
            return null;
        }

        public TrapDefinition FindTrap(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                UnityEngine.Debug.LogWarning("FindTrap called with empty id.");
                return null;
            }

            for (int index = 0; index < Traps.Count; index++)
            {
                if (Traps[index].id == id)
                {
                    return Traps[index];
                }
            }

            UnityEngine.Debug.LogWarning($"Trap with id '{id}' was not found.");
            return null;
        }

        public LevelDefinition FindLevel(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                UnityEngine.Debug.LogWarning("FindLevel called with empty id.");
                return null;
            }

            for (int index = 0; index < Levels.Count; index++)
            {
                if (Levels[index].id == id)
                {
                    return Levels[index];
                }
            }

            UnityEngine.Debug.LogWarning($"Level with id '{id}' was not found.");
            return null;
        }

        public HeroDefinition FindHero(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                UnityEngine.Debug.LogWarning("FindHero called with empty id.");
                return null;
            }

            for (int index = 0; index < Heroes.Count; index++)
            {
                if (Heroes[index].id == id)
                {
                    return Heroes[index];
                }
            }

            UnityEngine.Debug.LogWarning($"Hero with id '{id}' was not found.");
            return null;
        }

        public CivilizationDefinition FindCivilization(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                UnityEngine.Debug.LogWarning("FindCivilization called with empty id.");
                return null;
            }

            for (int index = 0; index < Civilizations.Count; index++)
            {
                if (Civilizations[index].id == id)
                {
                    return Civilizations[index];
                }
            }

            UnityEngine.Debug.LogWarning($"Civilization with id '{id}' was not found.");
            return null;
        }

        public AbilityDefinition FindAbility(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                UnityEngine.Debug.LogWarning("FindAbility called with empty id.");
                return null;
            }

            for (int index = 0; index < Abilities.Count; index++)
            {
                if (Abilities[index].id == id)
                {
                    return Abilities[index];
                }
            }

            UnityEngine.Debug.LogWarning($"Ability with id '{id}' was not found.");
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
    }
}
