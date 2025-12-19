using UnityEngine;

namespace TowerOffense.Gameplay.Entities
{
    public class UnitController
    {
        public string Id { get; set; }
        public bool IsEnabled { get; set; }

        public void Initialize()
        {
            UnityEngine.Debug.Log("Stub method called.");
        }

    }
}
