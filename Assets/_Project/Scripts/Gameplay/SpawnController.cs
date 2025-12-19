using UnityEngine;

namespace TowerOffense.Gameplay
{
    public class SpawnController
    {
        public string Id { get; set; }
        public bool IsEnabled { get; set; }

        public void Spawn(string unitId)
        {
            UnityEngine.Debug.Log("Stub method called.");
        }

    }
}
