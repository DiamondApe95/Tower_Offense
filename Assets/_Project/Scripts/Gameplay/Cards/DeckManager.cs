using UnityEngine;

namespace TowerOffense.Gameplay.Cards
{
    public class DeckManager
    {
        public string Id { get; set; }
        public bool IsEnabled { get; set; }

        public void Shuffle()
        {
            UnityEngine.Debug.Log("Stub method called.");
        }

    }
}
