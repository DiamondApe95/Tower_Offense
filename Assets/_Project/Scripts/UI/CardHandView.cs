using System;
using System.Collections.Generic;
using UnityEngine;
using TowerConquest.Gameplay.Cards;

namespace TowerConquest.UI
{
    public class CardHandView
    {
        public Transform handContainer;
        public CardView cardViewPrefab;

        public void Render(List<CardViewModel> hand, Action<string> onClick)
        {
            if (handContainer == null || cardViewPrefab == null)
            {
                return;
            }

            for (int i = handContainer.childCount - 1; i >= 0; i--)
            {
                Transform child = handContainer.GetChild(i);
                if (child != null)
                {
                    UnityEngine.Object.Destroy(child.gameObject);
                }
            }

            if (hand == null)
            {
                return;
            }

            foreach (CardViewModel card in hand)
            {
                if (card == null || string.IsNullOrEmpty(card.cardId))
                {
                    continue;
                }

                CardView viewInstance = UnityEngine.Object.Instantiate(cardViewPrefab, handContainer);
                viewInstance.Bind(card, onClick);
            }
        }

    }
}
