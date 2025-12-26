using System.Collections.Generic;
using TowerConquest.Debug;
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
            Log.Info($"[UnitDeck] Set civilization: {civId}");
        }

        public bool AddUnit(string unitId)
        {
            if (SelectedUnits.Count >= MAX_UNITS)
            {
                Log.Warning($"[UnitDeck] Cannot add more than {MAX_UNITS} units");
                return false;
            }

            if (SelectedUnits.Contains(unitId))
            {
                Log.Warning($"[UnitDeck] Unit already in deck: {unitId}");
                return false;
            }

            SelectedUnits.Add(unitId);
            Log.Info($"[UnitDeck] Added unit: {unitId} ({SelectedUnits.Count}/{MAX_UNITS})");
            return true;
        }

        public bool RemoveUnit(string unitId)
        {
            if (SelectedUnits.Remove(unitId))
            {
                Log.Info($"[UnitDeck] Removed unit: {unitId} ({SelectedUnits.Count}/{MAX_UNITS})");
                return true;
            }

            Log.Warning($"[UnitDeck] Unit not in deck: {unitId}");
            return false;
        }

        public void SetHero(string heroId)
        {
            SelectedHero = heroId;
            Log.Info($"[UnitDeck] Set hero: {heroId}");
        }

        public void ClearHero()
        {
            SelectedHero = null;
            Log.Info("[UnitDeck] Cleared hero");
        }

        public bool IsValid()
        {
            bool hasCivilization = !string.IsNullOrEmpty(CivilizationID);
            bool hasUnits = SelectedUnits.Count == MAX_UNITS;
            bool hasHero = !string.IsNullOrEmpty(SelectedHero);

            if (!hasCivilization)
                Log.Warning("[UnitDeck] Invalid: No civilization set");
            if (!hasUnits)
                Log.Warning($"[UnitDeck] Invalid: Need {MAX_UNITS} units (have {SelectedUnits.Count})");
            if (!hasHero)
                Log.Warning("[UnitDeck] Invalid: No hero set");

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
            Log.Info("[UnitDeck] Cleared deck");
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
