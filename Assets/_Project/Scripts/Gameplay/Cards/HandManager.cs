using System.Collections.Generic;

namespace TowerConquest.Gameplay.Cards
{
    public class HandManager
    {
        public List<string> hand = new();
        public int handSize = 5;

        public void FillStartingHandUnique(DeckManager deck)
        {
            if (deck == null)
            {
                return;
            }

            int safety = deck.drawPile.Count + deck.discardPile.Count + 10;
            while (hand.Count < handSize && safety-- > 0)
            {
                string card = deck.DrawOne();
                if (card == null)
                {
                    break;
                }

                if (hand.Contains(card))
                {
                    deck.Discard(card);
                    continue;
                }

                hand.Add(card);
            }
        }

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
