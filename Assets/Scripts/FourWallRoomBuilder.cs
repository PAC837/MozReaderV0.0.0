using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Builds and manages a 4-wall room configuration matching Mozaik's DES format.
/// 
/// Wall numbering (counter-clockwise from opening/front):
/// - Wall 1: FRONT wall (Ang=180°, front faces -Z toward center, has opening)
/// - Wall 2: LEFT wall (Ang=90°, front faces +X toward center)
/// - Wall 3: BACK wall (Ang=0°, front faces +Z toward center)
/// - Wall 4: RIGHT wall (Ang=270°, front faces -X toward center)
/// 
/// Layout (top-down view, looking from above):
/// 
///                    BACK WALL (Wall 3)
///                    Ang=0°, faces inward
///     +─────────────────────────────────────────+
///     │                                         │
///     │                                         │
///     │  LEFT WALL           RIGHT WALL         │
///     │  (Wall 2)            (Wall 4)           │
///     │  Ang=90°             Ang=270°           │
///     │  faces right         faces left         │
///     │                                         │
///     │                                         │
///     +────────[ OPENING ]──────────────────────+
///                    FRONT WALL (Wall 1)
///                    Ang=180°, faces inward
/// 
/// All walls have their FRONT face pointing toward room center (inward).
/// Opening is a fixture on Wall 1 (front wall), not a gap between walls.
/// </summary>
[ExecuteAlways]
public class FourWallRoomBuilder : MonoBehaviour
{
    [Header("Room Dimensions (mm)")]
    [Tooltip("Total room width (left to right, along X axis).")]
    public float roomWidthMm = 2438.4f; // 8ft = 96 inches

    [Tooltip("Total room depth (front to back, along Z axis).")]
    public float roomDepthMm = 609.6f; // 24 inches

    [Tooltip("Height of all walls (ceiling height).")]
    public float ceilingHeightMm = 2438.4f; // 8ft = 96 inches

    [Tooltip("Thickness of all walls.")]
    public float wallThicknessMm = 152.4f; // 6 inches (matches DES example)

    [Header("Opening Settings")]
    [Tooltip("Enable an opening (doorway) in the front wall.")]
    public bool hasOpening = true;

    [Tooltip("Width of the opening in the front wall (mm).")]
    public float openingWidthMm = 1778f; // ~70 inches

    [Tooltip("Height of the opening (mm). Default is near ceiling height.")]
    public float openingHeightMm = 2235.2f; // ~88 inches

    [Tooltip("Distance from left side of front wall to opening start (mm).")]
    public float openingXPositionMm = 330.2f; // ~13 inches from left

    [Header("Wall References (Auto-populated)")]
    [SerializeField] private MozaikWall _frontWall;   // Wall 1 (has opening)
    [SerializeField] private MozaikWall _leftWall;    // Wall 2
    [SerializeField] private MozaikWall _backWall;    // Wall 3
    [SerializeField] private MozaikWall _rightWall;   // Wall 4

    [Header("Options")]
    [Tooltip("Material to apply to all walls.")]
    public Material wallMaterial;

    [Tooltip("Auto-select the left wall after building (Wall 1 is primary).")]
    public bool autoSelectLeftWall = true;

    [Header("Debug")]
    [Tooltip("Show debug gizmos for wall layout.")]
    public bool showDebugGizmos = true;

    // Constants for conversion
    private const float MM_TO_M = 0.001f;

    /// <summary>
    /// Enum for wall types in the 4-wall room.
    /// </summary>
    public enum WallType
    {
        FrontWall = 1,  // Wall 1 (has opening)
        LeftWall = 2,   // Wall 2
        BackWall = 3,   // Wall 3
        RightWall = 4   // Wall 4
    }

    #region Properties

    /// <summary>
    /// All walls in the room.
    /// </summary>
    public IReadOnlyList<MozaikWall> AllWalls
    {
        get
        {
            var walls = new List<MozaikWall>();
            if (_frontWall != null) walls.Add(_frontWall);
            if (_leftWall != null) walls.Add(_leftWall);
            if (_backWall != null) walls.Add(_backWall);
            if (_rightWall != null) walls.Add(_rightWall);
            return walls;
        }
    }

    public MozaikWall FrontWall => _frontWall;  // Wall 1 (has opening)
    public MozaikWall LeftWall => _leftWall;    // Wall 2
    public MozaikWall BackWall => _backWall;    // Wall 3
    public MozaikWall RightWall => _rightWall;  // Wall 4

    #endregion

    #region Public Methods

    /// <summary>
    /// Builds the complete 4-wall room.
    /// Destroys any existing walls first.
    /// </summary>
    [ContextMenu("Build Room")]
    public void BuildRoom()
    {
        // Clean up existing walls
        DestroyExistingWalls();

        // Convert to meters for positioning
        float widthM = roomWidthMm * MM_TO_M;
        float depthM = roomDepthMm * MM_TO_M;
        float thickM = wallThicknessMm * MM_TO_M;

        float halfWidth = widthM * 0.5f;
        float halfDepth = depthM * 0.5f;
        float halfThick = thickM * 0.5f;

        // === WALL 1: FRONT WALL (has opening) ===
        // Position: Z = +halfDepth + halfThick (front of room)
        // X = centered in width
        // Rotation: Faces -Z (toward room center)
        // Mozaik Angle: 180°
        _frontWall = CreateWall(
            "Wall 1 - Front",
            new Vector3(0, 0, halfDepth + halfThick),
            Quaternion.Euler(0, 180, 0), // Faces -Z
            roomWidthMm,                  // Length runs along X
            ceilingHeightMm,
            wallThicknessMm,
            1,   // wallNumber
            180f // mozaikAngle
        );

        // === WALL 4: RIGHT WALL ===
        // Position: X = -halfWidth - halfThick (left edge of room)
        // Z = centered in depth
        // Rotation: Faces +X (toward room center)
        // Mozaik Angle: 270°
        _rightWall = CreateWall(
            "Wall 4 - Right",
            new Vector3(-halfWidth - halfThick, 0, 0),
            Quaternion.Euler(0, 90, 0),  // Faces +X
            roomDepthMm,                  // Length runs along Z
            ceilingHeightMm,
            wallThicknessMm,
            4,   // wallNumber
            270f // mozaikAngle
        );

        // === WALL 3: BACK WALL ===
        // Position: Z = -halfDepth - halfThick (back of room)
        // X = centered in width
        // Rotation: Faces +Z (toward room center)
        // Mozaik Angle: 0°
        _backWall = CreateWall(
            "Wall 3 - Back",
            new Vector3(0, 0, -halfDepth - halfThick),
            Quaternion.Euler(0, 0, 0),   // Faces +Z
            roomWidthMm,                  // Length runs along X
            ceilingHeightMm,
            wallThicknessMm,
            3,   // wallNumber
            0f   // mozaikAngle
        );

        // === WALL 2: LEFT WALL ===
        // Position: X = +halfWidth + halfThick (right edge of room)
        // Z = centered in depth
        // Rotation: Faces -X (toward room center)
        // Mozaik Angle: 90°
        _leftWall = CreateWall(
            "Wall 2 - Left",
            new Vector3(halfWidth + halfThick, 0, 0),
            Quaternion.Euler(0, -90, 0), // Faces -X
            roomDepthMm,                  // Length runs along Z
            ceilingHeightMm,
            wallThicknessMm,
            2,   // wallNumber
            90f  // mozaikAngle
        );

        // Create U-shaped opening in front wall if enabled
        if (hasOpening && _frontWall != null)
        {
            CreateOpeningVisual(_frontWall);
        }

        // Register walls with RuntimeWallSelector if available
        RegisterWallsWithSelector();

        // Auto-select left wall (Wall 1)
        if (autoSelectLeftWall && RuntimeWallSelector.Instance != null)
        {
            RuntimeWallSelector.Instance.SelectWall(_leftWall);
        }

        Debug.Log($"[FourWallRoomBuilder] Built 4-wall room:\n" +
                  $"  Room Width: {roomWidthMm}mm\n" +
                  $"  Room Depth: {roomDepthMm}mm\n" +
                  $"  Ceiling Height: {ceilingHeightMm}mm\n" +
                  $"  Wall Thickness: {wallThicknessMm}mm\n" +
                  $"\n" +
                  $"  === WALL NUMBERING ===\n" +
                  $"  Wall 1 = FRONT wall (Ang=180°, has opening)\n" +
                  $"  Wall 2 = LEFT wall (Ang=90°)\n" +
                  $"  Wall 3 = BACK wall (Ang=0°)\n" +
                  $"  Wall 4 = RIGHT wall (Ang=270°)\n" +
                  $"\n" +
                  (hasOpening ? $"  Opening: {openingWidthMm}mm wide at X={openingXPositionMm}mm on Wall 1 (Front)" : "  No opening"));
    }

    /// <summary>
    /// Updates all wall dimensions without destroying/recreating.
    /// Call this when user changes dimension sliders.
    /// </summary>
    [ContextMenu("Update Dimensions")]
    public void UpdateRoomDimensions()
    {
        if (_leftWall == null || _backWall == null)
        {
            Debug.LogWarning("[FourWallRoomBuilder] Walls not built yet. Call BuildRoom() first.");
            BuildRoom();
            return;
        }

        // Convert to meters
        float widthM = roomWidthMm * MM_TO_M;
        float depthM = roomDepthMm * MM_TO_M;
        float thickM = wallThicknessMm * MM_TO_M;
        float halfWidth = widthM * 0.5f;
        float halfDepth = depthM * 0.5f;
        float halfThick = thickM * 0.5f;

        // Update Wall 2 - LEFT
        _leftWall.lengthMm = roomDepthMm;
        _leftWall.heightMm = ceilingHeightMm;
        _leftWall.thicknessMm = wallThicknessMm;
        _leftWall.transform.position = new Vector3(-halfWidth - halfThick, 0, 0);
        _leftWall.SyncVisual();

        // Update Wall 3 - BACK
        _backWall.lengthMm = roomWidthMm;
        _backWall.heightMm = ceilingHeightMm;
        _backWall.thicknessMm = wallThicknessMm;
        _backWall.transform.position = new Vector3(0, 0, -halfDepth - halfThick);
        _backWall.SyncVisual();

        // Update Wall 4 - RIGHT
        _rightWall.lengthMm = roomDepthMm;
        _rightWall.heightMm = ceilingHeightMm;
        _rightWall.thicknessMm = wallThicknessMm;
        _rightWall.transform.position = new Vector3(halfWidth + halfThick, 0, 0);
        _rightWall.SyncVisual();

        // Update Wall 1 - FRONT (has opening)
        _frontWall.lengthMm = roomWidthMm;
        _frontWall.heightMm = ceilingHeightMm;
        _frontWall.thicknessMm = wallThicknessMm;
        _frontWall.transform.position = new Vector3(0, 0, halfDepth + halfThick);
        _frontWall.SyncVisual();

        Debug.Log($"[FourWallRoomBuilder] Updated dimensions.");
    }

    /// <summary>
    /// Gets a specific wall by type.
    /// </summary>
    public MozaikWall GetWallByType(WallType wallType)
    {
        return wallType switch
        {
            WallType.LeftWall => _leftWall,
            WallType.BackWall => _backWall,
            WallType.RightWall => _rightWall,
            WallType.FrontWall => _frontWall,
            _ => null
        };
    }

    /// <summary>
    /// Gets a wall by its wall number (1-4).
    /// </summary>
    public MozaikWall GetWallByNumber(int wallNumber)
    {
        return wallNumber switch
        {
            1 => _frontWall,
            2 => _leftWall,
            3 => _backWall,
            4 => _rightWall,
            _ => null
        };
    }

    /// <summary>
    /// Destroys all walls in the room.
    /// </summary>
    [ContextMenu("Destroy All Walls")]
    public void DestroyExistingWalls()
    {
        DestroyWallSafe(ref _leftWall);
        DestroyWallSafe(ref _backWall);
        DestroyWallSafe(ref _rightWall);
        DestroyWallSafe(ref _frontWall);
    }

    #endregion

    #region Private Methods

    private MozaikWall CreateWall(string wallName, Vector3 position, Quaternion rotation, 
        float lengthMm, float heightMm, float thicknessMm, int wallNumber, float mozaikAngle)
    {
        GameObject wallGO = new GameObject(wallName);
        wallGO.transform.SetParent(transform);
        wallGO.transform.position = position;
        wallGO.transform.rotation = rotation;

        MozaikWall wall = wallGO.AddComponent<MozaikWall>();
        wall.lengthMm = lengthMm;
        wall.heightMm = heightMm;
        wall.thicknessMm = thicknessMm;
        wall.wallNumber = wallNumber;
        wall.mozaikAngleDegrees = mozaikAngle;

        if (wallMaterial != null)
        {
            wall.wallMaterial = wallMaterial;
        }

        wall.SyncVisual();

        return wall;
    }

    private void DestroyWallSafe(ref MozaikWall wall)
    {
        if (wall != null)
        {
            if (Application.isPlaying)
            {
                Destroy(wall.gameObject);
            }
            else
            {
                DestroyImmediate(wall.gameObject);
            }
            wall = null;
        }
    }

    private void RegisterWallsWithSelector()
    {
        if (RuntimeWallSelector.Instance != null)
        {
            RuntimeWallSelector.Instance.RefreshWallList();
        }
    }

    /// <summary>
    /// Creates a U-shaped opening visual in the front wall.
    /// Replaces the solid WallVisual cube with 3 segments: left, header, right.
    /// </summary>
    private void CreateOpeningVisual(MozaikWall wall)
    {
        if (wall == null) return;

        // Make WallVisual invisible (but keep it for highlighting)
        Transform wallVisual = wall.transform.Find("WallVisual");
        if (wallVisual != null)
        {
            Renderer wallRenderer = wallVisual.GetComponent<Renderer>();
            if (wallRenderer != null)
            {
                // Create a transparent material
                Material transparentMat = new Material(Shader.Find("Sprites/Default"));
                transparentMat.color = new Color(0, 0, 0, 0); // Fully transparent
                wallRenderer.material = transparentMat;
            }
        }
        
        // Get dimensions in meters
        float wallWidthM = roomWidthMm * MM_TO_M;
        float wallHeightM = ceilingHeightMm * MM_TO_M;
        float thickM = wallThicknessMm * MM_TO_M;

        // Add a BoxCollider to the wall GameObject itself so it can be clicked
        // even when the visual is replaced with opening segments
        BoxCollider wallCollider = wall.GetComponent<BoxCollider>();
        if (wallCollider == null)
        {
            wallCollider = wall.gameObject.AddComponent<BoxCollider>();
        }
        
        // Position collider at wall center (accounting for height offset)
        wallCollider.center = new Vector3(0, wallHeightM * 0.5f, 0);
        wallCollider.size = new Vector3(wallWidthM, wallHeightM, thickM);
        wallCollider.isTrigger = true; // Make it a trigger so it doesn't block physics
        float openingStartM = openingXPositionMm * MM_TO_M;
        float openingWidthM = openingWidthMm * MM_TO_M;
        float openingHeightM = openingHeightMm * MM_TO_M;
        float openingEndM = openingStartM + openingWidthM;
        float rightSegmentStartM = openingEndM;
        float rightSegmentWidthM = wallWidthM - openingEndM;
        float headerHeightM = wallHeightM - openingHeightM;

        // Get wall material
        Material mat = wallMaterial;
        if (mat == null && TextureLibraryManager.Instance != null)
        {
            mat = TextureLibraryManager.Instance.GetDefaultWallMaterial();
        }

        // Create container for opening segments - offset slightly forward so they render in front of WallVisual
        GameObject openingContainer = new GameObject("OpeningSegments");
        openingContainer.transform.SetParent(wall.transform);
        openingContainer.transform.localPosition = new Vector3(0, 0, thickM * 0.05f); // Offset 5% of thickness forward
        openingContainer.transform.localRotation = Quaternion.identity;

        // === LEFT SEGMENT ===
        // Position: from wall left edge to opening start
        if (openingStartM > 0.01f) // Only create if there's meaningful width
        {
            float leftWidth = openingStartM;
            float leftCenterX = -wallWidthM * 0.5f + leftWidth * 0.5f; // In wall local space
            
            GameObject leftSegment = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leftSegment.name = "LeftSegment";
            leftSegment.transform.SetParent(openingContainer.transform);
            leftSegment.transform.localPosition = new Vector3(leftCenterX, wallHeightM * 0.5f, 0);
            leftSegment.transform.localRotation = Quaternion.identity;
            leftSegment.transform.localScale = new Vector3(leftWidth, wallHeightM, thickM);
            
            var leftCol = leftSegment.GetComponent<BoxCollider>();
            if (leftCol != null) leftCol.isTrigger = true;
            
            if (mat != null)
            {
                leftSegment.GetComponent<Renderer>().sharedMaterial = mat;
            }
        }

        // === RIGHT SEGMENT ===
        // Position: from opening end to wall right edge
        if (rightSegmentWidthM > 0.01f) // Only create if there's meaningful width
        {
            float rightCenterX = wallWidthM * 0.5f - rightSegmentWidthM * 0.5f; // In wall local space
            
            GameObject rightSegment = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rightSegment.name = "RightSegment";
            rightSegment.transform.SetParent(openingContainer.transform);
            rightSegment.transform.localPosition = new Vector3(rightCenterX, wallHeightM * 0.5f, 0);
            rightSegment.transform.localRotation = Quaternion.identity;
            rightSegment.transform.localScale = new Vector3(rightSegmentWidthM, wallHeightM, thickM);
            
            var rightCol = rightSegment.GetComponent<BoxCollider>();
            if (rightCol != null) rightCol.isTrigger = true;
            
            if (mat != null)
            {
                rightSegment.GetComponent<Renderer>().sharedMaterial = mat;
            }
        }

        // === HEADER SEGMENT ===
        // Position: above the opening (from opening start to opening end, at top)
        if (headerHeightM > 0.01f) // Only create if there's a header above opening
        {
            float headerCenterX = -wallWidthM * 0.5f + openingStartM + openingWidthM * 0.5f;
            float headerCenterY = openingHeightM + headerHeightM * 0.5f;
            
            GameObject headerSegment = GameObject.CreatePrimitive(PrimitiveType.Cube);
            headerSegment.name = "HeaderSegment";
            headerSegment.transform.SetParent(openingContainer.transform);
            headerSegment.transform.localPosition = new Vector3(headerCenterX, headerCenterY, 0);
            headerSegment.transform.localRotation = Quaternion.identity;
            headerSegment.transform.localScale = new Vector3(openingWidthM, headerHeightM, thickM);
            
            var headerCol = headerSegment.GetComponent<BoxCollider>();
            if (headerCol != null) headerCol.isTrigger = true;
            
            if (mat != null)
            {
                headerSegment.GetComponent<Renderer>().sharedMaterial = mat;
            }
        }

        Debug.Log($"[FourWallRoomBuilder] Created U-shaped opening:\n" +
                  $"  Opening: {openingWidthMm}mm wide x {openingHeightMm}mm high\n" +
                  $"  Position: {openingXPositionMm}mm from left\n" +
                  $"  Left segment: {openingStartM * 1000f:F0}mm\n" +
                  $"  Right segment: {rightSegmentWidthM * 1000f:F0}mm\n" +
                  $"  Header height: {headerHeightM * 1000f:F0}mm");
    }

    #endregion

    #region Unity Lifecycle

    private void OnValidate()
    {
        // Clamp values to reasonable minimums
        roomWidthMm = Mathf.Max(500f, roomWidthMm);
        roomDepthMm = Mathf.Max(300f, roomDepthMm);
        ceilingHeightMm = Mathf.Max(1000f, ceilingHeightMm);
        wallThicknessMm = Mathf.Max(50f, wallThicknessMm);

        // Clamp opening dimensions
        openingWidthMm = Mathf.Clamp(openingWidthMm, 0f, roomWidthMm - 200f);
        openingHeightMm = Mathf.Clamp(openingHeightMm, 0f, ceilingHeightMm);
        openingXPositionMm = Mathf.Clamp(openingXPositionMm, 0f, roomWidthMm - openingWidthMm);
    }

    #endregion

    #region Debug Visualization

    private void OnDrawGizmos()
    {
        if (!showDebugGizmos) return;

        float widthM = roomWidthMm * MM_TO_M;
        float depthM = roomDepthMm * MM_TO_M;
        float heightM = ceilingHeightMm * MM_TO_M;
        float halfWidth = widthM * 0.5f;
        float halfDepth = depthM * 0.5f;

        // Draw floor outline
        Gizmos.color = Color.yellow;
        
        // Back edge
        Gizmos.DrawLine(
            new Vector3(-halfWidth, 0, -halfDepth),
            new Vector3(halfWidth, 0, -halfDepth)
        );
        
        // Left edge
        Gizmos.DrawLine(
            new Vector3(-halfWidth, 0, -halfDepth),
            new Vector3(-halfWidth, 0, halfDepth)
        );
        
        // Right edge
        Gizmos.DrawLine(
            new Vector3(halfWidth, 0, -halfDepth),
            new Vector3(halfWidth, 0, halfDepth)
        );
        
        // Front edge
        Gizmos.DrawLine(
            new Vector3(-halfWidth, 0, halfDepth),
            new Vector3(halfWidth, 0, halfDepth)
        );

        // Draw opening indicator if enabled
        if (hasOpening)
        {
            Gizmos.color = Color.green;
            float openingStartX = -halfWidth + (openingXPositionMm * MM_TO_M);
            float openingEndX = openingStartX + (openingWidthMm * MM_TO_M);
            
            Gizmos.DrawLine(
                new Vector3(openingStartX, 0.01f, halfDepth),
                new Vector3(openingEndX, 0.01f, halfDepth)
            );

#if UNITY_EDITOR
            UnityEditor.Handles.color = Color.green;
            UnityEditor.Handles.Label(
                new Vector3((openingStartX + openingEndX) * 0.5f, 0.1f, halfDepth + 0.1f),
                $"Opening: {openingWidthMm:F0}mm"
            );
#endif
        }

        // Draw wall numbers
#if UNITY_EDITOR
        UnityEditor.Handles.color = Color.cyan;
        UnityEditor.Handles.Label(new Vector3(0, 0.5f, halfDepth + 0.2f), "Wall 1\nFRONT");
        UnityEditor.Handles.Label(new Vector3(-halfWidth - 0.2f, 0.5f, 0), "Wall 4\nRIGHT");
        UnityEditor.Handles.Label(new Vector3(0, 0.5f, -halfDepth - 0.2f), "Wall 3\nBACK");
        UnityEditor.Handles.Label(new Vector3(halfWidth + 0.2f, 0.5f, 0), "Wall 2\nLEFT");
#endif
    }

    #endregion
}
