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

    // Runtime selection flag (used in Play mode)
    private bool _runtimeSelected = false;

    private void OnEnable()
    {
        FindBoundsRendererIfNeeded();
        UpdateVisibility();
    }

    private void Update()
    {
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

        // Apply visibility to bounds renderer
        boundsRenderer.enabled = shouldShowBounds;

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
