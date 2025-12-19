using System.Collections.Generic;
using UnityEngine;

namespace TowerOffense.Gameplay.Cards
{
    public class HandManager
    {
        public string Id { get; set; }
        public bool IsEnabled { get; set; }
        public int handSize = 5;
        public List<string> hand = new();

        public void Draw(int count)
        {
            Debug.Log($"Stub draw {count}.");
        }

        public void FillToHandSize(DeckManager deck)
        {
            if (deck == null)
            {
                Debug.LogWarning("No deck available to draw from.");
                return;
            }

            while (hand.Count < handSize)
            {
                string cardId = deck.Draw();
                if (string.IsNullOrEmpty(cardId))
                {
                    break;
                }

                hand.Add(cardId);
            }
        }
    }
}
