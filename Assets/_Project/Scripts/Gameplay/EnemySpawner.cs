using System.Collections;
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
    public LevelController levelController;

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
            Debug.LogError("EnemySpawner: pathWaypointsRoot missing!");
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
            Debug.LogError("EnemySpawner: Missing references or waypoints.");
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
        GameObject enemyGO = Instantiate(enemyPrefab, spawnPoint.position, Quaternion.identity);

        EnemyMover mover = enemyGO.GetComponent<EnemyMover>();
        if (mover == null)
            mover = enemyGO.AddComponent<EnemyMover>();

        mover.Init(cachedWaypoints);
    }
}
