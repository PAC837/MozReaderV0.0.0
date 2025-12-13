using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Applies materials/textures to cabinets.
/// Can apply to all cabinets in room or just selected cabinet.
/// </summary>
public class CabinetMaterialApplicator : MonoBehaviour
{
    [Header("Filtering")]
    [Tooltip("Part name patterns to exclude from material application (e.g., rods, inserts)")]
    public List<string> excludePartNames = new List<string>
    {
        "Rod",
        "Insert",
        "Hardware",
        "Hanger"
    };

    // Singleton
    private static CabinetMaterialApplicator _instance;
    public static CabinetMaterialApplicator Instance => _instance;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
    }

    /// <summary>
    /// Applies material to all cabinets in the room.
    /// </summary>
    public void ApplyMaterialToRoom(Material material)
    {
        if (material == null)
        {
            Debug.LogError("[CabinetMaterialApplicator] Cannot apply null material.");
            return;
        }

        // Find all MozCabinetData components (marks cabinet roots)
        MozCabinetData[] allCabinets = FindObjectsByType<MozCabinetData>(FindObjectsSortMode.None);
        
        int cabinetCount = 0;
        int partCount = 0;

        foreach (MozCabinetData cabinet in allCabinets)
        {
            int partsChanged = ApplyMaterialToCabinet(cabinet.gameObject, material);
            if (partsChanged > 0)
            {
                cabinetCount++;
                partCount += partsChanged;
            }
        }

        Debug.Log($"[CabinetMaterialApplicator] Applied material '{material.name}' to {partCount} parts across {cabinetCount} cabinets.");
    }

    /// <summary>
    /// Applies material to a single cabinet.
    /// </summary>
    public void ApplyMaterialToCabinet(GameObject cabinet, Material material)
    {
        if (cabinet == null || material == null)
        {
            Debug.LogError("[CabinetMaterialApplicator] Cannot apply: cabinet or material is null.");
            return;
        }

        int partCount = ApplyMaterialToCabinetInternal(cabinet, material);
        Debug.Log($"[CabinetMaterialApplicator] Applied material '{material.name}' to {partCount} parts on cabinet '{cabinet.name}'.");
    }

    /// <summary>
    /// Internal method that applies material and returns part count.
    /// </summary>
    private int ApplyMaterialToCabinetInternal(GameObject cabinet, Material material)
    {
        int partCount = 0;

        // Get all renderers in cabinet hierarchy
        Renderer[] renderers = cabinet.GetComponentsInChildren<Renderer>();

        foreach (Renderer rend in renderers)
        {
            // Skip excluded parts (rods, inserts, hardware)
            if (ShouldExcludePart(rend.gameObject.name))
            {
                continue;
            }

            // Skip bounds and debug objects
            if (rend.gameObject.name.Contains("Bounds") || 
                rend.gameObject.name.Contains("Corner") ||
                rend.gameObject.name.Contains("Debug") ||
                rend.gameObject.name.Contains("Wireframe") ||
                rend.gameObject.name.Contains("Edge"))
            {
                continue;
            }

            // Apply material
            rend.material = material;
            partCount++;
        }

        return partCount;
    }

    /// <summary>
    /// Checks if a part should be excluded from material application.
    /// </summary>
    private bool ShouldExcludePart(string partName)
    {
        foreach (string excludePattern in excludePartNames)
        {
            if (partName.Contains(excludePattern, System.StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Applies material to currently selected cabinet (if any).
    /// </summary>
    public void ApplyMaterialToSelected(Material material)
    {
        if (material == null)
        {
            Debug.LogError("[CabinetMaterialApplicator] Cannot apply null material.");
            return;
        }

        if (MozRuntimeSelector.Instance == null || MozRuntimeSelector.Instance.SelectedCabinet == null)
        {
            Debug.LogWarning("[CabinetMaterialApplicator] No cabinet selected.");
            return;
        }

        ApplyMaterialToCabinet(MozRuntimeSelector.Instance.SelectedCabinet, material);
    }
}
