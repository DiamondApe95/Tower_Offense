using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;
using TowerConquest.Debug;

/// <summary>
/// Automatically bakes NavMesh at runtime for dynamically generated levels.
/// This enables units (builders, enemies) to use NavMeshAgent for pathfinding.
/// </summary>
public class NavMeshRuntimeBaker : MonoBehaviour
{
    [Header("NavMesh Settings")]
    [Tooltip("Agent radius - should match builder/unit size")]
    public float agentRadius = 0.5f;

    [Tooltip("Agent height - should match builder/unit height")]
    public float agentHeight = 1.0f;

    [Tooltip("Maximum walkable slope in degrees")]
    public float maxSlope = 45f;

    [Tooltip("Maximum step height")]
    public float stepHeight = 0.4f;

    [Header("Baking")]
    [Tooltip("Bake NavMesh automatically on Start")]
    public bool bakeOnStart = true;

    [Tooltip("Delay before baking (allows level generation to complete)")]
    public float bakeDelay = 0.5f;

    [Header("Walkable Layers")]
    [Tooltip("Layer names that should be walkable (path and build areas)")]
    public string[] walkableLayers = new string[] { "Path", "Build" };

    private NavMeshSurface navMeshSurface;

    private void Start()
    {
        if (bakeOnStart)
        {
            Invoke(nameof(BakeNavMesh), bakeDelay);
        }
    }

    /// <summary>
    /// Bakes the NavMesh for the level. Call this after level generation is complete.
    /// </summary>
    [ContextMenu("Bake NavMesh Now")]
    public void BakeNavMesh()
    {
        // Get or add NavMeshSurface component
        navMeshSurface = GetComponent<NavMeshSurface>();
        if (navMeshSurface == null)
        {
            navMeshSurface = gameObject.AddComponent<NavMeshSurface>();
        }

        // Configure NavMeshSurface
        navMeshSurface.collectObjects = CollectObjects.Children;
        navMeshSurface.useGeometry = NavMeshCollectGeometry.RenderMeshes;

        // Set up walkable layers mask
        int walkableMask = 0;
        foreach (string layerName in walkableLayers)
        {
            int layer = LayerMask.NameToLayer(layerName);
            if (layer >= 0)
            {
                walkableMask |= (1 << layer);
            }
            else
            {
                Log.Warning($"[NavMeshRuntimeBaker] Layer '{layerName}' not found. Add it to project layers.");
            }
        }
        navMeshSurface.layerMask = walkableMask;

        // Configure agent settings
        var settings = NavMesh.GetSettingsByID(0);
        if (settings.agentTypeID >= 0)
        {
            // Modify default agent settings
            navMeshSurface.agentTypeID = 0; // Use default agent type
        }

        // Bake the NavMesh
        Log.Info("[NavMeshRuntimeBaker] Baking NavMesh...");
        navMeshSurface.BuildNavMesh();

        // Verify NavMesh was created
        var triangulation = NavMesh.CalculateTriangulation();
        if (triangulation.vertices.Length > 0)
        {
            Log.Info($"[NavMeshRuntimeBaker] NavMesh baked successfully! Vertices: {triangulation.vertices.Length}, Indices: {triangulation.indices.Length}");
        }
        else
        {
            Log.Error("[NavMeshRuntimeBaker] NavMesh baking failed - no walkable surface found! Make sure tiles have colliders and are on correct layers.");
        }
    }

    /// <summary>
    /// Clears the NavMesh data
    /// </summary>
    [ContextMenu("Clear NavMesh")]
    public void ClearNavMesh()
    {
        if (navMeshSurface != null)
        {
            navMeshSurface.RemoveData();
            Log.Info("[NavMeshRuntimeBaker] NavMesh cleared.");
        }
    }

    private void OnDestroy()
    {
        // Clean up NavMesh data when object is destroyed
        if (navMeshSurface != null)
        {
            navMeshSurface.RemoveData();
        }
    }
}
