using System.IO;
using System.Xml;
using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Exports Unity room data (walls + cabinets) to Mozaik .des/.sbk room file.
/// 
/// Workflow:
///  1) Right-click > Pick Mozaik Job Folder (select the job folder containing Room1.des)
///  2) Add MozaikWall components for walls in scene
///  3) Import cabinets with MozImporterBounds and snap them to walls
///  4) Right-click > Export Room to Mozaik
/// 
/// The exporter will:
///  - Update <Walls> with all MozaikWall components
///  - Update <Products> with all GameObjects that have MozCabinetData
///  - Preserve all other DES file content (RoomSet, materials, etc.)
/// </summary>
public class MozaikDesJobExporter : MonoBehaviour
{
    [Header("Mozaik Job Folder")]
    [Tooltip("Path to the Mozaik job folder (containing Room1.des).")]
    public string mozaikJobFolder;

    [Tooltip("Room file name (typically Room1.des or Room1.sbk).")]
    public string roomFileName = "Room1.des";

    [Header("Wall Settings")]
    [Tooltip("Starting IDTag / WallNumber index.")]
    public int startingWallId = 1;

    [Header("Export Options")]
    [Tooltip("Export walls to the DES file.")]
    public bool exportWalls = true;

    [Tooltip("Export products/cabinets to the DES file.")]
    public bool exportProducts = true;

    [Tooltip("Create backup before overwriting.")]
    public bool createBackup = true;

    /// <summary>
    /// Gets the full path to the room file.
    /// </summary>
    public string RoomFilePath
    {
        get
        {
            if (string.IsNullOrEmpty(mozaikJobFolder))
                return string.Empty;
            return Path.Combine(mozaikJobFolder, roomFileName);
        }
    }

    // Wall ID to export, used for cabinet wall references
    private Dictionary<MozaikWall, int> _wallIdMap = new Dictionary<MozaikWall, int>();

#if UNITY_EDITOR
    [ContextMenu("Pick Mozaik Job Folder")]
    private void PickJobFolder()
    {
        string selected = EditorUtility.OpenFolderPanel(
            "Select Mozaik Job Folder",
            "",
            ""
        );

        if (!string.IsNullOrEmpty(selected))
        {
            mozaikJobFolder = selected;
            
            // Check for Room1.des
            string desPath = Path.Combine(selected, "Room1.des");
            string sbkPath = Path.Combine(selected, "Room1.sbk");
            
            if (File.Exists(desPath))
            {
                roomFileName = "Room1.des";
                Debug.Log($"[MozaikDesJobExporter] Found Room1.des in folder.");
            }
            else if (File.Exists(sbkPath))
            {
                roomFileName = "Room1.sbk";
                Debug.Log($"[MozaikDesJobExporter] Found Room1.sbk in folder.");
            }
            else
            {
                Debug.LogWarning($"[MozaikDesJobExporter] No Room1.des or Room1.sbk found in folder. " +
                                 $"You may need to create a new room in Mozaik first.");
            }
            
            EditorUtility.SetDirty(this);
            Debug.Log($"[MozaikDesJobExporter] Job folder set to: {mozaikJobFolder}");
        }
    }

    [ContextMenu("Pick Room File Directly")]
    private void PickRoomFile()
    {
        string selected = EditorUtility.OpenFilePanel(
            "Select Room File (.des or .sbk)",
            mozaikJobFolder,
            "des,sbk"
        );

        if (!string.IsNullOrEmpty(selected))
        {
            mozaikJobFolder = Path.GetDirectoryName(selected);
            roomFileName = Path.GetFileName(selected);
            EditorUtility.SetDirty(this);
            Debug.Log($"[MozaikDesJobExporter] Room file set to: {selected}");
        }
    }

    [ContextMenu("Export Room to Mozaik")]
    private void ExportRoomToMozaikMenu()
    {
        ExportRoomToMozaik();
    }

    [ContextMenu("Open Job Folder in Explorer")]
    private void OpenJobFolderInExplorer()
    {
        if (!string.IsNullOrEmpty(mozaikJobFolder) && Directory.Exists(mozaikJobFolder))
        {
            System.Diagnostics.Process.Start("explorer.exe", mozaikJobFolder);
        }
        else
        {
            Debug.LogWarning("[MozaikDesJobExporter] No valid job folder set.");
        }
    }
#endif

    /// <summary>
    /// Main export method - exports walls and products to Mozaik DES file.
    /// </summary>
    public void ExportRoomToMozaik()
    {
        string roomPath = RoomFilePath;
        
        if (string.IsNullOrEmpty(roomPath))
        {
            Debug.LogError("[MozaikDesJobExporter] No job folder set. Use 'Pick Mozaik Job Folder' first.");
            return;
        }

        if (!File.Exists(roomPath))
        {
            Debug.LogError($"[MozaikDesJobExporter] Room file does not exist: {roomPath}\n" +
                           "Create a new job in Mozaik first, or check the folder path.");
            return;
        }

        // 1. Read the existing DES/SBK
        string raw = File.ReadAllText(roomPath);
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
            Debug.LogError($"[MozaikDesJobExporter] Failed to parse XML: {ex.Message}");
            return;
        }

        XmlElement room = doc.DocumentElement;
        if (room == null || room.Name != "Room")
        {
            Debug.LogError("[MozaikDesJobExporter] Root element is not <Room>.");
            return;
        }

        int totalIdCount = startingWallId;
        _wallIdMap.Clear();

        // 3. Export Walls
        if (exportWalls)
        {
            totalIdCount = ExportWallsToXml(doc, room, totalIdCount);
        }

        // 4. Export Products
        if (exportProducts)
        {
            totalIdCount = ExportProductsToXml(doc, room, totalIdCount);
        }

        // 5. Update IdTagCount
        room.SetAttribute("IdTagCount", totalIdCount.ToString());

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
        if (createBackup)
        {
            string backupPath = roomPath + ".FROMUNITY.bak";
            File.Copy(roomPath, backupPath, true);
            Debug.Log($"[MozaikDesJobExporter] Backup created: {backupPath}");
        }

        // 9. Overwrite Room file
        File.WriteAllText(roomPath, finalText);

        Debug.Log($"[MozaikDesJobExporter] Export complete!\n" +
                  $"  File: {roomPath}\n" +
                  $"  Walls: {_wallIdMap.Count}\n" +
                  $"  Products: {(exportProducts ? "exported" : "skipped")}");
    }

    /// <summary>
    /// Export all MozaikWall components to the XML document.
    /// </summary>
    private int ExportWallsToXml(XmlDocument doc, XmlElement room, int startId)
    {
        // Get or create <Walls> node
        XmlElement wallsNode = room["Walls"];
        if (wallsNode == null)
        {
            wallsNode = doc.CreateElement("Walls");
            // Insert after RoomSet
            XmlNode roomSet = room["RoomSet"];
            if (roomSet != null)
                room.InsertAfter(wallsNode, roomSet);
            else
                room.AppendChild(wallsNode);
        }
        else
        {
            wallsNode.RemoveAll(); // wipe old walls
        }

        // Collect all MozaikWall components in the scene
        MozaikWall[] unityWalls;

#if UNITY_2023_1_OR_NEWER
        unityWalls = Object.FindObjectsByType<MozaikWall>(FindObjectsSortMode.None);
#else
        unityWalls = Object.FindObjectsOfType<MozaikWall>();
#endif

        Debug.Log($"[MozaikDesJobExporter] Found {unityWalls.Length} MozaikWall(s) in scene.");

        int id = startId;

        foreach (MozaikWall w in unityWalls)
        {
            if (w == null || !w.isActiveAndEnabled) 
                continue;

            // Store wall ID for product references
            _wallIdMap[w] = id;

            // Get endpoints in world space (meters)
            w.GetWorldEndpoints(out Vector3 worldStartM, out Vector3 worldEndM);

            // Convert to Mozaik plan (mm)
            Vector2 startPlanMm = UnityWorldToMozaikPlan(worldStartM);
            Vector2 endPlanMm   = UnityWorldToMozaikPlan(worldEndM);

            float dx    = endPlanMm.x - startPlanMm.x;
            float dy    = endPlanMm.y - startPlanMm.y;
            float lenMm = Mathf.Sqrt(dx * dx + dy * dy);
            float angDeg = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;

            XmlElement wallEl = doc.CreateElement("Wall");
            wallEl.SetAttribute("IDTag", id.ToString());
            wallEl.SetAttribute("WallNumber", id.ToString());
            wallEl.SetAttribute("Len", lenMm.ToString("F1"));
            wallEl.SetAttribute("Height", w.heightMm.ToString("F1"));
            wallEl.SetAttribute("PosX", startPlanMm.x.ToString("F1"));
            wallEl.SetAttribute("PosY", startPlanMm.y.ToString("F1"));
            wallEl.SetAttribute("Ang", angDeg.ToString("F1"));
            wallEl.SetAttribute("Thickness", w.thicknessMm.ToString("F1"));

            // Required defaults
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
            
            Debug.Log($"[MozaikDesJobExporter] Wall {id}: {w.gameObject.name} - Len={lenMm:F1}mm, Pos=({startPlanMm.x:F1}, {startPlanMm.y:F1})");
            
            id++;
        }

        return id;
    }

    /// <summary>
    /// Export all MozCabinetData components to the XML document.
    /// </summary>
    private int ExportProductsToXml(XmlDocument doc, XmlElement room, int startId)
    {
        // Get or create <Products> node
        XmlElement productsNode = room["Products"];
        if (productsNode == null)
        {
            productsNode = doc.CreateElement("Products");
            // Insert after Fixts
            XmlNode fixts = room["Fixts"];
            if (fixts != null)
                room.InsertAfter(productsNode, fixts);
            else
                room.AppendChild(productsNode);
        }
        else
        {
            productsNode.RemoveAll(); // wipe old products
        }

        // Collect all MozCabinetData components in the scene
        MozCabinetData[] cabinets;

#if UNITY_2023_1_OR_NEWER
        cabinets = Object.FindObjectsByType<MozCabinetData>(FindObjectsSortMode.None);
#else
        cabinets = Object.FindObjectsOfType<MozCabinetData>();
#endif

        Debug.Log($"[MozaikDesJobExporter] Found {cabinets.Length} cabinet(s) with MozCabinetData.");

        int id = startId;
        int cabNo = 1;

        foreach (MozCabinetData cab in cabinets)
        {
            if (cab == null || !cab.isActiveAndEnabled) 
                continue;

            // Create Product element with minimal required attributes
            XmlElement prodEl = doc.CreateElement("Product");

            // Use existing UniqueID if available, otherwise generate
            int uniqueId = cab.UniqueID > 0 ? cab.UniqueID : GenerateUniqueId();
            prodEl.SetAttribute("UniqueID", uniqueId.ToString());
            
            prodEl.SetAttribute("OrderID", "0");
            prodEl.SetAttribute("ProdName", string.IsNullOrEmpty(cab.ProductName) ? "Cabinet" : cab.ProductName);
            prodEl.SetAttribute("IDTag", id.ToString());
            prodEl.SetAttribute("ProductDesc", "");
            prodEl.SetAttribute("SourceLib", string.IsNullOrEmpty(cab.SourceLibrary) ? "" : cab.SourceLibrary);
            prodEl.SetAttribute("NonGraphic", "False");
            prodEl.SetAttribute("ImageFile", "");
            prodEl.SetAttribute("SketchUpFile", "");
            prodEl.SetAttribute("UseSUModel", "0");
            prodEl.SetAttribute("AutoFill", "False");
            prodEl.SetAttribute("AutoDimension", "True");
            prodEl.SetAttribute("Mirror", "True");
            prodEl.SetAttribute("SUDirty", "True");
            prodEl.SetAttribute("OrderDirty", "True");

            // Dimensions
            prodEl.SetAttribute("Width", cab.WidthMm.ToString("F1"));
            prodEl.SetAttribute("Height", cab.HeightMm.ToString("F1"));
            prodEl.SetAttribute("Depth", cab.DepthMm.ToString("F1"));

            // Stretch settings
            prodEl.SetAttribute("WStretch", "False");
            prodEl.SetAttribute("HStretch", "False");
            prodEl.SetAttribute("DStretch", "False");
            prodEl.SetAttribute("minW", "0");
            prodEl.SetAttribute("minH", "0");
            prodEl.SetAttribute("minD", "0");
            prodEl.SetAttribute("MaxW", "0");
            prodEl.SetAttribute("MaxH", "0");
            prodEl.SetAttribute("MaxD", "0");
            prodEl.SetAttribute("ModularWidths", "False");
            prodEl.SetAttribute("ModularHeights", "False");
            prodEl.SetAttribute("ModularDepths", "False");
            prodEl.SetAttribute("ModularFFHeights", "False");

            // Position - this is the key part!
            // Update cabinet position from current world position if it has a target wall
            if (cab.TargetWall != null)
            {
                cab.UpdateXPositionFromWorld();
                cab.UpdateElevationFromWorld();
            }

            prodEl.SetAttribute("X", cab.XPositionMm.ToString("F2"));
            prodEl.SetAttribute("Elev", cab.ElevationMm.ToString("F0"));
            prodEl.SetAttribute("Rot", "0"); // TODO: Calculate from transform rotation
            prodEl.SetAttribute("Outset", "0");

            // Wall reference - format is "WallIDTag_1"
            string wallRef = GetWallReference(cab);
            prodEl.SetAttribute("Wall", wallRef);

            prodEl.SetAttribute("CabNo", cabNo.ToString());
            prodEl.SetAttribute("Numbered", "True");
            prodEl.SetAttribute("SnapTo", "0");
            prodEl.SetAttribute("IsRectShape", "True");

            // Many override fields (empty = use defaults)
            prodEl.SetAttribute("DoorOR", "");
            prodEl.SetAttribute("TopDrwOR", "");
            prodEl.SetAttribute("MidDrwOR", "");
            prodEl.SetAttribute("BotDrwOR", "");
            prodEl.SetAttribute("DrwPullOR", "");
            prodEl.SetAttribute("DoorPullOR", "");
            prodEl.SetAttribute("HingeOR", "");
            prodEl.SetAttribute("GuideOR", "");
            prodEl.SetAttribute("GuideORSlowCloseOn", "False");
            prodEl.SetAttribute("GuideORSpacerState", "1");
            prodEl.SetAttribute("ShelfPinsOR", "");
            prodEl.SetAttribute("DrawerFrontFastenerOR", "");
            prodEl.SetAttribute("LockOR", "");
            prodEl.SetAttribute("LegOR", "");
            prodEl.SetAttribute("SpotLightOR", "");
            prodEl.SetAttribute("LinearLightOR", "");
            prodEl.SetAttribute("EndDoorOR", "");
            prodEl.SetAttribute("BackDoorOR", "");
            prodEl.SetAttribute("HardwareTextureORId", "-1");

            // Adjustments
            prodEl.SetAttribute("LToeAdj", "0");
            prodEl.SetAttribute("RToeAdj", "0");
            prodEl.SetAttribute("TopDepthAdj", "0");
            prodEl.SetAttribute("BottomDepthAdj", "0");
            prodEl.SetAttribute("CurrentConst", cab.CurrentConst.ToString());
            prodEl.SetAttribute("ShowElevDepth", "False");
            prodEl.SetAttribute("ExtEnds", "0");
            prodEl.SetAttribute("Flags", string.IsNullOrEmpty(cab.Flags) ? "1111111111111111" : cab.Flags);
            prodEl.SetAttribute("NoParts", "False");
            prodEl.SetAttribute("DoorStyle", "");
            prodEl.SetAttribute("FinInt", "False");
            prodEl.SetAttribute("AutomateInterior", "False");
            prodEl.SetAttribute("GrainMatched", "0");
            prodEl.SetAttribute("ScribeRefWasExterior", "True");
            prodEl.SetAttribute("PrevInteriorScribeStileWidths", "");
            prodEl.SetAttribute("PrevInteriorScribeSideThicknesses", "");
            prodEl.SetAttribute("Count", "1");
            prodEl.SetAttribute("LockConst", "True");
            prodEl.SetAttribute("EndsToFloor", "0");
            prodEl.SetAttribute("CornerVoidAssigned", "False");
            prodEl.SetAttribute("Notes", "");
            prodEl.SetAttribute("Price", "0");
            prodEl.SetAttribute("PricePerM", "0");
            prodEl.SetAttribute("PricePerSqM", "0");
            prodEl.SetAttribute("Upcharge", "0");
            prodEl.SetAttribute("Weight", "0");
            prodEl.SetAttribute("IncludeInCabCount", "True");
            prodEl.SetAttribute("IncludeInLinearCalculations", "True");
            prodEl.SetAttribute("IncludeInSqCalculations", "True");
            prodEl.SetAttribute("PricingColumn", "");
            prodEl.SetAttribute("NotchForLeftPanelEndFrontToe", "True");
            prodEl.SetAttribute("NotchForLeftPanelEndBackToe", "True");
            prodEl.SetAttribute("NotchForRightPanelEndFrontToe", "True");
            prodEl.SetAttribute("NotchForRightPanelEndBackToe", "True");
            prodEl.SetAttribute("NotchForLeftEndFrontToe", "True");
            prodEl.SetAttribute("NotchForLeftEndBackToe", "False");
            prodEl.SetAttribute("NotchForRightEndFrontToe", "True");
            prodEl.SetAttribute("NotchForRightEndBackToe", "False");
            prodEl.SetAttribute("ParmVersion", "17");

            // Add minimal child elements that Mozaik expects
            XmlElement productOptions = doc.CreateElement("ProductOptions");
            prodEl.AppendChild(productOptions);

            // CabProdParts - use stored XML from import or create empty element
            if (!string.IsNullOrEmpty(cab.CabProdPartsXml))
            {
                try
                {
                    XmlDocument tempDoc = new XmlDocument();
                    tempDoc.LoadXml(cab.CabProdPartsXml);
                    XmlNode importedNode = doc.ImportNode(tempDoc.DocumentElement, true);
                    prodEl.AppendChild(importedNode);
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"[MozaikDesJobExporter] Failed to parse CabProdPartsXml for '{cab.ProductName}': {ex.Message}. Using empty element.");
                    XmlElement cabProdParts = doc.CreateElement("CabProdParts");
                    prodEl.AppendChild(cabProdParts);
                }
            }
            else
            {
                XmlElement cabProdParts = doc.CreateElement("CabProdParts");
                prodEl.AppendChild(cabProdParts);
            }

            XmlElement cabProdParms = doc.CreateElement("CabProdParms");
            prodEl.AppendChild(cabProdParms);

            XmlElement customParmEnabledORs = doc.CreateElement("CustomParmEnabledORs");
            prodEl.AppendChild(customParmEnabledORs);

            XmlElement productDoors = doc.CreateElement("ProductDoors");
            prodEl.AppendChild(productDoors);

            XmlElement productDrawers = doc.CreateElement("ProductDrawers");
            prodEl.AppendChild(productDrawers);

            XmlElement productRolloutShelves = doc.CreateElement("ProductRolloutShelves");
            prodEl.AppendChild(productRolloutShelves);

            XmlElement jointFastenerCounts = doc.CreateElement("JointFastenerCounts");
            prodEl.AppendChild(jointFastenerCounts);

            XmlElement shelfPinCounts = doc.CreateElement("ShelfPinCounts");
            prodEl.AppendChild(shelfPinCounts);

            XmlElement productMoldings = doc.CreateElement("ProductMoldings");
            prodEl.AppendChild(productMoldings);

            // ProductInterior - use stored XML from import or create default element
            if (!string.IsNullOrEmpty(cab.ProductInteriorXml))
            {
                try
                {
                    XmlDocument tempDoc = new XmlDocument();
                    tempDoc.LoadXml(cab.ProductInteriorXml);
                    XmlNode importedNode = doc.ImportNode(tempDoc.DocumentElement, true);
                    prodEl.AppendChild(importedNode);
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"[MozaikDesJobExporter] Failed to parse ProductInteriorXml for '{cab.ProductName}': {ex.Message}. Using default.");
                    AppendDefaultProductInterior(doc, prodEl);
                }
            }
            else
            {
                AppendDefaultProductInterior(doc, prodEl);
            }

            XmlElement frontFace = doc.CreateElement("FrontFace");
            frontFace.SetAttribute("MaxSecID", "0");
            prodEl.AppendChild(frontFace);

            XmlElement leftFace = doc.CreateElement("LeftFace");
            leftFace.SetAttribute("MaxSecID", "0");
            prodEl.AppendChild(leftFace);

            XmlElement rightFace = doc.CreateElement("RightFace");
            rightFace.SetAttribute("MaxSecID", "0");
            prodEl.AppendChild(rightFace);

            XmlElement backFace = doc.CreateElement("BackFace");
            backFace.SetAttribute("MaxSecID", "0");
            prodEl.AppendChild(backFace);

            // TopShapeXml - use stored XML from import or create minimal default
            if (!string.IsNullOrEmpty(cab.TopShapeXml))
            {
                // Parse the stored XML string and import into our document
                try
                {
                    XmlDocument tempDoc = new XmlDocument();
                    tempDoc.LoadXml(cab.TopShapeXml);
                    XmlNode importedNode = doc.ImportNode(tempDoc.DocumentElement, true);
                    prodEl.AppendChild(importedNode);
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"[MozaikDesJobExporter] Failed to parse TopShapeXml for '{cab.ProductName}': {ex.Message}. Using default.");
                    AppendDefaultTopShapeXml(doc, prodEl);
                }
            }
            else
            {
                AppendDefaultTopShapeXml(doc, prodEl);
            }

            // ProductType - use values from MozCabinetData (parsed from .moz file)
            XmlElement prodType = doc.CreateElement("ProductType");
            prodType.SetAttribute("Type", cab.ProductType.ToString());
            prodType.SetAttribute("SubType", cab.ProductSubType.ToString());
            prodType.SetAttribute("SubSubType", cab.ProductSubSubType.ToString());
            prodEl.AppendChild(prodType);

            XmlElement labelDimOverride = doc.CreateElement("LabelDimensionOverrideMap");
            prodEl.AppendChild(labelDimOverride);

            productsNode.AppendChild(prodEl);
            
            Debug.Log($"[MozaikDesJobExporter] Product {id}: {cab.ProductName} - X={cab.XPositionMm:F2}mm, Elev={cab.ElevationMm:F0}mm, Wall={wallRef}");

            id++;
            cabNo++;
        }

        return id;
    }

    /// <summary>
    /// Gets wall reference string for a cabinet (e.g., "1_1").
    /// PRIORITY: TargetWall (from snapping) > WallRef (from import) > Default
    /// </summary>
    private string GetWallReference(MozCabinetData cab)
    {
        // FIRST: If cabinet has a target wall (from snapping), use that
        // This is the most reliable because user explicitly snapped to this wall
        if (cab.TargetWall != null && _wallIdMap.ContainsKey(cab.TargetWall))
        {
            int wallId = _wallIdMap[cab.TargetWall];
            return $"{wallId}_1";
        }

        // SECOND: If cabinet has WallRef from import AND it's not "0" (unassigned)
        if (!string.IsNullOrEmpty(cab.WallRef) && cab.WallRef != "0")
        {
            return cab.WallRef;
        }

        // DEFAULT: Assign to wall 1 if it exists in our map
        // Find the first wall
        foreach (var kvp in _wallIdMap)
        {
            return $"{kvp.Value}_1";
        }

        // Last resort
        return "1_1";
    }

    /// <summary>
    /// Generates a pseudo-unique ID for new products.
    /// </summary>
    private int GenerateUniqueId()
    {
        // Use timestamp-based ID to avoid collisions
        return (int)(System.DateTime.Now.Ticks % int.MaxValue);
    }

    /// <summary>
    /// Unity world (meters) → Mozaik plan (mm).
    /// X -> X, Z -> Y. 1m = 1000mm.
    /// </summary>
    private static Vector2 UnityWorldToMozaikPlan(Vector3 worldPosM)
    {
        float xMm = worldPosM.x * 1000f;
        float yMm = worldPosM.z * 1000f;
        return new Vector2(xMm, yMm);
    }

    /// <summary>
    /// Appends a default minimal TopShapeXml element when no roundtrip data is available.
    /// </summary>
    private void AppendDefaultTopShapeXml(XmlDocument doc, XmlElement prodEl)
    {
        XmlElement topShape = doc.CreateElement("TopShapeXml");
        topShape.SetAttribute("Version", "2");
        topShape.SetAttribute("Name", "");
        topShape.SetAttribute("Type", "1");
        topShape.SetAttribute("RadiusX", "0");
        topShape.SetAttribute("RadiusY", "0");
        topShape.SetAttribute("Source", "0");
        topShape.SetAttribute("Data1", "0");
        topShape.SetAttribute("Data2", "0");
        topShape.SetAttribute("RotAng", "0");
        topShape.SetAttribute("DoNotTranslateTo00", "False");
        prodEl.AppendChild(topShape);
    }

    /// <summary>
    /// Appends a default minimal ProductInterior element when no roundtrip data is available.
    /// </summary>
    private void AppendDefaultProductInterior(XmlDocument doc, XmlElement prodEl)
    {
        XmlElement productInterior = doc.CreateElement("ProductInterior");
        productInterior.SetAttribute("MaxIntSecID", "1");
        productInterior.SetAttribute("DelT", "False");
        productInterior.SetAttribute("DelB", "False");
        productInterior.SetAttribute("DelL", "False");
        productInterior.SetAttribute("DelR", "False");
        prodEl.AppendChild(productInterior);
    }
}
