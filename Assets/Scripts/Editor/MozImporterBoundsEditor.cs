using UnityEngine;
using UnityEditor;

/// <summary>
/// Custom editor for MozImporterBounds with "Import Moz With Bounds" button.
/// </summary>
[CustomEditor(typeof(MozImporterBounds))]
public class MozImporterBoundsEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        MozImporterBounds importer = (MozImporterBounds)target;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Import Options", EditorStyles.boldLabel);

        // Import button
        if (GUILayout.Button("Import Moz With Bounds", GUILayout.Height(30)))
        {
            importer.ImportWithBounds();
        }

        EditorGUILayout.HelpBox("Imports .moz file and creates a bounding box. Select the imported cabinet and look for 'Add to Library' button in its inspector.", MessageType.Info);
    }
}
