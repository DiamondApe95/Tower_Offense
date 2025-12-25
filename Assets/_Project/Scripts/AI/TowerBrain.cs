using System.Collections.Generic;
using UnityEngine;
using TowerConquest.Gameplay.Entities;
using TowerConquest.Combat;
using TowerConquest.Core;

namespace TowerConquest.AI
{
    /// <summary>
    /// TowerBrain: Steuert das Verhalten von Türmen.
    /// Target-Auswahl, Priorisierung und Angriffsmuster.
    /// </summary>
    public class TowerBrain : MonoBehaviour
    {
        public enum TargetPriority
        {
            Nearest,        // Nächstes Ziel
            LowestHp,       // Niedrigste HP
            HighestHp,      // Höchste HP
            MostDangerous,  // Höchster Schaden
            Fastest,        // Schnellste Einheit
            Cluster,        // Größte Gruppe
            First,          // Erste Einheit auf Pfad
            Last            // Letzte Einheit auf Pfad
        }

        public enum AttackPattern
        {
            Single,         // Ein Ziel
            Burst,          // Mehrere Schüsse schnell
            Sweep,          // Mehrere Ziele nacheinander
            Area            // AoE Angriff
        }

        [Header("Targeting")]
        public TargetPriority priority = TargetPriority.Nearest;
        public float range = 6f;
        public float scanInterval = 0.25f;
        public LayerMask targetMask;

        [Header("Attack Settings")]
        public AttackPattern pattern = AttackPattern.Single;
        public float attackRate = 1f;
        public float damage = 10f;
        public int burstCount = 3;
        public float burstInterval = 0.1f;

        [Header("AoE Settings")]
        public float aoeRadius = 2f;
        public float aoeDamageMultiplier = 0.5f;

        [Header("Cluster Detection")]
        public float clusterRadius = 3f;
        public int minClusterSize = 2;

        [Header("Debug")]
        public bool showDebugInfo = false;

        public Transform CurrentTarget { get; private set; }
        public bool IsAttacking { get; private set; }

        private TowerController towerController;
        private float lastScanTime;
        private float lastAttackTime;
        private List<UnitController> targetsInRange = new List<UnitController>();
        private EntityRegistry entityRegistry;
        private int currentBurstCount;

        private void Awake()
        {
            towerController = GetComponent<TowerController>();
        }

        private void Start()
        {
            ServiceLocator.TryGet(out entityRegistry);
        }

        private void Update()
        {
            // Periodische Zielsuche
            if (Time.time - lastScanTime > scanInterval)
            {
                lastScanTime = Time.time;
                ScanForTargets();
                SelectTarget();
            }

            // Angriff ausführen
            if (CurrentTarget != null && CanAttack())
            {
                ExecuteAttack();
            }
        }

        private void ScanForTargets()
        {
            targetsInRange.Clear();

            // Verwende EntityRegistry wenn verfügbar
            if (entityRegistry != null)
            {
                var units = entityRegistry.GetUnits();
                foreach (var unit in units)
                {
                    if (unit == null) continue;

                    float distance = Vector3.Distance(transform.position, unit.transform.position);
                    if (distance <= range)
                    {
                        targetsInRange.Add(unit);
                    }
                }
            }
            else
            {
                // Fallback
                var units = FindObjectsByType<UnitController>(FindObjectsSortMode.None);
                foreach (var unit in units)
                {
                    if (unit == null) continue;

                    float distance = Vector3.Distance(transform.position, unit.transform.position);
                    if (distance <= range)
                    {
                        targetsInRange.Add(unit);
                    }
                }
            }
        }

        private void SelectTarget()
        {
            if (targetsInRange.Count == 0)
            {
                CurrentTarget = null;
                return;
            }

            UnitController selectedUnit = null;

            switch (priority)
            {
                case TargetPriority.Nearest:
                    selectedUnit = SelectNearest();
                    break;

                case TargetPriority.LowestHp:
                    selectedUnit = SelectByHealth(true);
                    break;

                case TargetPriority.HighestHp:
                    selectedUnit = SelectByHealth(false);
                    break;

                case TargetPriority.MostDangerous:
                    selectedUnit = SelectMostDangerous();
                    break;

                case TargetPriority.Fastest:
                    selectedUnit = SelectFastest();
                    break;

                case TargetPriority.Cluster:
                    selectedUnit = SelectClusterCenter();
                    break;

                case TargetPriority.First:
                    selectedUnit = SelectByPathProgress(true);
                    break;

                case TargetPriority.Last:
                    selectedUnit = SelectByPathProgress(false);
                    break;
            }

            CurrentTarget = selectedUnit?.transform;
        }

        private UnitController SelectNearest()
        {
            UnitController nearest = null;
            float minDist = float.MaxValue;

            foreach (var unit in targetsInRange)
            {
                float dist = Vector3.Distance(transform.position, unit.transform.position);
                if (dist < minDist)
                {
                    minDist = dist;
                    nearest = unit;
                }
            }

            return nearest;
        }

        private UnitController SelectByHealth(bool lowest)
        {
            UnitController selected = null;
            float targetHp = lowest ? float.MaxValue : float.MinValue;

            foreach (var unit in targetsInRange)
            {
                var health = unit.GetComponent<HealthComponent>();
                if (health == null) continue;

                bool better = lowest ? (health.CurrentHp < targetHp) : (health.CurrentHp > targetHp);
                if (better)
                {
                    targetHp = health.CurrentHp;
                    selected = unit;
                }
            }

            return selected;
        }

        private UnitController SelectMostDangerous()
        {
            UnitController mostDangerous = null;
            float maxDps = 0f;

            foreach (var unit in targetsInRange)
            {
                // Schätze DPS basierend auf Unit-Stats
                float dps = unit.damage * unit.attacksPerSecond;
                if (dps > maxDps)
                {
                    maxDps = dps;
                    mostDangerous = unit;
                }
            }

            return mostDangerous;
        }

        private UnitController SelectFastest()
        {
            UnitController fastest = null;
            float maxSpeed = 0f;

            foreach (var unit in targetsInRange)
            {
                if (unit.moveSpeed > maxSpeed)
                {
                    maxSpeed = unit.moveSpeed;
                    fastest = unit;
                }
            }

            return fastest;
        }

        private UnitController SelectClusterCenter()
        {
            UnitController bestCenter = null;
            int maxClusterSize = 0;

            foreach (var unit in targetsInRange)
            {
                int clusterSize = CountUnitsNear(unit.transform.position, clusterRadius);
                if (clusterSize > maxClusterSize)
                {
                    maxClusterSize = clusterSize;
                    bestCenter = unit;
                }
            }

            // Nur wenn Cluster groß genug
            if (maxClusterSize >= minClusterSize)
            {
                return bestCenter;
            }

            // Fallback auf nearest
            return SelectNearest();
        }

        private UnitController SelectByPathProgress(bool first)
        {
            UnitController selected = null;
            float targetProgress = first ? float.MaxValue : float.MinValue;

            foreach (var unit in targetsInRange)
            {
                var mover = unit.GetComponent<UnitMover>();
                if (mover == null) continue;

                float progress = mover.pathProgress;
                bool better = first ? (progress > targetProgress) : (progress < targetProgress);
                if (better)
                {
                    targetProgress = progress;
                    selected = unit;
                }
            }

            return selected ?? SelectNearest();
        }

        private int CountUnitsNear(Vector3 center, float radius)
        {
            int count = 0;
            foreach (var unit in targetsInRange)
            {
                if (Vector3.Distance(center, unit.transform.position) <= radius)
                {
                    count++;
                }
            }
            return count;
        }

        private bool CanAttack()
        {
            return Time.time - lastAttackTime >= 1f / attackRate;
        }

        private void ExecuteAttack()
        {
            if (CurrentTarget == null) return;

            IsAttacking = true;

            switch (pattern)
            {
                case AttackPattern.Single:
                    AttackSingle();
                    break;

                case AttackPattern.Burst:
                    StartCoroutine(AttackBurst());
                    break;

                case AttackPattern.Sweep:
                    AttackSweep();
                    break;

                case AttackPattern.Area:
                    AttackArea();
                    break;
            }

            lastAttackTime = Time.time;
        }

        private void AttackSingle()
        {
            DealDamage(CurrentTarget, damage);

            if (showDebugInfo)
            {
                Debug.Log($"TowerBrain: {name} attacks {CurrentTarget.name} for {damage} damage.");
            }
        }

        private System.Collections.IEnumerator AttackBurst()
        {
            for (int i = 0; i < burstCount; i++)
            {
                if (CurrentTarget == null) break;

                DealDamage(CurrentTarget, damage);
                yield return new WaitForSeconds(burstInterval);
            }
        }

        private void AttackSweep()
        {
            // Greife mehrere Ziele nacheinander an
            int maxTargets = Mathf.Min(3, targetsInRange.Count);
            float damagePerTarget = damage / maxTargets;

            for (int i = 0; i < maxTargets; i++)
            {
                DealDamage(targetsInRange[i].transform, damagePerTarget);
            }
        }

        private void AttackArea()
        {
            if (CurrentTarget == null) return;

            // AoE Schaden
            Vector3 center = CurrentTarget.position;

            foreach (var unit in targetsInRange)
            {
                float dist = Vector3.Distance(center, unit.transform.position);
                if (dist <= aoeRadius)
                {
                    float dmg = dist < 0.5f ? damage : damage * aoeDamageMultiplier;
                    DealDamage(unit.transform, dmg);
                }
            }

            if (showDebugInfo)
            {
                Debug.Log($"TowerBrain: {name} AoE attack at {center} with radius {aoeRadius}.");
            }
        }

        private void DealDamage(Transform target, float amount)
        {
            if (target == null) return;

            var health = target.GetComponent<HealthComponent>();
            if (health != null)
            {
                health.TakeDamage(amount, "physical", gameObject);
            }
        }

        public void SetPriority(TargetPriority newPriority)
        {
            priority = newPriority;
        }

        public void SetPattern(AttackPattern newPattern)
        {
            pattern = newPattern;
        }

        public void ForceTarget(Transform target)
        {
            CurrentTarget = target;
        }

        public bool HasTarget()
        {
            return CurrentTarget != null;
        }

        public int GetTargetCount()
        {
            return targetsInRange.Count;
        }

        private void OnDrawGizmosSelected()
        {
            // Range
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, range);

            // AoE Radius
            if (pattern == AttackPattern.Area && CurrentTarget != null)
            {
                Gizmos.color = new Color(1, 0.5f, 0, 0.5f);
                Gizmos.DrawWireSphere(CurrentTarget.position, aoeRadius);
            }

            // Cluster Radius
            if (priority == TargetPriority.Cluster)
            {
                Gizmos.color = new Color(0.5f, 0, 0.5f, 0.3f);
                Gizmos.DrawWireSphere(transform.position, clusterRadius);
            }

            // Linie zum Ziel
            if (CurrentTarget != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(transform.position, CurrentTarget.position);
            }
        }
    }
}
