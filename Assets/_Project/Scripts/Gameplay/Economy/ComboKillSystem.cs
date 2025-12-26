using System;
using UnityEngine;
using TowerConquest.Core;
using TowerConquest.Gameplay.Entities;

namespace TowerConquest.Gameplay.Economy
{
    /// <summary>
    /// Tracks rapid kills and rewards bonus gold for combos
    /// Combo multiplier increases with consecutive kills within time window
    /// </summary>
    public class ComboKillSystem : MonoBehaviour
    {
        [Header("Combo Settings")]
        [Tooltip("Time window to maintain combo (seconds)")]
        [SerializeField] private float comboTimeWindow = 3f;

        [Tooltip("Maximum combo multiplier")]
        [SerializeField] private float maxComboMultiplier = 3f;

        [Tooltip("Multiplier increase per kill")]
        [SerializeField] private float multiplierPerKill = 0.25f;

        [Tooltip("Minimum kills for combo to start")]
        [SerializeField] private int minKillsForCombo = 2;

        [Header("Visual Feedback")]
        [SerializeField] private bool showComboUI = true;
        [SerializeField] private float comboDisplayDuration = 2f;

        [Header("Runtime")]
        [SerializeField] private int currentCombo;
        [SerializeField] private float comboTimer;
        [SerializeField] private float currentMultiplier = 1f;

        private GoldManager goldManager;
        private GoldManager.Team ownerTeam;
        private EntityRegistry entityRegistry;

        // Events
        public event Action<int, float> OnComboChanged; // (combo count, multiplier)
        public event Action<int> OnComboEnded; // final combo count
        public event Action<int, int> OnComboKillReward; // (base gold, bonus gold)

        public int CurrentCombo => currentCombo;
        public float CurrentMultiplier => currentMultiplier;
        public float ComboTimeRemaining => comboTimer;
        public bool IsComboActive => currentCombo >= minKillsForCombo;

        public void Initialize(GoldManager.Team team, GoldManager gold)
        {
            ownerTeam = team;
            goldManager = gold;
            ServiceLocator.TryGet(out entityRegistry);

            ResetCombo();
            SubscribeToKillEvents();

            Debug.Log($"[ComboKillSystem] Initialized for team {team}");
        }

        private void SubscribeToKillEvents()
        {
            // Subscribe to unit death events via EntityRegistry
            if (entityRegistry != null)
            {
                entityRegistry.OnUnitDied += OnUnitKilled;
            }
        }

        private void Update()
        {
            if (currentCombo > 0)
            {
                comboTimer -= Time.deltaTime;

                if (comboTimer <= 0)
                {
                    EndCombo();
                }
            }
        }

        /// <summary>
        /// Called when any unit dies
        /// </summary>
        private void OnUnitKilled(UnitController unit, GameObject killer)
        {
            if (unit == null) return;

            // Check if our team killed this unit
            bool isOurKill = IsOurKill(unit, killer);
            if (!isOurKill) return;

            // Register kill and extend combo
            RegisterKill(unit);
        }

        /// <summary>
        /// Register a kill manually (for systems not using EntityRegistry)
        /// </summary>
        public void RegisterKill(UnitController killedUnit)
        {
            currentCombo++;
            comboTimer = comboTimeWindow;

            // Calculate new multiplier
            if (currentCombo >= minKillsForCombo)
            {
                float bonusMultiplier = (currentCombo - minKillsForCombo + 1) * multiplierPerKill;
                currentMultiplier = Mathf.Min(1f + bonusMultiplier, maxComboMultiplier);
            }
            else
            {
                currentMultiplier = 1f;
            }

            OnComboChanged?.Invoke(currentCombo, currentMultiplier);

            if (IsComboActive)
            {
                Debug.Log($"[ComboKillSystem] Combo x{currentCombo}! Multiplier: {currentMultiplier:F2}x");
            }
        }

        /// <summary>
        /// Apply combo multiplier to gold reward
        /// </summary>
        public int ApplyComboToReward(int baseGold)
        {
            if (!IsComboActive)
            {
                return baseGold;
            }

            int bonusGold = Mathf.RoundToInt(baseGold * (currentMultiplier - 1f));
            int totalGold = baseGold + bonusGold;

            if (bonusGold > 0)
            {
                OnComboKillReward?.Invoke(baseGold, bonusGold);
                Debug.Log($"[ComboKillSystem] Combo bonus! Base: {baseGold}, Bonus: +{bonusGold} (x{currentMultiplier:F2})");
            }

            return totalGold;
        }

        private void EndCombo()
        {
            if (currentCombo >= minKillsForCombo)
            {
                Debug.Log($"[ComboKillSystem] Combo ended at x{currentCombo}");
                OnComboEnded?.Invoke(currentCombo);
            }

            ResetCombo();
        }

        private void ResetCombo()
        {
            currentCombo = 0;
            currentMultiplier = 1f;
            comboTimer = 0f;
        }

        private bool IsOurKill(UnitController killedUnit, GameObject killer)
        {
            if (killer == null) return false;

            // Check if killer is on our team
            int playerLayer = LayerMask.NameToLayer("PlayerUnit");
            int enemyLayer = LayerMask.NameToLayer("Enemy");

            if (ownerTeam == GoldManager.Team.Player)
            {
                // We killed an enemy unit
                bool killerIsPlayer = killer.layer == playerLayer || killer.layer == 0;
                bool victimIsEnemy = killedUnit.gameObject.layer == enemyLayer;
                return killerIsPlayer && victimIsEnemy;
            }
            else
            {
                // AI killed a player unit
                bool killerIsEnemy = killer.layer == enemyLayer;
                bool victimIsPlayer = killedUnit.gameObject.layer == playerLayer || killedUnit.gameObject.layer == 0;
                return killerIsEnemy && victimIsPlayer;
            }
        }

        /// <summary>
        /// Get combo tier name for display
        /// </summary>
        public string GetComboTierName()
        {
            if (currentCombo < minKillsForCombo) return "";
            if (currentCombo < 5) return "COMBO!";
            if (currentCombo < 10) return "SUPER COMBO!";
            if (currentCombo < 15) return "MEGA COMBO!";
            if (currentCombo < 20) return "ULTRA COMBO!";
            return "LEGENDARY COMBO!";
        }

        private void OnDestroy()
        {
            if (entityRegistry != null)
            {
                entityRegistry.OnUnitDied -= OnUnitKilled;
            }
        }
    }
}
