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
    [Tooltip("How much to inflate bounds (1.0 = same size, 1.02 = 2% larger).")]
    [Range(1f, 1.1f)]
    public float boundsInflation = 1.0f;

    [Tooltip("Color of selection wireframe lines.")]
    public Color wireframeColor = new Color(0f, 1f, 0f, 0.9f); // Green with 90% alpha

    [Tooltip("Width of wireframe lines.")]
    [Range(0.001f, 0.02f)]
    public float lineWidth = 0.005f;

    // Runtime selection flag (used in Play mode)
    private bool _runtimeSelected = false;

    // Store original materials for restoring
    private Material[] _originalCabinetMaterials;
    private Renderer[] _cabinetRenderers;

    // Track if transparency is currently applied (to avoid re-applying every frame)
    private bool _transparencyApplied = false;
    private bool _lastBoundsState = false;

    // Wireframe line renderers (12 edges of a cube)
    private LineRenderer[] _wireframeLines = null;
    private GameObject _wireframeContainer = null;

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

        // Hide old bounds renderer (we use wireframe lines instead)
        if (boundsRenderer != null)
        {
            boundsRenderer.enabled = false;
        }

        // Hide corner renderers
        if (cornerRenderers != null)
        {
            foreach (Renderer cornerRenderer in cornerRenderers)
            {
                if (cornerRenderer != null)
                {
                    cornerRenderer.enabled = false;
                }
            }
        }

        // Show/hide wireframe
        if (shouldShowBounds)
        {
            CreateWireframeIfNeeded();
        }
        else
        {
            DestroyWireframe();
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
                if (rend == null) continue; // Skip destroyed renderers (e.g., old wireframes)
                if (rend == boundsRenderer) continue; // Skip bounds
                if (cornerRenderers != null && System.Array.IndexOf(cornerRenderers, rend) >= 0) continue; // Skip corners
                if (rend is LineRenderer) continue; // Skip wireframe lines
                materialCount += rend.materials.Length;
            }

            _originalCabinetMaterials = new Material[materialCount];
            int index = 0;

            foreach (Renderer rend in _cabinetRenderers)
            {
                if (rend == null) continue; // Skip destroyed renderers
                if (rend == boundsRenderer) continue;
                if (cornerRenderers != null && System.Array.IndexOf(cornerRenderers, rend) >= 0) continue;
                if (rend is LineRenderer) continue; // Skip wireframe lines

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
            if (rend == null) continue; // Skip destroyed renderers
            if (rend == boundsRenderer) continue;
            if (cornerRenderers != null && System.Array.IndexOf(cornerRenderers, rend) >= 0) continue;
            if (rend is LineRenderer) continue; // Skip wireframe lines

            Material[] materials = rend.materials;
            Material[] transparentMaterials = new Material[materials.Length];

            for (int i = 0; i < materials.Length; i++)
            {
                transparentMaterials[i] = CreateTransparentMaterial(_originalCabinetMaterials[matIndex++], selectedCabinetAlpha);
            }

            rend.materials = transparentMaterials;
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
            if (rend is LineRenderer) continue; // Skip wireframe lines

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

        // Clear caches
        _originalCabinetMaterials = null;
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

    /// <summary>
    /// Calculates tight-fitting bounds from actual cabinet mesh geometry.
    /// Excludes Bounds object, corners, wireframes, and hardware.
    /// </summary>
    private Bounds CalculateCabinetBounds()
    {
        Bounds combinedBounds = new Bounds(transform.position, Vector3.zero);
        bool boundsInitialized = false;

        // Get all renderers
        Renderer[] renderers = GetComponentsInChildren<Renderer>();

        foreach (Renderer rend in renderers)
        {
            // Skip excluded objects
            if (rend == null) continue;
            if (rend == boundsRenderer) continue; // Skip Bounds object
            if (cornerRenderers != null && System.Array.IndexOf(cornerRenderers, rend) >= 0) continue;
            if (rend is LineRenderer) continue; // Skip wireframes
            
            // Skip hardware/rods based on name
            string objName = rend.gameObject.name;
            if (objName.Contains("Rod", System.StringComparison.OrdinalIgnoreCase) ||
                objName.Contains("Insert", System.StringComparison.OrdinalIgnoreCase) ||
                objName.Contains("Hardware", System.StringComparison.OrdinalIgnoreCase) ||
                objName.Contains("Hanger", System.StringComparison.OrdinalIgnoreCase) ||
                objName.Contains("Wireframe", System.StringComparison.OrdinalIgnoreCase) ||
                objName.Contains("Edge", System.StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            // Include this renderer's bounds
            if (!boundsInitialized)
            {
                combinedBounds = rend.bounds;
                boundsInitialized = true;
            }
            else
            {
                combinedBounds.Encapsulate(rend.bounds);
            }
        }

        return combinedBounds;
    }

    /// <summary>
    /// Creates 12 LineRenderers to draw a wireframe box around the cabinet bounds.
    /// </summary>
    private void CreateWireframeIfNeeded()
    {
        if (_wireframeLines != null) return; // Already created

        // Calculate bounds from actual cabinet geometry (not the Bounds object)
        Bounds bounds = CalculateCabinetBounds();

        // Inflate bounds
        Vector3 size = bounds.size * boundsInflation;
        Vector3 center = bounds.center;

        // Calculate 8 corners in world space
        Vector3 halfSize = size * 0.5f;
        Vector3[] corners = new Vector3[8];
        
        // Bottom 4 corners
        corners[0] = center + new Vector3(-halfSize.x, -halfSize.y, -halfSize.z);
        corners[1] = center + new Vector3(+halfSize.x, -halfSize.y, -halfSize.z);
        corners[2] = center + new Vector3(+halfSize.x, -halfSize.y, +halfSize.z);
        corners[3] = center + new Vector3(-halfSize.x, -halfSize.y, +halfSize.z);
        
        // Top 4 corners
        corners[4] = center + new Vector3(-halfSize.x, +halfSize.y, -halfSize.z);
        corners[5] = center + new Vector3(+halfSize.x, +halfSize.y, -halfSize.z);
        corners[6] = center + new Vector3(+halfSize.x, +halfSize.y, +halfSize.z);
        corners[7] = center + new Vector3(-halfSize.x, +halfSize.y, +halfSize.z);

        // Create container GameObject
        _wireframeContainer = new GameObject("SelectionWireframe");
        _wireframeContainer.transform.SetParent(transform);
        _wireframeContainer.transform.localPosition = Vector3.zero;
        _wireframeContainer.transform.localRotation = Quaternion.identity;

        // Create 12 line renderers (12 edges of cube)
        _wireframeLines = new LineRenderer[12];

        // Define the 12 edges (pairs of corner indices)
        int[,] edges = new int[12, 2]
        {
            // Bottom face (4 edges)
            {0, 1}, {1, 2}, {2, 3}, {3, 0},
            // Top face (4 edges)
            {4, 5}, {5, 6}, {6, 7}, {7, 4},
            // Vertical edges (4 edges)
            {0, 4}, {1, 5}, {2, 6}, {3, 7}
        };

        // Create each edge
        for (int i = 0; i < 12; i++)
        {
            GameObject lineGO = new GameObject($"Edge{i}");
            lineGO.transform.SetParent(_wireframeContainer.transform);
            lineGO.transform.localPosition = Vector3.zero;

            LineRenderer lr = lineGO.AddComponent<LineRenderer>();
            lr.positionCount = 2;
            lr.startWidth = lineWidth;
            lr.endWidth = lineWidth;
            
            // Use Unlit/Color shader for bright, always-visible lines
            Material lineMat = new Material(Shader.Find("Unlit/Color"));
            lineMat.color = wireframeColor;
            lr.material = lineMat;
            
            lr.startColor = wireframeColor;
            lr.endColor = wireframeColor;
            lr.useWorldSpace = true;

            // Set line positions
            lr.SetPosition(0, corners[edges[i, 0]]);
            lr.SetPosition(1, corners[edges[i, 1]]);

            _wireframeLines[i] = lr;
        }

        Debug.Log($"[MozBoundsHighlighter] Created wireframe with 12 lines for '{gameObject.name}'");
    }

    /// <summary>
    /// Destroys the wireframe LineRenderers.
    /// </summary>
    private void DestroyWireframe()
    {
        if (_wireframeContainer != null)
        {
            if (Application.isPlaying)
            {
                Destroy(_wireframeContainer);
            }
            else
            {
                DestroyImmediate(_wireframeContainer);
            }
            _wireframeContainer = null;
        }

        _wireframeLines = null;
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
