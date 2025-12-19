using UnityEngine;

namespace TowerOffense.Gameplay
{
    public class SpeedController
    {
        public string Id { get; set; }
        public bool IsEnabled { get; set; }

        public void SetSpeed(float speed)
        {
            UnityEngine.Debug.Log("Stub method called.");
        }

    }
}
