using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using TMPro;

/// <summary>
/// Runtime UI for wall management.
/// Creates a Canvas with:
/// - Big "ADD WALL" button
/// - Selected wall status text
/// 
/// Auto-creates UI elements if none are assigned.
/// Works with RuntimeWallSelector.
/// </summary>
public class RuntimeWallUI : MonoBehaviour
{
    [Header("UI References (Auto-created if null)")]
    [Tooltip("The Canvas for the wall UI. Created at runtime if null.")]
    public Canvas uiCanvas;

    [Tooltip("The ADD WALL button. Created at runtime if null.")]
    public Button addWallButton;

    [Tooltip("Text showing which wall is selected. Created at runtime if null.")]
    public TextMeshProUGUI selectedWallText;

    [Header("UI Settings")]
    [Tooltip("Button size in pixels.")]
    public Vector2 buttonSize = new Vector2(200f, 60f);

    [Tooltip("Font size for the button text.")]
    public int buttonFontSize = 24;

    [Tooltip("Button color.")]
    public Color buttonColor = new Color(0.2f, 0.6f, 0.2f, 1f);

    [Tooltip("Button hover color.")]
    public Color buttonHoverColor = new Color(0.3f, 0.7f, 0.3f, 1f);

    [Tooltip("Button text color.")]
    public Color buttonTextColor = Color.white;

    [Header("Position")]
    [Tooltip("Anchor position for the button (0,0 = bottom-left, 1,1 = top-right).")]
    public Vector2 buttonAnchor = new Vector2(0.02f, 0.95f);

    private RuntimeWallSelector _wallSelector;

    private void Start()
    {
        // Find or wait for RuntimeWallSelector
        _wallSelector = RuntimeWallSelector.Instance;
        if (_wallSelector == null)
        {
            _wallSelector = FindFirstObjectByType<RuntimeWallSelector>();
        }

        if (_wallSelector == null)
        {
            Debug.LogError("[RuntimeWallUI] No RuntimeWallSelector found in scene. UI will not function.");
            return;
        }

        // Subscribe to wall selection events
        _wallSelector.OnWallSelected += HandleWallSelected;

        // Create UI if not assigned
        EnsureUIExists();

        // Initial UI state
        UpdateSelectedWallText(null);
    }

    private void OnDestroy()
    {
        if (_wallSelector != null)
        {
            _wallSelector.OnWallSelected -= HandleWallSelected;
        }
    }

    private void EnsureUIExists()
    {
        // CRITICAL: Ensure EventSystem exists (required for UI clicks!)
        EnsureEventSystemExists();

        // Create Canvas if needed
        if (uiCanvas == null)
        {
            GameObject canvasGO = new GameObject("WallUI_Canvas");
            uiCanvas = canvasGO.AddComponent<Canvas>();
            uiCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            uiCanvas.sortingOrder = 100; // On top of other UI

            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();

            canvasGO.transform.SetParent(transform);
            
            Debug.Log("[RuntimeWallUI] Created Canvas for UI.");
        }

        // Create ADD WALL button if needed
        if (addWallButton == null)
        {
            addWallButton = CreateBigButton("ADD WALL", buttonAnchor, OnAddWallClicked);
            Debug.Log("[RuntimeWallUI] Created ADD WALL button.");
        }

        // Create selected wall text if needed
        if (selectedWallText == null)
        {
            selectedWallText = CreateStatusText(new Vector2(buttonAnchor.x, buttonAnchor.y - 0.08f));
        }
    }

    /// <summary>
    /// Ensures an EventSystem exists in the scene. Required for UI clicks to work!
    /// Uses InputSystemUIInputModule for compatibility with new Input System.
    /// </summary>
    private void EnsureEventSystemExists()
    {
        if (FindFirstObjectByType<EventSystem>() == null)
        {
            GameObject eventSystemGO = new GameObject("EventSystem");
            eventSystemGO.AddComponent<EventSystem>();
            // Use InputSystemUIInputModule for new Input System compatibility
            eventSystemGO.AddComponent<InputSystemUIInputModule>();
            Debug.Log("[RuntimeWallUI] Created EventSystem with InputSystemUIInputModule.");
        }
    }

    private Button CreateBigButton(string label, Vector2 anchorPosition, UnityEngine.Events.UnityAction onClick)
    {
        // Create button GameObject
        GameObject buttonGO = new GameObject($"Button_{label.Replace(" ", "")}");
        buttonGO.transform.SetParent(uiCanvas.transform, false);

        // Add Image component (required for Button)
        Image buttonImage = buttonGO.AddComponent<Image>();
        buttonImage.color = buttonColor;
        buttonImage.raycastTarget = true; // CRITICAL: Enable raycast for clicks!

        // Add Button component
        Button button = buttonGO.AddComponent<Button>();
        
        // CRITICAL: Set target graphic for button to receive clicks!
        button.targetGraphic = buttonImage;
        
        // Setup button colors
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white; // Use white so Image color shows through
        colors.highlightedColor = buttonHoverColor;
        colors.pressedColor = buttonColor * 0.8f;
        colors.selectedColor = buttonHoverColor;
        button.colors = colors;

        // Setup RectTransform for positioning
        RectTransform rectTransform = buttonGO.GetComponent<RectTransform>();
        rectTransform.anchorMin = anchorPosition;
        rectTransform.anchorMax = anchorPosition;
        rectTransform.pivot = new Vector2(0f, 1f); // Top-left pivot
        rectTransform.sizeDelta = buttonSize;
        rectTransform.anchoredPosition = Vector2.zero;

        // Create text child
        GameObject textGO = new GameObject("Text");
        textGO.transform.SetParent(buttonGO.transform, false);

        // Try to use TextMeshPro, fall back to legacy Text
        TextMeshProUGUI tmpText = textGO.AddComponent<TextMeshProUGUI>();
        tmpText.text = label;
        tmpText.fontSize = buttonFontSize;
        tmpText.color = buttonTextColor;
        tmpText.alignment = TextAlignmentOptions.Center;
        tmpText.fontStyle = FontStyles.Bold;

        // Make text fill button
        RectTransform textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        // Wire up click handler
        button.onClick.AddListener(onClick);

        return button;
    }

    private TextMeshProUGUI CreateStatusText(Vector2 anchorPosition)
    {
        GameObject textGO = new GameObject("SelectedWallText");
        textGO.transform.SetParent(uiCanvas.transform, false);

        TextMeshProUGUI text = textGO.AddComponent<TextMeshProUGUI>();
        text.text = "No wall selected";
        text.fontSize = 16;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Left;

        RectTransform rectTransform = textGO.GetComponent<RectTransform>();
        rectTransform.anchorMin = anchorPosition;
        rectTransform.anchorMax = anchorPosition;
        rectTransform.pivot = new Vector2(0f, 1f);
        rectTransform.sizeDelta = new Vector2(300f, 30f);
        rectTransform.anchoredPosition = Vector2.zero;

        return text;
    }

    private void OnAddWallClicked()
    {
        if (_wallSelector == null)
        {
            Debug.LogError("[RuntimeWallUI] Cannot add wall - no RuntimeWallSelector found.");
            return;
        }

        Debug.Log("[RuntimeWallUI] ADD WALL button clicked!");
        _wallSelector.CreateWallAtOrigin();
    }

    private void HandleWallSelected(MozaikWall wall)
    {
        UpdateSelectedWallText(wall);
    }

    private void UpdateSelectedWallText(MozaikWall wall)
    {
        if (selectedWallText == null) return;

        if (wall != null)
        {
            selectedWallText.text = $"Selected: {wall.name}";
            selectedWallText.color = new Color(0.5f, 1f, 0.5f); // Light green
        }
        else
        {
            selectedWallText.text = "No wall selected";
            selectedWallText.color = new Color(1f, 0.8f, 0.5f); // Orange/yellow
        }
    }

    /// <summary>
    /// Public method to trigger wall creation (can be called from other scripts or events).
    /// </summary>
    public void AddWall()
    {
        OnAddWallClicked();
    }
}
