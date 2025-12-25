using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using TowerConquest.Gameplay.Entities;

public class BuildManager : MonoBehaviour
{
    [Header("References")]
    public Camera mainCamera;
    public GameObject towerPrefab;
    public List<GameObject> availableTowerPrefabs = new List<GameObject>();

    [Header("Build Settings")]
    public LayerMask buildTileMask;     // Layer: BuildTile
    public LayerMask towerMask;         // Layer: Tower (für Upgrade/Verkauf)
    public float towerYOffset = 0.75f;

    [Header("Selection")]
    [Tooltip("Aktuell ausgewählter Tower-Index aus availableTowerPrefabs")]
    public int selectedTowerIndex = 0;

    [Header("Economy")]
    public int startingGold = 100;
    public int goldPerKill = 5;

    [Header("Visual Feedback")]
    public GameObject buildPreviewPrefab;
    public Material validPlacementMaterial;
    public Material invalidPlacementMaterial;

    // Events für UI-Updates
    public event Action<int> OnGoldChanged;
    public event Action<Transform, bool> OnTileHovered;
    public event Action<GameObject> OnTowerBuilt;
    public event Action<GameObject> OnTowerSold;
    public event Action<GameObject> OnTowerSelected;

    // Runtime State
    public int CurrentGold { get; private set; }
    public Transform HoveredTile { get; private set; }
    public TowerController SelectedTower { get; private set; }

    private readonly HashSet<Transform> occupiedTiles = new HashSet<Transform>();
    private readonly Dictionary<Transform, GameObject> tileToTower = new Dictionary<Transform, GameObject>();
    private GameObject currentPreview;
    private bool isBuildModeActive = true;

    private void Awake()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        CurrentGold = startingGold;

        // Falls kein spezifischer Tower ausgewählt, nutze den Standard
        if (availableTowerPrefabs.Count == 0 && towerPrefab != null)
        {
            availableTowerPrefabs.Add(towerPrefab);
        }

        // Auto-Setup Layer Masks wenn nicht gesetzt
        AutoSetupLayerMasks();
    }

    private void AutoSetupLayerMasks()
    {
        // BuildTile Layer automatisch finden
        if (buildTileMask == 0)
        {
            int buildLayer = LayerMask.NameToLayer("BuildTile");
            if (buildLayer >= 0)
            {
                buildTileMask = 1 << buildLayer;
                Debug.Log($"BuildManager: Auto-set buildTileMask to layer 'BuildTile' ({buildLayer})");
            }
            else
            {
                // Fallback: verwende Default layer
                buildTileMask = 1; // Default layer
                Debug.LogWarning("BuildManager: 'BuildTile' layer not found. Using Default layer.");
            }
        }

        // Tower Layer automatisch finden
        if (towerMask == 0)
        {
            int towerLayer = LayerMask.NameToLayer("Tower");
            if (towerLayer >= 0)
            {
                towerMask = 1 << towerLayer;
                Debug.Log($"BuildManager: Auto-set towerMask to layer 'Tower' ({towerLayer})");
            }
        }
    }

    private void Update()
    {
        UpdateHoveredTile();
        HandleInput();
    }

    private void UpdateHoveredTile()
    {
        if (mainCamera == null || Mouse.current == null)
        {
            SetHoveredTile(null);
            return;
        }

        Vector2 mousePosition = Mouse.current.position.ReadValue();
        Ray ray = mainCamera.ScreenPointToRay(mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, 500f, buildTileMask))
        {
            SetHoveredTile(hit.transform);
        }
        else
        {
            SetHoveredTile(null);
        }
    }

    private void SetHoveredTile(Transform tile)
    {
        if (HoveredTile == tile) return;

        Transform previousTile = HoveredTile;
        HoveredTile = tile;

        // Update Preview-Position
        if (currentPreview != null)
        {
            if (tile != null)
            {
                currentPreview.SetActive(true);
                currentPreview.transform.position = tile.position + Vector3.up * towerYOffset;

                // Material basierend auf Bau-Möglichkeit
                bool canBuild = CanBuildAt(tile);
                UpdatePreviewMaterial(canBuild);
            }
            else
            {
                currentPreview.SetActive(false);
            }
        }

        bool isValid = tile != null && CanBuildAt(tile);
        OnTileHovered?.Invoke(tile, isValid);
    }

    private void UpdatePreviewMaterial(bool isValid)
    {
        if (currentPreview == null) return;

        Renderer renderer = currentPreview.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material = isValid ? validPlacementMaterial : invalidPlacementMaterial;
        }
    }

    private void HandleInput()
    {
        if (Mouse.current == null) return;

        // Linke Maustaste - Bauen
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (isBuildModeActive && HoveredTile != null)
            {
                TryBuildAtTile(HoveredTile);
            }
            else
            {
                TrySelectTower();
            }
        }

        // Rechte Maustaste - Abbrechen/Deselektieren
        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            DeselectTower();
        }

        // Keyboard Shortcuts für Tower-Wechsel
        if (Keyboard.current != null)
        {
            for (int i = 0; i < Mathf.Min(9, availableTowerPrefabs.Count); i++)
            {
                Key key = Key.Digit1 + i;
                if (Keyboard.current[key].wasPressedThisFrame)
                {
                    SelectTowerType(i);
                }
            }

            // S - Verkaufen
            if (Keyboard.current.sKey.wasPressedThisFrame && SelectedTower != null)
            {
                SellSelectedTower();
            }
        }
    }

    public bool CanBuildAt(Transform tile)
    {
        if (tile == null) return false;
        if (occupiedTiles.Contains(tile)) return false;

        GameObject prefab = GetSelectedTowerPrefab();
        if (prefab == null) return false;

        int cost = GetTowerCost(prefab);
        return CurrentGold >= cost;
    }

    public void TryBuildAtTile(Transform tile)
    {
        if (!CanBuildAt(tile))
        {
            Debug.Log("BuildManager: Cannot build at this location.");
            return;
        }

        GameObject prefab = GetSelectedTowerPrefab();
        if (prefab == null)
        {
            Debug.LogError("BuildManager: No tower prefab selected.");
            return;
        }

        int cost = GetTowerCost(prefab);
        if (CurrentGold < cost)
        {
            Debug.Log($"BuildManager: Not enough gold. Need {cost}, have {CurrentGold}.");
            return;
        }

        Vector3 spawnPosition = tile.position + Vector3.up * towerYOffset;
        GameObject towerObj = Instantiate(prefab, spawnPosition, Quaternion.identity);

        occupiedTiles.Add(tile);
        tileToTower[tile] = towerObj;

        // Gold abziehen
        SpendGold(cost);

        Debug.Log($"BuildManager: Tower built on {tile.name}. Gold remaining: {CurrentGold}");
        OnTowerBuilt?.Invoke(towerObj);
    }

    public void SellTower(GameObject towerObj)
    {
        if (towerObj == null) return;

        Transform tileToRemove = null;
        foreach (var kvp in tileToTower)
        {
            if (kvp.Value == towerObj)
            {
                tileToRemove = kvp.Key;
                break;
            }
        }

        if (tileToRemove != null)
        {
            occupiedTiles.Remove(tileToRemove);
            tileToTower.Remove(tileToRemove);
        }

        int refund = GetTowerSellValue(towerObj);
        AddGold(refund);

        Debug.Log($"BuildManager: Tower sold for {refund} gold.");
        OnTowerSold?.Invoke(towerObj);

        Destroy(towerObj);

        if (SelectedTower != null && SelectedTower.gameObject == towerObj)
        {
            SelectedTower = null;
        }
    }

    public void SellSelectedTower()
    {
        if (SelectedTower != null)
        {
            SellTower(SelectedTower.gameObject);
        }
    }

    private void TrySelectTower()
    {
        if (mainCamera == null || Mouse.current == null) return;

        Vector2 mousePosition = Mouse.current.position.ReadValue();
        Ray ray = mainCamera.ScreenPointToRay(mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, 500f, towerMask))
        {
            TowerController tower = hit.collider.GetComponentInParent<TowerController>();
            if (tower != null)
            {
                SelectTower(tower);
            }
        }
    }

    public void SelectTower(TowerController tower)
    {
        SelectedTower = tower;
        isBuildModeActive = false;
        OnTowerSelected?.Invoke(tower?.gameObject);
        Debug.Log($"BuildManager: Selected tower {tower?.name}");
    }

    public void DeselectTower()
    {
        SelectedTower = null;
        isBuildModeActive = true;
        OnTowerSelected?.Invoke(null);
    }

    public void SelectTowerType(int index)
    {
        if (index >= 0 && index < availableTowerPrefabs.Count)
        {
            selectedTowerIndex = index;
            isBuildModeActive = true;
            Debug.Log($"BuildManager: Selected tower type {index}: {availableTowerPrefabs[index]?.name}");
        }
    }

    public GameObject GetSelectedTowerPrefab()
    {
        if (selectedTowerIndex >= 0 && selectedTowerIndex < availableTowerPrefabs.Count)
        {
            return availableTowerPrefabs[selectedTowerIndex];
        }

        return towerPrefab;
    }

    public int GetTowerCost(GameObject prefab)
    {
        if (prefab == null) return int.MaxValue;

        TowerController controller = prefab.GetComponent<TowerController>();
        if (controller != null)
        {
            return controller.BuildCost;
        }

        // Standard-Kosten wenn kein Controller
        return 50;
    }

    public int GetTowerSellValue(GameObject towerObj)
    {
        int cost = GetTowerCost(towerObj);
        return Mathf.RoundToInt(cost * 0.6f); // 60% Rückerstattung
    }

    public void AddGold(int amount)
    {
        CurrentGold += amount;
        OnGoldChanged?.Invoke(CurrentGold);
    }

    public void SpendGold(int amount)
    {
        CurrentGold = Mathf.Max(0, CurrentGold - amount);
        OnGoldChanged?.Invoke(CurrentGold);
    }

    public void SetBuildModeActive(bool active)
    {
        isBuildModeActive = active;
        if (currentPreview != null)
        {
            currentPreview.SetActive(active && HoveredTile != null);
        }
    }

    public void EnablePreview()
    {
        if (buildPreviewPrefab != null && currentPreview == null)
        {
            currentPreview = Instantiate(buildPreviewPrefab);
            currentPreview.SetActive(false);
        }
    }

    public void DisablePreview()
    {
        if (currentPreview != null)
        {
            Destroy(currentPreview);
            currentPreview = null;
        }
    }

    public bool IsTileOccupied(Transform tile)
    {
        return occupiedTiles.Contains(tile);
    }

    public GameObject GetTowerAtTile(Transform tile)
    {
        return tileToTower.TryGetValue(tile, out GameObject tower) ? tower : null;
    }

    public int OccupiedTileCount => occupiedTiles.Count;
}
