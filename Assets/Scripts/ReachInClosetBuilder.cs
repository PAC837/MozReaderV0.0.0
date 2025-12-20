using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Builds and manages a "Reach In" closet configuration with 5 walls:
/// - Back Wall: Main wall at the back
/// - Left Side Wall: Perpendicular to back, runs toward the opening
/// - Right Side Wall: Perpendicular to back, runs toward the opening
/// - Left Return Wall: Short wall at front-left, perpendicular to left side
/// - Right Return Wall: Short wall at front-right, perpendicular to right side
/// 
/// Layout (top-down view, looking at opening):
/// 
///     Left Return     [ Opening ]     Right Return
///     |_________|                     |_________|
///     |                                         |
///     |   Left Side                 Right Side  |
///     |                                         |
///     |_________________________________________|
///                     Back Wall
/// 
/// All walls face INWARD (toward the closet interior).
/// Opening size = Back Wall Length - (2 × Return Wall Length)
/// </summary>
[ExecuteAlways]
public class ReachInClosetBuilder : MonoBehaviour
{
    [Header("Closet Dimensions (mm)")]
    [Tooltip("Length of the back wall (total closet width).")]
    public float backWallLengthMm = 2438.4f; // 8ft = 96 inches

    [Tooltip("Depth of the side walls (closet depth from back to front).")]
    public float sideWallDepthMm = 609.6f; // 24 inches

    [Tooltip("Length of each return wall (opening reduction per side).")]
    public float returnWallLengthMm = 203.2f; // 8 inches each side = 16" total reduction

    [Tooltip("Height of all walls (ceiling height).")]
    public float ceilingHeightMm = 2438.4f; // 8ft = 96 inches

    [Tooltip("Thickness of all walls.")]
    public float wallThicknessMm = 101.6f; // 4 inches

    [Header("Wall References (Auto-populated)")]
    [SerializeField] private MozaikWall _backWall;
    [SerializeField] private MozaikWall _leftSideWall;
    [SerializeField] private MozaikWall _rightSideWall;
    [SerializeField] private MozaikWall _leftReturnWall;
    [SerializeField] private MozaikWall _rightReturnWall;

    [Header("Options")]
    [Tooltip("Material to apply to all walls.")]
    public Material wallMaterial;

    [Tooltip("Auto-select the back wall after building.")]
    public bool autoSelectBackWall = true;

    [Header("Debug")]
    [Tooltip("Show debug gizmos for wall layout.")]
    public bool showDebugGizmos = true;

    // Constants for conversion
    private const float MM_TO_M = 0.001f;

    /// <summary>
    /// Enum for wall types in the reach-in closet.
    /// </summary>
    public enum WallType
    {
        BackWall,
        LeftSideWall,
        RightSideWall,
        LeftReturnWall,
        RightReturnWall
    }

    #region Properties

    /// <summary>
    /// The calculated opening size (back wall - 2×return walls).
    /// </summary>
    public float OpeningWidthMm => backWallLengthMm - (2f * returnWallLengthMm);

    /// <summary>
    /// All walls in the closet.
    /// </summary>
    public IReadOnlyList<MozaikWall> AllWalls
    {
        get
        {
            var walls = new List<MozaikWall>();
            if (_backWall != null) walls.Add(_backWall);
            if (_leftSideWall != null) walls.Add(_leftSideWall);
            if (_rightSideWall != null) walls.Add(_rightSideWall);
            if (_leftReturnWall != null) walls.Add(_leftReturnWall);
            if (_rightReturnWall != null) walls.Add(_rightReturnWall);
            return walls;
        }
    }

    public MozaikWall BackWall => _backWall;
    public MozaikWall LeftSideWall => _leftSideWall;
    public MozaikWall RightSideWall => _rightSideWall;
    public MozaikWall LeftReturnWall => _leftReturnWall;
    public MozaikWall RightReturnWall => _rightReturnWall;

    #endregion

    #region Public Methods

    /// <summary>
    /// Builds the complete reach-in closet with 5 walls.
    /// Destroys any existing walls first.
    /// </summary>
    [ContextMenu("Build Closet")]
    public void BuildCloset()
    {
        // Clean up existing walls
        DestroyExistingWalls();

        // Convert to meters for positioning
        float backLengthM = backWallLengthMm * MM_TO_M;
        float sideDepthM = sideWallDepthMm * MM_TO_M;
        float returnLengthM = returnWallLengthMm * MM_TO_M;
        float thickM = wallThicknessMm * MM_TO_M;

        // Calculate key positions
        // Back wall is at Z=0, centered on X
        // Side walls run from back (Z=0) toward front (Z=sideDepth)
        // Return walls are at front, perpendicular to sides

        float halfBackLength = backLengthM * 0.5f;
        float halfThick = thickM * 0.5f;

        // --- 1. BACK WALL ---
        // Position: Z = -halfThick (back face at Z=0, wall extends into negative Z)
        // Rotation: Facing +Z (into the closet)
        _backWall = CreateWall(
            "Back Wall",
            new Vector3(0, 0, -halfThick),
            Quaternion.identity, // Faces +Z
            backWallLengthMm,
            ceilingHeightMm,
            wallThicknessMm
        );

        // --- 2. LEFT SIDE WALL ---
        // Position: X = -halfBackLength + halfThick, centered along Z
        // Rotation: Faces +X (into the closet, rotated 90° CW from above)
        float leftSideX = -halfBackLength + halfThick;
        float sideWallCenterZ = sideDepthM * 0.5f;
        _leftSideWall = CreateWall(
            "Left Side Wall",
            new Vector3(leftSideX, 0, sideWallCenterZ),
            Quaternion.Euler(0, 90, 0), // Faces +X (right)
            sideWallDepthMm,
            ceilingHeightMm,
            wallThicknessMm
        );

        // --- 3. RIGHT SIDE WALL ---
        // Position: X = +halfBackLength - halfThick, centered along Z
        // Rotation: Faces -X (into the closet, rotated 90° CCW from above)
        float rightSideX = halfBackLength - halfThick;
        _rightSideWall = CreateWall(
            "Right Side Wall",
            new Vector3(rightSideX, 0, sideWallCenterZ),
            Quaternion.Euler(0, -90, 0), // Faces -X (left)
            sideWallDepthMm,
            ceilingHeightMm,
            wallThicknessMm
        );

        // --- 4. LEFT RETURN WALL ---
        // Position: At front-left corner, perpendicular to left side
        // X = -halfBackLength + thickM + returnLength/2
        // Z = sideDepthM - halfThick
        float leftReturnX = -halfBackLength + thickM + (returnLengthM * 0.5f);
        float returnZ = sideDepthM - halfThick;
        _leftReturnWall = CreateWall(
            "Left Return Wall",
            new Vector3(leftReturnX, 0, returnZ),
            Quaternion.Euler(0, 180, 0), // Faces -Z (into closet from front)
            returnWallLengthMm,
            ceilingHeightMm,
            wallThicknessMm
        );

        // --- 5. RIGHT RETURN WALL ---
        // Position: At front-right corner, perpendicular to right side
        // X = +halfBackLength - thickM - returnLength/2
        // Z = sideDepthM - halfThick
        float rightReturnX = halfBackLength - thickM - (returnLengthM * 0.5f);
        _rightReturnWall = CreateWall(
            "Right Return Wall",
            new Vector3(rightReturnX, 0, returnZ),
            Quaternion.Euler(0, 180, 0), // Faces -Z (into closet from front)
            returnWallLengthMm,
            ceilingHeightMm,
            wallThicknessMm
        );

        // Register walls with RuntimeWallSelector if available
        RegisterWallsWithSelector();

        // Auto-select back wall
        if (autoSelectBackWall && RuntimeWallSelector.Instance != null)
        {
            RuntimeWallSelector.Instance.SelectWall(_backWall);
        }

        Debug.Log($"[ReachInClosetBuilder] Built reach-in closet:\n" +
                  $"  Back Wall: {backWallLengthMm}mm\n" +
                  $"  Side Depth: {sideWallDepthMm}mm\n" +
                  $"  Return Walls: {returnWallLengthMm}mm each\n" +
                  $"  Opening Width: {OpeningWidthMm}mm\n" +
                  $"  Ceiling Height: {ceilingHeightMm}mm");
    }

    /// <summary>
    /// Updates all wall dimensions without destroying/recreating.
    /// Call this when user changes dimension sliders.
    /// </summary>
    [ContextMenu("Update Dimensions")]
    public void UpdateClosetDimensions()
    {
        if (_backWall == null || _leftSideWall == null)
        {
            Debug.LogWarning("[ReachInClosetBuilder] Walls not built yet. Call BuildCloset() first.");
            BuildCloset();
            return;
        }

        // Convert to meters
        float backLengthM = backWallLengthMm * MM_TO_M;
        float sideDepthM = sideWallDepthMm * MM_TO_M;
        float returnLengthM = returnWallLengthMm * MM_TO_M;
        float thickM = wallThicknessMm * MM_TO_M;
        float halfBackLength = backLengthM * 0.5f;
        float halfThick = thickM * 0.5f;

        // Update Back Wall
        _backWall.lengthMm = backWallLengthMm;
        _backWall.heightMm = ceilingHeightMm;
        _backWall.thicknessMm = wallThicknessMm;
        _backWall.transform.position = new Vector3(0, 0, -halfThick);
        _backWall.SyncVisual();

        // Update Left Side Wall
        float leftSideX = -halfBackLength + halfThick;
        float sideWallCenterZ = sideDepthM * 0.5f;
        _leftSideWall.lengthMm = sideWallDepthMm;
        _leftSideWall.heightMm = ceilingHeightMm;
        _leftSideWall.thicknessMm = wallThicknessMm;
        _leftSideWall.transform.position = new Vector3(leftSideX, 0, sideWallCenterZ);
        _leftSideWall.SyncVisual();

        // Update Right Side Wall
        float rightSideX = halfBackLength - halfThick;
        _rightSideWall.lengthMm = sideWallDepthMm;
        _rightSideWall.heightMm = ceilingHeightMm;
        _rightSideWall.thicknessMm = wallThicknessMm;
        _rightSideWall.transform.position = new Vector3(rightSideX, 0, sideWallCenterZ);
        _rightSideWall.SyncVisual();

        // Update Left Return Wall
        float leftReturnX = -halfBackLength + thickM + (returnLengthM * 0.5f);
        float returnZ = sideDepthM - halfThick;
        _leftReturnWall.lengthMm = returnWallLengthMm;
        _leftReturnWall.heightMm = ceilingHeightMm;
        _leftReturnWall.thicknessMm = wallThicknessMm;
        _leftReturnWall.transform.position = new Vector3(leftReturnX, 0, returnZ);
        _leftReturnWall.SyncVisual();

        // Update Right Return Wall
        float rightReturnX = halfBackLength - thickM - (returnLengthM * 0.5f);
        _rightReturnWall.lengthMm = returnWallLengthMm;
        _rightReturnWall.heightMm = ceilingHeightMm;
        _rightReturnWall.thicknessMm = wallThicknessMm;
        _rightReturnWall.transform.position = new Vector3(rightReturnX, 0, returnZ);
        _rightReturnWall.SyncVisual();

        Debug.Log($"[ReachInClosetBuilder] Updated dimensions. Opening: {OpeningWidthMm}mm");
    }

    /// <summary>
    /// Gets a specific wall by type.
    /// </summary>
    public MozaikWall GetWallByType(WallType wallType)
    {
        return wallType switch
        {
            WallType.BackWall => _backWall,
            WallType.LeftSideWall => _leftSideWall,
            WallType.RightSideWall => _rightSideWall,
            WallType.LeftReturnWall => _leftReturnWall,
            WallType.RightReturnWall => _rightReturnWall,
            _ => null
        };
    }

    /// <summary>
    /// Destroys all walls in the closet.
    /// </summary>
    [ContextMenu("Destroy All Walls")]
    public void DestroyExistingWalls()
    {
        DestroyWallSafe(ref _backWall);
        DestroyWallSafe(ref _leftSideWall);
        DestroyWallSafe(ref _rightSideWall);
        DestroyWallSafe(ref _leftReturnWall);
        DestroyWallSafe(ref _rightReturnWall);
    }

    #endregion

    #region Private Methods

    private MozaikWall CreateWall(string wallName, Vector3 position, Quaternion rotation, float lengthMm, float heightMm, float thicknessMm)
    {
        GameObject wallGO = new GameObject(wallName);
        wallGO.transform.SetParent(transform);
        wallGO.transform.position = position;
        wallGO.transform.rotation = rotation;

        MozaikWall wall = wallGO.AddComponent<MozaikWall>();
        wall.lengthMm = lengthMm;
        wall.heightMm = heightMm;
        wall.thicknessMm = thicknessMm;

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

    #endregion

    #region Unity Lifecycle

    private void OnValidate()
    {
        // Clamp values to reasonable minimums
        backWallLengthMm = Mathf.Max(500f, backWallLengthMm);
        sideWallDepthMm = Mathf.Max(300f, sideWallDepthMm);
        returnWallLengthMm = Mathf.Max(0f, returnWallLengthMm);
        ceilingHeightMm = Mathf.Max(1000f, ceilingHeightMm);
        wallThicknessMm = Mathf.Max(50f, wallThicknessMm);

        // Ensure return walls don't exceed half the back wall
        float maxReturn = backWallLengthMm * 0.5f - 100f;
        returnWallLengthMm = Mathf.Min(returnWallLengthMm, maxReturn);
    }

    #endregion

    #region Debug Visualization

    private void OnDrawGizmos()
    {
        if (!showDebugGizmos) return;

        float backLengthM = backWallLengthMm * MM_TO_M;
        float sideDepthM = sideWallDepthMm * MM_TO_M;
        float returnLengthM = returnWallLengthMm * MM_TO_M;
        float heightM = ceilingHeightMm * MM_TO_M;
        float halfBack = backLengthM * 0.5f;

        // Draw floor outline
        Gizmos.color = Color.yellow;
        
        // Back edge
        Gizmos.DrawLine(
            new Vector3(-halfBack, 0, 0),
            new Vector3(halfBack, 0, 0)
        );
        
        // Left side
        Gizmos.DrawLine(
            new Vector3(-halfBack, 0, 0),
            new Vector3(-halfBack, 0, sideDepthM)
        );
        
        // Right side
        Gizmos.DrawLine(
            new Vector3(halfBack, 0, 0),
            new Vector3(halfBack, 0, sideDepthM)
        );
        
        // Left return
        Gizmos.DrawLine(
            new Vector3(-halfBack, 0, sideDepthM),
            new Vector3(-halfBack + returnLengthM, 0, sideDepthM)
        );
        
        // Right return
        Gizmos.DrawLine(
            new Vector3(halfBack, 0, sideDepthM),
            new Vector3(halfBack - returnLengthM, 0, sideDepthM)
        );

        // Draw opening indicator
        Gizmos.color = Color.green;
        float openingHalf = (OpeningWidthMm * MM_TO_M) * 0.5f;
        Gizmos.DrawLine(
            new Vector3(-openingHalf, 0.01f, sideDepthM),
            new Vector3(openingHalf, 0.01f, sideDepthM)
        );

#if UNITY_EDITOR
        // Draw opening label
        UnityEditor.Handles.color = Color.green;
        UnityEditor.Handles.Label(
            new Vector3(0, 0.1f, sideDepthM),
            $"Opening: {OpeningWidthMm:F0}mm"
        );
#endif
    }

    #endregion
}
