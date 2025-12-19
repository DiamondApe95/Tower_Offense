using System;
using UnityEngine;
using UnityEngine.UI;

namespace TowerOffense.UI
{
    public class CardView : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Text label;

        public void Bind(string cardId, Action<string> onClick)
        {
            if (label != null)
            {
                label.text = cardId;
            }

            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => onClick?.Invoke(cardId));
            }
        }
    }
}
