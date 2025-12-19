using System;
using UnityEngine;

/// <summary>
/// Alternative Mozaik importer that:
/// 1) Imports parts like MozImporter
/// 2) Adds a bounding box GameObject around all parts
///    that represents total width, depth, and height.
/// 3) Auto-snaps to selected wall via RuntimeWallSelector (if available)
/// Uses MozCabinet, MozPart, MozParser, MozCoordinateMapper from MozImporter.cs.
/// </summary>
public class MozImporterBounds : MonoBehaviour
{
    [Header("Input")]
    [Tooltip("Mozaik .moz file as a TextAsset (e.g. CALIB_AXES_FACES_01.moz.txt).")]
    public TextAsset mozFile;

    [Header("Visuals")]
    [Tooltip("Material for the part cubes (optional - will create URP Lit if not set).")]
    public Material panelMaterial;

    [Tooltip("Material for the bounding box cube (optional, e.g. transparent outline).")]
    public Material boundsMaterial;

    [Tooltip("Default color for auto-created URP Lit material.")]
    public Color defaultPanelColor = new Color(0.9f, 0.9f, 0.85f, 1f); // Light beige

    [Header("Part Visibility")]
    [Tooltip("Part names to hide in Unity (data still kept for export). Case-insensitive contains match.")]
    public string[] hiddenPartNames = new string[] { "Linear Light", "LED" };

    [Header("Auto-Snap Settings")]
    [Tooltip("Automatically snap imported cabinets to the currently selected wall.")]
    public bool autoSnapToSelectedWall = true;

    [Tooltip("Add CabinetWallSnapper component to imported cabinets.")]
    public bool addWallSnapperComponent = true;

    private const float MM_TO_M = 0.001f;

    [ContextMenu("Import Moz With Bounds")]
    public void ImportWithBounds()
    {
        if (mozFile == null)
        {
            Debug.LogError("[MozImporterBounds] No mozFile assigned.");
            return;
        }

        MozCabinet cab;
        try
        {
            cab = MozParser.ParseMozFromText(mozFile.text);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[MozImporterBounds] Failed to parse .moz: {ex.Message}");
            return;
        }

        if (cab == null || cab.Parts.Count == 0)
        {
            Debug.LogWarning("[MozImporterBounds] Parsed cabinet has no parts.");
            return;
        }

        string rootName = string.IsNullOrEmpty(cab.Name) ? "MozCabinet_WithBounds" : cab.Name + "_WithBounds";
        GameObject root = new GameObject(rootName);
        root.transform.position = Vector3.zero;
        root.transform.rotation = Quaternion.identity;

        bool hasBounds = false;
        Bounds totalBounds = new Bounds();

        foreach (MozPart part in cab.Parts)
        {
            GameObject partGO = SpawnPart(root.transform, part);

            // ONLY include visible parts in bounds calculation
            // Hidden parts (LED, Linear Light) should NOT affect bounding box
            if (!ShouldHidePart(part.Name))
            {
                Renderer r = partGO.GetComponent<Renderer>();
                if (r != null)
                {
                    if (!hasBounds)
                    {
                        totalBounds = r.bounds;
                        hasBounds = true;
                    }
                    else
                    {
                        totalBounds.Encapsulate(r.bounds);
                    }
                }
            }
        }

        if (hasBounds)
        {
            GameObject boundsGO = CreateBoundsObject(root.transform, totalBounds);

            // Attach MozCabinetData component and populate from parsed data
            MozCabinetData cabinetData = root.AddComponent<MozCabinetData>();
            PopulateCabinetData(cabinetData, cab);

            // Attach MozBoundsHighlighter for selection visibility
            MozBoundsHighlighter highlighter = root.AddComponent<MozBoundsHighlighter>();
            if (boundsGO != null)
            {
                highlighter.boundsRenderer = boundsGO.GetComponent<Renderer>();
            }

            // Auto-snap to selected wall if enabled
            if (addWallSnapperComponent || autoSnapToSelectedWall)
            {
                SetupWallSnapping(root, cabinetData);
            }

            Debug.Log($"[MozImporterBounds] Imported {cab.Parts.Count} parts into '{rootName}' with bounds size {totalBounds.size}.\n" +
                      $"  Elevation: {cab.ElevationMm}mm, Dimensions: {cab.WidthMm}x{cab.HeightMm}x{cab.DepthMm}mm");
        }
        else
        {
            Debug.LogWarning("[MozImporterBounds] No renderer bounds found for parts.");
        }
    }

    /// <summary>
    /// Populates MozCabinetData component from parsed MozCabinet data.
    /// </summary>
    private void PopulateCabinetData(MozCabinetData data, MozCabinet cab)
    {
        data.UniqueID = cab.UniqueID;
        data.ProductName = cab.Name;
        data.SourceLibrary = cab.SourceLibrary;

        data.WidthMm = cab.WidthMm;
        data.HeightMm = cab.HeightMm;
        data.DepthMm = cab.DepthMm;

        // Product type classification
        data.ProductType = cab.ProductType;
        data.ProductSubType = cab.ProductSubType;
        data.ProductSubSubType = cab.ProductSubSubType;

        // Construction settings
        data.CurrentConst = cab.CurrentConst;
        data.Flags = cab.Flags;

        // Shape data for roundtrip
        data.TopShapeXml = cab.TopShapeXml;

        // Parts data for roundtrip (shelves, rods, hangers)
        data.CabProdPartsXml = cab.CabProdPartsXml;

        // Interior layout for roundtrip (Section definitions)
        data.ProductInteriorXml = cab.ProductInteriorXml;
        data.CabProdParmsXml = cab.CabProdParmsXml;

        data.ElevationMm = cab.ElevationMm;
        data.XPositionMm = cab.XPositionMm;
        data.WallRef = cab.WallRef;
    }

    private GameObject SpawnPart(Transform root, MozPart part)
    {
        string partName = string.IsNullOrEmpty(part.Name) ? "Part" : part.Name;
        bool isRodOrCylinder = IsRodPart(partName);
        bool isMetalPart = IsMetalPart(partName);
        
        // Spawn cylinder for rods, cube for panels
        GameObject go;
        if (isRodOrCylinder)
        {
            go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Debug.Log($"[MozImporterBounds] Spawning CYLINDER for rod part: {partName}");
        }
        else
        {
            go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        }
        
        go.name = partName;
        go.transform.SetParent(root, false);

        float sx = part.PartL * MM_TO_M;       // length along X
        float sy = part.Thickness * MM_TO_M;   // thickness along Y
        float sz = part.PartW * MM_TO_M;       // depth along Z

        if (isRodOrCylinder)
        {
            // For cylinders: Unity cylinder is 2 units tall, 1 unit diameter
            // We want length along X axis, diameter based on thickness/width
            float diameter = Mathf.Max(Mathf.Abs(sy), Mathf.Abs(sz)) * 0.5f; // Half because cylinder is 1 unit diameter = 0.5 radius
            float length = Mathf.Abs(sx) * 0.5f; // Half because cylinder is 2 units tall
            go.transform.localScale = new Vector3(diameter, length, diameter);
            // Rotate to make length along X instead of Y
            go.transform.localRotation = Quaternion.Euler(0, 0, 90);
        }
        else
        {
            go.transform.localScale = new Vector3(
                Mathf.Abs(sx),
                Mathf.Abs(sy),
                Mathf.Abs(sz)
            );
        }

        Vector3 mozPos = MozCoordinateMapper.PositionMmToUnity(part.X, part.Y, part.Z);
        Quaternion mozRot = MozCoordinateMapper.RotationFromMoz(
            part.R1, part.A1,
            part.R2, part.A2,
            part.R3, part.A3
        );

        Vector3 half = new Vector3(
            sx * 0.5f,
            sy * 0.5f,
            -(sz * 0.5f)
        );

        if (!isRodOrCylinder)
        {
            go.transform.localRotation = mozRot;
        }
        else
        {
            // For rods, apply Mozaik rotation then our cylinder fix rotation
            go.transform.localRotation = mozRot * Quaternion.Euler(0, 0, 90);
        }
        go.transform.localPosition = mozPos + mozRot * half;

        // Apply material based on part type
        Renderer r = go.GetComponent<Renderer>();
        if (r != null)
        {
            Material mat = GetMaterialForPart(partName, isMetalPart);
            if (mat != null)
            {
                r.sharedMaterial = mat;
            }
            else
            {
                Debug.LogWarning($"[MozImporterBounds] Could not get material for '{partName}' - will be pink!");
            }
        }

        // Hide part if it matches any hidden name (data still kept for export)
        if (ShouldHidePart(part.Name))
        {
            go.SetActive(false);
            Debug.Log($"[MozImporterBounds] Hidden part '{part.Name}' (data preserved for export)");
        }

        return go;
    }

    /// <summary>
    /// Checks if part name indicates a rod/cylindrical part.
    /// </summary>
    private bool IsRodPart(string partName)
    {
        if (string.IsNullOrEmpty(partName)) return false;
        string lower = partName.ToLowerInvariant();
        return lower.Contains("rod") || lower.Contains("closetrod");
    }

    /// <summary>
    /// Checks if part name indicates a metal part (rods, hangers, hardware).
    /// </summary>
    private bool IsMetalPart(string partName)
    {
        if (string.IsNullOrEmpty(partName)) return false;
        string lower = partName.ToLowerInvariant();
        return lower.Contains("rod") || 
               lower.Contains("hanger") || 
               lower.Contains("hardware") ||
               lower.Contains("metal") ||
               lower.Contains("chrome");
    }

    /// <summary>
    /// Gets the appropriate material for a part based on type.
    /// </summary>
    private Material GetMaterialForPart(string partName, bool isMetal)
    {
        // If we have a panel material assigned, use it for non-metal parts
        if (!isMetal && panelMaterial != null)
        {
            return panelMaterial;
        }

        // Try to get from TextureLibraryManager
        if (TextureLibraryManager.Instance != null)
        {
            if (isMetal)
            {
                Material chrome = TextureLibraryManager.Instance.GetDefaultChromeMaterial();
                if (chrome != null)
                {
                    Debug.Log($"[MozImporterBounds] Using chrome material for metal part: {partName}");
                    return chrome;
                }
            }
            else
            {
                Material cabinet = TextureLibraryManager.Instance.GetDefaultCabinetMaterial();
                if (cabinet != null)
                {
                    Debug.Log($"[MozImporterBounds] Using default cabinet material for: {partName}");
                    return cabinet;
                }
            }
        }

        // Fallback: create our own URP material
        return CreateDefaultUrpMaterial();
    }

    /// <summary>
    /// Creates a default URP Lit material with the configured color.
    /// Falls back to Simple Lit, then Unlit if URP Lit not found.
    /// </summary>
    private Material CreateDefaultUrpMaterial()
    {
        // Try URP Lit shader first
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        
        // Fall back to Simple Lit
        if (shader == null)
        {
            shader = Shader.Find("Universal Render Pipeline/Simple Lit");
        }
        
        // Fall back to Unlit
        if (shader == null)
        {
            shader = Shader.Find("Universal Render Pipeline/Unlit");
        }

        if (shader == null)
        {
            Debug.LogWarning("[MozImporterBounds] No URP shader found - parts will use default pink material.");
            return null;
        }

        Material mat = new Material(shader);
        mat.color = defaultPanelColor;
        mat.name = "AutoCreatedPanelMaterial";
        
        return mat;
    }

    /// <summary>
    /// Checks if a part should be hidden in Unity display.
    /// </summary>
    private bool ShouldHidePart(string partName)
    {
        if (string.IsNullOrEmpty(partName) || hiddenPartNames == null)
            return false;

        string nameLower = partName.ToLowerInvariant().Trim();
        
        foreach (string hidden in hiddenPartNames)
        {
            if (!string.IsNullOrEmpty(hidden) && nameLower.Contains(hidden.ToLowerInvariant().Trim()))
            {
                return true;
            }
        }
        
        return false;
    }

    private GameObject CreateBoundsObject(Transform root, Bounds totalBounds)
    {
        GameObject boundsGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
        boundsGO.name = "Bounds";
        boundsGO.transform.SetParent(root, false);

        boundsGO.transform.position = totalBounds.center;
        boundsGO.transform.localScale = totalBounds.size;

        if (boundsMaterial != null)
        {
            Renderer r = boundsGO.GetComponent<Renderer>();
            if (r != null)
                r.sharedMaterial = boundsMaterial;
        }

        Collider col = boundsGO.GetComponent<Collider>();
        if (col != null)
            col.isTrigger = true;

        Debug.Log($"[MozImporterBounds] Bounds center: {totalBounds.center}, size: {totalBounds.size}");

        return boundsGO;
    }

    /// <summary>
    /// Sets up wall snapping for the imported cabinet.
    /// - Adds CabinetWallSnapper component
    /// - Assigns selected wall from RuntimeWallSelector
    /// - Optionally auto-snaps to the wall
    /// </summary>
    private void SetupWallSnapping(GameObject cabinetRoot, MozCabinetData cabinetData)
    {
        // Add CabinetWallSnapper component
        CabinetWallSnapper snapper = null;
        
        if (addWallSnapperComponent)
        {
            snapper = cabinetRoot.AddComponent<CabinetWallSnapper>();
        }

        // Find the selected wall
        MozaikWall selectedWall = null;
        
        // Try to get from RuntimeWallSelector singleton
        if (RuntimeWallSelector.Instance != null)
        {
            selectedWall = RuntimeWallSelector.Instance.SelectedWall;
        }
        else
        {
            // Fall back to finding one in the scene
            RuntimeWallSelector wallSelector = FindFirstObjectByType<RuntimeWallSelector>();
            if (wallSelector != null)
            {
                selectedWall = wallSelector.SelectedWall;
            }
        }

        // If we have a snapper and a wall, set it up
        if (snapper != null && selectedWall != null)
        {
            snapper.targetWall = selectedWall;

            // Auto-snap if enabled
            if (autoSnapToSelectedWall)
            {
                // Need to wait a frame for bounds to be calculated properly
                // Use a coroutine or delay if in play mode, otherwise do it immediately
                if (Application.isPlaying)
                {
                    // Schedule snap for next frame
                    StartCoroutine(DelayedSnap(snapper));
                }
                else
                {
                    // In editor mode, snap immediately
                    snapper.SnapToWall();
                }

                Debug.Log($"[MozImporterBounds] Auto-snapped '{cabinetRoot.name}' to wall '{selectedWall.name}'");
            }
            else
            {
                Debug.Log($"[MozImporterBounds] CabinetWallSnapper added to '{cabinetRoot.name}', target wall: '{selectedWall.name}' (not auto-snapped)");
            }
        }
        else if (snapper != null)
        {
            Debug.Log($"[MozImporterBounds] CabinetWallSnapper added to '{cabinetRoot.name}' (no wall selected - assign manually or select a wall first)");
        }

        // Update cabinet data with target wall reference
        if (cabinetData != null && selectedWall != null)
        {
            cabinetData.TargetWall = selectedWall;
        }
    }

    /// <summary>
    /// Coroutine to snap after a frame delay (allows bounds to calculate properly).
    /// </summary>
    private System.Collections.IEnumerator DelayedSnap(CabinetWallSnapper snapper)
    {
        yield return null; // Wait one frame
        
        if (snapper != null)
        {
            snapper.SnapToWall();
        }
    }
}
