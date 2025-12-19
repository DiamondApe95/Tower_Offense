using UnityEngine;

namespace TowerOffense.UI
{
    public class UIRoot
    {
        public string Id { get; set; }
        public bool IsEnabled { get; set; }

        public void Show()
        {
            UnityEngine.Debug.Log("Stub method called.");
        }

    }
}
