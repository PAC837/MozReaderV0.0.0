# Visual Setup Guide - MozReader Unity

One-time setup for the visual systems (managers, floor, materials).

---

## 🎯 Quick Setup (One-Time)

**Add these components to a "Managers" GameObject:**
1. `TextureLibraryManager` - handles all materials
2. `FloorManager` - auto-creates floor
3. `RuntimeWallSelector` - wall selection
4. `MozRuntimeSelector` - cabinet selection
5. `CabinetMaterialApplicator` - applies textures
6. `CabinetLibraryManager` - spawns cabinets (optional)

That's it! Floor auto-creates. Walls come from "ADD WALL" button. Cabinets import with proper materials.

---

## 1. Create Managers GameObject

1. In Hierarchy: **Right-click → Create Empty**
2. Name it `Managers`

### Add Required Components:

| Component | Purpose |
|-----------|---------|
| **TextureLibraryManager** | Default materials, texture loading |
| **FloorManager** | Auto-creates floor on play |
| **RuntimeWallSelector** | Wall clicking/selection |
| **MozRuntimeSelector** | Cabinet clicking/selection |
| **CabinetMaterialApplicator** | Apply materials to cabinets |

### Optional Components:
| Component | Purpose |
|-----------|---------|
| CabinetLibraryManager | Spawn cabinets from library |
| MozImporterBounds | Import .moz files (can be separate object) |
| MozaikDesJobExporter | Export to Mozaik (can be separate object) |

---

## 2. Configure TextureLibraryManager

In Inspector:
- **Textures Folder Path**: `C:/Users/info/Dropbox/PAC Library HQ/Textures/Colors` (or your path)
- **Default Wall Color**: (0.85, 0.85, 0.85) - light grey
- **Default Chrome Color**: (0.8, 0.8, 0.82) - metallic look
- **Chrome Smoothness**: 0.9
- **Default Floor Color**: (0.7, 0.7, 0.65) - tan

---

## 3. Configure FloorManager

In Inspector:
- **Auto Create Floor**: ✓ (checked)
- **Floor Size**: 20 (meters)
- **Floor Material**: Leave empty (uses TextureLibraryManager default)

**Result**: Floor auto-creates on Play with proper material!

---

## 4. Runtime Usage

### Walls
- Click **"ADD WALL"** button in UI
- Wall appears with grey material automatically
- Click wall to select it

### Cabinets
- Use MozImporterBounds to import .moz files
- Cabinets get proper materials:
  - Panels → cabinet material (first texture or beige)
  - Rods → chrome cylinders
  - Hangers → chrome material

### Materials
- Use texture picker UI to change cabinet materials
- "Apply to Room" or "Apply to Selected"

---

## 5. Material Defaults Summary

| Part Type | Material | Visual |
|-----------|----------|--------|
| Cabinet panels | First texture or beige | Wood/melamine look |
| Walls | Grey (0.85, 0.85, 0.85) | Matte grey |
| Closet rods | Chrome metallic | Shiny metal |
| Hangers | Chrome metallic | Shiny metal |
| Floor | Tan (0.7, 0.7, 0.65) | Neutral ground |

---

## 6. Troubleshooting

### Pink Materials?
- Ensure `TextureLibraryManager` component exists in scene
- Check Console for shader errors
- URP must be properly configured

### No Floor?
- Add `FloorManager` component to Managers
- Check `autoCreateFloor` is enabled
- Or right-click → "Create Floor"

### Rods are Cubes?
- Part name must contain "rod" or "closetrod"
- Re-import the cabinet

---

## Complete Hierarchy Example

```
Scene
├── Managers
│   ├── TextureLibraryManager
│   ├── FloorManager
│   ├── RuntimeWallSelector
│   ├── MozRuntimeSelector
│   ├── CabinetMaterialApplicator
│   └── CabinetLibraryManager (optional)
├── Main Camera (with RoomCamera)
├── Directional Light
├── Floor (auto-created by FloorManager)
├── [Walls created by ADD WALL button]
└── [Cabinets from imports]
```

---

## Change Log

| Date | Change |
|------|--------|
| 2024-12-18 | Simplified - removed manual wall/floor creation steps |
| 2024-12-18 | Added FloorManager for auto-floor |
