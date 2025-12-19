using System;
using System.Collections.Generic;
using UnityEngine;

namespace TowerOffense.UI
{
    public class CardHandView
    {
        public Transform handContainer;
        public CardView cardViewPrefab;

        public void Render(List<string> hand, Action<string> onClick)
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

            foreach (string cardId in hand)
            {
                if (string.IsNullOrEmpty(cardId))
                {
                    continue;
                }

                CardView viewInstance = UnityEngine.Object.Instantiate(cardViewPrefab, handContainer);
                viewInstance.Bind(cardId, onClick);
            }
        }

    }
}
