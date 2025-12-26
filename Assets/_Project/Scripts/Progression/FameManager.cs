using System;
using UnityEngine;

namespace TowerConquest.Progression
{
    /// <summary>
    /// Manages fame (progression currency) for the player
    /// </summary>
    public class FameManager : MonoBehaviour
    {
        [Header("Runtime")]
        [SerializeField] private int totalFame = 0;

        public int TotalFame => totalFame;

        /// <summary>
        /// Alias for TotalFame (for compatibility)
        /// </summary>
        public int CurrentFame => totalFame;

        public event Action<int> OnFameChanged;
        public event Action<int, int> OnFameEarned; // (amount, new total)
        public event Action<int, int> OnFameSpent; // (amount, remaining)

        private void Awake()
        {
            // Load from save
            LoadFame();
        }

        /// <summary>
        /// Add fame to total
        /// </summary>
        public void AddFame(int amount)
        {
            if (amount < 0)
            {
                Debug.LogWarning($"[FameManager] Cannot add negative fame: {amount}");
                return;
            }

            totalFame += amount;
            OnFameEarned?.Invoke(amount, totalFame);
            OnFameChanged?.Invoke(totalFame);

            Debug.Log($"[FameManager] Earned {amount} fame (total: {totalFame})");

            SaveFame();
        }

        /// <summary>
        /// Spend fame
        /// </summary>
        public bool SpendFame(int amount)
        {
            if (amount < 0)
            {
                Debug.LogWarning($"[FameManager] Cannot spend negative fame: {amount}");
                return false;
            }

            if (totalFame < amount)
            {
                Debug.LogWarning($"[FameManager] Not enough fame (have: {totalFame}, need: {amount})");
                return false;
            }

            totalFame -= amount;
            OnFameSpent?.Invoke(amount, totalFame);
            OnFameChanged?.Invoke(totalFame);

            Debug.Log($"[FameManager] Spent {amount} fame (remaining: {totalFame})");

            SaveFame();
            return true;
        }

        /// <summary>
        /// Check if player can afford cost
        /// </summary>
        public bool CanAfford(int cost)
        {
            return totalFame >= cost;
        }

        /// <summary>
        /// Reward fame for completing a level
        /// </summary>
        public void RewardLevelComplete(string levelId, bool victory, int baseReward, int bonusReward = 0)
        {
            int totalReward = victory ? baseReward : (baseReward / 5); // Small reward for defeat
            totalReward += bonusReward;

            AddFame(totalReward);

            Debug.Log($"[FameManager] Level {levelId} complete. Victory: {victory}, Reward: {totalReward}");
        }

        private void LoadFame()
        {
            // TODO: Load from SaveManager/PlayerProgress
            totalFame = PlayerPrefs.GetInt("PlayerFame", 0);
            Debug.Log($"[FameManager] Loaded fame: {totalFame}");
        }

        private void SaveFame()
        {
            // TODO: Save via SaveManager/PlayerProgress
            PlayerPrefs.SetInt("PlayerFame", totalFame);
            PlayerPrefs.Save();
        }

        #if UNITY_EDITOR
        [ContextMenu("Add 100 Fame")]
        private void DebugAdd100Fame()
        {
            AddFame(100);
        }

        [ContextMenu("Spend 50 Fame")]
        private void DebugSpend50Fame()
        {
            SpendFame(50);
        }

        [ContextMenu("Reset Fame")]
        private void DebugResetFame()
        {
            totalFame = 0;
            SaveFame();
            OnFameChanged?.Invoke(totalFame);
        }
        #endif
    }
}
