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
        public BattleCountdownTimer countdownTimer;

        [Header("Battle Settings")]
        [Tooltip("Starting gold for both player and AI")]
        public int startingGold = 500;
        [Tooltip("Gold gained per second (passive income)")]
        public float passiveGoldPerSecond = 5f;
        [Tooltip("Time limit in seconds (0 = no limit)")]
        public float timeLimitSeconds = 0f;

        [Header("Countdown Settings")]
        [Tooltip("Duration of the pre-battle countdown")]
        public float countdownDuration = 5f;
        [Tooltip("Use countdown timer before battle starts")]
        public bool useCountdown = true;

        [Header("Auto Start")]
        public bool autoStartBattle = true;
        public float autoStartDelay = 0.5f;

        // Runtime references
        public GoldManager PlayerGold { get; private set; }
        public GoldManager AIGold { get; private set; }
        public UnitDeck PlayerDeck { get; private set; }
        public UnitDeck AIDeck { get; private set; }
        public LiveBattleSpawnController PlayerSpawner { get; private set; }
        public LiveBattleSpawnController AISpawner { get; private set; }
        public BaseController PlayerBase { get; private set; }
        public BaseController EnemyBase { get; private set; }
        public AbilityManager PlayerAbility { get; private set; }
        public AbilityManager AIAbility { get; private set; }

        // State
        public bool IsBattleActive { get; private set; }
        public bool IsBattleEnded { get; private set; }
        public bool IsCountingDown { get; private set; }
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

        // Battle Statistics
        private int playerUnitsSpawned;
        private int playerEnemiesKilled;
        private int playerTowersBuilt;
        private int playerGoldEarned;

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

        /// <summary>
        /// Setup countdown timer and start countdown
        /// </summary>
        private void SetupCountdownTimer()
        {
            // Find or create countdown timer
            if (countdownTimer == null)
            {
                countdownTimer = FindFirstObjectByType<BattleCountdownTimer>();
            }

            if (countdownTimer == null)
            {
                GameObject timerGO = new GameObject("BattleCountdownTimer");
                countdownTimer = timerGO.AddComponent<BattleCountdownTimer>();

                // Create default UI
                Canvas canvas = FindFirstObjectByType<Canvas>();
                if (canvas != null)
                {
                    countdownTimer.CreateDefaultUI(canvas.transform);
                }
            }

            countdownTimer.OnCountdownComplete += OnCountdownComplete;
        }

        private void OnCountdownComplete()
        {
            IsCountingDown = false;
            ActuallyStartBattle();
        }

        private void ActuallyStartBattle()
        {
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

            // Setup Abilities
            SetupAbilities();

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

        private void SetupAbilities()
        {
            // Get player civilization ability
            var playerCiv = database?.FindCivilization(PlayerDeck?.CivilizationID);
            if (playerCiv != null && !string.IsNullOrEmpty(playerCiv.specialAbility))
            {
                GameObject playerAbilityGO = new GameObject("PlayerAbilityManager");
                PlayerAbility = playerAbilityGO.AddComponent<AbilityManager>();
                PlayerAbility.Initialize(playerCiv.specialAbility, GoldManager.Team.Player, database);
                Debug.Log($"LiveBattleLevelController: Set up player ability '{playerCiv.specialAbility}'");
            }

            // Get AI civilization ability
            var aiCiv = database?.FindCivilization(AIDeck?.CivilizationID);
            if (aiCiv != null && !string.IsNullOrEmpty(aiCiv.specialAbility))
            {
                GameObject aiAbilityGO = new GameObject("AIAbilityManager");
                AIAbility = aiAbilityGO.AddComponent<AbilityManager>();
                AIAbility.Initialize(aiCiv.specialAbility, GoldManager.Team.AI, database);
                Debug.Log($"LiveBattleLevelController: Set up AI ability '{aiCiv.specialAbility}'");
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
            if (IsBattleActive || IsBattleEnded || IsCountingDown) return;

            if (useCountdown)
            {
                IsCountingDown = true;
                SetupCountdownTimer();
                countdownTimer.StartCountdown(countdownDuration);
                Debug.Log("LiveBattleLevelController: Starting countdown...");
            }
            else
            {
                ActuallyStartBattle();
            }
        }

        public void EndBattle(bool playerWins)
        {
            if (IsBattleEnded) return;

            IsBattleActive = false;
            IsBattleEnded = true;
            PlayerVictory = playerWins;

            Debug.Log($"LiveBattleLevelController: Battle ended! Player {(playerWins ? "wins" : "loses")}");

            // Calculate fame reward
            int fameEarned = 0;
            if (levelDefinition?.fameReward != null && ServiceLocator.TryGet(out Progression.FameManager fameManager))
            {
                fameEarned = playerWins ? levelDefinition.fameReward.victory : levelDefinition.fameReward.defeat;
                fameManager.AddFame(fameEarned);
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

            // Show result screen with statistics
            if (resultScreen != null)
            {
                BattleStats stats = GetBattleStats();
                resultScreen.ShowResults(playerWins, playerWins, fameEarned, stats);
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
            if (!CanPerformGameplayActions() || PlayerSpawner == null) return false;
            return PlayerSpawner.TrySpawnUnit(slotIndex);
        }

        public bool TrySpawnPlayerHero()
        {
            if (!CanPerformGameplayActions() || PlayerSpawner == null) return false;
            return PlayerSpawner.TrySpawnHero();
        }

        public bool TryUseAbility()
        {
            if (!CanPerformGameplayActions()) return false;

            if (PlayerAbility == null)
            {
                Debug.LogWarning("LiveBattleLevelController: No ability manager available");
                return false;
            }

            if (!PlayerAbility.CanUse)
            {
                Debug.Log($"LiveBattleLevelController: Ability on cooldown ({PlayerAbility.CooldownRemaining:F1}s remaining)");
                return false;
            }

            return PlayerAbility.UseAbility();
        }

        /// <summary>
        /// Get the remaining cooldown for the player's ability
        /// </summary>
        public float GetAbilityCooldown()
        {
            return PlayerAbility?.CooldownRemaining ?? 0f;
        }

        /// <summary>
        /// Check if the player's ability can be used
        /// </summary>
        public bool CanUseAbility()
        {
            return PlayerAbility != null && PlayerAbility.CanUse && CanPerformGameplayActions();
        }

        /// <summary>
        /// Get the ability name for display
        /// </summary>
        public string GetAbilityName()
        {
            return PlayerAbility?.AbilityName ?? "";
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

            if (countdownTimer != null)
            {
                countdownTimer.OnCountdownComplete -= OnCountdownComplete;
            }
        }

        /// <summary>
        /// Check if gameplay actions are allowed (not during countdown)
        /// </summary>
        public bool CanPerformGameplayActions()
        {
            return IsBattleActive && !IsBattleEnded && !IsCountingDown;
        }

        #region Statistics Tracking

        /// <summary>
        /// Record that the player spawned a unit
        /// </summary>
        public void RecordUnitSpawned()
        {
            playerUnitsSpawned++;
        }

        /// <summary>
        /// Record that an enemy was killed
        /// </summary>
        public void RecordEnemyKilled()
        {
            playerEnemiesKilled++;
        }

        /// <summary>
        /// Record that a tower was built
        /// </summary>
        public void RecordTowerBuilt()
        {
            playerTowersBuilt++;
        }

        /// <summary>
        /// Record gold earned
        /// </summary>
        public void RecordGoldEarned(int amount)
        {
            playerGoldEarned += amount;
        }

        /// <summary>
        /// Get the current battle statistics
        /// </summary>
        public BattleStats GetBattleStats()
        {
            return new BattleStats(
                playerUnitsSpawned,
                playerEnemiesKilled,
                playerTowersBuilt,
                BattleTime,
                GetPlayerBaseHPPercent(),
                playerGoldEarned
            );
        }

        #endregion
    }
}
