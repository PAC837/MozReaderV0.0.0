using UnityEngine;

/// <summary>
/// Authoring component for a single Mozaik wall in Unity.
/// Attach this to a GameObject representing one wall.
/// - Length runs along local +X
/// - Height is local +Y
/// - Thickness is local +Z
/// </summary>
[ExecuteAlways]
public class MozaikWall : MonoBehaviour
{
    [Header("Mozaik Wall Dimensions (mm)")]
    [Tooltip("Wall length along local +X axis (mm)")]
    public float lengthMm = 3000f;

    [Tooltip("Wall height (mm)")]
    public float heightMm = 2768.6f;

    [Tooltip("Wall thickness (mm)")]
    public float thicknessMm = 101.6f;

    [Header("DES Export Settings")]
    [Tooltip("Wall number for Mozaik DES export (1 = left, 2 = back, 3 = right, 4 = front)")]
    public int wallNumber = 1;

    [Tooltip("Mozaik angle in degrees (0, 90, 180, 270) - direction front face points")]
    public float mozaikAngleDegrees = 0f;

    [Header("Visual Settings")]
    public Material wallMaterial;

    private GameObject visual;

    private void OnEnable()
    {
        EnsureVisual();
        SyncVisual();
    }

    private void OnValidate()
    {
        EnsureVisual();
        SyncVisual();
    }

    private void EnsureVisual()
    {
        if (visual != null) return;

        Transform child = transform.Find("WallVisual");
        if (child != null)
        {
            visual = child.gameObject;
        }
        else
        {
            visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visual.name = "WallVisual";
            visual.transform.SetParent(transform, false);
        }

        // Ensure collider exists for wall clicking (RuntimeWallSelector needs it)
        // Make it a trigger so it doesn't interfere with physics/cabinet placement
        var col = visual.GetComponent<BoxCollider>();
        if (col == null)
        {
            col = visual.AddComponent<BoxCollider>();
        }
        col.isTrigger = true; // Trigger = clickable but doesn't block physics
    }

    /// <summary>
    /// Sync the wall visual to match mm dimensions (converted to meters).
    /// The wall transform position represents the BOTTOM-CENTER of the wall.
    /// This means placing the wall at Y=0 will have its bottom on the floor.
    /// </summary>
    public void SyncVisual()
    {
        if (visual == null) EnsureVisual();

        float lenM     = Mathf.Max(0.001f, lengthMm * 0.001f);
        float heightM  = Mathf.Max(0.001f, heightMm * 0.001f);
        float thickM   = Mathf.Max(0.001f, thicknessMm * 0.001f);

        // Offset visual upward so wall bottom aligns with transform position
        // Unity cube pivot is at center, so offset by half height
        visual.transform.localPosition = new Vector3(0, heightM * 0.5f, 0);
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale    = new Vector3(lenM, heightM, thickM);

        // Apply wall material - use assigned material or create default
        var r = visual.GetComponent<Renderer>();
        if (r != null)
        {
            Debug.Log($"[MozaikWall] SyncVisual for '{gameObject.name}' - wallMaterial={wallMaterial}");
            
            Material mat = wallMaterial;
            
            // If no material assigned, try to get default from TextureLibraryManager
            if (mat == null)
            {
                if (TextureLibraryManager.Instance != null)
                {
                    mat = TextureLibraryManager.Instance.GetDefaultWallMaterial();
                    if (mat != null)
                    {
                        Debug.Log($"[MozaikWall] Got material from TextureLibraryManager: {mat.name}");
                    }
                    else
                    {
                        Debug.Log("[MozaikWall] TextureLibraryManager returned null material");
                    }
                }
                else
                {
                    Debug.Log("[MozaikWall] TextureLibraryManager.Instance is null");
                }
            }
            else
            {
                Debug.Log($"[MozaikWall] Using assigned wallMaterial: {mat.name}");
            }
            
            // Final fallback: create basic grey URP material (works in editor and runtime)
            if (mat == null)
            {
                Debug.Log("[MozaikWall] No material found, calling CreateFallbackWallMaterial()");
                mat = CreateFallbackWallMaterial();
            }
            
            if (mat != null)
            {
                r.sharedMaterial = mat;
                Debug.Log($"[MozaikWall] Applied material '{mat.name}' with shader '{mat.shader.name}'");
            }
            else
            {
                Debug.LogError("[MozaikWall] FAILED to get or create any material! Wall will be pink.");
            }
        }
    }

    /// <summary>
    /// Creates a fallback grey wall material using available shaders.
    /// Works in both editor and play mode.
    /// </summary>
    private Material CreateFallbackWallMaterial()
    {
        Debug.Log("[MozaikWall] CreateFallbackWallMaterial() called");
        
        // Try multiple shader options with debug logging
        Shader shader = null;
        
        // Try URP Lit
        shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader != null)
        {
            Debug.Log("[MozaikWall] Found shader: URP/Lit");
        }
        
        // Fallback to URP Simple Lit
        if (shader == null)
        {
            shader = Shader.Find("Universal Render Pipeline/Simple Lit");
            if (shader != null) Debug.Log("[MozaikWall] Found shader: URP/Simple Lit");
        }
        
        // Fallback to Sprites/Default (always available!)
        if (shader == null)
        {
            shader = Shader.Find("Sprites/Default");
            if (shader != null) Debug.Log("[MozaikWall] Found shader: Sprites/Default");
        }
        
        // Fallback to Standard
        if (shader == null)
        {
            shader = Shader.Find("Standard");
            if (shader != null) Debug.Log("[MozaikWall] Found shader: Standard");
        }
        
        // Fallback to Diffuse
        if (shader == null)
        {
            shader = Shader.Find("Diffuse");
            if (shader != null) Debug.Log("[MozaikWall] Found shader: Diffuse");
        }
        
        if (shader != null)
        {
            Material mat = new Material(shader);
            mat.name = "WallMaterial_Grey";
            
            // Set color based on shader type
            if (mat.HasProperty("_Color"))
            {
                mat.SetColor("_Color", new Color(0.85f, 0.85f, 0.85f, 1f));
                Debug.Log("[MozaikWall] Set _Color property");
            }
            if (mat.HasProperty("_BaseColor"))
            {
                mat.SetColor("_BaseColor", new Color(0.85f, 0.85f, 0.85f, 1f));
                Debug.Log("[MozaikWall] Set _BaseColor property");
            }
            
            Debug.Log($"[MozaikWall] Created wall material with shader: {shader.name}");
            return mat;
        }
        
        Debug.LogError("[MozaikWall] NO SHADER FOUND! Wall will be pink.");
        return null;
    }

    /// <summary>
    /// Get the wall's start and end points in WORLD space (meters),
    /// at the bottom of the wall (Y = transform.position.y).
    /// Wall length runs along local +X.
    /// START = where X=0 products go (counter-clockwise beginning)
    /// END = where X=max products go (counter-clockwise end)
    /// </summary>
    public void GetWorldEndpoints(out Vector3 worldStart, out Vector3 worldEnd)
    {
        float lenM = lengthMm * 0.001f;
        float half = lenM * 0.5f;

        // Wall transform position is at bottom-center
        Vector3 bottomCenter = transform.position;
        Vector3 dir = transform.right; // local +X

        // START is at local +X (right side in local space)
        // END is at local -X (left side in local space)
        // This matches Mozaik's counter-clockwise convention
        worldStart = bottomCenter + dir * half;
        worldEnd = bottomCenter - dir * half;
    }

    /// <summary>
    /// Gets the Y position of the wall's bottom in world space (floor level).
    /// Since the wall transform represents bottom-center, this is just transform.position.y
    /// </summary>
    public float GetFloorY()
    {
        return transform.position.y;
    }

    /// <summary>
    /// Gets the Z position of the wall's front face (the +Z face, facing into the room).
    /// </summary>
    public float GetFrontFaceZ()
    {
        float thickM = thicknessMm * 0.001f;
        // Wall transform is at bottom-center, front face is at +Z direction from center
        return transform.position.z + (thickM * 0.5f);
    }

    /// <summary>
    /// Gets the X position of the wall's left edge (start point).
    /// </summary>
    public float GetLeftEdgeX()
    {
        float lenM = lengthMm * 0.001f;
        // Wall transform is at bottom-center, left edge is at -X direction
        return transform.position.x - (lenM * 0.5f);
    }

    /// <summary>
    /// Gets the X position of the wall's right edge (end point).
    /// </summary>
    public float GetRightEdgeX()
    {
        float lenM = lengthMm * 0.001f;
        return transform.position.x + (lenM * 0.5f);
    }

    #region Rotation-Aware Methods

    /// <summary>
    /// Gets the world position of the center of the wall's FRONT face (where products are placed).
    /// Works correctly for walls at any rotation.
    /// </summary>
    public Vector3 GetFrontFaceCenter()
    {
        float thickM = thicknessMm * 0.001f;
        float heightM = heightMm * 0.001f;

        // Wall center is at position + half height (visual is offset up)
        Vector3 wallCenter = transform.position + Vector3.up * (heightM * 0.5f);
        
        // Front face is in the local +Z direction
        return wallCenter + transform.forward * (thickM * 0.5f);
    }

    /// <summary>
    /// Gets the world position of the center of the wall's BACK face (exterior side).
    /// Works correctly for walls at any rotation.
    /// </summary>
    public Vector3 GetBackFaceCenter()
    {
        float thickM = thicknessMm * 0.001f;
        float heightM = heightMm * 0.001f;

        Vector3 wallCenter = transform.position + Vector3.up * (heightM * 0.5f);
        return wallCenter - transform.forward * (thickM * 0.5f);
    }

    /// <summary>
    /// Gets the normal direction of the wall's front face (points into room).
    /// This is the direction cabinets should face when placed on this wall.
    /// </summary>
    public Vector3 GetFrontFaceNormal()
    {
        return transform.forward;
    }

    /// <summary>
    /// Gets a world position on the wall's front face at a given distance from the wall's local left edge.
    /// Used for positioning cabinets along the wall.
    /// </summary>
    /// <param name="distanceFromLeftMm">Distance from the wall's left edge in mm</param>
    /// <param name="elevationMm">Height above floor in mm</param>
    /// <returns>World position on the front face</returns>
    public Vector3 GetFrontFacePosition(float distanceFromLeftMm, float elevationMm)
    {
        float thickM = thicknessMm * 0.001f;
        float lenM = lengthMm * 0.001f;
        float halfLen = lenM * 0.5f;
        float halfThick = thickM * 0.5f;
        
        // Convert mm to meters
        float distanceM = distanceFromLeftMm * 0.001f;
        float elevationM = elevationMm * 0.001f;

        // Start at wall's local left edge (negative X direction in local space)
        // Wall position is at bottom-center
        Vector3 leftEdge = transform.position - transform.right * halfLen;
        
        // Move along wall (positive X direction) by distance
        Vector3 alongWall = leftEdge + transform.right * distanceM;
        
        // Move up by elevation
        Vector3 withElevation = alongWall + Vector3.up * elevationM;
        
        // Move to front face (positive Z direction)
        Vector3 frontFacePos = withElevation + transform.forward * halfThick;
        
        return frontFacePos;
    }

    /// <summary>
    /// Gets the "left edge" position of the wall at floor level (world coordinates).
    /// This is the START of the wall in Mozaik convention (where X=0 products are placed).
    /// Points to local +X (right side in wall's local space).
    /// </summary>
    public Vector3 GetLeftEdgePosition()
    {
        float lenM = lengthMm * 0.001f;
        float halfLen = lenM * 0.5f;
        // START = local +X direction
        return transform.position + transform.right * halfLen;
    }

    /// <summary>
    /// Gets the "right edge" position of the wall at floor level (world coordinates).
    /// This is the END of the wall in Mozaik convention (where X=max products are placed).
    /// Points to local -X (left side in wall's local space).
    /// </summary>
    public Vector3 GetRightEdgePosition()
    {
        float lenM = lengthMm * 0.001f;
        float halfLen = lenM * 0.5f;
        // END = local -X direction
        return transform.position - transform.right * halfLen;
    }

    /// <summary>
    /// Gets the required Y rotation for a cabinet to face into the room when placed on this wall.
    /// The cabinet's back should touch the wall's front face.
    /// </summary>
    public float GetCabinetYRotation()
    {
        // Cabinet needs to face the same direction as the wall's front face normal
        // Unity's forward is +Z, so we return the Y angle of the wall's forward vector
        return transform.eulerAngles.y;
    }

    #endregion

    #region Debug Visualization

    [Header("Debug Visualization")]
    [Tooltip("Show FRONT/BACK face indicators in Scene view.")]
    public bool showFaceGizmos = true;

    /// <summary>
    /// Draw debug gizmos showing FRONT and BACK faces of the wall.
    /// FRONT (local +Z) = Blue - where products are placed (inside room)
    /// BACK (local -Z) = Red - outside/exterior
    /// </summary>
    private void OnDrawGizmos()
    {
        if (!showFaceGizmos) return;

        float lenM = lengthMm * 0.001f;
        float heightM = heightMm * 0.001f;
        float thickM = thicknessMm * 0.001f;

        // Wall center (accounting for height offset)
        Vector3 wallCenter = transform.position + Vector3.up * (heightM * 0.5f);

        // Local directions
        Vector3 forward = transform.forward;  // local +Z = FRONT
        Vector3 right = transform.right;      // local +X = length direction
        Vector3 up = transform.up;            // local +Y = height

        float halfLen = lenM * 0.5f;
        float halfThick = thickM * 0.5f;
        float halfHeight = heightM * 0.5f;

        // === FRONT FACE (local +Z) - BLUE ===
        Vector3 frontCenter = wallCenter + forward * halfThick;
        Gizmos.color = Color.blue;
        
        // Draw front face outline
        Vector3 frontBL = frontCenter - right * halfLen - up * halfHeight;
        Vector3 frontBR = frontCenter + right * halfLen - up * halfHeight;
        Vector3 frontTL = frontCenter - right * halfLen + up * halfHeight;
        Vector3 frontTR = frontCenter + right * halfLen + up * halfHeight;
        
        Gizmos.DrawLine(frontBL, frontBR);
        Gizmos.DrawLine(frontBR, frontTR);
        Gizmos.DrawLine(frontTR, frontTL);
        Gizmos.DrawLine(frontTL, frontBL);
        
        // Draw X through front face for visibility
        Gizmos.DrawLine(frontBL, frontTR);
        Gizmos.DrawLine(frontBR, frontTL);

        // Arrow pointing outward from front
        Vector3 frontArrowStart = frontCenter;
        Vector3 frontArrowEnd = frontCenter + forward * 0.3f;
        Gizmos.DrawLine(frontArrowStart, frontArrowEnd);
        DrawArrowHead(frontArrowEnd, forward, Color.blue, 0.1f);

        // === BACK FACE (local -Z) - RED ===
        Vector3 backCenter = wallCenter - forward * halfThick;
        Gizmos.color = Color.red;
        
        // Draw back face outline
        Vector3 backBL = backCenter - right * halfLen - up * halfHeight;
        Vector3 backBR = backCenter + right * halfLen - up * halfHeight;
        Vector3 backTL = backCenter - right * halfLen + up * halfHeight;
        Vector3 backTR = backCenter + right * halfLen + up * halfHeight;
        
        Gizmos.DrawLine(backBL, backBR);
        Gizmos.DrawLine(backBR, backTR);
        Gizmos.DrawLine(backTR, backTL);
        Gizmos.DrawLine(backTL, backBL);

        // Arrow pointing outward from back
        Vector3 backArrowStart = backCenter;
        Vector3 backArrowEnd = backCenter - forward * 0.3f;
        Gizmos.DrawLine(backArrowStart, backArrowEnd);
        DrawArrowHead(backArrowEnd, -forward, Color.red, 0.1f);

        // === START/END MARKERS (wall length direction) ===
        GetWorldEndpoints(out Vector3 startPos, out Vector3 endPos);
        
        // START = Yellow sphere
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(startPos, 0.08f);
        
        // END = Green sphere
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(endPos, 0.08f);

        // Draw length direction arrow (START → END)
        Gizmos.color = Color.white;
        Vector3 midBottom = (startPos + endPos) * 0.5f;
        Gizmos.DrawLine(startPos + up * 0.1f, endPos + up * 0.1f);

#if UNITY_EDITOR
        // Draw labels
        UnityEditor.Handles.color = Color.blue;
        UnityEditor.Handles.Label(frontCenter + forward * 0.35f, "FRONT\n(Products Here)");
        
        UnityEditor.Handles.color = Color.red;
        UnityEditor.Handles.Label(backCenter - forward * 0.35f, "BACK");
        
        UnityEditor.Handles.color = Color.yellow;
        UnityEditor.Handles.Label(startPos + up * 0.2f, "START");
        
        UnityEditor.Handles.color = Color.green;
        UnityEditor.Handles.Label(endPos + up * 0.2f, "END");

        // Show wall info with prominent wall number
        UnityEditor.Handles.color = Color.white;
        UnityEditor.Handles.Label(wallCenter + up * (halfHeight + 0.2f), 
            $"WALL #{wallNumber} - {gameObject.name}\n" +
            $"Mozaik Angle: {mozaikAngleDegrees}°\n" +
            $"Length: {lengthMm}mm | Height: {heightMm}mm\n" +
            $"Forward: {forward:F2}");
#endif
    }

    /// <summary>
    /// Draws an arrowhead at the specified position pointing in direction.
    /// </summary>
    private void DrawArrowHead(Vector3 position, Vector3 direction, Color color, float size)
    {
        Gizmos.color = color;
        
        // Get perpendicular vectors
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

    #endregion
}
