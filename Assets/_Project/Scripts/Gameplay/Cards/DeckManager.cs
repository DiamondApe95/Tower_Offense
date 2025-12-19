using System;
using System.Collections.Generic;
using UnityEngine;

namespace TowerOffense.Gameplay.Cards
{
    public class DeckManager
    {
        public List<string> drawPile = new();
        public List<string> discardPile = new();

        public void Initialize(List<string> cards, int seed)
        {
            drawPile = cards == null ? new List<string>() : new List<string>(cards);
            discardPile.Clear();
            Shuffle(drawPile, seed);
        }

        public string DrawOne()
        {
            if (drawPile.Count == 0 && discardPile.Count > 0)
            {
                drawPile.AddRange(discardPile);
                discardPile.Clear();
                Shuffle(drawPile, Environment.TickCount);
            }

            if (drawPile.Count == 0)
            {
                return null;
            }

            string card = drawPile[^1];
            drawPile.RemoveAt(drawPile.Count - 1);
            return card;
        }

        public void Discard(string cardId)
        {
            if (string.IsNullOrEmpty(cardId))
            {
                return;
            }

            discardPile.Add(cardId);
        }

        public void DebugDemo()
        {
            var demoCards = new List<string>
            {
                "unit_knight",
                "unit_archer",
                "unit_mage",
                "spell_fireball",
                "spell_freeze",
                "unit_golem",
                "spell_heal",
                "unit_rogue"
            };

            Initialize(demoCards, 12345);

            var handManager = new HandManager();
            handManager.FillToHandSize(this);
            Debug.Log($"Demo hand size after draw: {handManager.hand.Count}");

            if (handManager.hand.Count > 0)
            {
                var resolver = new CardPlayResolver();
                var cardToPlay = handManager.hand[0];
                handManager.PlayCard(cardToPlay, this, resolver);
                Debug.Log($"Played card: {cardToPlay}");
            }
        }

        private static void Shuffle(List<string> cards, int seed)
        {
            if (cards == null || cards.Count <= 1)
            {
                return;
            }

            var random = new System.Random(seed);
            for (int i = cards.Count - 1; i > 0; i--)
            {
                int swapIndex = random.Next(i + 1);
                (cards[i], cards[swapIndex]) = (cards[swapIndex], cards[i]);
            }
        }
    }
}
