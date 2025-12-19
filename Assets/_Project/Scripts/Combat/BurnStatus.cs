using System.Collections;
using UnityEngine;

namespace TowerOffense.Combat
{
    public class BurnStatus : MonoBehaviour
    {
        private float tickDamage;
        private float tickInterval;
        private float duration;
        private Coroutine burnRoutine;

        public void Initialize(float damagePerTick, float intervalSeconds, float durationSeconds)
        {
            tickDamage = damagePerTick;
            tickInterval = Mathf.Max(0.1f, intervalSeconds);
            duration = durationSeconds;

            if (burnRoutine != null)
            {
                StopCoroutine(burnRoutine);
            }

            burnRoutine = StartCoroutine(DoBurn());
        }

        private IEnumerator DoBurn()
        {
            float elapsed = 0f;
            var wait = new WaitForSeconds(tickInterval);

            while (elapsed < duration)
            {
                HealthComponent health = GetComponent<HealthComponent>();
                if (health != null)
                {
                    health.ApplyDamage(tickDamage);
                }

                yield return wait;
                elapsed += tickInterval;
            }

            Destroy(this);
        }
    }
}
