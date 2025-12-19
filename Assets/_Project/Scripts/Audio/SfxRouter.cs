using UnityEngine;

namespace TowerOffense.Audio
{
    public class SfxRouter
    {
        public string Id { get; set; }
        public bool IsEnabled { get; set; }

        public void Route(string sfxId)
        {
            Debug.Log("Stub method called.");
        }

    }
}
