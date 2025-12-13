using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Controls visibility of the "Bounds" cube for a Moz-imported cabinet.
/// - In the Editor (Scene view), it shows the bounds when the root is selected.
/// - At runtime (Game view), it shows the bounds when SetRuntimeSelected(true) is called.
/// Attach this to the cabinet root (e.g. "87 DH_WithBounds").
/// </summary>
[ExecuteAlways]
public class MozBoundsHighlighter : MonoBehaviour
{
    [Tooltip("Renderer for the Bounds cube. If null, it will search for a child named 'Bounds'.")]
    public Renderer boundsRenderer;

    [Tooltip("Renderers for corner indicators. These will be toggled alongside the bounds.")]
    public Renderer[] cornerRenderers;

    [Tooltip("If true, bounds can be shown in Play mode via runtime selection.")]
    public bool showInPlayMode = true;

    [Header("Selection Transparency")]
    [Tooltip("Alpha value for cabinet meshes when selected (0-1). Lower = more transparent.")]
    [Range(0f, 1f)]
    public float selectedCabinetAlpha = 0.1f;

    [Tooltip("Alpha value for bounds outline when selected (0-1).")]
    [Range(0f, 1f)]
    public float selectedBoundsAlpha = 0.4f;

    [Header("Bounds Visualization")]
    [Tooltip("How much to inflate bounds scale (1.02 = 2% larger).")]
    [Range(1f, 1.1f)]
    public float boundsInflation = 1.02f;

    // Runtime selection flag (used in Play mode)
    private bool _runtimeSelected = false;

    // Store original materials for restoring
    private Material[] _originalCabinetMaterials;
    private Material _originalBoundsMaterial;
    private Renderer[] _cabinetRenderers;

    // Track if transparency is currently applied (to avoid re-applying every frame)
    private bool _transparencyApplied = false;
    private bool _lastBoundsState = false;

    // Track bounds original scale for inflation
    private Vector3 _originalBoundsScale = Vector3.zero;

    private void OnEnable()
    {
        FindBoundsRendererIfNeeded();
        UpdateVisibility();
    }

    private void Update()
    {
        // ONLY update if state changed (not every frame!)
        UpdateVisibility();
    }

    /// <summary>
    /// Called by a runtime selector (e.g. MozRuntimeSelector) to mark
    /// this cabinet as selected or not at runtime.
    /// </summary>
    public void SetRuntimeSelected(bool selected)
    {
        _runtimeSelected = selected;
        UpdateVisibility();
    }

    private void FindBoundsRendererIfNeeded()
    {
        if (boundsRenderer != null)
            return;

        Transform boundsTransform = FindChildByName(transform, "Bounds");
        if (boundsTransform != null)
        {
            boundsRenderer = boundsTransform.GetComponent<Renderer>();
        }
    }

    private void UpdateVisibility()
    {
        if (boundsRenderer == null)
            return;

        bool shouldShowBounds = false;

#if UNITY_EDITOR
        // Editor behavior (Scene view selection)
        bool isSelectedRoot = (Selection.activeGameObject == gameObject);

        if (Application.isPlaying)
        {
            // Play mode: use runtime selection flag (if allowed)
            if (showInPlayMode)
            {
                shouldShowBounds = _runtimeSelected;
            }
            else
            {
                shouldShowBounds = false;
            }
        }
        else
        {
            // Edit mode (not playing): show when this root is selected
            shouldShowBounds = isSelectedRoot;
        }
#else
        // In a built player (no editor), only runtime selection matters.
        if (Application.isPlaying && showInPlayMode)
        {
            shouldShowBounds = _runtimeSelected;
        }
        else
        {
            shouldShowBounds = false;
        }
#endif

        // Apply visibility and scale to bounds renderer
        boundsRenderer.enabled = shouldShowBounds;

        // Inflate or restore bounds scale
        if (shouldShowBounds)
        {
            // Store original scale if first time
            if (_originalBoundsScale == Vector3.zero)
            {
                _originalBoundsScale = boundsRenderer.transform.localScale;
                Debug.Log($"[MozBoundsHighlighter] Stored original bounds scale: {_originalBoundsScale}");
            }

            // Apply inflation
            boundsRenderer.transform.localScale = _originalBoundsScale * boundsInflation;
        }
        else
        {
            // Restore original scale when hiding
            if (_originalBoundsScale != Vector3.zero)
            {
                boundsRenderer.transform.localScale = _originalBoundsScale;
            }
        }

        // Apply same visibility to all corner renderers
        if (cornerRenderers != null)
        {
            foreach (Renderer cornerRenderer in cornerRenderers)
            {
                if (cornerRenderer != null)
                {
                    cornerRenderer.enabled = shouldShowBounds;
                }
            }
        }

        // CRITICAL: Only apply/remove transparency when state CHANGES (not every frame!)
        if (shouldShowBounds != _lastBoundsState)
        {
            _lastBoundsState = shouldShowBounds;

            if (shouldShowBounds && !_transparencyApplied)
            {
                ApplySelectionTransparency();
                _transparencyApplied = true;
                Debug.Log($"[MozBoundsHighlighter] Applied transparency to '{gameObject.name}'");
            }
            else if (!shouldShowBounds && _transparencyApplied)
            {
                RestoreOriginalMaterials();
                _transparencyApplied = false;
                Debug.Log($"[MozBoundsHighlighter] Restored materials for '{gameObject.name}'");
            }
        }
    }

    /// <summary>
    /// Applies transparent materials to cabinet meshes and bounds when selected.
    /// </summary>
    private void ApplySelectionTransparency()
    {
        // Find and cache cabinet renderers (excluding Bounds)
        if (_cabinetRenderers == null)
        {
            _cabinetRenderers = GetComponentsInChildren<Renderer>();
        }

        // Store and modify cabinet materials
        if (_originalCabinetMaterials == null)
        {
            int materialCount = 0;
            foreach (Renderer rend in _cabinetRenderers)
            {
                if (rend == boundsRenderer) continue; // Skip bounds
                if (cornerRenderers != null && System.Array.IndexOf(cornerRenderers, rend) >= 0) continue; // Skip corners
                materialCount += rend.materials.Length;
            }

            _originalCabinetMaterials = new Material[materialCount];
            int index = 0;

            foreach (Renderer rend in _cabinetRenderers)
            {
                if (rend == boundsRenderer) continue;
                if (cornerRenderers != null && System.Array.IndexOf(cornerRenderers, rend) >= 0) continue;

                Material[] materials = rend.materials;
                for (int i = 0; i < materials.Length; i++)
                {
                    _originalCabinetMaterials[index++] = materials[i];
                }
            }
        }

        // Apply transparent materials to cabinet
        int matIndex = 0;
        foreach (Renderer rend in _cabinetRenderers)
        {
            if (rend == boundsRenderer) continue;
            if (cornerRenderers != null && System.Array.IndexOf(cornerRenderers, rend) >= 0) continue;

            Material[] materials = rend.materials;
            Material[] transparentMaterials = new Material[materials.Length];

            for (int i = 0; i < materials.Length; i++)
            {
                transparentMaterials[i] = CreateTransparentMaterial(_originalCabinetMaterials[matIndex++], selectedCabinetAlpha);
            }

            rend.materials = transparentMaterials;
        }

        // Apply transparency to bounds
        if (_originalBoundsMaterial == null && boundsRenderer != null)
        {
            _originalBoundsMaterial = boundsRenderer.sharedMaterial;
        }

        if (boundsRenderer != null)
        {
            boundsRenderer.material = CreateTransparentMaterial(_originalBoundsMaterial, selectedBoundsAlpha);
        }
    }

    /// <summary>
    /// Restores original materials to cabinet and bounds.
    /// </summary>
    private void RestoreOriginalMaterials()
    {
        if (_originalCabinetMaterials == null) return;

        int matIndex = 0;
        foreach (Renderer rend in _cabinetRenderers)
        {
            if (rend == null) continue;
            if (rend == boundsRenderer) continue;
            if (cornerRenderers != null && System.Array.IndexOf(cornerRenderers, rend) >= 0) continue;

            Material[] materials = rend.materials;
            Material[] restoredMaterials = new Material[materials.Length];

            for (int i = 0; i < materials.Length; i++)
            {
                if (matIndex < _originalCabinetMaterials.Length)
                {
                    restoredMaterials[i] = _originalCabinetMaterials[matIndex++];
                }
            }

            rend.materials = restoredMaterials;
        }

        // Restore bounds material
        if (_originalBoundsMaterial != null && boundsRenderer != null)
        {
            boundsRenderer.material = _originalBoundsMaterial;
        }

        // Clear caches
        _originalCabinetMaterials = null;
        _originalBoundsMaterial = null;
    }

    /// <summary>
    /// Creates a transparent version of a material by setting its rendering mode and alpha.
    /// </summary>
    private Material CreateTransparentMaterial(Material original, float alpha)
    {
        if (original == null) return null;

        Material transparentMat = new Material(original);
        
        // Set rendering mode to Transparent
        transparentMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        transparentMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        transparentMat.SetInt("_ZWrite", 0);
        transparentMat.DisableKeyword("_ALPHATEST_ON");
        transparentMat.EnableKeyword("_ALPHABLEND_ON");
        transparentMat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        transparentMat.renderQueue = 3000;

        // Set alpha
        Color color = transparentMat.color;
        color.a = alpha;
        transparentMat.color = color;

        return transparentMat;
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
}
