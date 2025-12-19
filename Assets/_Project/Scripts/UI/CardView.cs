using System;
using UnityEngine;
using UnityEngine.UI;

namespace TowerOffense.UI
{
    public class CardView : MonoBehaviour
    {
        public Button button;
        public Text label;

        public void Bind(string cardId, Action<string> onClick)
        {
            if (label != null)
            {
                label.text = cardId;
            }

            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                if (onClick != null)
                {
                    button.onClick.AddListener(() => onClick(cardId));
                }
            }
        }

    }
}
