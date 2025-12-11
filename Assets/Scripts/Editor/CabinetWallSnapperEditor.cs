using UnityEngine;
using UnityEditor;

/// <summary>
/// Custom Inspector for CabinetWallSnapper.
/// Provides buttons for snapping, rotation, and debug functions.
/// </summary>
[CustomEditor(typeof(CabinetWallSnapper))]
public class CabinetWallSnapperEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Draw the default Inspector fields
        DrawDefaultInspector();

        CabinetWallSnapper snapper = (CabinetWallSnapper)target;

        EditorGUILayout.Space(10);

        // Show warning if no wall is assigned
        if (snapper.targetWall == null)
        {
            EditorGUILayout.HelpBox(
                "Assign a target MozaikWall to enable snapping.",
                MessageType.Warning);
        }

        // === MAIN ACTION BUTTONS ===
        EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);

        // Snap to Wall button (requires wall)
        EditorGUI.BeginDisabledGroup(snapper.targetWall == null);
        
        GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f); // Green tint
        if (GUILayout.Button("Snap to Wall", GUILayout.Height(30)))
        {
            snapper.SnapToWall();
        }
        GUI.backgroundColor = Color.white;
        
        EditorGUI.EndDisabledGroup();

        // Rotate to Face Room button (works without wall, but uses wall direction if available)
        GUI.backgroundColor = new Color(0.6f, 0.6f, 0.9f); // Blue tint
        if (GUILayout.Button("Rotate to Face Room", GUILayout.Height(24)))
        {
            snapper.RotateToFaceRoom();
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.Space(10);

        // === DEBUG SECTION ===
        EditorGUILayout.LabelField("Debug Tools", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginHorizontal();
        
        GUI.backgroundColor = new Color(0.9f, 0.9f, 0.6f); // Yellow tint
        if (GUILayout.Button("Log Orientation Info"))
        {
            snapper.LogOrientationDebugInfo();
        }
        GUI.backgroundColor = Color.white;
        
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);

        // Show bounds info if available
        if (snapper.TryGetCabinetBounds(out Bounds bounds))
        {
            EditorGUILayout.LabelField("Cabinet Bounds Info", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField("Center", bounds.center.ToString("F3"));
            EditorGUILayout.LabelField("Size", bounds.size.ToString("F3"));
            EditorGUILayout.LabelField("Min (Back-Bottom-Left)", bounds.min.ToString("F3"));
            EditorGUILayout.LabelField("Max (Front-Top-Right)", bounds.max.ToString("F3"));
            EditorGUI.indentLevel--;
        }

        // Show wall info if assigned
        if (snapper.targetWall != null)
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Target Wall Info", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            
            float wallLengthM = snapper.targetWall.lengthMm * 0.001f;
            float wallHeightM = snapper.targetWall.heightMm * 0.001f;
            float wallThickM = snapper.targetWall.thicknessMm * 0.001f;
            
            EditorGUILayout.LabelField("Position", snapper.targetWall.transform.position.ToString("F3"));
            EditorGUILayout.LabelField("Size (m)", $"L:{wallLengthM:F3} x H:{wallHeightM:F3} x T:{wallThickM:F3}");
            
            // Calculate wall edges
            Vector3 wallCenter = snapper.targetWall.transform.position;
            float wallLeftX = wallCenter.x - (wallLengthM * 0.5f);
            float wallBottomY = wallCenter.y - (wallHeightM * 0.5f);
            float wallFrontZ = wallCenter.z + (wallThickM * 0.5f);
            
            EditorGUILayout.LabelField("Left Edge (X)", wallLeftX.ToString("F3"));
            EditorGUILayout.LabelField("Bottom (Y)", wallBottomY.ToString("F3"));
            EditorGUILayout.LabelField("Front Face (Z)", wallFrontZ.ToString("F3"));
            
            EditorGUI.indentLevel--;
        }
    }
}
