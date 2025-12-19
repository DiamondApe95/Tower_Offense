using System;
using UnityEngine;

namespace TowerOffense.Combat
{
    public class HealthComponent : MonoBehaviour
    {
        public float maxHp;
        public float currentHp;

        public event Action<HealthComponent> OnDied;

        public void Initialize(float hp)
        {
            maxHp = hp;
            currentHp = hp;
        }

        public void ApplyDamage(float dmg)
        {
            if (currentHp <= 0f)
            {
                return;
            }

            currentHp -= dmg;
            Debug.Log($"{name} took {dmg} damage. HP: {currentHp}/{maxHp}");

            if (currentHp <= 0f)
            {
                Debug.Log($"{name} died.");
                OnDied?.Invoke(this);
                Destroy(gameObject);
            }
        }
    }
}
