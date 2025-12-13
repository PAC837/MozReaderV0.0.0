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

    [Tooltip("Dropdown for selecting cabinets from library. Created at runtime if null.")]
    public TMP_Dropdown cabinetDropdown;

    [Tooltip("Button to delete the currently selected cabinet. Created at runtime if null.")]
    public Button deleteCabinetButton;

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

        // Create cabinet dropdown if needed
        if (cabinetDropdown == null)
        {
            cabinetDropdown = CreateCabinetDropdown(new Vector2(buttonAnchor.x, buttonAnchor.y - 0.14f));
            PopulateCabinetDropdown();
        }

        // Create delete cabinet button if needed
        if (deleteCabinetButton == null)
        {
            deleteCabinetButton = CreateBigButton("DELETE CABINET", new Vector2(buttonAnchor.x, buttonAnchor.y - 0.22f), OnDeleteCabinetClicked);
            // Make delete button red
            Image deleteImage = deleteCabinetButton.GetComponent<Image>();
            if (deleteImage != null)
            {
                deleteImage.color = new Color(0.8f, 0.2f, 0.2f, 1f); // Red
            }
            Debug.Log("[RuntimeWallUI] Created DELETE CABINET button.");
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

    /// <summary>
    /// Creates a dropdown for selecting cabinets from the library.
    /// </summary>
    private TMP_Dropdown CreateCabinetDropdown(Vector2 anchorPosition)
    {
        // Create dropdown GameObject
        GameObject dropdownGO = new GameObject("CabinetDropdown");
        dropdownGO.transform.SetParent(uiCanvas.transform, false);

        // Add Image background
        Image bgImage = dropdownGO.AddComponent<Image>();
        bgImage.color = new Color(0.15f, 0.15f, 0.15f, 0.9f);

        // Add dropdown component
        TMP_Dropdown dropdown = dropdownGO.AddComponent<TMP_Dropdown>();

        // Setup RectTransform
        RectTransform rect = dropdownGO.GetComponent<RectTransform>();
        rect.anchorMin = anchorPosition;
        rect.anchorMax = anchorPosition;
        rect.pivot = new Vector2(0f, 1f);
        rect.sizeDelta = new Vector2(buttonSize.x, 40f); // Same width as button
        rect.anchoredPosition = Vector2.zero;

        // Create Label (shows selected item)
        GameObject labelGO = new GameObject("Label");
        labelGO.transform.SetParent(dropdownGO.transform, false);
        TextMeshProUGUI label = labelGO.AddComponent<TextMeshProUGUI>();
        label.text = "Select Cabinet...";
        label.fontSize = 16;
        label.color = Color.white;
        label.alignment = TextAlignmentOptions.Left;
        
        RectTransform labelRect = labelGO.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(10, 2);
        labelRect.offsetMax = new Vector2(-25, -2);

        // Create Arrow
        GameObject arrowGO = new GameObject("Arrow");
        arrowGO.transform.SetParent(dropdownGO.transform, false);
        TextMeshProUGUI arrow = arrowGO.AddComponent<TextMeshProUGUI>();
        arrow.text = "▼";
        arrow.fontSize = 14;
        arrow.color = Color.white;
        arrow.alignment = TextAlignmentOptions.Center;
        
        RectTransform arrowRect = arrowGO.GetComponent<RectTransform>();
        arrowRect.anchorMin = new Vector2(1, 0);
        arrowRect.anchorMax = new Vector2(1, 1);
        arrowRect.pivot = new Vector2(1, 0.5f);
        arrowRect.sizeDelta = new Vector2(20, 0);
        arrowRect.anchoredPosition = new Vector2(-5, 0);

        // Create Template (dropdown list) with proper structure
        GameObject templateGO = new GameObject("Template");
        templateGO.transform.SetParent(dropdownGO.transform, false);
        templateGO.SetActive(false); // Hidden by default
        
        RectTransform templateRect = templateGO.AddComponent<RectTransform>();
        templateRect.anchorMin = new Vector2(0, 0);
        templateRect.anchorMax = new Vector2(1, 0);
        templateRect.pivot = new Vector2(0.5f, 1f);
        templateRect.sizeDelta = new Vector2(0, 150);
        templateRect.anchoredPosition = new Vector2(0, 0);

        Image templateBg = templateGO.AddComponent<Image>();
        templateBg.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);

        // Create Viewport for scrolling
        GameObject viewportGO = new GameObject("Viewport");
        viewportGO.transform.SetParent(templateGO.transform, false);
        RectTransform viewportRect = viewportGO.AddComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.sizeDelta = Vector2.zero;
        viewportRect.offsetMin = Vector2.zero;
        viewportRect.offsetMax = Vector2.zero;

        // Create Content (holds items)
        GameObject contentGO = new GameObject("Content");
        contentGO.transform.SetParent(viewportGO.transform, false);
        RectTransform contentRect = contentGO.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 1);
        contentRect.anchorMax = new Vector2(1, 1);
        contentRect.pivot = new Vector2(0.5f, 1);
        contentRect.sizeDelta = new Vector2(0, 28);

        // Create Item (template item with Toggle)
        GameObject itemGO = new GameObject("Item");
        itemGO.transform.SetParent(contentGO.transform, false);
        
        RectTransform itemRect = itemGO.AddComponent<RectTransform>();
        itemRect.anchorMin = new Vector2(0, 0.5f);
        itemRect.anchorMax = new Vector2(1, 0.5f);
        itemRect.pivot = new Vector2(0.5f, 0.5f);
        itemRect.sizeDelta = new Vector2(0, 20);

        // Add Toggle component (CRITICAL for dropdown!)
        Toggle itemToggle = itemGO.AddComponent<Toggle>();
        
        // Item background
        Image itemBg = itemGO.AddComponent<Image>();
        itemBg.color = new Color(0.2f, 0.2f, 0.2f, 1f);
        itemToggle.targetGraphic = itemBg;

        // Item checkmark
        GameObject checkmarkGO = new GameObject("Item Checkmark");
        checkmarkGO.transform.SetParent(itemGO.transform, false);
        Image checkmark = checkmarkGO.AddComponent<Image>();
        checkmark.color = Color.white;
        RectTransform checkmarkRect = checkmarkGO.GetComponent<RectTransform>();
        checkmarkRect.anchorMin = Vector2.zero;
        checkmarkRect.anchorMax = new Vector2(0, 1);
        checkmarkRect.sizeDelta = new Vector2(20, 0);
        checkmarkRect.anchoredPosition = new Vector2(10, 0);
        itemToggle.graphic = checkmark;
        checkmarkGO.SetActive(false); // Hide by default

        // Item label
        GameObject itemLabelGO = new GameObject("Item Label");
        itemLabelGO.transform.SetParent(itemGO.transform, false);
        TextMeshProUGUI itemLabel = itemLabelGO.AddComponent<TextMeshProUGUI>();
        itemLabel.text = "Option";
        itemLabel.fontSize = 14;
        itemLabel.color = Color.white;
        itemLabel.alignment = TextAlignmentOptions.Left;
        
        RectTransform itemLabelRect = itemLabelGO.GetComponent<RectTransform>();
        itemLabelRect.anchorMin = Vector2.zero;
        itemLabelRect.anchorMax = Vector2.one;
        itemLabelRect.offsetMin = new Vector2(20, 2);
        itemLabelRect.offsetMax = new Vector2(-5, -2);

        // Assign dropdown references
        dropdown.template = templateRect;
        dropdown.captionText = label;
        dropdown.itemText = itemLabel;

        // Add callback
        dropdown.onValueChanged.AddListener(OnCabinetSelected);

        return dropdown;
    }

    /// <summary>
    /// Populates the cabinet dropdown with items from the library.
    /// </summary>
    private void PopulateCabinetDropdown()
    {
        if (cabinetDropdown == null) return;

        if (CabinetLibraryManager.Instance == null)
        {
            Debug.LogWarning("[RuntimeWallUI] No CabinetLibraryManager found - dropdown will be empty.");
            return;
        }

        string[] cabinetNames = CabinetLibraryManager.Instance.GetCabinetNames();
        
        cabinetDropdown.ClearOptions();
        
        if (cabinetNames.Length == 0)
        {
            cabinetDropdown.options.Add(new TMP_Dropdown.OptionData("No cabinets in library"));
            cabinetDropdown.interactable = false;
            Debug.Log("[RuntimeWallUI] Cabinet library is empty. Add cabinets via MozImporterBounds.");
        }
        else
        {
            // Add placeholder
            cabinetDropdown.options.Add(new TMP_Dropdown.OptionData("Select Cabinet..."));
            
            // Add all cabinet names
            foreach (string name in cabinetNames)
            {
                cabinetDropdown.options.Add(new TMP_Dropdown.OptionData(name));
            }
            
            cabinetDropdown.value = 0;
            cabinetDropdown.RefreshShownValue();
            cabinetDropdown.interactable = true;
            
            Debug.Log($"[RuntimeWallUI] Populated dropdown with {cabinetNames.Length} cabinets.");
        }
    }

    /// <summary>
    /// Called when the Delete Cabinet button is clicked.
    /// Deletes the currently selected cabinet (from MozRuntimeSelector).
    /// </summary>
    private void OnDeleteCabinetClicked()
    {
        Debug.Log("=== [RuntimeWallUI] DELETE BUTTON CLICKED ===");

        if (MozRuntimeSelector.Instance == null)
        {
            Debug.LogError("[RuntimeWallUI] Cannot delete cabinet - no MozRuntimeSelector.Instance found.");
            return;
        }

        Debug.Log($"[RuntimeWallUI] MozRuntimeSelector.Instance found: {MozRuntimeSelector.Instance.name}");

        MozBoundsHighlighter selectedCabinet = MozRuntimeSelector.Instance.GetCurrentSelection();

        if (selectedCabinet == null)
        {
            Debug.LogWarning("[RuntimeWallUI] No cabinet selected. Click on a cabinet first to delete it.");
            Debug.LogWarning("[RuntimeWallUI] Make sure you left-click on a cabinet in the scene to select it before clicking delete.");
            return;
        }

        string cabinetName = selectedCabinet.gameObject.name;
        
        Debug.Log($"[RuntimeWallUI] Found selected cabinet: {cabinetName}");
        Debug.Log($"[RuntimeWallUI] Deleting cabinet GameObject...");

        // Clear the selection first (prevents trying to highlight deleted object)
        MozRuntimeSelector.Instance.ManualClearSelection();

        // Destroy the cabinet GameObject
        Destroy(selectedCabinet.gameObject);

        Debug.Log($"[RuntimeWallUI] ✓ Successfully deleted cabinet '{cabinetName}'.");
    }

    /// <summary>
    /// Called when a cabinet is selected from the dropdown.
    /// Spawns the cabinet and snaps it to the selected wall.
    /// </summary>
    private void OnCabinetSelected(int index)
    {
        if (index == 0) return; // Skip placeholder "Select Cabinet..."

        if (CabinetLibraryManager.Instance == null)
        {
            Debug.LogError("[RuntimeWallUI] Cannot spawn cabinet - no CabinetLibraryManager.");
            return;
        }

        if (_wallSelector == null || _wallSelector.SelectedWall == null)
        {
            Debug.LogWarning("[RuntimeWallUI] Cannot spawn cabinet - no wall selected. Click on a wall first.");
            // Reset dropdown to placeholder
            cabinetDropdown.value = 0;
            cabinetDropdown.RefreshShownValue();
            return;
        }

        string cabinetName = cabinetDropdown.options[index].text;
        Debug.Log($"[RuntimeWallUI] Spawning cabinet: {cabinetName}");

        GameObject spawned = CabinetLibraryManager.Instance.SpawnCabinetToSelectedWall(cabinetName);

        if (spawned != null)
        {
            Debug.Log($"[RuntimeWallUI] Successfully spawned '{cabinetName}' on wall '{_wallSelector.SelectedWall.name}'");
        }

        // Reset dropdown to placeholder after spawning
        cabinetDropdown.value = 0;
        cabinetDropdown.RefreshShownValue();
    }
}
