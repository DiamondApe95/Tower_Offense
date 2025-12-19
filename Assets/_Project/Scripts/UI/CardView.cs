using UnityEngine;
using TowerOffense.Gameplay.Cards;

namespace TowerOffense.UI
{
    public class CardView
    {
        public string Id { get; set; }
        public bool IsEnabled { get; set; }

        public void Bind(CardViewModel model)
        {
            UnityEngine.Debug.Log("Stub method called.");
        }

    }
}
