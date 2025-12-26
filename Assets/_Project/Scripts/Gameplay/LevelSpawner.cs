using System.Collections.Generic;
using TowerConquest.Core;
using TowerConquest.Data;
using TowerConquest.Gameplay.Entities;
using UnityEngine;

namespace TowerConquest.Gameplay
{
    public class LevelSpawner : MonoBehaviour
    {
        public string levelId = "lvl_01_etruria_outpost";
        public BaseController SpawnedBase { get; private set; }

        private readonly List<Vector3> towerPositions = new List<Vector3>();
        private readonly List<float> towerRanges = new List<float>();
        private readonly List<List<Vector3>> pathLines = new List<List<Vector3>>();
        private readonly List<Vector3> spawnPositions = new List<Vector3>();
        private Vector3 basePosition;

        public void Spawn(LevelDefinition levelDefinition, GameMode mode)
        {
            if (levelDefinition == null)
            {
                UnityEngine.Debug.LogWarning("LevelSpawner.Spawn called with null level definition.");
                return;
            }

            towerPositions.Clear();
            towerRanges.Clear();
            pathLines.Clear();
            spawnPositions.Clear();

            SpawnBase(levelDefinition);
            SpawnPaths(levelDefinition);
            SpawnSpawnPoints(levelDefinition);

            // Always spawn enemy defenses (no more Defense mode with tower slots)
            SpawnEnemyDefenses(levelDefinition);
        }

        private void SpawnBase(LevelDefinition levelDefinition)
        {
            basePosition = Vector3.zero;
            if (levelDefinition.@base == null || levelDefinition.@base.position == null)
            {
                UnityEngine.Debug.LogWarning("LevelSpawner.SpawnBase missing base definition.");
                return;
            }

            LevelDefinition.PositionDto position = levelDefinition.@base.position;
            basePosition = new Vector3(position.x, position.y, position.z);

            GameObject baseObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            baseObject.name = $"Base_{levelDefinition.@base.id}";
            baseObject.transform.position = basePosition;
            baseObject.transform.localScale = new Vector3(1.5f, 1.2f, 1.5f);

            BaseController baseController = baseObject.GetComponent<BaseController>();
            if (baseController == null)
            {
                baseController = baseObject.AddComponent<BaseController>();
            }

            baseController.Initialize(levelDefinition.@base.hp, levelDefinition.@base.armor);
            SpawnedBase = baseController;
        }

        private void SpawnPaths(LevelDefinition levelDefinition)
        {
            if (levelDefinition.paths == null)
            {
                return;
            }

            foreach (LevelDefinition.PathDto path in levelDefinition.paths)
            {
                if (path?.waypoints == null)
                {
                    continue;
                }

                var points = new List<Vector3>();
                foreach (LevelDefinition.PositionDto waypoint in path.waypoints)
                {
                    if (waypoint == null)
                    {
                        continue;
                    }

                    points.Add(new Vector3(waypoint.x, waypoint.y, waypoint.z));
                }

                pathLines.Add(points);
            }
        }

        private void SpawnSpawnPoints(LevelDefinition levelDefinition)
        {
            if (levelDefinition.spawn_points == null)
            {
                return;
            }

            foreach (LevelDefinition.SpawnPointDto spawn in levelDefinition.spawn_points)
            {
                if (spawn?.position == null)
                {
                    continue;
                }

                Vector3 position = new Vector3(spawn.position.x, spawn.position.y, spawn.position.z);
                spawnPositions.Add(position);

                GameObject spawnObject = new GameObject($"SpawnPoint_{spawn.id}");
                spawnObject.transform.position = position;
            }
        }

        private void SpawnEnemyDefenses(LevelDefinition levelDefinition)
        {
            if (levelDefinition.enemy_defenses == null)
            {
                return;
            }

            if (levelDefinition.enemy_defenses.towers != null)
            {
                foreach (LevelDefinition.TowerPlacementDto placement in levelDefinition.enemy_defenses.towers)
                {
                    SpawnTower(placement);
                }
            }

            if (levelDefinition.enemy_defenses.traps != null)
            {
                foreach (LevelDefinition.TrapPlacementDto placement in levelDefinition.enemy_defenses.traps)
                {
                    SpawnTrap(placement);
                }
            }
        }

        private void SpawnTower(LevelDefinition.TowerPlacementDto placement)
        {
            if (placement == null)
            {
                return;
            }

            GameObject towerObject = CreateTowerObject(placement.tower_id);
            towerObject.name = $"Tower_{placement.instance_id}";
            towerObject.transform.position = new Vector3(placement.position.x, placement.position.y, placement.position.z);
            towerObject.transform.rotation = Quaternion.Euler(0f, placement.rotation_y_degrees, 0f);

            TowerController towerController = towerObject.GetComponent<TowerController>();
            if (towerController == null)
            {
                towerController = towerObject.AddComponent<TowerController>();
            }

            ConfigureTower(towerController, placement.tower_id, placement.tier);
        }


        private void SpawnTrap(LevelDefinition.TrapPlacementDto placement)
        {
            if (placement == null)
            {
                return;
            }

            GameObject trapObject = CreateTrapObject(placement.trap_id);
            trapObject.name = $"Trap_{placement.instance_id}";
            trapObject.transform.position = new Vector3(placement.position.x, placement.position.y, placement.position.z);

            TrapController trapController = trapObject.GetComponent<TrapController>();
            if (trapController == null)
            {
                trapController = trapObject.AddComponent<TrapController>();
            }

            Vector3 size = Vector3.one;
            if (placement.size != null)
            {
                size = new Vector3(Mathf.Max(0.5f, placement.size.x), 0.25f, Mathf.Max(0.5f, placement.size.z));
            }

            trapController.Initialize(placement.trap_id, size);
        }

        private GameObject CreateTowerObject(string towerId)
        {
            GameObject towerObject = null;
            if (ServiceLocator.TryGet(out PrefabRegistry registry))
            {
                towerObject = registry.CreateOrFallback(towerId);
            }

            if (towerObject == null)
            {
                towerObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                towerObject.name = $"{towerId}_Fallback";
            }

            return towerObject;
        }

        private GameObject CreateTrapObject(string trapId)
        {
            GameObject trapObject = null;
            if (ServiceLocator.TryGet(out PrefabRegistry registry))
            {
                trapObject = registry.CreateOrFallback(trapId);
            }

            if (trapObject == null)
            {
                trapObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                trapObject.name = $"{trapId}_Fallback";
            }

            return trapObject;
        }

        private void ConfigureTower(TowerController towerController, string towerId, int tier)
        {
            JsonDatabase database = ServiceLocator.Get<JsonDatabase>();
            TowerDefinition towerDefinition = database.FindTower(towerId);
            TowerDefinition.TierDto tierDefinition = GetTowerTier(towerDefinition, tier);

            if (tierDefinition?.attack != null)
            {
                towerController.range = tierDefinition.attack.range > 0f ? tierDefinition.attack.range : towerController.range;
                towerController.damage = tierDefinition.attack.base_damage > 0f ? tierDefinition.attack.base_damage : towerController.damage;
                towerController.attacksPerSecond = tierDefinition.attack.attacks_per_second > 0f ? tierDefinition.attack.attacks_per_second : towerController.attacksPerSecond;
            }

            towerController.effects = tierDefinition?.effects;
            towerController.UpdateDpsCache();

            towerPositions.Add(towerController.transform.position);
            towerRanges.Add(towerController.range);
        }

        private static TowerDefinition.TierDto GetTowerTier(TowerDefinition towerDefinition, int requestedTier)
        {
            if (towerDefinition == null || towerDefinition.tiers == null || towerDefinition.tiers.Length == 0)
            {
                return null;
            }

            foreach (TowerDefinition.TierDto tier in towerDefinition.tiers)
            {
                if (tier != null && tier.tier == requestedTier)
                {
                    return tier;
                }
            }

            return towerDefinition.tiers[0];
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.yellow;
            foreach (List<Vector3> path in pathLines)
            {
                if (path == null || path.Count < 2)
                {
                    continue;
                }

                for (int index = 0; index < path.Count - 1; index++)
                {
                    Gizmos.DrawLine(path[index], path[index + 1]);
                }
            }

            Gizmos.color = Color.cyan;
            foreach (Vector3 spawn in spawnPositions)
            {
                Gizmos.DrawSphere(spawn, 0.4f);
            }

            Gizmos.color = Color.red;
            Gizmos.DrawSphere(basePosition, 0.6f);

            Gizmos.color = new Color(0f, 1f, 0f, 0.25f);
            for (int index = 0; index < towerPositions.Count; index++)
            {
                if (index < towerRanges.Count)
                {
                    Gizmos.DrawWireSphere(towerPositions[index], towerRanges[index]);
                }
            }
        }
    }
}
