using System;
using UnityEngine;
using TowerOffense.Data;

namespace TowerOffense.Combat
{
    public class EffectResolver
    {
        private readonly StatusSystem statusSystem = new StatusSystem();

        public void ApplyEffects(GameObject source, GameObject target, SpellDefinition.EffectDto[] effects)
        {
            ApplyEffectsInternal(target, effects, effect => new EffectData(effect));
        }

        public void ApplyEffects(GameObject source, GameObject target, UnitDefinition.EffectDto[] effects)
        {
            ApplyEffectsInternal(target, effects, effect => new EffectData(effect));
        }

        public void ApplyEffects(GameObject source, GameObject target, TowerDefinition.EffectDto[] effects)
        {
            ApplyEffectsInternal(target, effects, effect => new EffectData(effect));
        }

        public void ApplyEffects(GameObject source, GameObject target, TrapDefinition.EffectDto[] effects)
        {
            ApplyEffectsInternal(target, effects, effect => new EffectData(effect));
        }

        public void ApplyEffects(GameObject source, GameObject target, HeroDefinition.EffectDto[] effects)
        {
            ApplyEffectsInternal(target, effects, effect => new EffectData(effect));
        }

        private void ApplyEffectsInternal<TEffect>(GameObject target, TEffect[] effects, Func<TEffect, EffectData> convert)
        {
            if (target == null)
            {
                Debug.LogWarning("ApplyEffects called with null target.");
                return;
            }

            if (effects == null || effects.Length == 0)
            {
                Debug.LogWarning("ApplyEffects called with no effects.");
                return;
            }

            foreach (TEffect effect in effects)
            {
                if (effect == null)
                {
                    continue;
                }

                EffectData data = convert(effect);
                if (string.IsNullOrWhiteSpace(data.effectType))
                {
                    Debug.LogWarning("Effect missing effect_type.");
                    continue;
                }

                switch (data.effectType)
                {
                    case "damage":
                        ApplyDamage(target, data.value);
                        break;
                    case "heal":
                        ApplyHeal(target, data.value);
                        break;
                    case "status":
                    case "status_on_hit":
                        ApplyStatus(target, data.status);
                        break;
                    case "buff":
                        Debug.Log($"Buff effect received (mode={data.mode}, stat={data.stat}, value={data.value}).");
                        break;
                    default:
                        Debug.LogWarning($"Unknown effect_type '{data.effectType}'.");
                        break;
                }
            }
        }

        private void ApplyDamage(GameObject target, float amount)
        {
            if (amount <= 0f)
            {
                Debug.LogWarning("Damage effect missing or non-positive value.");
                return;
            }

            HealthComponent health = target.GetComponent<HealthComponent>();
            if (health == null)
            {
                Debug.LogWarning("Damage effect target has no HealthComponent.");
                return;
            }

            health.ApplyDamage(amount);
        }

        private void ApplyHeal(GameObject target, float amount)
        {
            if (amount <= 0f)
            {
                Debug.LogWarning("Heal effect missing or non-positive value.");
                return;
            }

            HealthComponent health = target.GetComponent<HealthComponent>();
            if (health == null)
            {
                Debug.LogWarning("Heal effect target has no HealthComponent.");
                return;
            }

            health.Heal(amount);
        }

        private void ApplyStatus(GameObject target, StatusData status)
        {
            if (string.IsNullOrWhiteSpace(status.apply))
            {
                Debug.LogWarning("Status effect missing status.apply.");
                return;
            }

            switch (status.apply)
            {
                case "slow":
                    statusSystem.ApplySlow(target, status.slowPercent, status.durationSeconds);
                    break;
                case "burn":
                    statusSystem.ApplyBurn(target, status.tickDamage, status.tickIntervalSeconds, status.durationSeconds);
                    break;
                default:
                    Debug.LogWarning($"Unknown status apply '{status.apply}'.");
                    break;
            }
        }

        private readonly struct EffectData
        {
            public readonly string effectType;
            public readonly string mode;
            public readonly string stat;
            public readonly float value;
            public readonly StatusData status;

            public EffectData(SpellDefinition.EffectDto dto)
            {
                effectType = dto.effect_type;
                mode = dto.mode;
                stat = dto.stat;
                value = dto.value;
                status = new StatusData(dto.status);
            }

            public EffectData(UnitDefinition.EffectDto dto)
            {
                effectType = dto.effect_type;
                mode = dto.mode;
                stat = dto.stat;
                value = dto.value;
                status = new StatusData(dto.status);
            }

            public EffectData(TowerDefinition.EffectDto dto)
            {
                effectType = dto.effect_type;
                mode = dto.mode;
                stat = dto.stat;
                value = dto.value;
                status = new StatusData(dto.status);
            }

            public EffectData(TrapDefinition.EffectDto dto)
            {
                effectType = dto.effect_type;
                mode = dto.mode;
                stat = dto.stat;
                value = dto.value;
                status = new StatusData(dto.status);
            }

            public EffectData(HeroDefinition.EffectDto dto)
            {
                effectType = dto.effect_type;
                mode = dto.mode;
                stat = dto.stat;
                value = dto.value;
                status = new StatusData(dto.status);
            }
        }

        private readonly struct StatusData
        {
            public readonly string apply;
            public readonly float durationSeconds;
            public readonly float slowPercent;
            public readonly float tickDamage;
            public readonly float tickIntervalSeconds;

            public StatusData(SpellDefinition.StatusDto dto)
            {
                apply = dto?.apply;
                durationSeconds = dto?.duration_seconds ?? 0f;
                slowPercent = dto?.slow_percent ?? 0f;
                tickDamage = dto?.tick_damage ?? 0f;
                tickIntervalSeconds = dto?.tick_interval_seconds ?? 0f;
            }

            public StatusData(UnitDefinition.StatusDto dto)
            {
                apply = dto?.apply;
                durationSeconds = dto?.duration_seconds ?? 0f;
                slowPercent = dto?.slow_percent ?? 0f;
                tickDamage = dto?.tick_damage ?? 0f;
                tickIntervalSeconds = dto?.tick_interval_seconds ?? 0f;
            }

            public StatusData(TowerDefinition.StatusDto dto)
            {
                apply = dto?.apply;
                durationSeconds = dto?.duration_seconds ?? 0f;
                slowPercent = dto?.slow_percent ?? 0f;
                tickDamage = dto?.tick_damage ?? 0f;
                tickIntervalSeconds = dto?.tick_interval_seconds ?? 0f;
            }

            public StatusData(TrapDefinition.StatusDto dto)
            {
                apply = dto?.apply;
                durationSeconds = dto?.duration_seconds ?? 0f;
                slowPercent = dto?.slow_percent ?? 0f;
                tickDamage = dto?.tick_damage ?? 0f;
                tickIntervalSeconds = dto?.tick_interval_seconds ?? 0f;
            }

            public StatusData(HeroDefinition.StatusDto dto)
            {
                apply = dto?.apply;
                durationSeconds = dto?.duration_seconds ?? 0f;
                slowPercent = dto?.slow_percent ?? 0f;
                tickDamage = dto?.tick_damage ?? 0f;
                tickIntervalSeconds = dto?.tick_interval_seconds ?? 0f;
            }
        }
    }
}
