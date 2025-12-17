using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// ScriptableObject that stores the cabinet library for the game.
/// Each white-label version can have its own library of available cabinets.
/// Editable in Inspector for easy customization.
/// </summary>
[CreateAssetMenu(fileName = "CabinetLibrary", menuName = "MozReader/Cabinet Library")]
public class CabinetLibrary : ScriptableObject
{
    [System.Serializable]
    public class CabinetEntry
    {
        [Tooltip("Display name shown in dropdown")]
        public string displayName;

        [Tooltip("Cabinet prefab with MozCabinetData component")]
        public GameObject prefab;

        [Tooltip("Optional category for organizing (e.g., 'Base', 'Upper', 'Tall')")]
        public string category = "Cabinets";

        [Tooltip("Optional thumbnail for UI (future use)")]
        public Texture2D thumbnail;

        [Header("Metadata from .moz file")]
        public string productName;
        public string sourceLibrary;
        public float widthMm;
        public float heightMm;
        public float depthMm;
    }

    [Tooltip("List of all available cabinets in this library")]
    public List<CabinetEntry> cabinets = new List<CabinetEntry>();

    /// <summary>
    /// Finds a cabinet entry by display name.
    /// </summary>
    public CabinetEntry FindByName(string name)
    {
        return cabinets.Find(c => c.displayName == name);
    }

    /// <summary>
    /// Adds a new cabinet to the library, or updates if it already exists.
    /// </summary>
    public void AddCabinet(CabinetEntry entry)
    {
        int existingIndex = cabinets.FindIndex(c => c.displayName == entry.displayName);
        
        if (existingIndex >= 0)
        {
            // Update existing entry (preserves prefab if new entry has one, keeps metadata fresh)
            CabinetEntry existing = cabinets[existingIndex];
            
            // Update prefab if the new entry has one
            if (entry.prefab != null)
            {
                existing.prefab = entry.prefab;
            }
            
            // Always update metadata
            existing.category = entry.category;
            existing.productName = entry.productName;
            existing.sourceLibrary = entry.sourceLibrary;
            existing.widthMm = entry.widthMm;
            existing.heightMm = entry.heightMm;
            existing.depthMm = entry.depthMm;
            existing.thumbnail = entry.thumbnail ?? existing.thumbnail;
            
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
            Debug.Log($"[CabinetLibrary] Updated existing entry '{entry.displayName}' in library.");
            return;
        }

        cabinets.Add(entry);
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
        Debug.Log($"[CabinetLibrary] Added '{entry.displayName}' to library.");
    }

    /// <summary>
    /// Gets all cabinet display names.
    /// </summary>
    public string[] GetAllDisplayNames()
    {
        string[] names = new string[cabinets.Count];
        for (int i = 0; i < cabinets.Count; i++)
        {
            names[i] = cabinets[i].displayName;
        }
        return names;
    }

    /// <summary>
    /// Gets all cabinets in a specific category.
    /// </summary>
    public List<CabinetEntry> GetByCategory(string category)
    {
        return cabinets.FindAll(c => c.category == category);
    }
}
