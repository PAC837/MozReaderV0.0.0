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

        // Remove collider so cabinets/etc can sit against it cleanly
        var col = visual.GetComponent<Collider>();
        if (col != null)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                GameObject.DestroyImmediate(col);
            else
                GameObject.Destroy(col);
#else
            GameObject.Destroy(col);
#endif
        }
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

        if (wallMaterial != null)
        {
            var r = visual.GetComponent<Renderer>();
            if (r != null)
                r.sharedMaterial = wallMaterial;
        }
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
