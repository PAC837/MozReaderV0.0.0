using UnityEngine;

/// <summary>
/// Component that snaps a cabinet (imported with MozImporterBounds) to a wall.
/// Attach this to the cabinet root GameObject (e.g., "87 DH_WithBounds").
/// 
/// Snap behavior:
/// - Cabinet's back (-Z face of bounds) touches wall's front (+Z face)
/// - Cabinet's bottom aligns with wall's bottom
/// - Cabinet's left edge aligns with wall's left edge (-X)
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

    [Header("Debug Info (Read-Only)")]
    [Tooltip("The calculated bounds of this cabinet.")]
    [SerializeField] private Bounds _cabinetBounds;
    
    [Tooltip("Was the last snap operation successful?")]
    [SerializeField] private bool _lastSnapSuccessful;

    // Cached reference to the Bounds renderer
    private Renderer _boundsRenderer;

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
    /// Snaps this cabinet to the target wall.
    /// - Back (-Z) of cabinet touches front (+Z) of wall
    /// - Bottom of cabinet aligns with bottom of wall
    /// - Left edge of cabinet aligns with left edge (-X) of wall
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

        // Calculate wall position data (all in world space)
        Transform wallTransform = targetWall.transform;
        
        // Wall dimensions in meters
        float wallLengthM = targetWall.lengthMm * 0.001f;
        float wallHeightM = targetWall.heightMm * 0.001f;
        float wallThickM = targetWall.thicknessMm * 0.001f;

        // Wall pivot is at center. Calculate world positions of wall edges.
        // Wall local +X is length direction, +Y is height, +Z is thickness
        
        // Wall front face Z (the +Z face of the wall, facing into the room)
        // Wall center Z + half thickness
        Vector3 wallCenter = wallTransform.position;
        Vector3 wallForward = wallTransform.forward; // local +Z in world space
        Vector3 wallRight = wallTransform.right;     // local +X in world space
        Vector3 wallUp = wallTransform.up;           // local +Y in world space

        // Wall front face position (center of front face)
        Vector3 wallFrontCenter = wallCenter + wallForward * (wallThickM * 0.5f);
        
        // Wall left edge (-X from center)
        float wallLeftX = wallCenter.x - (wallRight.x * wallLengthM * 0.5f);
        
        // Wall bottom Y (-Y from center)
        float wallBottomY = wallCenter.y - (wallHeightM * 0.5f);

        // Calculate current cabinet edge positions
        float cabinetBackZ = cabinetBounds.min.z;   // -Z face of cabinet
        float cabinetBottomY = cabinetBounds.min.y; // Bottom of cabinet
        float cabinetLeftX = cabinetBounds.min.x;   // Left edge of cabinet

        // Calculate offsets needed to snap cabinet to wall
        // For Z: cabinet back should touch wall front
        float targetCabinetBackZ = wallFrontCenter.z;
        float zOffset = targetCabinetBackZ - cabinetBackZ;

        // For Y: cabinet bottom should align with wall bottom
        float yOffset = wallBottomY - cabinetBottomY;

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

        Debug.Log($"[CabinetWallSnapper] Snapped '{gameObject.name}' to wall '{targetWall.gameObject.name}'.\n" +
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
