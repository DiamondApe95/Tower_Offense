using UnityEngine;
using TowerConquest.Core;

namespace TowerConquest.Gameplay
{
    public class SpeedController
    {
        public float[] supportedSpeeds = new float[] { 1f, 2f, 3f };
        public int currentIndex = 0;
        public float CurrentSpeed => supportedSpeeds[currentIndex];

        public void SetSpeed(float speed)
        {
            if (supportedSpeeds == null || supportedSpeeds.Length == 0)
            {
                UnityEngine.Debug.LogWarning("SpeedController has no supported speeds configured.");
                return;
            }

            for (int index = 0; index < supportedSpeeds.Length; index++)
            {
                if (Mathf.Approximately(supportedSpeeds[index], speed))
                {
                    currentIndex = index;
                    ServiceLocator.Get<GameTime>().SetTimeScale(CurrentSpeed);
                    return;
                }
            }

            UnityEngine.Debug.LogWarning($"SpeedController: speed {speed} not supported.");
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
