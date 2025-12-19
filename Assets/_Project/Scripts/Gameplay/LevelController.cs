using System.Collections.Generic;
using TowerOffense.Core;
using TowerOffense.Data;
using TowerOffense.Gameplay.Cards;
using TowerOffense.Gameplay.Entities;
using TowerOffense.Saving;
using UnityEngine;

namespace TowerOffense.Gameplay
{
    public class LevelController : MonoBehaviour
    {
        public string levelId = "lvl_01_etruria_outpost";

        public DeckManager deck;
        public HandManager hand;
        public CardPlayResolver resolver;
        public SpeedController speedController;

        public RunState Run { get; private set; }
        public LevelStateMachine Fsm { get; private set; }
        public WaveController Waves { get; private set; }
        public PathManager PathManager { get; private set; }
        public SpawnController Spawner { get; private set; }

        private void Start()
        {
            Run = new RunState
            {
                levelId = levelId,
                maxWaves = 5
            };

            LevelDefinition levelDefinition = ServiceLocator.Get<JsonDatabase>().FindLevel(levelId);
            PathManager = new PathManager();
            PathManager.InitializeFromLevel(levelDefinition);

            Spawner = new SpawnController();
            Spawner.Initialize(levelDefinition, PathManager);

            SpawnEnemyTowers(levelDefinition);

            var testDeck = new List<string>
            {
                "unit_tank_legionary",
                "unit_swarm_auxilia",
                "spell_fire_pot",
                "unit_knight",
                "unit_archer",
                "spell_freeze",
                "unit_mage",
                "spell_heal"
            };

            deck = new DeckManager();
            hand = new HandManager { handSize = 5 };
            resolver = new CardPlayResolver();
            speedController = new SpeedController();

            deck.Initialize(testDeck, 123);
            hand.FillToHandSize(deck);
            Run.handCardIds = new List<string>(hand.hand);
            Run.speed = speedController.CurrentSpeed;

            Fsm = new LevelStateMachine();
            Fsm.OnFinished += HandleRunFinished;

            Waves = GetComponent<WaveController>();
            if (Waves == null)
            {
                Waves = gameObject.AddComponent<WaveController>();
            }

            Fsm.EnterPlanning(Run);
        }

        public void StartWave()
        {
            Fsm.StartWave(Run);
            Waves.StartWave(this);
        }

        public void ToggleSpeed()
        {
            if (speedController == null)
            {
                Debug.LogWarning("SpeedController not initialized.");
                return;
            }

            speedController.Toggle();
            Run.speed = speedController.CurrentSpeed;
        }

        public void PlayCard(string cardId)
        {
            if (hand == null || deck == null || resolver == null)
            {
                Debug.LogWarning("Cannot play card - hand/deck/resolver not ready.");
                return;
            }

            if (!hand.PlayCard(cardId, deck, resolver))
            {
                Debug.LogWarning($"Card '{cardId}' not found in hand.");
                return;
            }

            Run.handCardIds = new List<string>(hand.hand);
        }

        public void OnWaveSimulatedEnd()
        {
            Fsm.EndWave(Run);
            if (Run.waveIndex >= Run.maxWaves)
            {
                Fsm.Finish(Run, victory: true);
            }
        }

        [ContextMenu("DEBUG Play First Card")]
        private void DebugPlayFirstCard()
        {
            if (hand == null || deck == null || resolver == null || hand.hand.Count == 0)
            {
                Debug.LogWarning("Cannot play first card - hand/deck/resolver not ready.");
                return;
            }

            string cardId = hand.hand[0];
            hand.PlayCard(cardId, deck, resolver);
            Run.handCardIds = new List<string>(hand.hand);
            Debug.Log($"Hand after play: {string.Join(", ", hand.hand)}");
        }

        [ContextMenu("DEBUG Spawn Unit")]
        public void DebugSpawnUnit()
        {
            DebugSpawnUnit("unit_tank_legionary");
        }

        public void DebugSpawnUnit(string unitId)
        {
            if (Spawner == null)
            {
                Debug.LogWarning("Spawner not initialized.");
                return;
            }

            Spawner.SpawnUnit(unitId);
        }

        public void SpawnHero(string heroId)
        {
            if (string.IsNullOrWhiteSpace(heroId))
            {
                Debug.LogWarning("SpawnHero called with empty hero id.");
                return;
            }

            Vector3 spawnPosition = Vector3.zero;
            if (PathManager != null)
            {
                LevelDefinition levelDefinition = ServiceLocator.Get<JsonDatabase>().FindLevel(levelId);
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
        }

        private void HandleRunFinished(bool victory)
        {
            if (!victory)
            {
                return;
            }

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
                }
            }

            saveManager.SaveProgress(progress);
        }

        private void SpawnEnemyTowers(LevelDefinition levelDefinition)
        {
            if (levelDefinition == null || levelDefinition.enemy_defenses == null || levelDefinition.enemy_defenses.towers == null)
            {
                return;
            }

            JsonDatabase database = ServiceLocator.Get<JsonDatabase>();

            foreach (LevelDefinition.TowerPlacementDto placement in levelDefinition.enemy_defenses.towers)
            {
                GameObject towerObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                towerObject.name = $"Tower_{placement.instance_id}";
                towerObject.transform.position = new Vector3(placement.position.x, placement.position.y, placement.position.z);
                towerObject.transform.rotation = Quaternion.Euler(0f, placement.rotation_y_degrees, 0f);

                TowerController towerController = towerObject.AddComponent<TowerController>();

                TowerDefinition towerDefinition = database.FindTower(placement.tower_id);
                TowerDefinition.TierDto tierDefinition = GetTowerTier(towerDefinition, placement.tier);

                if (tierDefinition != null && tierDefinition.attack != null)
                {
                    if (tierDefinition.attack.range > 0f)
                    {
                        towerController.range = tierDefinition.attack.range;
                    }

                    if (tierDefinition.attack.base_damage > 0f)
                    {
                        towerController.damage = tierDefinition.attack.base_damage;
                    }

                    if (tierDefinition.attack.attacks_per_second > 0f)
                    {
                        towerController.attacksPerSecond = tierDefinition.attack.attacks_per_second;
                    }
                }
            }
        }

        private static TowerDefinition.TierDto GetTowerTier(TowerDefinition towerDefinition, int requestedTier)
        {
            if (towerDefinition == null || towerDefinition.tiers == null || towerDefinition.tiers.Length == 0)
            {
                return null;
            }

            foreach (TowerDefinition.TierDto tier in towerDefinition.tiers)
            {
                if (tier != null && tier.tier == requestedTier)
                {
                    return tier;
                }
            }

            return towerDefinition.tiers[0];
        }
    }
}
