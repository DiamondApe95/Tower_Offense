using System;
using UnityEngine;
using TowerConquest.Data;

namespace TowerConquest.Combat
{
    public class EffectResolver
    {
        private readonly StatusSystem statusSystem = new StatusSystem();

        public void ApplyEffects(GameObject source, GameObject target, SpellDefinition.EffectDto[] effects)
        {
            ApplyEffectsInternal(
                target,
                effects,
                effect => effect.effect_type,
                effect => effect.value,
                effect => effect.mode,
                effect => effect.stat,
                effect => effect.status,
                status => status.apply,
                status => status.duration_seconds,
                status => status.slow_percent,
                status => status.tick_damage,
                status => status.tick_interval_seconds);
        }

        public void ApplyEffects(GameObject source, GameObject target, UnitDefinition.EffectDto[] effects)
        {
            ApplyEffectsInternal(
                target,
                effects,
                effect => effect.effect_type,
                effect => effect.value,
                effect => effect.mode,
                effect => effect.stat,
                effect => effect.status,
                status => status.apply,
                status => status.duration_seconds,
                status => status.slow_percent,
                status => status.tick_damage,
                status => status.tick_interval_seconds);
        }

        public void ApplyEffects(GameObject source, GameObject target, TowerDefinition.EffectDto[] effects)
        {
            ApplyEffectsInternal(
                target,
                effects,
                effect => effect.effect_type,
                effect => effect.value,
                effect => effect.mode,
                effect => effect.stat,
                effect => effect.status,
                status => status.apply,
                status => status.duration_seconds,
                status => status.slow_percent,
                status => status.tick_damage,
                status => status.tick_interval_seconds);
        }

        public void ApplyEffects(GameObject source, GameObject target, TrapDefinition.EffectDto[] effects)
        {
            ApplyEffectsInternal(
                target,
                effects,
                effect => effect.effect_type,
                effect => effect.value,
                effect => effect.mode,
                effect => effect.stat,
                effect => effect.status,
                status => status.apply,
                status => status.duration_seconds,
                status => status.slow_percent,
                status => status.tick_damage,
                status => status.tick_interval_seconds);
        }

        public void ApplyEffects(GameObject source, GameObject target, HeroDefinition.EffectDto[] effects)
        {
            ApplyEffectsInternal(
                target,
                effects,
                effect => effect.effect_type,
                effect => effect.value,
                effect => effect.mode,
                effect => effect.stat,
                effect => effect.status,
                status => status.apply,
                status => status.duration_seconds,
                status => status.slow_percent,
                status => status.tick_damage,
                status => status.tick_interval_seconds);
        }

        private void ApplyEffectsInternal<TEffect, TStatus>(
            GameObject target,
            TEffect[] effects,
            Func<TEffect, string> effectType,
            Func<TEffect, float> value,
            Func<TEffect, string> mode,
            Func<TEffect, string> stat,
            Func<TEffect, TStatus> status,
            Func<TStatus, string> statusApply,
            Func<TStatus, float> statusDurationSeconds,
            Func<TStatus, float> statusSlowPercent,
            Func<TStatus, float> statusTickDamage,
            Func<TStatus, float> statusTickIntervalSeconds)
            where TStatus : class
            where TEffect : class
        {
            if (target == null)
            {
                UnityEngine.Debug.LogWarning("ApplyEffects called with null target.");
                return;
            }

            if (effects == null || effects.Length == 0)
            {
                UnityEngine.Debug.LogWarning("ApplyEffects called with no effects.");
                return;
            }

            foreach (TEffect effect in effects)
            {
                if (effect == null)
                {
                    continue;
                }

                string effectTypeValue = effectType(effect);
                if (string.IsNullOrWhiteSpace(effectTypeValue))
                {
                    UnityEngine.Debug.LogWarning("Effect missing effect_type.");
                    continue;
                }

                float effectValue = value(effect);
                string effectMode = mode(effect);
                string effectStat = stat(effect);

                TStatus statusDto = status(effect);
                string apply = statusDto != null ? statusApply(statusDto) : null;
                float durationSeconds = statusDto != null ? statusDurationSeconds(statusDto) : 0f;
                float slowPercent = statusDto != null ? statusSlowPercent(statusDto) : 0f;
                float tickDamage = statusDto != null ? statusTickDamage(statusDto) : 0f;
                float tickIntervalSeconds = statusDto != null ? statusTickIntervalSeconds(statusDto) : 0f;

                switch (effectTypeValue)
                {
                    case "damage":
                        ApplyDamage(target, effectValue);
                        break;
                    case "heal":
                        ApplyHeal(target, effectValue);
                        break;
                    case "status":
                    case "status_on_hit":
                        ApplyStatus(target, apply, durationSeconds, slowPercent, tickDamage, tickIntervalSeconds, effectValue);
                        break;
                    case "buff":
                        UnityEngine.Debug.Log($"Buff effect received (mode={effectMode}, stat={effectStat}, value={effectValue}).");
                        break;
                    default:
                        UnityEngine.Debug.LogWarning($"Unknown effect_type '{effectTypeValue}'.");
                        break;
                }
            }
        }

        private void ApplyDamage(GameObject target, float amount)
        {
            if (amount <= 0f)
            {
                UnityEngine.Debug.LogWarning("Damage effect missing or non-positive value.");
                return;
            }

            HealthComponent health = target.GetComponent<HealthComponent>();
            if (health == null)
            {
                UnityEngine.Debug.LogWarning("Damage effect target has no HealthComponent.");
                return;
            }

            health.ApplyDamage(amount);
        }

        private void ApplyHeal(GameObject target, float amount)
        {
            if (amount <= 0f)
            {
                UnityEngine.Debug.LogWarning("Heal effect missing or non-positive value.");
                return;
            }

            HealthComponent health = target.GetComponent<HealthComponent>();
            if (health == null)
            {
                UnityEngine.Debug.LogWarning("Heal effect target has no HealthComponent.");
                return;
            }

            health.Heal(amount);
        }

        private void ApplyStatus(
            GameObject target,
            string apply,
            float durationSeconds,
            float slowPercent,
            float tickDamage,
            float tickIntervalSeconds,
            float value)
        {
            if (string.IsNullOrWhiteSpace(apply))
            {
                UnityEngine.Debug.LogWarning("Status effect missing status.apply.");
                return;
            }

            switch (apply)
            {
                case "slow":
                    statusSystem.ApplySlow(target, slowPercent, durationSeconds);
                    break;
                case "burn":
                    statusSystem.ApplyBurn(target, tickDamage, tickIntervalSeconds, durationSeconds);
                    break;
                case "armor_shred":
                    statusSystem.ApplyArmorShred(target, value, durationSeconds);
                    break;
                default:
                    UnityEngine.Debug.LogWarning($"Unknown status apply '{apply}'.");
                    break;
            }
        }
    }
}
