using System.Collections;
using TowerConquest.Debug;
using UnityEngine;
using TowerConquest.Gameplay;

public class EnemySpawner : MonoBehaviour
{
    [Header("References")]
    public Transform spawnPoint;
    public Transform pathWaypointsRoot;   // Parent: PathWaypoints
    public GameObject enemyPrefab;

    [Header("Wave Settings")]
    public int enemiesPerWave = 10;
    public float spawnInterval = 0.8f;

    [Header("Level Integration")]
    public LiveBattleLevelController levelController;

    private Transform[] cachedWaypoints;
    private bool waypointsCached = false;

    private void Awake()
    {
        // Don't cache in Awake - fields might not be set yet when added via AddComponent
        // CacheWaypoints will be called later when needed
    }

    private void CacheWaypoints()
    {
        if (waypointsCached)
            return;

        if (pathWaypointsRoot == null)
        {
            Log.Error("EnemySpawner: pathWaypointsRoot missing!");
            return;
        }

        int count = pathWaypointsRoot.childCount;
        cachedWaypoints = new Transform[count];

        for (int i = 0; i < count; i++)
            cachedWaypoints[i] = pathWaypointsRoot.GetChild(i);

        waypointsCached = true;
    }

    [Header("Auto Start")]
    [Tooltip("Wenn aktiviert, startet die Wave automatisch beim Spielstart (nur für Testzwecke)")]
    public bool autoStartWave = false;

    private void Start()
    {
        // Cache waypoints in Start (after fields are set)
        CacheWaypoints();

        // Nur für Testzwecke - normalerweise wird StartWave() vom LevelController aufgerufen
        if (autoStartWave)
        {
            StartWave();
        }
    }

    public void StartWave()
    {
        // Ensure waypoints are cached
        CacheWaypoints();

        if (enemyPrefab == null || spawnPoint == null || cachedWaypoints == null || cachedWaypoints.Length == 0)
        {
            Log.Error("EnemySpawner: Missing references or waypoints.");
            return;
        }

        StopAllCoroutines();
        StartCoroutine(SpawnWaveRoutine());
    }

    private IEnumerator SpawnWaveRoutine()
    {
        for (int i = 0; i < enemiesPerWave; i++)
        {
            SpawnOne();
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    private void SpawnOne()
    {
        Vector3 spawnPosition = spawnPoint.position;

        // Validate and clamp position to map boundaries
        if (MapBoundary.Instance != null && !MapBoundary.Instance.IsWithinBounds(spawnPosition))
        {
            spawnPosition = MapBoundary.Instance.ClampToBounds(spawnPosition);
            UnityEngine.Debug.LogWarning($"[EnemySpawner] Spawn position was outside map bounds, clamped to: {spawnPosition}");
        }

        GameObject enemyGO = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);

        // Setup EnemyMover for basic movement
        EnemyMover mover = enemyGO.GetComponent<EnemyMover>();
        if (mover == null)
            mover = enemyGO.AddComponent<EnemyMover>();

        mover.Init(cachedWaypoints);

        // Add combat capability
        SetupEnemyCombat(enemyGO);

        // Set enemy layer for targeting
        SetLayerRecursively(enemyGO, LayerMask.NameToLayer("Enemy"));
    }

    private void SetupEnemyCombat(GameObject enemyGO)
    {
        // Add combat component if not exists
        var combat = enemyGO.GetComponent<TowerConquest.Gameplay.Entities.UnitCombat>();
        if (combat == null)
        {
            combat = enemyGO.AddComponent<TowerConquest.Gameplay.Entities.UnitCombat>();
        }

        // Add health component if not exists
        var health = enemyGO.GetComponent<TowerConquest.Combat.HealthComponent>();
        if (health == null)
        {
            health = enemyGO.AddComponent<TowerConquest.Combat.HealthComponent>();
            health.Initialize(100f, 0f); // Default HP and armor
        }

        // Find enemy base (player base is the target for enemies)
        var playerBase = FindFirstObjectByType<TowerConquest.Gameplay.Entities.BaseController>();

        // Initialize combat with AI team settings (enemies are AI controlled)
        combat.Initialize(GoldManager.Team.AI, 25f, 2f, 1f, playerBase);

        Log.Info($"[EnemySpawner] Setup combat for spawned enemy");
    }

    private void SetLayerRecursively(GameObject obj, int layer)
    {
        if (layer < 0) return; // Invalid layer

        obj.layer = layer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }
}
