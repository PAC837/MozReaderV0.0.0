# APP_DIARY.md

Running development log for MozReaderV0.0.0.

---

## [2025-12-19] Front Wall U-Shaped Opening with Proper Highlighting

### Summary
- Fixed wall number assignments to match gizmo labels and Mozaik export format
- Implemented U-shaped opening visualization on front wall (Wall 1)
- Fixed highlighting system to work correctly with opening segments
- Opening area stays clear when wall is selected (only visible segments highlight)

### Files
- `Assets/Scripts/FourWallRoomBuilder.cs` – Fixed wall number assignments; added U-shaped opening with transparent WallVisual + visible segments
- `Assets/Scripts/RuntimeWallSelector.cs` – Modified SetWallHighlight() to detect OpeningSegments and skip WallVisual

### Behavior / Notes

**Wall Number Fix:**
- Swapped wallNumber assignments for left/right walls to match gizmo labels
- Wall at X = -halfWidth: Now wallNumber = 4 (RIGHT in Mozaik, Ang=270°)
- Wall at X = +halfWidth: Now wallNumber = 2 (LEFT in Mozaik, Ang=90°)
- Export to Mozaik now has correct wall numbers matching visual labels

**U-Shaped Opening System:**
- Front wall (Wall 1) creates U-shaped opening with 3 visible segments:
  - LeftSegment: From wall left edge to opening start
  - RightSegment: From opening end to wall right edge  
  - HeaderSegment: Above opening (from opening start to end)
- WallVisual made transparent (still exists for highlighting system)
- Opening segments offset 5% of thickness forward in Z to render in front
- All segments have trigger colliders for click detection

**Smart Highlighting:**
- RuntimeWallSelector detects if wall has "OpeningSegments" child
- **With opening**: Only highlights visible segments (LeftSegment, RightSegment, HeaderSegment), skips transparent WallVisual
- **Without opening**: Highlights all renderers normally (back, left, right walls)
- Result: Front wall shows U-shaped opening with blue segments when selected, opening area stays clear

**Opening Configuration:**
- `hasOpening` – Enable/disable opening on front wall
- `openingWidthMm` – Width of opening (~70 inches default)
- `openingHeightMm` – Height of opening (~88 inches default)
- `openingXPositionMm` – Distance from left edge to opening start

### Visual Result
```
Front Wall Selected View:
┌──────────────────────────────────┐ ← Blue HeaderSegment
│                                  │
│  Blue      Opening Area      Blue│
│  Left      (stays clear)     Right│
│  Segment                    Segment│
└──────────────────────────────────┘
```

### Test Steps
1. Play scene - 4-wall room auto-builds
2. Click front wall - U-shaped opening visible
3. Only solid wall segments turn blue, opening stays clear
4. Click other walls - entire wall turns blue (normal behavior)
5. Export to Mozaik - wall numbers correct, opening preserved

---

## [2025-12-19] Rotation-Aware Cabinet Snapping & 4-Wall Room Builder

### Summary
- **MAJOR FIX**: CabinetWallSnapper now works correctly for walls at ANY rotation (0°, 90°, 180°, 270°)
- Added rotation-aware helper methods to MozaikWall (GetFrontFaceCenter, GetFrontFaceNormal, etc.)
- Created FourWallRoomBuilder matching Mozaik's DES format (4 joined walls + opening fixture)
- Added wallNumber and mozaikAngleDegrees fields to MozaikWall for DES export

### Files
- `Assets/Scripts/MozaikWall.cs` – Added rotation-aware methods, wallNumber, mozaikAngleDegrees fields
- `Assets/Scripts/CabinetWallSnapper.cs` – Rewrote SnapToWall() to use wall's local coordinate system
- `Assets/Scripts/FourWallRoomBuilder.cs` – **NEW** 4-wall room matching Mozaik format

### Behavior / Notes

**The Bug (Fixed):**
- `GetFrontFaceZ()` always added to world Z coordinate (only worked for walls facing +Z)
- Side walls (rotated 90° or -90°) got wrong cabinet positions
- Cabinets would float in space or snap to wrong location

**The Fix:**
- New rotation-aware methods use `transform.forward` and `transform.right`
- `GetFrontFaceCenter()` - returns world position of front face center
- `GetFrontFaceNormal()` - returns direction front face points (into room)
- `GetLeftEdgePosition()` / `GetRightEdgePosition()` - returns world positions
- `GetCabinetYRotation()` - returns Y angle for cabinet placement
- `GetFrontFacePosition(distanceMm, elevationMm)` - returns position along wall

**CabinetWallSnapper Changes:**
- Uses wall's local coordinate system (wallForward, wallRight)
- Positions cabinet relative to wall's left edge using vector math
- Works for ANY wall rotation (0°, 90°, 180°, 270°, or arbitrary)
- Rotation is already applied via RotateToFaceRoom()

**FourWallRoomBuilder (Mozaik Compatible):**

Wall numbering matches Mozaik DES format:
| Wall | Position | Rotation | Mozaik Angle | Faces |
|------|----------|----------|--------------|-------|
| 1 | FRONT | 180° | 180° | -Z (inward) |
| 2 | LEFT | -90° | 90° | -X (inward) |
| 3 | BACK | 0° | 0° | +Z (inward) |
| 4 | RIGHT | 90° | 270° | +X (inward) |

Layout (top-down view):
```
                   BACK (Wall 3)
    +─────────────────────────────────+
    │                                 │
    │  RIGHT        LEFT              │
    │  (Wall 4)     (Wall 2)          │
    │                                 │
    +────────[ OPENING ]──────────────+
                   FRONT (Wall 1)
```

**Opening System:**
- Opening is a fixture on Wall 1 (front wall), not a gap between walls
- Matches how Mozaik DES files represent openings
- hasOpening, openingWidthMm, openingHeightMm, openingXPositionMm configurable

**MozaikWall New Fields:**
- `wallNumber` (1-4) - for DES export
- `mozaikAngleDegrees` (0, 90, 180, 270) - for DES export

### Unity Implementation Checklist

1. **Using FourWallRoomBuilder:**
   - Create empty GameObject named "4-Wall Room"
   - Add `FourWallRoomBuilder` component
   - Set room dimensions (width, depth, height, thickness)
   - Configure opening (enabled, width, position)
   - Right-click → "Build Room"

2. **Placing Cabinets:**
   - Select a wall (RuntimeWallSelector)
   - Spawn cabinet from library
   - Cabinet automatically snaps to wall front face
   - Works on ALL 4 walls at any rotation

3. **Verifying:**
   - Select any wall in Scene view
   - Blue gizmo = FRONT face (where cabinets go)
   - Red gizmo = BACK face
   - Yellow sphere = START of wall
   - Green sphere = END of wall

### What's Preserved
- Smart placement (gap finding) - uses XPositionMm (1D, rotation-independent)
- Elevation from MozCabinetData
- All existing DES export functionality
- Wall selection and highlighting

---

## [2025-12-19] Reach-In Closet: 5-Wall Room Configuration

### Summary
- Added `ReachInClosetBuilder` component that creates a 5-wall reach-in closet configuration
- Room now starts with 5 walls instead of 1 (back wall, 2 side walls, 2 return walls)
- All existing cabinet placement and snapping systems work with the new walls
- User can adjust wall dimensions and ceiling height via Inspector

### Files
- `Assets/Scripts/ReachInClosetBuilder.cs` – **NEW** Builds and manages reach-in closet with 5 walls
- `Assets/Scripts/RuntimeWallSelector.cs` – Added `autoCreateReachInCloset` setting, `CreateReachInCloset()` method

### Behavior / Notes

**Reach-In Closet Layout (top-down view):**
```
    Left Return     [ Opening ]     Right Return
    |_________|                     |_________|
    |                                         |
    |   Left Side                 Right Side  |
    |                                         |
    |_________________________________________|
                    Back Wall
```

**Default Dimensions:**
| Wall | Default Size | Notes |
|------|-------------|-------|
| Back Wall | 2438.4mm (8ft) | Total closet width |
| Side Walls | 609.6mm (24in) | Closet depth |
| Return Walls | 203.2mm (8in) each | Opening = Back - 16" |
| Ceiling Height | 2438.4mm (8ft) | Same for all walls |
| Wall Thickness | 101.6mm (4in) | Standard |

**Wall Orientation:**
- All walls face INWARD (toward closet interior)
- Back Wall: Faces +Z (into room)
- Left Side: Faces +X (toward center)
- Right Side: Faces -X (toward center)
- Return Walls: Face -Z (toward back)

**Opening Calculation:**
- Opening Width = Back Wall Length - (2 × Return Wall Length)
- Default: 2438.4mm - (2 × 203.2mm) = 2032mm opening

**Inspector Controls:**
- `backWallLengthMm` - Total closet width
- `sideWallDepthMm` - How deep the closet is
- `returnWallLengthMm` - How much each side "returns" to create opening
- `ceilingHeightMm` - Height of all walls
- `wallThicknessMm` - Thickness of all walls

**Context Menu Actions:**
- "Build Closet" - Creates/rebuilds all 5 walls
- "Update Dimensions" - Updates existing walls without destroying
- "Destroy All Walls" - Removes all walls

### Unity Implementation Checklist

1. **Automatic Setup (Default)**
   - Simply enter Play mode
   - If `RuntimeWallSelector.autoCreateReachInCloset = true` (default), closet is auto-created
   - Back wall is auto-selected

2. **Manual Setup**
   - Create empty GameObject named "Reach-In Closet"
   - Add `ReachInClosetBuilder` component
   - Set dimensions in Inspector
   - Right-click component → "Build Closet"

3. **Adjusting Dimensions**
   - Select "Reach-In Closet" GameObject
   - Change dimension fields in Inspector
   - Right-click → "Update Dimensions" (or values update live)

### Integration Notes
- All 5 walls work with existing `CabinetWallSnapper`
- Walls are clickable/selectable via `RuntimeWallSelector`
- Export via `MozaikDesJobExporter` handles all walls correctly
- Cabinets can be placed on any wall (back, sides, or returns)

---

## [2025-12-19] Fix Pink Shader Issues: Walls, Highlights, and Wireframes

### Summary
- Fixed all pink shader issues that occurred at runtime (URP shader lookup failures)
- Wall highlight now uses `Sprites/Default` shader (always available)
- Wall base material uses `TextureLibraryManager.GetDefaultWallMaterial()` with URP/Lit
- Cabinet wireframe selection uses `Sprites/Default` shader
- Added automatic cleanup of old wireframe children baked into prefabs

### Files
- `Assets/Scripts/RuntimeWallSelector.cs` – Fixed `SetWallHighlight()` to use URP-compatible shader cascade
- `Assets/Scripts/MozaikWall.cs` – Added comprehensive debug logging for material application
- `Assets/Scripts/MozBoundsHighlighter.cs` – Fixed wireframe shader, added `CleanupOldWireframes()` in OnEnable

### Behavior / Notes

**Wall Highlight Fix:**
- **Problem**: `SetWallHighlight()` used `Shader.Find("Standard")` which returns null in URP at runtime
- **Solution**: Cascade of shaders: `Sprites/Default` → `URP/Lit` → `Standard`
- **Result**: Selected walls now show blue highlight tint (not pink)

**Wall Base Material:**
- Uses `TextureLibraryManager.GetDefaultWallMaterial()` which creates URP/Lit material
- Fallback creates inline material with `Sprites/Default` if URP shaders not found
- Debug logging traces entire material lookup chain

**Wireframe Shader Fix:**
- **Problem**: `CreateWireframeMaterial()` tried URP shaders that might not be available at runtime
- **Solution**: Try `Sprites/Default` first (always available for LineRenderer)
- **Result**: Selection wireframe is green (not pink)

**Old Wireframe Cleanup:**
- **Problem**: Prefabs saved with old wireframe children caused pink ghosts
- **Solution**: `CleanupOldWireframes()` runs on OnEnable, deletes any children named:
  - "SelectionWireframe"
  - "Edge*" (Edge0, Edge1, etc.)
  - "Wireframe"
- **Result**: No more duplicate wireframe artifacts

### Shader Lookup Pattern (Use This!)
```csharp
// Always works for runtime material creation:
Shader shader = Shader.Find("Sprites/Default"); // First choice - always available
if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");
if (shader == null) shader = Shader.Find("Standard");
```

---

## [2025-12-18] Visual Cleanup: Default Materials, Cylinder Rods, Auto-Floor, No More Pink!

### Summary
- Added default material system to TextureLibraryManager (cabinet, wall, chrome, floor)
- Fixed pink material issue - walls and cabinets now have proper fallback materials
- Closet rods now spawn as **cylinders** instead of cubes
- Metal parts (rods, hangers) use chrome metallic material
- **FloorManager** auto-creates floor on scene start
- Simplified VISUAL_SETUP.md - removed redundant manual steps

### Files
- `Assets/Scripts/TextureLibraryManager.cs` – Added GetDefaultCabinetMaterial(), GetDefaultWallMaterial(), GetDefaultChromeMaterial(), GetDefaultFloorMaterial() + CreateUrpMaterial() helper
- `Assets/Scripts/MozImporterBounds.cs` – Added IsRodPart(), IsMetalPart(), GetMaterialForPart(); rods spawn as Cylinder; metal parts get chrome material
- `Assets/Scripts/MozaikWall.cs` – Added fallback to TextureLibraryManager.GetDefaultWallMaterial() with cascading fallback to inline grey URP material
- `docs/VISUAL_SETUP.md` – **NEW** Complete setup guide with step-by-step checklist

### Behavior / Notes

**Default Materials:**
| Part Type | Material | Properties |
|-----------|----------|------------|
| Cabinet panels | First texture in library OR beige (0.9, 0.87, 0.8) | URP Lit, smoothness 0.3 |
| Walls | Light grey (0.85, 0.85, 0.85) | URP Lit, smoothness 0.2 |
| Closet rods/Hangers | Chrome (0.8, 0.8, 0.82) | URP Lit, metallic 0.9, smoothness 0.9 |
| Floor | Tan (0.7, 0.7, 0.65) | URP Lit, smoothness 0.4 |

**Cylinder Rods:**
- Parts containing "rod" or "closetrod" spawn as `PrimitiveType.Cylinder`
- Cylinder scaled and rotated to match Mozaik dimensions (length along rod axis)
- Other parts continue to spawn as cubes

**Metal Detection:**
- Part names containing: `rod`, `hanger`, `hardware`, `metal`, `chrome`
- These parts get chrome material instead of cabinet material

**Material Cascade:**
1. Check if explicit `panelMaterial` assigned on importer
2. Try `TextureLibraryManager.Instance.GetDefaultXMaterial()`
3. Create inline URP Lit material with fallback color
4. If all fail → pink (but now has multiple fallbacks)

### Setup Guide
See `docs/VISUAL_SETUP.md` for complete setup instructions including:
- Scene manager setup (TextureLibraryManager, etc.)
- Floor creation
- Wall setup
- Importer configuration
- Troubleshooting pink materials

---

## [2025-12-16] 🎉 MAJOR BREAKTHROUGH: Parametric Panel Operations Working!

### Summary
- **CabProdParms preservation** - LED grooves now appear in Mozaik!
- **Parametric panel operations** - Holes appear when panel touches section, disappear when moved away!
- **Panel operations stripping** - Auto-generated ops stripped for panels, user ops preserved
- **CabinetLibrary update fix** - AddCabinet now updates existing entries instead of silently failing

### Files
- `Assets/Scripts/MozCabinetData.cs` – Added `CabProdParmsXml` field for parameter roundtrip
- `Assets/Scripts/MozImporter.cs` – Added CabProdParmsXml parsing to MozCabinet and MozParser
- `Assets/Scripts/MozImporterBounds.cs` – Added CabProdParmsXml assignment to cabinet component
- `Assets/Scripts/MozaikDesJobExporter.cs` – Exports stored CabProdParmsXml; strips auto-ops for Type=8 panels
- `Assets/Scripts/CabinetLibrary.cs` – AddCabinet now updates existing entries (was silently skipping)

### Behavior / Notes

**CabProdParms Roundtrip (THE KEY FIX!):**
- **Problem**: LED grooves weren't showing because parameters like `LEDConfig` were lost
- **Root Cause**: `<CabProdParms>` element was exported empty (`<CabProdParms />`)
- **Solution**: Parse and store entire CabProdParms XML from .moz, export it back
- **Result**: LED channels visible! Parameters drive parametric operations correctly!

**Parametric Panel Operations:**
- **For PANELS (Type=8)**: Strip all auto-generated operations (IsUserOp="False")
- **User operations preserved**: LED grooves, custom holes marked IsUserOp="True" survive
- **Adjacent cab operations**: Mozaik regenerates line bores, fasteners based on new adjacency
- **Result**: Holes appear when panel touches section, disappear when moved!

**CabinetLibrary Update Logic:**
- **Bug**: AddCabinet checked if entry existed but just logged warning and returned
- **Problem**: If entry had null prefab, clicking "Add to Library" again didn't fix it
- **Fix**: Now updates existing entries - prefab, metadata all updated properly

### Key XML Elements Now Roundtripped
| Element | Purpose | New? |
|---------|---------|------|
| `CabProdParmsXml` | Product parameters (LEDConfig, SysHoles, etc.) | ✅ NEW |
| `TopShapeXml` | Cabinet shape, corner points, EdgeType | Existing |
| `CabProdPartsXml` | Parts with positions (shelves, rods) | Existing |
| `ProductInteriorXml` | Section layout definitions | Existing |

### Panel Operation Export Logic
```
For PANELS (ProductType == 8):
  - Strip operations where IsUserOp != "True"
  - Keep user operations (LED grooves, custom holes)
  
For SECTIONS (ProductType == 3, etc.):
  - Keep all operations as-is
```

### Test Verified ✅
1. Import FS 87 LED Right panel → Add to Library
2. Place next to 87 DH section → Export to DES
3. Open in Mozaik → LED groove visible!
4. Move panel away from section → Export again
5. Open in Mozaik → Holes removed! (Mozaik regenerates based on adjacency)
6. Move panel back → Holes return!

### Victory Lap 🚀
This was the final piece of the parametric puzzle! Unity can now:
- Import products from Mozaik libraries
- Place them on walls with correct positions
- Export back to Mozaik with ALL data intact
- Mozaik recalculates adjacency operations automatically

---

## [2025-12-12] Selection System Fixes & Texture Picker Debug

### Summary
- Fixed critical freeze/crash when clicking between cabinets (destroyed LineRenderer bug)
- Improved wireframe bounds to perfectly fit cabinet geometry
- Added folder browse button to TextureLibraryManager for easier configuration
- Fixed pink material issue with URP shader compatibility
- Added comprehensive debug logging for texture/shader diagnosis
- Fixed camera controls conflict (pan moved to middle mouse)

### Files
- `Assets/Scripts/MozBoundsHighlighter.cs` – Fixed null reference crash, added CalculateCabinetBounds() for tight wireframe fit
- `Assets/Scripts/MozRuntimeSelector.cs` – Added SelectedCabinet property and OnSelectionChanged event
- `Assets/Scripts/CabinetMaterialApplicator.cs` – Fixed void return bug, added material debug logging
- `Assets/Scripts/RoomCamera.cs` – Changed pan from LEFT to MIDDLE mouse button
- `Assets/Scripts/TextureLibraryManager.cs` – Changed shader to URP Lit, added shader detection and debug
- `Assets/Scripts/Editor/TextureLibraryManagerEditor.cs` – **NEW** Custom Inspector with "Browse Folder..." button

### Behavior / Notes

**Selection Freeze Fix:**
- **Root Cause**: When selecting different cabinets, old wireframe LineRenderers were destroyed but remained cached in `_cabinetRenderers` array. Accessing `.materials` on destroyed objects caused `MissingReferenceException` → freeze.
- **Solution**: Added `if (rend == null)` and `if (rend is LineRenderer)` checks in all material application loops
- **Impact**: Can now rapidly click between cabinets without freezing

**Wireframe Improvements:**
- **Old Behavior**: Used `boundsRenderer.bounds` (the oversized dummy Bounds object)
- **New Behavior**: `CalculateCabinetBounds()` calculates from actual mesh geometry
- **Result**: Wireframe perfectly fits cabinet (no gap above/below)
- **Exclusions**: Skips Bounds object, corners, wireframes, rods, inserts, hardware

**Texture System UX:**
- **Folder Browse Button**: Inspector now has big "Browse Folder..." button
- **Auto-Update**: Selecting folder automatically updates path field
- **No Copy/Paste**: Click → browse → done!

**URP Shader Compatibility:**
- **Problem**: Materials used "Standard" shader (doesn't exist in URP) → pink materials
- **Solution**: Changed to "Universal Render Pipeline/Lit"
- **Fallbacks**: Tries "Simple Lit" then "Unlit/Texture" if Lit not found
- **Debug**: Logs shader name for each loaded texture

**Camera Controls Fix:**
- **Problem**: LEFT CLICK used by both selection and camera pan → conflicts
- **Solution**: Moved pan to MIDDLE MOUSE button
- **New Controls**: 
  - LEFT = select cabinets
  - MIDDLE = pan camera
  - RIGHT = orbit camera
  - WHEEL = zoom
- **Industry Standard**: Matches Blender/Maya/CAD apps

**Debug Logging:**
- **TextureLibraryManager**: Logs shader found/used for each texture
- **CabinetMaterialApplicator**: Logs material name, shader, texture presence when applying
- **Purpose**: Diagnose shader/texture issues without guessing

### Unity Implementation Notes
**After updating from Git:**
1. Unity will auto-recompile new Editor script
2. Select "Texture Library Manager" GameObject
3. See new "Browse Folder..." button in Inspector
4. Click button → select texture folder → done
5. Right-click component → "Reload Textures"
6. Play mode → textures should work (no more pink if URP shaders exist)

### Test Steps
1. **Test Selection**: Click between cabinets rapidly - should not freeze
2. **Test Wireframe**: Select cabinet - green lines should fit perfectly (no gaps)
3. **Test Textures**: Apply material - should show color (not pink)
4. **Test Camera**: MIDDLE mouse drag to pan (no conflict with LEFT click selection)

### Known Issues
- `[DEBUG MODE]` Many console logs - for diagnosis only, can be reduced later
- Pink materials indicate URP Lit shader not found (rare in URP projects)

---

## [2025-12-12] Material Picker System & Wireframe Selection Improvements

### Summary
- Implemented complete material/texture picker system with admin-configurable texture folder
- Replaced bounds cube with clean 12-line wireframe visualization
- Materials can be applied to entire room or individual selected cabinets
- Auto-excludes hardware parts (rods, inserts, hangers)

### Files
- `Assets/Scripts/TextureLibraryManager.cs` – **NEW** Loads textures from folder, creates materials, singleton access
- `Assets/Scripts/CabinetMaterialApplicator.cs` – **NEW** Applies materials to room or selected cabinet with smart filtering
- `Assets/Scripts/TexturePickerUI.cs` – **NEW** UI with texture grid and apply buttons
- `Assets/Scripts/MozBoundsHighlighter.cs` – Replaced inflated cube with 12 LineRenderer wireframe, 90% alpha
- `docs/MATERIAL_PICKER_SETUP.md` – **NEW** Complete setup instructions for material system

### Behavior / Notes

**Material Picker System:**
- **Admin Configuration**: Admin sets texture folder path in TextureLibraryManager
- **Texture Loading**: Automatically loads all JPG/PNG files from folder on Start
- **UI Grid**: Displays textures as preview squares in scrollable grid
- **Dual Apply Modes**: 
  - "Apply to Room" → changes all cabinets
  - "Apply to Selected" → changes only selected cabinet
- **Smart Filtering**: Excludes rods, inserts, hardware, hangers, bounds objects
- **Live Linking**: Points to Mozaik's texture folder for real-time updates

**Wireframe Selection:**
- **Clean Visualization**: 12 green lines form cube edges (no solid mesh)
- **World Space**: Lines use world coordinates → no camera movement artifacts
- **Configurable**: Color (90% alpha green), width (0.005m), inflation (1.0 = exact size)
- **Dynamic**: Created when selected, destroyed when deselected
- **Performance**: No more material recreation every frame

### Unity Implementation Checklist

**Material System Setup:**
1. Create 3 empty GameObjects:
   - "Texture Library Manager" + TextureLibraryManager component
   - "Cabinet Material Applicator" + CabinetMaterialApplicator component  
   - "Texture Picker UI" + TexturePickerUI component

2. Create UI Panel with:
   - Scroll View (texture grid parent)
   - Texture button prefab (80×80px)
   - "Apply to Room" button
   - "Apply to Selected" button

3. Wire up references on TexturePickerUI:
   - textureGridParent → Content object from Scroll View
   - textureButtonPrefab → prefab from Assets
   - applyToRoomButton → button reference
   - applyToSelectedButton → button reference

4. Configure TextureLibraryManager:
   - Set texturesFolderPath (e.g., `C:/Users/info/Dropbox/PAC Library HQ/Textures/Colors`)
   - Right-click → "Reload Textures"

5. Test in Play mode:
   - Texture grid populates automatically
   - Click texture → highlights yellow
   - Click "Apply to Room" → all cabinets change
   - Select cabinet → "Apply to Selected" enables
   - Click "Apply to Selected" → only that cabinet changes

**Wireframe Selection:**
- Existing cabinets with MozBoundsHighlighter automatically use new wireframe
- Inspector fields: boundsInflation (1.0), wireframeColor (0,1,0,0.9), lineWidth (0.005)

### Future Enhancements
- Parse TextureGroups.dat for organized categories
- Save material choice per cabinet (export to .des)
- Material preview on hover
- Search/filter textures by name
- Custom PBR shader support

---

## [2025-12-11] Fixed Cabinet Positioning for All Wall Orientations

### Summary
- Fixed visual left/right determination for walls at any orientation
- Position calculation now measures from visual LEFT endpoint (not hardcoded wallStart)
- Added cabinet edge debug markers (yellow=left, red=right)
- Export to Mozaik now positions cabinets correctly

### Files
- `Assets/Scripts/CabinetWallSnapper.cs` – Fixed visual left logic to use correct endpoint based on wall orientation
- `Assets/Scripts/MozCabinetData.cs` – Fixed UpdateXPositionFromWorld to measure from visual left; updated gizmo visualization

### Behavior / Notes
- **Visual Left/Right Determination**: For X-aligned walls, visual left = higher X value; for Z-aligned walls, visual left = higher Z value
- **Position Calculation**: Now measures distance from visual LEFT edge to cabinet LEFT edge (was always using wallStart)
- **Wall Direction**: Direction vector now goes from visual left → visual right for correct positive distance
- **Debug Markers on Cabinet**: 
  - Yellow ball = cabinet LEFT edge (bounds.max.x for X-aligned walls)
  - Red ball = cabinet RIGHT edge (bounds.min.x)
- **Gizmo Visualization**: Magenta measurement line now starts from wall's yellow marker (visual left)
- **Export**: XPositionMm now exports correct distance for Mozaik to position cabinet properly

### The Bug
Previously, the system always measured from `wallStart` regardless of which endpoint was the visual "left". For a wall running from X=-3.48 to X=0.52:
- Visual left is at X=0.52 (wallEnd)
- But code measured from X=-3.48 (wallStart)
- This gave wrong distance for Mozaik export

### The Fix
1. Determine which endpoint is visual left based on coordinate values
2. Measure from THAT endpoint (not always wallStart)
3. Update both snapper and position calculator to use same logic
4. Update gizmos to visualize the actual calculation

### Test Steps
1. Snap cabinet to wall - yellow markers should touch (wall left = cabinet left)
2. Check console log - should show measurement from "Visual Left Edge"
3. Export to DES - cabinet should appear at correct position in Mozaik

---

## [2025-12-11] Enhanced DES Export with Product Support

### Summary
- Enhanced `MozaikDesJobExporter` to export both walls AND cabinets/products
- Added folder picker for easier Mozaik job selection
- Cabinets export with position, elevation, wall reference, and dimensions

### Files
- `Assets/Scripts/MozaikDesJobExporter.cs` – Complete rewrite with product export support

### Behavior / Notes
- **Folder Picker**: Right-click > "Pick Mozaik Job Folder" opens folder browser, auto-detects Room1.des
- **Export Options**: Toggle to export walls, products, or both
- **Product Export**: Exports minimal required attributes for Mozaik to recognize:
  - `UniqueID`, `ProdName`, `SourceLib`
  - `X` (position along wall in mm)
  - `Elev` (elevation from floor in mm)
  - `Wall` (reference like "1_1")
  - Dimensions: `Width`, `Height`, `Depth`
- **Wall Reference Mapping**: Cabinets automatically get correct wall reference from their `TargetWall`
- **Backup**: Creates `.FROMUNITY.bak` before overwriting

### Context Menu Options
- `Pick Mozaik Job Folder` – Browse to job folder
- `Pick Room File Directly` – Select specific .des/.sbk file
- `Export Room to Mozaik` – Run the export
- `Open Job Folder in Explorer` – Quick access to files

### Unity Implementation Checklist
1. **Setup**
   - Create empty GameObject named "Room Exporter"
   - Add `MozaikDesJobExporter` component
   
2. **Configure**
   - Right-click component > "Pick Mozaik Job Folder"
   - Select your Mozaik job folder (must have Room1.des)

3. **Export**
   - Add walls to scene (MozaikWall components)
   - Import cabinets and snap to walls
   - Right-click > "Export Room to Mozaik"

4. **Verify in Mozaik**
   - Open the job in Mozaik Designer
   - Check that walls and products appear at correct positions

### Known Limitations
- Products export as "position only" – Mozaik will rebuild construction details
- Rotation not yet calculated from Unity transform
- Single room export only (Room1.des)

---

## [2025-12-10] RuntimeWallSelector & Auto-Snap System (MVP Complete!)

### Summary
- Added RuntimeWallSelector for runtime wall management and selection
- Added RuntimeWallUI with big "ADD WALL" button
- Cabinets imported via MozImporterBounds now auto-snap to the selected wall
- Fixed Input System compatibility (uses InputSystemUIInputModule)
- Fixed deprecated API warnings (FindObjectOfType → FindFirstObjectByType, etc.)

### Files
- `Assets/Scripts/RuntimeWallSelector.cs` – **NEW** Manages wall selection, creation, and provides singleton access
- `Assets/Scripts/RuntimeWallUI.cs` – **NEW** Runtime UI with ADD WALL button and selected wall status
- `Assets/Scripts/MozImporterBounds.cs` – Added auto-snap integration with RuntimeWallSelector

### Behavior / Notes
- **Wall Selection**: Click on a wall in Play mode to select it; selected wall is highlighted with a tint
- **ADD WALL Button**: Creates a new wall at (0, 0, 0) with default dimensions; auto-selects the new wall
- **Auto-Snap**: When `autoSnapToSelectedWall = true` (default), imported cabinets snap to the currently selected wall
- **Events**: `OnWallSelected` and `OnWallCreated` events for other systems to react
- **Singleton Access**: `RuntimeWallSelector.Instance` provides easy access from any script

### Unity Implementation Checklist
1. **Setup Scene Objects**
   - Create empty GameObject named "Wall Manager"
   - Add `RuntimeWallSelector` component
   - Add `RuntimeWallUI` component (can be on same object or separate)

2. **Test Steps**
   - Play the scene
   - Click "ADD WALL" button - wall appears at origin
   - Click on wall to select it (should highlight)
   - Use MozImporterBounds to import a cabinet
   - Cabinet should auto-snap to the selected wall

### Future Work
- Connected walls at angles
- Outside miters where walls connect
- Wall positioning at different locations (not just origin)

---

## [2025-12-10] Cabinet Orientation Debug & Auto-Rotation

### Summary
- Added cabinet orientation debugging tools (gizmos, console logging)
- Added `autoRotateToFaceRoom` option that automatically rotates cabinet when snapping
- Cabinet front (+Z) now correctly faces into room, back (-Z) faces wall

### Files
- `Assets/Scripts/CabinetWallSnapper.cs` – Added debug visualization, `RotateToFaceRoom()`, auto-rotation in `SnapToWall()`
- `Assets/Scripts/Editor/CabinetWallSnapperEditor.cs` – Added "Rotate to Face Room" and "Log Orientation Info" buttons

### Behavior / Notes
- **Debug Gizmos**: When cabinet is selected, shows colored arrows:
  - Blue arrow = FRONT (+Z) - door side, faces into room
  - Red arrow = BACK (-Z) - against wall
  - Green arrow = LEFT (-X)
  - Yellow arrow = RIGHT (+X)
- **Auto-Rotation**: `autoRotateToFaceRoom = true` (default) rotates cabinet during snap
- **Manual Rotation**: "Rotate to Face Room" button applies rotation without repositioning
- **Debug Logging**: "Log Orientation Info" button prints detailed position/rotation info to console
- **Inspector fields** show current forward direction and Y rotation in real-time

### Test Steps
1. Select a cabinet with `CabinetWallSnapper` component
2. Observe orientation arrows in Scene view (if `showOrientationGizmos` is true)
3. Click "Log Orientation Info" to see detailed console output
4. Click "Snap to Wall" - cabinet should rotate to face room then position against wall
5. If cabinet was facing wrong way, it should now face correctly into the room

---

## [2025-12-10] Cabinet Snap to Wall with Elevation Support

### Summary
- Added elevation support to cabinet-wall snapping system
- Cabinets now snap to floor + elevation from `.moz` file data
- Walls now properly sit on floor when placed at Y=0

### Files
- `Assets/Scripts/MozCabinetData.cs` – **NEW** Component storing cabinet metadata (elevation, dimensions, wall ref)
- `Assets/Scripts/MozImporter.cs` – Added `ElevationMm`, dimensions, and `UniqueID` parsing to `MozCabinet`/`MozParser`
- `Assets/Scripts/MozImporterBounds.cs` – Attaches `MozCabinetData` component on import
- `Assets/Scripts/MozaikWall.cs` – Fixed wall visual offset so bottom aligns with transform position; added helper methods
- `Assets/Scripts/CabinetWallSnapper.cs` – Uses elevation from `MozCabinetData`, updates position data after snap

### Behavior / Notes
- **Wall positioning**: Wall transform position now represents bottom-center. Placing wall at Y=0 puts bottom on floor.
- **Elevation from .moz**: The `Elev` attribute in `<Product>` element is parsed and stored in `MozCabinetData.ElevationMm`
- **Snapper behavior**: 
  - `useElevationFromData = true` (default): Uses `MozCabinetData.ElevationMm`
  - `useElevationFromData = false`: Uses `manualElevationMm` field
- **Data updated after snap**: `MozCabinetData.XPositionMm` and `ElevationMm` are updated from world position after snapping

### Unity Implementation Notes
1. Import a `.moz` file using `MozImporterBounds` context menu
2. The imported cabinet root will have `MozCabinetData` component attached
3. Add `CabinetWallSnapper` component to the cabinet
4. Assign `targetWall` reference
5. Click "Snap to Wall" button in inspector

### Future Work
- Auto-placement next to existing cabinets
- Upper cabinet stacking on lowers
- Drag-to-adjust elevation in scene view

## [2025-12-11] Complete XML Roundtrip for DES Export

### Summary
- Added full XML roundtrip for cabinet data from .moz files
- TopShapeXml (cabinet shape, end types) preserved on export
- CabProdPartsXml (shelves, rods, hangers) preserved on export
- ProductInteriorXml (section layout) preserved on export
- ProductType, CurrentConst, Flags now correctly exported

### Files
- `MozCabinetData.cs` – Added TopShapeXml, CabProdPartsXml, ProductInteriorXml fields
- `MozImporter.cs` – Parse all XML elements from .moz file
- `MozImporterBounds.cs` – Copy XML data to cabinet component
- `MozaikDesJobExporter.cs` – Export stored XML or use defaults

### Behavior / Notes
- XML elements stored as strings for lossless roundtrip
- Shelves, closet rods, hangers export with correct positions
- Interior section layout preserved (controls where parts go in Mozaik)
- End types (EdgeType="14" = nothing) preserved via TopShapeXml

### Key Fields Roundtripped
| Field | Purpose |
|-------|---------|
| TopShapeXml | Cabinet shape, corner points, EdgeType values |
| CabProdPartsXml | All parts (F.Shelf, ClosetRod, Hanger, etc.) |
| ProductInteriorXml | Section layout with DivideType definitions |
| ProductType | Type/SubType/SubSubType classification |
| CurrentConst | 0=Faceframe, 1=Frameless, 2=Inset |
| Flags | 16-bit binary string for options |
