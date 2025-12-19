using UnityEngine;

namespace TowerOffense.Audio
{
    public class MusicController
    {
        public string Id { get; set; }
        public bool IsEnabled { get; set; }

        public void Crossfade(string musicId)
        {
            UnityEngine.Debug.Log("Stub method called.");
        }

    }
}
