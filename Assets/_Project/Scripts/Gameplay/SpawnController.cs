using System;
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

        // Object Pooling
        private readonly Dictionary<string, Queue<GameObject>> unitPools = new Dictionary<string, Queue<GameObject>>();
        private readonly Dictionary<string, GameObject> prefabCache = new Dictionary<string, GameObject>();
        private Transform poolParent;
        private int defaultPoolSize = 10;

        // Statistics
        public int TotalSpawned { get; private set; }
        public int ActiveUnits { get; private set; }
        public int PooledUnits => GetTotalPooledCount();

        // Events
        public event Action<UnitController> OnUnitSpawned;
        public event Action<UnitController> OnUnitDespawned;

        public void Initialize(LevelDefinition levelDefinition, PathManager pathManagerInstance, BaseController baseTarget)
        {
            level = levelDefinition;
            pathManager = pathManagerInstance;
            baseController = baseTarget;

            // Pool Parent für saubere Hierarchie
            if (poolParent == null)
            {
                poolParent = new GameObject("UnitPool").transform;
                poolParent.gameObject.SetActive(false);
            }
        }

        public void PrewarmPool(string unitId, int count)
        {
            if (string.IsNullOrWhiteSpace(unitId)) return;

            for (int i = 0; i < count; i++)
            {
                GameObject unit = CreateNewUnitObject(unitId);
                if (unit != null)
                {
                    ReturnToPool(unitId, unit);
                }
            }

            UnityEngine.Debug.Log($"SpawnController: Prewarmed pool for {unitId} with {count} units.");
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

            return SpawnUnitAtPosition(unitId, spawnPosition, path);
        }

        public List<UnitController> SpawnUnitGroup(string unitId)
        {
            var spawned = new List<UnitController>();
            if (string.IsNullOrWhiteSpace(unitId))
            {
                UnityEngine.Debug.LogWarning("SpawnUnitGroup called with empty unit id.");
                return spawned;
            }

            UnitDefinition definition = ServiceLocator.Get<JsonDatabase>()?.FindUnit(unitId);
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

        public UnitController SpawnUnitAtPosition(string unitId, Vector3 position, IReadOnlyList<Vector3> path)
        {
            GameObject unitObject = GetFromPool(unitId);

            if (unitObject == null)
            {
                unitObject = CreateNewUnitObject(unitId);
            }

            if (unitObject == null)
            {
                UnityEngine.Debug.LogError($"SpawnController: Failed to create unit {unitId}");
                return null;
            }

            unitObject.transform.position = position;
            unitObject.transform.SetParent(null);
            unitObject.SetActive(true);

            UnitController unitController = unitObject.GetComponent<UnitController>();
            if (unitController == null)
            {
                unitController = unitObject.AddComponent<UnitController>();
            }

            unitController.Initialize(unitId, path, baseController);

            // Event für Despawn registrieren
            unitController.OnUnitDestroyed -= HandleUnitDestroyed;
            unitController.OnUnitDestroyed += HandleUnitDestroyed;

            TotalSpawned++;
            ActiveUnits++;
            OnUnitSpawned?.Invoke(unitController);

            return unitController;
        }

        private GameObject CreateNewUnitObject(string unitId)
        {
            GameObject unitObject = null;

            // Versuche aus Prefab Cache
            if (!prefabCache.TryGetValue(unitId, out GameObject prefab))
            {
                if (ServiceLocator.TryGet(out PrefabRegistry registry))
                {
                    prefab = registry.GetPrefab(unitId);
                    if (prefab != null)
                    {
                        prefabCache[unitId] = prefab;
                    }
                }
            }

            if (prefab != null)
            {
                unitObject = UnityEngine.Object.Instantiate(prefab);
            }
            else if (ServiceLocator.TryGet(out PrefabRegistry registry))
            {
                unitObject = registry.CreateOrFallback(unitId);
            }

            if (unitObject == null)
            {
                unitObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                unitObject.name = $"{unitId}_Fallback";
            }

            return unitObject;
        }

        private GameObject GetFromPool(string unitId)
        {
            if (!unitPools.TryGetValue(unitId, out Queue<GameObject> pool))
            {
                return null;
            }

            while (pool.Count > 0)
            {
                GameObject obj = pool.Dequeue();
                if (obj != null)
                {
                    return obj;
                }
            }

            return null;
        }

        private void ReturnToPool(string unitId, GameObject unit)
        {
            if (unit == null) return;

            if (!unitPools.TryGetValue(unitId, out Queue<GameObject> pool))
            {
                pool = new Queue<GameObject>();
                unitPools[unitId] = pool;
            }

            unit.SetActive(false);
            if (poolParent != null)
            {
                unit.transform.SetParent(poolParent);
            }
            pool.Enqueue(unit);
        }

        private void HandleUnitDestroyed(UnitController unit)
        {
            if (unit == null) return;

            ActiveUnits = Mathf.Max(0, ActiveUnits - 1);
            OnUnitDespawned?.Invoke(unit);

            // Zurück in Pool statt Destroy
            string unitId = unit.UnitId;
            if (!string.IsNullOrWhiteSpace(unitId))
            {
                ReturnToPool(unitId, unit.gameObject);
            }
            else
            {
                UnityEngine.Object.Destroy(unit.gameObject);
            }
        }

        public void DespawnUnit(UnitController unit)
        {
            if (unit == null) return;

            unit.OnUnitDestroyed -= HandleUnitDestroyed;
            HandleUnitDestroyed(unit);
        }

        public void DespawnAllUnits()
        {
            UnitController[] allUnits = UnityEngine.Object.FindObjectsByType<UnitController>(FindObjectsSortMode.None);
            foreach (UnitController unit in allUnits)
            {
                DespawnUnit(unit);
            }
        }

        public void ClearPools()
        {
            foreach (var pool in unitPools.Values)
            {
                while (pool.Count > 0)
                {
                    GameObject obj = pool.Dequeue();
                    if (obj != null)
                    {
                        UnityEngine.Object.Destroy(obj);
                    }
                }
            }
            unitPools.Clear();
            prefabCache.Clear();

            if (poolParent != null)
            {
                UnityEngine.Object.Destroy(poolParent.gameObject);
                poolParent = null;
            }
        }

        private int GetTotalPooledCount()
        {
            int total = 0;
            foreach (var pool in unitPools.Values)
            {
                total += pool.Count;
            }
            return total;
        }

        public void ResetStatistics()
        {
            TotalSpawned = 0;
            ActiveUnits = 0;
        }
    }
}
