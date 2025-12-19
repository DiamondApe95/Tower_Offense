using System;
using TowerConquest.Gameplay.Cards;
using UnityEngine;
using UnityEngine.UI;

namespace TowerConquest.UI
{
    public class CardView : MonoBehaviour
    {
        public Button button;
        public Text label;
        public Text costLabel;
        public Color playableColor = Color.white;
        public Color disabledColor = Color.gray;

        public void Bind(CardViewModel model, Action<string> onClick)
        {
            if (model == null)
            {
                return;
            }

            string nameText = string.IsNullOrWhiteSpace(model.displayName) ? model.cardId : model.displayName;
            if (label != null)
            {
                label.text = costLabel == null ? $"{nameText} ({model.cost})" : nameText;
                label.color = model.isPlayable ? playableColor : disabledColor;
            }

            if (costLabel != null)
            {
                costLabel.text = model.cost.ToString();
                costLabel.color = model.isPlayable ? playableColor : disabledColor;
            }

            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.interactable = model.isPlayable;
                if (onClick != null && model.isPlayable)
                {
                    button.onClick.AddListener(() => onClick(model.cardId));
                }
            }
        }

    }
}
