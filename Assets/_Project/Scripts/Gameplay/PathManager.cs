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
                Debug.LogWarning("PathManager.InitializeFromLevel called with no paths.");
                return;
            }

            LevelDefinition.PathDto pathDto = level.paths[0];
            if (pathDto == null || pathDto.waypoints == null)
            {
                Debug.LogWarning("PathManager.InitializeFromLevel called with empty waypoints.");
                return;
            }

            foreach (LevelDefinition.PositionDto waypoint in pathDto.waypoints)
            {
                if (waypoint == null)
                {
                    continue;
                }

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
                Debug.LogWarning("PathManager.GetSpawnPosition called with no spawn points.");
                return Vector3.zero;
            }

            LevelDefinition.PositionDto spawn = level.spawn_points[0]?.position;
            if (spawn == null)
            {
                Debug.LogWarning("PathManager.GetSpawnPosition called with missing spawn position.");
                return Vector3.zero;
            }

            return new Vector3(spawn.x, spawn.y, spawn.z);
        }
    }
}
