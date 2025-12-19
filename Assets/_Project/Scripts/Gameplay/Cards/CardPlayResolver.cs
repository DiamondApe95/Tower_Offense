using TowerConquest.Combat;
using TowerConquest.Core;
using TowerConquest.Data;
using UnityEngine;

namespace TowerConquest.Gameplay.Cards
{
    public class CardPlayResolver
    {
        private LevelController levelController;

        public void Initialize(LevelController controller)
        {
            levelController = controller;
        }

        public void Play(string cardId)
        {
            if (string.IsNullOrEmpty(cardId))
            {
                UnityEngine.Debug.LogWarning("Play request received with empty card id.");
                return;
            }

            if (cardId.StartsWith("unit_"))
            {

                ResolveUnitCard(cardId);

            }
            else if (cardId.StartsWith("spell_"))
            {
                CastSpell(cardId);
            }
            else
            {
                UnityEngine.Debug.LogWarning($"Unknown card type: {cardId}");
            }
        }

        private void ResolveUnitCard(string unitId)
        {
            if (levelController == null)
            {
                UnityEngine.Debug.LogWarning($"Unit card '{unitId}' played without LevelController.");
                return;
            }

            if (levelController.Run != null && levelController.Run.isPlanning)
            {
                levelController.QueueUnitCard(unitId);
                UnityEngine.Debug.Log($"Queued unit card '{unitId}' for next wave.");
                return;
            }

            if (levelController.Run != null && levelController.Run.allowMidWaveSpawns)
            {
                levelController.Spawner?.SpawnUnitGroup(unitId);
                UnityEngine.Debug.Log($"Spawned unit card '{unitId}' mid-wave.");
                return;
            }

            UnityEngine.Debug.Log($"Unit card '{unitId}' ignored (not allowed mid-wave).");
        }

        private void CastSpell(string spellId)
        {
            JsonDatabase database = ServiceLocator.Get<JsonDatabase>();
            SpellDefinition spell = database.FindSpell(spellId);
            if (spell == null)
            {
                UnityEngine.Debug.LogWarning($"Spell '{spellId}' not found.");
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

        private void SpawnUnit(string unitId)
        {
            LevelController levelController = Object.FindObjectOfType<LevelController>();
            if (levelController == null || levelController.Spawner == null)
            {
                UnityEngine.Debug.LogWarning("No LevelController/Spawner available to spawn unit.");
                return;
            }

            levelController.Spawner.SpawnUnit(unitId);
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
