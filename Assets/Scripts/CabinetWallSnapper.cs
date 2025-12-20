using UnityEngine;

/// <summary>
/// Component that snaps a cabinet (imported with MozImporterBounds) to a wall.
/// Attach this to the cabinet root GameObject (e.g., "87 DH_WithBounds").
/// 
/// Snap behavior:
/// - Cabinet's back (-Z face of bounds) touches wall's front (+Z face)
/// - Cabinet's bottom aligns with floor + elevation (from MozCabinetData)
/// - Cabinet's left edge aligns with wall's left edge (or next to existing cabinets)
/// - Cabinet is rotated to face the room (front toward +Z, back toward wall)
/// 
/// Usage:
/// 1. Attach this component to your cabinet root
/// 2. Assign the target MozaikWall in the Inspector
/// 3. Click "Snap to Wall" button in the Inspector
/// </summary>
[ExecuteAlways]
public class CabinetWallSnapper : MonoBehaviour
{
    [Header("Wall Reference")]
    [Tooltip("The wall to snap this cabinet to.")]
    public MozaikWall targetWall;

    [Header("Snap Settings")]
    [Tooltip("Use elevation from MozCabinetData if available.")]
    public bool useElevationFromData = true;

    [Tooltip("Manual elevation override (mm). Used if no MozCabinetData or useElevationFromData is false.")]
    public float manualElevationMm = 0f;

    [Tooltip("Automatically rotate cabinet to face the room when snapping.")]
    public bool autoRotateToFaceRoom = true;

    [Header("Debug Visualization")]
    [Tooltip("Show orientation arrows in Scene view (front=blue, back=red, left=green, right=yellow).")]
    public bool showOrientationGizmos = true;

    [Tooltip("Length of debug arrows in meters.")]
    public float gizmoArrowLength = 0.3f;

    [Tooltip("Print detailed orientation info to console when snapping.")]
    public bool debugLogOrientation = true;

    [Header("Debug Info (Read-Only)")]
    [Tooltip("The calculated bounds of this cabinet.")]
    [SerializeField] private Bounds _cabinetBounds;
    
    [Tooltip("Was the last snap operation successful?")]
    [SerializeField] private bool _lastSnapSuccessful;

    [Tooltip("The elevation used in the last snap (mm).")]
    [SerializeField] private float _lastElevationUsedMm;

    [Tooltip("Cabinet's current forward direction (local +Z in world space).")]
    [SerializeField] private Vector3 _currentForward;

    [Tooltip("Cabinet's current Y rotation in degrees.")]
    [SerializeField] private float _currentYRotation;

    // Cached references
    private Renderer _boundsRenderer;
    private MozCabinetData _cabinetData;

    /// <summary>
    /// Finds the Bounds child and returns its world-space bounds.
    /// Returns true if bounds were found, false otherwise.
    /// </summary>
    public bool TryGetCabinetBounds(out Bounds worldBounds)
    {
        worldBounds = new Bounds();

        // Find the Bounds child if not cached
        if (_boundsRenderer == null)
        {
            Transform boundsTransform = FindChildByName(transform, "Bounds");
            if (boundsTransform != null)
            {
                _boundsRenderer = boundsTransform.GetComponent<Renderer>();
            }
        }

        if (_boundsRenderer == null)
        {
            Debug.LogError($"[CabinetWallSnapper] No 'Bounds' child found in '{gameObject.name}'. " +
                           "Make sure this cabinet was imported with MozImporterBounds.");
            return false;
        }

        // Get world-space bounds from the renderer
        worldBounds = _boundsRenderer.bounds;
        _cabinetBounds = worldBounds;
        return true;
    }

    /// <summary>
    /// Gets the elevation to use for snapping (in mm).
    /// Checks MozCabinetData first if useElevationFromData is true.
    /// </summary>
    public float GetEffectiveElevationMm()
    {
        if (useElevationFromData)
        {
            // Try to get from MozCabinetData
            if (_cabinetData == null)
            {
                _cabinetData = GetComponent<MozCabinetData>();
            }

            if (_cabinetData != null)
            {
                return _cabinetData.ElevationMm;
            }
        }

        return manualElevationMm;
    }

    /// <summary>
    /// Snaps this cabinet to the target wall.
    /// - Rotates cabinet to face room (if autoRotateToFaceRoom is enabled)
    /// - Back of cabinet touches wall's front face
    /// - Bottom of cabinet aligns with floor + elevation (from MozCabinetData or manual)
    /// - Cabinet positioned along wall based on XPositionMm
    /// 
    /// ROTATION-AWARE: Works correctly for walls at any angle (0°, 90°, 180°, 270°, etc.)
    /// </summary>
    public void SnapToWall()
    {
        _lastSnapSuccessful = false;

        if (targetWall == null)
        {
            Debug.LogError("[CabinetWallSnapper] No target wall assigned.");
            return;
        }

        // STEP 1: Rotate cabinet to face the room FIRST (before getting bounds)
        // This ensures the bounds are calculated with correct orientation
        if (autoRotateToFaceRoom)
        {
            RotateToFaceRoom();
        }

        if (!TryGetCabinetBounds(out Bounds cabinetBounds))
        {
            return;
        }

        // Log orientation before snap if debug enabled
        if (debugLogOrientation)
        {
            LogOrientationDebugInfo();
        }

        // Get cabinet data if available
        if (_cabinetData == null)
        {
            _cabinetData = GetComponent<MozCabinetData>();
        }

        // Get elevation from MozCabinetData or manual setting
        float elevationMm = GetEffectiveElevationMm();
        float elevationM = elevationMm * 0.001f;
        _lastElevationUsedMm = elevationMm;

        // === ROTATION-AWARE POSITIONING ===
        // Use wall's local coordinate system instead of world X/Y/Z

        // Get wall directions
        Vector3 wallForward = targetWall.GetFrontFaceNormal(); // Direction into room
        Vector3 wallRight = targetWall.transform.right;        // Direction along wall length (local +X)
        
        // Get cabinet dimensions in WALL-RELATIVE space (not world AABB!)
        // After rotation, bounds.size is axis-aligned to WORLD, not the cabinet's local axes.
        // Cabinet width runs along the WALL (wallRight direction), depth runs perpendicular (wallForward)
        Vector3 boundsSize = cabinetBounds.size;
        
        // Calculate cabinet dimensions relative to wall orientation:
        // - Width = extent along wallRight direction
        // - Depth = extent along wallForward direction  
        // For axis-aligned bounds, we need to figure out which world axis aligns with which wall direction
        float cabinetWidthM, cabinetDepthM;
        
        // Determine which bounds dimension corresponds to wall width/depth
        // wallRight is the direction along the wall (cabinet width runs this way)
        // wallForward is perpendicular to wall (cabinet depth runs this way)
        float wallRightDotX = Mathf.Abs(Vector3.Dot(wallRight, Vector3.right));
        float wallRightDotZ = Mathf.Abs(Vector3.Dot(wallRight, Vector3.forward));
        
        if (wallRightDotX > wallRightDotZ)
        {
            // Wall runs mostly along world X, so cabinet width = bounds X, depth = bounds Z
            cabinetWidthM = boundsSize.x;
            cabinetDepthM = boundsSize.z;
        }
        else
        {
            // Wall runs mostly along world Z, so cabinet width = bounds Z, depth = bounds X
            cabinetWidthM = boundsSize.z;
            cabinetDepthM = boundsSize.x;
        }
        
        float cabinetHeightM = boundsSize.y;  // Height is always Y
        float halfCabinetWidthM = cabinetWidthM * 0.5f;
        float halfCabinetDepthM = cabinetDepthM * 0.5f;
        
        // Get floor Y from wall
        float floorY = targetWall.GetFloorY();

        // Determine XPositionMm (distance from wall's local left edge)
        float xPositionMm;
        if (_cabinetData != null && _cabinetData.XPositionMm > 0)
        {
            // Smart placement has set a specific position - use it
            xPositionMm = _cabinetData.XPositionMm;
            Debug.Log($"[CabinetWallSnapper] Using smart placement position: {xPositionMm}mm");
        }
        else
        {
            // No specific position set - default to 0 (wall left edge)
            xPositionMm = 0f;
            Debug.Log($"[CabinetWallSnapper] No position set - defaulting to wall left edge (0mm)");
        }

        // Calculate target position on wall's front face
        // XPositionMm is measured to the cabinet's LEFT EDGE, so add half width to get center
        float xPositionM = xPositionMm * 0.001f;
        float cabinetCenterDistanceM = xPositionM + halfCabinetWidthM;

        // Get wall's left edge position (START position in Mozaik convention)
        Vector3 wallLeftEdge = targetWall.GetLeftEdgePosition();

        // Calculate cabinet center position:
        // 1. Start at wall left edge (START)
        // 2. Move along wall in NEGATIVE wallRight direction (toward END)
        // 3. Move up for elevation + half cabinet height
        // 4. Move outward from wall (wallForward) by half cabinet depth (so back touches wall)
        
        Vector3 targetPosition = wallLeftEdge
            - wallRight * cabinetCenterDistanceM           // Along wall (negative = toward END)
            + Vector3.up * (floorY + elevationM + cabinetHeightM * 0.5f - cabinetBounds.center.y + transform.position.y) // Vertical (adjusted for bounds offset)
            + wallForward * halfCabinetDepthM;             // Out from wall

        // Calculate the offset from current bounds center to target position
        // We need to account for the fact that bounds.center may not equal transform.position
        Vector3 boundsOffset = cabinetBounds.center - transform.position;
        
        // Adjust: We want the cabinet's back face touching the wall's front face
        // So we position based on where the cabinet CENTER should be
        Vector3 cabinetTargetCenter = wallLeftEdge
            - wallRight * cabinetCenterDistanceM  // Negative = toward END
            + Vector3.up * (floorY + elevationM + cabinetHeightM * 0.5f)
            + wallForward * halfCabinetDepthM;

        // Calculate offset needed
        Vector3 offset = cabinetTargetCenter - cabinetBounds.center;

        // Record undo for Editor
#if UNITY_EDITOR
        UnityEditor.Undo.RecordObject(transform, "Snap Cabinet to Wall");
#endif
        
        Vector3 newPosition = transform.position + offset;
        transform.position = newPosition;
        _lastSnapSuccessful = true;

        // Update MozCabinetData if present
        if (_cabinetData != null)
        {
#if UNITY_EDITOR
            UnityEditor.Undo.RecordObject(_cabinetData, "Update Cabinet Position Data");
#endif
            _cabinetData.TargetWall = targetWall;
            _cabinetData.UpdateXPositionFromWorld();
            // NOTE: Don't call UpdateElevationFromWorld() here - it would overwrite the original
            // elevation from the .moz file. The elevation should only be updated when user
            // explicitly changes the cabinet height (future drag-to-adjust feature).
        }

        Debug.Log($"[CabinetWallSnapper] Snapped '{gameObject.name}' to wall '{targetWall.gameObject.name}'.\n" +
                  $"  Wall forward: {wallForward}\n" +
                  $"  Wall rotation: {targetWall.transform.eulerAngles.y}°\n" +
                  $"  XPositionMm: {xPositionMm}mm\n" +
                  $"  Elevation: {elevationMm}mm ({elevationM:F4}m)\n" +
                  $"  Offset applied: {offset}\n" +
                  $"  New position: {newPosition}");
    }

    /// <summary>
    /// Helper to find a child by name recursively.
    /// </summary>
    private Transform FindChildByName(Transform root, string name)
    {
        foreach (Transform t in root.GetComponentsInChildren<Transform>())
        {
            if (t.name == name)
                return t;
        }
        return null;
    }

    /// <summary>
    /// Logs detailed orientation info to the console for debugging.
    /// Call this from the Inspector button or via context menu.
    /// </summary>
    [ContextMenu("Log Orientation Debug Info")]
    public void LogOrientationDebugInfo()
    {
        if (!TryGetCabinetBounds(out Bounds bounds))
        {
            Debug.LogWarning("[CabinetWallSnapper] Cannot log orientation - no bounds found.");
            return;
        }

        // Update debug fields
        _currentForward = transform.forward;
        _currentYRotation = transform.eulerAngles.y;

        // Calculate cabinet face positions
        Vector3 center = bounds.center;
        float halfDepth = bounds.size.z * 0.5f;
        
        Vector3 frontFaceCenter = center + Vector3.forward * halfDepth;  // +Z world
        Vector3 backFaceCenter = center + Vector3.back * halfDepth;      // -Z world

        Debug.Log($"[CabinetWallSnapper] Orientation Debug for '{gameObject.name}':\n" +
                  $"  Transform Position: {transform.position}\n" +
                  $"  Transform Rotation: {transform.eulerAngles} (Y={_currentYRotation:F1}°)\n" +
                  $"  Transform Forward: {_currentForward}\n" +
                  $"  ---\n" +
                  $"  Bounds Center: {center}\n" +
                  $"  Bounds Size: {bounds.size}\n" +
                  $"  Bounds Min (back/bottom/left): {bounds.min}\n" +
                  $"  Bounds Max (front/top/right): {bounds.max}\n" +
                  $"  ---\n" +
                  $"  Front Face Center (+Z): {frontFaceCenter}\n" +
                  $"  Back Face Center (-Z): {backFaceCenter}\n" +
                  (targetWall != null ? $"  ---\n  Wall Front Face Z: {targetWall.GetFrontFaceZ()}\n" : ""));
    }

    /// <summary>
    /// Rotates the cabinet so its front faces the room (away from wall).
    /// For a standard wall (facing +Z), the cabinet should have back toward -Z.
    /// </summary>
    [ContextMenu("Rotate to Face Room")]
    public void RotateToFaceRoom()
    {
        if (targetWall == null)
        {
            Debug.LogWarning("[CabinetWallSnapper] No target wall assigned. Cabinet will face +Z (world forward).");
        }

#if UNITY_EDITOR
        UnityEditor.Undo.RecordObject(transform, "Rotate Cabinet to Face Room");
#endif

        // For a wall along X axis (standard), cabinet should face +Z (into room)
        // This means cabinet local -Z (back) faces the wall
        // Simple solution: Set rotation to identity (facing +Z)
        // TODO: Support walls at different angles
        
        Quaternion targetRotation = Quaternion.identity; // Faces +Z

        if (targetWall != null)
        {
            // Get wall's forward direction (wall front faces +Z by default)
            Vector3 wallForward = targetWall.transform.forward;
            // Cabinet should face the same direction as wall (into room)
            targetRotation = Quaternion.LookRotation(wallForward, Vector3.up);
        }

        transform.rotation = targetRotation;

        // Update debug fields
        _currentForward = transform.forward;
        _currentYRotation = transform.eulerAngles.y;

        if (debugLogOrientation)
        {
            Debug.Log($"[CabinetWallSnapper] Rotated '{gameObject.name}' to face room.\n" +
                      $"  New rotation: {transform.eulerAngles}\n" +
                      $"  Forward direction: {transform.forward}");
        }
    }

    // Draw gizmos to visualize the snap target and orientation in Scene view
    private void OnDrawGizmosSelected()
    {
        // Update debug fields for inspector display
        _currentForward = transform.forward;
        _currentYRotation = transform.eulerAngles.y;

        if (!TryGetCabinetBounds(out Bounds bounds))
            return;

        Vector3 center = bounds.center;

        // Draw cabinet bounds
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(center, bounds.size);

        // Draw cabinet LEFT and RIGHT edge markers for debugging
        Vector3 cabinetLeftEdge = new Vector3(bounds.max.x, center.y, center.z);
        Vector3 cabinetRightEdge = new Vector3(bounds.min.x, center.y, center.z);
        
        // Yellow = LEFT edge (what we think is left)
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(cabinetLeftEdge, 0.1f);
#if UNITY_EDITOR
        UnityEditor.Handles.color = Color.yellow;
        UnityEditor.Handles.Label(cabinetLeftEdge + Vector3.up * 0.2f, $"CAB LEFT\n(max.x={bounds.max.x:F2})");
#endif

        // Red = RIGHT edge (what we think is right)
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(cabinetRightEdge, 0.1f);
#if UNITY_EDITOR
        UnityEditor.Handles.color = Color.red;
        UnityEditor.Handles.Label(cabinetRightEdge + Vector3.up * 0.2f, $"CAB RIGHT\n(min.x={bounds.min.x:F2})");
#endif

        // Draw orientation arrows if enabled
        if (showOrientationGizmos)
        {
            float arrowLen = gizmoArrowLength;
            
            // FRONT (+Z) - Blue arrow (door side, faces into room)
            Gizmos.color = Color.blue;
            Vector3 frontDir = Vector3.forward;
            Vector3 frontStart = center;
            Vector3 frontEnd = frontStart + frontDir * arrowLen;
            Gizmos.DrawLine(frontStart, frontEnd);
            DrawArrowHead(frontEnd, frontDir, Color.blue, arrowLen * 0.2f);

            // BACK (-Z) - Red arrow (against wall)
            Gizmos.color = Color.red;
            Vector3 backDir = Vector3.back;
            Vector3 backStart = center;
            Vector3 backEnd = backStart + backDir * arrowLen;
            Gizmos.DrawLine(backStart, backEnd);
            DrawArrowHead(backEnd, backDir, Color.red, arrowLen * 0.2f);

            // LEFT (-X) - Green arrow
            Gizmos.color = Color.green;
            Vector3 leftDir = Vector3.left;
            Vector3 leftStart = center;
            Vector3 leftEnd = leftStart + leftDir * arrowLen * 0.5f;
            Gizmos.DrawLine(leftStart, leftEnd);

            // RIGHT (+X) - Yellow arrow
            Gizmos.color = Color.yellow;
            Vector3 rightDir = Vector3.right;
            Vector3 rightStart = center;
            Vector3 rightEnd = rightStart + rightDir * arrowLen * 0.5f;
            Gizmos.DrawLine(rightStart, rightEnd);

            // Draw face labels using handles (editor only)
#if UNITY_EDITOR
            UnityEditor.Handles.color = Color.blue;
            UnityEditor.Handles.Label(frontEnd + Vector3.up * 0.05f, "FRONT (+Z)");
            
            UnityEditor.Handles.color = Color.red;
            UnityEditor.Handles.Label(backEnd + Vector3.up * 0.05f, "BACK (-Z)");
#endif
        }

        // Draw line to wall if assigned
        if (targetWall != null)
        {
            Gizmos.color = Color.yellow;
            Vector3 wallCenter = targetWall.transform.position;
            Gizmos.DrawLine(center, wallCenter);
        }
    }

    /// <summary>
    /// Draws an arrowhead at the specified position.
    /// </summary>
    private void DrawArrowHead(Vector3 position, Vector3 direction, Color color, float size)
    {
        Gizmos.color = color;
        
        // Simple arrowhead using perpendicular lines
        Vector3 right = Vector3.Cross(direction, Vector3.up).normalized;
        if (right.magnitude < 0.001f)
            right = Vector3.Cross(direction, Vector3.forward).normalized;
        
        Vector3 up = Vector3.Cross(right, direction).normalized;
        
        Vector3 arrowBack = position - direction * size;
        
        Gizmos.DrawLine(position, arrowBack + right * size * 0.5f);
        Gizmos.DrawLine(position, arrowBack - right * size * 0.5f);
        Gizmos.DrawLine(position, arrowBack + up * size * 0.5f);
        Gizmos.DrawLine(position, arrowBack - up * size * 0.5f);
    }
}
