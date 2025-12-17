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

    [Header("Product Type (Mozaik Classification)")]
    [Tooltip("Mozaik product type (e.g., 3=Cabinet, 8=Closet).")]
    public int ProductType = 3;

    [Tooltip("Mozaik product sub-type (e.g., 3=Tall, 21=Floor Mount Panel).")]
    public int ProductSubType = 3;

    [Tooltip("Mozaik product sub-sub-type.")]
    public int ProductSubSubType = 1;

    [Header("Construction Settings")]
    [Tooltip("Face style: 0=Faceframe, 1=Frameless, 2=Inset")]
    public int CurrentConst = 0;

    [Tooltip("16-bit flags controlling sides, options, etc. Stored as-is for roundtrip.")]
    public string Flags = "1111111111111111";

    [Header("Shape Data (XML Roundtrip)")]
    [Tooltip("TopShapeXml from .moz file - controls cabinet shape and end types. Stored as XML string for roundtrip.")]
    [TextArea(3, 6)]
    public string TopShapeXml = "";

    [Header("Parts Data (XML Roundtrip)")]
    [Tooltip("CabProdParts from .moz file - contains shelves, rods, hangers with positions. Stored as XML string for roundtrip.")]
    [TextArea(3, 8)]
    public string CabProdPartsXml = "";

    [Tooltip("ProductInterior from .moz file - contains Section layout (where shelves/rods go). Stored as XML string for roundtrip.")]
    [TextArea(3, 8)]
    public string ProductInteriorXml = "";

    [Header("Parameters Data (XML Roundtrip)")]
    [Tooltip("CabProdParms from .moz file - contains product parameters (LEDConfig, etc). Required for parametric operations.")]
    [TextArea(3, 8)]
    public string CabProdParmsXml = "";

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
    /// Uses vector projection to work correctly for walls at any angle.
    /// </summary>
    public void UpdateXPositionFromWorld()
    {
        if (TargetWall == null) return;

        TargetWall.GetWorldEndpoints(out Vector3 wallStart, out Vector3 wallEnd);

        // Get cabinet bounds
        Renderer boundsRenderer = GetBoundsRenderer();
        if (boundsRenderer == null) return;

        Bounds bounds = boundsRenderer.bounds;
        Vector3 cabinetCenter = bounds.center;

        // Determine which endpoint is the visual "left" (same logic as snapper)
        bool isXAligned = Mathf.Abs(wallEnd.x - wallStart.x) > Mathf.Abs(wallEnd.z - wallStart.z);
        Vector3 visualLeftEdgePos;
        
        if (isXAligned)
        {
            // Wall runs along X axis - visual left is higher X value
            visualLeftEdgePos = (wallStart.x < wallEnd.x) ? wallEnd : wallStart;
        }
        else
        {
            // Wall runs along Z axis - visual left is higher Z value
            visualLeftEdgePos = (wallStart.z < wallEnd.z) ? wallEnd : wallStart;
        }

        // Calculate wall direction (normalized) - from visual LEFT to visual RIGHT
        // This ensures positive distances go from left to right along the wall
        Vector3 visualRightEdgePos = (visualLeftEdgePos == wallStart) ? wallEnd : wallStart;
        Vector3 wallDirection = (visualRightEdgePos - visualLeftEdgePos).normalized;

        // Vector from VISUAL LEFT edge to cabinet center
        Vector3 visualLeftToCabinetCenter = cabinetCenter - visualLeftEdgePos;

        // Project onto wall direction to get distance to cabinet CENTER along wall
        float distanceToCenterAlongWall = Vector3.Dot(visualLeftToCabinetCenter, wallDirection);

        // Mozaik X coordinate is the LEFT EDGE position, so subtract half the cabinet width
        float halfWidthM = (WidthMm * 0.001f) * 0.5f;
        float distanceToLeftEdgeAlongWall = distanceToCenterAlongWall - halfWidthM;

        // Convert to mm
        XPositionMm = distanceToLeftEdgeAlongWall * 1000f;

        // Debug logging for position calculation
        Debug.Log($"[MozCabinetData] Position Update for '{gameObject.name}':\n" +
                  $"  Wall: {TargetWall.gameObject.name}\n" +
                  $"  Wall Start: {wallStart}\n" +
                  $"  Wall End: {wallEnd}\n" +
                  $"  Visual Left Edge: {visualLeftEdgePos}\n" +
                  $"  Visual Right Edge: {visualRightEdgePos}\n" +
                  $"  Wall Direction (L→R): {wallDirection}\n" +
                  $"  Cabinet Center: {cabinetCenter}\n" +
                  $"  Cabinet Bounds: Min={bounds.min}, Max={bounds.max}, Size={bounds.size}\n" +
                  $"  ---\n" +
                  $"  Distance to Center Along Wall: {distanceToCenterAlongWall:F4}m ({distanceToCenterAlongWall * 1000f:F1}mm)\n" +
                  $"  Half Cabinet Width: {halfWidthM:F4}m ({halfWidthM * 1000f:F1}mm)\n" +
                  $"  Distance to Left Edge Along Wall: {distanceToLeftEdgeAlongWall:F4}m ({distanceToLeftEdgeAlongWall * 1000f:F1}mm)\n" +
                  $"  ---\n" +
                  $"  RESULT: XPositionMm = {XPositionMm:F1} mm");
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

        // Draw wall distance visualization if snapped to a wall
        if (TargetWall != null && boundsRenderer != null)
        {
            TargetWall.GetWorldEndpoints(out Vector3 wallStart, out Vector3 wallEnd);
            
            Bounds bounds = boundsRenderer.bounds;
            Vector3 cabinetCenter = bounds.center;

            // Determine which endpoint is the visual "left" based on world coordinates
            // For X-aligned walls: left = lower X
            // For Z-aligned walls: left = lower Z
            Vector3 visualLeftEdge, visualRightEdge;
            bool isXAligned = Mathf.Abs(wallEnd.x - wallStart.x) > Mathf.Abs(wallEnd.z - wallStart.z);
            
            if (isXAligned)
            {
                // Wall runs along X axis - left is HIGHER X (from user's viewing angle)
                if (wallStart.x > wallEnd.x)
                {
                    visualRightEdge = wallEnd;
                    visualLeftEdge = wallStart;
                }
                else
                {
                    visualRightEdge = wallStart;
                    visualLeftEdge = wallEnd;
                }
            }
            else
            {
                // Wall runs along Z axis - left is HIGHER Z (from user's viewing angle)
                if (wallStart.z > wallEnd.z)
                {
                    visualRightEdge = wallEnd;
                    visualLeftEdge = wallStart;
                }
                else
                {
                    visualRightEdge = wallStart;
                    visualLeftEdge = wallEnd;
                }
            }

            // Calculate wall direction from visual LEFT to visual RIGHT (same as UpdateXPositionFromWorld)
            Vector3 wallDirection = (visualRightEdge - visualLeftEdge).normalized;

            // Vector from VISUAL LEFT edge to cabinet center
            Vector3 visualLeftToCabinetCenter = cabinetCenter - visualLeftEdge;

            // Project onto wall direction to get distance to cabinet CENTER along wall
            float distanceToCenterAlongWall = Vector3.Dot(visualLeftToCabinetCenter, wallDirection);

            // Mozaik X coordinate is the LEFT EDGE position, so subtract half the cabinet width
            float halfWidthM = (WidthMm * 0.001f) * 0.5f;
            float distanceToLeftEdgeAlongWall = distanceToCenterAlongWall - halfWidthM;

            // Calculate actual 3D positions for visualization (using visual left for correct display)
            Vector3 calcStartPos = visualLeftEdge;
            Vector3 cabinetLeftEdgePos = visualLeftEdge + wallDirection * distanceToLeftEdgeAlongWall;

            // Use cabinet's Y position for the line (mid-height for visibility)
            float lineY = bounds.center.y;
            calcStartPos.y = lineY;
            visualLeftEdge.y = lineY;
            visualRightEdge.y = lineY;
            cabinetLeftEdgePos.y = lineY;

            // Draw both endpoints to show wall orientation
            // Visual LEFT edge (yellow)
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(visualLeftEdge, 0.05f);
            UnityEditor.Handles.color = Color.yellow;
            UnityEditor.Handles.Label(visualLeftEdge + Vector3.up * 0.15f, "VISUAL LEFT");

            // Visual RIGHT edge (red)
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(visualRightEdge, 0.05f);
            UnityEditor.Handles.color = Color.red;
            UnityEditor.Handles.Label(visualRightEdge + Vector3.up * 0.15f, "VISUAL RIGHT");

            // Draw calculation start point if different from visual left (for debugging)
            if (Vector3.Distance(calcStartPos, visualLeftEdge) > 0.01f)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawSphere(calcStartPos, 0.04f);
                UnityEditor.Handles.color = Color.cyan;
                UnityEditor.Handles.Label(calcStartPos + Vector3.up * 0.2f, "CALC START (wallStart)");
            }

            // Draw line from calculation start to cabinet edge
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(calcStartPos, cabinetLeftEdgePos);

            // Draw marker at calculated cabinet left edge
            Gizmos.color = Color.white;
            Gizmos.DrawSphere(cabinetLeftEdgePos, 0.04f);

            // Display distance label
            float distanceMm = distanceToLeftEdgeAlongWall * 1000f;
            Vector3 labelPos = (calcStartPos + cabinetLeftEdgePos) * 0.5f + Vector3.up * 0.1f;
            
            UnityEditor.Handles.color = Color.white;
            UnityEditor.Handles.Label(labelPos, 
                $"Distance from Calc Start:\n{distanceMm:F1} mm\n(Wall: {(isXAligned ? "X-aligned" : "Z-aligned")})");
        }
    }
#endif
}
