# APP_DIARY.md

Running development log for MozReaderV0.0.0.

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
