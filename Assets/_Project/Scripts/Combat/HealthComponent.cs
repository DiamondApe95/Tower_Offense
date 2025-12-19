using System;
using UnityEngine;

namespace TowerConquest.Combat
{
    public class HealthComponent : MonoBehaviour
    {
        public float maxHp;
        public float currentHp;
        [Range(0f, 1f)]
        public float armor;

        public event Action<HealthComponent> OnDied;

        public void Initialize(float hp)
        {
            Initialize(hp, 0f);
        }

        public void Initialize(float hp, float armorValue)
        {
            maxHp = hp;
            currentHp = hp;
            armor = Mathf.Clamp01(armorValue);
        }

        public void ApplyDamage(float dmg)
        {
            if (currentHp <= 0f)
            {
                return;
            }

            float mitigated = Mathf.Max(0f, dmg * (1f - armor));
            currentHp -= mitigated;
            Debug.Log($"{name} took {mitigated:0.##} damage (raw {dmg:0.##}). HP: {currentHp:0.##}/{maxHp:0.##}");

            if (currentHp <= 0f)
            {
                Debug.Log($"{name} died.");
                OnDied?.Invoke(this);
                Destroy(gameObject);
            }
        }

        public void Heal(float amount)
        {
            if (currentHp <= 0f)
            {
                return;
            }

            currentHp = Mathf.Min(maxHp, currentHp + amount);
            Debug.Log($"{name} healed {amount}. HP: {currentHp}/{maxHp}");
        }

        public void ApplyArmorModifier(float amount)
        {
            armor = Mathf.Clamp01(armor + amount);
            Debug.Log($"{name} armor modified by {amount:0.##}. Armor now {armor:0.##}");
        }
    }
}
