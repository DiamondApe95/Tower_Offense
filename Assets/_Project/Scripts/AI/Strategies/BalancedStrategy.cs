using UnityEngine;

namespace TowerConquest.AI
{
    /// <summary>
    /// Balanced AI strategy - mix of units and towers
    /// </summary>
    public class BalancedStrategy : AIStrategy
    {
        private int actionCount = 0;

        public override void DecideActions()
        {
            // Balanced strategy: 50% units, 50% towers

            actionCount++;

            if (actionCount % 2 == 0)
            {
                // Spawn unit
                if (ShouldSpawnUnit())
                {
                    string unitId = SelectUnitToSpawn();
                    if (unitId != null)
                    {
                        commander.GetUnitSpawner().TrySpawnUnit(unitId);
                    }
                }
            }
            else
            {
                // Build tower
                if (ShouldBuildTower())
                {
                    string towerId = SelectTowerToBuild();
                    if (towerId != null)
                    {
                        commander.GetBuildPlanner().TryBuildTower(towerId);
                    }
                }
            }
        }
    }
}
