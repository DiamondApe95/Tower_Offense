using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TowerConquest.Gameplay
{
    /// <summary>
    /// Represents a player's selected units and hero for a level
    /// </summary>
    [System.Serializable]
    public class UnitDeck
    {
        public string CivilizationID { get; private set; }
        public List<string> SelectedUnits { get; private set; } = new List<string>();
        public string SelectedHero { get; private set; }

        public const int MAX_UNITS = 5;

        public UnitDeck()
        {
            SelectedUnits = new List<string>();
        }

        public void SetCivilization(string civId)
        {
            CivilizationID = civId;
            SelectedUnits.Clear();
            SelectedHero = null;
            Debug.Log($"[UnitDeck] Set civilization: {civId}");
        }

        public bool AddUnit(string unitId)
        {
            if (SelectedUnits.Count >= MAX_UNITS)
            {
                Debug.LogWarning($"[UnitDeck] Cannot add more than {MAX_UNITS} units");
                return false;
            }

            if (SelectedUnits.Contains(unitId))
            {
                Debug.LogWarning($"[UnitDeck] Unit already in deck: {unitId}");
                return false;
            }

            SelectedUnits.Add(unitId);
            Debug.Log($"[UnitDeck] Added unit: {unitId} ({SelectedUnits.Count}/{MAX_UNITS})");
            return true;
        }

        public bool RemoveUnit(string unitId)
        {
            if (SelectedUnits.Remove(unitId))
            {
                Debug.Log($"[UnitDeck] Removed unit: {unitId} ({SelectedUnits.Count}/{MAX_UNITS})");
                return true;
            }

            Debug.LogWarning($"[UnitDeck] Unit not in deck: {unitId}");
            return false;
        }

        public void SetHero(string heroId)
        {
            SelectedHero = heroId;
            Debug.Log($"[UnitDeck] Set hero: {heroId}");
        }

        public void ClearHero()
        {
            SelectedHero = null;
            Debug.Log("[UnitDeck] Cleared hero");
        }

        public bool IsValid()
        {
            bool hasCivilization = !string.IsNullOrEmpty(CivilizationID);
            bool hasUnits = SelectedUnits.Count == MAX_UNITS;
            bool hasHero = !string.IsNullOrEmpty(SelectedHero);

            if (!hasCivilization)
                Debug.LogWarning("[UnitDeck] Invalid: No civilization set");
            if (!hasUnits)
                Debug.LogWarning($"[UnitDeck] Invalid: Need {MAX_UNITS} units (have {SelectedUnits.Count})");
            if (!hasHero)
                Debug.LogWarning("[UnitDeck] Invalid: No hero set");

            return hasCivilization && hasUnits && hasHero;
        }

        public bool HasUnit(string unitId)
        {
            return SelectedUnits.Contains(unitId);
        }

        public int GetUnitCount()
        {
            return SelectedUnits.Count;
        }

        public void Clear()
        {
            CivilizationID = null;
            SelectedUnits.Clear();
            SelectedHero = null;
            Debug.Log("[UnitDeck] Cleared deck");
        }

        public UnitDeck Clone()
        {
            var clone = new UnitDeck();
            clone.CivilizationID = CivilizationID;
            clone.SelectedUnits = new List<string>(SelectedUnits);
            clone.SelectedHero = SelectedHero;
            return clone;
        }

        public override string ToString()
        {
            return $"UnitDeck(Civ: {CivilizationID}, Units: {SelectedUnits.Count}/{MAX_UNITS}, Hero: {SelectedHero})";
        }
    }
}
