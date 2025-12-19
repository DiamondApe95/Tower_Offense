using System.Collections.Generic;
using TowerOffense.Data;
using UnityEngine;

namespace TowerOffense.Gameplay
{
    public class PathManager
    {
        private readonly List<Vector3> mainPath = new List<Vector3>();

        public void InitializeFromLevel(LevelDefinition level)
        {
            mainPath.Clear();
            if (level == null || level.paths == null || level.paths.Length == 0)
            {
                return;
            }

            LevelDefinition.PathDto path = level.paths[0];
            if (path.waypoints == null)
            {
                return;
            }

            for (int index = 0; index < path.waypoints.Length; index++)
            {
                LevelDefinition.PositionDto waypoint = path.waypoints[index];
                mainPath.Add(new Vector3(waypoint.x, waypoint.y, waypoint.z));
            }
        }

        public IReadOnlyList<Vector3> GetMainPath()
        {
            return mainPath;
        }

        public Vector3 GetSpawnPosition(LevelDefinition level)
        {
            if (level == null || level.spawn_points == null || level.spawn_points.Length == 0)
            {
                return Vector3.zero;
            }

            LevelDefinition.PositionDto spawnPosition = level.spawn_points[0].position;
            return new Vector3(spawnPosition.x, spawnPosition.y, spawnPosition.z);
        }
    }
}
