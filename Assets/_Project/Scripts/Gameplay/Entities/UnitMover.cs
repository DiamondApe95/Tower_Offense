using UnityEngine;

namespace TowerOffense.Gameplay.Entities
{
    public class UnitMover
    {
        public string Id { get; set; }
        public bool IsEnabled { get; set; }

        public void Move(Vector3 destination)
        {
            UnityEngine.Debug.Log("Stub method called.");
        }

    }
}
