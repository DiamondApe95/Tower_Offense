using UnityEngine;

namespace TowerConquest.Audio
{
    public class AudioManager
    {
        public string Id { get; set; }
        public bool IsEnabled { get; set; }

        public void PlaySfx(string sfxId)
        {
            UnityEngine.Debug.Log("Stub method called.");
        }

        public void PlayMusic(string musicId)
        {
            UnityEngine.Debug.Log("Stub method called.");
        }

    }
}
