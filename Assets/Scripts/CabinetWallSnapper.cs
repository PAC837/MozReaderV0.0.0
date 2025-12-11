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
    /// - Back (-Z) of cabinet touches front (+Z) of wall
    /// - Bottom of cabinet aligns with floor + elevation (from MozCabinetData or manual)
    /// - Left edge of cabinet aligns with wall's left edge
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

        // Get elevation from MozCabinetData or manual setting
        float elevationMm = GetEffectiveElevationMm();
        float elevationM = elevationMm * 0.001f;
        _lastElevationUsedMm = elevationMm;

        // Use wall helper methods for clean positioning
        float wallFrontZ = targetWall.GetFrontFaceZ();
        float wallLeftX = targetWall.GetLeftEdgeX();
        float floorY = targetWall.GetFloorY();

        // Calculate current cabinet edge positions
        float cabinetBackZ = cabinetBounds.min.z;   // -Z face of cabinet
        float cabinetBottomY = cabinetBounds.min.y; // Bottom of cabinet
        float cabinetLeftX = cabinetBounds.min.x;   // Left edge of cabinet

        // Calculate offsets needed to snap cabinet to wall
        // For Z: cabinet back should touch wall front
        float zOffset = wallFrontZ - cabinetBackZ;

        // For Y: cabinet bottom should be at floor + elevation
        float targetBottomY = floorY + elevationM;
        float yOffset = targetBottomY - cabinetBottomY;

        // For X: cabinet left edge should align with wall left edge
        float xOffset = wallLeftX - cabinetLeftX;

        // Apply the offset to the cabinet root transform
        Vector3 newPosition = transform.position + new Vector3(xOffset, yOffset, zOffset);
        
        // Record undo for Editor
#if UNITY_EDITOR
        UnityEditor.Undo.RecordObject(transform, "Snap Cabinet to Wall");
#endif
        
        transform.position = newPosition;
        _lastSnapSuccessful = true;

        // Update MozCabinetData if present (X position only - elevation is preserved from .moz file)
        if (_cabinetData == null)
        {
            _cabinetData = GetComponent<MozCabinetData>();
        }
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
                  $"  Elevation: {elevationMm}mm ({elevationM:F4}m)\n" +
                  $"  Offset applied: ({xOffset:F4}, {yOffset:F4}, {zOffset:F4})\n" +
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
