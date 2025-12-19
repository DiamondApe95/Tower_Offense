using UnityEngine;

namespace TowerOffense.Saving
{
    public class RunSnapshot
    {
        public string Id { get; set; }
        public bool IsEnabled { get; set; }

        public void Capture()
        {
            UnityEngine.Debug.Log("Stub method called.");
        }

    }
}
