using TowerConquest.Combat;
using TowerConquest.Debug;
using TowerConquest.Core;
using TowerConquest.Data;
using UnityEngine;

namespace TowerConquest.Gameplay.Entities
{
    public class TowerController : MonoBehaviour
    {
        [Header("Tower Info")]
        public string towerId = "tower_basic";

        [Header("Tower Stats")]
        public float range = 6f;
        public float damage = 20f;
        public float attacksPerSecond = 1f;
        public TowerDefinition.EffectDto[] effects;

        [Header("Health")]
        public float maxHp = 500f;
        public float armor = 0.1f;

        [Header("Economy")]
        public int buildCost = 50;
        public int upgradeCost = 30;

        [Header("Team")]
        public GoldManager.Team ownerTeam = GoldManager.Team.Player;

        public float EstimatedDps { get; private set; }
        public int BuildCost => buildCost;
        public int UpgradeCost => upgradeCost;
        public bool IsDestroyed { get; private set; }

        private float scanTimer;
        private float attackTimer;
        private Transform currentTarget;
        private readonly EffectResolver effectResolver = new EffectResolver();
        private EntityRegistry entityRegistry;
        private HealthComponent healthComponent;
        private TargetingSystem targetingSystem;

        // Cached layer masks for efficient targeting
        private int enemyLayerMask;
        private bool layerMaskInitialized;

        private void Awake()
        {
            UpdateDpsCache();
            ServiceLocator.TryGet(out entityRegistry);
            targetingSystem = new TargetingSystem();

            // Initialize HealthComponent
            healthComponent = GetComponent<HealthComponent>();
            if (healthComponent == null)
            {
                healthComponent = gameObject.AddComponent<HealthComponent>();
            }
            healthComponent.Initialize(maxHp, armor);
            healthComponent.OnDeath += HandleDeath;
        }

        private void OnEnable()
        {
            if (ServiceLocator.TryGet(out EntityRegistry registry))
            {
                registry.RegisterTower(this);
            }
        }

        private void OnDisable()
        {
            if (ServiceLocator.TryGet(out EntityRegistry registry))
            {
                registry.UnregisterTower(this);
            }
        }

        private void OnDestroy()
        {
            if (healthComponent != null)
            {
                healthComponent.OnDeath -= HandleDeath;
            }
        }

        private void HandleDeath()
        {
            if (IsDestroyed) return;

            IsDestroyed = true;
            Log.Info($"[TowerController] Tower {towerId} destroyed!");

            // Unregister before destruction
            if (ServiceLocator.TryGet(out EntityRegistry registry))
            {
                registry.UnregisterTower(this);
            }

            // Destroy the tower game object
            Destroy(gameObject);
        }

        private void Update()
        {
            if (IsDestroyed) return;

            scanTimer += Time.deltaTime;
            if (scanTimer >= 0.25f)
            {
                scanTimer = 0f;
                AcquireTarget();
            }

            if (currentTarget == null)
            {
                return;
            }

            attackTimer += Time.deltaTime;
            float attackInterval = attacksPerSecond > 0f ? 1f / attacksPerSecond : 0.25f;
            if (attackTimer >= attackInterval)
            {
                attackTimer = 0f;
                DamageSystem.Apply(currentTarget.gameObject, damage);
                if (effects != null && effects.Length > 0)
                {
                    effectResolver.ApplyEffects(gameObject, currentTarget.gameObject, effects);
                }

                // Notify the unit that this tower is attacking it (for priority targeting)
                var unitCombat = currentTarget.GetComponent<UnitCombat>();
                if (unitCombat != null)
                {
                    unitCombat.RegisterAttacker(gameObject);
                }

                var unit = currentTarget.GetComponent<UnitController>();
                Log.Info($"Tower hit target {(unit != null ? unit.UnitId : currentTarget.name)}");
            }
        }

        private void AcquireTarget()
        {
            // Initialize layer mask if needed
            if (!layerMaskInitialized)
            {
                InitializeLayerMask();
            }

            // Target prioritization: 1. Units, 2. Towers, 3. Base
            // Within each category, prioritize by distance (nearest first)

            // 1. Try to find enemy units first
            Transform target = FindEnemyUnits();

            // 2. If no units in range, target enemy towers
            if (target == null)
            {
                target = FindEnemyTowers();
            }

            // 3. If no towers in range, target enemy base
            if (target == null)
            {
                target = FindEnemyBase();
            }

            currentTarget = target;
        }

        private Transform FindEnemyUnits()
        {
            UnitController[] units;
            if (entityRegistry != null)
            {
                units = entityRegistry.GetAllUnits();
            }
            else
            {
                units = FindObjectsByType<UnitController>(FindObjectsSortMode.None);
            }

            UnitController closest = null;
            float closestDistance = float.MaxValue;

            Vector3 towerPosition = transform.position;
            foreach (UnitController unit in units)
            {
                if (unit == null || !unit.IsAlive)
                {
                    continue;
                }

                // Check if this unit is an enemy (on the opposite team)
                if (!IsEnemy(unit.gameObject))
                {
                    continue;
                }

                float distance = Vector3.Distance(towerPosition, unit.transform.position);
                if (distance <= range && distance < closestDistance)
                {
                    closest = unit;
                    closestDistance = distance;
                }
            }

            return closest != null ? closest.transform : null;
        }

        private Transform FindEnemyTowers()
        {
            TowerController[] towers;
            if (entityRegistry != null)
            {
                towers = entityRegistry.GetAllTowers();
            }
            else
            {
                towers = FindObjectsByType<TowerController>(FindObjectsSortMode.None);
            }

            TowerController closest = null;
            float closestDistance = float.MaxValue;

            Vector3 towerPosition = transform.position;
            foreach (TowerController tower in towers)
            {
                if (tower == null || tower.IsDestroyed || tower == this)
                {
                    continue;
                }

                // Check if this tower is an enemy (on the opposite team)
                if (tower.ownerTeam == this.ownerTeam)
                {
                    continue;
                }

                float distance = Vector3.Distance(towerPosition, tower.transform.position);
                if (distance <= range && distance < closestDistance)
                {
                    closest = tower;
                    closestDistance = distance;
                }
            }

            return closest != null ? closest.transform : null;
        }

        private Transform FindEnemyBase()
        {
            BaseController[] bases = FindObjectsByType<BaseController>(FindObjectsSortMode.None);

            BaseController enemyBase = null;
            float closestDistance = float.MaxValue;

            Vector3 towerPosition = transform.position;
            foreach (BaseController baseCtrl in bases)
            {
                if (baseCtrl == null)
                {
                    continue;
                }

                // Check if this is an enemy base
                bool isEnemyBase = false;
                if (ownerTeam == GoldManager.Team.Player)
                {
                    isEnemyBase = baseCtrl.CompareTag("EnemyBase") || baseCtrl.gameObject.name.Contains("Enemy");
                }
                else
                {
                    isEnemyBase = baseCtrl.CompareTag("PlayerBase") || baseCtrl.gameObject.name.Contains("Player");
                }

                if (!isEnemyBase)
                {
                    continue;
                }

                float distance = Vector3.Distance(towerPosition, baseCtrl.transform.position);
                if (distance <= range && distance < closestDistance)
                {
                    enemyBase = baseCtrl;
                    closestDistance = distance;
                }
            }

            return enemyBase != null ? enemyBase.transform : null;
        }

        private void InitializeLayerMask()
        {
            // Player towers target enemy units, AI towers target player units
            if (ownerTeam == GoldManager.Team.Player)
            {
                int enemyLayer = LayerMask.NameToLayer("Enemy");
                if (enemyLayer >= 0)
                {
                    enemyLayerMask = 1 << enemyLayer;
                }
            }
            else
            {
                int playerLayer = LayerMask.NameToLayer("PlayerUnit");
                if (playerLayer >= 0)
                {
                    enemyLayerMask = 1 << playerLayer;
                }
            }
            layerMaskInitialized = true;
        }

        private bool IsEnemy(GameObject target)
        {
            if (target == null) return false;

            // Check layer
            if (enemyLayerMask != 0)
            {
                return ((1 << target.layer) & enemyLayerMask) != 0;
            }

            // Fallback: check tag
            if (ownerTeam == GoldManager.Team.Player)
            {
                return target.CompareTag("Enemy") || target.CompareTag("EnemyUnit");
            }
            else
            {
                return target.CompareTag("Player") || target.CompareTag("PlayerUnit");
            }
        }

        public void UpdateDpsCache()
        {
            EstimatedDps = Mathf.Max(0f, damage) * Mathf.Max(0.1f, attacksPerSecond);
        }
    }
}
