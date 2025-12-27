using System.Collections.Generic;
using TowerConquest.Debug;
using UnityEngine;
using TowerConquest.Gameplay;
using TowerConquest.Gameplay.Entities;
using TowerConquest.Combat;
using TowerConquest.Core;

namespace TowerConquest.AI
{
    /// <summary>
    /// UnitBrain: Steuert das Verhalten von Einheiten.
    /// Sucht Ziele, entscheidet Kampf vs. Bewegung, reagiert auf Bedrohungen.
    /// </summary>
    public class UnitBrain : MonoBehaviour
    {
        public enum UnitState
        {
            Idle,
            Moving,
            Attacking,
            Fleeing,
            Dead
        }

        public enum CombatBehavior
        {
            Aggressive,     // Greift alle Feinde in Reichweite an
            Defensive,      // Greift nur an wenn angegriffen
            Passive,        // Ignoriert Feinde, fokussiert auf Ziel
            Berserker       // Greift nächsten Feind an, ignoriert alles andere
        }

        [Header("Behavior Settings")]
        public CombatBehavior behavior = CombatBehavior.Aggressive;
        public float aggroRange = 5f;
        public float attackRange = 1.5f;
        public float detectionInterval = 0.25f;

        [Header("Combat Settings")]
        public float attackCooldown = 1f;
        public float damage = 10f;
        public bool canAttackWhileMoving = false;

        [Header("Movement")]
        public float moveSpeed = 3f;
        public float fleeHealthThreshold = 0.2f;
        public bool canFlee = false;

        [Header("Debug")]
        public bool showDebugInfo = false;

        public UnitState CurrentState { get; private set; } = UnitState.Idle;
        public Transform CurrentTarget { get; private set; }

        private UnitController unitController;
        private UnitMover unitMover;
        private HealthComponent health;
        private float lastDetectionTime;
        private float lastAttackTime;
        private List<Transform> nearbyEnemies = new List<Transform>();
        private EntityRegistry entityRegistry;

        // Team reference for enemy detection
        private GoldManager.Team ownerTeam = GoldManager.Team.Player;

        private void Awake()
        {
            unitController = GetComponent<UnitController>();
            unitMover = GetComponent<UnitMover>();
            health = GetComponent<HealthComponent>();
        }

        private void Start()
        {
            ServiceLocator.TryGet(out entityRegistry);

            // Get team from UnitController
            if (unitController != null)
            {
                ownerTeam = unitController.OwnerTeam;
            }

            if (health != null)
            {
                health.OnDamaged += OnDamaged;
                health.OnDeath += OnDeath;
            }
        }

        private void OnDestroy()
        {
            if (health != null)
            {
                health.OnDamaged -= OnDamaged;
                health.OnDeath -= OnDeath;
            }
        }

        private void Update()
        {
            if (CurrentState == UnitState.Dead) return;

            // Periodische Zielsuche
            if (Time.time - lastDetectionTime > detectionInterval)
            {
                lastDetectionTime = Time.time;
                DetectEnemies();
                EvaluateState();
            }

            ExecuteState();
        }

        private void DetectEnemies()
        {
            nearbyEnemies.Clear();

            // Detect enemy towers
            TowerController[] towers;
            if (entityRegistry != null)
            {
                towers = entityRegistry.GetAllTowers();
            }
            else
            {
                towers = FindObjectsByType<TowerController>(FindObjectsSortMode.None);
            }

            foreach (var tower in towers)
            {
                if (tower == null || tower.IsDestroyed) continue;

                // Only target enemy towers (different team)
                if (tower.ownerTeam == ownerTeam) continue;

                float distance = Vector3.Distance(transform.position, tower.transform.position);
                if (distance <= aggroRange)
                {
                    nearbyEnemies.Add(tower.transform);
                }
            }

            // Detect enemy units
            UnitController[] units;
            if (entityRegistry != null)
            {
                units = entityRegistry.GetAllUnits();
            }
            else
            {
                units = FindObjectsByType<UnitController>(FindObjectsSortMode.None);
            }

            foreach (var unit in units)
            {
                if (unit == null || unit.IsDead || unit == unitController) continue;

                // Only target enemy units (different team)
                if (unit.OwnerTeam == ownerTeam) continue;

                float distance = Vector3.Distance(transform.position, unit.transform.position);
                if (distance <= aggroRange)
                {
                    nearbyEnemies.Add(unit.transform);
                }
            }

            // Sortiere nach Distanz
            nearbyEnemies.Sort((a, b) =>
            {
                float distA = Vector3.Distance(transform.position, a.position);
                float distB = Vector3.Distance(transform.position, b.position);
                return distA.CompareTo(distB);
            });
        }

        private void EvaluateState()
        {
            // Prüfe Flucht-Bedingung
            if (canFlee && health != null && health.CurrentHp / health.MaxHp < fleeHealthThreshold)
            {
                CurrentState = UnitState.Fleeing;
                return;
            }

            // Prüfe ob Ziel noch gültig ist
            if (CurrentTarget != null)
            {
                var targetHealth = CurrentTarget.GetComponent<HealthComponent>();
                if (targetHealth == null || targetHealth.CurrentHp <= 0)
                {
                    CurrentTarget = null;
                }
            }

            // Verhaltensbasierte Zielwahl
            switch (behavior)
            {
                case CombatBehavior.Aggressive:
                    SelectAggressiveTarget();
                    break;

                case CombatBehavior.Defensive:
                    // Nur angreifen wenn angegriffen
                    if (CurrentTarget == null)
                    {
                        CurrentState = UnitState.Moving;
                    }
                    break;

                case CombatBehavior.Passive:
                    CurrentState = UnitState.Moving;
                    CurrentTarget = null;
                    break;

                case CombatBehavior.Berserker:
                    SelectBerserkerTarget();
                    break;
            }

            // State basierend auf Ziel setzen
            if (CurrentTarget != null)
            {
                float distance = Vector3.Distance(transform.position, CurrentTarget.position);
                CurrentState = distance <= attackRange ? UnitState.Attacking : UnitState.Moving;
            }
            else if (CurrentState != UnitState.Fleeing)
            {
                CurrentState = UnitState.Moving;
            }
        }

        private void SelectAggressiveTarget()
        {
            if (nearbyEnemies.Count > 0)
            {
                CurrentTarget = nearbyEnemies[0];
            }
            else
            {
                CurrentTarget = null;
            }
        }

        private void SelectBerserkerTarget()
        {
            // Immer das nächste Ziel, auch außerhalb normaler Aggro-Range
            if (nearbyEnemies.Count > 0)
            {
                CurrentTarget = nearbyEnemies[0];
            }
            else
            {
                // Suche weiter entfernte Ziele (enemy towers only)
                TowerController[] towers;
                if (entityRegistry != null)
                {
                    towers = entityRegistry.GetAllTowers();
                }
                else
                {
                    towers = FindObjectsByType<TowerController>(FindObjectsSortMode.None);
                }

                float closestDist = float.MaxValue;
                TowerController closest = null;

                foreach (var tower in towers)
                {
                    if (tower == null || tower.IsDestroyed) continue;

                    // Only target enemy towers
                    if (tower.ownerTeam == ownerTeam) continue;

                    float dist = Vector3.Distance(transform.position, tower.transform.position);
                    if (dist < closestDist)
                    {
                        closestDist = dist;
                        closest = tower;
                    }
                }

                CurrentTarget = closest?.transform;
            }
        }

        private void ExecuteState()
        {
            switch (CurrentState)
            {
                case UnitState.Idle:
                    ExecuteIdle();
                    break;

                case UnitState.Moving:
                    ExecuteMoving();
                    break;

                case UnitState.Attacking:
                    ExecuteAttacking();
                    break;

                case UnitState.Fleeing:
                    ExecuteFleeing();
                    break;

                case UnitState.Dead:
                    break;
            }
        }

        private void ExecuteIdle()
        {
            // Warte auf neuen Zustand
        }

        private void ExecuteMoving()
        {
            if (unitMover != null && unitMover.enabled)
            {
                // UnitMover kümmert sich um Pfadverfolgung
                return;
            }

            // Manuelle Bewegung zum Ziel
            if (CurrentTarget != null && canAttackWhileMoving)
            {
                Vector3 direction = (CurrentTarget.position - transform.position).normalized;
                transform.position += direction * moveSpeed * Time.deltaTime;
            }
        }

        private void ExecuteAttacking()
        {
            if (CurrentTarget == null) return;

            // Drehe zum Ziel
            Vector3 lookDir = CurrentTarget.position - transform.position;
            lookDir.y = 0;
            if (lookDir != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(lookDir);
            }

            // Angriff ausführen
            if (Time.time - lastAttackTime >= attackCooldown)
            {
                PerformAttack();
                lastAttackTime = Time.time;
            }
        }

        private void ExecuteFleeing()
        {
            // Fliehe vom nächsten Feind
            if (nearbyEnemies.Count > 0)
            {
                Vector3 fleeDirection = transform.position - nearbyEnemies[0].position;
                fleeDirection.y = 0;
                fleeDirection.Normalize();

                transform.position += fleeDirection * moveSpeed * 1.5f * Time.deltaTime;
            }
        }

        private void PerformAttack()
        {
            if (CurrentTarget == null) return;

            var targetHealth = CurrentTarget.GetComponent<HealthComponent>();
            if (targetHealth != null)
            {
                targetHealth.TakeDamage(damage, "physical", gameObject);

                if (showDebugInfo)
                {
                    Log.Info($"UnitBrain: {name} attacks {CurrentTarget.name} for {damage} damage.");
                }
            }
        }

        private void OnDamaged(float amount, string type, GameObject source)
        {
            // Bei Defensive-Behavior: Angreifer als Ziel setzen
            if (behavior == CombatBehavior.Defensive && source != null)
            {
                CurrentTarget = source.transform;
                CurrentState = UnitState.Attacking;
            }
        }

        private void OnDeath()
        {
            CurrentState = UnitState.Dead;
            CurrentTarget = null;
        }

        public void SetBehavior(CombatBehavior newBehavior)
        {
            behavior = newBehavior;
        }

        public void ForceTarget(Transform target)
        {
            CurrentTarget = target;
            if (target != null)
            {
                CurrentState = UnitState.Attacking;
            }
        }

        public void ClearTarget()
        {
            CurrentTarget = null;
            CurrentState = UnitState.Idle;
        }

        private void OnDrawGizmosSelected()
        {
            // Aggro Range
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, aggroRange);

            // Attack Range
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);

            // Linie zum Ziel
            if (CurrentTarget != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(transform.position, CurrentTarget.position);
            }
        }
    }
}
