using UnityEngine;

/// <summary>
/// Component that snaps a cabinet (imported with MozImporterBounds) to a wall.
/// Attach this to the cabinet root GameObject (e.g., "87 DH_WithBounds").
/// 
/// Snap behavior:
/// - Cabinet's back (-Z face of bounds) touches wall's front (+Z face)
/// - Cabinet's bottom aligns with floor + elevation (from MozCabinetData)
/// - Cabinet's left edge aligns with wall's left edge (or next to existing cabinets)
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

    [Header("Debug Info (Read-Only)")]
    [Tooltip("The calculated bounds of this cabinet.")]
    [SerializeField] private Bounds _cabinetBounds;
    
    [Tooltip("Was the last snap operation successful?")]
    [SerializeField] private bool _lastSnapSuccessful;

    [Tooltip("The elevation used in the last snap (mm).")]
    [SerializeField] private float _lastElevationUsedMm;

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

        if (!TryGetCabinetBounds(out Bounds cabinetBounds))
        {
            return;
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

        // Update MozCabinetData if present
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
            _cabinetData.UpdateElevationFromWorld();
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

    // Draw gizmos to visualize the snap target in Scene view
    private void OnDrawGizmosSelected()
    {
        if (targetWall == null) return;

        // Draw a line from cabinet center to wall
        if (TryGetCabinetBounds(out Bounds bounds))
        {
            Gizmos.color = Color.yellow;
            Vector3 cabinetCenter = bounds.center;
            Vector3 wallCenter = targetWall.transform.position;
            Gizmos.DrawLine(cabinetCenter, wallCenter);

            // Draw cabinet bounds
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(bounds.center, bounds.size);
        }
    }
}
