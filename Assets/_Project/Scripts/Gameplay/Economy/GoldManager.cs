using System;
using TowerConquest.Debug;
using UnityEngine;

namespace TowerConquest.Gameplay
{
    /// <summary>
    /// Manages gold for a player or AI
    /// </summary>
    public class GoldManager : MonoBehaviour
    {
        public enum Team
        {
            Player,
            AI
        }

        [Header("Settings")]
        [SerializeField] private Team team;
        [SerializeField] private int startingGold = 500;

        [Header("Runtime")]
        [SerializeField] private int currentGold;

        public int CurrentGold => currentGold;
        public Team OwnerTeam => team;

        public event Action<int> OnGoldChanged;
        public event Action<int, int> OnGoldSpent; // (amount, remaining)
        public event Action<int, int> OnGoldEarned; // (amount, new total)

        private void Awake()
        {
            currentGold = startingGold;
        }

        /// <summary>
        /// Initialize gold manager with starting amount
        /// </summary>
        public void Initialize(int startGold, Team ownerTeam)
        {
            team = ownerTeam;
            startingGold = startGold;
            currentGold = startGold;
            OnGoldChanged?.Invoke(currentGold);
        }

        /// <summary>
        /// Check if player can afford a cost
        /// </summary>
        public bool CanAfford(int cost)
        {
            return currentGold >= cost;
        }

        /// <summary>
        /// Attempt to spend gold. Returns true if successful.
        /// </summary>
        public bool SpendGold(int amount)
        {
            if (amount < 0)
            {
                Log.Warning($"[GoldManager] Cannot spend negative gold: {amount}");
                return false;
            }

            if (!CanAfford(amount))
            {
                Log.Info($"[GoldManager] {team} cannot afford {amount} gold (have: {currentGold})");
                return false;
            }

            currentGold -= amount;
            OnGoldSpent?.Invoke(amount, currentGold);
            OnGoldChanged?.Invoke(currentGold);

            Log.Info($"[GoldManager] {team} spent {amount} gold (remaining: {currentGold})");
            return true;
        }

        /// <summary>
        /// Add gold to current amount
        /// </summary>
        public void AddGold(int amount)
        {
            if (amount < 0)
            {
                Log.Warning($"[GoldManager] Cannot add negative gold: {amount}");
                return;
            }

            currentGold += amount;
            OnGoldEarned?.Invoke(amount, currentGold);
            OnGoldChanged?.Invoke(currentGold);

            Log.Info($"[GoldManager] {team} earned {amount} gold (total: {currentGold})");
        }

        /// <summary>
        /// Reward gold for killing a unit
        /// </summary>
        public void RewardKill(string unitId, int goldReward)
        {
            AddGold(goldReward);
        }

        /// <summary>
        /// Reward gold for destroying a tower
        /// </summary>
        public void RewardDestruction(string towerId, int goldReward)
        {
            AddGold(goldReward);
        }

        /// <summary>
        /// Reset gold to starting amount
        /// </summary>
        public void Reset()
        {
            currentGold = startingGold;
            OnGoldChanged?.Invoke(currentGold);
        }

        /// <summary>
        /// Set gold to specific amount (for debugging/cheats)
        /// </summary>
        public void SetGold(int amount)
        {
            currentGold = Mathf.Max(0, amount);
            OnGoldChanged?.Invoke(currentGold);
        }

        #if UNITY_EDITOR
        [ContextMenu("Add 100 Gold")]
        private void DebugAdd100Gold()
        {
            AddGold(100);
        }

        [ContextMenu("Spend 50 Gold")]
        private void DebugSpend50Gold()
        {
            SpendGold(50);
        }
        #endif
    }
}
