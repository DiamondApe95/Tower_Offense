using System;
using UnityEngine;

namespace TowerConquest.Gameplay
{
    public class BaseController : MonoBehaviour
    {
        [Header("Base Stats")]
        public float maxHp = 1000f;
        public float currentHp = 1000f;
        [Range(0f, 1f)]
        public float armor;

        public event Action<BaseController> OnBaseDestroyed;

        private bool isDestroyed;

        public void Initialize(float hp, float armorValue)
        {
            maxHp = Mathf.Max(1f, hp);
            currentHp = maxHp;
            armor = Mathf.Clamp01(armorValue);
            isDestroyed = false;
            UnityEngine.Debug.Log($"BaseController: Initialized HP {currentHp}/{maxHp}, armor {armor:0.##}.");
        }

        public void ApplyDamage(float amount)
        {
            if (isDestroyed)
            {
                return;
            }

            if (amount <= 0f)
            {
                UnityEngine.Debug.LogWarning("BaseController.ApplyDamage called with non-positive damage.");
                return;
            }

            float mitigated = Mathf.Max(0f, amount * (1f - armor));
            currentHp = Mathf.Max(0f, currentHp - mitigated);
            UnityEngine.Debug.Log($"BaseController: Took {mitigated:0.##} damage (raw {amount:0.##}). HP {currentHp:0.##}/{maxHp:0.##}.");

            if (currentHp <= 0f && !isDestroyed)
            {
                isDestroyed = true;
                UnityEngine.Debug.Log("BaseController: Base destroyed.");
                OnBaseDestroyed?.Invoke(this);
            }
        }
    }
}
