# unity-ui-style-rules.md

These rules keep UI for the Unity room designer consistent.

They sit on top of:

- `cline-global-rules.md`
- `unity-cline-rules.md` (especially `AppConfig.VisualConfig`)

---

## 1. Layout

Default layout:

- **Left:** tools / modes / navigation.
- **Center:** main canvas / viewport.
- **Right:** properties / inspector / numeric inputs.
- **Top:** global actions (file, undo/redo, view, settings).

Guidelines:

- Use consistent spacing (e.g. 4/8/12/16 px).
- Align controls to a grid; avoid “almost aligned”.
- Keep the main canvas roomy; don’t jam it against window edges.
- Prefer docked panels; only use floating windows when clearly modal/temporary.

---

## 2. Colors via `VisualConfig`

All UI colors come from `AppConfig.VisualConfig`.

Typical tokens:

- `ColorPrimary`
- `ColorPrimaryVariant`
- `ColorAccent`
- `ColorBackground`
- `ColorPanelBackground`
- `ColorBorder`
- `ColorTextPrimary`
- `ColorTextSecondary`
- `ColorDanger`
- `ColorWarning`
- `ColorSuccess`
- `ColorSelectionHighlight`

Rules:

- Don’t hard-code hex codes in multiple places.
- Use tokens from `VisualConfig` in UITK USS or UI code.
- If a new visual token is truly needed:
  - Name it clearly.
  - Add it to `VisualConfig`.
  - Mention it in your Change Summary.

Overall vibe:

- Calm, professional, readable.
- Accent colors used sparingly (selected tools, primary actions, important status).

---

## 3. Typography

Use a small, consistent hierarchy:

- `Title` – screen/page titles.
- `SectionHeader` – panel sections.
- `Label` – control labels.
- `Body` – normal text.
- `Caption` – smaller explanatory text.

Rules:

- Avoid more than 2 different weights (e.g. Regular + SemiBold) in a single panel.
- Labels are left-aligned above or left of fields.
- Avoid ALL CAPS except tiny labels.

---

## 4. Controls

### Buttons

- Rounded corners (subtle).
- Clear distinction from background.
- Min hit area ≈ 32×32 px.

States:

- Default, hover, pressed, disabled.
- Hover/pressed transitions fast (≈150–250 ms), not flashy.

Primary action:

- Uses `ColorPrimary` and high-contrast text.

Secondary:

- Outline or subtle fill using panel/background + border colors.

### Inputs

- Numeric inputs are first-class (dimensions, positions, etc.):
  - Label + field.
  - Units shown clearly (`in`, `mm`, etc.).
- Sliders are optional helpers; they do **not** replace numeric entry.
- Dropdowns:
  - For discrete options (material preset, wall type).
  - Show label and current selection.

### Toggles / Checkboxes

- Use for boolean options (snap to grid, show dimensions).
- Place near the area they affect.

---

## 5. Icons

- Simple, flat icons with consistent stroke weight.
- No random mixing of styles (no one glossy icon next to flat icons).
- Each tool/mode:
  - Has an icon plus label or tooltip.
  - Avoid unlabeled mystery icons.

Reuse icon concepts where possible so related tools look like they belong together.

---

## 6. Interaction & Motion

- Use motion sparingly:
  - Panel open/close.
  - Hover/selection transitions.
- Duration: ~150–250 ms.
- No bouncy/cartoonish easing.

Feedback:

- When geometry changes, give immediate visual feedback:
  - Highlights, outlines, dimension lines, or overlays.
- On invalid input:
  - Prefer inline messages near the control.
  - Use popups only for critical issues.

---

## 7. Unity Implementation

When you touch UI in Unity:

1. Prefer reusing existing:
   - Panels/prefabs.
   - USS styles.
   - Layout patterns.
2. Make UI read from `AppConfig.VisualConfig` where relevant:
   - Colors.
   - Line thickness / grid settings if visual.
3. Name things clearly:
   - `LeftToolPanel`, `RoomPropertiesPanel`, `WallToolButton`, etc.
4. For new UI surfaces:
   - Add/extend a short entry in `docs/UI_OVERVIEW.md`:
     - Panel name.
     - Position (left/right/top/overlay).
     - Purpose.
     - Key controls.

---

## 8. Proposing New UI

When you propose UI changes:

- State:
  - Where the UI lives (left/right/top/overlay).
  - What it controls.
- Reference:
  - The color tokens and text styles from `VisualConfig`.
- Provide a quick text mockup:

```text
Panel: Wall Tools (Left)

- Section: Add / Edit Walls
  - Button: Add Wall
  - Button: Split Wall

- Section: Snapping
  - Toggle: Snap to Grid
  - Toggle: Snap to Corners
```

Keep proposals concrete and aligned with this style.
