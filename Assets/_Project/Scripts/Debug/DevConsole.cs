using UnityEngine;

namespace TowerOffense.Debug
{
    public class DevConsole
    {
        public string Id { get; set; }
        public bool IsEnabled { get; set; }

        public void Toggle()
        {
            Debug.Log("Stub method called.");
        }

    }
}
