using UnityEngine;
using TowerConquest.Debug;

namespace TowerConquest.UI
{
    public class WavePanelView
    {
        public string Id { get; set; }
        public bool IsEnabled { get; set; }

        public void SetWave(int waveIndex)
        {
            Log.Info("Stub method called.");
        }

    }
}
