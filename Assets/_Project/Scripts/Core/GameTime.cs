using UnityEngine;

namespace TowerOffense.Core
{
    public class GameTime
    {
        public string Id { get; set; }
        public bool IsEnabled { get; set; }

        public void Tick(float deltaTime)
        {
            UnityEngine.Debug.Log("Stub method called.");
        }

    }
}
