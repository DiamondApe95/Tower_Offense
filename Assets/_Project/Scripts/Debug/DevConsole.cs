using UnityEngine;
using TowerConquest.Debug;

namespace TowerConquest.Debug
{
    public class DevConsole
    {
        public string Id { get; set; }
        public bool IsEnabled { get; set; }

        public void Toggle()
        {
            Log.Info("Stub method called.");
        }

    }
}
