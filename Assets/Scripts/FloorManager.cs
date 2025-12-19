using UnityEngine;

/// <summary>
/// Manages automatic floor creation and sizing.
/// Auto-creates a floor plane at scene start with proper material.
/// </summary>
public class FloorManager : MonoBehaviour
{
    [Header("Floor Settings")]
    [Tooltip("Auto-create floor on Start if none exists.")]
    public bool autoCreateFloor = true;

    [Tooltip("Floor size in meters (X and Z).")]
    public float floorSize = 20f;

    [Tooltip("Floor material. If null, uses TextureLibraryManager default.")]
    public Material floorMaterial;

    [Header("Runtime")]
    [SerializeField] private GameObject _floorObject;

    // Singleton
    private static FloorManager _instance;
    public static FloorManager Instance => _instance;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
    }

    private void Start()
    {
        if (autoCreateFloor)
        {
            EnsureFloorExists();
        }
    }

    /// <summary>
    /// Creates floor if it doesn't exist.
    /// </summary>
    [ContextMenu("Create Floor")]
    public void EnsureFloorExists()
    {
        // Check if floor already exists
        if (_floorObject != null)
        {
            Debug.Log("[FloorManager] Floor already exists.");
            return;
        }

        // Look for existing floor in scene
        GameObject existingFloor = GameObject.Find("Floor");
        if (existingFloor != null)
        {
            _floorObject = existingFloor;
            Debug.Log("[FloorManager] Found existing Floor object.");
            ApplyFloorMaterial();
            return;
        }

        // Create new floor
        CreateFloor();
    }

    /// <summary>
    /// Creates a new floor plane.
    /// </summary>
    private void CreateFloor()
    {
        _floorObject = GameObject.CreatePrimitive(PrimitiveType.Plane);
        _floorObject.name = "Floor";
        _floorObject.transform.position = Vector3.zero;
        
        // Unity plane is 10x10 by default, so scale accordingly
        float scale = floorSize / 10f;
        _floorObject.transform.localScale = new Vector3(scale, 1f, scale);

        ApplyFloorMaterial();

        Debug.Log($"[FloorManager] Created floor ({floorSize}m x {floorSize}m).");
    }

    /// <summary>
    /// Applies material to floor.
    /// </summary>
    private void ApplyFloorMaterial()
    {
        if (_floorObject == null) return;

        Renderer r = _floorObject.GetComponent<Renderer>();
        if (r == null) return;

        Material mat = floorMaterial;

        // Try TextureLibraryManager if no material assigned
        if (mat == null && TextureLibraryManager.Instance != null)
        {
            mat = TextureLibraryManager.Instance.GetDefaultFloorMaterial();
            Debug.Log("[FloorManager] Using default floor material from TextureLibraryManager.");
        }

        // Fallback: create basic grey URP material
        if (mat == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Simple Lit");
            
            if (shader != null)
            {
                mat = new Material(shader);
                mat.color = new Color(0.7f, 0.7f, 0.65f, 1f);
                mat.name = "FloorMaterial";
                Debug.Log("[FloorManager] Created fallback floor material.");
            }
        }

        if (mat != null)
        {
            r.sharedMaterial = mat;
        }
        else
        {
            Debug.LogWarning("[FloorManager] Could not create floor material - will be pink!");
        }
    }

    /// <summary>
    /// Updates floor size at runtime.
    /// </summary>
    public void SetFloorSize(float size)
    {
        floorSize = size;
        if (_floorObject != null)
        {
            float scale = floorSize / 10f;
            _floorObject.transform.localScale = new Vector3(scale, 1f, scale);
            Debug.Log($"[FloorManager] Floor resized to {floorSize}m x {floorSize}m.");
        }
    }

    /// <summary>
    /// Updates floor material at runtime.
    /// </summary>
    public void SetFloorMaterial(Material mat)
    {
        floorMaterial = mat;
        ApplyFloorMaterial();
    }
}
