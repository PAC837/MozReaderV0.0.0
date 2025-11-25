using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Resizes a cabinet imported by MozImporterBounds by "pulling" the bounding
/// box corners. Creates a transparent box with a green outline and big red
/// corner handles. Moving the red handles updates:
///   - The Bounds cube
///   - All parts' positions and scales
/// Full-width items (rods, toe, top, f. shelf, bottom) stretch with width.
/// Thickness (local Y) is kept constant.
/// </summary>
[ExecuteAlways]
public class CabinetBoundsResizer : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Root transform of the imported cabinet. Defaults to this.transform if null.")]
    public Transform cabinetRoot;

    [Tooltip("The 'Bounds' cube created by MozImporterBounds.")]
    public Transform boundsCube;

    [Header("Visuals")]
    [Tooltip("Material for the red corner handles.")]
    public Material cornerMaterial;

    [Tooltip("Material for the green outline.")]
    public Material outlineMaterial;

    [Header("Behavior")]
    [Tooltip("Automatically recalc and rescale every frame based on corner positions.")]
    public bool autoUpdate = true;

    [Tooltip("Size of the red corner handle spheres.")]
    public float handleSize = 0.05f;

    [Tooltip("Width of the green outline lines.")]
    public float outlineWidth = 0.01f;

    // Original bounds, in cabinetRoot local space
    private Vector3 _originalBoundsCenterLocal;
    private Vector3 _originalBoundsSizeLocal;

    private LineRenderer _outlineRenderer;
    private readonly List<Transform> _handles = new List<Transform>();

    private bool _initialized;

    private class PartInfo
    {
        public Transform t;
        public Vector3 originalLocalPos;
        public Vector3 originalLocalScale;
        public bool stretchWidth;
    }

    private readonly List<PartInfo> _parts = new List<PartInfo>();

    private void OnEnable()
    {
        TryInitialize();
    }

    private void Update()
    {
        if (!_initialized)
            TryInitialize();

        if (!_initialized || !autoUpdate)
            return;

        if (_handles.Count != 8 || boundsCube == null || cabinetRoot == null)
            return;

        UpdateBoundsFromHandles();
    }

    private void TryInitialize()
    {
        if (_initialized)
            return;

        if (cabinetRoot == null)
            cabinetRoot = transform;

        if (boundsCube == null)
        {
            boundsCube = FindChildByName(cabinetRoot, "Bounds");
            if (boundsCube == null)
            {
                Debug.LogWarning("[CabinetBoundsResizer] No Bounds cube assigned and none named 'Bounds' found.");
                return;
            }
        }

        CacheOriginalBounds();
        CacheParts();
        SetupVisuals();

        _initialized = true;
    }

    private void CacheOriginalBounds()
    {
        _originalBoundsCenterLocal = boundsCube.localPosition;
        _originalBoundsSizeLocal = boundsCube.localScale;

        if (_originalBoundsSizeLocal.x <= 0f || _originalBoundsSizeLocal.y <= 0f || _originalBoundsSizeLocal.z <= 0f)
        {
            Debug.LogWarning("[CabinetBoundsResizer] Original bounds size has non-positive component(s).");
        }
    }

    private void CacheParts()
    {
        _parts.Clear();

        foreach (Transform t in cabinetRoot.GetComponentsInChildren<Transform>())
        {
            if (t == cabinetRoot)
                continue;

            if (t == boundsCube)
                continue;

            if (t.parent == boundsCube)
                continue;

            if (t.GetComponent<Renderer>() == null)
                continue;

            PartInfo info = new PartInfo
            {
                t = t,
                originalLocalPos = t.localPosition,
                originalLocalScale = t.localScale,
                stretchWidth = IsFullWidthName(t.name)
            };

            _parts.Add(info);
        }

        Debug.Log($"[CabinetBoundsResizer] Cached {_parts.Count} parts for resizing.");
    }

    private void SetupVisuals()
    {
        // Hide the solid bounds cube
        MeshRenderer mr = boundsCube.GetComponent<MeshRenderer>();
        if (mr != null)
            mr.enabled = false;

        Collider col = boundsCube.GetComponent<Collider>();
        if (col != null)
            col.enabled = false;

        CreateOutline();
        CreateCornerHandles();
    }

    private void CreateOutline()
    {
        if (_outlineRenderer != null)
        {
            if (Application.isPlaying)
                Destroy(_outlineRenderer.gameObject);
            else
                DestroyImmediate(_outlineRenderer.gameObject);
        }

        GameObject outlineGO = new GameObject("BoundsOutline");
        outlineGO.transform.SetParent(boundsCube, false);
        outlineGO.transform.localPosition = Vector3.zero;
        outlineGO.transform.localRotation = Quaternion.identity;

        _outlineRenderer = outlineGO.AddComponent<LineRenderer>();
        _outlineRenderer.useWorldSpace = false;
        _outlineRenderer.loop = false;
        _outlineRenderer.widthMultiplier = outlineWidth;

        if (outlineMaterial != null)
            _outlineRenderer.sharedMaterial = outlineMaterial;

        Vector3 ext = boundsCube.localScale * 0.5f;

        Vector3 c000 = new Vector3(-ext.x, -ext.y, -ext.z);
        Vector3 c100 = new Vector3(ext.x, -ext.y, -ext.z);
        Vector3 c110 = new Vector3(ext.x, ext.y, -ext.z);
        Vector3 c010 = new Vector3(-ext.x, ext.y, -ext.z);
        Vector3 c001 = new Vector3(-ext.x, -ext.y, ext.z);
        Vector3 c101 = new Vector3(ext.x, -ext.y, ext.z);
        Vector3 c111 = new Vector3(ext.x, ext.y, ext.z);
        Vector3 c011 = new Vector3(-ext.x, ext.y, ext.z);

        Vector3[] pts = new Vector3[]
        {
            c000, c100, c110, c010, c000,
            c001, c101, c100,
            c101, c111, c110,
            c111, c011, c010,
            c011, c001
        };

        _outlineRenderer.positionCount = pts.Length;
        _outlineRenderer.SetPositions(pts);
    }

    private void CreateCornerHandles()
    {
        // Clear any old handles
        foreach (Transform h in _handles)
        {
            if (h == null) continue;
            if (Application.isPlaying)
                Destroy(h.gameObject);
            else
                DestroyImmediate(h.gameObject);
        }
        _handles.Clear();

        Vector3 ext = boundsCube.localScale * 0.5f;

        Vector3[] localCorners =
        {
            new Vector3(-ext.x, -ext.y, -ext.z),
            new Vector3(ext.x,  -ext.y, -ext.z),
            new Vector3(ext.x,   ext.y, -ext.z),
            new Vector3(-ext.x,  ext.y, -ext.z),
            new Vector3(-ext.x, -ext.y,  ext.z),
            new Vector3(ext.x,  -ext.y,  ext.z),
            new Vector3(ext.x,   ext.y,  ext.z),
            new Vector3(-ext.x,  ext.y,  ext.z)
        };

        for (int i = 0; i < localCorners.Length; i++)
        {
            GameObject handle = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            handle.name = $"BoundsHandle_{i}";
            handle.transform.SetParent(boundsCube, false);
            handle.transform.localPosition = localCorners[i];
            handle.transform.localScale = Vector3.one * handleSize;

            if (cornerMaterial != null)
            {
                Renderer r = handle.GetComponent<Renderer>();
                if (r != null)
                    r.sharedMaterial = cornerMaterial;
            }

            Collider hc = handle.GetComponent<Collider>();
            if (hc != null)
                hc.isTrigger = true;

            _handles.Add(handle.transform);
        }
    }

    /// <summary>
    /// Recomputes the bounds cube based on the red handle positions,
    /// then applies scaling to the cabinet parts.
    /// NaN / Infinity safe.
    /// </summary>
    private void UpdateBoundsFromHandles()
    {
        // Collect min/max in cabinetRoot local space
        Vector3 minLocal = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
        Vector3 maxLocal = new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);

        int validCount = 0;

        foreach (Transform h in _handles)
        {
            if (h == null)
                continue;

            Vector3 worldPos = h.position;
            if (!IsFinite(worldPos))
                continue;

            Vector3 local = cabinetRoot.InverseTransformPoint(worldPos);
            if (!IsFinite(local))
                continue;

            minLocal = Vector3.Min(minLocal, local);
            maxLocal = Vector3.Max(maxLocal, local);
            validCount++;
        }

        // No valid handles → bail out rather than producing NaNs
        if (validCount == 0)
        {
            return;
        }

        if (!IsFinite(minLocal) || !IsFinite(maxLocal))
        {
            Debug.LogWarning("[CabinetBoundsResizer] Invalid min/max local handle positions, skipping update.");
            return;
        }

        Vector3 newCenterLocal = (minLocal + maxLocal) * 0.5f;
        Vector3 newSizeLocal = maxLocal - minLocal;

        if (!IsFinite(newCenterLocal) || !IsFinite(newSizeLocal))
        {
            Debug.LogWarning("[CabinetBoundsResizer] Computed NaN/Infinity for bounds center/size, skipping update.");
            return;
        }

        // Option A: only block **negative** sizes; 0 is allowed
        if (newSizeLocal.x < 0f || newSizeLocal.y < 0f || newSizeLocal.z < 0f)
        {
            Debug.LogWarning("[CabinetBoundsResizer] New bounds size has negative component(s), skipping update.");
            return;
        }

        boundsCube.localPosition = newCenterLocal;
        boundsCube.localScale = newSizeLocal;

        UpdateOutlineAndHandles(newSizeLocal);
        ApplyScalingToParts(newCenterLocal, newSizeLocal);
    }

    private void UpdateOutlineAndHandles(Vector3 newSizeLocal)
    {
        Vector3 ext = newSizeLocal * 0.5f;

        if (_outlineRenderer != null)
        {
            Vector3 c000 = new Vector3(-ext.x, -ext.y, -ext.z);
            Vector3 c100 = new Vector3(ext.x, -ext.y, -ext.z);
            Vector3 c110 = new Vector3(ext.x, ext.y, -ext.z);
            Vector3 c010 = new Vector3(-ext.x, ext.y, -ext.z);
            Vector3 c001 = new Vector3(-ext.x, -ext.y, ext.z);
            Vector3 c101 = new Vector3(ext.x, -ext.y, ext.z);
            Vector3 c111 = new Vector3(ext.x, ext.y, ext.z);
            Vector3 c011 = new Vector3(-ext.x, ext.y, ext.z);

            Vector3[] pts = new Vector3[]
            {
                c000, c100, c110, c010, c000,
                c001, c101, c100,
                c101, c111, c110,
                c111, c011, c010,
                c011, c001
            };

            _outlineRenderer.positionCount = pts.Length;
            _outlineRenderer.SetPositions(pts);
        }

        Vector3[] localCorners =
        {
            new Vector3(-ext.x, -ext.y, -ext.z),
            new Vector3(ext.x,  -ext.y, -ext.z),
            new Vector3(ext.x,   ext.y, -ext.z),
            new Vector3(-ext.x,  ext.y, -ext.z),
            new Vector3(-ext.x, -ext.y,  ext.z),
            new Vector3(ext.x,  -ext.y,  ext.z),
            new Vector3(ext.x,   ext.y,  ext.z),
            new Vector3(-ext.x,  ext.y,  ext.z)
        };

        for (int i = 0; i < _handles.Count && i < localCorners.Length; i++)
        {
            Transform h = _handles[i];
            if (h == null) continue;
            h.localPosition = localCorners[i];
            h.localScale = Vector3.one * handleSize;
        }
    }

    /// <summary>
    /// Applies scale based on new bounds, but keeps thickness (local Y) constant.
    /// Full-width parts (name contains ROD/TOE/TOP/BOTTOM/BOT/FSHELF/F SHELF) stretch in X.
    /// NaN / Infinity safe.
    /// </summary>
    private void ApplyScalingToParts(Vector3 newCenterLocal, Vector3 newSizeLocal)
    {
        if (!IsFinite(newCenterLocal) || !IsFinite(newSizeLocal))
        {
            Debug.LogWarning("[CabinetBoundsResizer] ApplyScalingToParts received invalid center/size, skipping.");
            return;
        }

        Vector3 origSize = _originalBoundsSizeLocal;
        if (origSize.x == 0f || origSize.y == 0f || origSize.z == 0f)
            return;

        float scaleX = newSizeLocal.x / origSize.x;
        float scaleY = newSizeLocal.y / origSize.y;
        float scaleZ = newSizeLocal.z / origSize.z;

        if (!float.IsFinite(scaleX) || !float.IsFinite(scaleY) || !float.IsFinite(scaleZ))
        {
            Debug.LogWarning("[CabinetBoundsResizer] Invalid scale factors, skipping.");
            return;
        }

        foreach (PartInfo p in _parts)
        {
            if (p.t == null)
                continue;

            Vector3 origPos = p.originalLocalPos;
            Vector3 rel = origPos - _originalBoundsCenterLocal;

            Vector3 relScaled = new Vector3(
                rel.x * scaleX,
                rel.y * scaleY,
                rel.z * scaleZ
            );

            if (!IsFinite(relScaled))
                continue;

            Vector3 newPos = newCenterLocal + relScaled;
            if (!IsFinite(newPos))
                continue;

            p.t.localPosition = newPos;

            Vector3 origScale = p.originalLocalScale;
            float newScaleX = p.stretchWidth ? origScale.x * scaleX : origScale.x;
            float newScaleY = origScale.y;              // thickness stays constant
            float newScaleZ = origScale.z * scaleZ;

            Vector3 newScale = new Vector3(newScaleX, newScaleY, newScaleZ);
            if (!IsFinite(newScale))
                continue;

            p.t.localScale = newScale;
        }
    }

    private bool IsFullWidthName(string name)
    {
        if (string.IsNullOrEmpty(name))
            return false;

        string upper = name.ToUpperInvariant();

        return upper.Contains("ROD")
            || upper.Contains("TOE")
            || upper.Contains("TOP")
            || upper.Contains("BOT")
            || upper.Contains("BOTTOM")
            || upper.Contains("FSHELF")
            || upper.Contains("F.SHELF")
            || upper.Contains("F SHELF");
    }

    private Transform FindChildByName(Transform root, string name)
    {
        foreach (Transform t in root.GetComponentsInChildren<Transform>())
        {
            if (t.name == name)
                return t;
        }
        return null;
    }

    private bool IsFinite(Vector3 v)
    {
        return float.IsFinite(v.x) && float.IsFinite(v.y) && float.IsFinite(v.z);
    }
}
