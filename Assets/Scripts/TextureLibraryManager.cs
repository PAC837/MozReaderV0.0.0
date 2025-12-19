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

    [Header("Default Materials")]
    [Tooltip("Default color for walls (grey)")]
    public Color defaultWallColor = new Color(0.85f, 0.85f, 0.85f, 1f);

    [Tooltip("Default color for chrome/metal parts (rods, hangers)")]
    public Color defaultChromeColor = new Color(0.8f, 0.8f, 0.82f, 1f);

    [Tooltip("Default smoothness for chrome/metal (0-1)")]
    [Range(0f, 1f)]
    public float chromeSmoothness = 0.9f;

    [Tooltip("Default color for floor")]
    public Color defaultFloorColor = new Color(0.7f, 0.7f, 0.65f, 1f);

    // Cached default materials
    private Material _defaultWallMaterial;
    private Material _defaultChromeMaterial;
    private Material _defaultFloorMaterial;

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

            // Create material using URP Lit shader (Standard doesn't work in URP)
            Shader urpShader = Shader.Find("Universal Render Pipeline/Lit");
            if (urpShader == null)
            {
                Debug.LogWarning($"[TextureLibraryManager] URP Lit shader not found! Trying fallback shaders...");
                // Try alternative shader names
                urpShader = Shader.Find("Universal Render Pipeline/Simple Lit");
                if (urpShader == null)
                {
                    urpShader = Shader.Find("Unlit/Texture");
                }
            }
            
            Material material = new Material(urpShader);
            material.mainTexture = texture;
            
            Debug.Log($"[TextureLibraryManager] Created material with shader: '{material.shader.name}' for texture: '{Path.GetFileName(filePath)}'");

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

    /// <summary>
    /// Gets the default cabinet material (first texture in library, or beige URP material).
    /// </summary>
    public Material GetDefaultCabinetMaterial()
    {
        // If textures loaded, use first one
        if (loadedTextures.Count > 0 && loadedTextures[0].material != null)
        {
            Debug.Log($"[TextureLibraryManager] Using first texture as cabinet default: {loadedTextures[0].displayName}");
            return loadedTextures[0].material;
        }

        // Fallback: create beige URP material
        return CreateUrpMaterial(new Color(0.9f, 0.87f, 0.8f, 1f), 0.3f, "DefaultCabinetMaterial");
    }

    /// <summary>
    /// Gets the default wall material (grey matte).
    /// </summary>
    public Material GetDefaultWallMaterial()
    {
        if (_defaultWallMaterial == null)
        {
            _defaultWallMaterial = CreateUrpMaterial(defaultWallColor, 0.2f, "DefaultWallMaterial");
        }
        return _defaultWallMaterial;
    }

    /// <summary>
    /// Gets the default chrome/metal material for rods and hangers.
    /// </summary>
    public Material GetDefaultChromeMaterial()
    {
        if (_defaultChromeMaterial == null)
        {
            _defaultChromeMaterial = CreateUrpMaterial(defaultChromeColor, chromeSmoothness, "DefaultChromeMaterial");
            
            // Make it metallic
            if (_defaultChromeMaterial.HasProperty("_Metallic"))
            {
                _defaultChromeMaterial.SetFloat("_Metallic", 0.9f);
            }
        }
        return _defaultChromeMaterial;
    }

    /// <summary>
    /// Gets the default floor material.
    /// </summary>
    public Material GetDefaultFloorMaterial()
    {
        if (_defaultFloorMaterial == null)
        {
            _defaultFloorMaterial = CreateUrpMaterial(defaultFloorColor, 0.4f, "DefaultFloorMaterial");
        }
        return _defaultFloorMaterial;
    }

    /// <summary>
    /// Creates a URP Lit material with specified color and smoothness.
    /// </summary>
    private Material CreateUrpMaterial(Color color, float smoothness, string name)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        
        if (shader == null)
        {
            shader = Shader.Find("Universal Render Pipeline/Simple Lit");
        }
        if (shader == null)
        {
            shader = Shader.Find("Unlit/Color");
        }
        
        if (shader == null)
        {
            Debug.LogError($"[TextureLibraryManager] No URP shader found for {name}! Materials will be pink.");
            return null;
        }

        Material mat = new Material(shader);
        mat.name = name;
        mat.color = color;
        
        if (mat.HasProperty("_Smoothness"))
        {
            mat.SetFloat("_Smoothness", smoothness);
        }

        Debug.Log($"[TextureLibraryManager] Created {name} with shader '{shader.name}'");
        return mat;
    }
}
