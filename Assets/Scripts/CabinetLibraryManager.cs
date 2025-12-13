using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Singleton manager for the cabinet library system.
/// Loads the CabinetLibrary asset and provides methods to spawn cabinets at runtime.
/// </summary>
public class CabinetLibraryManager : MonoBehaviour
{
    private static CabinetLibraryManager _instance;
    public static CabinetLibraryManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<CabinetLibraryManager>();
                if (_instance == null)
                {
                    Debug.LogError("[CabinetLibraryManager] No instance found in scene. Add CabinetLibraryManager component to a GameObject.");
                }
            }
            return _instance;
        }
    }

    [Header("Library")]
    [Tooltip("Cabinet library asset containing all available cabinets")]
    public CabinetLibrary library;

    [Header("Runtime Settings")]
    [Tooltip("Parent for spawned cabinets (keep scene organized)")]
    public Transform cabinetParent;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Debug.LogWarning("[CabinetLibraryManager] Multiple instances detected. Destroying duplicate.");
            Destroy(gameObject);
            return;
        }

        _instance = this;

        // Load library from Resources if not assigned
        if (library == null)
        {
            library = Resources.Load<CabinetLibrary>("CabinetLibrary/CabinetLibrary");
            if (library == null)
            {
                Debug.LogWarning("[CabinetLibraryManager] No CabinetLibrary asset found at 'Resources/CabinetLibrary/CabinetLibrary'. " +
                                 "Create one via: Assets > Create > MozReader > Cabinet Library");
            }
            else
            {
                Debug.Log($"[CabinetLibraryManager] Loaded library with {library.cabinets.Count} cabinets.");
            }
        }

        // Create cabinet parent if not assigned
        if (cabinetParent == null)
        {
            GameObject parent = GameObject.Find("Cabinets");
            if (parent == null)
            {
                parent = new GameObject("Cabinets");
            }
            cabinetParent = parent.transform;
        }
    }

    /// <summary>
    /// Gets all cabinet display names from the library.
    /// </summary>
    public string[] GetCabinetNames()
    {
        if (library == null)
        {
            Debug.LogWarning("[CabinetLibraryManager] No library loaded.");
            return new string[0];
        }

        return library.GetAllDisplayNames();
    }

    /// <summary>
    /// Spawns a cabinet from the library and snaps it to the specified wall.
    /// Uses smart placement: finds first available space that fits the cabinet width.
    /// </summary>
    /// <param name="cabinetName">Display name of the cabinet in the library</param>
    /// <param name="targetWall">Wall to snap the cabinet to</param>
    /// <returns>The spawned cabinet GameObject, or null if failed</returns>
    public GameObject SpawnCabinet(string cabinetName, MozaikWall targetWall)
    {
        if (library == null)
        {
            Debug.LogError("[CabinetLibraryManager] Cannot spawn cabinet - no library loaded.");
            return null;
        }

        if (targetWall == null)
        {
            Debug.LogError("[CabinetLibraryManager] Cannot spawn cabinet - no target wall specified.");
            return null;
        }

        // Find cabinet in library
        CabinetLibrary.CabinetEntry entry = library.FindByName(cabinetName);
        if (entry == null)
        {
            Debug.LogError($"[CabinetLibraryManager] Cabinet '{cabinetName}' not found in library.");
            return null;
        }

        if (entry.prefab == null)
        {
            Debug.LogError($"[CabinetLibraryManager] Cabinet '{cabinetName}' has no prefab assigned.");
            return null;
        }

        // Get cabinet width from entry
        float cabinetWidthMm = entry.widthMm;

        // Find next available position using smart placement
        float? positionMm = FindNextAvailablePosition(targetWall, cabinetWidthMm);
        
        if (!positionMm.HasValue)
        {
            Debug.LogError($"[CabinetLibraryManager] Cannot place '{cabinetName}' (width {cabinetWidthMm}mm) - wall is full or cabinet too wide.");
            return null;
        }

        // Instantiate cabinet
        GameObject cabinet = Instantiate(entry.prefab, cabinetParent);
        cabinet.name = cabinetName; // Remove "(Clone)" suffix

        // Get cabinet data to set position before snapping
        MozCabinetData cabinetData = cabinet.GetComponent<MozCabinetData>();
        if (cabinetData != null)
        {
            cabinetData.XPositionMm = positionMm.Value;
        }

        // Get or add CabinetWallSnapper component
        CabinetWallSnapper snapper = cabinet.GetComponent<CabinetWallSnapper>();
        if (snapper == null)
        {
            snapper = cabinet.AddComponent<CabinetWallSnapper>();
        }

        // Snap to wall (will use XPositionMm from cabinetData)
        snapper.targetWall = targetWall;
        snapper.SnapToWall();

        Debug.Log($"[CabinetLibraryManager] Spawned '{cabinetName}' at position {positionMm.Value}mm on wall '{targetWall.gameObject.name}'.");

        return cabinet;
    }

    /// <summary>
    /// Spawns a cabinet and snaps it to the currently selected wall (from RuntimeWallSelector).
    /// </summary>
    public GameObject SpawnCabinetToSelectedWall(string cabinetName)
    {
        if (RuntimeWallSelector.Instance == null)
        {
            Debug.LogError("[CabinetLibraryManager] Cannot spawn - no RuntimeWallSelector in scene.");
            return null;
        }

        MozaikWall selectedWall = RuntimeWallSelector.Instance.SelectedWall;
        if (selectedWall == null)
        {
            Debug.LogError("[CabinetLibraryManager] Cannot spawn - no wall selected. Click on a wall first.");
            return null;
        }

        return SpawnCabinet(cabinetName, selectedWall);
    }

    /// <summary>
    /// Finds the next available position on a wall for a cabinet of given width.
    /// Looks for gaps between existing cabinets or at the end of the wall.
    /// Returns null if no space is available.
    /// </summary>
    /// <param name="wall">The wall to check</param>
    /// <param name="cabinetWidthMm">Width of the cabinet to place (mm)</param>
    /// <returns>XPositionMm for placement, or null if no space</returns>
    private float? FindNextAvailablePosition(MozaikWall wall, float cabinetWidthMm)
    {
        if (wall == null) return null;

        // Get all cabinets on this wall, sorted by position
        List<MozCabinetData> cabinetsOnWall = GetCabinetsOnWall(wall);

        float wallLengthMm = wall.lengthMm;

        // If no cabinets on wall, place at start (0)
        if (cabinetsOnWall.Count == 0)
        {
            // Check if cabinet fits on wall
            if (cabinetWidthMm <= wallLengthMm)
            {
                Debug.Log($"[CabinetLibraryManager] Wall is empty - placing at position 0mm");
                return 0f;
            }
            else
            {
                Debug.LogWarning($"[CabinetLibraryManager] Cabinet too wide ({cabinetWidthMm}mm) for wall ({wallLengthMm}mm)");
                return null;
            }
        }

        // Check gap at the start (before first cabinet)
        MozCabinetData firstCabinet = cabinetsOnWall[0];
        float firstGapWidth = firstCabinet.XPositionMm; // Space from 0 to first cabinet
        if (firstGapWidth >= cabinetWidthMm)
        {
            Debug.Log($"[CabinetLibraryManager] Found space at start - placing at position 0mm");
            return 0f;
        }

        // Check gaps between cabinets
        for (int i = 0; i < cabinetsOnWall.Count - 1; i++)
        {
            MozCabinetData currentCabinet = cabinetsOnWall[i];
            MozCabinetData nextCabinet = cabinetsOnWall[i + 1];

            float currentEnd = currentCabinet.XPositionMm + currentCabinet.WidthMm;
            float nextStart = nextCabinet.XPositionMm;
            float gapWidth = nextStart - currentEnd;

            if (gapWidth >= cabinetWidthMm)
            {
                Debug.Log($"[CabinetLibraryManager] Found gap between cabinets - placing at position {currentEnd}mm");
                return currentEnd;
            }
        }

        // Check space at the end (after last cabinet)
        MozCabinetData lastCabinet = cabinetsOnWall[cabinetsOnWall.Count - 1];
        float lastCabinetEnd = lastCabinet.XPositionMm + lastCabinet.WidthMm;
        float remainingSpace = wallLengthMm - lastCabinetEnd;

        if (remainingSpace >= cabinetWidthMm)
        {
            Debug.Log($"[CabinetLibraryManager] Found space at end - placing at position {lastCabinetEnd}mm");
            return lastCabinetEnd;
        }

        // No space found
        Debug.LogWarning($"[CabinetLibraryManager] No space on wall for cabinet (width {cabinetWidthMm}mm)");
        return null;
    }

    /// <summary>
    /// Gets all cabinets snapped to a specific wall, sorted by XPositionMm (left to right).
    /// </summary>
    /// <param name="wall">The wall to check</param>
    /// <returns>List of cabinet data, sorted by position</returns>
    private List<MozCabinetData> GetCabinetsOnWall(MozaikWall wall)
    {
        if (wall == null) return new List<MozCabinetData>();

        // Find all CabinetWallSnapper components in the scene
        CabinetWallSnapper[] allSnappers = FindObjectsByType<CabinetWallSnapper>(FindObjectsSortMode.None);

        List<MozCabinetData> cabinetsOnThisWall = new List<MozCabinetData>();

        foreach (var snapper in allSnappers)
        {
            // Check if this cabinet is snapped to the target wall
            if (snapper.targetWall == wall)
            {
                MozCabinetData cabinetData = snapper.GetComponent<MozCabinetData>();
                if (cabinetData != null)
                {
                    cabinetsOnThisWall.Add(cabinetData);
                }
            }
        }

        // Sort by XPositionMm (left to right)
        cabinetsOnThisWall = cabinetsOnThisWall.OrderBy(c => c.XPositionMm).ToList();

        Debug.Log($"[CabinetLibraryManager] Found {cabinetsOnThisWall.Count} cabinets on wall '{wall.name}'");

        return cabinetsOnThisWall;
    }
}
