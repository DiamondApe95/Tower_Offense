using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TowerConquest.Core;
using TowerConquest.Data;
using TowerConquest.Gameplay.Entities;
using TowerConquest.Combat;

namespace TowerConquest.Gameplay
{
    /// <summary>
    /// Manages special abilities (civilization abilities)
    /// Fully implements ability effects: buffs, damage, spawning units, etc.
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

        [Header("References")]
        [SerializeField] private Transform spawnPoint;

        private JsonDatabase database;
        private AbilityDefinition abilityDef;
        private LiveBattleSpawnController spawnController;

        // Track active buffs for cleanup
        private List<ActiveBuff> activeBuffs = new List<ActiveBuff>();

        public string AbilityID => abilityId;
        public string AbilityName => abilityDef?.name ?? abilityId;
        public bool CanUse => !isOnCooldown;
        public float CooldownRemaining => Mathf.Max(0, (lastUseTime + cooldownDuration) - Time.time);
        public float CooldownDuration => cooldownDuration;

        public event Action<string> OnAbilityUsed;
        public event Action OnCooldownComplete;

        private class ActiveBuff
        {
            public UnitController unit;
            public string effectType;
            public float originalValue;
            public float endTime;
        }

        public void Initialize(string aId, GoldManager.Team team, JsonDatabase db)
        {
            abilityId = aId;
            ownerTeam = team;
            database = db;

            // Load ability from database
            abilityDef = database?.FindAbility(abilityId);
            if (abilityDef != null)
            {
                cooldownDuration = abilityDef.cooldown > 0 ? abilityDef.cooldown : 90f;
                Debug.Log($"[AbilityManager] Loaded ability '{abilityDef.name}' with cooldown {cooldownDuration}s");
            }
            else
            {
                cooldownDuration = 90f; // Default 90 seconds
                Debug.LogWarning($"[AbilityManager] Ability definition not found for '{abilityId}', using defaults");
            }

            // Find spawn controller for this team
            var spawners = FindObjectsByType<LiveBattleSpawnController>(FindObjectsSortMode.None);
            foreach (var spawner in spawners)
            {
                if (spawner.team == ownerTeam)
                {
                    spawnController = spawner;
                    spawnPoint = spawner.spawnPoint;
                    break;
                }
            }

            Debug.Log($"[AbilityManager] Initialized ability {abilityId} for team {team}, cooldown: {cooldownDuration}s");
        }

        private void Update()
        {
            // Update cooldown status
            if (isOnCooldown && Time.time >= lastUseTime + cooldownDuration)
            {
                isOnCooldown = false;
                OnCooldownComplete?.Invoke();
                Debug.Log($"[AbilityManager] Ability {abilityId} ready to use");
            }

            // Clean up expired buffs
            CleanupExpiredBuffs();
        }

        /// <summary>
        /// Use the ability
        /// </summary>
        public bool UseAbility()
        {
            if (!CanUse)
            {
                Debug.LogWarning($"[AbilityManager] Cannot use ability (on cooldown: {CooldownRemaining:F1}s remaining)");
                return false;
            }

            Debug.Log($"[AbilityManager] Using ability {abilityDef?.name ?? abilityId}");

            ApplyEffects();

            lastUseTime = Time.time;
            isOnCooldown = true;

            OnAbilityUsed?.Invoke(abilityId);

            return true;
        }

        private void ApplyEffects()
        {
            if (abilityDef == null || abilityDef.effects == null || abilityDef.effects.Length == 0)
            {
                Debug.LogWarning($"[AbilityManager] No effects defined for ability {abilityId}");
                return;
            }

            foreach (var effect in abilityDef.effects)
            {
                ApplyEffect(effect);
            }
        }

        private void ApplyEffect(AbilityDefinition.EffectDto effect)
        {
            if (effect == null) return;

            Debug.Log($"[AbilityManager] Applying effect: {effect.type} to {effect.target}");

            switch (effect.type.ToLower())
            {
                case "buff_armor":
                    ApplyArmorBuff(effect);
                    break;
                case "buff_damage":
                    ApplyDamageBuff(effect);
                    break;
                case "buff_speed":
                    ApplySpeedBuff(effect);
                    break;
                case "damage_aoe":
                    ApplyAoEDamage(effect);
                    break;
                case "heal":
                    ApplyHeal(effect);
                    break;
                case "spawn_units":
                    SpawnUnits(effect);
                    break;
                case "debuff_slow":
                    ApplySlowDebuff(effect);
                    break;
                default:
                    Debug.LogWarning($"[AbilityManager] Unknown effect type: {effect.type}");
                    break;
            }
        }

        private void ApplyArmorBuff(AbilityDefinition.EffectDto effect)
        {
            var targets = GetTargetUnits(effect.target);
            float bonusArmor = effect.value;
            float duration = effect.duration > 0 ? effect.duration : 10f;

            foreach (var unit in targets)
            {
                var health = unit.GetComponent<HealthComponent>();
                if (health != null)
                {
                    // Track the buff for later removal
                    activeBuffs.Add(new ActiveBuff
                    {
                        unit = unit,
                        effectType = "armor",
                        originalValue = health.Armor,
                        endTime = Time.time + duration
                    });

                    // Apply armor buff (additive)
                    health.SetArmor(Mathf.Clamp01(health.Armor + bonusArmor));
                    Debug.Log($"[AbilityManager] Applied +{bonusArmor * 100}% armor to {unit.UnitId} for {duration}s");
                }
            }
        }

        private void ApplyDamageBuff(AbilityDefinition.EffectDto effect)
        {
            var targets = GetTargetUnits(effect.target);
            float damageMultiplier = 1f + effect.value;
            float duration = effect.duration > 0 ? effect.duration : 10f;

            foreach (var unit in targets)
            {
                // Store original damage
                activeBuffs.Add(new ActiveBuff
                {
                    unit = unit,
                    effectType = "damage",
                    originalValue = unit.BaseDamage,
                    endTime = Time.time + duration
                });

                // Apply damage buff via component
                var damageBuff = unit.GetComponent<DamageBuff>();
                if (damageBuff == null)
                {
                    damageBuff = unit.gameObject.AddComponent<DamageBuff>();
                }
                damageBuff.ApplyBuff(damageMultiplier, duration);

                Debug.Log($"[AbilityManager] Applied +{effect.value * 100}% damage to {unit.UnitId} for {duration}s");
            }
        }

        private void ApplySpeedBuff(AbilityDefinition.EffectDto effect)
        {
            var targets = GetTargetUnits(effect.target);
            float speedMultiplier = 1f + effect.value;
            float duration = effect.duration > 0 ? effect.duration : 10f;

            foreach (var unit in targets)
            {
                var mover = unit.GetComponent<UnitMover>();
                if (mover != null)
                {
                    activeBuffs.Add(new ActiveBuff
                    {
                        unit = unit,
                        effectType = "speed",
                        originalValue = mover.moveSpeedMultiplier,
                        endTime = Time.time + duration
                    });

                    mover.SetSpeedMultiplier(mover.moveSpeedMultiplier * speedMultiplier);
                    Debug.Log($"[AbilityManager] Applied +{effect.value * 100}% speed to {unit.UnitId} for {duration}s");
                }
            }
        }

        private void ApplyAoEDamage(AbilityDefinition.EffectDto effect)
        {
            var targets = GetTargetUnits(effect.target);
            float damage = effect.value;

            foreach (var unit in targets)
            {
                var health = unit.GetComponent<HealthComponent>();
                if (health != null)
                {
                    health.TakeDamage(damage, "magic", gameObject);
                    Debug.Log($"[AbilityManager] Dealt {damage} AoE damage to {unit.UnitId}");
                }
            }
        }

        private void ApplyHeal(AbilityDefinition.EffectDto effect)
        {
            var targets = GetTargetUnits(effect.target);
            float healAmount = effect.value;

            foreach (var unit in targets)
            {
                var health = unit.GetComponent<HealthComponent>();
                if (health != null)
                {
                    health.Heal(healAmount);
                    Debug.Log($"[AbilityManager] Healed {unit.UnitId} for {healAmount}");
                }
            }
        }

        private void SpawnUnits(AbilityDefinition.EffectDto effect)
        {
            string unitId = effect.spawnUnitId;
            int count = effect.spawnCount > 0 ? effect.spawnCount : 1;

            if (string.IsNullOrEmpty(unitId))
            {
                Debug.LogWarning("[AbilityManager] SpawnUnits effect has no unit ID");
                return;
            }

            Vector3 spawnPos = spawnPoint != null ? spawnPoint.position : transform.position;

            for (int i = 0; i < count; i++)
            {
                // Offset spawn position slightly to avoid stacking
                Vector3 offset = new Vector3(
                    UnityEngine.Random.Range(-1f, 1f),
                    0,
                    UnityEngine.Random.Range(-1f, 1f)
                );

                if (spawnController != null)
                {
                    var unit = spawnController.SpawnUnit(unitId);
                    if (unit != null)
                    {
                        unit.transform.position = spawnPos + offset;
                        Debug.Log($"[AbilityManager] Spawned {unitId} via ability");
                    }
                }
                else
                {
                    // Fallback: Create basic unit
                    CreateFallbackUnit(unitId, spawnPos + offset);
                }
            }

            Debug.Log($"[AbilityManager] Spawned {count} {unitId} units");
        }

        private void CreateFallbackUnit(string unitId, Vector3 position)
        {
            GameObject unitObj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            unitObj.name = $"{unitId}_Spawned";
            unitObj.transform.position = position;
            unitObj.transform.localScale = Vector3.one * 0.5f;

            var controller = unitObj.AddComponent<UnitController>();

            // Find target base
            var bases = FindObjectsByType<BaseController>(FindObjectsSortMode.None);
            BaseController targetBase = null;
            foreach (var baseCtrl in bases)
            {
                bool isEnemyBase = (ownerTeam == GoldManager.Team.Player && baseCtrl.gameObject.name.Contains("Enemy")) ||
                                   (ownerTeam == GoldManager.Team.AI && baseCtrl.gameObject.name.Contains("Player"));
                if (isEnemyBase)
                {
                    targetBase = baseCtrl;
                    break;
                }
            }

            var path = new List<Vector3>();
            if (targetBase != null)
            {
                path.Add(targetBase.transform.position);
            }

            controller.Initialize(unitId, path, targetBase);
        }

        private void ApplySlowDebuff(AbilityDefinition.EffectDto effect)
        {
            var targets = GetTargetUnits(effect.target);
            float slowAmount = effect.value; // e.g., 0.5 = 50% slow
            float duration = effect.duration > 0 ? effect.duration : 5f;

            foreach (var unit in targets)
            {
                var mover = unit.GetComponent<UnitMover>();
                if (mover != null)
                {
                    activeBuffs.Add(new ActiveBuff
                    {
                        unit = unit,
                        effectType = "slow",
                        originalValue = mover.moveSpeedMultiplier,
                        endTime = Time.time + duration
                    });

                    mover.SetSpeedMultiplier(mover.moveSpeedMultiplier * (1f - slowAmount));
                    Debug.Log($"[AbilityManager] Applied {slowAmount * 100}% slow to {unit.UnitId} for {duration}s");
                }
            }
        }

        private List<UnitController> GetTargetUnits(string targetType)
        {
            var result = new List<UnitController>();
            var allUnits = FindObjectsByType<UnitController>(FindObjectsSortMode.None);

            foreach (var unit in allUnits)
            {
                if (unit == null || unit.IsDead) continue;

                bool isFriendly = IsFriendlyUnit(unit);

                switch (targetType?.ToLower())
                {
                    case "all_friendly_units":
                    case "friendly":
                        if (isFriendly) result.Add(unit);
                        break;
                    case "all_enemy_units":
                    case "enemy":
                        if (!isFriendly) result.Add(unit);
                        break;
                    case "all":
                        result.Add(unit);
                        break;
                    default:
                        // Default to friendly if not specified
                        if (isFriendly) result.Add(unit);
                        break;
                }
            }

            return result;
        }

        private bool IsFriendlyUnit(UnitController unit)
        {
            // Determine if unit is friendly based on layer or team
            int playerLayer = LayerMask.NameToLayer("PlayerUnit");
            int enemyLayer = LayerMask.NameToLayer("Enemy");

            if (ownerTeam == GoldManager.Team.Player)
            {
                return unit.gameObject.layer == playerLayer || unit.gameObject.layer == 0;
            }
            else
            {
                return unit.gameObject.layer == enemyLayer;
            }
        }

        private void CleanupExpiredBuffs()
        {
            for (int i = activeBuffs.Count - 1; i >= 0; i--)
            {
                var buff = activeBuffs[i];

                if (Time.time >= buff.endTime)
                {
                    RemoveBuff(buff);
                    activeBuffs.RemoveAt(i);
                }
                else if (buff.unit == null || buff.unit.IsDead)
                {
                    activeBuffs.RemoveAt(i);
                }
            }
        }

        private void RemoveBuff(ActiveBuff buff)
        {
            if (buff.unit == null) return;

            switch (buff.effectType)
            {
                case "armor":
                    var health = buff.unit.GetComponent<HealthComponent>();
                    if (health != null)
                    {
                        health.SetArmor(buff.originalValue);
                    }
                    break;
                case "speed":
                case "slow":
                    var mover = buff.unit.GetComponent<UnitMover>();
                    if (mover != null)
                    {
                        mover.SetSpeedMultiplier(buff.originalValue);
                    }
                    break;
            }

            Debug.Log($"[AbilityManager] Removed {buff.effectType} buff from {buff.unit?.UnitId}");
        }

        /// <summary>
        /// Get remaining cooldown time
        /// </summary>
        public float GetCooldownRemaining()
        {
            return CooldownRemaining;
        }

        /// <summary>
        /// Get cooldown progress (0 = ready, 1 = just used)
        /// </summary>
        public float GetCooldownProgress()
        {
            if (!isOnCooldown) return 0f;
            return CooldownRemaining / cooldownDuration;
        }

        private void OnDestroy()
        {
            // Clean up all remaining buffs
            foreach (var buff in activeBuffs)
            {
                RemoveBuff(buff);
            }
            activeBuffs.Clear();
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

    /// <summary>
    /// Component to track damage buff on a unit
    /// </summary>
    public class DamageBuff : MonoBehaviour
    {
        private float multiplier = 1f;
        private float endTime;

        public float Multiplier => multiplier;

        public void ApplyBuff(float mult, float duration)
        {
            multiplier = mult;
            endTime = Time.time + duration;
        }

        private void Update()
        {
            if (Time.time >= endTime)
            {
                multiplier = 1f;
                Destroy(this);
            }
        }
    }
}
