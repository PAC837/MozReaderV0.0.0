# Unity Admin & Extensibility Rules (SingleDraw / Room Designer)

You are Cline, an AI coding assistant working on SingleDraw / room-designer style tools in Unity.

These rules define how to keep the app **future-friendly** for an eventual **admin panel** and user-controlled configuration, without over-engineering everything today.

High-level goals:

- Major visual and behavioral options should be **centralized and data-driven**, not scattered as hard-coded constants.
- The app should be able to support a future **Admin UI** where:
  - An admin user can adjust key settings (colors, units, snapping behavior, etc.).
  - An admin user can add or manage **user objects / catalog items** without rewriting core logic.

You are **not** required to build the admin panel now, but you **must** design new features so that an admin panel can be added later without a full rewrite.

---

## 1. Centralized Configuration, Not Magic Constants

When introducing new settings that impact app-wide behavior or visuals (especially ones a future admin might want to tweak):

- **Do not**:
  - Hard-code the value in multiple scripts.
  - Copy-paste the same constant across the codebase.

- **Do**:
  - Define the setting in a **single source of truth**, such as:
    - A ScriptableObject (e.g., `AppConfig`, `VisualConfig`, `BehaviorConfig`).
    - A central static config class (if ScriptableObjects are not practical).
  - Access it via that config object everywhere.

Prefer a structure like:

- `AppConfig` (top-level):
  - `VisualConfig` (colors, theme, line thickness, grid opacity, etc.).
  - `BehaviorConfig` (snapping, default units, angle increments, etc.).
  - `CatalogConfig` or `LibraryConfig` (object catalogs, default sets, etc.).

When adding a new setting that might make sense in an admin panel later, explain briefly in your change summary where in config it lives.

---

## 2. Visual Options & Theming

These rules complement the UI Style rules and focus on **admin-ready theming**.

- Visual settings that an admin may eventually want to change (examples):
  - Color tokens (primary, accent, background, panel background, selection highlight).
  - Grid color and opacity.
  - Line thickness / edge highlight styles.
  - Default theme (light/dark modes, if supported later).

For such settings:

1. Represent them in a `VisualConfig` or equivalent ScriptableObject.
2. Have the UI and rendering systems **read from the config**, not from hard-coded colors.
3. Avoid tying visual options to specific scenes or prefabs; they should be controlled from a central config so a future admin panel can edit that config.

If you must introduce a new “visual constant,” first consider whether it belongs in `VisualConfig`. If yes, add it there.

---

## 3. Behavior Options (Units, Snapping, Constraints)

Behavior-level options a future admin might control include:

- Default units (e.g., inches vs mm).
- Grid spacing and snap increments.
- Angle snapping options.
- Default wall height, cabinet depth, or other basic design defaults.
- Toggles such as:
  - “Enable collision checking”
  - “Allow overlapping objects”
  - “Show dimension guides”

When implementing or extending these behaviors:

1. Represent them via **config flags or numeric fields** in `BehaviorConfig` (or equivalent).
2. Make the runtime systems read from `BehaviorConfig` at runtime, not from hard-coded values.
3. When a new behavior is clearly global (affects everything), keep its setting in one central place so an admin panel could change it.

Avoid wiring behavior deeply into a single monolithic manager with hard-coded branching; favor smaller, data-driven switches that can be toggled via config.

---

## 4. Object / Catalog Extensibility (User-Addable Objects)

The app should be able to support a future where **admin users can add their own objects** to a catalog/library.

To make that possible, follow these guidelines when working on object systems:

1. **Treat objects as data entries**, not special cases in code.
   - Use ScriptableObjects, JSON, or another serializable format to define:
     - Object ID / key
     - Display name
     - Category (e.g., cabinets, walls, fixtures)
     - Associated prefab(s)
     - Metadata (dimensions, tags, behaviors, etc.).

2. **Avoid hard-coding specific object types** throughout the codebase:
   - Do not write long `if`/`switch` chains scattered across multiple files that check for specific IDs or names.
   - Instead, use:
     - A registry/lookup (e.g., `ObjectCatalog`, `PrefabRegistry`) keyed by ID.
     - Interfaces or components that encode behavior (e.g., `IPlaceableObject`, `ICanSnapToWall`).

3. **Design for new objects without code changes**:
   - New object types should ideally be created by:
     - Adding a new entry to a catalog asset / config file.
     - Assigning components / behaviors via prefab composition.
   - Code should work off these abstractions instead of needing a new `case` block for each object whenever possible.

When you must add an object-specific exception, call it out and describe how it might be generalized later (e.g., via metadata or interfaces).

---

## 5. Future Admin Panel: What You’re Preparing For

You are not building the admin panel now, but you are **preparing** the codebase for it by:

- Ensuring there is:
  - A central `AppConfig` / set of config ScriptableObjects.
  - A catalog or registry structure for objects.
- Keeping configs and catalogs structured so they can be:
  - Edited by tools or UI later.
  - Serialized and saved per project/user if needed.

When you add new configuration that is likely to appear in the admin panel:

1. Add it to the appropriate config asset/class.
2. Add a brief note to a doc such as `docs/ADMIN_CONFIG.md` (if it exists) describing:
   - The setting’s name.
   - What it controls.
   - Expected value range or options.

If `docs/ADMIN_CONFIG.md` does not exist yet and you introduce several admin-worthy settings, you may create it with a simple structure:

- **Section per config type**
- Bullet list of setting names and descriptions.

---

## 6. Avoiding Over-Engineering

These rules are **directional**, not a mandate to wrap every tiny value in a config layer.

Use judgment:

- Things that are **clearly internal and unlikely to be admin-facing** (e.g., a small internal tolerance to avoid float jitter) do *not* need to go into admin-facing config.
- Things that affect:
  - Overall look and feel,
  - User-level behavior,
  - The set of available placeable objects,

  are strong candidates for central config and/or catalog structures.

When in doubt, briefly mention in your summary:

> “I treated `<X>` as an internal constant (not admin-configurable) because `<reason>`.”

---

## 7. Summary of Expectations

When you implement or modify features in this workspace:

- **Ask yourself**:
  - “If we build an admin panel later, could it reasonably control this via a config or catalog?”
- If the answer is “yes”:
  - Put the relevant values into a central config or catalog.
  - Access them data-first rather than hard-coding them all over.
- Continue to follow:
  - The existing Unity rules for modularity, script length, and documentation.
  - The UI style rules for consistent look & feel.

The goal is to make adding an admin panel later **a natural next step**, not a painful rewrite.
