using System;
using TowerConquest.Debug;
using UnityEngine;
using TowerConquest.Core;
using TowerConquest.Data;
using TowerConquest.Gameplay.Entities;

namespace TowerConquest.Gameplay
{
    /// <summary>
    /// Manages hero spawning and cooldowns
    /// </summary>
    public class HeroManager : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private string heroId;
        [SerializeField] private GoldManager.Team ownerTeam;

        [Header("Runtime")]
        [SerializeField] private GameObject currentHero;
        [SerializeField] private float cooldownDuration;
        [SerializeField] private float lastSpawnTime = -999f;
        [SerializeField] private bool isOnCooldown;

        private JsonDatabase database;
        private PrefabRegistry prefabRegistry;
        private Transform spawnPoint;

        public string HeroID => heroId;
        public bool CanSpawn => !isOnCooldown && currentHero == null;
        public float CooldownRemaining => Mathf.Max(0, (lastSpawnTime + cooldownDuration) - Time.time);

        public event Action<GameObject> OnHeroSpawned;
        public event Action OnHeroDied;

        public void Initialize(string hId, GoldManager.Team team, JsonDatabase db, Transform spawn)
        {
            heroId = hId;
            ownerTeam = team;
            database = db;
            spawnPoint = spawn;

            // Try to get PrefabRegistry
            ServiceLocator.TryGet(out prefabRegistry);

            var heroDef = database.GetHero(heroId);
            if (heroDef != null)
            {
                cooldownDuration = heroDef.spawnCooldown > 0 ? heroDef.spawnCooldown : 120f;
            }
            else
            {
                cooldownDuration = 120f; // Default 2 minutes
            }

            Log.Info($"[HeroManager] Initialized hero {heroId} for team {team}, cooldown: {cooldownDuration}s");
        }

        private void Update()
        {
            // Update cooldown status
            if (isOnCooldown && Time.time >= lastSpawnTime + cooldownDuration)
            {
                isOnCooldown = false;
                Log.Info($"[HeroManager] Hero {heroId} ready to spawn");
            }
        }

        /// <summary>
        /// Spawn the hero
        /// </summary>
        public GameObject SpawnHero()
        {
            if (!CanSpawn)
            {
                Log.Warning($"[HeroManager] Cannot spawn hero (on cooldown or already alive)");
                return null;
            }

            var heroDef = database.GetHero(heroId);
            if (heroDef == null)
            {
                Log.Error($"[HeroManager] Hero definition not found: {heroId}");
                return null;
            }

            // Try to load from prefab registry first
            if (prefabRegistry != null)
            {
                currentHero = prefabRegistry.CreateOrFallback(heroId);
            }

            // Fallback: Create default hero object
            if (currentHero == null)
            {
                currentHero = CreateDefaultHero(heroDef);
            }

            currentHero.name = $"Hero_{heroId}_{ownerTeam}";
            currentHero.transform.position = spawnPoint != null ? spawnPoint.position : Vector3.zero;

            // Setup HeroController
            var heroController = currentHero.GetComponent<HeroController>();
            if (heroController == null)
            {
                heroController = currentHero.AddComponent<HeroController>();
            }

            // Initialize hero with stats from definition
            float hp = heroDef.stats?.hp ?? 500f;
            heroController.Initialize(heroId, hp);

            // Subscribe to death event
            heroController.OnHeroDied += HandleHeroDeath;

            // Set team layer
            int layer = ownerTeam == GoldManager.Team.Player
                ? LayerMask.NameToLayer("PlayerUnit")
                : LayerMask.NameToLayer("Enemy");
            if (layer >= 0)
            {
                SetLayerRecursively(currentHero, layer);
            }

            lastSpawnTime = Time.time;
            isOnCooldown = true;

            OnHeroSpawned?.Invoke(currentHero);

            Log.Info($"[HeroManager] Spawned hero {heroId} at {currentHero.transform.position}");
            return currentHero;
        }

        private void HandleHeroDeath(HeroController hero)
        {
            if (hero != null)
            {
                hero.OnHeroDied -= HandleHeroDeath;
            }
            OnHeroKilled();
        }

        /// <summary>
        /// Create a default hero object when no prefab is available
        /// </summary>
        private GameObject CreateDefaultHero(HeroDefinition heroDef)
        {
            GameObject heroObj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            heroObj.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);

            var renderer = heroObj.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material mat = new Material(Shader.Find("Standard"));
                // Heroes get a golden color
                mat.color = new Color(1f, 0.85f, 0.2f, 1f);
                renderer.material = mat;
            }

            // Add NavMeshAgent for movement
            var agent = heroObj.AddComponent<UnityEngine.AI.NavMeshAgent>();
            agent.speed = heroDef.stats?.speed ?? 4f;
            agent.stoppingDistance = 1.5f;

            return heroObj;
        }

        /// <summary>
        /// Recursively set layer on object and all children
        /// </summary>
        private void SetLayerRecursively(GameObject obj, int layer)
        {
            obj.layer = layer;
            foreach (Transform child in obj.transform)
            {
                SetLayerRecursively(child.gameObject, layer);
            }
        }

        /// <summary>
        /// Called when hero dies
        /// </summary>
        public void OnHeroKilled()
        {
            if (currentHero == null) return;

            Log.Info($"[HeroManager] Hero {heroId} killed");

            currentHero = null;
            OnHeroDied?.Invoke();

            // Cooldown already started when spawned, so hero can be re-spawned after cooldown
        }

        /// <summary>
        /// Get remaining cooldown time
        /// </summary>
        public float GetCooldownRemaining()
        {
            return CooldownRemaining;
        }

        /// <summary>
        /// Check if hero is currently alive
        /// </summary>
        public bool IsHeroAlive()
        {
            return currentHero != null;
        }

        #if UNITY_EDITOR
        [ContextMenu("Spawn Hero (Debug)")]
        private void DebugSpawnHero()
        {
            SpawnHero();
        }

        [ContextMenu("Kill Hero (Debug)")]
        private void DebugKillHero()
        {
            OnHeroKilled();
        }
        #endif
    }
}
