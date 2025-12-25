using System.Collections.Generic;
using TowerConquest.Gameplay;
using UnityEngine;

/// <summary>
/// Automatischer Turm-Bau-Controller für den Defense-Modus.
/// Baut automatisch Türme, wenn genug Gold vorhanden ist.
/// </summary>
public class DefenseAutoBuildController : MonoBehaviour
{
    [Header("Auto-Build Settings")]
    [Tooltip("Automatisch Türme bauen im Defense-Modus")]
    public bool autoBuildEnabled = true;

    [Tooltip("Intervall zwischen Auto-Build-Versuchen (Sekunden)")]
    public float buildInterval = 2f;

    [Tooltip("Mindest-Gold-Reserve nach dem Bauen")]
    public int minGoldReserve = 20;

    [Tooltip("Priorität: 0 = Zufällig, 1 = Nah an Pfad, 2 = Ausgewogen")]
    public int placementStrategy = 1;

    [Header("References")]
    public BuildManager buildManager;
    public LevelController levelController;

    private float buildTimer;
    private readonly List<Transform> availableBuildTiles = new List<Transform>();
    private bool isDefenseMode;

    private void Start()
    {
        // Referenzen automatisch finden
        if (buildManager == null)
        {
            buildManager = FindFirstObjectByType<BuildManager>();
        }

        if (levelController == null)
        {
            levelController = FindFirstObjectByType<LevelController>();
        }

        // Prüfe ob Defense-Modus aktiv ist
        if (levelController != null && levelController.Run != null)
        {
            isDefenseMode = levelController.Run.gameMode == GameMode.Defense;
        }
        else
        {
            // Falls kein LevelController, prüfe global rules
            isDefenseMode = false;
        }

        if (!isDefenseMode)
        {
            autoBuildEnabled = false;
            Debug.Log("DefenseAutoBuildController: Not in Defense mode, auto-build disabled.");
        }
        else
        {
            Debug.Log("DefenseAutoBuildController: Defense mode active, auto-build enabled.");
        }

        // Alle BuildTiles sammeln
        CollectBuildTiles();
    }

    private void Update()
    {
        if (!autoBuildEnabled || buildManager == null)
        {
            return;
        }

        buildTimer += Time.deltaTime;
        if (buildTimer >= buildInterval)
        {
            buildTimer = 0f;
            TryAutoBuild();
        }
    }

    private void CollectBuildTiles()
    {
        availableBuildTiles.Clear();

        int buildLayer = LayerMask.NameToLayer("BuildTile");
        if (buildLayer < 0)
        {
            Debug.LogWarning("DefenseAutoBuildController: BuildTile layer not found.");
            return;
        }

        // Alle GameObjects mit BuildTile layer finden
        GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        foreach (var obj in allObjects)
        {
            if (obj.layer == buildLayer)
            {
                availableBuildTiles.Add(obj.transform);
            }
        }

        Debug.Log($"DefenseAutoBuildController: Found {availableBuildTiles.Count} build tiles.");
    }

    private void TryAutoBuild()
    {
        if (buildManager == null)
        {
            return;
        }

        GameObject towerPrefab = buildManager.GetSelectedTowerPrefab();
        if (towerPrefab == null)
        {
            Debug.LogWarning("DefenseAutoBuildController: No tower prefab available.");
            return;
        }

        int cost = buildManager.GetTowerCost(towerPrefab);

        // Prüfe ob genug Gold vorhanden (mit Reserve)
        if (buildManager.CurrentGold < cost + minGoldReserve)
        {
            return;
        }

        // Finde das beste Build-Tile
        Transform bestTile = FindBestBuildTile();
        if (bestTile == null)
        {
            return;
        }

        // Baue den Turm
        if (buildManager.CanBuildAt(bestTile))
        {
            buildManager.TryBuildAtTile(bestTile);
            Debug.Log($"DefenseAutoBuildController: Auto-built tower at {bestTile.name}");
        }
    }

    private Transform FindBestBuildTile()
    {
        List<Transform> validTiles = new List<Transform>();

        foreach (var tile in availableBuildTiles)
        {
            if (tile == null)
            {
                continue;
            }

            if (!buildManager.IsTileOccupied(tile) && buildManager.CanBuildAt(tile))
            {
                validTiles.Add(tile);
            }
        }

        if (validTiles.Count == 0)
        {
            return null;
        }

        switch (placementStrategy)
        {
            case 0: // Zufällig
                return validTiles[Random.Range(0, validTiles.Count)];

            case 1: // Nah an Pfad
                return FindTileNearPath(validTiles);

            case 2: // Ausgewogen (verteilt über das Spielfeld)
                return FindBalancedTile(validTiles);

            default:
                return validTiles[0];
        }
    }

    private Transform FindTileNearPath(List<Transform> validTiles)
    {
        // Finde Path-Tiles
        int pathLayer = LayerMask.NameToLayer("Path");
        if (pathLayer < 0)
        {
            return validTiles[Random.Range(0, validTiles.Count)];
        }

        Transform bestTile = null;
        float bestDistance = float.MaxValue;

        GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        List<Vector3> pathPositions = new List<Vector3>();

        foreach (var obj in allObjects)
        {
            if (obj.layer == pathLayer)
            {
                pathPositions.Add(obj.transform.position);
            }
        }

        if (pathPositions.Count == 0)
        {
            return validTiles[Random.Range(0, validTiles.Count)];
        }

        foreach (var tile in validTiles)
        {
            float minDist = float.MaxValue;
            foreach (var pathPos in pathPositions)
            {
                float dist = Vector3.Distance(tile.position, pathPos);
                if (dist < minDist)
                {
                    minDist = dist;
                }
            }

            if (minDist < bestDistance)
            {
                bestDistance = minDist;
                bestTile = tile;
            }
        }

        return bestTile;
    }

    private Transform FindBalancedTile(List<Transform> validTiles)
    {
        if (buildManager.OccupiedTileCount == 0)
        {
            // Erstes Tile: wähle eins in der Mitte
            Vector3 center = Vector3.zero;
            foreach (var tile in validTiles)
            {
                center += tile.position;
            }
            center /= validTiles.Count;

            Transform closestToCenter = null;
            float bestDist = float.MaxValue;
            foreach (var tile in validTiles)
            {
                float dist = Vector3.Distance(tile.position, center);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    closestToCenter = tile;
                }
            }
            return closestToCenter;
        }

        // Wähle ein Tile, das am weitesten von existierenden Türmen entfernt ist
        Transform bestTile = null;
        float bestMinDistance = 0f;

        foreach (var tile in validTiles)
        {
            float minDistToTower = float.MaxValue;

            // Distanz zu allen existierenden Türmen berechnen
            var towers = FindObjectsByType<TowerConquest.Gameplay.Entities.TowerController>(FindObjectsSortMode.None);
            foreach (var tower in towers)
            {
                if (tower == null) continue;
                float dist = Vector3.Distance(tile.position, tower.transform.position);
                if (dist < minDistToTower)
                {
                    minDistToTower = dist;
                }
            }

            if (minDistToTower > bestMinDistance)
            {
                bestMinDistance = minDistToTower;
                bestTile = tile;
            }
        }

        return bestTile ?? validTiles[Random.Range(0, validTiles.Count)];
    }

    /// <summary>
    /// Aktiviert oder deaktiviert Auto-Build zur Laufzeit.
    /// </summary>
    public void SetAutoBuildEnabled(bool enabled)
    {
        autoBuildEnabled = enabled;
    }

    /// <summary>
    /// Setzt das Build-Intervall zur Laufzeit.
    /// </summary>
    public void SetBuildInterval(float interval)
    {
        buildInterval = Mathf.Max(0.5f, interval);
    }
}
