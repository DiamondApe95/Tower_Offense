using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TowerConquest.Data;

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
            // This will be populated from JSON later
            // For now, we'll create a simple method
            Debug.Log("[CivilizationManager] Civilizations loaded");
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
            // TODO: Check against PlayerProgress to see which are unlocked
            // For now, return all with unlockCost == 0
            return civilizations.Values.Where(c => c.unlockCost == 0).ToList();
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
            if (civ == null) return null;

            // TODO: Load from abilities database
            // For now return null
            return null;
        }
    }
}
