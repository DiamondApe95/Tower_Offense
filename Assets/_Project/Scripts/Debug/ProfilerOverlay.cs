using UnityEngine;

namespace TowerOffense.Debug
{
    public class ProfilerOverlay
    {
        public string Id { get; set; }
        public bool IsEnabled { get; set; }

        public void Show()
        {
            UnityEngine.Debug.Log("Stub method called.");
        }

    }
}
