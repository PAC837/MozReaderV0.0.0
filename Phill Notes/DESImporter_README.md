# DES Importer Architecture & Implementation Ideas

## Overview

Mozaik DES (Design) files contain complete room layouts with walls and positioned cabinets. After analyzing `Room1.des`, it's clear that **the DES import system should be built BEFORE individual cabinet positioning**, as cabinets are positioned relative to walls, not in world space.

## Key Discovery: Wall-Relative Positioning

### DES File Structure
```xml
<Room Name="Room 1">
  <Walls>
    <Wall IDTag="1" Len="3500" Height="2768.6" PosX="0" PosY="0" Ang="0" WallNumber="1"/>
  </Walls>
  <Products>
    <Product ProdName="FS 87" X="0.00" Wall="1_1" CabNo="1"/>
    <Product ProdName="87 DH" X="19" Wall="1_1" CabNo="2"/>  
    <Product ProdName="FS 87" X="590.50" Wall="1_1" CabNo="3"/>
  </Products>
</Room>
```

**Critical Insight**: `X="19"` is NOT world position - it's **19mm along Wall 1**!

## Proposed Architecture

### 1. Room Hierarchy
```
"Room 1" (Root GameObject)
├── "Wall 1" (Transform: world position, angle, length)
│   ├── "FS 87" (Cabinet at X=0mm along wall)
│   ├── "87 DH" (Cabinet at X=19mm along wall) 
│   └── "FS 87" (Cabinet at X=590.50mm along wall)
├── "Wall 2" (if exists)
└── "Wall 3" (if exists)
```

### 2. Cabinet Structure (UNCHANGED)
Your existing bounds/corners system is perfect! Just positioned by wall:
```
"87 DH" (Cabinet positioned on wall)
├── Part_001
├── Part_002 
├── ...
└── Bounds (your existing bounds system)
    └── Corners
        ├── Corner_00
        └── ...
```

## Implementation Phases

### Phase 1: DES Parser
**Script: `DESParser.cs`**
- Parse DES XML structure
- Extract Room → Walls → Products data
- Handle coordinate systems and units (mm → meters)

```csharp
public class DESRoom
{
    public string Name;
    public List<DESWall> Walls;
    public List<DESProduct> Products;
}

public class DESWall  
{
    public int WallNumber;
    public float Length;    // mm
    public float Height;    // mm
    public Vector3 Position; // world position
    public float Angle;     // rotation in degrees
}

public class DESProduct
{
    public string ProductName;
    public string WallID;      // e.g. "1_1"
    public float XAlongWall;   // position along wall in mm
    public int CabinetNumber;
}
```

### Phase 2: Room/Wall Creator
**Script: `DESImporter.cs`**
- Create Room GameObject hierarchy
- Position walls in world space using DES coordinates
- Set up wall transforms for cabinet placement

```csharp
public class DESImporter : MonoBehaviour
{
    [Header("Input")]
    public TextAsset desFile;
    
    [ContextMenu("Import DES Room")]
    public void ImportRoom()
    {
        DESRoom room = DESParser.ParseDESFromText(desFile.text);
        CreateRoomHierarchy(room);
    }
}
```

### Phase 3: Cabinet Placement  
- Use existing `MozImporterBounds` for cabinet creation
- Position cabinets relative to their parent walls
- Convert wall-relative position to local transform

```csharp
// For each product in DES:
Transform wallTransform = FindWall(product.WallID);
Vector3 localPosition = new Vector3(product.XAlongWall * MM_TO_M, 0, 0);

GameObject cabinet = CreateCabinetFromMozFile(product.ProductName);
cabinet.transform.SetParent(wallTransform, false);
cabinet.transform.localPosition = localPosition;
```

### Phase 4: Interactive Wall System
**Script: `WallCabinetPositioning.cs`**
- Drag cabinets along walls
- Snap to wall positions
- Maintain wall-relative coordinates

## Benefits of This Architecture

### ✅ Solves Your Original Problem
- **Moving cabinets**: Drag along wall, not arbitrary world space
- **Wall reference**: Clear parent-child relationship  
- **Bounds integration**: Your existing system works perfectly as cabinet children

### ✅ Logical Hierarchy
- Room contains walls
- Walls contain cabinets
- Cabinets contain parts + bounds + corners

### ✅ Real-World Workflow
- Import entire room layout from Mozaik
- Individual cabinets positioned correctly on walls
- Interactive editing maintains wall relationships

## Integration with Existing Systems

### Your Current Systems (KEEP AS-IS)
- ✅ `MozImporter.cs` - Create individual cabinets
- ✅ `MozImporterBounds.cs` - Add bounds + corners  
- ✅ `MozBoundsHighlighter.cs` - Control visibility
- ✅ `MozRuntimeSelector.cs` - Click selection
- ✅ `RoomCamera.cs` - Camera controls

### New Systems (TO BUILD)
- 📝 `DESParser.cs` - Parse DES XML files
- 📝 `DESImporter.cs` - Create room/wall hierarchy
- 📝 `WallCabinetPositioning.cs` - Interactive positioning

## Technical Implementation Notes

### Coordinate System Conversion
```csharp
// DES uses mm, Unity uses meters
const float MM_TO_M = 0.001f;

// Wall positioning in world space
Vector3 wallWorldPos = new Vector3(
    desWall.PosX * MM_TO_M, 
    0, 
    desWall.PosY * MM_TO_M
);

// Cabinet positioning along wall (local to wall)  
Vector3 cabinetLocalPos = new Vector3(
    desProduct.XAlongWall * MM_TO_M,
    desProduct.Elevation * MM_TO_M, 
    0
);
```

### Wall Transform Setup
```csharp
wallGameObject.transform.position = wallWorldPos;
wallGameObject.transform.rotation = Quaternion.Euler(0, desWall.Ang, 0);
```

## File Structure
```
Assets/Scripts/
├── DESParser.cs          (NEW - Parse DES XML)
├── DESImporter.cs        (NEW - Create room hierarchy) 
├── WallCabinetPositioning.cs (NEW - Interactive editing)
├── MozImporter.cs        (EXISTING - Individual cabinets)
├── MozImporterBounds.cs  (EXISTING - Bounds + corners)
├── MozBoundsHighlighter.cs (EXISTING - Visibility control)
├── MozRuntimeSelector.cs (EXISTING - Click selection)
└── RoomCamera.cs         (EXISTING - Camera controls)
```

## Next Steps

1. **Build DES Parser** - Extract room/wall/product data from XML
2. **Build DES Importer** - Create GameObject hierarchy 
3. **Integrate with MozImporterBounds** - Position cabinets on walls
4. **Test with Room1.des** - Verify complete room import
5. **Add Interactive Positioning** - Drag cabinets along walls

## Future Enhancements

- **Multiple Rooms** - Support multiple room imports
- **Wall Editing** - Modify wall lengths, angles, positions
- **Cabinet Libraries** - Reference MOZ files from DES product names
- **Export Back to DES** - Save modified layouts
- **Collision Detection** - Prevent cabinet overlaps on walls
- **Automated Spacing** - Smart cabinet gap calculations

---

*This architecture preserves your excellent bounds/corners/selection system while solving the positioning challenge through proper wall-based hierarchy.*
