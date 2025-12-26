using System;
using System.Collections.Generic;
using TowerConquest.Core;
using TowerConquest.Data;
using TowerConquest.Gameplay.Entities;
using TowerConquest.Saving;
using TowerConquest.UI;
using UnityEngine;

namespace TowerConquest.Gameplay
{
    /// <summary>
    /// Live Battle Level Controller - Real-time RTS/Tower Defense Hybrid
    /// Manages the game flow without wave phases - continuous real-time battle
    /// </summary>
    public class LiveBattleLevelController : MonoBehaviour
    {
        [Header("Level Settings")]
        public string levelId = "lvl_01";

        [Header("UI References")]
        public LiveBattleHUD hud;
        public ResultScreenView resultScreen;

        [Header("Battle Settings")]
        [Tooltip("Starting gold for both player and AI")]
        public int startingGold = 500;
        [Tooltip("Gold gained per second (passive income)")]
        public float passiveGoldPerSecond = 5f;
        [Tooltip("Time limit in seconds (0 = no limit)")]
        public float timeLimitSeconds = 0f;

        [Header("Auto Start")]
        public bool autoStartBattle = true;
        public float autoStartDelay = 2f;

        // Runtime references
        public GoldManager PlayerGold { get; private set; }
        public GoldManager AIGold { get; private set; }
        public UnitDeck PlayerDeck { get; private set; }
        public UnitDeck AIDeck { get; private set; }
        public LiveBattleSpawnController PlayerSpawner { get; private set; }
        public LiveBattleSpawnController AISpawner { get; private set; }
        public BaseController PlayerBase { get; private set; }
        public BaseController EnemyBase { get; private set; }

        // State
        public bool IsBattleActive { get; private set; }
        public bool IsBattleEnded { get; private set; }
        public float BattleTime { get; private set; }
        public bool PlayerVictory { get; private set; }

        // Events
        public event Action OnBattleStarted;
        public event Action<bool> OnBattleEnded; // true = player victory

        private LevelDefinition levelDefinition;
        private JsonDatabase database;
        private float passiveGoldTimer;
        private float autoStartTimer;
        private bool autoStartPending;
        private EntityRegistry entityRegistry;

        private void Awake()
        {
            EnsureGameBootstrapper();
        }

        private void EnsureGameBootstrapper()
        {
            GameBootstrapper bootstrapper = FindFirstObjectByType<GameBootstrapper>();
            if (bootstrapper == null)
            {
                GameObject bootstrapperGO = new GameObject("GameBootstrapper");
                bootstrapper = bootstrapperGO.AddComponent<GameBootstrapper>();
                Debug.Log("LiveBattleLevelController: Created GameBootstrapper automatically.");
            }
        }

        private void Start()
        {
            Initialize();

            if (autoStartBattle)
            {
                autoStartTimer = autoStartDelay;
                autoStartPending = true;
            }
        }

        private void Initialize()
        {
            database = ServiceLocator.Get<JsonDatabase>();
            ServiceLocator.TryGet(out entityRegistry);

            // Load level from save or use default
            SaveManager saveManager = ServiceLocator.Get<SaveManager>();
            PlayerProgress progress = saveManager.GetOrCreateProgress();
            if (!string.IsNullOrWhiteSpace(progress.lastSelectedLevelId))
            {
                levelId = progress.lastSelectedLevelId;
            }

            levelDefinition = database.FindLevel(levelId);
            if (levelDefinition == null)
            {
                Debug.LogError($"LiveBattleLevelController: Level '{levelId}' not found!");
                // Create default level for testing
                CreateDefaultLevel();
            }
            else
            {
                startingGold = levelDefinition.startGold > 0 ? levelDefinition.startGold : startingGold;
            }

            // Setup Gold Managers
            SetupGoldManagers();

            // Setup Unit Decks
            SetupUnitDecks();

            // Setup Spawners
            SetupSpawners();

            // Setup Bases
            SetupBases();

            // Setup HUD
            if (hud != null)
            {
                hud.Initialize(this);
            }

            Debug.Log($"LiveBattleLevelController: Initialized level '{levelId}' with {startingGold} starting gold.");
        }

        private void CreateDefaultLevel()
        {
            levelDefinition = new LevelDefinition
            {
                id = levelId,
                display_name = "Generated Level",
                startGold = startingGold,
                aiDifficulty = "normal",
                aiStrategy = "balanced"
            };
        }

        private void SetupGoldManagers()
        {
            // Find or create player gold manager
            var goldManagers = FindObjectsByType<GoldManager>(FindObjectsSortMode.None);
            foreach (var gm in goldManagers)
            {
                if (gm.OwnerTeam == GoldManager.Team.Player)
                {
                    PlayerGold = gm;
                }
                else if (gm.OwnerTeam == GoldManager.Team.AI)
                {
                    AIGold = gm;
                }
            }

            if (PlayerGold == null)
            {
                GameObject playerGoldGO = new GameObject("PlayerGoldManager");
                PlayerGold = playerGoldGO.AddComponent<GoldManager>();
                PlayerGold.Initialize(startingGold, GoldManager.Team.Player);
            }
            else
            {
                PlayerGold.Initialize(startingGold, GoldManager.Team.Player);
            }

            if (AIGold == null)
            {
                GameObject aiGoldGO = new GameObject("AIGoldManager");
                AIGold = aiGoldGO.AddComponent<GoldManager>();
                AIGold.Initialize(startingGold, GoldManager.Team.AI);
            }
            else
            {
                AIGold.Initialize(startingGold, GoldManager.Team.AI);
            }

            // Register with ServiceLocator
            ServiceLocator.Register(PlayerGold);
        }

        private void SetupUnitDecks()
        {
            PlayerDeck = new UnitDeck();
            AIDeck = new UnitDeck();

            // Set default civilization
            var defaultCiv = database.GetDefaultCivilization();
            if (defaultCiv != null)
            {
                PlayerDeck.SetCivilization(defaultCiv.id);
                AIDeck.SetCivilization(defaultCiv.id);

                // Auto-fill with available units
                if (defaultCiv.availableUnits != null)
                {
                    for (int i = 0; i < Mathf.Min(UnitDeck.MAX_UNITS, defaultCiv.availableUnits.Length); i++)
                    {
                        PlayerDeck.AddUnit(defaultCiv.availableUnits[i]);
                        AIDeck.AddUnit(defaultCiv.availableUnits[i]);
                    }
                }

                // Set hero
                if (defaultCiv.availableHeroes != null && defaultCiv.availableHeroes.Length > 0)
                {
                    PlayerDeck.SetHero(defaultCiv.availableHeroes[0]);
                    AIDeck.SetHero(defaultCiv.availableHeroes[0]);
                }
            }
        }

        private void SetupSpawners()
        {
            // Find existing spawners or create new ones
            var spawners = FindObjectsByType<LiveBattleSpawnController>(FindObjectsSortMode.None);

            foreach (var spawner in spawners)
            {
                if (spawner.team == GoldManager.Team.Player)
                {
                    PlayerSpawner = spawner;
                }
                else if (spawner.team == GoldManager.Team.AI)
                {
                    AISpawner = spawner;
                }
            }

            // Create if not found
            if (PlayerSpawner == null)
            {
                GameObject playerSpawnerGO = new GameObject("PlayerSpawnController");
                PlayerSpawner = playerSpawnerGO.AddComponent<LiveBattleSpawnController>();
                PlayerSpawner.team = GoldManager.Team.Player;
            }

            if (AISpawner == null)
            {
                GameObject aiSpawnerGO = new GameObject("AISpawnController");
                AISpawner = aiSpawnerGO.AddComponent<LiveBattleSpawnController>();
                AISpawner.team = GoldManager.Team.AI;
            }

            // Initialize spawners
            PlayerSpawner.Initialize(this, PlayerGold, PlayerDeck);
            AISpawner.Initialize(this, AIGold, AIDeck);
        }

        private void SetupBases()
        {
            // Find bases in scene
            var bases = FindObjectsByType<BaseController>(FindObjectsSortMode.None);

            foreach (var baseCtrl in bases)
            {
                if (baseCtrl.CompareTag("PlayerBase") || baseCtrl.gameObject.name.Contains("Player"))
                {
                    PlayerBase = baseCtrl;
                }
                else if (baseCtrl.CompareTag("EnemyBase") || baseCtrl.gameObject.name.Contains("Enemy"))
                {
                    EnemyBase = baseCtrl;
                }
            }

            // Subscribe to base destruction events
            if (PlayerBase != null)
            {
                PlayerBase.OnBaseDestroyed += OnPlayerBaseDestroyed;
            }

            if (EnemyBase != null)
            {
                EnemyBase.OnBaseDestroyed += OnEnemyBaseDestroyed;
            }
        }

        private void Update()
        {
            if (IsBattleEnded) return;

            // Handle auto start
            if (autoStartPending)
            {
                autoStartTimer -= Time.deltaTime;
                if (autoStartTimer <= 0f)
                {
                    autoStartPending = false;
                    StartBattle();
                }
                return;
            }

            if (!IsBattleActive) return;

            // Update battle time
            BattleTime += Time.deltaTime;

            // Passive gold income
            passiveGoldTimer += Time.deltaTime;
            if (passiveGoldTimer >= 1f)
            {
                passiveGoldTimer -= 1f;
                int goldAmount = Mathf.RoundToInt(passiveGoldPerSecond);
                if (goldAmount > 0)
                {
                    PlayerGold.AddGold(goldAmount);
                    AIGold.AddGold(goldAmount);
                }
            }

            // Check time limit
            if (timeLimitSeconds > 0 && BattleTime >= timeLimitSeconds)
            {
                // Time's up - determine winner by remaining base HP
                DetermineWinnerByHP();
            }

            // Update HUD
            if (hud != null)
            {
                hud.Refresh();
            }
        }

        public void StartBattle()
        {
            if (IsBattleActive || IsBattleEnded) return;

            IsBattleActive = true;
            BattleTime = 0f;
            passiveGoldTimer = 0f;

            Debug.Log("LiveBattleLevelController: Battle started!");
            OnBattleStarted?.Invoke();

            if (hud != null)
            {
                hud.Refresh();
            }
        }

        public void EndBattle(bool playerWins)
        {
            if (IsBattleEnded) return;

            IsBattleActive = false;
            IsBattleEnded = true;
            PlayerVictory = playerWins;

            Debug.Log($"LiveBattleLevelController: Battle ended! Player {(playerWins ? "wins" : "loses")}");

            // Award fame
            if (levelDefinition?.fameReward != null && ServiceLocator.TryGet(out Progression.FameManager fameManager))
            {
                int fameReward = playerWins ? levelDefinition.fameReward.victory : levelDefinition.fameReward.defeat;
                fameManager.AddFame(fameReward);
            }

            // Save progress
            if (playerWins)
            {
                SaveManager saveManager = ServiceLocator.Get<SaveManager>();
                PlayerProgress progress = saveManager.GetOrCreateProgress();
                if (!progress.completedLevelIds.Contains(levelId))
                {
                    progress.completedLevelIds.Add(levelId);
                }
                saveManager.SaveProgress(progress);
            }

            OnBattleEnded?.Invoke(playerWins);

            // Show result screen
            if (resultScreen != null)
            {
                resultScreen.ShowResults(playerWins, false);
            }
        }

        private void OnPlayerBaseDestroyed(BaseController baseController)
        {
            EndBattle(false);
        }

        private void OnEnemyBaseDestroyed(BaseController baseController)
        {
            EndBattle(true);
        }

        private void DetermineWinnerByHP()
        {
            float playerBaseHp = PlayerBase != null ? PlayerBase.currentHp : 0f;
            float enemyBaseHp = EnemyBase != null ? EnemyBase.currentHp : 0f;

            bool playerWins = playerBaseHp > enemyBaseHp;
            EndBattle(playerWins);
        }

        // Public API for spawning units
        public bool TrySpawnPlayerUnit(int slotIndex)
        {
            if (!IsBattleActive || PlayerSpawner == null) return false;
            return PlayerSpawner.TrySpawnUnit(slotIndex);
        }

        public bool TrySpawnPlayerHero()
        {
            if (!IsBattleActive || PlayerSpawner == null) return false;
            return PlayerSpawner.TrySpawnHero();
        }

        public bool TryUseAbility()
        {
            if (!IsBattleActive) return false;

            // Get civilization ability
            var civ = database.FindCivilization(PlayerDeck.CivilizationID);
            if (civ == null || string.IsNullOrEmpty(civ.specialAbility)) return false;

            var ability = database.FindAbility(civ.specialAbility);
            if (ability == null) return false;

            // TODO: Implement ability usage with cooldown
            Debug.Log($"Using ability: {ability.name}");
            return true;
        }

        public float GetPlayerBaseHPPercent()
        {
            if (PlayerBase == null) return 0f;
            return PlayerBase.currentHp / PlayerBase.maxHp;
        }

        public float GetEnemyBaseHPPercent()
        {
            if (EnemyBase == null) return 0f;
            return EnemyBase.currentHp / EnemyBase.maxHp;
        }

        public string GetFormattedBattleTime()
        {
            int minutes = Mathf.FloorToInt(BattleTime / 60f);
            int seconds = Mathf.FloorToInt(BattleTime % 60f);
            return $"{minutes:00}:{seconds:00}";
        }

        private void OnDestroy()
        {
            if (PlayerBase != null)
            {
                PlayerBase.OnBaseDestroyed -= OnPlayerBaseDestroyed;
            }

            if (EnemyBase != null)
            {
                EnemyBase.OnBaseDestroyed -= OnEnemyBaseDestroyed;
            }
        }
    }
}
