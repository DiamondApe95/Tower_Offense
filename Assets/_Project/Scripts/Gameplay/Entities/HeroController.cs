using System.Collections;
using TowerOffense.Combat;
using UnityEngine;

namespace TowerOffense.Gameplay.Entities
{
    public class HeroController : MonoBehaviour
    {
        public string HeroId { get; private set; }

        private Coroutine auraRoutine;

        public void Initialize(string heroId, float hp)
        {
            HeroId = heroId;

            HealthComponent health = GetComponent<HealthComponent>();
            if (health == null)
            {
                health = gameObject.AddComponent<HealthComponent>();
            }

            health.Initialize(hp);
            StartAura();
        }

        private void StartAura()
        {
            if (auraRoutine != null)
            {
                StopCoroutine(auraRoutine);
            }

            auraRoutine = StartCoroutine(AuraTick());
        }

        private IEnumerator AuraTick()
        {
            var wait = new WaitForSeconds(2f);
            while (true)
            {
                Debug.Log($"Hero aura tick ({HeroId})");
                yield return wait;
            }
        }

        public void ActivateSkill()
        {
            Debug.Log("Hero skill activated.");
        }

    }
}
