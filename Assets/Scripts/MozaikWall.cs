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
    /// </summary>
    public void GetWorldEndpoints(out Vector3 worldStart, out Vector3 worldEnd)
    {
        float lenM = lengthMm * 0.001f;
        float half = lenM * 0.5f;

        // Wall transform position is at bottom-center
        Vector3 bottomCenter = transform.position;
        Vector3 dir = transform.right; // local +X

        worldStart = bottomCenter - dir * half;
        worldEnd = bottomCenter + dir * half;
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
}
