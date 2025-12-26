using System.Collections.Generic;
using TowerConquest.Debug;
using TowerConquest.Combat;
using TowerConquest.Core;
using UnityEngine;

namespace TowerConquest.Gameplay.Entities
{
    /// <summary>
    /// Handles unit combat with target prioritization
    /// Priority: 1. Enemy Units, 2. Towers (if attacking us), 3. Construction Sites, 4. Enemy Base
    /// </summary>
    public class UnitCombat : MonoBehaviour
    {
        [Header("Combat Settings")]
        [SerializeField] private float attackRange = 2f;
        [SerializeField] private float attackDamage = 50f;
        [SerializeField] private float attackCooldown = 1f;
        [SerializeField] private float detectionRange = 5f;

        [Header("Target Priority")]
        [Tooltip("Priority 1: Enemy units in range")]
        [SerializeField] private float unitPriority = 100f;
        [Tooltip("Priority 2: Towers attacking us")]
        [SerializeField] private float towerPriority = 75f;
        [Tooltip("Priority 3: Construction sites in range")]
        [SerializeField] private float constructionPriority = 50f;
        [Tooltip("Priority 4: Enemy base (default target)")]
        #pragma warning disable 0414 // Field assigned but never used (base targeting handled by UnitMover)
        [SerializeField] private float basePriority = 25f;
        #pragma warning restore 0414

        [Header("Runtime")]
        [SerializeField] private GameObject currentTarget;
        [SerializeField] private float lastAttackTime;
        [SerializeField] private bool isInCombat;

        // Team info
        private GoldManager.Team ownerTeam;
        private UnitController unitController;
        private UnitMover unitMover;
        private HealthComponent healthComponent;
        private BaseController targetBase;

        // Track who is attacking us
        private List<GameObject> attackers = new List<GameObject>();

        public bool IsInCombat => isInCombat;
        public GameObject CurrentTarget => currentTarget;

        private void Awake()
        {
            unitController = GetComponent<UnitController>();
            unitMover = GetComponent<UnitMover>();
            healthComponent = GetComponent<HealthComponent>();
        }

        public void Initialize(GoldManager.Team team, float damage, float range, float cooldown, BaseController baseTarget)
        {
            ownerTeam = team;
            attackDamage = damage;
            attackRange = range;
            attackCooldown = cooldown;
            targetBase = baseTarget;
            detectionRange = Mathf.Max(range * 2f, 5f);

            // Subscribe to damage events
            if (healthComponent != null)
            {
                healthComponent.OnDamaged += OnDamageTaken;
            }
        }

        private void OnDamageTaken(float damage, string damageType, GameObject source)
        {
            if (source != null && !attackers.Contains(source))
            {
                attackers.Add(source);
            }
        }

        private void Update()
        {
            // Clean up destroyed attackers
            attackers.RemoveAll(a => a == null);

            // Find best target
            FindBestTarget();

            // Attack if we have a target
            if (currentTarget != null)
            {
                float distance = Vector3.Distance(transform.position, currentTarget.transform.position);

                if (distance <= attackRange)
                {
                    // Stop moving and attack
                    if (unitMover != null)
                    {
                        unitMover.Pause();
                    }

                    isInCombat = true;
                    TryAttack();
                }
                else if (distance <= detectionRange)
                {
                    // Move towards target
                    isInCombat = true;
                    MoveTowardsTarget();
                }
                else
                {
                    // Target too far, clear it
                    currentTarget = null;
                    isInCombat = false;
                    if (unitMover != null)
                    {
                        unitMover.Resume();
                    }
                }
            }
            else
            {
                isInCombat = false;
                if (unitMover != null)
                {
                    unitMover.Resume();
                }
            }
        }

        private void FindBestTarget()
        {
            float bestScore = 0f;
            GameObject bestTarget = null;

            // Priority 1: Enemy units in detection range
            var units = FindObjectsByType<UnitController>(FindObjectsSortMode.None);
            foreach (var unit in units)
            {
                if (unit == null || unit.IsDead || unit.gameObject == gameObject) continue;
                if (!IsEnemy(unit.gameObject)) continue;

                float distance = Vector3.Distance(transform.position, unit.transform.position);
                if (distance <= detectionRange)
                {
                    // Closer units get higher score
                    float distanceScore = 1f - (distance / detectionRange);
                    float score = unitPriority * distanceScore;

                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestTarget = unit.gameObject;
                    }
                }
            }

            // Priority 2: Towers attacking us
            if (bestTarget == null)
            {
                var towers = FindObjectsByType<TowerController>(FindObjectsSortMode.None);
                foreach (var tower in towers)
                {
                    if (tower == null) continue;
                    if (!IsEnemy(tower.gameObject)) continue;

                    // Only target towers that are attacking us
                    if (attackers.Contains(tower.gameObject))
                    {
                        float distance = Vector3.Distance(transform.position, tower.transform.position);
                        float distanceScore = 1f - Mathf.Min(distance / detectionRange, 1f);
                        float score = towerPriority * distanceScore;

                        if (score > bestScore)
                        {
                            bestScore = score;
                            bestTarget = tower.gameObject;
                        }
                    }
                }
            }

            // Priority 3: Construction sites in range
            if (bestTarget == null)
            {
                var sites = FindObjectsByType<ConstructionSite>(FindObjectsSortMode.None);
                foreach (var site in sites)
                {
                    if (site == null || site.IsComplete) continue;
                    if (!IsEnemySite(site)) continue;

                    float distance = Vector3.Distance(transform.position, site.transform.position);
                    if (distance <= detectionRange)
                    {
                        float distanceScore = 1f - (distance / detectionRange);
                        float score = constructionPriority * distanceScore;

                        if (score > bestScore)
                        {
                            bestScore = score;
                            bestTarget = site.gameObject;
                        }
                    }
                }
            }

            // Priority 4: Enemy base (default - handled by UnitMover)
            // We don't set it as target here, let UnitMover handle path movement

            currentTarget = bestTarget;
        }

        private void TryAttack()
        {
            if (Time.time < lastAttackTime + attackCooldown) return;
            if (currentTarget == null) return;

            lastAttackTime = Time.time;

            // Apply damage to target
            var targetHealth = currentTarget.GetComponent<HealthComponent>();
            if (targetHealth != null)
            {
                // Apply damage buff if present
                float finalDamage = attackDamage;
                var damageBuff = GetComponent<DamageBuff>();
                if (damageBuff != null)
                {
                    finalDamage *= damageBuff.Multiplier;
                }

                targetHealth.TakeDamage(finalDamage, "physical", gameObject);
                Log.Info($"[UnitCombat] {gameObject.name} dealt {finalDamage} damage to {currentTarget.name}");
            }

            // Handle construction site damage
            var constructionSite = currentTarget.GetComponent<ConstructionSite>();
            if (constructionSite != null)
            {
                constructionSite.TakeDamage(attackDamage);
            }

            // Handle base damage (if directly attacking base)
            var baseController = currentTarget.GetComponent<BaseController>();
            if (baseController != null)
            {
                baseController.ApplyDamage(attackDamage);
            }
        }

        private void MoveTowardsTarget()
        {
            if (currentTarget == null) return;

            Vector3 direction = (currentTarget.transform.position - transform.position).normalized;
            float moveSpeed = unitMover != null ? unitMover.moveSpeed * unitMover.moveSpeedMultiplier : 2.5f;

            transform.position += direction * moveSpeed * Time.deltaTime;

            // Face target
            if (direction.sqrMagnitude > 0.001f)
            {
                transform.forward = direction;
            }
        }

        private bool IsEnemy(GameObject obj)
        {
            int playerLayer = LayerMask.NameToLayer("PlayerUnit");
            int enemyLayer = LayerMask.NameToLayer("Enemy");

            if (ownerTeam == GoldManager.Team.Player)
            {
                return obj.layer == enemyLayer;
            }
            else
            {
                return obj.layer == playerLayer || obj.layer == 0;
            }
        }

        private bool IsEnemySite(ConstructionSite site)
        {
            return site.OwnerTeam != ownerTeam;
        }

        /// <summary>
        /// Register an attacker (for tower priority)
        /// </summary>
        public void RegisterAttacker(GameObject attacker)
        {
            if (attacker != null && !attackers.Contains(attacker))
            {
                attackers.Add(attacker);
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
            // Draw attack range
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);

            // Draw detection range
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRange);

            // Draw line to current target
            if (currentTarget != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(transform.position, currentTarget.transform.position);
            }
        }
    }
}
