# APP_DIARY.md

Running development log for MozReaderV0.0.0.

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
