using UnityEngine;
using TowerConquest.Debug;

namespace TowerConquest.Audio
{
    public class MusicController
    {
        public string Id { get; set; }
        public bool IsEnabled { get; set; }

        public void Crossfade(string musicId)
        {
            Log.Info("Stub method called.");
        }

    }
}
