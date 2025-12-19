using System.Collections.Generic;
using TowerConquest.Core;
using TowerConquest.Data;
using TowerConquest.Gameplay.Entities;
using UnityEngine;

namespace TowerConquest.Gameplay
{
    public class SpawnController
    {
        private LevelDefinition level;
        private PathManager pathManager;
        private BaseController baseController;

        public void Initialize(LevelDefinition levelDefinition, PathManager pathManagerInstance, BaseController baseTarget)
        {
            level = levelDefinition;
            pathManager = pathManagerInstance;
            baseController = baseTarget;
        }

        public UnitController SpawnUnit(string unitId)
        {
            Vector3 spawnPosition = Vector3.zero;
            IReadOnlyList<Vector3> path = null;
            if (pathManager != null)
            {
                spawnPosition = pathManager.GetSpawnPosition(level);
                path = pathManager.GetMainPath();
            }

            GameObject unitObject = null;
            if (ServiceLocator.TryGet(out PrefabRegistry registry))
            {
                unitObject = registry.CreateOrFallback(unitId);
            }

            if (unitObject == null)
            {
                unitObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                unitObject.name = $"{unitId}_Fallback";
            }

            unitObject.transform.position = spawnPosition;

            UnitController unitController = unitObject.GetComponent<UnitController>();
            if (unitController == null)
            {
                unitController = unitObject.AddComponent<UnitController>();
            }

            unitController.Initialize(unitId, path, baseController);
            return unitController;
        }

        public List<UnitController> SpawnUnitGroup(string unitId)
        {
            var spawned = new List<UnitController>();
            if (string.IsNullOrWhiteSpace(unitId))
            {
                UnityEngine.Debug.LogWarning("SpawnUnitGroup called with empty unit id.");
                return spawned;
            }

            UnitDefinition definition = ServiceLocator.Get<JsonDatabase>().FindUnit(unitId);
            int count = Mathf.Max(1, definition?.spawn?.count ?? 1);
            float spacing = Mathf.Max(0f, definition?.spawn?.spacing ?? 0.5f);

            Vector3 spawnPosition = Vector3.zero;
            IReadOnlyList<Vector3> path = null;
            if (pathManager != null)
            {
                spawnPosition = pathManager.GetSpawnPosition(level);
                path = pathManager.GetMainPath();
            }

            for (int index = 0; index < count; index++)
            {
                Vector3 offset = new Vector3(index * spacing, 0f, 0f);
                UnitController unit = SpawnUnitAtPosition(unitId, spawnPosition + offset, path);
                if (unit != null)
                {
                    spawned.Add(unit);
                }
            }

            return spawned;
        }

        private UnitController SpawnUnitAtPosition(string unitId, Vector3 position, IReadOnlyList<Vector3> path)
        {
            GameObject unitObject = null;
            if (ServiceLocator.TryGet(out PrefabRegistry registry))
            {
                unitObject = registry.CreateOrFallback(unitId);
            }

            if (unitObject == null)
            {
                unitObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                unitObject.name = $"{unitId}_Fallback";
            }

            unitObject.transform.position = position;

            UnitController unitController = unitObject.GetComponent<UnitController>();
            if (unitController == null)
            {
                unitController = unitObject.AddComponent<UnitController>();
            }

            unitController.Initialize(unitId, path, baseController);
            return unitController;
        }
    }
}
