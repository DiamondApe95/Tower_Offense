using UnityEngine;

namespace TowerOffense.Gameplay.Cards
{
    public class CardPlayResolver
    {
        public void Play(string cardId)
        {
            if (string.IsNullOrEmpty(cardId))
            {
                Debug.LogWarning("Play request received with empty card id.");
                return;
            }

            if (cardId.StartsWith("unit_"))
            {
                Debug.Log($"Spawn unit request: {cardId}");
            }
            else if (cardId.StartsWith("spell_"))
            {
                Debug.Log($"Cast spell request: {cardId}");
            }
            else
            {
                Debug.LogWarning($"Unknown card type: {cardId}");
            }
        }
    }
}
