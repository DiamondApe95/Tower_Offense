using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class BuildManager : MonoBehaviour
{
    [Header("References")]
    public Camera mainCamera;
    public GameObject towerPrefab;

    [Header("Build Settings")]
    public LayerMask buildTileMask;     // Layer: BuildTile
    public float towerYOffset = 0.75f;

    // Merkt sich belegte Tiles
    private readonly HashSet<Transform> occupiedTiles = new();

    private void Awake()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
    }

    private void Update()
    {
        // Linke Maustaste (neues Input System)
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            TryBuildAtMouse();
        }
    }

    private void TryBuildAtMouse()
    {
        if (mainCamera == null)
        {
            Debug.LogError("BuildManager: Main Camera missing.");
            return;
        }

        if (towerPrefab == null)
        {
            Debug.LogError("BuildManager: Tower Prefab missing.");
            return;
        }

        if (Mouse.current == null)
            return;

        Vector2 mousePosition = Mouse.current.position.ReadValue();
        Ray ray = mainCamera.ScreenPointToRay(mousePosition);

        if (!Physics.Raycast(ray, out RaycastHit hit, 500f, buildTileMask))
            return;

        Transform tile = hit.transform;

        if (occupiedTiles.Contains(tile))
        {
            Debug.Log("BuildManager: Tile already occupied.");
            return;
        }

        Vector3 spawnPosition = tile.position + Vector3.up * towerYOffset;
        Instantiate(towerPrefab, spawnPosition, Quaternion.identity);

        occupiedTiles.Add(tile);
        Debug.Log($"BuildManager: Tower built on {tile.name}");
    }
}
