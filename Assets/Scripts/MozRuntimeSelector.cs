using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Handles click selection in the Game view using the new Input System.
/// On left mouse click:
/// - Raycasts from the camera into the scene
/// - Finds a MozBoundsHighlighter on the hit object or its parents
/// - Marks that cabinet as selected (shows its bounds)
/// - Deselects any previously selected cabinet
/// Attach this to an empty GameObject (e.g. "Moz Runtime Selector") in your scene.
/// </summary>
public class MozRuntimeSelector : MonoBehaviour
{
    [Header("Raycast Settings")]
    [Tooltip("Camera used for raycasting. If null, will default to Camera.main.")]
    public Camera targetCamera;

    [Tooltip("Maximum raycast distance.")]
    public float maxDistance = 1000f;

    [Tooltip("Layers to raycast against. Use this to filter what can be selected.")]
    public LayerMask raycastLayers = ~0; // Default to everything

    [Header("Debug")]
    [Tooltip("Show debug information in console.")]
    public bool showDebugLogs = false;

    private MozBoundsHighlighter _currentSelection;
    
    // New Input System references
    private Mouse mouse;

    private void Awake()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }
    }

    private void Start()
    {
        mouse = Mouse.current;
    }

    private void Update()
    {
        if (!Application.isPlaying || mouse == null)
            return;

        // Left mouse button down (using new Input System)
        if (mouse.leftButton.wasPressedThisFrame)
        {
            HandleClick();
        }
    }

    private void HandleClick()
    {
        if (targetCamera == null)
        {
            if (showDebugLogs)
                Debug.LogWarning("[MozRuntimeSelector] No target camera available for raycasting.");
            return;
        }

        // Get mouse position and create ray
        Vector2 mousePosition = mouse.position.ReadValue();
        Ray ray = targetCamera.ScreenPointToRay(mousePosition);

        if (showDebugLogs)
            Debug.Log($"[MozRuntimeSelector] Raycasting from mouse position: {mousePosition}");

        // Raycast with layer filtering
        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, raycastLayers))
        {
            if (showDebugLogs)
                Debug.Log($"[MozRuntimeSelector] Hit object: {hit.collider.name} at {hit.point}");

            // Look for a MozBoundsHighlighter on this object or any of its parents
            MozBoundsHighlighter hitHighlighter = hit.collider.GetComponentInParent<MozBoundsHighlighter>();

            if (hitHighlighter != null)
            {
                if (showDebugLogs)
                    Debug.Log($"[MozRuntimeSelector] Found MozBoundsHighlighter on: {hitHighlighter.name}");

                // Deselect previous selection if it's different
                if (_currentSelection != null && _currentSelection != hitHighlighter)
                {
                    _currentSelection.SetRuntimeSelected(false);
                    if (showDebugLogs)
                        Debug.Log($"[MozRuntimeSelector] Deselected: {_currentSelection.name}");
                }

                _currentSelection = hitHighlighter;

                // Select the new one
                _currentSelection.SetRuntimeSelected(true);
                if (showDebugLogs)
                    Debug.Log($"[MozRuntimeSelector] Selected: {_currentSelection.name}");
            }
            else
            {
                if (showDebugLogs)
                    Debug.Log("[MozRuntimeSelector] Hit object has no MozBoundsHighlighter component.");

                // Hit something but no highlighter - clear selection
                ClearSelection();
            }
        }
        else
        {
            if (showDebugLogs)
                Debug.Log("[MozRuntimeSelector] Raycast hit nothing - clearing selection.");

            // Clicked empty space: clear selection
            ClearSelection();
        }
    }

    private void ClearSelection()
    {
        if (_currentSelection != null)
        {
            _currentSelection.SetRuntimeSelected(false);
            if (showDebugLogs)
                Debug.Log($"[MozRuntimeSelector] Cleared selection: {_currentSelection.name}");
            _currentSelection = null;
        }
    }

    /// <summary>
    /// Manually clear the current selection (useful for UI buttons or other scripts)
    /// </summary>
    public void ManualClearSelection()
    {
        ClearSelection();
    }

    /// <summary>
    /// Get the currently selected cabinet highlighter (if any)
    /// </summary>
    public MozBoundsHighlighter GetCurrentSelection()
    {
        return _currentSelection;
    }

    private void OnDrawGizmosSelected()
    {
        // Draw a visual representation of the raycast in the Scene view
        if (targetCamera != null && mouse != null)
        {
            Vector2 mousePos = mouse.position.ReadValue();
            Ray ray = targetCamera.ScreenPointToRay(mousePos);
            
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(ray.origin, ray.direction * maxDistance);
        }
    }
}
