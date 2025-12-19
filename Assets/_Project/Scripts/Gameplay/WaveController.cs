using UnityEngine;

namespace TowerOffense.Gameplay
{
    public class WaveController
    {
        public string Id { get; set; }
        public bool IsEnabled { get; set; }

        public void StartWave(int waveIndex)
        {
            UnityEngine.Debug.Log("Stub method called.");
        }

    }
}
