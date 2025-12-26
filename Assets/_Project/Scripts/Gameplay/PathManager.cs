using System.Collections.Generic;
using TowerConquest.Debug;
using TowerConquest.Data;
using UnityEngine;

namespace TowerConquest.Gameplay
{
    public class PathManager
    {
        private readonly List<PathData> paths = new List<PathData>();
        private readonly List<LevelDefinition.SpawnPointDto> spawnPoints = new List<LevelDefinition.SpawnPointDto>();

        public void InitializeFromLevel(LevelDefinition level)
        {
            paths.Clear();
            spawnPoints.Clear();
            if (level == null)
            {
                Log.Warning("PathManager.InitializeFromLevel called with null level.");
                return;
            }

            if (level.spawn_points != null)
            {
                spawnPoints.AddRange(level.spawn_points);
            }

            if (level.paths == null || level.paths.Length == 0)
            {
                Log.Warning("PathManager.InitializeFromLevel called with no paths.");
                return;
            }

            foreach (LevelDefinition.PathDto pathDto in level.paths)
            {
                if (pathDto == null || pathDto.waypoints == null)
                {
                    continue;
                }

                var waypoints = new List<Vector3>();
                foreach (LevelDefinition.PositionDto waypoint in pathDto.waypoints)
                {
                    if (waypoint == null)
                    {
                        continue;
                    }

                    waypoints.Add(new Vector3(waypoint.x, waypoint.y, waypoint.z));
                }

                paths.Add(new PathData(pathDto.id, pathDto.from_spawn_id, waypoints));
            }
        }

        public IReadOnlyList<Vector3> GetMainPath()
        {
            if (paths.Count == 0)
            {
                return new List<Vector3>();
            }

            return paths[0].waypoints;
        }

        public Vector3 GetSpawnPosition(LevelDefinition level)
        {
            if (spawnPoints.Count == 0)
            {
                Log.Warning("PathManager.GetSpawnPosition called with no spawn points.");
                return Vector3.zero;
            }

            LevelDefinition.PositionDto spawn = spawnPoints[0]?.position;
            if (spawn == null)
            {
                Log.Warning("PathManager.GetSpawnPosition called with missing spawn position.");
                return Vector3.zero;
            }

            return new Vector3(spawn.x, spawn.y, spawn.z);
        }

        public IReadOnlyList<PathData> GetPaths()
        {
            return paths;
        }

        public IReadOnlyList<LevelDefinition.SpawnPointDto> GetSpawnPoints()
        {
            return spawnPoints;
        }

        public readonly struct PathData
        {
            public readonly string id;
            public readonly string spawnId;
            public readonly List<Vector3> waypoints;

            public PathData(string id, string spawnId, List<Vector3> waypoints)
            {
                this.id = id;
                this.spawnId = spawnId;
                this.waypoints = waypoints ?? new List<Vector3>();
            }
        }
    }
}
