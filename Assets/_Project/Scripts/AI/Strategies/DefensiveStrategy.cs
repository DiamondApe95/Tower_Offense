using UnityEngine;

namespace TowerConquest.AI
{
    /// <summary>
    /// Defensive AI strategy - focuses on building towers
    /// </summary>
    public class DefensiveStrategy : AIStrategy
    {
        private int towerCount = 0;

        public override void DecideActions()
        {
            // Defensive strategy: 30% units, 70% towers (with builders)

            if (ShouldBuildTower() && Random.value < 0.7f && towerCount < 10)
            {
                string towerId = SelectTowerToBuild();
                if (towerId != null)
                {
                    commander.GetBuildPlanner().TryBuildTower(towerId);
                    towerCount++;
                }
            }
            else if (ShouldSpawnUnit())
            {
                string unitId = SelectUnitToSpawn();
                if (unitId != null)
                {
                    commander.GetUnitSpawner().TrySpawnUnit(unitId);
                }
            }
        }
    }
}
