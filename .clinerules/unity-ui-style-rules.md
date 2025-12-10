# Unity UI Style Rules (SingleDraw / Room Designer)

You are Cline, an AI coding assistant working on SingleDraw / room-designer style tools in Unity.

These rules define the **visual and interaction style** of the app so that all new UI you propose feels consistent.

Overall vibe:

- **Level of complexity:** In between something like *17 Squares* and *Fusion 360*.
  - More polished and “pro” than a basic web app.
  - Less dense / intimidating than full CAD.
- **Tone:** Calm, focused, “tool for serious work” but not sterile or sci-fi.

---

## 1. Layout Principles

When creating or modifying UI layout:

1. **Three-zone mental model**
   - **Left:** Navigation / modes / tools list.
   - **Center:** Main canvas / viewport (room, walls, cabinets, etc.).
   - **Right:** Properties / inspector / numeric inputs.
   - **Top bar:** Global actions (file, undo/redo, view modes, settings).

2. **Spacing & alignment**
   - Use consistent spacing increments (e.g., 4 / 8 / 12 / 16 px).
   - Align content to a simple grid; avoid “almost aligned” elements.
   - Maintain generous padding around the main canvas so it never touches the window edges.

3. **Density**
   - Avoid cramped panels with lots of tiny controls.
   - Group related controls into **sections** with clear headings.
   - Prefer fewer, well-chosen controls visible by default; advanced options can be collapsible.

4. **Panel behavior**
   - Panels should dock cleanly to edges (left/right/top).
   - Avoid random floating windows unless they are clearly modal or temporary.

---

## 2. Color System (Design Tokens)

Do **not** hard-code arbitrary colors in each UI element.

Instead:

1. Assume or create a central style reference (e.g., `UIStyleConfig`, `ScriptableObject`, or a constants file) with tokens:

   - `Color Primary`
   - `Color PrimaryVariant`
   - `Color Accent`
   - `Color Background`
   - `Color PanelBackground`
   - `Color Border`
   - `Color TextPrimary`
   - `Color TextSecondary`
   - `Color Danger`
   - `Color Warning`
   - `Color Success`

2. When you need a color:
   - **Reuse these tokens** instead of inventing new ones.
   - If you truly need a new token, explain why and suggest a name, e.g. `Color SelectionHighlight`.

3. Visual intent:
   - Backgrounds: low-contrast, calm.
   - Primary / accent: used sparingly (selected tool, key actions).
   - Errors / warnings: clearly visible but not neon.

*(Later, Phill will define actual hex values; until then, focus on consistent token usage.)*

---

## 3. Typography

Assume a single primary typeface (system UI or one chosen by Phill).

Rules:

1. **Hierarchy levels**
   - `Title` – Large, used for main page/screen titles.
   - `SectionHeader` – Medium, used for panel section labels.
   - `Label` – Small/medium, used for control labels.
   - `Body` – Default text.
   - `Caption` – Smaller explanatory text / hints.

2. **Usage**
   - Never mix more than 2 font weights in one panel (e.g., Regular + SemiBold).
   - Labels are left-aligned next to or above controls.
   - Avoid all-caps except for small UI labels where it clearly improves readability.

If you introduce a new text style in code, explain where it fits in this hierarchy.

---

## 4. Controls & Components

### 4.1 Buttons

- Default buttons:
  - Slightly rounded corners (e.g., 4–6 px radius).
  - Clear contrast with the background.
  - Min touch/click area ≈ 32 x 32 px.
- Primary action button:
  - Uses `Color Primary` background, `TextPrimary` or white text.
  - Should be visually distinct but not gigantic.
- Secondary actions:
  - Use outline or subtle fill (e.g., `PanelBackground` with a border).

States:

- **Default:** Calm, flat or subtle shadow.
- **Hover:** Slightly brighter or raised; cursor change.
- **Pressed:** Slightly darker / inset; quick, subtle transition.
- **Disabled:** Lower contrast, never pure grey on grey; still legible.

### 4.2 Inputs (numeric, dropdowns, sliders)

- Numeric inputs (dimensions, offsets, angles) are **critical**:
  - Label on the left or above, value field on the right.
  - Units indicated clearly (e.g., `in`, `mm`), either in the label or as a suffix.
- Sliders are optional, not a replacement for precise numeric input.
- Dropdowns:
  - Use for discrete choices (e.g., “Wall Type”, “Material Preset”).
  - Keep label + selected value always visible.

### 4.3 Toggles / checkboxes

- Use toggles for on/off states that affect the current context (e.g., “Snap to wall”, “Show grid”).
- Place them near the thing they control (e.g., grid settings near canvas/view options).

---

## 5. Iconography

- Keep icons **simple, 2D, and consistent**:
  - Prefer outline or duotone icons with consistent stroke width.
  - Avoid mixing glossy, 3D, or highly detailed icons with flat ones.
- Each tool or mode should have:
  - Icon + label (at least in tooltips or on hover), not icon-only mystery buttons.
- Reuse icon patterns:
  - Transform tools, snapping, measurements should feel similar to other pro tools, not cartoon-y.

---

## 6. Interaction & Motion

- Animation:
  - Use subtle transitions (150–250 ms) for panel open/close, hover, and selection changes.
  - No large, bouncy, or playful animations; this is a work tool, not a game menu.
- Feedback:
  - Actions that affect geometry or layout should give **immediate visual feedback** (highlighted objects, temporary outline, etc.).
  - If something fails (e.g., invalid dimension), show a small inline error near the control.

---

## 7. Unity Implementation Rules (UI)

When you modify or add UI in Unity:

1. **Prefer reuse first**
   - Reuse existing prefabs, UI Toolkit styles, or layout patterns whenever possible.
   - If you must create a new prefab/panel, model it after an existing one.

2. **Centralize style**
   - Do **not** hard-code colors, fonts, or spacing in many scripts.
   - Use:
     - A `UIStyleConfig` ScriptableObject, or
     - A central static class / SO with constants, or
     - A Unity UI Toolkit `.uss` (if you’re using UITK).
   - Any new style tokens should be added there and documented.

3. **Naming**
   - Name panels and controls descriptively:
     - `LeftToolPanel`, `RoomPropertiesPanel`, `WallToolButton`, etc.
   - Avoid temporary names like `NewPanel`, `Panel (1)` in committed scenes/prefabs.

4. **Documentation**
   - For any new non-trivial UI surface, add a short entry to a doc like `docs/UI_OVERVIEW.md`:
     - What the panel is.
     - Where it lives in the layout.
     - Key controls.

---

## 8. When Cline Proposes New UI

Whenever you propose a new UI panel, window, or control set, you must:

1. Specify:
   - Where it lives (left / right / top / part of canvas).
   - How it fits into the three-zone layout.
2. Explain:
   - Which existing patterns it reuses (buttons, sliders, property layout).
3. Confirm:
   - Which style tokens it uses (colors, text styles).
4. Provide:
   - A brief text mockup (hierarchy) of the panel:

     - Panel: `Wall Tools`
       - Section: `Add / Edit Walls`
         - Button: `Add Wall`
         - Button: `Split Wall`
       - Section: `Snapping`
         - Toggle: `Snap to Grid`
         - Toggle: `Snap to Corners`

This ensures new UI feels like it belongs to the same family as the rest of the app.
