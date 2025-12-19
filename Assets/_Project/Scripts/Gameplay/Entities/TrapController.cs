using UnityEngine;

namespace TowerOffense.Gameplay.Entities
{
    public class TrapController
    {
        public string Id { get; set; }
        public bool IsEnabled { get; set; }

        public void Trigger()
        {
            UnityEngine.Debug.Log("Stub method called.");
        }

    }
}
