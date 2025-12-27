using System;
using TowerConquest.Debug;
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

        [Header("Team")]
        public GoldManager.Team ownerTeam = GoldManager.Team.Player;

        [Header("Visual")]
        public Transform modelTransform;
        public ParticleSystem damageVFX;
        public ParticleSystem destroyVFX;

        [Header("HP Display")]
        public bool showHPDisplay = true;
        private GUIStyle hpStyle;

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

            // Set tag and team based on type
            if (isPlayerBase)
            {
                gameObject.tag = "PlayerBase";
                ownerTeam = GoldManager.Team.Player;
            }
            else
            {
                gameObject.tag = "EnemyBase";
                ownerTeam = GoldManager.Team.AI;
            }

            // Initialize HP display style
            hpStyle = new GUIStyle();
            hpStyle.fontSize = 14;
            hpStyle.fontStyle = FontStyle.Bold;
            hpStyle.alignment = TextAnchor.MiddleCenter;
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

            Log.Info($"[BaseController] {baseId} took {finalDamage} damage (raw: {rawDamage}, armor: {armor}). HP: {currentHp}/{maxHp}");

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

            Log.Info($"[BaseController] Base {baseId} destroyed!");

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
            Vector3 startScale = modelTransform != null ? modelTransform.localScale : Vector3.one;
            Vector3 startPos = modelTransform != null ? modelTransform.localPosition : Vector3.zero;

            while (elapsed < duration)
            {
                float t = elapsed / duration;
                if (modelTransform != null)
                {
                    modelTransform.localScale = Vector3.Lerp(startScale, startScale * 0.5f, t);
                    modelTransform.localPosition = startPos + Vector3.down * t * 2f;
                }
                elapsed += Time.deltaTime;
                yield return null;
            }

            // Actually destroy the game object after animation
            Destroy(gameObject, 0.5f);
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

        /// <summary>
        /// Apply damage to the base (wrapper for TakeDamage)
        /// </summary>
        public void ApplyDamage(float damage)
        {
            TakeDamage(damage);
        }

        private void OnGUI()
        {
            if (!showHPDisplay || IsDestroyed) return;
            if (Camera.main == null) return;

            // Convert world position to screen position
            Vector3 worldPos = transform.position + Vector3.up * 3f;
            Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos);

            // Check if in front of camera
            if (screenPos.z < 0) return;

            // Convert to GUI coordinates (Y is inverted)
            float guiY = Screen.height - screenPos.y;

            // Calculate HP percentage
            float hpPercent = currentHp / maxHp;

            // Set color based on HP
            if (hpPercent > 0.6f)
                hpStyle.normal.textColor = Color.green;
            else if (hpPercent > 0.3f)
                hpStyle.normal.textColor = Color.yellow;
            else
                hpStyle.normal.textColor = Color.red;

            // Draw HP bar background
            float barWidth = 80f;
            float barHeight = 12f;
            Rect bgRect = new Rect(screenPos.x - barWidth / 2, guiY - 25, barWidth, barHeight);
            GUI.Box(bgRect, "");

            // Draw HP bar fill
            Rect fillRect = new Rect(bgRect.x + 2, bgRect.y + 2, (barWidth - 4) * hpPercent, barHeight - 4);
            Color oldColor = GUI.color;
            GUI.color = hpStyle.normal.textColor;
            GUI.DrawTexture(fillRect, Texture2D.whiteTexture);
            GUI.color = oldColor;

            // Draw HP text
            string hpText = $"{Mathf.CeilToInt(currentHp)}/{Mathf.CeilToInt(maxHp)}";
            Rect textRect = new Rect(screenPos.x - 60, guiY - 5, 120, 20);
            GUI.Label(textRect, hpText, hpStyle);

            // Draw base name
            string baseName = isPlayerBase ? "YOUR BASE" : "ENEMY BASE";
            hpStyle.normal.textColor = isPlayerBase ? Color.cyan : Color.magenta;
            Rect nameRect = new Rect(screenPos.x - 60, guiY - 45, 120, 20);
            GUI.Label(nameRect, baseName, hpStyle);
        }
    }
}
