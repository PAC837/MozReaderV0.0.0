# unity-admin-extensibility-rules.md

Goal: make it easy to build an **admin panel later** without rewriting the app.

Admin users should eventually be able to:

- Change key visual and behavior settings.
- Manage catalogs of placeable objects.

You’re not building the admin UI now—just making sure the code is ready for it.

---

## 1. Canonical Config: `AppConfig`

Use a single root ScriptableObject:

- `AppConfig`
  - `VisualConfig`
  - `BehaviorConfig`
  - `CatalogConfig`

These are the **only** canonical config types for this purpose. All other docs refer to them by these names.

### 1.1 VisualConfig

Holds visual tokens:

- Color tokens (as in UI style rules).
- Line thickness, grid opacity.
- Theme-related flags if needed.

UI and visuals should **read from** `VisualConfig`, not from scattered constants.

### 1.2 BehaviorConfig

Holds behavior knobs:

- Units (in/mm).
- Snap increments (grid, angle).
- Default wall heights, depths, panel thickness.
- Global flags:
  - Show dimensions / rulers.
  - Allow overlapping / collision checking.

Code that needs these values should go through `BehaviorConfig`.

### 1.3 CatalogConfig

Holds catalogs of objects/presets (ScriptableObjects or serializable assets):

- Entries with:
  - ID/key.
  - Display name.
  - Category.
  - Prefab reference.
  - Metadata (dimensions, tags, placement rules).

The goal: adding a new object later should mostly mean “add an entry + prefab”, not “add a new `if` in 10 places”.

---

## 2. No Scattered Magic Constants for Admin Stuff

If a value:

- Would reasonably be changed by an admin (visual feel, units, snapping, defaults), or
- Affects what objects users can place,

…it belongs in `VisualConfig`, `BehaviorConfig`, or `CatalogConfig`, **not** hard-coded in many scripts.

If you keep something as an internal constant (e.g., tiny epsilon for float comparisons), say so in your Change Summary:

> “Kept `<X>` as an internal constant (not admin-configurable) because `<reason>`.”

---

## 3. Objects as Data, Not Branches

When working on “objects in the room”:

- Treat them as data-driven where possible:
  - Catalog entries + prefabs + components.
- Avoid long `switch`/`if` chains on IDs/names scattered around.

Prefer:

- A central registry (from `CatalogConfig`) that maps IDs → prefab/config.
- Interfaces/components to describe capabilities:
  - `IPlaceableObject`
  - `ISnapToWall`
  - `IHasDimensions`, etc.

Object-specific behavior should live on the prefab/components, not in a giant manager class.

---

## 4. Preparing for Admin UI

Make configs naturally UI-friendly:

- Group related fields logically.
- Use clear names and ranges.
- Avoid deeply nested, weird data structures for core settings.

Optionally extend `docs/ADMIN_CONFIG.md` when you add new admin-worthy settings:

- Setting name.
- Purpose.
- Range/options.
- Which config it lives in (`VisualConfig`, `BehaviorConfig`, `CatalogConfig`).

---

## 5. Don’t Over-Engineer

These rules are about **direction**, not turning everything into a config slider.

Good candidates for config:

- Anything affecting user-visible look/feel.
- Defaults and global behavior flags.
- Which objects exist and how they appear.

Bad candidates:

- Deep internal implementation details.
- One-off constants that are unlikely to change.

When in doubt, ask:

> “Would an admin realistically want to change this later?”

If yes → config.  
If no → keep it simple.

---

## 6. Workspace Expectation

In this Unity workspace:

- Use `AppConfig` and its child configs as the single source of truth.
- Keep behaviors and objects wired through those configs/catalogs when they’re admin-relevant.
- Continue to follow:
  - `cline-global-rules.md`
  - `unity-cline-rules.md`
  - `unity-ui-style-rules.md`

So when it’s time to build an admin panel, it feels like a thin UI layer on top of a system that was ready for it all along.
