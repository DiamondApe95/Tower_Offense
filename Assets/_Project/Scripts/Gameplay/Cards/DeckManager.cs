using System.Collections.Generic;
using UnityEngine;

namespace TowerOffense.Gameplay.Cards
{
    public class DeckManager
    {
        public string Id { get; set; }
        public bool IsEnabled { get; set; }
        public List<string> cards = new();

        private System.Random rng;

        public void Initialize(IEnumerable<string> cardIds, int seed)
        {
            cards = new List<string>(cardIds);
            rng = new System.Random(seed);
            Shuffle();
        }

        public void Shuffle()
        {
            if (rng == null)
            {
                rng = new System.Random();
            }

            for (int i = cards.Count - 1; i > 0; i--)
            {
                int swapIndex = rng.Next(i + 1);
                (cards[i], cards[swapIndex]) = (cards[swapIndex], cards[i]);
            }
        }

        public string Draw()
        {
            if (cards.Count == 0)
            {
                Debug.LogWarning("Deck is empty.");
                return null;
            }

            int lastIndex = cards.Count - 1;
            string cardId = cards[lastIndex];
            cards.RemoveAt(lastIndex);
            return cardId;
        }
    }
}
