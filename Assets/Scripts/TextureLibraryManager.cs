using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;

/// <summary>
/// Loads and manages textures from a folder specified by admin.
/// Provides texture library for material picker UI.
/// </summary>
public class TextureLibraryManager : MonoBehaviour
{
    [Header("Admin Configuration")]
    [Tooltip("Path to textures folder (e.g., C:/Users/info/Dropbox/PAC Library HQ/Textures/Colors)")]
    public string texturesFolderPath = "";

    [Header("Runtime")]
    [Tooltip("List of loaded textures. Auto-populated on Start.")]
    public List<TextureEntry> loadedTextures = new List<TextureEntry>();

    // Singleton
    private static TextureLibraryManager _instance;
    public static TextureLibraryManager Instance => _instance;

    [System.Serializable]
    public class TextureEntry
    {
        public string fileName;        // e.g., "01 Cloud White.jpg"
        public string displayName;     // e.g., "01 Cloud White"
        public Texture2D texture;      // The loaded texture
        public Material material;      // Unity material with this texture
    }

    private void Awake()
    {
        // Singleton setup
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
    }

    private void Start()
    {
        LoadTexturesFromFolder();
    }

    /// <summary>
    /// Loads all JPG/PNG files from the specified folder.
    /// </summary>
    [ContextMenu("Reload Textures")]
    public void LoadTexturesFromFolder()
    {
        loadedTextures.Clear();

        if (string.IsNullOrEmpty(texturesFolderPath))
        {
            Debug.LogWarning("[TextureLibraryManager] No textures folder path configured.");
            return;
        }

        if (!Directory.Exists(texturesFolderPath))
        {
            Debug.LogError($"[TextureLibraryManager] Textures folder not found: {texturesFolderPath}");
            return;
        }

        // Get all .jpg and .png files
        string[] jpgFiles = Directory.GetFiles(texturesFolderPath, "*.jpg");
        string[] pngFiles = Directory.GetFiles(texturesFolderPath, "*.png");
        string[] allFiles = jpgFiles.Concat(pngFiles).ToArray();

        Debug.Log($"[TextureLibraryManager] Found {allFiles.Length} texture files in '{texturesFolderPath}'");

        foreach (string filePath in allFiles)
        {
            TextureEntry entry = LoadTextureFile(filePath);
            if (entry != null)
            {
                loadedTextures.Add(entry);
            }
        }

        Debug.Log($"[TextureLibraryManager] Loaded {loadedTextures.Count} textures successfully.");
    }

    /// <summary>
    /// Loads a single texture file and creates a material.
    /// </summary>
    private TextureEntry LoadTextureFile(string filePath)
    {
        try
        {
            // Read file bytes
            byte[] fileData = File.ReadAllBytes(filePath);
            
            // Create texture
            Texture2D texture = new Texture2D(2, 2);
            if (!texture.LoadImage(fileData))
            {
                Debug.LogWarning($"[TextureLibraryManager] Failed to load texture: {filePath}");
                return null;
            }

            // Create material using Standard shader
            Material material = new Material(Shader.Find("Standard"));
            material.mainTexture = texture;

            // Extract display name (filename without extension)
            string fileName = Path.GetFileName(filePath);
            string displayName = Path.GetFileNameWithoutExtension(filePath);

            return new TextureEntry
            {
                fileName = fileName,
                displayName = displayName,
                texture = texture,
                material = material
            };
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[TextureLibraryManager] Error loading texture '{filePath}': {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Gets a texture entry by display name.
    /// </summary>
    public TextureEntry GetTextureByName(string displayName)
    {
        return loadedTextures.FirstOrDefault(t => t.displayName == displayName);
    }

    /// <summary>
    /// Gets a material by display name.
    /// </summary>
    public Material GetMaterialByName(string displayName)
    {
        TextureEntry entry = GetTextureByName(displayName);
        return entry?.material;
    }
}
