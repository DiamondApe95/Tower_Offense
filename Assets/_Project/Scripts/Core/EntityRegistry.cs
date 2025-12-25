using System.Collections.Generic;
using UnityEngine;
using TowerConquest.Gameplay.Entities;

namespace TowerConquest.Core
{
    /// <summary>
    /// Zentrale Registry für alle aktiven Entities (Units, Towers, Heroes).
    /// Vermeidet teure FindObjectsByType-Aufrufe.
    /// </summary>
    public class EntityRegistry
    {
        private readonly HashSet<UnitController> units = new HashSet<UnitController>();
        private readonly HashSet<TowerController> towers = new HashSet<TowerController>();
        private readonly HashSet<HeroController> heroes = new HashSet<HeroController>();

        // Cached arrays für schnellen Zugriff
        private UnitController[] cachedUnits;
        private TowerController[] cachedTowers;
        private bool unitsDirty = true;
        private bool towersDirty = true;

        // === UNITS ===
        public void RegisterUnit(UnitController unit)
        {
            if (unit != null && units.Add(unit))
            {
                unitsDirty = true;
            }
        }

        public void UnregisterUnit(UnitController unit)
        {
            if (unit != null && units.Remove(unit))
            {
                unitsDirty = true;
            }
        }

        public int UnitCount => units.Count;

        public UnitController[] GetAllUnits()
        {
            if (unitsDirty || cachedUnits == null)
            {
                cachedUnits = new UnitController[units.Count];
                units.CopyTo(cachedUnits);
                unitsDirty = false;
            }
            return cachedUnits;
        }

        public bool HasActiveUnits() => units.Count > 0;

        // === TOWERS ===
        public void RegisterTower(TowerController tower)
        {
            if (tower != null && towers.Add(tower))
            {
                towersDirty = true;
            }
        }

        public void UnregisterTower(TowerController tower)
        {
            if (tower != null && towers.Remove(tower))
            {
                towersDirty = true;
            }
        }

        public int TowerCount => towers.Count;

        public TowerController[] GetAllTowers()
        {
            if (towersDirty || cachedTowers == null)
            {
                cachedTowers = new TowerController[towers.Count];
                towers.CopyTo(cachedTowers);
                towersDirty = false;
            }
            return cachedTowers;
        }

        // === HEROES ===
        public void RegisterHero(HeroController hero)
        {
            if (hero != null)
            {
                heroes.Add(hero);
            }
        }

        public void UnregisterHero(HeroController hero)
        {
            if (hero != null)
            {
                heroes.Remove(hero);
            }
        }

        public IReadOnlyCollection<HeroController> GetAllHeroes() => heroes;

        // === UTILITY ===
        public float CalculateTotalTowerDps()
        {
            float sum = 0f;
            foreach (var tower in towers)
            {
                if (tower != null)
                {
                    sum += tower.EstimatedDps;
                }
            }
            return sum;
        }

        public void Clear()
        {
            units.Clear();
            towers.Clear();
            heroes.Clear();
            cachedUnits = null;
            cachedTowers = null;
            unitsDirty = true;
            towersDirty = true;
        }
    }
}
