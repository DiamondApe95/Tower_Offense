using System;
using System.Collections.Generic;
using UnityEngine;

namespace TowerOffense.UI
{
    public class CardHandView
    {
        private readonly Transform handContainer;
        private readonly CardView cardViewPrefab;

        public CardHandView(Transform handContainer, CardView cardViewPrefab)
        {
            this.handContainer = handContainer;
            this.cardViewPrefab = cardViewPrefab;
        }

        public void Render(List<string> hand, Action<string> onClick)
        {
            if (handContainer == null || cardViewPrefab == null)
            {
                Debug.LogWarning("CardHandView missing container or prefab reference.");
                return;
            }

            for (int i = handContainer.childCount - 1; i >= 0; i--)
            {
                var child = handContainer.GetChild(i);
                UnityEngine.Object.Destroy(child.gameObject);
            }

            if (hand == null)
            {
                return;
            }

            foreach (string cardId in hand)
            {
                var cardView = UnityEngine.Object.Instantiate(cardViewPrefab, handContainer);
                cardView.Bind(cardId, onClick);
            }
        }
    }
}
