using UnityEngine;
using TowerConquest.Debug;

namespace TowerConquest.Core
{
    public class GameTime
    {
        public string Id { get; set; }
        public bool IsEnabled { get; set; }

        public void Tick(float deltaTime)
        {
            Log.Info("Stub method called.");
        }

        public void SetTimeScale(float scale)
        {
            Time.timeScale = scale;
            Log.Info($"GameTime set Time.timeScale to {scale}.");
        }

    }
}
