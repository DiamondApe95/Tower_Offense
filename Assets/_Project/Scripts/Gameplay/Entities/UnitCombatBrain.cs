using System.Collections.Generic;
using TowerConquest.Debug;
using TowerConquest.Combat;
using UnityEngine;

namespace TowerConquest.Gameplay.Entities
{
    /// <summary>
    /// AI component for units that handles targeting priorities
    /// Priority order: Enemy Units > Towers (when attacked) > Construction Sites > Enemy Base
    /// </summary>
    public class UnitCombatBrain : MonoBehaviour
    {
        public enum TargetPriority
        {
            EnemyUnits = 0,      // Highest priority
            Towers = 1,          // When attacked by tower
            ConstructionSites = 2,
            EnemyBase = 3        // Lowest priority (default target)
        }

        [Header("Detection Settings")]
        [SerializeField] private float detectionRange = 8f;
        [SerializeField] private float attackRange = 2f;
        [SerializeField] private float attackCooldown = 1f;

        [Header("Layer Masks")]
        [SerializeField] private LayerMask enemyUnitLayer;
        [SerializeField] private LayerMask towerLayer;
        [SerializeField] private LayerMask constructionSiteLayer;

        [Header("State")]
        [SerializeField] private Transform currentTarget;
        [SerializeField] private TargetPriority currentPriority = TargetPriority.EnemyBase;
        [SerializeField] private bool isEngagedInCombat = false;
        [SerializeField] private bool wasAttackedByTower = false;

        private UnitController unitController;
        private UnitMover unitMover;
        private HealthComponent healthComponent;
        private BaseController targetBase;
        private GoldManager.Team team;
        private float lastAttackTime;
        private float baseDamage = 50f;
        private Transform lastTowerAttacker;

        public bool IsEngagedInCombat => isEngagedInCombat;
        public Transform CurrentTarget => currentTarget;
        public TargetPriority CurrentPriority => currentPriority;

        private void Awake()
        {
            unitController = GetComponent<UnitController>();
            unitMover = GetComponent<UnitMover>();
            healthComponent = GetComponent<HealthComponent>();

            if (healthComponent != null)
            {
                healthComponent.OnDamaged += OnDamageTaken;
            }
        }

        public void Initialize(BaseController enemyBase, GoldManager.Team unitTeam, float damage)
        {
            targetBase = enemyBase;
            team = unitTeam;
            baseDamage = damage;

            // Setup layer masks based on team
            if (team == GoldManager.Team.Player)
            {
                enemyUnitLayer = LayerMask.GetMask("Enemy", "EnemyUnit");
                towerLayer = LayerMask.GetMask("EnemyTower", "Tower");
            }
            else
            {
                enemyUnitLayer = LayerMask.GetMask("Player", "PlayerUnit");
                towerLayer = LayerMask.GetMask("PlayerTower", "Tower");
            }

            constructionSiteLayer = LayerMask.GetMask("ConstructionSite");
        }

        private void Update()
        {
            if (unitController == null || unitController.IsDead) return;

            // Find best target based on priorities
            FindBestTarget();

            // Handle combat
            if (isEngagedInCombat && currentTarget != null)
            {
                EngageTarget();
            }
        }

        private void FindBestTarget()
        {
            Transform newTarget = null;
            TargetPriority newPriority = TargetPriority.EnemyBase;

            // Priority 1: Check for enemy units in range
            var enemyUnit = FindNearestEnemy(detectionRange);
            if (enemyUnit != null)
            {
                newTarget = enemyUnit;
                newPriority = TargetPriority.EnemyUnits;
                isEngagedInCombat = true;
            }
            // Priority 2: If attacked by tower and no enemy units nearby, attack the tower
            else if (wasAttackedByTower && lastTowerAttacker != null)
            {
                float distToTower = Vector3.Distance(transform.position, lastTowerAttacker.position);
                if (distToTower <= detectionRange * 1.5f)
                {
                    newTarget = lastTowerAttacker;
                    newPriority = TargetPriority.Towers;
                    isEngagedInCombat = true;
                }
            }
            // Priority 3: Check for construction sites in range
            else
            {
                var constructionSite = FindNearestConstructionSite(detectionRange);
                if (constructionSite != null)
                {
                    newTarget = constructionSite;
                    newPriority = TargetPriority.ConstructionSites;
                    isEngagedInCombat = true;
                }
                // Priority 4: Default to enemy base
                else
                {
                    if (targetBase != null)
                    {
                        newTarget = targetBase.transform;
                        newPriority = TargetPriority.EnemyBase;
                    }
                    isEngagedInCombat = false;
                    wasAttackedByTower = false;
                }
            }

            currentTarget = newTarget;
            currentPriority = newPriority;

            // Update movement
            UpdateMovement();
        }

        private void UpdateMovement()
        {
            if (unitMover == null) return;

            if (isEngagedInCombat && currentTarget != null)
            {
                // Stop moving when in combat range
                float distToTarget = Vector3.Distance(transform.position, currentTarget.position);
                if (distToTarget <= attackRange)
                {
                    unitMover.Pause();
                }
                else
                {
                    // Move towards target if not in range
                    unitMover.Resume();
                }
            }
            else
            {
                // Resume normal movement towards base
                unitMover.Resume();
            }
        }

        private void EngageTarget()
        {
            if (currentTarget == null) return;

            float distance = Vector3.Distance(transform.position, currentTarget.position);

            if (distance <= attackRange)
            {
                // Attack if cooldown is ready
                if (Time.time >= lastAttackTime + attackCooldown)
                {
                    Attack(currentTarget);
                    lastAttackTime = Time.time;
                }

                // Face target
                Vector3 lookDir = (currentTarget.position - transform.position).normalized;
                lookDir.y = 0;
                if (lookDir.sqrMagnitude > 0.01f)
                {
                    transform.forward = lookDir;
                }
            }
        }

        private void Attack(Transform target)
        {
            if (target == null) return;

            // Try to damage the target
            var health = target.GetComponent<HealthComponent>();
            if (health != null)
            {
                health.TakeDamage(baseDamage, "Physical", gameObject);
                Log.Info($"[UnitCombatBrain] {gameObject.name} attacked {target.name} for {baseDamage} damage");
            }

            // Also check for specific component types
            var baseCtrl = target.GetComponent<BaseController>();
            if (baseCtrl != null)
            {
                baseCtrl.ApplyDamage(baseDamage);
            }

            var constructionSite = target.GetComponent<ConstructionSite>();
            if (constructionSite != null)
            {
                constructionSite.TakeDamage(baseDamage);
            }

            var trapSite = target.GetComponent<TrapConstructionSite>();
            if (trapSite != null)
            {
                trapSite.TakeDamage(baseDamage);
            }
        }

        private Transform FindNearestEnemy(float range)
        {
            Collider[] colliders = Physics.OverlapSphere(transform.position, range, enemyUnitLayer);

            Transform nearest = null;
            float nearestDist = float.MaxValue;

            foreach (var col in colliders)
            {
                // Skip self
                if (col.gameObject == gameObject) continue;

                // Check if it's a valid enemy unit
                var enemyUnit = col.GetComponent<UnitController>();
                if (enemyUnit != null && !enemyUnit.IsDead)
                {
                    float dist = Vector3.Distance(transform.position, col.transform.position);
                    if (dist < nearestDist)
                    {
                        nearestDist = dist;
                        nearest = col.transform;
                    }
                }

                // Also check for heroes
                var heroUnit = col.GetComponent<HeroController>();
                if (heroUnit != null)
                {
                    float dist = Vector3.Distance(transform.position, col.transform.position);
                    if (dist < nearestDist)
                    {
                        nearestDist = dist;
                        nearest = col.transform;
                    }
                }
            }

            // Fallback: check all units by tag
            if (nearest == null)
            {
                string enemyTag = team == GoldManager.Team.Player ? "Enemy" : "Player";
                var enemies = GameObject.FindGameObjectsWithTag(enemyTag);
                foreach (var enemy in enemies)
                {
                    float dist = Vector3.Distance(transform.position, enemy.transform.position);
                    if (dist <= range && dist < nearestDist)
                    {
                        var unit = enemy.GetComponent<UnitController>();
                        if (unit != null && !unit.IsDead)
                        {
                            nearestDist = dist;
                            nearest = enemy.transform;
                        }
                    }
                }
            }

            return nearest;
        }

        private Transform FindNearestConstructionSite(float range)
        {
            // Find enemy construction sites
            var sites = FindObjectsByType<ConstructionSite>(FindObjectsSortMode.None);

            Transform nearest = null;
            float nearestDist = float.MaxValue;

            foreach (var site in sites)
            {
                // Only target enemy construction sites
                if (site.OwnerTeam == team) continue;
                if (site.IsComplete) continue;

                float dist = Vector3.Distance(transform.position, site.transform.position);
                if (dist <= range && dist < nearestDist)
                {
                    nearestDist = dist;
                    nearest = site.transform;
                }
            }

            return nearest;
        }

        private void OnDamageTaken(float damage, string damageType, GameObject source)
        {
            if (source == null) return;

            // Check if attacked by a tower
            var tower = source.GetComponent<TowerController>();
            if (tower != null)
            {
                wasAttackedByTower = true;
                lastTowerAttacker = source.transform;
                Log.Info($"[UnitCombatBrain] {gameObject.name} was attacked by tower {source.name}");
            }
        }

        private void OnDestroy()
        {
            if (healthComponent != null)
            {
                healthComponent.OnDamaged -= OnDamageTaken;
            }
        }

        private void OnDrawGizmosSelected()
        {
            // Detection range
            Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, detectionRange);

            // Attack range
            Gizmos.color = new Color(1f, 0f, 0f, 0.5f);
            Gizmos.DrawWireSphere(transform.position, attackRange);

            // Target line
            if (currentTarget != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(transform.position, currentTarget.position);
            }
        }
    }
}
