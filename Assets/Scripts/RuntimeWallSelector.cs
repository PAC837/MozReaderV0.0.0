using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

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
    [Header("Startup Settings")]
    [Tooltip("Automatically create a 4-wall room on startup if no walls exist.")]
    public bool autoCreateRoom = true;

    [Tooltip("Reference to FourWallRoomBuilder (auto-found if null).")]
    public FourWallRoomBuilder roomBuilder;

    [Tooltip("Legacy: Reference to ReachInClosetBuilder (use FourWallRoomBuilder instead).")]
    [Obsolete("Use roomBuilder (FourWallRoomBuilder) instead")]
    public ReachInClosetBuilder closetBuilder;

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

    // Original material storage for selection highlighting (per wall)
    private System.Collections.Generic.Dictionary<MozaikWall, Material> _originalWallMaterials = 
        new System.Collections.Generic.Dictionary<MozaikWall, Material>();

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

        // Auto-create 4-wall room if enabled and no walls exist
        if (autoCreateRoom && _allWalls.Count == 0)
        {
            CreateFourWallRoom();
        }
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
        // CRITICAL: Don't handle wall clicks if pointer is over UI
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            if (showDebugLogs)
                Debug.Log("[RuntimeWallSelector] Click ignored - pointer is over UI.");
            return;
        }

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
            // PRIORITY CHECK: If we hit a cabinet, let MozRuntimeSelector handle it (don't select wall)
            MozBoundsHighlighter hitCabinet = hit.collider.GetComponentInParent<MozBoundsHighlighter>();
            if (hitCabinet != null)
            {
                if (showDebugLogs)
                    Debug.Log($"[RuntimeWallSelector] Hit cabinet '{hitCabinet.name}' - ignoring wall selection.");
                return; // Cabinet selection takes priority
            }

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

        // Check if wall has OpeningSegments (front wall with U-shaped opening)
        Transform openingSegments = wall.transform.Find("OpeningSegments");
        bool hasOpening = (openingSegments != null);

        // Find ALL renderers on this wall
        Renderer[] allRenderers = wall.GetComponentsInChildren<Renderer>();
        
        if (allRenderers == null || allRenderers.Length == 0)
        {
            if (showDebugLogs)
                Debug.LogWarning($"[RuntimeWallSelector] No Renderers found on {wall.name}");
            return;
        }

        // If wall has opening, exclude WallVisual from highlighting (keep opening clear)
        // Otherwise highlight all renderers (normal walls)
        Renderer[] renderersToHighlight;
        if (hasOpening)
        {
            // Only highlight the opening segments, skip WallVisual
            System.Collections.Generic.List<Renderer> segmentRenderers = new System.Collections.Generic.List<Renderer>();
            foreach (Renderer r in allRenderers)
            {
                // Skip WallVisual renderer
                if (r.transform.name != "WallVisual")
                {
                    segmentRenderers.Add(r);
                }
            }
            renderersToHighlight = segmentRenderers.ToArray();
        }
        else
        {
            // Normal wall - highlight everything
            renderersToHighlight = allRenderers;
        }

        if (highlighted)
        {
            // Store original materials
            if (!_originalWallMaterials.ContainsKey(wall))
            {
                if (renderersToHighlight.Length > 0 && renderersToHighlight[0].sharedMaterial != null)
                {
                    _originalWallMaterials[wall] = renderersToHighlight[0].sharedMaterial;
                }
            }
            
            // Create highlight material
            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            
            if (shader == null)
            {
                Debug.LogError("[RuntimeWallSelector] No shader found for highlight material!");
                return;
            }
            
            Material highlightMat = new Material(shader);
            highlightMat.name = "WallHighlight";
            
            if (highlightMat.HasProperty("_Color"))
            {
                highlightMat.SetColor("_Color", selectedWallTint);
            }
            if (highlightMat.HasProperty("_BaseColor"))
            {
                highlightMat.SetColor("_BaseColor", selectedWallTint);
            }
            
            // Apply highlight only to selected renderers
            foreach (Renderer renderer in renderersToHighlight)
            {
                renderer.material = highlightMat;
            }
            
            if (showDebugLogs)
                Debug.Log($"[RuntimeWallSelector] Applied highlight to {renderersToHighlight.Length} renderer(s) on {wall.name} (hasOpening: {hasOpening})");
        }
        else
        {
            // Restore original material
            if (_originalWallMaterials.TryGetValue(wall, out Material originalMat) && originalMat != null)
            {
                foreach (Renderer renderer in renderersToHighlight)
                {
                    renderer.material = originalMat;
                }
                _originalWallMaterials.Remove(wall);
                
                if (showDebugLogs)
                    Debug.Log($"[RuntimeWallSelector] Removed highlight from {renderersToHighlight.Length} renderer(s) on {wall.name}");
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

    /// <summary>
    /// Creates a 4-wall room using FourWallRoomBuilder.
    /// If no builder exists, one is created automatically.
    /// This is the default room type matching Mozaik's DES format.
    /// </summary>
    public void CreateFourWallRoom()
    {
        // Find or create the room builder
        if (roomBuilder == null)
        {
            roomBuilder = FindFirstObjectByType<FourWallRoomBuilder>();
        }

        if (roomBuilder == null)
        {
            // Create a new FourWallRoomBuilder
            GameObject builderGO = new GameObject("4-Wall Room");
            roomBuilder = builderGO.AddComponent<FourWallRoomBuilder>();

            // Apply default wall material if set
            if (defaultWallMaterial != null)
            {
                roomBuilder.wallMaterial = defaultWallMaterial;
            }

            if (showDebugLogs)
                Debug.Log("[RuntimeWallSelector] Created FourWallRoomBuilder.");
        }

        // Build the room (this creates all 4 walls)
        roomBuilder.BuildRoom();

        // Refresh our wall list to include the new walls
        RefreshWallList();

        // Auto-select Wall 1 (Left wall)
        if (roomBuilder.LeftWall != null)
        {
            SelectWall(roomBuilder.LeftWall);
        }

        if (showDebugLogs)
            Debug.Log($"[RuntimeWallSelector] Created 4-wall room with {_allWalls.Count} walls.");
    }

    /// <summary>
    /// Legacy: Creates a reach-in closet with 5 walls using ReachInClosetBuilder.
    /// Use CreateFourWallRoom() instead for Mozaik-compatible output.
    /// </summary>
    [Obsolete("Use CreateFourWallRoom() instead")]
    public void CreateReachInCloset()
    {
        // Find or create the closet builder
        if (closetBuilder == null)
        {
            closetBuilder = FindFirstObjectByType<ReachInClosetBuilder>();
        }

        if (closetBuilder == null)
        {
            // Create a new ReachInClosetBuilder
            GameObject builderGO = new GameObject("Reach-In Closet");
            closetBuilder = builderGO.AddComponent<ReachInClosetBuilder>();

            // Apply default wall material if set
            if (defaultWallMaterial != null)
            {
                closetBuilder.wallMaterial = defaultWallMaterial;
            }

            if (showDebugLogs)
                Debug.Log("[RuntimeWallSelector] Created ReachInClosetBuilder.");
        }

        // Build the closet (this creates all 5 walls)
        closetBuilder.BuildCloset();

        // Refresh our wall list to include the new walls
        RefreshWallList();

        if (showDebugLogs)
            Debug.Log($"[RuntimeWallSelector] Created reach-in closet with {_allWalls.Count} walls.");
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
