using UnityEngine;

namespace TowerOffense.Audio
{
    public class AudioManager
    {
        public string Id { get; set; }
        public bool IsEnabled { get; set; }

        public void PlaySfx(string sfxId)
        {
            Debug.Log("Stub method called.");
        }

        public void PlayMusic(string musicId)
        {
            Debug.Log("Stub method called.");
        }

    }
}
