using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

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
    public bool showDebugLogs = true; // ALWAYS ON for debugging

    private MozBoundsHighlighter _currentSelection;
    
    // New Input System references
    private Mouse mouse;

    // Singleton pattern
    public static MozRuntimeSelector Instance { get; private set; }

    // Public API for accessing selected cabinet
    public GameObject SelectedCabinet => _currentSelection?.gameObject;

    // Event fired when selection changes
    public event System.Action OnSelectionChanged;

    private void Awake()
    {
        // Singleton setup
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Debug.LogWarning("[MozRuntimeSelector] Multiple instances detected. Destroying duplicate.");
            Destroy(gameObject);
            return;
        }

        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
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
        // CRITICAL: Don't handle clicks if pointer is over UI (prevents clearing selection when clicking buttons!)
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            if (showDebugLogs)
                Debug.Log("[MozRuntimeSelector] Click ignored - pointer is over UI.");
            return;
        }

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
            Debug.Log($"[CLICK] MozRuntimeSelector: Mouse at {mousePosition}, raycasting...");

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
                
                // Fire selection changed event
                OnSelectionChanged?.Invoke();
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
            
            // Fire selection changed event
            OnSelectionChanged?.Invoke();
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
