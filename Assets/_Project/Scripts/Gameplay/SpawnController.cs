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
            if (level == null)
            {
                Debug.LogWarning("SpawnUnit called before Initialize.");
                return null;
            }

            PrefabRegistry prefabRegistry = ServiceLocator.Get<PrefabRegistry>();
            GameObject unitObject = prefabRegistry.CreateOrFallback(unitId, out bool usedFallback);
            if (usedFallback || unitObject == null)
            {
                unitObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            }

            unitObject.name = $"{unitId}_Unit";
            unitObject.transform.position = pathManager.GetSpawnPosition(level);

            UnitController controller = unitObject.GetComponent<UnitController>();
            if (controller == null)
            {
                controller = unitObject.AddComponent<UnitController>();
            }

            controller.Initialize(unitId, pathManager.GetMainPath());
            return controller;
        }
    }
}
