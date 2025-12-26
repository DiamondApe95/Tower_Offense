using System;
using UnityEngine;
using TowerConquest.Data;

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

            var heroDef = database.GetHero(heroId);
            if (heroDef != null)
            {
                cooldownDuration = heroDef.spawnCooldown;
            }
            else
            {
                cooldownDuration = 120f; // Default 2 minutes
            }

            Debug.Log($"[HeroManager] Initialized hero {heroId} for team {team}, cooldown: {cooldownDuration}s");
        }

        private void Update()
        {
            // Update cooldown status
            if (isOnCooldown && Time.time >= lastSpawnTime + cooldownDuration)
            {
                isOnCooldown = false;
                Debug.Log($"[HeroManager] Hero {heroId} ready to spawn");
            }
        }

        /// <summary>
        /// Spawn the hero
        /// </summary>
        public GameObject SpawnHero()
        {
            if (!CanSpawn)
            {
                Debug.LogWarning($"[HeroManager] Cannot spawn hero (on cooldown or already alive)");
                return null;
            }

            var heroDef = database.GetHero(heroId);
            if (heroDef == null)
            {
                Debug.LogError($"[HeroManager] Hero definition not found: {heroId}");
                return null;
            }

            // TODO: Load hero prefab and spawn
            // For now, create empty game object as placeholder
            currentHero = new GameObject($"Hero_{heroId}_{ownerTeam}");
            currentHero.transform.position = spawnPoint != null ? spawnPoint.position : Vector3.zero;

            // TODO: Initialize hero controller
            // var heroController = currentHero.AddComponent<HeroController>();
            // heroController.Initialize(heroDef, ownerTeam);

            lastSpawnTime = Time.time;
            isOnCooldown = true;

            OnHeroSpawned?.Invoke(currentHero);

            Debug.Log($"[HeroManager] Spawned hero {heroId} at {currentHero.transform.position}");
            return currentHero;
        }

        /// <summary>
        /// Called when hero dies
        /// </summary>
        public void OnHeroKilled()
        {
            if (currentHero == null) return;

            Debug.Log($"[HeroManager] Hero {heroId} killed");

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
