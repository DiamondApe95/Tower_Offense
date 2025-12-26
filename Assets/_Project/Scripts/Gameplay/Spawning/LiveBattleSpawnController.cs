using System;
using TowerConquest.Debug;
using System.Collections.Generic;
using TowerConquest.Core;
using TowerConquest.Data;
using TowerConquest.Gameplay.Entities;
using UnityEngine;

namespace TowerConquest.Gameplay
{
    /// <summary>
    /// Spawn Controller for Live Battle mode
    /// Handles unit spawning with gold cost and cooldowns
    /// </summary>
    public class LiveBattleSpawnController : MonoBehaviour
    {
        [Header("Team Settings")]
        public GoldManager.Team team = GoldManager.Team.Player;

        [Header("Spawn Settings")]
        public Transform spawnPoint;
        public Transform targetBase;
        public float spawnCooldown = 1f;

        [Header("Hero Settings")]
        public float heroCooldown = 60f;
        public float heroRespawnTime = 30f;

        // Runtime
        public GoldManager GoldManager { get; private set; }
        public UnitDeck UnitDeck { get; private set; }
        public LiveBattleLevelController LevelController { get; private set; }

        // Cooldowns
        private float[] unitCooldowns = new float[UnitDeck.MAX_UNITS];
        private float heroCooldownTimer;
        private bool heroAlive;
        private bool heroOnCooldown;
        private HeroController currentHero; // Track hero reference for safety checks

        // Object pooling
        private readonly Dictionary<string, Queue<GameObject>> unitPools = new Dictionary<string, Queue<GameObject>>();
        private readonly Dictionary<string, GameObject> prefabCache = new Dictionary<string, GameObject>();
        private Transform poolParent;

        // Events
        public event Action<UnitController> OnUnitSpawned;
        public event Action<HeroController> OnHeroSpawned;

        private JsonDatabase database;
        private PrefabRegistry prefabRegistry;

        public void Initialize(LiveBattleLevelController controller, GoldManager goldManager, UnitDeck deck)
        {
            LevelController = controller;
            GoldManager = goldManager;
            UnitDeck = deck;

            database = ServiceLocator.Get<JsonDatabase>();
            ServiceLocator.TryGet(out prefabRegistry);

            // Reset cooldowns
            for (int i = 0; i < unitCooldowns.Length; i++)
            {
                unitCooldowns[i] = 0f;
            }
            heroCooldownTimer = 0f;
            heroAlive = false;
            heroOnCooldown = false;
            currentHero = null;

            // Setup pool parent
            if (poolParent == null)
            {
                poolParent = new GameObject($"UnitPool_{team}").transform;
                poolParent.gameObject.SetActive(false);
            }

            // Find spawn point if not set
            if (spawnPoint == null)
            {
                FindSpawnPoint();
            }

            // Find target base if not set
            if (targetBase == null)
            {
                FindTargetBase();
            }

            Log.Info($"[LiveBattleSpawnController] Initialized for {team}");
        }

        private void FindSpawnPoint()
        {
            // Look for spawn points based on team
            string searchName = team == GoldManager.Team.Player ? "PlayerSpawn" : "EnemySpawn";
            var spawns = FindObjectsByType<Transform>(FindObjectsSortMode.None);
            foreach (var spawn in spawns)
            {
                if (spawn.name.Contains(searchName) || spawn.name.Contains("SpawnPoint"))
                {
                    spawnPoint = spawn;
                    break;
                }
            }

            // Fallback to first spawn point
            if (spawnPoint == null)
            {
                var spawnPoints = GameObject.FindGameObjectsWithTag("SpawnPoint");
                if (spawnPoints.Length > 0)
                {
                    spawnPoint = spawnPoints[0].transform;
                }
            }
        }

        private void FindTargetBase()
        {
            // Find enemy base
            if (team == GoldManager.Team.Player)
            {
                targetBase = LevelController?.EnemyBase?.transform;
            }
            else
            {
                targetBase = LevelController?.PlayerBase?.transform;
            }
        }

        private void Update()
        {
            if (LevelController == null || !LevelController.IsBattleActive) return;

            // Update unit cooldowns
            for (int i = 0; i < unitCooldowns.Length; i++)
            {
                if (unitCooldowns[i] > 0)
                {
                    unitCooldowns[i] -= Time.deltaTime;
                }
            }

            // Safety check: If hero is marked alive but reference is null/destroyed, trigger cooldown
            // This handles edge cases where the hero GameObject was destroyed without firing OnHeroDied
            if (heroAlive && (currentHero == null || currentHero.gameObject == null))
            {
                Log.Warning($"[LiveBattleSpawnController] Hero was destroyed without firing death event for {team}. Triggering cooldown.");
                heroAlive = false;
                heroOnCooldown = true;
                heroCooldownTimer = heroRespawnTime;
                currentHero = null;
            }

            // Update hero cooldown
            if (heroOnCooldown && heroCooldownTimer > 0)
            {
                heroCooldownTimer -= Time.deltaTime;
                if (heroCooldownTimer <= 0)
                {
                    heroOnCooldown = false;
                }
            }
        }

        public bool CanSpawnUnit(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= UnitDeck.MAX_UNITS) return false;
            if (slotIndex >= UnitDeck.SelectedUnits.Count) return false;
            if (unitCooldowns[slotIndex] > 0) return false;

            string unitId = UnitDeck.SelectedUnits[slotIndex];
            var unitDef = database?.FindUnit(unitId);
            if (unitDef == null) return false;

            return GoldManager.CanAfford(unitDef.goldCost);
        }

        public bool TrySpawnUnit(int slotIndex)
        {
            if (!CanSpawnUnit(slotIndex)) return false;

            string unitId = UnitDeck.SelectedUnits[slotIndex];
            var unitDef = database.FindUnit(unitId);

            // Spend gold
            if (!GoldManager.SpendGold(unitDef.goldCost))
            {
                return false;
            }

            // Spawn unit
            var unit = SpawnUnit(unitId);
            if (unit != null)
            {
                // Apply cooldown
                float cooldown = unitDef.card?.cooldown_seconds ?? spawnCooldown;
                unitCooldowns[slotIndex] = cooldown;

                // Track statistics for player team
                if (team == GoldManager.Team.Player && LevelController != null)
                {
                    LevelController.RecordUnitSpawned();
                }

                OnUnitSpawned?.Invoke(unit);
                return true;
            }

            // Refund gold if spawn failed
            GoldManager.AddGold(unitDef.goldCost);
            return false;
        }

        public UnitController SpawnUnit(string unitId)
        {
            if (string.IsNullOrEmpty(unitId)) return null;

            Vector3 position = spawnPoint != null ? spawnPoint.position : transform.position;

            // Get from pool or create new
            GameObject unitObject = GetFromPool(unitId);
            if (unitObject == null)
            {
                unitObject = CreateUnitObject(unitId);
            }

            if (unitObject == null)
            {
                Log.Error($"[LiveBattleSpawnController] Failed to create unit: {unitId}");
                return null;
            }

            unitObject.transform.position = position;
            unitObject.transform.SetParent(null);
            unitObject.SetActive(true);

            // Setup UnitController
            UnitController controller = unitObject.GetComponent<UnitController>();
            if (controller == null)
            {
                controller = unitObject.AddComponent<UnitController>();
            }

            // Set team layer
            int layer = team == GoldManager.Team.Player ? LayerMask.NameToLayer("PlayerUnit") : LayerMask.NameToLayer("Enemy");
            if (layer >= 0)
            {
                SetLayerRecursively(unitObject, layer);
            }

            // Initialize with target
            var path = new List<Vector3>();
            if (targetBase != null)
            {
                path.Add(targetBase.position);
            }

            var targetBaseController = team == GoldManager.Team.Player
                ? LevelController?.EnemyBase
                : LevelController?.PlayerBase;

            controller.Initialize(unitId, path, targetBaseController);

            // Subscribe to destruction for gold reward
            controller.OnUnitDestroyed += HandleUnitDestroyed;

            Log.Info($"[LiveBattleSpawnController] Spawned unit: {unitId} for {team}");
            return controller;
        }

        private void HandleUnitDestroyed(UnitController unit)
        {
            if (unit == null) return;

            unit.OnUnitDestroyed -= HandleUnitDestroyed;

            // Award gold to opponent
            var unitDef = database?.FindUnit(unit.UnitId);
            if (unitDef != null)
            {
                int reward = unitDef.goldReward;
                if (reward > 0)
                {
                    // Give gold to opposite team
                    if (team == GoldManager.Team.Player)
                    {
                        LevelController?.AIGold?.AddGold(reward);
                    }
                    else
                    {
                        LevelController?.PlayerGold?.AddGold(reward);
                        // Track enemy kill for player
                        LevelController?.RecordEnemyKilled();
                        LevelController?.RecordGoldEarned(reward);
                    }
                }
            }

            // Return to pool
            string unitId = unit.UnitId;
            if (!string.IsNullOrEmpty(unitId))
            {
                ReturnToPool(unitId, unit.gameObject);
            }
            else
            {
                Destroy(unit.gameObject);
            }
        }

        public bool CanSpawnHero()
        {
            if (heroAlive || heroOnCooldown) return false;
            if (string.IsNullOrEmpty(UnitDeck?.SelectedHero)) return false;

            var heroDef = database?.FindHero(UnitDeck.SelectedHero);
            if (heroDef == null) return false;

            return true; // Heroes are free to spawn
        }

        public bool TrySpawnHero()
        {
            if (!CanSpawnHero()) return false;

            var hero = SpawnHero(UnitDeck.SelectedHero);
            if (hero != null)
            {
                heroAlive = true;
                currentHero = hero; // Track hero reference
                OnHeroSpawned?.Invoke(hero);
                return true;
            }

            return false;
        }

        public HeroController SpawnHero(string heroId)
        {
            if (string.IsNullOrEmpty(heroId)) return null;

            Vector3 position = spawnPoint != null ? spawnPoint.position : transform.position;

            GameObject heroObject = CreateHeroObject(heroId);
            if (heroObject == null)
            {
                Log.Error($"[LiveBattleSpawnController] Failed to create hero: {heroId}");
                return null;
            }

            heroObject.transform.position = position;

            HeroController controller = heroObject.GetComponent<HeroController>();
            if (controller == null)
            {
                controller = heroObject.AddComponent<HeroController>();
            }

            var heroDef = database.FindHero(heroId);
            float hp = heroDef?.stats?.hp ?? 500f;
            controller.Initialize(heroId, hp);

            // Subscribe to death
            controller.OnHeroDied += HandleHeroDied;

            Log.Info($"[LiveBattleSpawnController] Spawned hero: {heroId} for {team}");
            return controller;
        }

        private void HandleHeroDied(HeroController hero)
        {
            if (hero == null) return;

            hero.OnHeroDied -= HandleHeroDied;
            heroAlive = false;
            heroOnCooldown = true;
            heroCooldownTimer = heroRespawnTime;
            currentHero = null; // Clear hero reference

            Log.Info($"[LiveBattleSpawnController] Hero died for {team}. Respawn in {heroRespawnTime}s");
        }

        private GameObject CreateUnitObject(string unitId)
        {
            if (prefabRegistry != null)
            {
                return prefabRegistry.CreateOrFallback(unitId);
            }

            // Fallback
            GameObject fallback = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            fallback.name = $"{unitId}_Fallback";
            fallback.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
            return fallback;
        }

        private GameObject CreateHeroObject(string heroId)
        {
            if (prefabRegistry != null)
            {
                return prefabRegistry.CreateOrFallback(heroId);
            }

            // Fallback
            GameObject fallback = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            fallback.name = $"{heroId}_Fallback";
            fallback.transform.localScale = new Vector3(0.7f, 0.7f, 0.7f);

            var renderer = fallback.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material mat = new Material(Shader.Find("Standard"));
                mat.color = Color.yellow;
                renderer.material = mat;
            }

            return fallback;
        }

        private GameObject GetFromPool(string unitId)
        {
            if (!unitPools.TryGetValue(unitId, out Queue<GameObject> pool)) return null;

            while (pool.Count > 0)
            {
                GameObject obj = pool.Dequeue();
                if (obj != null) return obj;
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

        private void SetLayerRecursively(GameObject obj, int layer)
        {
            obj.layer = layer;
            foreach (Transform child in obj.transform)
            {
                SetLayerRecursively(child.gameObject, layer);
            }
        }

        public float GetUnitCooldown(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= unitCooldowns.Length) return 0f;
            return Mathf.Max(0f, unitCooldowns[slotIndex]);
        }

        public float GetHeroCooldown()
        {
            return heroOnCooldown ? Mathf.Max(0f, heroCooldownTimer) : 0f;
        }

        public bool IsHeroAvailable()
        {
            return !heroAlive && !heroOnCooldown;
        }

        public int GetUnitCost(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= UnitDeck.SelectedUnits.Count) return 0;
            string unitId = UnitDeck.SelectedUnits[slotIndex];
            var unitDef = database?.FindUnit(unitId);
            return unitDef?.goldCost ?? 0;
        }
    }
}
