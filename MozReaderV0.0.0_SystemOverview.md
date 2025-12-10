# MozReader V0.0.0 - System Overview & Design Analysis

## Current Script Architecture

MozReader V0.0.0 consists of 6 main scripts that work together to import, visualize, and interact with Mozaik cabinet files in Unity.

---

## Core Scripts & Their Functions

### 1. **MozImporter.cs** - Basic Cabinet Importer
**Purpose**: Imports individual Mozaik .moz files and creates simple cube representations

**Key Features**:
- Parses XML from .moz files 
- Creates GameObject hierarchy for each cabinet
- Converts Mozaik coordinates (mm) to Unity coordinates (meters)
- Spawns cube primitives for each cabinet part

**Creates Hierarchy**:
```
"MozCabinet" (Root)
├── Part_001 (Cube)
├── Part_002 (Cube)
└── Part_003 (Cube)
```

**Usage**: Context menu "Import Moz" on GameObject with script attached

---

### 2. **MozImporterBounds.cs** - Enhanced Cabinet Importer  
**Purpose**: Extended version of MozImporter with bounds visualization and corner indicators

**Key Features**:
- Everything from MozImporter
- Calculates bounding box around all parts
- Creates transparent "Bounds" cube for visualization
- Generates 8 corner sphere indicators
- Auto-attaches MozBoundsHighlighter component

**Creates Hierarchy**:
```
"MozCabinet_WithBounds" (Root + MozBoundsHighlighter)
├── Part_001 (Cube)
├── Part_002 (Cube)
└── Bounds (Transparent cube)
    └── Corners (Parent)
        ├── Corner_00 (Sphere)
        ├── Corner_01 (Sphere)
        └── ... (8 total)
```

**Materials Support**:
- `panelMaterial` - For cabinet parts
- `boundsMaterial` - For transparent bounds box
- `cornerMaterial` - For corner sphere indicators

---

### 3. **MozBoundsHighlighter.cs** - Visibility Controller
**Purpose**: Controls when bounds and corners are visible based on selection

**Visibility Logic**:
- **Editor Mode**: Shows bounds when root GameObject is selected in hierarchy
- **Play Mode**: Shows bounds when `SetRuntimeSelected(true)` is called
- **Built Player**: Only runtime selection works

**Key Features**:
- `[ExecuteAlways]` - Works in both Edit and Play mode
- Controls both bounds renderer and all corner renderers simultaneously
- `showInPlayMode` toggle for runtime behavior

**Integration**: Auto-attached by MozImporterBounds during import

---

### 4. **MozRuntimeSelector.cs** - Click Selection System
**Purpose**: Handles mouse clicks in Game view to select cabinets

**Key Features**:
- Uses new Input System (Mouse.current)
- Raycasts from camera on left mouse click
- Finds MozBoundsHighlighter components via `GetComponentInParent()`
- Single-selection system (deselects previous when selecting new)
- Layer mask filtering for selective raycasting

**Setup**: Attach to empty GameObject, configure camera and layer settings

---

### 5. **RoomCamera.cs** - Scene Camera Controller
**Purpose**: Provides orbit/pan/zoom camera controls with auto-focus capabilities

**Control Scheme**:
- **Right-click + drag**: Orbit around target
- **Left-click + drag**: Pan view
- **Mouse wheel**: Zoom in/out

**Smart Features**:
- Auto-focuses on "Bounds" objects or "Moz" GameObjects on start
- Calculates appropriate viewing distance based on scene size
- `RecenterCamera()` method for manual refocusing
- Works with both individual cabinets and full scenes

**Integration**: Works automatically with MozImporterBounds output

---

### 6. **CabinetBoundsResizer.cs** - Interactive Resizer Tool
**Purpose**: Allows real-time resizing of imported cabinets by dragging corner handles

**Key Features**:
- Creates 8 red sphere handles at Bounds corners
- Green wireframe outline visualization
- Live part scaling and repositioning
- Preserves part thickness (Y-axis) during scaling
- Intelligent part categorization (full-width vs. fixed-width)

**Visual Elements**:
- Hides original bounds cube
- Creates LineRenderer wireframe outline
- Red sphere handles for corner manipulation

**Smart Scaling**:
- Full-width parts (rods, toe, top, shelves) stretch with cabinet width
- Other parts maintain proportional scaling
- Thickness always preserved

---

## System Interactions & Data Flow

### Import Workflow
```
1. User assigns .moz TextAsset to MozImporterBounds
2. MozImporterBounds parses file → creates parts + Bounds + corners
3. MozBoundsHighlighter auto-attached → controls visibility
4. RoomCamera auto-focuses on new Bounds if enabled
```

### Runtime Selection Workflow  
```
1. User clicks in Game view → MozRuntimeSelector raycasts
2. Hit detected → finds MozBoundsHighlighter via GetComponentInParent()
3. MozBoundsHighlighter.SetRuntimeSelected(true) called
4. Bounds + corners become visible
5. Previous selection automatically deselected
```

### Camera Integration
```
1. RoomCamera searches for "Bounds" objects or "Moz" roots
2. Calculates combined bounds of scene
3. Positions camera at appropriate distance and angle
4. Provides smooth orbit/pan/zoom controls
```

---

## Component Dependencies

**MozImporter** → *Standalone*

**MozImporterBounds** → *Uses MozImporter parsing logic*

**MozBoundsHighlighter** → *Requires Renderer references*

**MozRuntimeSelector** → *Requires MozBoundsHighlighter on target objects*

**RoomCamera** → *Works with any scene, enhanced by Bounds objects*

**CabinetBoundsResizer** → *Requires MozImporterBounds output structure*

---

## Strengths of Current Design

### ✅ **Clean Separation of Concerns**
- Import logic separate from visualization
- Selection system separate from rendering
- Camera system works independently

### ✅ **Flexible Material System**
- Different materials for parts, bounds, and corners
- Easy visual customization

### ✅ **Dual-Mode Operation**
- Works in Editor (selection-based visibility)
- Works in Play mode (click-based selection)

### ✅ **Robust Input Handling**
- New Input System integration
- Layer mask filtering
- Proper parent-child component finding

### ✅ **Smart Camera Behavior**
- Auto-focus on imported content
- Appropriate distance calculation
- Preserves user control after auto-setup

---

## Areas for Simplification

### 🔄 **Potential Consolidation Opportunities**

#### **Option 1: Merge MozImporter into MozImporterBounds**
**Current**: Two separate import scripts with overlapping code
**Simplified**: Single import script with optional bounds generation

**Benefits**:
- Eliminates code duplication
- Cleaner file structure
- Single point of maintenance

**Implementation**:
```csharp
public class MozImporter : MonoBehaviour
{
    [Header("Bounds Visualization (Optional)")]
    public bool generateBounds = true;
    public Material boundsMaterial;
    public Material cornerMaterial;
    // ... rest of current MozImporterBounds functionality
}
```

#### **Option 2: Auto-Setup Helper Script**
**Current**: User needs to manually create MozRuntimeSelector GameObject
**Simplified**: Auto-created during first import

**Implementation**:
```csharp
// In MozImporter, after successful import:
private void EnsureRuntimeSelectorExists()
{
    if (FindObjectOfType<MozRuntimeSelector>() == null)
    {
        GameObject selectorGO = new GameObject("Moz Runtime Selector");
        selectorGO.AddComponent<MozRuntimeSelector>();
    }
}
```

#### **Option 3: Combined Camera + Selection System**
**Current**: Separate MozRuntimeSelector and RoomCamera scripts
**Simplified**: Single "MozSceneController" script

**Benefits**:
- One script handles all scene interaction
- Unified input handling
- Simplified setup

---

## Recommended Simplifications

### **High Priority (Easy Wins)**

1. **Merge Import Scripts**
   - Consolidate MozImporter and MozImporterBounds
   - Add toggle for bounds generation
   - Eliminate code duplication

2. **Auto-Setup Scene Controller**
   - Create MozRuntimeSelector automatically on first import
   - Add to scene if missing
   - Reduce manual setup steps

### **Medium Priority (User Experience)**

3. **Unified Scene Controller**  
   - Combine MozRuntimeSelector + RoomCamera into MozSceneController
   - Handle all mouse inputs in one place
   - Cleaner Inspector organization

4. **Material Preset System**
   - Create ScriptableObject material presets
   - "Modern", "Classic", "Debug" material sets
   - One-click visual style switching

### **Low Priority (Advanced Features)**

5. **Component Auto-Wiring**
   - Auto-find and assign material references
   - Smart defaults for missing assignments
   - Validation and warning system

---

## Minimal Setup Workflow (After Simplifications)

### **Current Setup (6 steps)**:
1. Create GameObject with MozImporterBounds
2. Assign .moz file and materials  
3. Run "Import Moz With Bounds"
4. Create empty GameObject for MozRuntimeSelector
5. Attach MozRuntimeSelector script
6. Configure camera and layer settings

### **Simplified Setup (2 steps)**:
1. Create GameObject with unified MozImporter
2. Assign .moz file → everything auto-configured

---

## Current File Structure Assessment

**Complexity Level**: Medium
- 6 scripts with clear responsibilities
- Good separation of concerns  
- Some code duplication between importers

**Maintenance Burden**: Low-Medium
- Well-documented code
- Clear naming conventions
- Some interdependencies to track

**User Friendliness**: Medium
- Requires understanding of multiple components
- Manual setup steps
- Good once configured

**Recommendation**: Implement High Priority simplifications to improve user experience while maintaining the solid architectural foundation.

---

## Conclusion

The current V0.0.0 system demonstrates excellent software engineering principles with clean separation of concerns, robust error handling, and flexible material systems. The main opportunities for improvement lie in **reducing setup complexity** rather than architectural changes.

The proposed simplifications would maintain all current functionality while significantly improving the user experience, especially for new users getting started with the system.
