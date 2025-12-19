using UnityEngine;

namespace TowerOffense.UI
{
    public class LevelHUD
    {
        public string Id { get; set; }
        public bool IsEnabled { get; set; }

        public void UpdateHUD()
        {
            UnityEngine.Debug.Log("Stub method called.");
        }

    }
}
