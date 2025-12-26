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

        public float CurrentHp => currentHp;
        public float MaxHp => maxHp;
        public float Armor => armor;
        public float HealthPercent => maxHp > 0f ? currentHp / maxHp : 0f;
        public bool IsDead => currentHp <= 0f;

        // Events
        public event Action<HealthComponent> OnDied;
        public event Action OnDeath;
        public event Action<float, string, GameObject> OnDamaged;
        public event Action<float> OnHealed;

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

        public void TakeDamage(float dmg, string damageType, GameObject source)
        {
            if (currentHp <= 0f)
            {
                return;
            }

            float mitigated = Mathf.Max(0f, dmg * (1f - armor));
            currentHp -= mitigated;

            OnDamaged?.Invoke(mitigated, damageType, source);

            if (currentHp <= 0f)
            {
                currentHp = 0f;
                UnityEngine.Debug.Log($"{name} died.");
                OnDied?.Invoke(this);
                OnDeath?.Invoke();
                Destroy(gameObject);
            }
        }

        public void ApplyDamage(float dmg)
        {
            TakeDamage(dmg, "physical", null);
        }

        public void Heal(float amount)
        {
            if (currentHp <= 0f)
            {
                return;
            }

            float healedAmount = Mathf.Min(maxHp - currentHp, amount);
            currentHp = Mathf.Min(maxHp, currentHp + amount);
            OnHealed?.Invoke(healedAmount);
        }

        public void ApplyArmorModifier(float amount)
        {
            armor = Mathf.Clamp01(armor + amount);
        }

        /// <summary>
        /// Set armor to a specific value
        /// </summary>
        public void SetArmor(float value)
        {
            armor = Mathf.Clamp01(value);
        }

        public void ResetHealth()
        {
            currentHp = maxHp;
        }
    }
}
