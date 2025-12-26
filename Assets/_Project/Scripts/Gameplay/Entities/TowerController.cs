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

        [Header("Economy")]
        public int buildCost = 50;
        public int upgradeCost = 30;

        [Header("Team")]
        public GoldManager.Team ownerTeam = GoldManager.Team.Player;

        public float EstimatedDps { get; private set; }
        public int BuildCost => buildCost;
        public int UpgradeCost => upgradeCost;

        private float scanTimer;
        private float attackTimer;
        private UnitController currentTarget;
        private readonly EffectResolver effectResolver = new EffectResolver();
        private EntityRegistry entityRegistry;

        // Cached layer masks for efficient targeting
        private int enemyLayerMask;
        private bool layerMaskInitialized;

        private void Awake()
        {
            UpdateDpsCache();
            ServiceLocator.TryGet(out entityRegistry);
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

        private void Update()
        {
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

                Log.Info($"Tower hit unit {currentTarget.UnitId}");
            }
        }

        private void AcquireTarget()
        {
            // Initialize layer mask if needed
            if (!layerMaskInitialized)
            {
                InitializeLayerMask();
            }

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

            currentTarget = closest;
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
