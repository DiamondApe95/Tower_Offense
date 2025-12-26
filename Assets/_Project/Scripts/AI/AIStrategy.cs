using UnityEngine;
using TowerConquest.Gameplay;
using TowerConquest.Data;

namespace TowerConquest.AI
{
    /// <summary>
    /// Base class for AI strategies
    /// </summary>
    public abstract class AIStrategy
    {
        protected AICommander commander;
        protected GoldManager goldManager;
        protected ConstructionManager constructionManager;
        protected JsonDatabase database;
        protected UnitDeck aiDeck;

        public virtual void Initialize(AICommander cmd, GoldManager gold, ConstructionManager construction, JsonDatabase db, UnitDeck deck)
        {
            commander = cmd;
            goldManager = gold;
            constructionManager = construction;
            database = db;
            aiDeck = deck;
        }

        public abstract void DecideActions();

        protected bool ShouldSpawnUnit()
        {
            // Base logic: spawn if we have enough gold
            int cheapestUnitCost = GetCheapestUnitCost();
            return goldManager.CanAfford(cheapestUnitCost);
        }

        protected bool ShouldBuildTower()
        {
            // Base logic: build if we have enough gold
            int cheapestTowerCost = GetCheapestTowerCost();
            return goldManager.CanAfford(cheapestTowerCost);
        }

        protected string SelectUnitToSpawn()
        {
            // Simple selection: pick random unit from deck
            if (aiDeck == null || aiDeck.SelectedUnits.Count == 0)
                return null;

            // Filter affordable units
            var affordableUnits = aiDeck.SelectedUnits.FindAll(unitId =>
            {
                var unitDef = database.GetUnit(unitId);
                return unitDef != null && goldManager.CanAfford(unitDef.goldCost);
            });

            if (affordableUnits.Count == 0)
                return null;

            return affordableUnits[Random.Range(0, affordableUnits.Count)];
        }

        protected string SelectTowerToBuild()
        {
            // Get towers from civilization
            var civ = database.GetCivilization(aiDeck.CivilizationID);
            if (civ == null || civ.availableTowers == null || civ.availableTowers.Length == 0)
                return null;

            // Filter affordable towers
            var affordableTowers = System.Array.FindAll(civ.availableTowers, towerId =>
            {
                var towerDef = database.GetTower(towerId);
                return towerDef != null && goldManager.CanAfford(towerDef.goldCost);
            });

            if (affordableTowers.Length == 0)
                return null;

            return affordableTowers[Random.Range(0, affordableTowers.Length)];
        }

        private int GetCheapestUnitCost()
        {
            int cheapest = int.MaxValue;
            foreach (var unitId in aiDeck.SelectedUnits)
            {
                var unitDef = database.GetUnit(unitId);
                if (unitDef != null && unitDef.goldCost < cheapest)
                {
                    cheapest = unitDef.goldCost;
                }
            }
            return cheapest == int.MaxValue ? 100 : cheapest;
        }

        private int GetCheapestTowerCost()
        {
            var civ = database.GetCivilization(aiDeck.CivilizationID);
            if (civ == null) return 150;

            int cheapest = int.MaxValue;
            foreach (var towerId in civ.availableTowers)
            {
                var towerDef = database.GetTower(towerId);
                if (towerDef != null && towerDef.goldCost < cheapest)
                {
                    cheapest = towerDef.goldCost;
                }
            }
            return cheapest == int.MaxValue ? 150 : cheapest;
        }
    }
}
