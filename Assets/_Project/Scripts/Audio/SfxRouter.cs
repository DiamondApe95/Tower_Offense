using UnityEngine;

namespace TowerOffense.Audio
{
    public class SfxRouter
    {
        public string Id { get; set; }
        public bool IsEnabled { get; set; }

        public void Route(string sfxId)
        {
            UnityEngine.Debug.Log("Stub method called.");
        }

    }
}
