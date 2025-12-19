using UnityEngine;

namespace TowerOffense.Core
{
    public class GameConfig
    {
        public string Id { get; set; }
        public bool IsEnabled { get; set; }

        public void ApplyDefaults()
        {
            Debug.Log("Stub method called.");
        }

    }
}
