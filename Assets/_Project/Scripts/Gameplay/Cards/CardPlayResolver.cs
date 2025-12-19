using TowerOffense.Combat;
using TowerOffense.Core;
using TowerOffense.Data;
using TowerOffense.Gameplay;
using UnityEngine;

namespace TowerOffense.Gameplay.Cards
{
    public class CardPlayResolver
    {
        public void Play(string cardId)
        {
            if (string.IsNullOrEmpty(cardId))
            {
                Debug.LogWarning("Play request received with empty card id.");
                return;
            }

            if (cardId.StartsWith("unit_"))
            {
                Debug.Log($"Spawn unit request: {cardId}");
            }
            else if (cardId.StartsWith("spell_"))
            {
                CastSpell(cardId);
            }
            else
            {
                Debug.LogWarning($"Unknown card type: {cardId}");
            }
        }

        private void CastSpell(string spellId)
        {
            JsonDatabase database = ServiceLocator.Get<JsonDatabase>();
            SpellDefinition spell = database.FindSpell(spellId);
            if (spell == null)
            {
                Debug.LogWarning($"Spell '{spellId}' not found.");
                return;
            }

            Vector3 targetPosition = ResolveTargetPosition();
            GameObject areaObject = new GameObject($"AreaEffect_{spellId}");
            areaObject.transform.position = targetPosition;

            AreaEffect areaEffect = areaObject.AddComponent<AreaEffect>();
            areaEffect.effects = spell.effects;
            areaEffect.radius = ResolveRadius(spell.effects);
            areaEffect.duration = 0.2f;
        }

        private Vector3 ResolveTargetPosition()
        {
            if (Input.GetMouseButtonDown(0))
            {
                Camera camera = Camera.main;
                if (camera != null)
                {
                    Ray ray = camera.ScreenPointToRay(Input.mousePosition);
                    Plane plane = new Plane(Vector3.up, Vector3.zero);
                    if (plane.Raycast(ray, out float enter))
                    {
                        return ray.GetPoint(enter);
                    }
                }
            }

            LevelController levelController = Object.FindObjectOfType<LevelController>();
            if (levelController != null)
            {
                JsonDatabase database = ServiceLocator.Get<JsonDatabase>();
                LevelDefinition levelDefinition = database.FindLevel(levelController.levelId);
                if (levelController.PathManager != null)
                {
                    Vector3 spawn = levelController.PathManager.GetSpawnPosition(levelDefinition);
                    return spawn + new Vector3(1f, 0f, 1f);
                }
            }

            return new Vector3(1f, 0f, 1f);
        }

        private float ResolveRadius(SpellDefinition.EffectDto[] effects)
        {
            if (effects == null || effects.Length == 0)
            {
                return 2f;
            }

            float maxRadius = 0f;
            foreach (SpellDefinition.EffectDto effect in effects)
            {
                if (effect?.area != null && effect.area.radius > maxRadius)
                {
                    maxRadius = effect.area.radius;
                }
            }

            return maxRadius > 0f ? maxRadius : 2f;
        }
    }
}
