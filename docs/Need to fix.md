# APP_DIARY.md

Running development log for MozReaderV0.0.0.

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
