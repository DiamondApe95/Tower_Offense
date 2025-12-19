using UnityEngine;
using TowerOffense.Data;

namespace TowerOffense.Core
{
    public class GameBootstrapper
    {
        public string Id { get; set; }
        public bool IsEnabled { get; set; }

        public void Initialize()
        {
            var db = new JsonDatabase();
            db.LoadAll();
            DataValidator.ValidateAll(db);

            Debug.Log($"Loaded Units: {db.Units.Count}, Spells: {db.Spells.Count}, Towers: {db.Towers.Count}, Traps: {db.Traps.Count}, Levels: {db.Levels.Count}.");
        }

        public void Shutdown()
        {
            UnityEngine.Debug.Log("Stub method called.");
        }

    }
}
