using UnityEngine;

namespace TowerOffense.Core
{
    public class GameBootstrapper
    {
        public string Id { get; set; }
        public bool IsEnabled { get; set; }

        public void Initialize()
        {
            UnityEngine.Debug.Log("Stub method called.");
        }

        public void Shutdown()
        {
            UnityEngine.Debug.Log("Stub method called.");
        }

    }
}
