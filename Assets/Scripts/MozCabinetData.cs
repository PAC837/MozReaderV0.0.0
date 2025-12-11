using UnityEngine;

/// <summary>
/// Component storing cabinet metadata from Mozaik .moz files.
/// Attach this to the cabinet root GameObject after import.
/// 
/// This data is used for:
/// - Positioning (elevation from floor, X position along wall)
/// - DES export (seed data for roundtrip to Mozaik)
/// - Future features (upper cabinet stacking, drag-to-adjust elevation)
/// </summary>
public class MozCabinetData : MonoBehaviour
{
    [Header("Mozaik File Data")]
    [Tooltip("Unique ID from Mozaik for export roundtrip.")]
    public int UniqueID;

    [Tooltip("Product name from .moz file.")]
    public string ProductName;

    [Tooltip("Source library name.")]
    public string SourceLibrary;

    [Header("Dimensions (mm)")]
    [Tooltip("Overall cabinet width (mm).")]
    public float WidthMm;

    [Tooltip("Overall cabinet height (mm).")]
    public float HeightMm;

    [Tooltip("Overall cabinet depth (mm).")]
    public float DepthMm;

    [Header("Positioning")]
    [Tooltip("Elevation from floor (mm). Editable to adjust cabinet height.")]
    public float ElevationMm;

    [Tooltip("X position along wall (mm). Updated after snapping.")]
    public float XPositionMm;

    [Tooltip("Wall reference for DES export (e.g., '1_1').")]
    public string WallRef;

    [Header("Runtime")]
    [Tooltip("Reference to the wall this cabinet is snapped to.")]
    public MozaikWall TargetWall;

    /// <summary>
    /// Converts elevation from mm to meters for Unity positioning.
    /// </summary>
    public float ElevationMeters => ElevationMm * 0.001f;

    /// <summary>
    /// Converts X position from mm to meters for Unity positioning.
    /// </summary>
    public float XPositionMeters => XPositionMm * 0.001f;

    /// <summary>
    /// Updates XPositionMm based on the cabinet's current world position
    /// relative to the target wall's left edge.
    /// </summary>
    public void UpdateXPositionFromWorld()
    {
        if (TargetWall == null) return;

        TargetWall.GetWorldEndpoints(out Vector3 wallStart, out Vector3 wallEnd);

        // Get cabinet bounds
        Renderer boundsRenderer = GetBoundsRenderer();
        if (boundsRenderer == null) return;

        Bounds bounds = boundsRenderer.bounds;
        float cabinetLeftX = bounds.min.x;

        // X position is distance from wall's left edge to cabinet's left edge
        // along the wall's length direction (local +X)
        Vector3 toLeftEdge = new Vector3(cabinetLeftX, wallStart.y, wallStart.z) - wallStart;
        float distanceAlongWall = Vector3.Dot(toLeftEdge, TargetWall.transform.right);

        XPositionMm = distanceAlongWall * 1000f;
    }

    /// <summary>
    /// Updates ElevationMm based on the cabinet's current world position.
    /// </summary>
    public void UpdateElevationFromWorld()
    {
        Renderer boundsRenderer = GetBoundsRenderer();
        if (boundsRenderer == null) return;

        Bounds bounds = boundsRenderer.bounds;
        // Elevation is the Y position of the cabinet's bottom
        ElevationMm = bounds.min.y * 1000f;
    }

    /// <summary>
    /// Finds the Bounds child renderer.
    /// </summary>
    private Renderer GetBoundsRenderer()
    {
        Transform boundsTransform = FindChildByName(transform, "Bounds");
        if (boundsTransform != null)
        {
            return boundsTransform.GetComponent<Renderer>();
        }
        return null;
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

#if UNITY_EDITOR
    /// <summary>
    /// Draw gizmos to visualize cabinet data in Scene view.
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        // Draw elevation line
        Renderer boundsRenderer = GetBoundsRenderer();
        if (boundsRenderer != null)
        {
            Bounds bounds = boundsRenderer.bounds;
            Vector3 bottomCenter = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
            Vector3 floorPoint = new Vector3(bounds.center.x, 0, bounds.center.z);

            Gizmos.color = Color.green;
            Gizmos.DrawLine(floorPoint, bottomCenter);

            // Draw floor reference
            Gizmos.color = new Color(0, 1, 0, 0.3f);
            Gizmos.DrawCube(floorPoint, new Vector3(0.1f, 0.01f, 0.1f));
        }
    }
#endif
}
