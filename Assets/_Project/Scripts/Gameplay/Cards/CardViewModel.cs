using UnityEngine;

namespace TowerOffense.Gameplay.Cards
{
    public class CardViewModel
    {
        public string Id { get; set; }
        public bool IsEnabled { get; set; }

        public void Refresh()
        {
            Debug.Log("Stub method called.");
        }

    }
}
