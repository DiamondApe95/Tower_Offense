using UnityEngine;

namespace TowerOffense.Gameplay.Cards
{
    public class CardPlayResolver
    {
        public string Id { get; set; }
        public bool IsEnabled { get; set; }

        public void Resolve(string cardId)
        {
            Debug.Log("Stub method called.");
        }

    }
}
