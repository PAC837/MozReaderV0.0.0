using System;
using System.Collections.Generic;
using System.Globalization;
using System.Xml.Linq;
using UnityEngine;

/// <summary>
/// Imports a Mozaik .moz cabinet file and spawns simple cube parts in Unity.
/// Coordinate mapping:
///   Mozaik X (width)  → Unity X
///   Mozaik Y (depth)  → Unity -Z
///   Mozaik Z (height) → Unity Y
/// Units: mm in Mozaik, meters in Unity.
/// </summary>
public class MozImporter : MonoBehaviour
{
    [Header("Input")]
    [Tooltip("Mozaik .moz file as a TextAsset (you can drag in a .moz.txt, .xml, or .txt).")]
    public TextAsset mozFile;

    [Header("Visuals")]
    [Tooltip("Optional material to assign to all generated parts.")]
    public Material panelMaterial;

    private const float MM_TO_M = 0.001f;

    [ContextMenu("Import Moz")]
    public void Import()
    {
        if (mozFile == null)
        {
            Debug.LogError("[MozImporter] No mozFile assigned.");
            return;
        }

        MozCabinet cab;
        try
        {
            cab = MozParser.ParseMozFromText(mozFile.text);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[MozImporter] Failed to parse .moz: {ex.Message}");
            return;
        }

        if (cab == null || cab.Parts.Count == 0)
        {
            Debug.LogWarning("[MozImporter] Parsed cabinet has no parts.");
            return;
        }

        // Root object for this cabinet/job
        string rootName = string.IsNullOrEmpty(cab.Name) ? "MozCabinet" : cab.Name;
        GameObject root = new GameObject(rootName);
        root.transform.position = Vector3.zero;
        root.transform.rotation = Quaternion.identity;

        foreach (MozPart part in cab.Parts)
        {
            SpawnPart(root.transform, part);
        }

        Debug.Log($"[MozImporter] Imported {cab.Parts.Count} parts into '{rootName}'.");
    }

    private void SpawnPart(Transform root, MozPart part)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = string.IsNullOrEmpty(part.Name) ? "Part" : part.Name;
        go.transform.SetParent(root, false);

        // Dimensions in meters
        float sx = part.PartL * MM_TO_M;       // length along X
        float sy = part.Thickness * MM_TO_M;   // thickness along Y
        float sz = part.PartW * MM_TO_M;       // depth along Z

        go.transform.localScale = new Vector3(
            Mathf.Abs(sx),
            Mathf.Abs(sy),
            Mathf.Abs(sz)
        );

        // Position & rotation from Mozaik
        Vector3 mozPos = MozCoordinateMapper.PositionMmToUnity(part.X, part.Y, part.Z);
        Quaternion mozRot = MozCoordinateMapper.RotationFromMoz(
            part.R1, part.A1,
            part.R2, part.A2,
            part.R3, part.A3
        );

        // Offset because Unity cube pivot = center, Mozaik coords = front/bottom/left
        Vector3 half = new Vector3(
            sx * 0.5f,
            sy * 0.5f,
            -(sz * 0.5f) // negative so local z = 0 is front
        );

        go.transform.localRotation = mozRot;
        go.transform.localPosition = mozPos + mozRot * half;

        if (panelMaterial != null)
        {
            Renderer r = go.GetComponent<Renderer>();
            if (r != null)
                r.sharedMaterial = panelMaterial;
        }
    }
}

/// <summary>
/// Container for a cabinet/job and its parts.
/// Includes product-level metadata from the .moz file.
/// </summary>
public class MozCabinet
{
    public string Name;

    // Product-level metadata from <Product> element
    public int UniqueID;
    public string SourceLibrary;

    // Overall dimensions (mm)
    public float WidthMm;
    public float HeightMm;
    public float DepthMm;

    // Positioning data (for import/export roundtrip)
    public float ElevationMm;   // Elev attribute - distance from floor
    public float XPositionMm;   // X attribute - position along wall
    public float Rotation;      // Rot attribute
    public string WallRef;      // Wall attribute (e.g., "1_1")

    public readonly List<MozPart> Parts = new List<MozPart>();
}

/// <summary>
/// Container for a single part in a cabinet.
/// Fields correspond to attributes in the .moz file.
/// </summary>
public class MozPart
{
    public string Name;

    // Position in Mozaik job space (mm)
    public float X;
    public float Y;
    public float Z;

    // Dimensions in mm
    public float PartL;       // length along X
    public float PartW;       // depth along Y
    public float Thickness;   // thickness along Z

    // Rotation axes and angles (from .moz)
    public char R1;
    public char R2;
    public char R3;
    public float A1;
    public float A2;
    public float A3;
}

/// <summary>
/// Static helper for mapping Mozaik coordinates to Unity.
/// </summary>
public static class MozCoordinateMapper
{
    private const float MM_TO_M = 0.001f;

    public static Vector3 PositionMmToUnity(float xMm, float yMm, float zMm)
    {
        return new Vector3(
            xMm * MM_TO_M,       // Mozaik X → Unity X
            zMm * MM_TO_M,       // Mozaik Z → Unity Y
            -yMm * MM_TO_M       // Mozaik Y → Unity -Z
        );
    }

    public static Vector3 AxisFromMoz(char mozAxis)
    {
        switch (mozAxis)
        {
            case 'X':
            case 'x':
                return Vector3.right;      // (1,0,0)
            case 'Y':
            case 'y':
                return Vector3.back;       // (0,0,-1) depth
            case 'Z':
            case 'z':
                return Vector3.up;         // (0,1,0)
            default:
                return Vector3.zero;
        }
    }

    public static Quaternion RotationFromMoz(char r1, float a1,
                                             char r2, float a2,
                                             char r3, float a3)
    {
        Quaternion q1 = Quaternion.AngleAxis(a1, AxisFromMoz(r1));
        Quaternion q2 = Quaternion.AngleAxis(a2, AxisFromMoz(r2));
        Quaternion q3 = Quaternion.AngleAxis(a3, AxisFromMoz(r3));

        // Apply R1 then R2 then R3 (verify with calibration rig)
        return q3 * q2 * q1;
    }
}

/// <summary>
/// XML parser for Mozaik .moz files.
/// Handles the text header Mozaik puts before the XML.
/// </summary>
public static class MozParser
{
    // Use a reasonable default sheet thickness in mm if the part doesn't store one explicitly
    private const float DefaultThicknessMm = 19f; // ~3/4"

    /// <summary>
    /// Parse a Mozaik .moz XML-ish string into a MozCabinet + MozPart list.
    /// Mozaik puts a text header before the XML, so we strip down to the first XML tag.
    /// </summary>
    public static MozCabinet ParseMozFromText(string mozRaw)
    {
        if (string.IsNullOrWhiteSpace(mozRaw))
            throw new Exception("Empty .moz text.");

        // Find where the real XML starts
        int idx = mozRaw.IndexOf("<?xml", StringComparison.OrdinalIgnoreCase);
        if (idx < 0)
            idx = mozRaw.IndexOf("<Product", StringComparison.OrdinalIgnoreCase);

        if (idx < 0)
            throw new Exception("Could not locate XML root in .moz file.");

        string xml = mozRaw.Substring(idx);

        XDocument doc = XDocument.Parse(xml);

        XElement root = doc.Root;
        if (root == null)
            throw new Exception("Root element is null in .moz XML.");

        MozCabinet cab = new MozCabinet
        {
            Name = (string)root.Attribute("ProdName") ??
                   (string)root.Attribute("Name") ??
                   "MozCabinet",

            // Product-level metadata
            UniqueID = AttrInt(root, "UniqueID"),
            SourceLibrary = (string)root.Attribute("SourceLib") ?? "",

            // Overall dimensions (mm)
            WidthMm = AttrFloat(root, "Width"),
            HeightMm = AttrFloat(root, "Height"),
            DepthMm = AttrFloat(root, "Depth"),

            // Positioning data
            ElevationMm = AttrFloat(root, "Elev"),
            XPositionMm = AttrFloat(root, "X"),
            Rotation = AttrFloat(root, "Rot"),
            WallRef = (string)root.Attribute("Wall") ?? ""
        };

        // Parts live under <CabProdParts><CabProdPart .../>
        foreach (XElement partElem in root.Descendants("CabProdPart"))
        {
            MozPart part = new MozPart
            {
                Name = (string)partElem.Attribute("Name") ?? "Part",

                // Positions in mm (Mozaik uses X/Y/Z)
                X = AttrFloat(partElem, "X"),
                Y = AttrFloat(partElem, "Y"),
                Z = AttrFloat(partElem, "Z"),

                // Dimensions in mm
                // L = part length, W = part width/depth
                PartL = AttrFloat(partElem, "PartL",
                        AttrFloat(partElem, "L")),
                PartW = AttrFloat(partElem, "PARTW",
                        AttrFloat(partElem, "W")),
                // No explicit thickness stored per-part in some files,
                // so fall back to Thickness/T or our default 19mm.
                Thickness = AttrFloat(partElem, "Thickness",
                             AttrFloat(partElem, "T", DefaultThicknessMm)),

                // Rotations
                R1 = AttrChar(partElem, "R1", 'X'),
                R2 = AttrChar(partElem, "R2", 'Y'),
                R3 = AttrChar(partElem, "R3", 'Z'),
                A1 = AttrFloat(partElem, "A1"),
                A2 = AttrFloat(partElem, "A2"),
                A3 = AttrFloat(partElem, "A3")
            };

            cab.Parts.Add(part);
        }

        return cab;
    }

    private static float AttrFloat(XElement el, string name, float defaultValue = 0f)
    {
        XAttribute a = el.Attribute(name);
        if (a == null)
            return defaultValue;

        if (float.TryParse(a.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out float v))
            return v;

        return defaultValue;
    }

    private static int AttrInt(XElement el, string name, int defaultValue = 0)
    {
        XAttribute a = el.Attribute(name);
        if (a == null)
            return defaultValue;

        if (int.TryParse(a.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int v))
            return v;

        return defaultValue;
    }

    private static char AttrChar(XElement el, string name, char defaultValue)
    {
        XAttribute a = el.Attribute(name);
        if (a == null || string.IsNullOrEmpty(a.Value))
            return defaultValue;

        return a.Value[0];
    }
}
