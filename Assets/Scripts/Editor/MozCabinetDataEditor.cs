using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// Custom editor for MozCabinetData with "Add to Library" button.
/// Button adds THIS cabinet (the one the component is attached to) to the library.
/// </summary>
[CustomEditor(typeof(MozCabinetData))]
public class MozCabinetDataEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        MozCabinetData cabinetData = (MozCabinetData)target;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Cabinet Library", EditorStyles.boldLabel);

        if (GUILayout.Button("Add This Cabinet to Library", GUILayout.Height(35)))
        {
            AddThisCabinetToLibrary(cabinetData);
        }

        EditorGUILayout.HelpBox("Saves this cabinet as a prefab and adds it to the cabinet library for spawning at runtime.", MessageType.Info);
    }

    private void AddThisCabinetToLibrary(MozCabinetData cabinetData)
    {
        GameObject cabinetGO = cabinetData.gameObject;

        if (cabinetGO == null)
        {
            EditorUtility.DisplayDialog("Error", "Cannot find cabinet GameObject.", "OK");
            return;
        }

        // Ensure directories exist
        string prefabDir = "Assets/Resources/CabinetLibrary/Prefabs";
        if (!Directory.Exists(prefabDir))
        {
            Directory.CreateDirectory(prefabDir);
            AssetDatabase.Refresh();
        }

        // Create prefab name from product name
        string prefabName = string.IsNullOrEmpty(cabinetData.ProductName) 
            ? "Cabinet" 
            : cabinetData.ProductName.Replace(" ", "_");
        
        string prefabPath = $"{prefabDir}/{prefabName}.prefab";

        // Check if prefab already exists
        if (File.Exists(prefabPath))
        {
            if (!EditorUtility.DisplayDialog("Overwrite Prefab?", 
                $"A prefab named '{prefabName}' already exists. Overwrite it?", 
                "Overwrite", "Cancel"))
            {
                return;
            }
        }

        // Save as prefab
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(cabinetGO, prefabPath);
        
        if (prefab == null)
        {
            EditorUtility.DisplayDialog("Error", $"Failed to create prefab at '{prefabPath}'.", "OK");
            return;
        }

        Debug.Log($"[MozCabinetData] Created prefab at '{prefabPath}'");

        // Load or create the CabinetLibrary asset
        string libraryPath = "Assets/Resources/CabinetLibrary/CabinetLibrary.asset";
        CabinetLibrary library = AssetDatabase.LoadAssetAtPath<CabinetLibrary>(libraryPath);

        if (library == null)
        {
            // Create the library asset
            string libraryDir = "Assets/Resources/CabinetLibrary";
            if (!Directory.Exists(libraryDir))
            {
                Directory.CreateDirectory(libraryDir);
                AssetDatabase.Refresh();
            }

            library = ScriptableObject.CreateInstance<CabinetLibrary>();
            AssetDatabase.CreateAsset(library, libraryPath);
            Debug.Log($"[MozCabinetData] Created CabinetLibrary asset at '{libraryPath}'");
        }

        // Create library entry
        CabinetLibrary.CabinetEntry entry = new CabinetLibrary.CabinetEntry
        {
            displayName = prefabName,
            prefab = prefab,
            category = "Cabinets",
            productName = cabinetData.ProductName,
            sourceLibrary = cabinetData.SourceLibrary,
            widthMm = cabinetData.WidthMm,
            heightMm = cabinetData.HeightMm,
            depthMm = cabinetData.DepthMm
        };

        // Add to library
        library.AddCabinet(entry);
        
        // Save the library asset
        EditorUtility.SetDirty(library);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Success", 
            $"'{prefabName}' has been added to the cabinet library!\n\n" +
            $"Prefab: {prefabPath}\n" +
            $"Library entries: {library.cabinets.Count}", 
            "OK");

        // Ping the library asset in the project window
        EditorGUIUtility.PingObject(library);
    }
}
