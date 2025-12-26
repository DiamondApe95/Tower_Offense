using System;
using System.Collections;
using TowerConquest.Combat;
using TowerConquest.Core;
using TowerConquest.Data;
using UnityEngine;

namespace TowerConquest.Gameplay.Entities
{
    public class HeroController : MonoBehaviour
    {
        public string HeroId { get; private set; }
        public bool IsDead { get; private set; }

        // Events
        public event Action<HeroController> OnHeroDied;

        private Coroutine auraRoutine;
        private HeroDefinition heroDefinition;
        private HealthComponent healthComponent;
        private float activeCooldownRemaining;
        private readonly EffectResolver effectResolver = new EffectResolver();

        public void Initialize(string heroId, float fallbackHp)
        {
            HeroId = heroId;
            IsDead = false;
            heroDefinition = ServiceLocator.Get<JsonDatabase>().FindHero(heroId);

            float hp = heroDefinition?.stats?.hp ?? fallbackHp;
            float armor = heroDefinition?.stats?.armor ?? 0f;

            healthComponent = GetComponent<HealthComponent>();
            if (healthComponent == null)
            {
                healthComponent = gameObject.AddComponent<HealthComponent>();
            }

            healthComponent.Initialize(hp, armor);
            healthComponent.OnDeath -= HandleDeath;
            healthComponent.OnDeath += HandleDeath;
            StartAura();
        }

        private void HandleDeath()
        {
            if (IsDead) return;
            IsDead = true;

            if (auraRoutine != null)
            {
                StopCoroutine(auraRoutine);
            }

            UnityEngine.Debug.Log($"HeroController: Hero {HeroId} died!");
            OnHeroDied?.Invoke(this);
        }

        private void OnDestroy()
        {
            if (healthComponent != null)
            {
                healthComponent.OnDeath -= HandleDeath;
            }
        }

        private void Update()
        {
            if (activeCooldownRemaining > 0f)
            {
                activeCooldownRemaining -= Time.deltaTime;
            }
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
            if (heroDefinition?.abilities == null)
            {
                yield break;
            }

            HeroDefinition.AbilityDto aura = GetAuraAbility();
            if (aura == null)
            {
                yield break;
            }

            float interval = Mathf.Max(0.5f, aura.interval_seconds);
            var wait = new WaitForSeconds(interval);

            while (true)
            {
                ApplyAbilityEffects(aura);
                yield return wait;
            }
        }

        public void ActivateSkill()
        {
            HeroDefinition.AbilityDto active = GetActiveAbility();
            if (active == null)
            {
                UnityEngine.Debug.LogWarning("HeroController: No active ability configured.");
                return;
            }

            if (activeCooldownRemaining > 0f)
            {
                UnityEngine.Debug.Log($"HeroController: Active ability on cooldown ({activeCooldownRemaining:0.0}s).");
                return;
            }

            ApplyAbilityEffects(active);
            activeCooldownRemaining = Mathf.Max(1f, active.cooldown_seconds);
            UnityEngine.Debug.Log($"HeroController: Activated ability '{active.id}' for hero {HeroId}.");
        }

        private void ApplyAbilityEffects(HeroDefinition.AbilityDto ability)
        {
            if (ability == null || ability.effects == null || ability.effects.Length == 0)
            {
                UnityEngine.Debug.LogWarning("HeroController: Ability has no effects.");
                return;
            }

            float radius = ResolveRadius(ability.effects);
            UnitController[] units = FindObjectsByType<UnitController>(FindObjectsSortMode.None);
            foreach (UnitController unit in units)
            {
                if (unit == null)
                {
                    continue;
                }

                float distance = Vector3.Distance(transform.position, unit.transform.position);
                if (distance <= radius)
                {
                    effectResolver.ApplyEffects(gameObject, unit.gameObject, ability.effects);
                    UnityEngine.Debug.Log($"HeroController: Applied {ability.id} to {unit.UnitId}.");
                }
            }
        }

        private HeroDefinition.AbilityDto GetAuraAbility()
        {
            if (heroDefinition?.abilities == null)
            {
                return null;
            }

            foreach (HeroDefinition.AbilityDto ability in heroDefinition.abilities)
            {
                if (ability == null)
                {
                    continue;
                }

                if (ability.type == "passive" || ability.trigger == "interval")
                {
                    return ability;
                }
            }

            return null;
        }

        private HeroDefinition.AbilityDto GetActiveAbility()
        {
            if (heroDefinition?.abilities == null)
            {
                return null;
            }

            foreach (HeroDefinition.AbilityDto ability in heroDefinition.abilities)
            {
                if (ability == null)
                {
                    continue;
                }

                if (ability.type == "active")
                {
                    return ability;
                }
            }

            return null;
        }

        private float ResolveRadius(HeroDefinition.EffectDto[] effects)
        {
            float maxRadius = 2.5f;
            foreach (HeroDefinition.EffectDto effect in effects)
            {
                if (effect?.area != null && effect.area.radius > maxRadius)
                {
                    maxRadius = effect.area.radius;
                }
            }

            return maxRadius;
        }
    }
}
