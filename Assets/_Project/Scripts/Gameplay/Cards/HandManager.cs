using System.Collections.Generic;

namespace TowerOffense.Gameplay.Cards
{
    public class HandManager
    {
        public List<string> hand = new();
        public int handSize = 5;

        public void FillToHandSize(DeckManager deck)
        {
            if (deck == null)
            {
                return;
            }

            while (hand.Count < handSize)
            {
                string card = deck.DrawOne();
                if (card == null)
                {
                    break;
                }

                hand.Add(card);
            }
        }

        public bool PlayCard(string cardId, DeckManager deck, CardPlayResolver resolver)
        {
            if (deck == null || resolver == null)
            {
                return false;
            }

            int index = hand.IndexOf(cardId);
            if (index < 0)
            {
                return false;
            }

            hand.RemoveAt(index);
            resolver.Play(cardId);
            deck.Discard(cardId);
            FillToHandSize(deck);
            return true;
        }
    }
}
