using System.Collections;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("References")]
    public Transform spawnPoint;
    public Transform pathWaypointsRoot;   // Parent: PathWaypoints
    public GameObject enemyPrefab;

    [Header("Wave Settings")]
    public int enemiesPerWave = 10;
    public float spawnInterval = 0.8f;

    private Transform[] cachedWaypoints;

    private void Awake()
    {
        CacheWaypoints();
    }

    private void CacheWaypoints()
    {
        if (pathWaypointsRoot == null)
        {
            Debug.LogError("EnemySpawner: pathWaypointsRoot missing!");
            return;
        }

        int count = pathWaypointsRoot.childCount;
        cachedWaypoints = new Transform[count];

        for (int i = 0; i < count; i++)
            cachedWaypoints[i] = pathWaypointsRoot.GetChild(i);
    }

    // Zum Test: starte Wave automatisch
    private void Start()
    {
        StartWave();
    }

    public void StartWave()
    {
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
