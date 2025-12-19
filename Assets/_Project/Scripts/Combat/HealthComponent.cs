using System;
using UnityEngine;

namespace TowerOffense.Combat
{
    public class HealthComponent : MonoBehaviour
    {
        [SerializeField] private float maxHp = 100f;
        [SerializeField] private float currentHp = 100f;

        public float MaxHp => maxHp;
        public float CurrentHp => currentHp;

        public event Action<HealthComponent> OnDied;

        public void Initialize(float hp)
        {
            maxHp = Mathf.Max(1f, hp);
            currentHp = maxHp;
        }

        public void ApplyDamage(float dmg)
        {
            if (dmg <= 0f)
            {
                return;
            }

            currentHp = Mathf.Max(0f, currentHp - dmg);
            Debug.Log($"{name} took {dmg} damage. HP {currentHp}/{maxHp}.");

            if (currentHp <= 0f)
            {
                Debug.Log($"{name} died.");
                OnDied?.Invoke(this);
                Destroy(gameObject);
            }
        }
    }
}
