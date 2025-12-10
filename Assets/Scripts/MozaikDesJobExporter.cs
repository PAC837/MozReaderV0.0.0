using System.IO;
using System.Xml;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Exports Mozaik .des/.sbk room file directly inside a Mozaik job.
/// Attach this to a "RoomRoot" GameObject.
/// Workflow:
///  1) Right-click on component header > Pick Mozaik Room File (Room 1.des).
///  2) Add MozaikWall components somewhere in the scene.
///  3) Right-click > Export Walls Into Room File.
/// </summary>
public class MozaikDesJobExporter : MonoBehaviour
{
    [Header("Mozaik Room File (.des or .sbk)")]
    [Tooltip("Full path to Room 1.des (or .sbk) inside a Mozaik job folder.")]
    public string roomFilePath;

    [Header("Wall Settings")]
    [Tooltip("Starting IDTag / WallNumber index.")]
    public int startingWallId = 1;

#if UNITY_EDITOR
    [ContextMenu("Pick Mozaik Room File")]
    private void PickRoomFile()
    {
        string selected = EditorUtility.OpenFilePanel(
            "Select Room 1.des (or .sbk)",
            "",
            "des,sbk"
        );

        if (!string.IsNullOrEmpty(selected))
        {
            roomFilePath = selected;
            EditorUtility.SetDirty(this);
            Debug.Log("[MozaikDesJobExporter] Room file set to: " + roomFilePath);
        }
    }

    [ContextMenu("Export Walls Into Room File")]
    private void ExportWallsIntoRoomFileMenu()
    {
        ExportWallsIntoRoomFile();
    }
#endif

    public void ExportWallsIntoRoomFile()
    {
        if (string.IsNullOrEmpty(roomFilePath))
        {
            Debug.LogError("[MozaikDesJobExporter] roomFilePath is empty. Use 'Pick Mozaik Room File' first.");
            return;
        }

        if (!File.Exists(roomFilePath))
        {
            Debug.LogError("[MozaikDesJobExporter] File does not exist: " + roomFilePath);
            return;
        }

        // 1. Read the existing DES/SBK
        string raw = File.ReadAllText(roomFilePath);
        raw = raw.Replace("\r\n", "\n");
        string[] lines = raw.Split('\n');
        if (lines.Length < 2)
        {
            Debug.LogError("[MozaikDesJobExporter] Room file looks invalid (too few lines).");
            return;
        }

        string headerLine = lines[0].Trim();               // e.g. "15"
        string xmlBody    = string.Join("\n", lines, 1, lines.Length - 1);

        // 2. Parse XML
        var doc = new XmlDocument();
        try
        {
            doc.LoadXml(xmlBody);
        }
        catch (System.Exception ex)
        {
            Debug.LogError("[MozaikDesJobExporter] Failed to parse XML: " + ex.Message);
            return;
        }

        XmlElement room = doc.DocumentElement;
        if (room == null || room.Name != "Room")
        {
            Debug.LogError("[MozaikDesJobExporter] Root element is not <Room>.");
            return;
        }

        // 3. Get or create <Walls> node
        XmlElement wallsNode = room["Walls"];
        if (wallsNode == null)
        {
            wallsNode = doc.CreateElement("Walls");
            room.AppendChild(wallsNode);
        }
        else
        {
            wallsNode.RemoveAll(); // wipe old walls
        }

        // 4. Collect all MozaikWall components in the scene
        MozaikWall[] unityWalls;

#if UNITY_2023_1_OR_NEWER
        unityWalls = Object.FindObjectsByType<MozaikWall>(FindObjectsSortMode.None);
#else
        unityWalls = Object.FindObjectsOfType<MozaikWall>();
#endif

        Debug.Log("[MozaikDesJobExporter] Found " + unityWalls.Length + " MozaikWall(s) in the scene.");

        // Counter for IDTag / WallNumber
        int id = startingWallId;

        foreach (MozaikWall w in unityWalls)
        {
            if (w == null || !w.isActiveAndEnabled) 
                continue;

            // Get endpoints in world space (meters)
            w.GetWorldEndpoints(out Vector3 worldStartM, out Vector3 worldEndM);

            // Convert to Mozaik plan (mm)
            // ASSUMPTION right now:
            //   Mozaik X = Unity X * 1000
            //   Mozaik Y = Unity Z * 1000
            Vector2 startPlanMm = UnityWorldToMozaikPlan(worldStartM);
            Vector2 endPlanMm   = UnityWorldToMozaikPlan(worldEndM);

            float dx    = endPlanMm.x - startPlanMm.x;
            float dy    = endPlanMm.y - startPlanMm.y;
            float lenMm = Mathf.Sqrt(dx * dx + dy * dy);
            float angDeg = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;

            XmlElement wallEl = doc.CreateElement("Wall");
            wallEl.SetAttribute("IDTag", id.ToString());
            wallEl.SetAttribute("WallNumber", id.ToString());
            wallEl.SetAttribute("Len", lenMm.ToString("F3"));
            wallEl.SetAttribute("Height", w.heightMm.ToString("F3"));
            wallEl.SetAttribute("PosX", startPlanMm.x.ToString("F3"));
            wallEl.SetAttribute("PosY", startPlanMm.y.ToString("F3"));
            wallEl.SetAttribute("Ang", angDeg.ToString("F3"));
            wallEl.SetAttribute("Thickness", w.thicknessMm.ToString("F3"));

            // Defaults (tune later if needed)
            wallEl.SetAttribute("Invisible", "False");
            wallEl.SetAttribute("SUDirty", "True");
            wallEl.SetAttribute("Bulge", "0");
            wallEl.SetAttribute("HasLeftEndCap", "False");
            wallEl.SetAttribute("HasRightEndCap", "False");
            wallEl.SetAttribute("HasBackSpace", "False");
            wallEl.SetAttribute("HasLeftSpace", "False");
            wallEl.SetAttribute("HasRightSpace", "False");
            wallEl.SetAttribute("LeftSpaceLength", "0");
            wallEl.SetAttribute("RightSpaceLength", "0");
            wallEl.SetAttribute("ShapeType", "0");
            wallEl.SetAttribute("CathedralHeight", "0");

            XmlElement labelOverride = doc.CreateElement("LabelDimensionOverrideMap");
            wallEl.AppendChild(labelOverride);

            wallsNode.AppendChild(wallEl);
            id++;
        }

        // 5. Update IdTagCount (optional)
        room.SetAttribute("IdTagCount", id.ToString());

        // 6. Serialize XML back to string
        string xmlOut;
        using (var sw = new StringWriter())
        {
            using (var xw = new XmlTextWriter(sw) { Formatting = Formatting.Indented })
            {
                doc.WriteTo(xw);
            }
            xmlOut = sw.ToString();
        }

        // 7. Combine header + XML
        string finalText = headerLine + "\r\n" + xmlOut;

        // 8. Backup original file
        string backupPath = roomFilePath + ".FROMUNITY.bak";
        File.Copy(roomFilePath, backupPath, true);

        // 9. Overwrite Room 1.des
        File.WriteAllText(roomFilePath, finalText);

        Debug.Log("[MozaikDesJobExporter] Exported walls into: " + roomFilePath);
    }

    /// <summary>
    /// Unity world (meters) → Mozaik plan (mm).
    /// Right now: X -> X, Z -> Y. 1m = 1000mm.
    /// Swap/invert later to match MozImporter calibration exactly.
    /// </summary>
    private static Vector2 UnityWorldToMozaikPlan(Vector3 worldPosM)
    {
        float xMm = worldPosM.x * 1000f;
        float yMm = worldPosM.z * 1000f;
        return new Vector2(xMm, yMm);
    }
}
