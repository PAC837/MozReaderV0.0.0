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
    [Tooltip("Material for the part cubes (optional).")]
    public Material panelMaterial;

    [Tooltip("Material for the bounding box cube (optional, e.g. transparent outline).")]
    public Material boundsMaterial;

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

        data.ElevationMm = cab.ElevationMm;
        data.XPositionMm = cab.XPositionMm;
        data.WallRef = cab.WallRef;
    }

    private GameObject SpawnPart(Transform root, MozPart part)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = string.IsNullOrEmpty(part.Name) ? "Part" : part.Name;
        go.transform.SetParent(root, false);

        float sx = part.PartL * MM_TO_M;       // length along X
        float sy = part.Thickness * MM_TO_M;   // thickness along Y
        float sz = part.PartW * MM_TO_M;       // depth along Z

        go.transform.localScale = new Vector3(
            Mathf.Abs(sx),
            Mathf.Abs(sy),
            Mathf.Abs(sz)
        );

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

        go.transform.localRotation = mozRot;
        go.transform.localPosition = mozPos + mozRot * half;

        if (panelMaterial != null)
        {
            Renderer r = go.GetComponent<Renderer>();
            if (r != null)
                r.sharedMaterial = panelMaterial;
        }

        return go;
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
