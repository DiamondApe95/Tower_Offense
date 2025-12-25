using System;
using UnityEngine;

namespace TowerConquest.Core
{
    /// <summary>
    /// GameConfig: Globale Spielkonfiguration für Balancing und Gameplay-Parameter.
    /// Kann zur Laufzeit angepasst werden.
    /// </summary>
    [Serializable]
    public class GameConfig
    {
        private static GameConfig instance;
        public static GameConfig Instance => instance ??= new GameConfig();

        // =====================
        // WAVE SETTINGS
        // =====================

        [Header("Wave Settings")]
        public int defaultMaxWaves = 5;
        public float waveTransitionDelay = 2f;
        public bool autoStartFirstWave = false;
        public float autoStartDelay = 3f;
        public bool autoStartNextWave = false;

        // =====================
        // CARD SYSTEM
        // =====================

        [Header("Card System")]
        public int defaultHandSize = 5;
        public int maxHandSize = 7;
        public int startingEnergy = 10;
        public int energyPerWave = 10;
        public int maxEnergy = 20;
        public float cardDrawDelay = 0.3f;
        public bool allowMidWaveSpawns = false;

        // =====================
        // HERO SETTINGS
        // =====================

        [Header("Hero Settings")]
        public int heroEveryNWaves = 5;
        public float heroBaseHp = 300f;
        public float heroAuraRadius = 5f;
        public float heroAbilityCooldown = 30f;

        // =====================
        // COMBAT SETTINGS
        // =====================

        [Header("Combat Settings")]
        public float baseDamageMultiplier = 1f;
        public float baseArmorMultiplier = 1f;
        public float criticalHitChance = 0.05f;
        public float criticalHitMultiplier = 2f;
        public float minDamage = 1f;

        // =====================
        // STATUS EFFECTS
        // =====================

        [Header("Status Effects")]
        public float maxSlowPercent = 0.9f;
        public float burnTickInterval = 1f;
        public float armorShredCap = 0.5f;
        public int maxStacksPerEffect = 5;

        // =====================
        // UNIT SETTINGS
        // =====================

        [Header("Unit Settings")]
        public float defaultMoveSpeed = 3f;
        public float defaultAttackRange = 1.5f;
        public float defaultAttacksPerSecond = 1f;
        public float unitSpawnInterval = 0.5f;

        // =====================
        // TOWER SETTINGS
        // =====================

        [Header("Tower Settings")]
        public int baseTowerCost = 50;
        public float towerSellMultiplier = 0.6f;
        public float towerScanInterval = 0.25f;
        public float defaultTowerRange = 6f;

        // =====================
        // ECONOMY
        // =====================

        [Header("Economy")]
        public int startingGold = 100;
        public int goldPerKill = 5;
        public int goldPerWave = 20;
        public float goldMultiplier = 1f;

        // =====================
        // SPEED SETTINGS
        // =====================

        [Header("Speed Settings")]
        public float[] availableSpeedModes = { 1f, 2f, 3f };
        public float maxGameSpeed = 3f;

        // =====================
        // DIFFICULTY MODIFIERS
        // =====================

        [Header("Difficulty Modifiers")]
        public float easyDamageMultiplier = 0.8f;
        public float easyHealthMultiplier = 0.8f;
        public float normalDamageMultiplier = 1f;
        public float normalHealthMultiplier = 1f;
        public float hardDamageMultiplier = 1.2f;
        public float hardHealthMultiplier = 1.2f;
        public float insaneDamageMultiplier = 1.5f;
        public float insaneHealthMultiplier = 1.5f;

        // =====================
        // VISUAL SETTINGS
        // =====================

        [Header("Visual Settings")]
        public bool showDamageNumbers = true;
        public bool showHealthBars = true;
        public bool showRangeIndicators = true;
        public float healthBarFadeDelay = 2f;
        public float damageNumberDuration = 1f;

        // =====================
        // POOLING
        // =====================

        [Header("Object Pooling")]
        public int unitPoolSize = 50;
        public int projectilePoolSize = 30;
        public int effectPoolSize = 20;

        // =====================
        // DEBUG
        // =====================

        [Header("Debug")]
        public bool debugMode = false;
        public bool showPathGizmos = false;
        public bool logCombatEvents = false;
        public bool invincibleUnits = false;
        public bool infiniteEnergy = false;
        public bool instantKill = false;

        /// <summary>
        /// Wendet Standard-Werte an.
        /// </summary>
        public void ApplyDefaults()
        {
            defaultMaxWaves = 5;
            waveTransitionDelay = 2f;
            autoStartFirstWave = false;
            autoStartDelay = 3f;
            autoStartNextWave = false;

            defaultHandSize = 5;
            maxHandSize = 7;
            startingEnergy = 10;
            energyPerWave = 10;
            maxEnergy = 20;
            cardDrawDelay = 0.3f;
            allowMidWaveSpawns = false;

            heroEveryNWaves = 5;
            heroBaseHp = 300f;
            heroAuraRadius = 5f;
            heroAbilityCooldown = 30f;

            baseDamageMultiplier = 1f;
            baseArmorMultiplier = 1f;
            criticalHitChance = 0.05f;
            criticalHitMultiplier = 2f;
            minDamage = 1f;

            maxSlowPercent = 0.9f;
            burnTickInterval = 1f;
            armorShredCap = 0.5f;
            maxStacksPerEffect = 5;

            defaultMoveSpeed = 3f;
            defaultAttackRange = 1.5f;
            defaultAttacksPerSecond = 1f;
            unitSpawnInterval = 0.5f;

            baseTowerCost = 50;
            towerSellMultiplier = 0.6f;
            towerScanInterval = 0.25f;
            defaultTowerRange = 6f;

            startingGold = 100;
            goldPerKill = 5;
            goldPerWave = 20;
            goldMultiplier = 1f;

            availableSpeedModes = new float[] { 1f, 2f, 3f };
            maxGameSpeed = 3f;

            showDamageNumbers = true;
            showHealthBars = true;
            showRangeIndicators = true;
            healthBarFadeDelay = 2f;
            damageNumberDuration = 1f;

            unitPoolSize = 50;
            projectilePoolSize = 30;
            effectPoolSize = 20;

            debugMode = false;
            showPathGizmos = false;
            logCombatEvents = false;
            invincibleUnits = false;
            infiniteEnergy = false;
            instantKill = false;

            UnityEngine.Debug.Log("GameConfig: Defaults applied.");
        }

        /// <summary>
        /// Wendet Schwierigkeitsmodifikatoren an.
        /// </summary>
        public void ApplyDifficulty(Difficulty difficulty)
        {
            switch (difficulty)
            {
                case Difficulty.Easy:
                    baseDamageMultiplier = easyDamageMultiplier;
                    baseArmorMultiplier = 1f / easyHealthMultiplier;
                    startingGold = 150;
                    energyPerWave = 12;
                    break;

                case Difficulty.Normal:
                    baseDamageMultiplier = normalDamageMultiplier;
                    baseArmorMultiplier = 1f;
                    startingGold = 100;
                    energyPerWave = 10;
                    break;

                case Difficulty.Hard:
                    baseDamageMultiplier = hardDamageMultiplier;
                    baseArmorMultiplier = 1f / hardHealthMultiplier;
                    startingGold = 80;
                    energyPerWave = 8;
                    break;

                case Difficulty.Insane:
                    baseDamageMultiplier = insaneDamageMultiplier;
                    baseArmorMultiplier = 1f / insaneHealthMultiplier;
                    startingGold = 60;
                    energyPerWave = 6;
                    break;
            }

            UnityEngine.Debug.Log($"GameConfig: Applied {difficulty} difficulty.");
        }

        /// <summary>
        /// Gibt den Schaden nach Multiplikatoren zurück.
        /// </summary>
        public float CalculateDamage(float baseDamage, bool isCritical = false)
        {
            float damage = baseDamage * baseDamageMultiplier;

            if (isCritical)
            {
                damage *= criticalHitMultiplier;
            }

            return Mathf.Max(minDamage, damage);
        }

        /// <summary>
        /// Prüft auf kritischen Treffer.
        /// </summary>
        public bool RollCritical()
        {
            return UnityEngine.Random.value < criticalHitChance;
        }

        /// <summary>
        /// Gibt den Verkaufswert eines Turms zurück.
        /// </summary>
        public int CalculateSellValue(int buildCost)
        {
            return Mathf.RoundToInt(buildCost * towerSellMultiplier);
        }

        /// <summary>
        /// Gibt die Energie für eine Welle zurück.
        /// </summary>
        public int GetWaveEnergy(int waveIndex)
        {
            if (infiniteEnergy) return 999;

            // Leicht ansteigend pro Welle
            return energyPerWave + (waveIndex / 2);
        }

        /// <summary>
        /// Gibt das Gold für einen Kill zurück.
        /// </summary>
        public int GetKillGold(float enemyValue = 1f)
        {
            return Mathf.RoundToInt(goldPerKill * enemyValue * goldMultiplier);
        }

        /// <summary>
        /// Gibt das Gold pro Welle zurück.
        /// </summary>
        public int GetWaveGold(int waveIndex)
        {
            return Mathf.RoundToInt(goldPerWave * goldMultiplier);
        }

        /// <summary>
        /// Validiert die aktuelle Konfiguration.
        /// </summary>
        public bool Validate()
        {
            bool valid = true;

            if (defaultHandSize <= 0)
            {
                UnityEngine.Debug.LogError("GameConfig: defaultHandSize must be positive.");
                valid = false;
            }

            if (defaultMaxWaves <= 0)
            {
                UnityEngine.Debug.LogError("GameConfig: defaultMaxWaves must be positive.");
                valid = false;
            }

            if (availableSpeedModes == null || availableSpeedModes.Length == 0)
            {
                UnityEngine.Debug.LogError("GameConfig: availableSpeedModes must have at least one value.");
                valid = false;
            }

            return valid;
        }

        public enum Difficulty
        {
            Easy,
            Normal,
            Hard,
            Insane
        }
    }
}
