# Summary of Changes - Product Wall Snapper Implementation

## Date: December 7, 2025

## Overview
Implemented automatic product-to-wall snapping system and DES export capability for products.

---

## Files Modified

### 1. MozImporterBounds.cs
**Changes:**
- Consolidated all shared classes from deleted `MozImporter.cs` (`MozCabinet`, `MozPart`, `MozCoordinateMapper`, `MozParser`)
- Added `autoSnapToSelectedWall` toggle (default: true)
- Added `TryAutoSnapToWall()` method that:
  - Queries `MozRuntimeSelector` for selected wall
  - Auto-attaches `CabinetWallSnapper` component to imported cabinet
  - Calls `SnapToWall()` to position on wall
- Added `OnCabinetImported` event for UI integration

### 2. MozRuntimeSelector.cs
**Changes:**
- Now supports **both wall and cabinet selection**
- Left-click on `MozaikWall` → selects wall for imports
- Left-click on cabinet → selects cabinet (shows bounds)
- Right-click → clears wall selection
- Added `GetSelectedWall()` method for importers to query
- Added `selectedWallMaterial` field for visual feedback
- Added `OnWallSelectionChanged` event
- Added `OnCabinetSelectionChanged` event

### 3. CabinetWallSnapper.cs
**Changes:**
- Added `xAlongWallMm` field - tracks position along wall for DES export
- Added `_cabinetWidthMm` field - stores cabinet width
- Modified `SnapToWall()` to use `xAlongWallMm` offset
- Added `UpdateXAlongWallFromPosition()` - recalculates X from current position
- Added `GetProductName()` - extracts name for DES export
- Added `GetCabinetWidthMm()` - returns cabinet width

### 4. MozaikDesJobExporter.cs
**Changes:**
- Added `startingCabNo` field for product numbering
- Added `ExportWallsAndProductsIntoRoomFile()` method (main export)
- Added `ExportProductsToXml()` method that:
  - Finds all `CabinetWallSnapper` components
  - Exports `<Product>` elements with `ProdName`, `X`, `Wall`, `CabNo` attributes
- Renamed old method to `ExportWallsIntoRoomFile()` (legacy)
- Added context menu "Export Walls And Products Into Room File"

---

## Files Deleted

| File | Reason |
|------|--------|
| `Assets/Scripts/MozImporter.cs` | Consolidated into `MozImporterBounds.cs` |
| `Assets/Scripts/MozImporter.cs.meta` | Associated meta file |
| `Assets/Scripts/MozImporter/` folder | Empty folder |
| `Assets/Scripts/MozImporter.meta` | Associated meta file |
| `Assets/Scripts/Wall System/` folder | Empty folder |
| `Assets/Scripts/Wall System.meta` | Associated meta file |

---

## Files Unchanged

- `MozaikWall.cs` - No changes needed
- `MozBoundsHighlighter.cs` - No changes needed
- `RoomCamera.cs` - No changes needed
- `CabinetBoundsResizer.cs` - No changes needed
- `Editor/CabinetWallSnapperEditor.cs` - No changes needed

---

## Documentation Updated

- `MozReaderV0.0.0_SystemOverview.md` - Complete rewrite reflecting new architecture
- `MozImporter_README.md` - Updated for consolidated importer
- `DESImporter_README.md` - No changes (still relevant for future DES import)

---

## New Workflow

### Auto-Snap on Import
```
1. Create MozaikWall in scene
2. Add MozRuntimeSelector to scene
3. Enter Play mode
4. Click on wall to select it
5. Import cabinet using MozImporterBounds
6. Cabinet automatically snaps to selected wall at X=0
```

### Export to DES
```
1. Position cabinets on walls (auto or manual)
2. Add MozaikDesJobExporter to scene
3. Pick Mozaik Room File (.des)
4. Right-click → Export Walls And Products Into Room File
5. Walls and products exported with positions
```

---

## DES Export Format

```xml
<Walls>
  <Wall IDTag="1" Len="3000" Height="2768.6" PosX="0" PosY="0" Ang="0" Thickness="101.6"/>
</Walls>
<Products>
  <Product ProdName="87 DH" X="0.00" Wall="1_1" CabNo="1"/>
  <Product ProdName="FS 87" X="590.50" Wall="1_1" CabNo="2"/>
</Products>
```

---

## Architecture Principles Maintained

✅ **Loose Coupling** - Components communicate via queries/events, not hard dependencies  
✅ **Single Responsibility** - Each script has one clear purpose  
✅ **No New Files** - All functionality consolidated into existing scripts  
✅ **Clean Codebase** - Deleted obsolete files and empty folders
