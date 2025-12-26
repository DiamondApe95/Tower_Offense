using System;
using UnityEngine;

namespace TowerConquest.Gameplay.Entities
{
    /// <summary>
    /// Controls a player or enemy base that must be destroyed to win/lose
    /// </summary>
    public class BaseController : MonoBehaviour
    {
        [Header("Base Settings")]
        public string baseId = "base_default";
        public float maxHp = 2000f;
        public float armor = 0.15f;
        public bool isPlayerBase = false;

        [Header("Visual")]
        public Transform modelTransform;
        public ParticleSystem damageVFX;
        public ParticleSystem destroyVFX;

        // Runtime state
        public float currentHp { get; private set; }
        public bool IsDestroyed { get; private set; }

        // Events
        public event Action<BaseController> OnBaseDestroyed;
        public event Action<BaseController, float> OnDamageTaken;

        private void Awake()
        {
            currentHp = maxHp;
            IsDestroyed = false;

            // Set tag based on type
            if (isPlayerBase)
            {
                gameObject.tag = "PlayerBase";
            }
            else
            {
                gameObject.tag = "EnemyBase";
            }
        }

        public void Initialize(float hp, float armorValue)
        {
            maxHp = hp;
            armor = armorValue;
            currentHp = maxHp;
            IsDestroyed = false;
        }

        public void TakeDamage(float rawDamage)
        {
            if (IsDestroyed) return;

            // Apply armor reduction
            float damageReduction = Mathf.Clamp01(armor);
            float finalDamage = rawDamage * (1f - damageReduction);

            currentHp -= finalDamage;

            OnDamageTaken?.Invoke(this, finalDamage);

            // Visual feedback
            if (damageVFX != null)
            {
                damageVFX.Play();
            }

            // Shake effect
            if (modelTransform != null)
            {
                StartCoroutine(ShakeEffect());
            }

            if (currentHp <= 0)
            {
                currentHp = 0;
                Destroy();
            }
        }

        private System.Collections.IEnumerator ShakeEffect()
        {
            Vector3 originalPos = modelTransform.localPosition;
            float elapsed = 0f;
            float duration = 0.2f;
            float magnitude = 0.1f;

            while (elapsed < duration)
            {
                float x = UnityEngine.Random.Range(-1f, 1f) * magnitude;
                float z = UnityEngine.Random.Range(-1f, 1f) * magnitude;
                modelTransform.localPosition = originalPos + new Vector3(x, 0, z);
                elapsed += Time.deltaTime;
                yield return null;
            }

            modelTransform.localPosition = originalPos;
        }

        private void Destroy()
        {
            if (IsDestroyed) return;

            IsDestroyed = true;

            Debug.Log($"[BaseController] Base {baseId} destroyed!");

            if (destroyVFX != null)
            {
                destroyVFX.Play();
            }

            OnBaseDestroyed?.Invoke(this);

            // Visual destruction
            if (modelTransform != null)
            {
                StartCoroutine(DestructionAnimation());
            }
        }

        private System.Collections.IEnumerator DestructionAnimation()
        {
            float duration = 1f;
            float elapsed = 0f;
            Vector3 startScale = modelTransform.localScale;
            Vector3 startPos = modelTransform.localPosition;

            while (elapsed < duration)
            {
                float t = elapsed / duration;
                modelTransform.localScale = Vector3.Lerp(startScale, startScale * 0.5f, t);
                modelTransform.localPosition = startPos + Vector3.down * t * 2f;
                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        public float GetHPPercent()
        {
            return currentHp / maxHp;
        }

        public void Heal(float amount)
        {
            if (IsDestroyed) return;

            currentHp = Mathf.Min(maxHp, currentHp + amount);
        }
    }
}
