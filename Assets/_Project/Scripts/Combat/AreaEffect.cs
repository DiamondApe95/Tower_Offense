using System.Collections.Generic;
using UnityEngine;
using TowerOffense.Data;

namespace TowerOffense.Combat
{
    public class AreaEffect : MonoBehaviour
    {
        public float radius = 2f;
        public float duration = 0.2f;
        public SpellDefinition.EffectDto[] effects;
        public GameObject source;

        private readonly EffectResolver resolver = new EffectResolver();

        private void Start()
        {
            Apply();
            if (duration <= 0f)
            {
                Destroy(gameObject);
                return;
            }

            Destroy(gameObject, duration);
        }

        private void Apply()
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, radius);
            if (hits == null || hits.Length == 0)
            {
                return;
            }

            var seen = new HashSet<GameObject>();
            foreach (Collider hit in hits)
            {
                if (hit == null)
                {
                    continue;
                }

                GameObject target = hit.gameObject;
                if (!seen.Add(target))
                {
                    continue;
                }

                resolver.ApplyEffects(source, target, effects);
            }
        }
    }
}
