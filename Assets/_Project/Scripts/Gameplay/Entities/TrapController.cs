using TowerConquest.Combat;
using TowerConquest.Core;
using TowerConquest.Data;
using UnityEngine;

namespace TowerConquest.Gameplay.Entities
{
    public class TrapController : MonoBehaviour
    {
        public string trapId;
        public float cooldownSeconds = 1f;

        private float cooldownTimer;
        private TrapDefinition trapDefinition;
        private readonly EffectResolver effectResolver = new EffectResolver();

        public void Initialize(string trapDefinitionId, Vector3 size)
        {
            trapId = trapDefinitionId;
            trapDefinition = ServiceLocator.Get<JsonDatabase>().FindTrap(trapId);

            BoxCollider collider = GetComponent<BoxCollider>();
            if (collider == null)
            {
                collider = gameObject.AddComponent<BoxCollider>();
            }

            collider.isTrigger = true;
            collider.size = size;

            cooldownSeconds = trapDefinition?.trigger?.cooldown_seconds ?? 1f;
        }

        private void Update()
        {
            if (cooldownTimer > 0f)
            {
                cooldownTimer -= Time.deltaTime;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (cooldownTimer > 0f)
            {
                return;
            }

            UnitController unit = other.GetComponent<UnitController>();
            if (unit == null)
            {
                return;
            }

            if (trapDefinition?.effects == null || trapDefinition.effects.Length == 0)
            {
                UnityEngine.Debug.LogWarning($"Trap '{trapId}' has no effects.");
                return;
            }

            effectResolver.ApplyEffects(gameObject, unit.gameObject, trapDefinition.effects);
            cooldownTimer = Mathf.Max(0f, cooldownSeconds);
            UnityEngine.Debug.Log($"Trap '{trapId}' triggered on {unit.UnitId}.");
        }
    }
}
