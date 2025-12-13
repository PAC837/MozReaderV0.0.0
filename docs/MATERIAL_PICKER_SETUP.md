# Material Picker System - Setup Instructions

Complete setup guide for the texture/material picker feature.

---

## Overview

This system allows users to:
- Pick materials from a grid of texture preview squares
- Apply materials to entire room (all cabinets)
- Apply materials to individual selected cabinets
- Admin configures which texture folder to use

**Components:**
- `TextureLibraryManager` - Loads textures from folder
- `CabinetMaterialApplicator` - Applies materials to cabinets
- `TexturePickerUI` - UI with texture grid and buttons

---

## Unity Scene Setup

### 1. Create Manager Objects

Create 3 empty GameObjects in your scene:

#### A. Texture Library Manager
1. Create empty GameObject named "Texture Library Manager"
2. Add `TextureLibraryManager` component
3. Configure:
   - **Textures Folder Path** (Admin): `C:/Users/info/Dropbox/PAC Library HQ/Textures/Colors`
   - Or use: `Mozaik Files Examples/Textures/Colors` (for testing)

#### B. Cabinet Material Applicator
1. Create empty GameObject named "Cabinet Material Applicator"
2. Add `CabinetMaterialApplicator` component
3. Default exclude patterns should work (Rod, Insert, Hardware, Hanger)

#### C. Texture Picker UI
1. Create empty GameObject named "Texture Picker UI"
2. Add `TexturePickerUI` component
3. Configure (see UI Setup below)

---

## 2. Create UI Elements

### A. Texture Grid Panel

1. **Create Panel** (Right-click in Hierarchy: UI → Panel)
   - Name: "Texture Picker Panel"
   - Anchor: Right side of screen
   - Recommended size: 400px width × full height

2. **Add Scroll View** (Right-click Panel: UI → Scroll View)
   - Name: "Texture Grid Scroll"
   - Remove horizontal scrollbar
   - Vertical scrollbar only

3. **Setup Content Area**
   - Select "Content" child object
   - Add `Content Size Fitter` component:
     - Horizontal Fit: Preferred Size
     - Vertical Fit: Preferred Size
   - This will be your `textureGridParent` reference

### B. Create Texture Button Prefab

1. **Create Button** (Right-click Hierarchy: UI → Button)
   - Name: "TextureButtonPrefab"
   
2. **Configure Button**
   - Size: 80×80 pixels (matches `textureSquareSize`)
   - Image component: Will show texture
   
3. **Add Text Child** (optional, for labels)
   - Right-click button: UI → Text
   - Position below image
   - Font size: 10-12

4. **Save as Prefab**
   - Drag to `Assets/` folder
   - Delete from Hierarchy

### C. Create Apply Buttons

1. **Create "Apply to Room" Button**
   - Position below texture grid
   - Text: "Apply to Room"
   
2. **Create "Apply to Selected" Button**
   - Position below "Apply to Room"
   - Text: "Apply to Selected Cabinet"

---

## 3. Wire Up References

### On TexturePickerUI Component:

- **Texture Grid Parent**: Drag the "Content" object from Scroll View
- **Texture Button Prefab**: Drag your prefab from Assets
- **Apply To Room Button**: Drag the "Apply to Room" button
- **Apply To Selected Button**: Drag the "Apply to Selected" button

### Settings:
- **Texture Square Size**: 80 (matches button prefab size)
- **Grid Spacing**: 5
- **Grid Columns**: 4 (or 0 for auto)

---

## 4. Test the System

### A. Configure Texture Path

1. Select "Texture Library Manager"
2. Set **Textures Folder Path** to your texture folder
3. Right-click component → "Reload Textures"
4. Check Console for "Loaded X textures successfully"

### B. Runtime Testing

1. **Play the scene**
2. Texture grid should populate with preview squares
3. Click a texture → should highlight yellow
4. Click "Apply to Room" → all cabinets change material
5. Select a cabinet → "Apply to Selected" enables
6. Click "Apply to Selected" → only that cabinet changes

---

## Folder Structure

```
Scene Hierarchy:
├─ Canvas
│  └─ Texture Picker Panel
│     ├─ Texture Grid Scroll
│     │  └─ Viewport
│     │     └─ Content (textureGridParent)
│     ├─ Apply to Room Button
│     └─ Apply to Selected Button
│
├─ Texture Library Manager (TextureLibraryManager)
├─ Cabinet Material Applicator (CabinetMaterialApplicator)
└─ Texture Picker UI (TexturePickerUI)
```

---

## Admin Configuration

### Changing Texture Folder

1. Select "Texture Library Manager" GameObject
2. Update "Textures Folder Path" field
3. Right-click component → "Reload Textures"

**Path Examples:**
- `C:/Users/info/Dropbox/PAC Library HQ/Textures/Colors`
- `C:/MozaikData/TextureGroups/Colors`
- `C:/Program Files/Mozaik/Textures/Colors`

---

## How It Works

### Load Flow:
1. `TextureLibraryManager.Start()` → Loads all JPG/PNG files
2. Creates Texture2D for each image
3. Creates Material with Standard shader + texture
4. Stores in `loadedTextures` list

### UI Flow:
1. `TexturePickerUI.Start()` → Generates button grid
2. Each button shows texture preview
3. Click button → stores `_selectedTexture`
4. Click "Apply" → calls `CabinetMaterialApplicator`

### Application Flow:
1. Find all `MozCabinetData` components (cabinet roots)
2. Get all `Renderer` components in each cabinet
3. Skip excluded parts (rods, inserts, bounds)
4. Apply selected material

---

## Troubleshooting

### Textures Not Loading
- Check folder path is correct
- Ensure files are .jpg or .png
- Check Console for error messages
- Right-click → "Reload Textures"

### Buttons Not Appearing
- Check `textureGridParent` reference is set
- Check `textureButtonPrefab` reference is set
- Ensure prefab has Button, Image, and optional Text components

### Apply Buttons Not Working
- Check button references are set
- Ensure singletons are initialized (MozRuntimeSelector, CabinetMaterialApplicator)
- Check Console for error messages

### Materials Applied to Wrong Parts
- Add part name patterns to `excludePartNames` list
- Patterns are case-insensitive substring matches

---

## Future Enhancements

- Parse TextureGroups.dat for organized categories
- Save material choice per cabinet (export to .des)
- Material preview on hover
- Search/filter textures
- Custom shader support
- PBR material properties (metallic, roughness)
