using UnityEngine;

namespace TowerConquest.Combat
{
    public class ArmorShredStatus : MonoBehaviour
    {
        private float duration;
        private float shredAmount;

        public void Initialize(float amount, float durationSeconds)
        {
            duration = durationSeconds;
            shredAmount = Mathf.Clamp01(amount);
            ApplyShred();
        }

        public void Refresh(float amount, float durationSeconds)
        {
            float newAmount = Mathf.Clamp01(amount);
            if (newAmount > shredAmount)
            {
                RemoveShred();
                shredAmount = newAmount;
                ApplyShred();
            }

            duration = Mathf.Max(duration, durationSeconds);
        }

        private void Update()
        {
            if (duration <= 0f)
            {
                Destroy(this);
                return;
            }

            duration -= Time.deltaTime;
        }

        private void ApplyShred()
        {
            HealthComponent health = GetComponent<HealthComponent>();
            if (health != null)
            {
                health.ApplyArmorModifier(-shredAmount);
                UnityEngine.Debug.Log($"{name} armor shred applied ({shredAmount:0.##}).");
            }
        }

        private void RemoveShred()
        {
            HealthComponent health = GetComponent<HealthComponent>();
            if (health != null)
            {
                health.ApplyArmorModifier(shredAmount);
            }
        }

        private void OnDestroy()
        {
            RemoveShred();
        }
    }
}
