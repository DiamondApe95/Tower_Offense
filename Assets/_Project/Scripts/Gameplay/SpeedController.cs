using UnityEngine;
using TowerOffense.Core;

namespace TowerOffense.Gameplay
{
    public class SpeedController
    {
        public string Id { get; set; }
        public bool IsEnabled { get; set; }

        public float[] supportedSpeeds = new float[] { 1f, 2f };
        public int currentIndex = 0;
        public float CurrentSpeed => supportedSpeeds[currentIndex];

        public void SetSpeed(float speed)
        {
            UnityEngine.Debug.Log("Stub method called.");
        }

        public void Toggle()
        {
            if (supportedSpeeds == null || supportedSpeeds.Length == 0)
            {
                UnityEngine.Debug.LogWarning("SpeedController has no supported speeds configured.");
                return;
            }

            currentIndex = (currentIndex + 1) % supportedSpeeds.Length;
            ServiceLocator.Get<GameTime>().SetTimeScale(CurrentSpeed);
        }

    }
}
