using System.Collections.Generic;
using TowerOffense.Core;
using TowerOffense.Data;
using TowerOffense.Gameplay.Entities;
using UnityEngine;

namespace TowerOffense.Gameplay
{
    public class SpawnController
    {
        private LevelDefinition level;
        private PathManager pathManager;

        public void Initialize(LevelDefinition levelDefinition, PathManager pathManagerInstance)
        {
            level = levelDefinition;
            pathManager = pathManagerInstance;
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

            unitController.Initialize(unitId, path);
            return unitController;
        }
    }
}
