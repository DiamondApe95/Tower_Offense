using UnityEngine;
using TowerConquest.Debug;

namespace TowerConquest.UI
{
    public class UIRoot
    {
        public string Id { get; set; }
        public bool IsEnabled { get; set; }

        public void Show()
        {
            Log.Info("Stub method called.");
        }

    }
}
