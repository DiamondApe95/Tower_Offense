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
        public GlobalRulesDto GlobalRules;

        public void LoadAll()
        {
            var loader = new JsonLoader();

            string unitsPath = Path.Combine(Application.streamingAssetsPath, "Data/JSON/units.json");
            string towersPath = Path.Combine(Application.streamingAssetsPath, "Data/JSON/towers.json");
            string levelsPath = Path.Combine(Application.streamingAssetsPath, "Data/JSON/levels.json");
            string heroesPath = Path.Combine(Application.streamingAssetsPath, "Data/JSON/heroes.json");

            Units = new List<UnitDefinition>();
            Spells = new List<SpellDefinition>();
            Towers = new List<TowerDefinition>();
            Traps = new List<TrapDefinition>();
            Levels = new List<LevelDefinition>();
            Heroes = new List<HeroDefinition>();
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

            Debug.Log($"JsonDatabase loaded: Units={Units.Count}, Spells={Spells.Count}, Towers={Towers.Count}, Traps={Traps.Count}, Levels={Levels.Count}.");
        }

        public UnitDefinition FindUnit(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                Debug.LogWarning("FindUnit called with empty id.");
                return null;
            }

            for (int index = 0; index < Units.Count; index++)
            {
                if (Units[index].id == id)
                {
                    return Units[index];
                }
            }

            Debug.LogWarning($"Unit with id '{id}' was not found.");
            return null;
        }

        public SpellDefinition FindSpell(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                Debug.LogWarning("FindSpell called with empty id.");
                return null;
            }

            for (int index = 0; index < Spells.Count; index++)
            {
                if (Spells[index].id == id)
                {
                    return Spells[index];
                }
            }

            Debug.LogWarning($"Spell with id '{id}' was not found.");
            return null;
        }

        public TowerDefinition FindTower(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                Debug.LogWarning("FindTower called with empty id.");
                return null;
            }

            for (int index = 0; index < Towers.Count; index++)
            {
                if (Towers[index].id == id)
                {
                    return Towers[index];
                }
            }

            Debug.LogWarning($"Tower with id '{id}' was not found.");
            return null;
        }

        public TrapDefinition FindTrap(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                Debug.LogWarning("FindTrap called with empty id.");
                return null;
            }

            for (int index = 0; index < Traps.Count; index++)
            {
                if (Traps[index].id == id)
                {
                    return Traps[index];
                }
            }

            Debug.LogWarning($"Trap with id '{id}' was not found.");
            return null;
        }

        public LevelDefinition FindLevel(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                Debug.LogWarning("FindLevel called with empty id.");
                return null;
            }

            for (int index = 0; index < Levels.Count; index++)
            {
                if (Levels[index].id == id)
                {
                    return Levels[index];
                }
            }

            Debug.LogWarning($"Level with id '{id}' was not found.");
            return null;
        }

        public HeroDefinition FindHero(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                Debug.LogWarning("FindHero called with empty id.");
                return null;
            }

            for (int index = 0; index < Heroes.Count; index++)
            {
                if (Heroes[index].id == id)
                {
                    return Heroes[index];
                }
            }

            Debug.LogWarning($"Hero with id '{id}' was not found.");
            return null;
        }
    }
}
