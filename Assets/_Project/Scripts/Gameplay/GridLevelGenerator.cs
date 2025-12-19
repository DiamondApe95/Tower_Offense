using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

public class AutoLevelGenerator : MonoBehaviour
{
    public enum Difficulty { Easy, Normal, Hard, Insane }
    public enum Complexity { Simple, Medium, Complex, Extreme }

    [Header("Generate")]
    public bool generateOnPlay = false;

    [ContextMenu("Generate Now")]
    public void GenerateNow() => Generate();

    [Header("Prefabs (Auto-load in Editor from Assets/Prefab)")]
    [Tooltip("Editor-only auto load folder. In builds please assign prefabs manually.")]
    public string prefabFolder = "Assets/Prefab";

    [Tooltip("Optional override. If null, auto-load Enemy.prefab in editor.")]
    public GameObject enemyPrefab;

    [Tooltip("Optional override. If null, auto-load TowerPrefab.prefab in editor.")]
    public GameObject towerPrefab;

    [Tooltip("Optional override. If null, auto-load Projectile.prefab in editor.")]
    public GameObject projectilePrefab;

    [Header("Difficulty & Complexity")]
    public Difficulty difficulty = Difficulty.Normal;
    public Complexity complexity = Complexity.Medium;

    [Header("Spawns, Lanes, Goals")]
    [Range(1, 5)] public int spawnCount = 1;
    [Range(1, 6)] public int laneCount = 2;

    [Tooltip("Wenn nur 1 Spawn aber mehrere Lanes: Aufteilung innerhalb der ersten N Tiles.")]
    [Range(2, 12)] public int splitWithinTiles = 6;

    [Tooltip("Anzahl Ziele (GoalPoints) auf der rechten Map-Kante. Pfade gehen aktuell zu Goal #0.")]
    [Range(1, 4)] public int goalCount = 1;

    [Tooltip("0 = random; sonst deterministisch")]
    public int seed = 0;

    [Header("Grid Size (Base)")]
    public int baseWidth = 18;
    public int baseHeight = 12;

    [Header("Path Settings")]
    [Range(1, 2)] public int pathWidth = 1;
    [Range(0f, 1f)] public float winding = 0.70f;
    [Range(0f, 1f)] public float branchChance = 0.20f;
    [Range(3, 20)] public int branchMaxLength = 8;

    [Header("Build Area")]
    [Range(1, 4)] public int buildMargin = 2;
    [Range(0f, 1f)] public float buildFill = 0.70f;

    [Header("Tiles")]
    public float tileSize = 1f;
    public float tileHeight = 0.2f;
    public GameObject tilePrefab; // optional, sonst Cube

    public Material buildMat;
    public Material pathMat;
    public Material blockedMat;

    [Header("Layer Names (must exist, auto-created in Editor)")]
    public string buildLayerName = "BuildTile";
    public string pathLayerName = "Path";
    public string blockedLayerName = "Block";
    public string enemyLayerName = "Enemy";

    [Header("Auto Setup Toggles")]
    public bool autoCreateEnemySpawners = true;
    public bool autoCreateBuildManager = true;
    public bool autoSetupTowerPrefab = true;
    public bool autoSetupEnemyPrefab = true;

    [Header("EnemySpawner Defaults")]
    public int enemiesPerWave = 10;
    public float spawnInterval = 0.8f;

    [Header("Outputs (generated)")]
    public List<Transform> spawnPoints = new();
    public List<Transform> goalPoints = new();
    public List<Transform> waypointsRoots = new();

    // internal grid:
    // '.' blocked, 'P' path, 'B' build, 'S' spawn, 'G' goal
    private char[,] grid;
    private int width;
    private int height;
    private Vector3 origin;
    private System.Random rng;

    private Transform buildParent;
    private Transform pathParent;
    private Transform blockedParent;

    private void Start()
    {
        if (generateOnPlay)
            Generate();
    }

    [ContextMenu("Generate")]
    public void Generate()
    {
        ApplyDifficultyPresets();

#if UNITY_EDITOR
        // 1) Ensure layers exist (Editor only)
        EnsureProjectLayer(buildLayerName);
        EnsureProjectLayer(pathLayerName);
        EnsureProjectLayer(blockedLayerName);
        EnsureProjectLayer(enemyLayerName);

        // 2) Auto-load prefabs from Assets/Prefab
        AutoLoadPrefabsEditorOnly();

        // 3) Auto-setup prefabs (Tower/Enemy)
        if (autoSetupTowerPrefab) AutoSetupTowerPrefabEditorOnly();
        if (autoSetupEnemyPrefab) AutoSetupEnemyPrefabEditorOnly();
#endif

        rng = new System.Random(seed != 0 ? seed : UnityEngine.Random.Range(int.MinValue / 2, int.MaxValue / 2));

        (int wBonus, int hBonus) = ComplexityBonus(complexity);
        width = Mathf.Max(10, baseWidth + wBonus);
        height = Mathf.Max(8, baseHeight + hBonus);

        grid = new char[width, height];
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                grid[x, y] = '.';

        // clear old generated content
        ClearChildren(transform);

        spawnPoints.Clear();
        goalPoints.Clear();
        waypointsRoots.Clear();

        buildParent = new GameObject("BuildTiles").transform;
        pathParent = new GameObject("PathTiles").transform;
        blockedParent = new GameObject("BlockedTiles").transform;

        buildParent.SetParent(transform, false);
        pathParent.SetParent(transform, false);
        blockedParent.SetParent(transform, false);

        origin = new Vector3(-(width - 1) * tileSize * 0.5f, 0f, -(height - 1) * tileSize * 0.5f);

        // --- Choose goals ---
        var goalCells = ChooseGoals(goalCount);
        foreach (var g in goalCells) grid[g.x, g.y] = 'G';
        Vector2Int mainGoal = goalCells[0];

        // --- Choose spawns ---
        var spawns = ChooseSpawns(spawnCount, mainGoal);
        foreach (var s in spawns) grid[s.x, s.y] = 'S';

        // --- Backbone path ---
        Vector2Int primarySpawn = spawns[0];
        var backbone = CarvePathRandomAStar(primarySpawn, mainGoal);
        if (backbone == null || backbone.Count < 3)
        {
            Debug.LogError("AutoLevelGenerator: Failed to create backbone path.");
            return;
        }

        PaintPath(backbone);
        if (pathWidth > 1) WidenPath(backbone, pathWidth);

        int lanesToBuild = Mathf.Max(1, laneCount);

        var lanePaths = new List<List<Vector2Int>>();
        lanePaths.Add(new List<Vector2Int>(backbone));

        if (lanesToBuild > 1)
        {
            if (spawns.Count == 1)
                BuildEarlySplitLanes(mainGoal, backbone, lanesToBuild, lanePaths);
            else
                BuildMultiSpawnLanes(spawns, mainGoal, backbone, lanesToBuild, lanePaths);
        }

        AddBranches(backbone);
        CreateBuildAreasAroundPaths();
        BakeTilesToScene(); // sets layers for tiles

        // Markers
        for (int i = 0; i < goalCells.Count; i++)
        {
            var gp = CreateMarker($"GoalPoint_{i:00}", CellToWorld(goalCells[i]), Color.red);
            goalPoints.Add(gp);
        }

        for (int i = 0; i < spawns.Count; i++)
        {
            var sp = CreateMarker($"SpawnPoint_{i:00}", CellToWorld(spawns[i]), Color.cyan);
            spawnPoints.Add(sp);
        }

        // Waypoints roots per lane
        for (int i = 0; i < lanePaths.Count; i++)
        {
            var simplified = SimplifyPath(lanePaths[i]);

            var root = new GameObject($"PathWaypoints_{i:00}").transform;
            root.SetParent(transform, false);

            for (int wpi = 0; wpi < simplified.Count; wpi++)
            {
                var wp = new GameObject($"WP_{wpi:00}").transform;
                wp.position = CellToWorld(simplified[wpi]) + Vector3.up * 0.2f;
                wp.SetParent(root, true);
            }

            waypointsRoots.Add(root);
        }

        // Auto components
        if (autoCreateEnemySpawners)
            AddOrConfigureEnemySpawnerOnEachSpawn();

        if (autoCreateBuildManager)
            CreateOrUpdateBuildManager();

#if UNITY_EDITOR
        EditorSceneManager.MarkSceneDirty(gameObject.scene);
#endif

        Debug.Log($"AutoLevelGenerator: Generated grid={width}x{height}, spawns={spawnPoints.Count}, lanes={waypointsRoots.Count}, goals={goalPoints.Count}");
    }

    // =========================================================
    // AUTO: EnemySpawner per spawn (component on SpawnPoint)
    // =========================================================
    private void AddOrConfigureEnemySpawnerOnEachSpawn()
    {
        if (enemyPrefab == null)
        {
            Debug.LogError("AutoLevelGenerator: enemyPrefab is null. Put Enemy.prefab in Assets/Prefab or assign manually.");
            return;
        }

        // If multiple waypoint paths exist, "immer den ersten"
        Transform defaultWaypoints = waypointsRoots != null && waypointsRoots.Count > 0 ? waypointsRoots[0] : null;
        if (defaultWaypoints == null)
        {
            Debug.LogError("AutoLevelGenerator: No PathWaypoints_00 found. Cannot configure EnemySpawners.");
            return;
        }

        LevelController lc = FindObjectOfType<LevelController>();

        for (int i = 0; i < spawnPoints.Count; i++)
        {
            Transform sp = spawnPoints[i];
            if (sp == null) continue;

            // Attach spawner directly to the SpawnPoint marker
            EnemySpawner spawner = sp.GetComponent<EnemySpawner>();
            if (spawner == null) spawner = sp.gameObject.AddComponent<EnemySpawner>();

            // Configure (field names must exist in your EnemySpawner.cs)
            spawner.spawnPoint = sp;
            spawner.pathWaypointsRoot = defaultWaypoints; // always first path
            spawner.enemyPrefab = enemyPrefab;
            spawner.enemiesPerWave = enemiesPerWave;
            spawner.spawnInterval = spawnInterval;

            // optional if your EnemySpawner has this
            spawner.levelController = lc;
        }

        Debug.Log($"AutoLevelGenerator: Added/Configured EnemySpawner on {spawnPoints.Count} SpawnPoints (using {defaultWaypoints.name}).");
    }

    // =========================================================
    // AUTO: BuildManager
    // =========================================================
    private void CreateOrUpdateBuildManager()
    {
        BuildManager bm = FindObjectOfType<BuildManager>();
        if (bm == null)
        {
            GameObject go = new GameObject("BuildManager");
            bm = go.AddComponent<BuildManager>();
        }

        if (bm.mainCamera == null)
            bm.mainCamera = Camera.main;

        if (bm.towerPrefab == null && towerPrefab != null)
            bm.towerPrefab = towerPrefab;

        int buildLayer = LayerMask.NameToLayer(buildLayerName);
        if (buildLayer >= 0)
            bm.buildTileMask = 1 << buildLayer;
        else
            Debug.LogWarning($"AutoLevelGenerator: Layer '{buildLayerName}' not found. BuildManager.buildTileMask not set.");
    }

    // =========================================================
    // EDITOR: Auto-load + Auto-setup Prefabs + Ensure Layers
    // =========================================================
#if UNITY_EDITOR
    private void AutoLoadPrefabsEditorOnly()
    {
        enemyPrefab ??= LoadPrefabByName("Enemy");
        towerPrefab ??= LoadPrefabByName("TowerPrefab");
        projectilePrefab ??= LoadPrefabByName("Projectile");

        if (enemyPrefab == null) Debug.LogWarning("AutoLevelGenerator(Editor): Could not auto-load Enemy.prefab from Assets/Prefab.");
        if (towerPrefab == null) Debug.LogWarning("AutoLevelGenerator(Editor): Could not auto-load TowerPrefab.prefab from Assets/Prefab.");
        if (projectilePrefab == null) Debug.LogWarning("AutoLevelGenerator(Editor): Could not auto-load Projectile.prefab from Assets/Prefab.");
    }

    private GameObject LoadPrefabByName(string prefabNameNoExt)
    {
        string[] guids = AssetDatabase.FindAssets($"{prefabNameNoExt} t:Prefab", new[] { prefabFolder });
        if (guids == null || guids.Length == 0) return null;

        foreach (var g in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(g);
            if (path.EndsWith($"/{prefabNameNoExt}.prefab", StringComparison.OrdinalIgnoreCase))
                return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }

        string firstPath = AssetDatabase.GUIDToAssetPath(guids[0]);
        return AssetDatabase.LoadAssetAtPath<GameObject>(firstPath);
    }

    private void AutoSetupTowerPrefabEditorOnly()
    {
        if (towerPrefab == null)
            return;

        string path = AssetDatabase.GetAssetPath(towerPrefab);
        if (string.IsNullOrEmpty(path))
            return;

        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            // Ensure TowerShooter exists
            var shooter = root.GetComponent<TowerShooter>();
            if (shooter == null)
                shooter = root.AddComponent<TowerShooter>();

            // Ensure FirePoint exists
            Transform firePoint = root.transform.Find("FirePoint");
            if (firePoint == null)
            {
                var fpGo = new GameObject("FirePoint");
                fpGo.transform.SetParent(root.transform, false);
                fpGo.transform.localPosition = new Vector3(0f, 1.2f, 0.3f);
                fpGo.transform.localRotation = Quaternion.identity;
                fpGo.transform.localScale = Vector3.one;
                firePoint = fpGo.transform;
            }

            shooter.firePoint = firePoint;

            // Set projectile prefab if missing
            if (shooter.projectilePrefab == null && projectilePrefab != null)
                shooter.projectilePrefab = projectilePrefab;

            // Set enemy mask to Enemy layer
            int enemyLayer = LayerMask.NameToLayer(enemyLayerName);
            if (enemyLayer >= 0)
                shooter.enemyMask = 1 << enemyLayer;

            // Basic defaults if you want
            if (shooter.range <= 0.1f) shooter.range = 6f;
            if (shooter.fireRate <= 0.1f) shooter.fireRate = 1.2f;
            if (shooter.damage <= 0.1f) shooter.damage = 3f;
            if (shooter.projectileSpeed <= 0.1f) shooter.projectileSpeed = 10f;

            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private void AutoSetupEnemyPrefabEditorOnly()
    {
        if (enemyPrefab == null)
            return;

        string path = AssetDatabase.GetAssetPath(enemyPrefab);
        if (string.IsNullOrEmpty(path))
            return;

        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            // Ensure layer Enemy
            int enemyLayer = LayerMask.NameToLayer(enemyLayerName);
            if (enemyLayer >= 0)
                SetLayerRecursively(root, enemyLayer);

            // Ensure EnemyHealth exists
            if (root.GetComponent<EnemyHealth>() == null)
                root.AddComponent<EnemyHealth>();

            // Ensure EnemyMover exists (if your mover is required on prefab; if you add it in spawner, this is optional)
            if (root.GetComponent<EnemyMover>() == null)
                root.AddComponent<EnemyMover>();

            // Ensure a collider exists (for targeting overlap sphere it helps; capsule usually has one already)
            if (root.GetComponent<Collider>() == null)
                root.AddComponent<CapsuleCollider>();

            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private void SetLayerRecursively(GameObject go, int layer)
    {
        go.layer = layer;
        foreach (Transform child in go.transform)
            SetLayerRecursively(child.gameObject, layer);
    }

    private void EnsureProjectLayer(string layerName)
    {
        if (LayerMask.NameToLayer(layerName) != -1)
            return;

        // Add layer to TagManager.asset
        var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        var layersProp = tagManager.FindProperty("layers");

        // Unity has 0-7 reserved. We'll use first empty slot from 8..31.
        for (int i = 8; i <= 31; i++)
        {
            var sp = layersProp.GetArrayElementAtIndex(i);
            if (sp != null && string.IsNullOrEmpty(sp.stringValue))
            {
                sp.stringValue = layerName;
                tagManager.ApplyModifiedProperties();
                Debug.Log($"AutoLevelGenerator(Editor): Created layer '{layerName}' at index {i}.");
                return;
            }
        }

        Debug.LogWarning($"AutoLevelGenerator(Editor): Could not create layer '{layerName}'. No free user layer slots (8..31).");
    }
#endif

    // =========================================================
    // GENERATION: Lanes
    // =========================================================
    private void BuildEarlySplitLanes(Vector2Int goal, List<Vector2Int> backbone, int lanesToBuild, List<List<Vector2Int>> lanePaths)
    {
        int maxSplitIndex = Mathf.Clamp(splitWithinTiles, 2, Mathf.Min(backbone.Count - 2, 20));
        int minSplitIndex = 2;

        int mergeMin = Mathf.Clamp(backbone.Count / 3, 4, backbone.Count - 3);
        int mergeMax = Mathf.Clamp(backbone.Count * 2 / 3, mergeMin + 1, backbone.Count - 2);

        var usedFirstDirs = new HashSet<Vector2Int>();

        for (int lane = 1; lane < lanesToBuild; lane++)
        {
            int splitIndex = rng.Next(minSplitIndex, maxSplitIndex + 1);
            Vector2Int splitCell = backbone[splitIndex];

            Vector2Int backboneNext = backbone[Mathf.Min(splitIndex + 1, backbone.Count - 1)];
            Vector2Int backboneDir = backboneNext - splitCell;

            var neighbors = Neighbors4(splitCell)
                .Where(Inside)
                .Where(n => n != backboneNext)
                .OrderBy(_ => rng.Next())
                .ToList();

            if (neighbors.Count == 0) continue;

            Vector2Int forcedFirst = default;
            bool found = false;

            foreach (var n in neighbors)
            {
                Vector2Int dir = n - splitCell;
                if (dir == backboneDir) continue;
                if (usedFirstDirs.Contains(dir)) continue;
                forcedFirst = n;
                usedFirstDirs.Add(dir);
                found = true;
                break;
            }

            if (!found)
            {
                forcedFirst = neighbors[0];
                usedFirstDirs.Add(forcedFirst - splitCell);
            }

            Vector2Int mergeCell = backbone[rng.Next(mergeMin, mergeMax + 1)];
            var mid = CarvePathRandomAStar(forcedFirst, mergeCell);

            if (mid == null || mid.Count < 2)
            {
                mid = CarvePathRandomAStar(forcedFirst, goal);
                if (mid == null || mid.Count < 2) continue;
                mergeCell = goal;
            }

            var fullLane = new List<Vector2Int>();
            for (int i = 0; i <= splitIndex; i++) fullLane.Add(backbone[i]);

            if (fullLane.Count == 0 || fullLane[^1] != splitCell) fullLane.Add(splitCell);
            fullLane.Add(forcedFirst);

            for (int i = 1; i < mid.Count; i++) fullLane.Add(mid[i]);

            PaintPath(fullLane);
            if (pathWidth > 1) WidenPath(fullLane, pathWidth);

            int mergeIdx = backbone.IndexOf(mergeCell);
            if (mergeIdx >= 0)
            {
                for (int i = mergeIdx + 1; i < backbone.Count; i++)
                    fullLane.Add(backbone[i]);
            }

            lanePaths.Add(fullLane);
        }

        while (lanePaths.Count < lanesToBuild)
            lanePaths.Add(new List<Vector2Int>(backbone));
    }

    private void BuildMultiSpawnLanes(List<Vector2Int> spawns, Vector2Int goal, List<Vector2Int> backbone, int lanesToBuild, List<List<Vector2Int>> lanePaths)
    {
        int mergeMin = Mathf.Clamp(backbone.Count / 3, 4, backbone.Count - 3);
        int mergeMax = Mathf.Clamp(backbone.Count * 2 / 3, mergeMin + 1, backbone.Count - 2);

        int laneIndex = 1;
        int spawnIdx = 1;

        while (laneIndex < lanesToBuild)
        {
            Vector2Int spawn = spawns[Mathf.Min(spawnIdx, spawns.Count - 1)];
            spawnIdx++;

            Vector2Int merge = backbone[rng.Next(mergeMin, mergeMax + 1)];
            var lane = CarvePathRandomAStar(spawn, merge);

            if (lane == null || lane.Count < 2)
            {
                lane = CarvePathRandomAStar(spawn, goal);
                if (lane == null || lane.Count < 2) break;
                merge = goal;
            }

            PaintPath(lane);
            if (pathWidth > 1) WidenPath(lane, pathWidth);

            int mergeIdx = backbone.IndexOf(merge);
            var fullLane = new List<Vector2Int>(lane);

            if (mergeIdx >= 0)
            {
                if (fullLane.Count > 0 && fullLane[^1] == backbone[mergeIdx])
                {
                    for (int i = mergeIdx + 1; i < backbone.Count; i++)
                        fullLane.Add(backbone[i]);
                }
                else
                {
                    for (int i = mergeIdx; i < backbone.Count; i++)
                        fullLane.Add(backbone[i]);
                }
            }

            lanePaths.Add(fullLane);
            laneIndex++;
        }

        while (lanePaths.Count < lanesToBuild)
            lanePaths.Add(new List<Vector2Int>(backbone));
    }

    // =========================================================
    // GENERATION: Difficulty / Complexity / Goals / Spawns
    // =========================================================
    private void ApplyDifficultyPresets()
    {
        switch (difficulty)
        {
            case Difficulty.Easy:
                pathWidth = 2; winding = 0.50f; branchChance = 0.10f; buildMargin = 3; buildFill = 0.80f; break;
            case Difficulty.Normal:
                pathWidth = 2; winding = 0.70f; branchChance = 0.20f; buildMargin = 2; buildFill = 0.70f; break;
            case Difficulty.Hard:
                pathWidth = 1; winding = 0.82f; branchChance = 0.28f; buildMargin = 2; buildFill = 0.60f; break;
            case Difficulty.Insane:
                pathWidth = 1; winding = 0.92f; branchChance = 0.38f; buildMargin = 1; buildFill = 0.55f; break;
        }
    }

    private (int w, int h) ComplexityBonus(Complexity c) => c switch
    {
        Complexity.Simple => (0, 0),
        Complexity.Medium => (2, 2),
        Complexity.Complex => (6, 4),
        Complexity.Extreme => (10, 6),
        _ => (0, 0)
    };

    private List<Vector2Int> ChooseGoals(int count)
    {
        count = Mathf.Max(1, count);

        var candidates = new List<Vector2Int>();
        for (int y = 1; y < height - 1; y++)
            candidates.Add(new Vector2Int(width - 1, y));

        candidates = candidates.OrderBy(_ => rng.Next()).ToList();

        var goals = new List<Vector2Int>();
        foreach (var c in candidates)
        {
            bool farEnough = true;
            foreach (var g in goals)
                if (Manhattan(g, c) < 4) { farEnough = false; break; }

            if (farEnough) goals.Add(c);
            if (goals.Count >= count) break;
        }

        if (goals.Count == 0)
            goals.Add(new Vector2Int(width - 1, height / 2));

        return goals;
    }

    private List<Vector2Int> ChooseSpawns(int count, Vector2Int mainGoal)
    {
        var candidates = new List<Vector2Int>();

        for (int y = 1; y < height - 1; y++)
            candidates.Add(new Vector2Int(0, y));

        if (complexity >= Complexity.Complex)
        {
            for (int x = 1; x < width - 1; x++)
            {
                candidates.Add(new Vector2Int(x, 0));
                candidates.Add(new Vector2Int(x, height - 1));
            }
        }

        candidates = candidates.OrderBy(_ => rng.Next()).ToList();

        var spawns = new List<Vector2Int>();
        foreach (var c in candidates)
        {
            if (c == mainGoal) continue;

            bool farEnough = true;
            foreach (var s in spawns)
                if (Manhattan(s, c) < 4) { farEnough = false; break; }

            if (farEnough) spawns.Add(c);
            if (spawns.Count >= count) break;
        }

        if (spawns.Count == 0)
            spawns.Add(new Vector2Int(0, height / 2));

        return spawns;
    }

    // =========================================================
    // GENERATION: Path carving
    // =========================================================
    private List<Vector2Int> CarvePathRandomAStar(Vector2Int start, Vector2Int goal)
    {
        var open = new List<Vector2Int> { start };
        var cameFrom = new Dictionary<Vector2Int, Vector2Int> { [start] = start };
        var gScore = new Dictionary<Vector2Int, int> { [start] = 0 };

        int safety = width * height * 30;

        while (open.Count > 0 && safety-- > 0)
        {
            Vector2Int current = open[0];
            int bestF = int.MaxValue;

            for (int i = 0; i < open.Count; i++)
            {
                var n = open[i];
                int g = gScore.TryGetValue(n, out var gv) ? gv : int.MaxValue / 4;
                int h = Manhattan(n, goal);
                int noise = (int)(rng.NextDouble() * 6.0 * winding);
                int f = g + h + noise;

                if (f < bestF)
                {
                    bestF = f;
                    current = n;
                }
            }

            if (current == goal)
                return Reconstruct(cameFrom, start, goal);

            open.Remove(current);

            foreach (var nb in Neighbors4(current))
            {
                if (!Inside(nb)) continue;

                int tentativeG = gScore[current] + 1;
                if (!gScore.TryGetValue(nb, out var oldG) || tentativeG < oldG)
                {
                    cameFrom[nb] = current;
                    gScore[nb] = tentativeG;
                    if (!open.Contains(nb)) open.Add(nb);
                }
            }
        }

        return null;
    }

    private List<Vector2Int> Reconstruct(Dictionary<Vector2Int, Vector2Int> cameFrom, Vector2Int start, Vector2Int goal)
    {
        if (!cameFrom.ContainsKey(goal)) return null;

        var path = new List<Vector2Int>();
        var cur = goal;

        int guard = width * height + 10;
        while (cur != start && guard-- > 0)
        {
            path.Add(cur);
            cur = cameFrom[cur];
        }

        path.Add(start);
        path.Reverse();
        return path;
    }

    private void PaintPath(IEnumerable<Vector2Int> cells)
    {
        foreach (var c in cells)
        {
            if (grid[c.x, c.y] == 'G') continue;
            if (grid[c.x, c.y] == 'S') continue;
            grid[c.x, c.y] = 'P';
        }
    }

    private void WidenPath(IEnumerable<Vector2Int> cells, int widthTiles)
    {
        if (widthTiles <= 1) return;
        int radius = widthTiles - 1;

        var list = cells.ToList();
        foreach (var c in list)
        {
            foreach (var n in Neighbors4(c))
            {
                if (!Inside(n)) continue;
                if (Manhattan(n, c) <= radius)
                    if (grid[n.x, n.y] == '.') grid[n.x, n.y] = 'P';
            }
        }
    }

    private void AddBranches(List<Vector2Int> backbone)
    {
        if (branchChance <= 0f) return;

        int maxBranches = complexity switch
        {
            Complexity.Simple => 1,
            Complexity.Medium => 2,
            Complexity.Complex => 4,
            Complexity.Extreme => 7,
            _ => 2
        };

        int branches = 0;

        for (int i = 0; i < backbone.Count && branches < maxBranches; i++)
        {
            if (rng.NextDouble() > branchChance) continue;

            var start = backbone[rng.Next(1, backbone.Count - 1)];
            var cur = start;
            int len = rng.Next(3, branchMaxLength + 1);

            for (int s = 0; s < len; s++)
            {
                var opts = Neighbors4(cur).Where(Inside).OrderBy(_ => rng.Next()).ToList();
                Vector2Int next = default;
                bool found = false;

                foreach (var o in opts)
                {
                    if (grid[o.x, o.y] == '.')
                    {
                        next = o;
                        found = true;
                        break;
                    }
                }
                if (!found) break;

                grid[next.x, next.y] = 'P';
                cur = next;
            }

            branches++;
        }
    }

    private void CreateBuildAreasAroundPaths()
    {
        var pathCells = new HashSet<Vector2Int>();
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                if (grid[x, y] == 'P' || grid[x, y] == 'S' || grid[x, y] == 'G')
                    pathCells.Add(new Vector2Int(x, y));
            }

        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                var c = new Vector2Int(x, y);
                if (pathCells.Contains(c)) continue;

                int d = DistanceToNearest(pathCells, c, buildMargin + 2);
                if (d <= buildMargin)
                {
                    float diffPenalty = difficulty switch
                    {
                        Difficulty.Easy => 0.05f,
                        Difficulty.Normal => 0.12f,
                        Difficulty.Hard => 0.22f,
                        Difficulty.Insane => 0.32f,
                        _ => 0.12f
                    };

                    float p = Mathf.Clamp01(buildFill - diffPenalty);
                    if (rng.NextDouble() < p)
                        grid[x, y] = 'B';
                }
            }
    }

    // =========================================================
    // Baking tiles (set correct layers here!)
    // =========================================================
    private void BakeTilesToScene()
    {
        int buildLayer = LayerMask.NameToLayer(buildLayerName);
        int pathLayer = LayerMask.NameToLayer(pathLayerName);
        int blockedLayer = LayerMask.NameToLayer(blockedLayerName);

        if (buildLayer < 0) Debug.LogWarning($"Layer '{buildLayerName}' missing. Build tiles may be on Default.");
        if (pathLayer < 0) Debug.LogWarning($"Layer '{pathLayerName}' missing. Path tiles may be on Default.");
        if (blockedLayer < 0) Debug.LogWarning($"Layer '{blockedLayerName}' missing. Blocked tiles may be on Default.");

        // set parent layers too (helps debugging in hierarchy)
        if (buildLayer >= 0) buildParent.gameObject.layer = buildLayer;
        if (pathLayer >= 0) pathParent.gameObject.layer = pathLayer;
        if (blockedLayer >= 0) blockedParent.gameObject.layer = blockedLayer;

        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                char c = grid[x, y];
                Vector3 pos = CellToWorld(new Vector2Int(x, y));

                switch (c)
                {
                    case 'B':
                        CreateTile($"Build_{x}_{y}", pos, buildMat, buildLayer, buildParent);
                        break;

                    case 'P':
                    case 'S':
                    case 'G':
                        CreateTile($"Path_{x}_{y}", pos, pathMat, pathLayer, pathParent);
                        break;

                    default:
                        CreateTile($"Block_{x}_{y}", pos, blockedMat, blockedLayer, blockedParent);
                        break;
                }
            }
    }

    private GameObject CreateTile(string name, Vector3 pos, Material mat, int layer, Transform parent)
    {
        GameObject go;
        if (tilePrefab != null)
        {
            go = Instantiate(tilePrefab, pos, Quaternion.identity, parent);
        }
        else
        {
            go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.transform.SetParent(parent, true);
            go.transform.position = pos;
            go.transform.localScale = new Vector3(tileSize, tileHeight, tileSize);
        }

        go.name = name;
        if (layer >= 0) go.layer = layer;

        var r = go.GetComponent<Renderer>();
        if (r != null && mat != null) r.sharedMaterial = mat;

        return go;
    }

    private Transform CreateMarker(string name, Vector3 pos, Color color)
    {
        var marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        marker.name = name;
        marker.transform.SetParent(transform, true);
        marker.transform.position = pos + Vector3.up * 0.5f;
        marker.transform.localScale = new Vector3(0.6f, 0.5f, 0.6f);

        var r = marker.GetComponent<Renderer>();
        if (r != null)
        {
            var m = new Material(Shader.Find("Standard"));
            m.color = color;
            r.sharedMaterial = m;
        }
        return marker.transform;
    }

    // =========================================================
    // Helpers
    // =========================================================
    private Vector3 CellToWorld(Vector2Int cell)
    {
        return origin + new Vector3(cell.x * tileSize, tileHeight * 0.5f, (height - 1 - cell.y) * tileSize);
    }

    private void ClearChildren(Transform root)
    {
        for (int i = root.childCount - 1; i >= 0; i--)
            DestroyImmediate(root.GetChild(i).gameObject);
    }

    private IEnumerable<Vector2Int> Neighbors4(Vector2Int c)
    {
        yield return new Vector2Int(c.x + 1, c.y);
        yield return new Vector2Int(c.x - 1, c.y);
        yield return new Vector2Int(c.x, c.y + 1);
        yield return new Vector2Int(c.x, c.y - 1);
    }

    private bool Inside(Vector2Int c) => c.x >= 0 && c.x < width && c.y >= 0 && c.y < height;

    private int Manhattan(Vector2Int a, Vector2Int b) => Math.Abs(a.x - b.x) + Math.Abs(a.y - b.y);

    private int DistanceToNearest(HashSet<Vector2Int> set, Vector2Int cell, int maxSearch)
    {
        int best = int.MaxValue;
        foreach (var p in set)
        {
            int d = Manhattan(p, cell);
            if (d < best) best = d;
            if (best <= 1) return best;
        }
        return best > maxSearch ? maxSearch + 1 : best;
    }

    private List<Vector2Int> SimplifyPath(List<Vector2Int> path)
    {
        if (path == null || path.Count <= 2) return path;

        var res = new List<Vector2Int>();
        res.Add(path[0]);

        Vector2Int prevDir = path[1] - path[0];
        for (int i = 1; i < path.Count - 1; i++)
        {
            Vector2Int dir = path[i + 1] - path[i];
            if (dir != prevDir) res.Add(path[i]);
            prevDir = dir;
        }

        res.Add(path[^1]);
        return res;
    }
}
