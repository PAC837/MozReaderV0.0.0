using UnityEngine;
using UnityEditor;

/// <summary>
/// Custom Inspector for TextureLibraryManager with folder browse button.
/// </summary>
[CustomEditor(typeof(TextureLibraryManager))]
public class TextureLibraryManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        TextureLibraryManager manager = (TextureLibraryManager)target;

        // Draw default inspector fields
        DrawDefaultInspector();

        EditorGUILayout.Space(10);

        // Add Browse Folder button
        if (GUILayout.Button("Browse Folder...", GUILayout.Height(30)))
        {
            string selectedPath = EditorUtility.OpenFolderPanel("Select Textures Folder", manager.texturesFolderPath, "");
            
            if (!string.IsNullOrEmpty(selectedPath))
            {
                // Update the path field
                SerializedObject so = new SerializedObject(manager);
                SerializedProperty pathProperty = so.FindProperty("texturesFolderPath");
                pathProperty.stringValue = selectedPath;
                so.ApplyModifiedProperties();

                Debug.Log($"[TextureLibraryManager] Selected folder: {selectedPath}");

                // Auto-reload textures
                if (Application.isPlaying)
                {
                    manager.LoadTexturesFromFolder();
                }
                else
                {
                    Debug.Log("[TextureLibraryManager] Enter Play mode to load textures.");
                }
            }
        }

        EditorGUILayout.Space(5);

        // Show texture count if loaded
        if (Application.isPlaying && manager.loadedTextures != null)
        {
            EditorGUILayout.HelpBox($"Loaded {manager.loadedTextures.Count} textures", MessageType.Info);
        }
        else if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Textures load at runtime (Play mode)", MessageType.Info);
        }
    }
}
