using UnityEngine;

namespace TowerOffense.Gameplay.Entities
{
    public class TowerController
    {
        public string Id { get; set; }
        public bool IsEnabled { get; set; }

        public void Initialize()
        {
            UnityEngine.Debug.Log("Stub method called.");
        }

    }
}
