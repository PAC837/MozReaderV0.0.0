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
    /// </summary>
    public void SyncVisual()
    {
        if (visual == null) EnsureVisual();

        float lenM     = Mathf.Max(0.001f, lengthMm * 0.001f);
        float heightM  = Mathf.Max(0.001f, heightMm * 0.001f);
        float thickM   = Mathf.Max(0.001f, thicknessMm * 0.001f);

        visual.transform.localPosition = Vector3.zero;
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
    /// assuming pivot at center and wall length along local +X.
    /// </summary>
    public void GetWorldEndpoints(out Vector3 worldStart, out Vector3 worldEnd)
    {
        float lenM = lengthMm * 0.001f;
        float half = lenM * 0.5f;

        Vector3 center = transform.position;
        Vector3 dir    = transform.right; // local +X

        worldStart = center - dir * half;
        worldEnd   = center + dir * half;
    }
}
