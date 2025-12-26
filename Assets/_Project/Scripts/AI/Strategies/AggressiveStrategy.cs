using UnityEngine;

namespace TowerConquest.AI
{
    /// <summary>
    /// Aggressive AI strategy - focuses on spawning units
    /// </summary>
    public class AggressiveStrategy : AIStrategy
    {
        private int consecutiveUnitSpawns = 0;

        public override void DecideActions()
        {
            // Aggressive strategy: 80% units, 20% towers

            if (ShouldSpawnUnit() && Random.value < 0.8f)
            {
                string unitId = SelectUnitToSpawn();
                if (unitId != null)
                {
                    commander.GetUnitSpawner().TrySpawnUnit(unitId);
                    consecutiveUnitSpawns++;
                }
            }
            else if (ShouldBuildTower() && consecutiveUnitSpawns >= 5)
            {
                // Build a tower every 5 units
                string towerId = SelectTowerToBuild();
                if (towerId != null)
                {
                    commander.GetBuildPlanner().TryBuildTower(towerId);
                    consecutiveUnitSpawns = 0;
                }
            }
        }
    }
}
