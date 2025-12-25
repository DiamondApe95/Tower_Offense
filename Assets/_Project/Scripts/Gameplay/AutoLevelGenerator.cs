using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TowerConquest.Gameplay;
using TowerConquest.UI;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

/// <summary>
/// AutoLevelGenerator: Generiert komplette spielbare Levels mit einem Klick.
/// Erstellt Grid, Pfade, Spawner, GUI und alle notwendigen Komponenten.
/// </summary>
public class AutoLevelGenerator : MonoBehaviour
{
    public enum Difficulty { Easy, Normal, Hard, Insane }
    public enum Complexity { Simple, Medium, Complex, Extreme }

    [Header("=== ONE-CLICK COMPLETE SETUP ===")]
    [Tooltip("Erstellt Level + GUI + Controller + Kamera - alles in einem Klick!")]
    public bool autoSetupOnPlay = false;

    [ContextMenu("★ Complete Scene Setup (One-Click)")]
    public void CompleteSceneSetup()
    {
        Debug.Log("AutoLevelGenerator: Starting Complete Scene Setup...");

        // 1. Generate Level Geometry
        Generate();

        // 2. Setup Camera
        SetupCamera();

        // 3. Create GUI (Canvas, HUD, ResultScreen)
        CreateCompleteGUI();

        // 4. Setup LevelController with all references
        SetupLevelController();

        // 5. Configure Lighting
        SetupLighting();

#if UNITY_EDITOR
        EditorSceneManager.MarkSceneDirty(gameObject.scene);
#endif

        Debug.Log("AutoLevelGenerator: ★ Complete Scene Setup finished! Scene is ready to play.");
    }

    [Header("Generate Level Only")]
    public bool generateOnPlay = false;

    [ContextMenu("Generate Level Only")]
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
        if (autoSetupOnPlay)
        {
            CompleteSceneSetup();
        }
        else if (generateOnPlay)
        {
            Generate();
        }
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

    // =========================================================
    // GUI GENERATION: Complete UI Setup
    // =========================================================

    private Canvas generatedCanvas;
    private LevelHUD generatedHUD;
    private ResultScreenView generatedResultScreen;
    private GameObject generatedCardViewPrefab;

    /// <summary>
    /// Erstellt die komplette GUI für das Level:
    /// - Canvas mit EventSystem
    /// - LevelHUD mit Buttons und Texten
    /// - ResultScreen für Sieg/Niederlage
    /// - CardView Template für Handkarten
    /// </summary>
    private void CreateCompleteGUI()
    {
        Debug.Log("AutoLevelGenerator: Creating Complete GUI...");

        // Entferne alte GUI falls vorhanden
        CleanupOldGUI();

        // 1. Canvas erstellen
        CreateCanvas();

        // 2. LevelHUD erstellen
        CreateLevelHUD();

        // 3. ResultScreen erstellen
        CreateResultScreen();

        // 4. CardView Prefab erstellen (als Template im Container)
        CreateCardViewTemplate();

        Debug.Log("AutoLevelGenerator: GUI creation complete.");
    }

    private void CleanupOldGUI()
    {
        // Suche und entferne alte generierte GUI
        var oldCanvas = GameObject.Find("GeneratedCanvas");
        if (oldCanvas != null)
            DestroyImmediate(oldCanvas);

        var oldEventSystem = GameObject.Find("EventSystem");
        if (oldEventSystem != null && oldEventSystem.GetComponent<UnityEngine.EventSystems.EventSystem>() != null)
        {
            // Nur entfernen wenn es keine anderen gibt
            var allEventSystems = FindObjectsByType<UnityEngine.EventSystems.EventSystem>(FindObjectsSortMode.None);
            if (allEventSystems.Length <= 1)
                DestroyImmediate(oldEventSystem);
        }
    }

    private void CreateCanvas()
    {
        // Canvas GameObject
        var canvasGO = new GameObject("GeneratedCanvas");
        generatedCanvas = canvasGO.AddComponent<Canvas>();
        generatedCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        generatedCanvas.sortingOrder = 100;

        // Canvas Scaler für responsive UI
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        // Graphic Raycaster für Interaktion
        canvasGO.AddComponent<GraphicRaycaster>();

        // EventSystem erstellen falls nicht vorhanden
        if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var eventSystemGO = new GameObject("EventSystem");
            eventSystemGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystemGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        Debug.Log("AutoLevelGenerator: Canvas created.");
    }

    private void CreateLevelHUD()
    {
        if (generatedCanvas == null) return;

        // HUD Container
        var hudGO = new GameObject("LevelHUD");
        hudGO.transform.SetParent(generatedCanvas.transform, false);
        generatedHUD = hudGO.AddComponent<LevelHUD>();

        var hudRect = hudGO.AddComponent<RectTransform>();
        hudRect.anchorMin = Vector2.zero;
        hudRect.anchorMax = Vector2.one;
        hudRect.offsetMin = Vector2.zero;
        hudRect.offsetMax = Vector2.zero;

        // === TOP BAR ===
        var topBar = CreateUIPanel("TopBar", hudGO.transform,
            new Vector2(0, 1), new Vector2(1, 1),
            new Vector2(0, -10), new Vector2(0, -60),
            new Color(0.1f, 0.1f, 0.1f, 0.8f));

        // Wave Text (links oben)
        var waveText = CreateUIText("WaveText", topBar.transform, "Wave 1/5",
            new Vector2(0, 0.5f), new Vector2(0, 0.5f),
            new Vector2(20, 0), new Vector2(200, 40), 24);
        generatedHUD.waveText = waveText;

        // Energy Text (mitte)
        var energyText = CreateUIText("EnergyText", topBar.transform, "Energy 10/10",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(-100, 0), new Vector2(200, 40), 24);
        generatedHUD.energyText = energyText;

        // Speed Button (rechts)
        var speedBtn = CreateUIButton("SpeedButton", topBar.transform, "1x",
            new Vector2(1, 0.5f), new Vector2(1, 0.5f),
            new Vector2(-120, 0), new Vector2(80, 40),
            new Color(0.3f, 0.3f, 0.6f, 1f));
        generatedHUD.speedButton = speedBtn.GetComponent<Button>();
        generatedHUD.speedText = speedBtn.GetComponentInChildren<Text>();

        // === START WAVE BUTTON (unten rechts) ===
        var startBtn = CreateUIButton("StartWaveButton", hudGO.transform, "START WAVE",
            new Vector2(1, 0), new Vector2(1, 0),
            new Vector2(-170, 100), new Vector2(150, 60),
            new Color(0.2f, 0.6f, 0.2f, 1f));
        generatedHUD.startWaveButton = startBtn.GetComponent<Button>();

        // === HAND CONTAINER (unten) ===
        var handContainer = CreateUIPanel("HandContainer", hudGO.transform,
            new Vector2(0, 0), new Vector2(0.7f, 0),
            new Vector2(20, 20), new Vector2(-20, 120),
            new Color(0.15f, 0.15f, 0.15f, 0.7f));

        // Horizontal Layout für Karten
        var hlg = handContainer.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 10;
        hlg.padding = new RectOffset(10, 10, 10, 10);
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childControlWidth = false;
        hlg.childControlHeight = false;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;

        generatedHUD.handContainer = handContainer.transform;

        Debug.Log("AutoLevelGenerator: LevelHUD created with all UI elements.");
    }

    private void CreateResultScreen()
    {
        if (generatedCanvas == null) return;

        // Result Screen Container (standardmäßig deaktiviert)
        var resultGO = new GameObject("ResultScreen");
        resultGO.transform.SetParent(generatedCanvas.transform, false);
        generatedResultScreen = resultGO.AddComponent<ResultScreenView>();

        var resultRect = resultGO.AddComponent<RectTransform>();
        resultRect.anchorMin = Vector2.zero;
        resultRect.anchorMax = Vector2.one;
        resultRect.offsetMin = Vector2.zero;
        resultRect.offsetMax = Vector2.zero;

        // Root ist der Container selbst
        generatedResultScreen.root = resultGO;

        // Dunkler Overlay-Hintergrund
        var overlay = CreateUIPanel("Overlay", resultGO.transform,
            Vector2.zero, Vector2.one,
            Vector2.zero, Vector2.zero,
            new Color(0, 0, 0, 0.75f));

        // Zentriertes Panel
        var centerPanel = CreateUIPanel("CenterPanel", resultGO.transform,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(-200, -150), new Vector2(200, 150),
            new Color(0.2f, 0.2f, 0.25f, 0.95f));

        // Result Label
        var resultLabel = CreateUIText("ResultLabel", centerPanel.transform, "VICTORY",
            new Vector2(0.5f, 0.7f), new Vector2(0.5f, 0.7f),
            new Vector2(-150, -30), new Vector2(150, 30), 48);
        resultLabel.alignment = TextAnchor.MiddleCenter;
        resultLabel.fontStyle = FontStyle.Bold;
        generatedResultScreen.resultLabel = resultLabel;

        // Next Level Button
        var nextBtn = CreateUIButton("NextLevelButton", centerPanel.transform, "NEXT LEVEL",
            new Vector2(0.5f, 0.3f), new Vector2(0.5f, 0.3f),
            new Vector2(-75, -25), new Vector2(75, 25),
            new Color(0.2f, 0.5f, 0.7f, 1f));
        generatedResultScreen.nextLevelButton = nextBtn.GetComponent<Button>();

        // Standardmäßig deaktiviert
        resultGO.SetActive(false);

        Debug.Log("AutoLevelGenerator: ResultScreen created.");
    }

    private void CreateCardViewTemplate()
    {
        if (generatedCanvas == null || generatedHUD == null) return;

        // CardView als Template erstellen (wird im HandContainer geklont)
        var cardGO = new GameObject("CardViewTemplate");
        cardGO.transform.SetParent(generatedCanvas.transform, false);

        var cardView = cardGO.AddComponent<CardView>();

        var cardRect = cardGO.AddComponent<RectTransform>();
        cardRect.sizeDelta = new Vector2(120, 80);

        // Card Background
        var bgImage = cardGO.AddComponent<Image>();
        bgImage.color = new Color(0.25f, 0.25f, 0.3f, 1f);

        // Card Button
        var cardButton = cardGO.AddComponent<Button>();
        cardButton.targetGraphic = bgImage;
        var colors = cardButton.colors;
        colors.normalColor = new Color(0.25f, 0.25f, 0.3f, 1f);
        colors.highlightedColor = new Color(0.35f, 0.35f, 0.45f, 1f);
        colors.pressedColor = new Color(0.2f, 0.2f, 0.25f, 1f);
        colors.disabledColor = new Color(0.15f, 0.15f, 0.15f, 0.5f);
        cardButton.colors = colors;

        cardView.button = cardButton;

        // Card Label
        var labelGO = new GameObject("Label");
        labelGO.transform.SetParent(cardGO.transform, false);
        var labelRect = labelGO.AddComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0, 0.3f);
        labelRect.anchorMax = new Vector2(1, 1);
        labelRect.offsetMin = new Vector2(5, 0);
        labelRect.offsetMax = new Vector2(-5, -5);

        var labelText = labelGO.AddComponent<Text>();
        labelText.text = "Card Name";
        labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        labelText.fontSize = 14;
        labelText.alignment = TextAnchor.MiddleCenter;
        labelText.color = Color.white;
        cardView.label = labelText;

        // Cost Label
        var costGO = new GameObject("CostLabel");
        costGO.transform.SetParent(cardGO.transform, false);
        var costRect = costGO.AddComponent<RectTransform>();
        costRect.anchorMin = Vector2.zero;
        costRect.anchorMax = new Vector2(1, 0.3f);
        costRect.offsetMin = new Vector2(5, 5);
        costRect.offsetMax = new Vector2(-5, 0);

        var costText = costGO.AddComponent<Text>();
        costText.text = "3";
        costText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        costText.fontSize = 18;
        costText.fontStyle = FontStyle.Bold;
        costText.alignment = TextAnchor.MiddleCenter;
        costText.color = new Color(1f, 0.85f, 0.3f, 1f);
        cardView.costLabel = costText;

        // Template deaktivieren und als Prefab-Ersatz nutzen
        cardGO.SetActive(false);
        generatedHUD.cardViewPrefab = cardView;

        Debug.Log("AutoLevelGenerator: CardView template created.");
    }

    // =========================================================
    // HELPER: UI Element Creation
    // =========================================================

    private GameObject CreateUIPanel(string name, Transform parent,
        Vector2 anchorMin, Vector2 anchorMax,
        Vector2 offsetMin, Vector2 offsetMax, Color color)
    {
        var panelGO = new GameObject(name);
        panelGO.transform.SetParent(parent, false);

        var rect = panelGO.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;

        var image = panelGO.AddComponent<Image>();
        image.color = color;

        return panelGO;
    }

    private Text CreateUIText(string name, Transform parent, string text,
        Vector2 anchorMin, Vector2 anchorMax,
        Vector2 offsetMin, Vector2 offsetMax, int fontSize)
    {
        var textGO = new GameObject(name);
        textGO.transform.SetParent(parent, false);

        var rect = textGO.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;

        var textComp = textGO.AddComponent<Text>();
        textComp.text = text;
        textComp.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        textComp.fontSize = fontSize;
        textComp.color = Color.white;
        textComp.alignment = TextAnchor.MiddleLeft;

        return textComp;
    }

    private GameObject CreateUIButton(string name, Transform parent, string text,
        Vector2 anchorMin, Vector2 anchorMax,
        Vector2 offsetMin, Vector2 offsetMax, Color bgColor)
    {
        var btnGO = new GameObject(name);
        btnGO.transform.SetParent(parent, false);

        var rect = btnGO.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;

        var image = btnGO.AddComponent<Image>();
        image.color = bgColor;

        var button = btnGO.AddComponent<Button>();
        button.targetGraphic = image;

        // Text als Kind
        var textGO = new GameObject("Text");
        textGO.transform.SetParent(btnGO.transform, false);
        var textRect = textGO.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        var textComp = textGO.AddComponent<Text>();
        textComp.text = text;
        textComp.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        textComp.fontSize = 18;
        textComp.color = Color.white;
        textComp.alignment = TextAnchor.MiddleCenter;

        return btnGO;
    }

    // =========================================================
    // CAMERA SETUP
    // =========================================================

    private void SetupCamera()
    {
        Camera mainCam = Camera.main;
        if (mainCam == null)
        {
            var camGO = new GameObject("Main Camera");
            camGO.tag = "MainCamera";
            mainCam = camGO.AddComponent<Camera>();
            camGO.AddComponent<AudioListener>();
        }

        // Positioniere Kamera für Top-Down-Ansicht des Levels
        float levelWidth = width * tileSize;
        float levelHeight = height * tileSize;
        float maxDimension = Mathf.Max(levelWidth, levelHeight);
        float cameraHeight = maxDimension * 0.8f + 5f;

        mainCam.transform.position = new Vector3(0, cameraHeight, -maxDimension * 0.3f);
        mainCam.transform.rotation = Quaternion.Euler(60f, 0f, 0f);

        mainCam.clearFlags = CameraClearFlags.SolidColor;
        mainCam.backgroundColor = new Color(0.1f, 0.12f, 0.15f);
        mainCam.nearClipPlane = 0.3f;
        mainCam.farClipPlane = 500f;

        Debug.Log($"AutoLevelGenerator: Camera positioned for {width}x{height} level.");
    }

    // =========================================================
    // LEVEL CONTROLLER SETUP
    // =========================================================

    private void SetupLevelController()
    {
        // Suche oder erstelle LevelController
        LevelController lc = FindObjectOfType<LevelController>();
        if (lc == null)
        {
            var lcGO = new GameObject("LevelController");
            lc = lcGO.AddComponent<LevelController>();
        }

        // Setze HUD-Referenz
        if (generatedHUD != null)
        {
            lc.hud = generatedHUD;
            Debug.Log("AutoLevelGenerator: LevelController.hud assigned.");
        }

        // Setze ResultScreen-Referenz
        if (generatedResultScreen != null)
        {
            lc.resultScreen = generatedResultScreen;
            Debug.Log("AutoLevelGenerator: LevelController.resultScreen assigned.");
        }

        // LevelSpawner hinzufügen falls nicht vorhanden
        var spawner = lc.GetComponent<LevelSpawner>();
        if (spawner == null)
        {
            spawner = lc.gameObject.AddComponent<LevelSpawner>();
        }
        lc.levelSpawner = spawner;

        Debug.Log("AutoLevelGenerator: LevelController fully configured.");
    }

    // =========================================================
    // LIGHTING SETUP
    // =========================================================

    private void SetupLighting()
    {
        // Suche oder erstelle Directional Light
        Light dirLight = null;
        var lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
        foreach (var l in lights)
        {
            if (l.type == LightType.Directional)
            {
                dirLight = l;
                break;
            }
        }

        if (dirLight == null)
        {
            var lightGO = new GameObject("Directional Light");
            dirLight = lightGO.AddComponent<Light>();
            dirLight.type = LightType.Directional;
        }

        dirLight.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        dirLight.color = new Color(1f, 0.96f, 0.9f);
        dirLight.intensity = 1.2f;
        dirLight.shadows = LightShadows.Soft;

        Debug.Log("AutoLevelGenerator: Lighting configured.");
    }
}
