using UnityEngine;

namespace TowerConquest.Debug
{
    public class DevConsole
    {
        public string Id { get; set; }
        public bool IsEnabled { get; set; }

        public void Toggle()
        {
            UnityEngine.Debug.Log("Stub method called.");
        }

    }
}
