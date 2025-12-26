using System;
using UnityEngine;
using TowerConquest.Data;

namespace TowerConquest.Gameplay
{
    /// <summary>
    /// Manages special abilities (civilization abilities)
    /// </summary>
    public class AbilityManager : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private string abilityId;
        [SerializeField] private GoldManager.Team ownerTeam;

        [Header("Runtime")]
        [SerializeField] private float cooldownDuration;
        [SerializeField] private float lastUseTime = -999f;
        [SerializeField] private bool isOnCooldown;

        private JsonDatabase database;
        private AbilityDefinition abilityDef;

        public string AbilityID => abilityId;
        public bool CanUse => !isOnCooldown;
        public float CooldownRemaining => Mathf.Max(0, (lastUseTime + cooldownDuration) - Time.time);

        public event Action<string> OnAbilityUsed;

        public void Initialize(string aId, GoldManager.Team team, JsonDatabase db)
        {
            abilityId = aId;
            ownerTeam = team;
            database = db;

            // TODO: Load ability from database when abilities are implemented
            // For now, use default cooldown
            cooldownDuration = 90f; // Default 90 seconds

            Debug.Log($"[AbilityManager] Initialized ability {abilityId} for team {team}, cooldown: {cooldownDuration}s");
        }

        private void Update()
        {
            // Update cooldown status
            if (isOnCooldown && Time.time >= lastUseTime + cooldownDuration)
            {
                isOnCooldown = false;
                Debug.Log($"[AbilityManager] Ability {abilityId} ready to use");
            }
        }

        /// <summary>
        /// Use the ability
        /// </summary>
        public bool UseAbility()
        {
            if (!CanUse)
            {
                Debug.LogWarning($"[AbilityManager] Cannot use ability (on cooldown)");
                return false;
            }

            Debug.Log($"[AbilityManager] Using ability {abilityId}");

            // TODO: Apply ability effects
            ApplyEffects();

            lastUseTime = Time.time;
            isOnCooldown = true;

            OnAbilityUsed?.Invoke(abilityId);

            return true;
        }

        private void ApplyEffects()
        {
            // TODO: Implement ability effects
            // For now, just log
            Debug.Log($"[AbilityManager] Applying effects for {abilityId}");

            // Example effects:
            // - Buff all friendly units
            // - Damage all enemies in area
            // - Heal structures
            // - Spawn temporary units
        }

        /// <summary>
        /// Get remaining cooldown time
        /// </summary>
        public float GetCooldownRemaining()
        {
            return CooldownRemaining;
        }

        #if UNITY_EDITOR
        [ContextMenu("Use Ability (Debug)")]
        private void DebugUseAbility()
        {
            UseAbility();
        }

        [ContextMenu("Reset Cooldown (Debug)")]
        private void DebugResetCooldown()
        {
            lastUseTime = -999f;
            isOnCooldown = false;
        }
        #endif
    }
}
