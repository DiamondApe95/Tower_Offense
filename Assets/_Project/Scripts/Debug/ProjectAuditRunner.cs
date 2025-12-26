using System.Collections;
using System.Collections.Generic;
using System.IO;
using TowerConquest.Combat;
using TowerConquest.Core;
using TowerConquest.Data;
using TowerConquest.Gameplay;
using TowerConquest.Gameplay.Entities;
using TowerConquest.Saving;
using UnityEngine;

namespace TowerConquest.Debug
{
    public class ProjectAuditRunner : MonoBehaviour
    {
        [SerializeField] private bool runOnStart = true;

        private int total;
        private int passed;
        private int failed;

        private readonly List<GameObject> spawnedObjects = new List<GameObject>();

        private void Start()
        {
            if (runOnStart)
            {
                StartCoroutine(RunAudit());
            }
        }

        private IEnumerator RunAudit()
        {
            ResetCounters();

            yield return null;

            GameBootstrapper bootstrapper = FindAnyObjectByType<GameBootstrapper>();
            bool bootstrapperPresent = bootstrapper != null;
            bool gameTimeRegistered = ServiceLocator.TryGet(out GameTime _);
            LogResult("ServiceLocator/GameBootstrapper available", bootstrapperPresent || gameTimeRegistered,
                bootstrapperPresent ? "GameBootstrapper found." : "GameBootstrapper missing; using ServiceLocator check.");

            JsonDatabase database = GetOrCreateDatabase(out bool databaseRegistered);
            LogResult("JsonDatabase registered", databaseRegistered, databaseRegistered ? null : "JsonDatabase was not registered; fallback created.");

            bool jsonLoaded = database != null && database.Units.Count > 0 && database.Towers.Count > 0 && database.Levels.Count > 0;
            LogResult("JsonDatabase loaded with counts > 0", jsonLoaded,
                database == null ? "JsonDatabase missing." : $"Counts Units={database?.Units.Count}, Towers={database?.Towers.Count}, Levels={database?.Levels.Count}.");

            LogResult("StreamingAssets JSON files exist",
                File.Exists(Path.Combine(Application.streamingAssetsPath, "Data/JSON/units.json"))
                && File.Exists(Path.Combine(Application.streamingAssetsPath, "Data/JSON/towers.json"))
                && File.Exists(Path.Combine(Application.streamingAssetsPath, "Data/JSON/levels.json")),
                "Missing expected JSON files in StreamingAssets/Data/JSON.");

            LevelDefinition levelDefinition = database?.Levels != null && database.Levels.Count > 0 ? database.Levels[0] : null;
            LogResult("First level loaded from JsonDatabase", levelDefinition != null,
                levelDefinition == null ? "No level definitions loaded." : $"Level={levelDefinition.id}");

            LevelController levelController = FindAnyObjectByType<LevelController>();
            if (levelController == null)
            {
                GameObject levelObject = new GameObject("Audit_LevelController");
                spawnedObjects.Add(levelObject);
                levelController = levelObject.AddComponent<LevelController>();
                levelController.levelId = levelDefinition?.id ?? levelController.levelId;
            }

            yield return null;

            LogResult("LevelController initialized", levelController != null && levelController.Run != null,
                levelController == null ? "LevelController missing." : "RunState not initialized.");

            int startingWave = levelController?.Run?.waveIndex ?? -1;
            levelController?.StartWave();
            yield return new WaitForSeconds(2.2f);

            bool waveCompleted = levelController != null && levelController.Run != null
                && levelController.Run.waveIndex == startingWave + 1
                && levelController.Run.isPlanning;
            LogResult("Planning -> Wave -> Planning loop", waveCompleted,
                levelController == null ? "LevelController missing." : $"WaveIndex={levelController.Run?.waveIndex}, Planning={levelController.Run?.isPlanning}");

            yield return PlayUnitCardAudit(levelController);

            yield return UnitMovementAudit();

            yield return TowerDamageAudit();

            yield return SpeedToggleAudit(levelController);

            yield return SaveProgressAudit(levelController);

            yield return HeroSpawnAudit(levelController);

            yield return StatusEffectAudit();

            yield return SpellAoeAudit(database);

            CleanupSpawnedObjects();

            UnityEngine.Debug.Log($"[AUDIT SUMMARY] Total={total}, Passed={passed}, Failed={failed}");
        }

        private void ResetCounters()
        {
            total = 0;
            passed = 0;
            failed = 0;
        }

        private void LogResult(string testName, bool success, string details = null)
        {
            total++;
            if (success)
            {
                passed++;
                UnityEngine.Debug.Log($"[AUDIT PASS] {testName}");
            }
            else
            {
                failed++;
                UnityEngine.Debug.LogWarning(details == null
                    ? $"[AUDIT FAIL] {testName}"
                    : $"[AUDIT FAIL] {testName} :: {details}");
            }
        }

        private JsonDatabase GetOrCreateDatabase(out bool registered)
        {
            if (ServiceLocator.TryGet(out JsonDatabase database))
            {
                registered = true;
                return database;
            }

            registered = false;
            database = new JsonDatabase();
            database.LoadAll();
            ServiceLocator.Register(database);
            return database;
        }

        private IEnumerator PlayUnitCardAudit(LevelController levelController)
        {
            if (levelController == null || levelController.hand == null)
            {
                LogResult("Play unit card from hand", false, "Hand not initialized.");
                yield break;
            }

            string unitCardId = levelController.hand.hand.Find(cardId => cardId.StartsWith("unit_"));
            if (string.IsNullOrWhiteSpace(unitCardId))
            {
                LogResult("Play unit card from hand", false, "No unit card found in hand.");
                yield break;
            }

            int unitCountBefore = FindObjectsByType<UnitController>(FindObjectsSortMode.None).Length;
            int handCountBefore = levelController.hand.hand.Count;

            levelController.PlayCard(unitCardId);
            yield return new WaitForSeconds(0.1f);

            int unitCountAfter = FindObjectsByType<UnitController>(FindObjectsSortMode.None).Length;
            bool cardPlayed = unitCountAfter > unitCountBefore;
            bool handRefilled = levelController.hand.hand.Count == handCountBefore;

            LogResult("Play unit card spawns unit", cardPlayed, $"Units before={unitCountBefore}, after={unitCountAfter}");
            LogResult("Hand refilled after play", handRefilled,
                $"Hand size before={handCountBefore}, after={levelController.hand.hand.Count}");
        }

        private IEnumerator UnitMovementAudit()
        {
            UnitController unit = FindAnyObjectByType<UnitController>();
            if (unit == null)
            {
                LogResult("Unit moves along path", false, "No unit found for movement check.");
                yield break;
            }

            Vector3 startPosition = unit.transform.position;
            yield return new WaitForSeconds(3f);

            bool movedOrDestroyed = unit == null || Vector3.Distance(startPosition, unit.transform.position) > 0.5f;
            LogResult("Unit moves along path", movedOrDestroyed,
                unit == null ? "Unit destroyed before check." : $"Distance={Vector3.Distance(startPosition, unit.transform.position):0.00}");
        }

        private IEnumerator TowerDamageAudit()
        {
            TowerController tower = FindAnyObjectByType<TowerController>();
            LogResult("Tower exists in scene", tower != null, "No TowerController found.");
            if (tower == null)
            {
                yield break;
            }

            UnitController unit = FindAnyObjectByType<UnitController>();
            if (unit == null)
            {
                Vector3 spawnPosition = tower.transform.position + new Vector3(0.5f, 0f, 0.5f);
                unit = SpawnAuditUnit(spawnPosition);
            }

            if (unit == null)
            {
                LogResult("Unit receives tower damage", false, "No unit found for damage check.");
                yield break;
            }

            HealthComponent health = unit.GetComponent<HealthComponent>();
            if (health == null)
            {
                LogResult("Unit receives tower damage", false, "Unit missing HealthComponent.");
                yield break;
            }

            float hpBefore = health.currentHp;
            yield return new WaitForSeconds(4f);

            bool damaged = health == null || health.currentHp < hpBefore;
            LogResult("Unit receives tower damage", damaged,
                health == null ? "Unit destroyed (damage assumed)." : $"HP before={hpBefore}, after={health.currentHp}");
        }

        private IEnumerator SpeedToggleAudit(LevelController levelController)
        {
            if (levelController == null)
            {
                LogResult("Speed toggle updates Time.timeScale", false, "LevelController missing.");
                yield break;
            }

            float before = Time.timeScale;
            levelController.ToggleSpeed();
            yield return null;
            float after = Time.timeScale;

            bool changed = !Mathf.Approximately(before, after);
            LogResult("Speed toggle updates Time.timeScale", changed, $"TimeScale before={before}, after={after}");

            levelController.ToggleSpeed();
        }

        private IEnumerator SaveProgressAudit(LevelController levelController)
        {
            SaveManager saveManager = GetOrCreateSaveManager(out bool registered);
            LogResult("SaveManager registered", registered, registered ? null : "SaveManager not registered; fallback created.");

            if (levelController == null || levelController.Fsm == null || levelController.Run == null)
            {
                LogResult("Progress saved on victory", false, "LevelController not initialized for save check.");
                yield break;
            }

            levelController.Fsm.Finish(levelController.Run, true);
            yield return null;

            bool progressSaved = File.Exists(saveManager.GetProgressPath());
            LogResult("Progress saved on victory", progressSaved, "Progress file not found.");
        }

        private IEnumerator HeroSpawnAudit(LevelController levelController)
        {
            if (levelController == null || levelController.Run == null)
            {
                LogResult("Hero spawns on wave 5", false, "LevelController missing.");
                yield break;
            }

            levelController.Run.waveIndex = 4;
            levelController.StartWave();
            yield return null;

            HeroController hero = FindAnyObjectByType<HeroController>();
            LogResult("Hero spawns on wave 5", hero != null, "HeroController not found after wave 5 start.");
        }

        private IEnumerator StatusEffectAudit()
        {
            UnitController unit = FindAnyObjectByType<UnitController>();
            if (unit == null)
            {
                unit = SpawnAuditUnit(new Vector3(3f, 0f, 3f));
            }

            if (unit == null)
            {
                LogResult("Status effects apply", false, "No unit for status check.");
                yield break;
            }

            StatusSystem statusSystem = new StatusSystem();

            UnitMover mover = unit.GetComponent<UnitMover>();
            float originalMultiplier = mover != null ? mover.moveSpeedMultiplier : 1f;
            statusSystem.ApplySlow(unit.gameObject, 0.5f, 1f);
            yield return null;

            bool slowApplied = mover != null && mover.moveSpeedMultiplier < originalMultiplier;
            LogResult("Slow status affects movement", slowApplied,
                mover == null ? "UnitMover missing." : $"Multiplier before={originalMultiplier}, after={mover.moveSpeedMultiplier}");

            HealthComponent health = unit.GetComponent<HealthComponent>();
            float hpBefore = health != null ? health.currentHp : 0f;
            statusSystem.ApplyBurn(unit.gameObject, 5f, 0.5f, 1.2f);
            yield return new WaitForSeconds(0.6f);

            bool burnApplied = health != null && health.currentHp < hpBefore;
            LogResult("Burn status ticks damage", burnApplied,
                health == null ? "HealthComponent missing." : $"HP before={hpBefore}, after={health.currentHp}");
        }

        private IEnumerator SpellAoeAudit(JsonDatabase database)
        {
            if (database == null)
            {
                LogResult("Spell AoE damages units", false, "JsonDatabase missing.");
                yield break;
            }

            SpellDefinition spell = database.FindSpell("spell_fire_pot");
            if (spell == null || spell.effects == null || spell.effects.Length == 0)
            {
                LogResult("Spell AoE damages units", false, "spell_fire_pot not found or has no effects.");
                yield break;
            }

            UnitController unitA = SpawnAuditUnit(new Vector3(2f, 0f, 2f));
            UnitController unitB = SpawnAuditUnit(new Vector3(2.5f, 0f, 2.5f));
            if (unitA == null || unitB == null)
            {
                LogResult("Spell AoE damages units", false, "Failed to spawn units for AoE test.");
                yield break;
            }

            float hpA = unitA.GetComponent<HealthComponent>()?.currentHp ?? 0f;
            float hpB = unitB.GetComponent<HealthComponent>()?.currentHp ?? 0f;

            GameObject aoeObject = new GameObject("Audit_AreaEffect");
            spawnedObjects.Add(aoeObject);
            aoeObject.transform.position = unitA.transform.position;
            AreaEffect areaEffect = aoeObject.AddComponent<AreaEffect>();
            areaEffect.effects = spell.effects;
            areaEffect.radius = 3f;
            areaEffect.duration = 0.1f;

            yield return new WaitForSeconds(0.1f);

            bool damaged = (unitA != null && unitA.GetComponent<HealthComponent>()?.currentHp < hpA)
                || (unitB != null && unitB.GetComponent<HealthComponent>()?.currentHp < hpB);
            LogResult("Spell AoE damages units", damaged,
                $"HP A before={hpA}, after={unitA?.GetComponent<HealthComponent>()?.currentHp}; HP B before={hpB}, after={unitB?.GetComponent<HealthComponent>()?.currentHp}");
        }

        private UnitController SpawnAuditUnit(Vector3 position)
        {
            GameObject unitObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            unitObject.name = "Audit_Unit";
            unitObject.transform.position = position;
            spawnedObjects.Add(unitObject);

            UnitController unit = unitObject.AddComponent<UnitController>();
            BaseController baseController = GetOrCreateBaseController();
            unit.Initialize("audit_unit", new List<Vector3> { position + new Vector3(1f, 0f, 1f) }, baseController);
            return unit;
        }

        private TowerConquest.Gameplay.Entities.BaseController GetOrCreateBaseController()
        {
            TowerConquest.Gameplay.Entities.BaseController baseController = FindAnyObjectByType<TowerConquest.Gameplay.Entities.BaseController>();
            if (baseController != null)
            {
                return baseController;
            }

            GameObject baseObject = new GameObject("Audit_BaseController");
            spawnedObjects.Add(baseObject);
            baseController = baseObject.AddComponent<TowerConquest.Gameplay.Entities.BaseController>();
            baseController.Initialize(1000f, 0f);
            return baseController;
        }

        private SaveManager GetOrCreateSaveManager(out bool registered)
        {
            if (ServiceLocator.TryGet(out SaveManager manager))
            {
                registered = true;
                return manager;
            }

            registered = false;
            manager = new SaveManager();
            ServiceLocator.Register(manager);
            return manager;
        }

        private void CleanupSpawnedObjects()
        {
            foreach (GameObject obj in spawnedObjects)
            {
                if (obj != null)
                {
                    Destroy(obj);
                }
            }

            spawnedObjects.Clear();
        }
    }
}
