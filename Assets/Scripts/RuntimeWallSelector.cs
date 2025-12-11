using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Manages wall selection at runtime and provides wall creation functionality.
/// - Click on a wall to select it as the "active" wall
/// - Cabinets imported via MozImporterBounds will auto-snap to the active wall
/// - Provides methods to create new walls
/// 
/// Attach to a persistent GameObject in your scene (e.g., "Wall Manager").
/// </summary>
public class RuntimeWallSelector : MonoBehaviour
{
    [Header("Wall Settings")]
    [Tooltip("Default wall dimensions when creating new walls (mm).")]
    public float defaultWallLengthMm = 3000f;
    public float defaultWallHeightMm = 2768.6f;
    public float defaultWallThicknessMm = 101.6f;

    [Tooltip("Material to apply to new walls.")]
    public Material defaultWallMaterial;

    [Header("Selection Settings")]
    [Tooltip("Camera used for raycasting. If null, will use Camera.main.")]
    public Camera targetCamera;

    [Tooltip("Maximum raycast distance for wall selection.")]
    public float maxRaycastDistance = 1000f;

    [Tooltip("Layers to raycast against for wall selection.")]
    public LayerMask wallRaycastLayers = ~0;

    [Header("Selection Visual")]
    [Tooltip("Color to tint the selected wall.")]
    public Color selectedWallTint = new Color(0.5f, 0.8f, 1f, 1f);

    [Header("Debug")]
    public bool showDebugLogs = true;

    [Header("Runtime State (Read-Only)")]
    [SerializeField] private MozaikWall _selectedWall;
    [SerializeField] private List<MozaikWall> _allWalls = new List<MozaikWall>();
    [SerializeField] private int _wallCounter = 0;

    // Events for other systems to react to wall changes
    public event Action<MozaikWall> OnWallSelected;
    public event Action<MozaikWall> OnWallCreated;

    // Singleton-style access (optional, but convenient)
    public static RuntimeWallSelector Instance { get; private set; }

    // Input System
    private Mouse _mouse;

    // Original material storage for selection highlighting
    private Material _originalWallMaterial;

    #region Properties

    /// <summary>
    /// The currently selected wall. Cabinets will snap to this wall.
    /// </summary>
    public MozaikWall SelectedWall => _selectedWall;

    /// <summary>
    /// All walls currently in the scene.
    /// </summary>
    public IReadOnlyList<MozaikWall> AllWalls => _allWalls;

    /// <summary>
    /// Number of walls created (used for naming).
    /// </summary>
    public int WallCount => _allWalls.Count;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        // Singleton setup
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Debug.LogWarning("[RuntimeWallSelector] Multiple instances detected. Destroying duplicate.");
            Destroy(gameObject);
            return;
        }

        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }
    }

    private void Start()
    {
        _mouse = Mouse.current;

        // Find any existing walls in the scene
        RefreshWallList();
    }

    private void Update()
    {
        if (!Application.isPlaying || _mouse == null)
            return;

        // Left click to select wall
        if (_mouse.leftButton.wasPressedThisFrame)
        {
            HandleWallClick();
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    #endregion

    #region Wall Selection

    private void HandleWallClick()
    {
        if (targetCamera == null)
        {
            if (showDebugLogs)
                Debug.LogWarning("[RuntimeWallSelector] No camera available for raycasting.");
            return;
        }

        Vector2 mousePos = _mouse.position.ReadValue();
        Ray ray = targetCamera.ScreenPointToRay(mousePos);

        if (Physics.Raycast(ray, out RaycastHit hit, maxRaycastDistance, wallRaycastLayers))
        {
            // Check if we hit a wall
            MozaikWall hitWall = hit.collider.GetComponentInParent<MozaikWall>();

            if (hitWall != null)
            {
                SelectWall(hitWall);
            }
            else if (showDebugLogs)
            {
                Debug.Log($"[RuntimeWallSelector] Hit {hit.collider.name} but it's not a wall.");
            }
        }
    }

    /// <summary>
    /// Select a wall as the active wall for cabinet placement.
    /// </summary>
    public void SelectWall(MozaikWall wall)
    {
        if (wall == null)
        {
            ClearSelection();
            return;
        }

        // Deselect previous wall
        if (_selectedWall != null && _selectedWall != wall)
        {
            SetWallHighlight(_selectedWall, false);
        }

        _selectedWall = wall;
        SetWallHighlight(_selectedWall, true);

        if (showDebugLogs)
            Debug.Log($"[RuntimeWallSelector] Selected wall: {wall.name}");

        OnWallSelected?.Invoke(wall);
    }

    /// <summary>
    /// Clear the current wall selection.
    /// </summary>
    public void ClearSelection()
    {
        if (_selectedWall != null)
        {
            SetWallHighlight(_selectedWall, false);
            
            if (showDebugLogs)
                Debug.Log($"[RuntimeWallSelector] Cleared wall selection.");
            
            _selectedWall = null;
            OnWallSelected?.Invoke(null);
        }
    }

    private void SetWallHighlight(MozaikWall wall, bool highlighted)
    {
        if (wall == null) return;

        // Find the wall's visual child
        Transform visualTransform = wall.transform.Find("WallVisual");
        if (visualTransform == null) return;

        Renderer renderer = visualTransform.GetComponent<Renderer>();
        if (renderer == null) return;

        if (highlighted)
        {
            // Store original and apply tint
            _originalWallMaterial = renderer.sharedMaterial;
            
            // Create a temporary material with the tint
            Material tintedMaterial = new Material(renderer.sharedMaterial);
            tintedMaterial.color = selectedWallTint;
            renderer.material = tintedMaterial;
        }
        else
        {
            // Restore original material
            if (_originalWallMaterial != null)
            {
                renderer.sharedMaterial = _originalWallMaterial;
            }
        }
    }

    #endregion

    #region Wall Creation

    /// <summary>
    /// Creates a new wall at the specified position with default dimensions.
    /// </summary>
    public MozaikWall CreateWall(Vector3 position)
    {
        return CreateWall(position, defaultWallLengthMm, defaultWallHeightMm, defaultWallThicknessMm);
    }

    /// <summary>
    /// Creates a new wall with specified dimensions at the given position.
    /// </summary>
    public MozaikWall CreateWall(Vector3 position, float lengthMm, float heightMm, float thicknessMm)
    {
        _wallCounter++;
        string wallName = $"Wall_{_wallCounter:D2}";

        // Create the wall GameObject
        GameObject wallGO = new GameObject(wallName);
        wallGO.transform.position = position;
        wallGO.transform.rotation = Quaternion.identity;

        // Add MozaikWall component
        MozaikWall wall = wallGO.AddComponent<MozaikWall>();
        wall.lengthMm = lengthMm;
        wall.heightMm = heightMm;
        wall.thicknessMm = thicknessMm;

        if (defaultWallMaterial != null)
        {
            wall.wallMaterial = defaultWallMaterial;
        }

        // Force visual sync
        wall.SyncVisual();

        // Add collider to the wall visual for raycasting
        Transform visual = wallGO.transform.Find("WallVisual");
        if (visual != null)
        {
            BoxCollider collider = visual.gameObject.AddComponent<BoxCollider>();
            // Collider size is automatically set by Unity based on the cube's scale
        }

        // Track the wall
        _allWalls.Add(wall);

        if (showDebugLogs)
            Debug.Log($"[RuntimeWallSelector] Created wall '{wallName}' at {position}");

        OnWallCreated?.Invoke(wall);

        // Auto-select the new wall
        SelectWall(wall);

        return wall;
    }

    /// <summary>
    /// Creates a wall at origin (0, 0, 0). Called by the ADD WALL button.
    /// </summary>
    public MozaikWall CreateWallAtOrigin()
    {
        return CreateWall(Vector3.zero);
    }

    #endregion

    #region Wall List Management

    /// <summary>
    /// Refresh the list of all walls in the scene.
    /// Call this if walls are created/destroyed outside of RuntimeWallSelector.
    /// </summary>
    public void RefreshWallList()
    {
        _allWalls.Clear();
        MozaikWall[] wallsInScene = FindObjectsByType<MozaikWall>(FindObjectsSortMode.None);
        _allWalls.AddRange(wallsInScene);

        if (showDebugLogs)
            Debug.Log($"[RuntimeWallSelector] Found {_allWalls.Count} walls in scene.");

        // Update counter based on existing walls
        _wallCounter = _allWalls.Count;
    }

    /// <summary>
    /// Remove a wall from the scene.
    /// </summary>
    public void DestroyWall(MozaikWall wall)
    {
        if (wall == null) return;

        if (_selectedWall == wall)
        {
            ClearSelection();
        }

        _allWalls.Remove(wall);

        if (showDebugLogs)
            Debug.Log($"[RuntimeWallSelector] Destroyed wall: {wall.name}");

        Destroy(wall.gameObject);
    }

    #endregion

    #region Debug Visualization

    private void OnDrawGizmosSelected()
    {
        // Draw all walls' positions
        foreach (var wall in _allWalls)
        {
            if (wall == null) continue;

            Gizmos.color = (wall == _selectedWall) ? Color.green : Color.gray;
            Gizmos.DrawWireCube(wall.transform.position, Vector3.one * 0.2f);
        }

        // Draw selected wall highlight
        if (_selectedWall != null)
        {
            Gizmos.color = Color.green;
            float lenM = _selectedWall.lengthMm * 0.001f;
            float heightM = _selectedWall.heightMm * 0.001f;
            float thickM = _selectedWall.thicknessMm * 0.001f;
            
            Vector3 size = new Vector3(lenM, heightM, thickM);
            Vector3 center = _selectedWall.transform.position + Vector3.up * (heightM * 0.5f);
            
            Gizmos.DrawWireCube(center, size);
        }
    }

    #endregion
}
