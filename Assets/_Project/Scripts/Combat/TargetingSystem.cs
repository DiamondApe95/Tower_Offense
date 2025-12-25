using System;
using System.Collections.Generic;
using UnityEngine;
using TowerConquest.Gameplay.Entities;
using TowerConquest.Core;

namespace TowerConquest.Combat
{
    /// <summary>
    /// TargetingSystem: Zentrales System für Zielauswahl und -priorisierung.
    /// Unterstützt verschiedene Targeting-Prioritäten und Filter.
    /// </summary>
    public class TargetingSystem
    {
        public enum Priority
        {
            Nearest,        // Nächstes Ziel
            Farthest,       // Entferntestes Ziel
            LowestHp,       // Niedrigste HP
            HighestHp,      // Höchste HP
            LowestPercent,  // Niedrigster HP-Prozentsatz
            MostDangerous,  // Höchster DPS
            Fastest,        // Höchste Bewegungsgeschwindigkeit
            Slowest,        // Niedrigste Bewegungsgeschwindigkeit
            Cluster,        // Zentrum der größten Gruppe
            First,          // Am weitesten auf Pfad
            Last,           // Am wenigsten weit auf Pfad
            Random,         // Zufälliges Ziel
            Weakest,        // Niedrigste Rüstung
            Strongest       // Höchste Rüstung
        }

        [Flags]
        public enum TargetFilter
        {
            None = 0,
            Units = 1,
            Towers = 2,
            Heroes = 4,
            Traps = 8,
            Base = 16,
            All = Units | Towers | Heroes | Traps | Base
        }

        private EntityRegistry entityRegistry;
        private static readonly System.Random random = new System.Random();

        public TargetingSystem()
        {
            ServiceLocator.TryGet(out entityRegistry);
        }

        /// <summary>
        /// Findet das beste Ziel basierend auf Priorität.
        /// </summary>
        public Transform FindTarget(Vector3 origin, float range, Priority priority, TargetFilter filter = TargetFilter.All)
        {
            var candidates = GetCandidates(origin, range, filter);
            if (candidates.Count == 0) return null;

            return SelectBestTarget(origin, candidates, priority);
        }

        /// <summary>
        /// Findet alle Ziele in Reichweite.
        /// </summary>
        public List<Transform> FindAllTargets(Vector3 origin, float range, TargetFilter filter = TargetFilter.All)
        {
            return GetCandidates(origin, range, filter);
        }

        /// <summary>
        /// Findet die N besten Ziele basierend auf Priorität.
        /// </summary>
        public List<Transform> FindTopTargets(Vector3 origin, float range, Priority priority, int count, TargetFilter filter = TargetFilter.All)
        {
            var candidates = GetCandidates(origin, range, filter);
            if (candidates.Count == 0) return new List<Transform>();

            SortByPriority(origin, candidates, priority);
            return candidates.GetRange(0, Mathf.Min(count, candidates.Count));
        }

        /// <summary>
        /// Findet das nächste Ziel.
        /// </summary>
        public Transform FindNearest(Vector3 origin, float range, TargetFilter filter = TargetFilter.All)
        {
            return FindTarget(origin, range, Priority.Nearest, filter);
        }

        /// <summary>
        /// Findet Ziele in einem Cluster.
        /// </summary>
        public List<Transform> FindCluster(Vector3 origin, float range, float clusterRadius, int minSize, TargetFilter filter = TargetFilter.All)
        {
            var candidates = GetCandidates(origin, range, filter);
            if (candidates.Count < minSize) return new List<Transform>();

            Transform center = SelectClusterCenter(candidates, clusterRadius, minSize);
            if (center == null) return new List<Transform>();

            var cluster = new List<Transform>();
            foreach (var target in candidates)
            {
                if (Vector3.Distance(center.position, target.position) <= clusterRadius)
                {
                    cluster.Add(target);
                }
            }

            return cluster;
        }

        /// <summary>
        /// Prüft ob ein Ziel in Reichweite ist.
        /// </summary>
        public bool IsInRange(Vector3 origin, Transform target, float range)
        {
            if (target == null) return false;
            return Vector3.Distance(origin, target.position) <= range;
        }

        /// <summary>
        /// Prüft ob ein Ziel gültig ist (existiert und lebt).
        /// </summary>
        public bool IsValidTarget(Transform target)
        {
            if (target == null) return false;

            var health = target.GetComponent<HealthComponent>();
            if (health != null && health.CurrentHp <= 0) return false;

            return target.gameObject.activeInHierarchy;
        }

        private List<Transform> GetCandidates(Vector3 origin, float range, TargetFilter filter)
        {
            var candidates = new List<Transform>();
            float rangeSqr = range * range;

            if ((filter & TargetFilter.Units) != 0)
            {
                AddUnitsInRange(origin, rangeSqr, candidates);
            }

            if ((filter & TargetFilter.Towers) != 0)
            {
                AddTowersInRange(origin, rangeSqr, candidates);
            }

            if ((filter & TargetFilter.Heroes) != 0)
            {
                AddHeroesInRange(origin, rangeSqr, candidates);
            }

            if ((filter & TargetFilter.Traps) != 0)
            {
                AddTrapsInRange(origin, rangeSqr, candidates);
            }

            return candidates;
        }

        private void AddUnitsInRange(Vector3 origin, float rangeSqr, List<Transform> candidates)
        {
            IReadOnlyList<UnitController> units;

            if (entityRegistry != null)
            {
                units = entityRegistry.GetUnits();
            }
            else
            {
                units = new List<UnitController>(UnityEngine.Object.FindObjectsByType<UnitController>(FindObjectsSortMode.None));
            }

            foreach (var unit in units)
            {
                if (unit == null) continue;
                if ((unit.transform.position - origin).sqrMagnitude <= rangeSqr)
                {
                    candidates.Add(unit.transform);
                }
            }
        }

        private void AddTowersInRange(Vector3 origin, float rangeSqr, List<Transform> candidates)
        {
            IReadOnlyList<TowerController> towers;

            if (entityRegistry != null)
            {
                towers = entityRegistry.GetTowers();
            }
            else
            {
                towers = new List<TowerController>(UnityEngine.Object.FindObjectsByType<TowerController>(FindObjectsSortMode.None));
            }

            foreach (var tower in towers)
            {
                if (tower == null) continue;
                if ((tower.transform.position - origin).sqrMagnitude <= rangeSqr)
                {
                    candidates.Add(tower.transform);
                }
            }
        }

        private void AddHeroesInRange(Vector3 origin, float rangeSqr, List<Transform> candidates)
        {
            var heroes = UnityEngine.Object.FindObjectsByType<HeroController>(FindObjectsSortMode.None);
            foreach (var hero in heroes)
            {
                if (hero == null) continue;
                if ((hero.transform.position - origin).sqrMagnitude <= rangeSqr)
                {
                    candidates.Add(hero.transform);
                }
            }
        }

        private void AddTrapsInRange(Vector3 origin, float rangeSqr, List<Transform> candidates)
        {
            var traps = UnityEngine.Object.FindObjectsByType<TrapController>(FindObjectsSortMode.None);
            foreach (var trap in traps)
            {
                if (trap == null) continue;
                if ((trap.transform.position - origin).sqrMagnitude <= rangeSqr)
                {
                    candidates.Add(trap.transform);
                }
            }
        }

        private Transform SelectBestTarget(Vector3 origin, List<Transform> candidates, Priority priority)
        {
            if (candidates.Count == 0) return null;
            if (candidates.Count == 1) return candidates[0];

            switch (priority)
            {
                case Priority.Nearest:
                    return SelectByDistance(origin, candidates, true);

                case Priority.Farthest:
                    return SelectByDistance(origin, candidates, false);

                case Priority.LowestHp:
                    return SelectByHealth(candidates, true, false);

                case Priority.HighestHp:
                    return SelectByHealth(candidates, false, false);

                case Priority.LowestPercent:
                    return SelectByHealth(candidates, true, true);

                case Priority.MostDangerous:
                    return SelectByDps(candidates, true);

                case Priority.Fastest:
                    return SelectBySpeed(candidates, true);

                case Priority.Slowest:
                    return SelectBySpeed(candidates, false);

                case Priority.Cluster:
                    return SelectClusterCenter(candidates, 3f, 2);

                case Priority.First:
                    return SelectByPathProgress(candidates, true);

                case Priority.Last:
                    return SelectByPathProgress(candidates, false);

                case Priority.Random:
                    return candidates[random.Next(candidates.Count)];

                case Priority.Weakest:
                    return SelectByArmor(candidates, true);

                case Priority.Strongest:
                    return SelectByArmor(candidates, false);

                default:
                    return candidates[0];
            }
        }

        private void SortByPriority(Vector3 origin, List<Transform> candidates, Priority priority)
        {
            switch (priority)
            {
                case Priority.Nearest:
                    candidates.Sort((a, b) =>
                        Vector3.Distance(origin, a.position).CompareTo(Vector3.Distance(origin, b.position)));
                    break;

                case Priority.Farthest:
                    candidates.Sort((a, b) =>
                        Vector3.Distance(origin, b.position).CompareTo(Vector3.Distance(origin, a.position)));
                    break;

                case Priority.LowestHp:
                    candidates.Sort((a, b) =>
                        GetHealth(a).CompareTo(GetHealth(b)));
                    break;

                case Priority.HighestHp:
                    candidates.Sort((a, b) =>
                        GetHealth(b).CompareTo(GetHealth(a)));
                    break;

                default:
                    // Default: nach Distanz
                    candidates.Sort((a, b) =>
                        Vector3.Distance(origin, a.position).CompareTo(Vector3.Distance(origin, b.position)));
                    break;
            }
        }

        private Transform SelectByDistance(Vector3 origin, List<Transform> candidates, bool nearest)
        {
            Transform best = null;
            float bestDist = nearest ? float.MaxValue : float.MinValue;

            foreach (var target in candidates)
            {
                float dist = Vector3.Distance(origin, target.position);
                bool isBetter = nearest ? (dist < bestDist) : (dist > bestDist);

                if (isBetter)
                {
                    bestDist = dist;
                    best = target;
                }
            }

            return best;
        }

        private Transform SelectByHealth(List<Transform> candidates, bool lowest, bool usePercent)
        {
            Transform best = null;
            float bestValue = lowest ? float.MaxValue : float.MinValue;

            foreach (var target in candidates)
            {
                var health = target.GetComponent<HealthComponent>();
                if (health == null) continue;

                float value = usePercent ? (health.CurrentHp / health.MaxHp) : health.CurrentHp;
                bool isBetter = lowest ? (value < bestValue) : (value > bestValue);

                if (isBetter)
                {
                    bestValue = value;
                    best = target;
                }
            }

            return best;
        }

        private Transform SelectByDps(List<Transform> candidates, bool highest)
        {
            Transform best = null;
            float bestDps = highest ? float.MinValue : float.MaxValue;

            foreach (var target in candidates)
            {
                float dps = GetDps(target);
                bool isBetter = highest ? (dps > bestDps) : (dps < bestDps);

                if (isBetter)
                {
                    bestDps = dps;
                    best = target;
                }
            }

            return best;
        }

        private Transform SelectBySpeed(List<Transform> candidates, bool fastest)
        {
            Transform best = null;
            float bestSpeed = fastest ? float.MinValue : float.MaxValue;

            foreach (var target in candidates)
            {
                float speed = GetSpeed(target);
                bool isBetter = fastest ? (speed > bestSpeed) : (speed < bestSpeed);

                if (isBetter)
                {
                    bestSpeed = speed;
                    best = target;
                }
            }

            return best;
        }

        private Transform SelectByArmor(List<Transform> candidates, bool weakest)
        {
            Transform best = null;
            float bestArmor = weakest ? float.MaxValue : float.MinValue;

            foreach (var target in candidates)
            {
                var health = target.GetComponent<HealthComponent>();
                float armor = health?.armor ?? 0f;
                bool isBetter = weakest ? (armor < bestArmor) : (armor > bestArmor);

                if (isBetter)
                {
                    bestArmor = armor;
                    best = target;
                }
            }

            return best;
        }

        private Transform SelectByPathProgress(List<Transform> candidates, bool first)
        {
            Transform best = null;
            float bestProgress = first ? float.MinValue : float.MaxValue;

            foreach (var target in candidates)
            {
                var mover = target.GetComponent<UnitMover>();
                if (mover == null) continue;

                float progress = mover.Progress;
                bool isBetter = first ? (progress > bestProgress) : (progress < bestProgress);

                if (isBetter)
                {
                    bestProgress = progress;
                    best = target;
                }
            }

            return best ?? SelectByDistance(Vector3.zero, candidates, true);
        }

        private Transform SelectClusterCenter(List<Transform> candidates, float radius, int minSize)
        {
            Transform bestCenter = null;
            int maxClusterSize = 0;

            foreach (var target in candidates)
            {
                int clusterSize = 0;
                foreach (var other in candidates)
                {
                    if (Vector3.Distance(target.position, other.position) <= radius)
                    {
                        clusterSize++;
                    }
                }

                if (clusterSize > maxClusterSize)
                {
                    maxClusterSize = clusterSize;
                    bestCenter = target;
                }
            }

            return maxClusterSize >= minSize ? bestCenter : null;
        }

        private float GetHealth(Transform target)
        {
            var health = target.GetComponent<HealthComponent>();
            return health?.CurrentHp ?? 0f;
        }

        private float GetDps(Transform target)
        {
            var unit = target.GetComponent<UnitController>();
            if (unit != null)
            {
                return unit.damage * unit.attacksPerSecond;
            }

            var tower = target.GetComponent<TowerController>();
            if (tower != null)
            {
                return tower.EstimatedDps;
            }

            return 0f;
        }

        private float GetSpeed(Transform target)
        {
            var unit = target.GetComponent<UnitController>();
            return unit?.moveSpeed ?? 0f;
        }

        /// <summary>
        /// Konvertiert einen String-Wert in eine Priority-Enum.
        /// </summary>
        public static Priority ParsePriority(string value)
        {
            if (string.IsNullOrEmpty(value)) return Priority.Nearest;

            return value.ToLowerInvariant() switch
            {
                "nearest" => Priority.Nearest,
                "farthest" => Priority.Farthest,
                "lowest_hp" => Priority.LowestHp,
                "highest_hp" => Priority.HighestHp,
                "lowest_percent" => Priority.LowestPercent,
                "most_dangerous" => Priority.MostDangerous,
                "fastest" => Priority.Fastest,
                "slowest" => Priority.Slowest,
                "cluster" => Priority.Cluster,
                "first" => Priority.First,
                "last" => Priority.Last,
                "random" => Priority.Random,
                "weakest" => Priority.Weakest,
                "strongest" => Priority.Strongest,
                _ => Priority.Nearest
            };
        }
    }
}
