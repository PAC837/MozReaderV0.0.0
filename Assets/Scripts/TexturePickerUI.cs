using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// UI for picking materials/textures.
/// Shows texture squares in a grid with apply buttons.
/// </summary>
public class TexturePickerUI : MonoBehaviour
{
    [Header("UI Elements")]
    [Tooltip("Parent object for texture grid (uses GridLayoutGroup)")]
    public Transform textureGridParent;

    [Tooltip("Prefab for texture button (should have Button + Image + Text components)")]
    public GameObject textureButtonPrefab;

    [Tooltip("Button to apply material to entire room")]
    public Button applyToRoomButton;

    [Tooltip("Button to apply material to selected cabinet only")]
    public Button applyToSelectedButton;

    [Header("Settings")]
    [Tooltip("Size of each texture square in pixels")]
    public float textureSquareSize = 80f;

    [Tooltip("Spacing between texture squares")]
    public float gridSpacing = 5f;

    [Tooltip("Number of columns in grid (0 = auto)")]
    public int gridColumns = 4;

    // Currently selected texture
    private TextureLibraryManager.TextureEntry _selectedTexture;
    private GameObject _selectedButton;

    private List<GameObject> _textureButtons = new List<GameObject>();

    private void Start()
    {
        // Wire up buttons
        if (applyToRoomButton != null)
        {
            applyToRoomButton.onClick.AddListener(OnApplyToRoom);
        }

        if (applyToSelectedButton != null)
        {
            applyToSelectedButton.onClick.AddListener(OnApplyToSelected);
            UpdateApplyToSelectedButtonState();
        }

        // Subscribe to selection changes
        if (MozRuntimeSelector.Instance != null)
        {
            // Update button state when selection changes
            MozRuntimeSelector.Instance.OnSelectionChanged += UpdateApplyToSelectedButtonState;
        }

        // Generate texture grid after library loads
        Invoke(nameof(GenerateTextureGrid), 0.5f);
    }

    private void UpdateApplyToSelectedButtonState()
    {
        if (applyToSelectedButton != null)
        {
            bool hasSelection = MozRuntimeSelector.Instance != null && 
                               MozRuntimeSelector.Instance.SelectedCabinet != null;
            applyToSelectedButton.interactable = hasSelection && _selectedTexture != null;
        }
    }

    /// <summary>
    /// Generates the texture grid from loaded textures.
    /// </summary>
    [ContextMenu("Regenerate Texture Grid")]
    public void GenerateTextureGrid()
    {
        if (textureGridParent == null || textureButtonPrefab == null)
        {
            Debug.LogError("[TexturePickerUI] Missing UI references (grid parent or button prefab).");
            return;
        }

        if (TextureLibraryManager.Instance == null)
        {
            Debug.LogWarning("[TexturePickerUI] TextureLibraryManager not found.");
            return;
        }

        // Clear existing buttons
        foreach (GameObject btn in _textureButtons)
        {
            Destroy(btn);
        }
        _textureButtons.Clear();

        // Setup grid layout
        GridLayoutGroup gridLayout = textureGridParent.GetComponent<GridLayoutGroup>();
        if (gridLayout == null)
        {
            gridLayout = textureGridParent.gameObject.AddComponent<GridLayoutGroup>();
        }

        gridLayout.cellSize = new Vector2(textureSquareSize, textureSquareSize);
        gridLayout.spacing = new Vector2(gridSpacing, gridSpacing);
        gridLayout.constraint = gridColumns > 0 ? GridLayoutGroup.Constraint.FixedColumnCount : GridLayoutGroup.Constraint.Flexible;
        gridLayout.constraintCount = gridColumns;

        // Create button for each texture
        foreach (TextureLibraryManager.TextureEntry texture in TextureLibraryManager.Instance.loadedTextures)
        {
            CreateTextureButton(texture);
        }

        Debug.Log($"[TexturePickerUI] Generated {_textureButtons.Count} texture buttons.");
    }

    /// <summary>
    /// Creates a button for a texture.
    /// </summary>
    private void CreateTextureButton(TextureLibraryManager.TextureEntry texture)
    {
        GameObject buttonObj = Instantiate(textureButtonPrefab, textureGridParent);
        _textureButtons.Add(buttonObj);

        // Get components
        Button button = buttonObj.GetComponent<Button>();
        Image image = buttonObj.GetComponent<Image>();
        Text text = buttonObj.GetComponentInChildren<Text>();

        // Set texture as button image
        if (image != null && texture.texture != null)
        {
            Sprite sprite = Sprite.Create(
                texture.texture,
                new Rect(0, 0, texture.texture.width, texture.texture.height),
                new Vector2(0.5f, 0.5f)
            );
            image.sprite = sprite;
        }

        // Set tooltip/label
        if (text != null)
        {
            text.text = texture.displayName;
        }

        // Wire up click event
        if (button != null)
        {
            button.onClick.AddListener(() => OnTextureSelected(texture, buttonObj));
        }
    }

    /// <summary>
    /// Called when a texture is selected.
    /// </summary>
    private void OnTextureSelected(TextureLibraryManager.TextureEntry texture, GameObject buttonObj)
    {
        _selectedTexture = texture;
        _selectedButton = buttonObj;

        Debug.Log($"[TexturePickerUI] Selected texture: {texture.displayName}");

        // Update visual highlight (could add outline or tint)
        HighlightSelectedButton();

        // Update button states
        UpdateApplyToSelectedButtonState();
        if (applyToRoomButton != null)
        {
            applyToRoomButton.interactable = true;
        }
    }

    /// <summary>
    /// Highlights the selected texture button.
    /// </summary>
    private void HighlightSelectedButton()
    {
        // Reset all buttons
        foreach (GameObject btn in _textureButtons)
        {
            Image img = btn.GetComponent<Image>();
            if (img != null)
            {
                img.color = Color.white;
            }
        }

        // Highlight selected
        if (_selectedButton != null)
        {
            Image img = _selectedButton.GetComponent<Image>();
            if (img != null)
            {
                img.color = Color.yellow; // Or use outline
            }
        }
    }

    /// <summary>
    /// Applies selected texture to entire room.
    /// </summary>
    private void OnApplyToRoom()
    {
        if (_selectedTexture == null)
        {
            Debug.LogWarning("[TexturePickerUI] No texture selected.");
            return;
        }

        if (CabinetMaterialApplicator.Instance == null)
        {
            Debug.LogError("[TexturePickerUI] CabinetMaterialApplicator not found.");
            return;
        }

        CabinetMaterialApplicator.Instance.ApplyMaterialToRoom(_selectedTexture.material);
    }

    /// <summary>
    /// Applies selected texture to selected cabinet only.
    /// </summary>
    private void OnApplyToSelected()
    {
        if (_selectedTexture == null)
        {
            Debug.LogWarning("[TexturePickerUI] No texture selected.");
            return;
        }

        if (CabinetMaterialApplicator.Instance == null)
        {
            Debug.LogError("[TexturePickerUI] CabinetMaterialApplicator not found.");
            return;
        }

        CabinetMaterialApplicator.Instance.ApplyMaterialToSelected(_selectedTexture.material);
    }
}
