using UnityEngine;
using TowerConquest.Debug;
using TowerConquest.Gameplay;
using TowerConquest.Data;

namespace TowerConquest.AI
{
    /// <summary>
    /// Main AI controller that makes strategic decisions
    /// </summary>
    public class AICommander : MonoBehaviour
    {
        public enum Difficulty
        {
            Easy,
            Normal,
            Hard
        }

        public enum StrategyType
        {
            Aggressive,
            Defensive,
            Balanced
        }

        [Header("Configuration")]
        [SerializeField] private Difficulty difficulty = Difficulty.Normal;
        [SerializeField] private StrategyType strategyType = StrategyType.Balanced;

        [Header("References")]
        [SerializeField] private GoldManager goldManager;
        [SerializeField] private ConstructionManager constructionManager;
        [SerializeField] private Transform aiBase;

        [Header("Decision Timing")]
        [SerializeField] private float decisionInterval = 2f; // Make decisions every 2 seconds
        [SerializeField] private float nextDecisionTime;

        private AIStrategy strategy;
        private AIBuildPlanner buildPlanner;
        private AIUnitSpawner unitSpawner;
        private UnitDeck aiDeck;
        private JsonDatabase database;

        private float reactionSpeedMultiplier = 1f;

        private void Awake()
        {
            buildPlanner = new AIBuildPlanner();
            unitSpawner = new AIUnitSpawner();
        }

        public void Initialize(Difficulty diff, StrategyType strat, UnitDeck deck, GoldManager gold, JsonDatabase db, ConstructionManager conManager, Transform baseTransform)
        {
            difficulty = diff;
            strategyType = strat;
            aiDeck = deck;
            goldManager = gold;
            database = db;
            constructionManager = conManager;
            aiBase = baseTransform;

            // Set reaction speed based on difficulty
            switch (difficulty)
            {
                case Difficulty.Easy:
                    reactionSpeedMultiplier = 1.5f; // Slower reactions
                    decisionInterval = 3f;
                    break;
                case Difficulty.Normal:
                    reactionSpeedMultiplier = 1f;
                    decisionInterval = 2f;
                    break;
                case Difficulty.Hard:
                    reactionSpeedMultiplier = 0.5f; // Faster reactions
                    decisionInterval = 1f;
                    break;
            }

            // Create strategy
            strategy = CreateStrategy(strategyType);
            strategy.Initialize(this, goldManager, constructionManager, database, aiDeck);

            buildPlanner.Initialize(this, goldManager, constructionManager, database);
            unitSpawner.Initialize(this, goldManager, database, aiBase);

            nextDecisionTime = Time.time + decisionInterval;

            Log.Info($"[AICommander] Initialized with {difficulty} difficulty and {strategyType} strategy");
        }

        private AIStrategy CreateStrategy(StrategyType type)
        {
            switch (type)
            {
                case StrategyType.Aggressive:
                    return new AggressiveStrategy();
                case StrategyType.Defensive:
                    return new DefensiveStrategy();
                case StrategyType.Balanced:
                default:
                    return new BalancedStrategy();
            }
        }

        private void Update()
        {
            if (Time.time >= nextDecisionTime)
            {
                MakeDecisions();
                nextDecisionTime = Time.time + decisionInterval * reactionSpeedMultiplier;
            }
        }

        private void MakeDecisions()
        {
            if (strategy == null || goldManager == null) return;

            // Let strategy decide what to do
            strategy.DecideActions();
        }

        public GoldManager GetGoldManager() => goldManager;
        public ConstructionManager GetConstructionManager() => constructionManager;
        public AIBuildPlanner GetBuildPlanner() => buildPlanner;
        public AIUnitSpawner GetUnitSpawner() => unitSpawner;
        public UnitDeck GetDeck() => aiDeck;
        public Difficulty GetDifficulty() => difficulty;
        public Transform GetBase() => aiBase;
    }
}
