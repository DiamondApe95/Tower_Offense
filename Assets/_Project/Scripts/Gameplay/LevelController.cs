using System;
using System.Collections.Generic;
using TowerConquest.Core;
using TowerConquest.Data;
using TowerConquest.Gameplay.Cards;
using TowerConquest.Gameplay.Entities;
using TowerConquest.Saving;
using TowerConquest.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TowerConquest.Gameplay
{
    public class LevelController : MonoBehaviour
    {
        [Header("Level Settings")]
        public string levelId = "lvl_01_etruria_outpost";

        [Header("UI References")]
        public LevelHUD hud;
        public ResultScreenView resultScreen;
        public LevelSpawner levelSpawner;

        [Header("Wave Settings")]
        [Tooltip("Automatisch die erste Welle starten nach einer Verzögerung")]
        public bool autoStartFirstWave = false;
        [Tooltip("Verzögerung in Sekunden bevor die erste Welle automatisch startet")]
        public float autoStartDelay = 3f;
        [Tooltip("Automatisch nächste Welle starten nach Ende der vorherigen")]
        public bool autoStartNextWave = false;
        [Tooltip("Verzögerung zwischen Wellen bei Autostart")]
        public float waveTransitionDelay = 2f;

        public DeckManager deck;
        public HandManager hand;
        public CardPlayResolver resolver;
        public SpeedController speedController;

        public RunState Run { get; private set; }
        public LevelStateMachine Fsm { get; private set; }
        public WaveController Waves { get; private set; }
        public PathManager PathManager { get; private set; }
        public SpawnController Spawner { get; private set; }
        public BaseController BaseController { get; private set; }

        public float BaseHp => BaseController != null ? BaseController.currentHp : 0f;
        public int ActiveUnits => GetActiveUnitCount();
        public float EstimatedDps => CalculateEstimatedDps();

        private EntityRegistry entityRegistry;

        private LevelDefinition levelDefinition;
        private GlobalRulesDto globalRules;
        private readonly List<string> queuedUnitCards = new List<string>();
        private readonly List<string> defenseWaveUnits = new List<string>();
        private readonly List<HeroController> activeHeroes = new List<HeroController>();
        private bool hudInitialized;
        private float autoStartTimer;
        private bool autoStartPending;
        private Coroutine autoStartCoroutine;

        private void Start()
        {
            if (hud == null)
            {
                UnityEngine.Debug.LogError("LevelController: LevelHUD reference is missing! Please assign it in the inspector.");
            }

            if (resultScreen == null)
            {
                UnityEngine.Debug.LogError("LevelController: ResultScreenView reference is missing! Please assign it in the inspector.");
            }

            // EntityRegistry für Performance-optimierte Entity-Abfragen
            ServiceLocator.TryGet(out entityRegistry);

            SaveManager saveManager = ServiceLocator.Get<SaveManager>();
            PlayerProgress progress = saveManager.GetOrCreateProgress();
            if (!string.IsNullOrWhiteSpace(progress.lastSelectedLevelId))
            {
                levelId = progress.lastSelectedLevelId;
                UnityEngine.Debug.Log($"Loading level from PlayerProgress: {levelId}");
            }
            else
            {
                UnityEngine.Debug.Log($"No level selected in PlayerProgress, using default: {levelId}");
            }

            JsonDatabase database = ServiceLocator.Get<JsonDatabase>();
            levelDefinition = database.FindLevel(levelId);
            if (levelDefinition == null)
            {
                UnityEngine.Debug.LogError($"LevelController: Level '{levelId}' not found in database. Cannot start level.");
                enabled = false;
                return;
            }

            globalRules = database.GlobalRules;

            Run = new RunState
            {
                levelId = levelId,
                maxWaves = levelDefinition.player_rules?.max_waves ?? 5,
                heroEveryNWaves = globalRules?.wave_rules?.hero_every_n_waves ?? 5,
                allowMidWaveSpawns = globalRules?.wave_rules?.allow_mid_wave_spawns ?? false,
                maxEnergyPerWave = Mathf.Max(5, 8 + levelDefinition.recommended_power)
            };

            Run.gameMode = ResolveGameMode(globalRules?.mode);
            Run.selectedHeroId = ResolveDefaultHeroId();

            int handSize = globalRules?.hand_size > 0 ? globalRules.hand_size : 5;

            deck = new DeckManager();
            hand = new HandManager { handSize = handSize };
            resolver = new CardPlayResolver();
            resolver.Initialize(this);

            speedController = new SpeedController();
            speedController.supportedSpeeds = ResolveSpeedModes(globalRules?.wave_rules?.speed_modes);
            speedController.SetSpeed(speedController.CurrentSpeed);

            List<string> startingDeck = BuildStartingDeck();
            deck.Initialize(startingDeck, Environment.TickCount);
            hand.FillStartingHandUnique(deck);
            Run.handCardIds = new List<string>(hand.hand);
            Run.deckUnitIds = new List<string>(levelDefinition.player_rules?.starting_deck?.unit_cards ?? Array.Empty<string>());
            Run.deckSpellIds = new List<string>(levelDefinition.player_rules?.starting_deck?.spell_cards ?? Array.Empty<string>());
            Run.speed = speedController.CurrentSpeed;

            PathManager = new PathManager();
            PathManager.InitializeFromLevel(levelDefinition);

            if (levelSpawner == null)
            {
                levelSpawner = GetComponent<LevelSpawner>();
                if (levelSpawner == null)
                {
                    levelSpawner = gameObject.AddComponent<LevelSpawner>();
                }
            }

            levelSpawner.Spawn(levelDefinition, Run.gameMode, this);
            BaseController = levelSpawner.SpawnedBase;
            if (BaseController != null)
            {
                BaseController.OnBaseDestroyed += HandleBaseDestroyed;
            }

            Spawner = new SpawnController();
            Spawner.Initialize(levelDefinition, PathManager, BaseController);

            Waves = GetComponent<WaveController>();
            if (Waves == null)
            {
                Waves = gameObject.AddComponent<WaveController>();
            }

            Fsm = new LevelStateMachine();
            Fsm.OnFinished += HandleRunFinished;
            Fsm.OnPlanningStarted += RefreshHud;
            Fsm.OnWaveEnded += _ => RefreshHud();

            if (Run.gameMode == GameMode.Defense)
            {
                PrepareDefenseWaveUnits();
            }

            Fsm.EnterPlanning(Run);
            RefreshHud();

            // Autostart erste Welle
            if (autoStartFirstWave)
            {
                ScheduleAutoStart(autoStartDelay);
            }
        }

        private void Update()
        {
            // Neues Input System für Hero-Skill Aktivierung
            if (Keyboard.current != null && Keyboard.current.hKey.wasPressedThisFrame)
            {
                ActivateHeroSkill();
            }

            // Autostart Timer verarbeiten
            if (autoStartPending && Run != null && Run.isPlanning && !Run.isFinished)
            {
                autoStartTimer -= Time.deltaTime;
                if (autoStartTimer <= 0f)
                {
                    autoStartPending = false;
                    StartWave();
                }
            }
        }

        public void StartWave()
        {
            if (Run == null || Run.isFinished)
            {
                return;
            }

            if (!Run.isPlanning)
            {
                UnityEngine.Debug.LogWarning("StartWave called outside planning phase.");
                return;
            }

            Fsm.StartWave(Run);
            Waves.StartWave(this);
            RefreshHud();
        }

        public void ToggleSpeed()
        {
            if (speedController == null)
            {
                UnityEngine.Debug.LogWarning("SpeedController not initialized.");
                return;
            }

            speedController.Toggle();
            Run.speed = speedController.CurrentSpeed;
            RefreshHud();
        }

        public void PlayCard(string cardId)
        {
            if (hand == null || deck == null || resolver == null)
            {
                UnityEngine.Debug.LogWarning("Cannot play card - hand/deck/resolver not ready.");
                return;
            }

            if (!IsCardPlayableNow(cardId))
            {
                UnityEngine.Debug.Log($"Card '{cardId}' cannot be played in the current phase.");
                return;
            }

            if (!TryGetCardCost(cardId, out int cost))
            {
                UnityEngine.Debug.LogWarning($"Card '{cardId}' cost not found.");
                return;
            }

            if (Run.energy < cost)
            {
                UnityEngine.Debug.Log($"Not enough energy for card '{cardId}'. {Run.energy}/{cost}");
                return;
            }

            if (!hand.PlayCard(cardId, deck, resolver))
            {
                UnityEngine.Debug.LogWarning($"Card '{cardId}' not found in hand.");
                return;
            }

            Run.energy = Mathf.Max(0, Run.energy - cost);
            Run.handCardIds = new List<string>(hand.hand);
            RefreshHud();
        }

        public bool CanPlayCard(string cardId)
        {
            return IsCardPlayableNow(cardId);
        }

        public void QueueUnitCard(string unitId)
        {
            if (string.IsNullOrWhiteSpace(unitId))
            {
                return;
            }

            queuedUnitCards.Add(unitId);
        }

        public IReadOnlyList<string> GetWaveUnits()
        {
            if (Run.gameMode == GameMode.Defense)
            {
                return new List<string>(defenseWaveUnits);
            }

            List<string> units = new List<string>(queuedUnitCards);
            queuedUnitCards.Clear();
            return units;
        }

        public bool HasActiveUnits()
        {
            if (entityRegistry != null)
            {
                return entityRegistry.HasActiveUnits();
            }
            return FindObjectsByType<UnitController>(FindObjectsSortMode.None).Length > 0;
        }

        private int GetActiveUnitCount()
        {
            if (entityRegistry != null)
            {
                return entityRegistry.UnitCount;
            }
            return FindObjectsByType<UnitController>(FindObjectsSortMode.None).Length;
        }

        public bool IsBaseDestroyed()
        {
            return BaseController != null && BaseController.currentHp <= 0f;
        }

        public void OnWaveFinished()
        {
            if (IsBaseDestroyed())
            {
                Fsm.Finish(Run, true);
                return;
            }

            Fsm.EndWave(Run);

            if (Run.waveIndex >= Run.maxWaves)
            {
                Fsm.Finish(Run, false);
                return;
            }

            RefreshHud();

            // Autostart nächste Welle
            if (autoStartNextWave)
            {
                ScheduleAutoStart(waveTransitionDelay);
            }
        }

        public void ScheduleAutoStart(float delay)
        {
            autoStartTimer = Mathf.Max(0.1f, delay);
            autoStartPending = true;
            UnityEngine.Debug.Log($"LevelController: Wave auto-start scheduled in {delay} seconds.");
        }

        public void CancelAutoStart()
        {
            autoStartPending = false;
            autoStartTimer = 0f;
        }

        public bool IsAutoStartPending => autoStartPending;
        public float AutoStartTimeRemaining => autoStartPending ? autoStartTimer : 0f;

        public void SpawnHero(string heroId)
        {
            if (string.IsNullOrWhiteSpace(heroId))
            {
                UnityEngine.Debug.LogWarning("SpawnHero called with empty hero id.");
                return;
            }

            Vector3 spawnPosition = Vector3.zero;
            if (PathManager != null)
            {
                spawnPosition = PathManager.GetSpawnPosition(levelDefinition);
            }

            spawnPosition += new Vector3(0.75f, 0f, 0.75f);

            GameObject heroObject = null;
            if (ServiceLocator.TryGet(out PrefabRegistry registry))
            {
                foreach (PrefabRegistry.IdPrefabPair entry in registry.entries)
                {
                    if (entry != null && entry.id == heroId && entry.prefab != null)
                    {
                        heroObject = Instantiate(entry.prefab);
                        break;
                    }
                }
            }

            if (heroObject == null)
            {
                heroObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                heroObject.name = $"{heroId}_Fallback";
            }

            heroObject.transform.position = spawnPosition;

            HeroController heroController = heroObject.GetComponent<HeroController>();
            if (heroController == null)
            {
                heroController = heroObject.AddComponent<HeroController>();
            }

            float heroHp = 300f;
            heroController.Initialize(heroId, heroHp);
            activeHeroes.Add(heroController);
        }

        private void ActivateHeroSkill()
        {
            if (activeHeroes.Count == 0)
            {
                return;
            }

            HeroController hero = activeHeroes[0];
            if (hero != null)
            {
                hero.ActivateSkill();
            }
        }

        private void HandleBaseDestroyed(BaseController baseController)
        {
            if (Run == null || Run.isFinished)
            {
                return;
            }

            Fsm.Finish(Run, true);
        }

        private void HandleRunFinished(bool victory)
        {
            bool nextLevelUnlocked = false;
            if (victory)
            {
                SaveManager saveManager = ServiceLocator.Get<SaveManager>();
                PlayerProgress progress = saveManager.GetOrCreateProgress();

                if (!progress.completedLevelIds.Contains(levelId))
                {
                    progress.completedLevelIds.Add(levelId);
                }

                JsonDatabase database = ServiceLocator.Get<JsonDatabase>();
                if (database.Levels != null && database.Levels.Count > 0)
                {
                    int currentIndex = -1;
                    for (int index = 0; index < database.Levels.Count; index++)
                    {
                        if (database.Levels[index].id == levelId)
                        {
                            currentIndex = index;
                            break;
                        }
                    }

                    int nextIndex = currentIndex + 1;
                    if (currentIndex >= 0 && nextIndex < database.Levels.Count)
                    {
                        string nextLevelId = database.Levels[nextIndex].id;
                        if (!string.IsNullOrWhiteSpace(nextLevelId) && !progress.unlockedLevelIds.Contains(nextLevelId))
                        {
                            progress.unlockedLevelIds.Add(nextLevelId);
                        }

                        nextLevelUnlocked = !string.IsNullOrWhiteSpace(nextLevelId);
                    }
                }

                saveManager.SaveProgress(progress);
            }

            if (resultScreen != null)
            {
                resultScreen.ShowResults(victory, nextLevelUnlocked);
            }
        }

        private List<string> BuildStartingDeck()
        {
            var cards = new List<string>();
            if (levelDefinition.player_rules?.starting_deck?.unit_cards != null)
            {
                cards.AddRange(levelDefinition.player_rules.starting_deck.unit_cards);
            }

            if (levelDefinition.player_rules?.starting_deck?.spell_cards != null)
            {
                cards.AddRange(levelDefinition.player_rules.starting_deck.spell_cards);
            }

            return cards;
        }

        private void PrepareDefenseWaveUnits()
        {
            defenseWaveUnits.Clear();

            if (levelDefinition.player_rules?.starting_deck?.unit_cards != null)
            {
                defenseWaveUnits.AddRange(levelDefinition.player_rules.starting_deck.unit_cards);
            }
        }

        private bool TryGetCardCost(string cardId, out int cost)
        {
            cost = 0;
            JsonDatabase database = ServiceLocator.Get<JsonDatabase>();

            if (cardId.StartsWith("unit_"))
            {
                UnitDefinition unit = database.FindUnit(cardId);
                cost = unit?.card?.cost ?? 0;
                return unit != null;
            }

            if (cardId.StartsWith("spell_"))
            {
                SpellDefinition spell = database.FindSpell(cardId);
                cost = spell?.card?.cost ?? 0;
                return spell != null;
            }

            return false;
        }

        private bool IsCardPlayableNow(string cardId)
        {
            if (Run == null)
            {
                return false;
            }

            if (Run.isPlanning)
            {
                return true;
            }

            if (cardId.StartsWith("unit_"))
            {
                return Run.allowMidWaveSpawns;
            }

            if (cardId.StartsWith("spell_"))
            {
                return Run.isAttacking;
            }

            return false;
        }

        private void RefreshHud()
        {
            if (hud != null)
            {
                if (!hudInitialized)
                {
                    hud.Initialize(this);
                    hudInitialized = true;
                }

                hud.Refresh();
            }
        }

        private float CalculateEstimatedDps()
        {
            if (entityRegistry != null)
            {
                return entityRegistry.CalculateTotalTowerDps();
            }

            TowerController[] towers = FindObjectsByType<TowerController>(FindObjectsSortMode.None);
            float sum = 0f;
            foreach (TowerController tower in towers)
            {
                if (tower != null)
                {
                    sum += tower.EstimatedDps;
                }
            }

            return sum;
        }

        private static GameMode ResolveGameMode(string modeValue)
        {
            if (string.Equals(modeValue, "tower_defense", StringComparison.OrdinalIgnoreCase))
            {
                return GameMode.Defense;
            }

            return GameMode.Offense;
        }

        private static float[] ResolveSpeedModes(float[] configured)
        {
            var modes = new List<float>();
            if (configured != null && configured.Length > 0)
            {
                modes.AddRange(configured);
            }
            else
            {
                modes.Add(1f);
                modes.Add(2f);
            }

            bool hasThree = false;
            foreach (float mode in modes)
            {
                if (Mathf.Approximately(mode, 3f))
                {
                    hasThree = true;
                    break;
                }
            }

            if (!hasThree)
            {
                modes.Add(3f);
            }

            return modes.ToArray();
        }

        private string ResolveDefaultHeroId()
        {
            if (levelDefinition?.player_rules?.hero_pool == null || levelDefinition.player_rules.hero_pool.Length == 0)
            {
                return "hero_legatus";
            }

            string heroId = levelDefinition.player_rules.hero_pool[0];
            return string.IsNullOrWhiteSpace(heroId) ? "hero_legatus" : heroId;
        }
    }
}
