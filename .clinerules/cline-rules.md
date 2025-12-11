# unity-cline-rules.md

These rules apply to the **Unity / SingleDraw / room-designer** workspace, in addition to `cline-global-rules.md`.

---

## 0. When to Use the Unity Feature Workflow

Use `unity-feature-workflow.md` when:

- Implementing a new feature/tool/mode.
- Changing geometry/import/export behavior.
- Doing a multi-script refactor.

For **small, local Unity fixes** (single script, obvious bug, no new behavior design):

- You can skip the full feature workflow.
- Still follow:
  - Global rules.
  - The Change Summary format.
  - The Implementation Checklist if Unity setup changed.

---

## 1. App Diary (`docs/APP_DIARY.md`)

This is the project’s running dev log.

### 1.1 When to update

Update the diary when you:

- Add a new feature.
- Change behavior in a visible way.
- Do a non-trivial refactor.

Skip the diary for trivial edits (typos, comments, tiny private refactors).

Use this format:

```md
## [YYYY-MM-DD] <Short Title>

### Summary
- 1–3 bullets: what changed and why.

### Files
- `Path/To/FileA.cs` – quick note.
- `Path/To/FileB.cs` – quick note.

### Behavior / Notes
- How it behaves now.
- Gotchas, assumptions, limitations.
```

### 1.2 Using the diary

When you rely on the diary:

- Say so (“According to APP_DIARY…”).
- Treat it as possibly out of date.
- If a diary note conflicts with code or with your new plan, call that out.

You don’t need to re-ask about the same assumption multiple times in one conversation.

---

## 2. Unity Implementation Checklist (Single Source of Truth)

Any time your changes require Unity editor setup (prefabs, components, wiring), include:

```md
## Unity Implementation Checklist

1. **Scripts**
   - Create/open `Path/To/Script.cs`.
   - Paste/replace code as shown.

2. **GameObjects & Components**
   - In `<SceneName>`, select/create `<GameObjectName>`.
   - Add `<ComponentName>` component.
   - Create required children:
     - `ChildName` – purpose.

3. **Inspector Setup**
   - Assign:
     - `<Field>` ← `<OtherGameObject/ScriptableObject>`.
   - Set recommended defaults.

4. **Config / Assets**
   - Update `AppConfig` or child configs if needed.
   - Ensure any ScriptableObjects referenced exist and are assigned.

5. **Test Steps**
   - Open `<SceneName>`.
   - Play and:
     - Step 1…
     - Step 2…
   - Expected result: …
```

Do **not** redefine this checklist anywhere else. Other docs should just say “use the Unity Implementation Checklist”.

---

## 3. Editing Shared / Core Unity Scripts

Before changing behavior in scripts that many systems depend on (importers, core managers, geometry tools):

- Say:
  - Which scripts you’ll touch.
  - What will change.
  - Any risk (e.g., might break existing scenes).
- Ask before doing it, unless the user clearly requested that exact change.

You may skip this for:

- Tight, obvious bugfixes.
- Adding logs or comments.
- Adding small private helpers.

---

## 4. Config Structure (Canonical Names)

The **canonical configuration model** for this project is:

- `AppConfig` (ScriptableObject)
  - `VisualConfig` – colors, line thickness, grid opacity, theme.
  - `BehaviorConfig` – units, snapping, defaults, constraints.
  - `CatalogConfig` – catalogs/presets for placeable objects.

All other rules (UI, admin, workflows) refer to these names.

Whenever you need a global-ish setting:

- Prefer putting it in `VisualConfig`, `BehaviorConfig` or `CatalogConfig`.
- Do **not** invent separate “StyleConfig” or “SettingsConfig” types unless there’s a clear, new responsibility.

Mention any new config fields you add in your Change Summary.

---

## 5. Tightly-Coupled Unity Code

Two scripts are “tightly coupled” if:

- One directly manipulates the other’s internals, or
- They both depend on shared global state/singletons, or
- Changing one almost always requires changing the other.

If you introduce tight coupling:

- Call it out.
- Suggest a more modular alternative (events, interfaces, ScriptableObjects, delegates).
- If you keep the tight coupling (for speed or simplicity), say why.

---

## 6. Unity File Size & Structure

Same idea as global rules, with Unity context:

- Don't let MonoBehaviours turn into giant managers.
- Prefer:
  - Pure C# classes for logic.
  - MonoBehaviours for Unity glue and view wiring.
- If a script is doing too much (input, logic, rendering, config), say so and propose a split.

---

## 7. Unity Debug Visualization Patterns

When debugging **spatial, geometric, or orientation issues** in Unity, prefer **visual debugging** over print-only approaches.

### 7.1 Gizmo Visualization

Use `OnDrawGizmos()` or `OnDrawGizmosSelected()` to draw debug shapes in the Scene view:

```csharp
private void OnDrawGizmosSelected()
{
    // Draw bounds
    Gizmos.color = Color.cyan;
    Gizmos.DrawWireCube(bounds.center, bounds.size);
    
    // Draw direction arrows with color-coding
    // Blue = front (+Z), Red = back (-Z), Green = left, Yellow = right
    Gizmos.color = Color.blue;
    Gizmos.DrawLine(center, center + Vector3.forward * 0.3f);
    
    // Add labels (Editor only)
    #if UNITY_EDITOR
    UnityEditor.Handles.Label(position, "FRONT (+Z)");
    #endif
}
```

**Color conventions** for orientation:
- **Blue** = Front / Forward (+Z)
- **Red** = Back (-Z)
- **Green** = Left (-X)
- **Yellow** = Right (+X)
- **Cyan** = Bounds / Neutral info

### 7.2 Inspector Debug Fields

Expose calculated values as read-only serialized fields:

```csharp
[Header("Debug Info (Read-Only)")]
[SerializeField] private Vector3 _currentForward;
[SerializeField] private float _currentYRotation;
```

Update these in `OnDrawGizmosSelected()` or relevant methods so they stay current in the Inspector.

### 7.3 Debug Action Methods

Add methods that can be triggered from Inspector or context menu:

```csharp
[ContextMenu("Log Debug Info")]
public void LogDebugInfo()
{
    Debug.Log($"[ComponentName] Debug for '{gameObject.name}':\n" +
              $"  Position: {transform.position}\n" +
              $"  Rotation: {transform.eulerAngles}\n" +
              $"  Forward: {transform.forward}");
}
```

Also add buttons in custom Editors for frequently-used debug actions.

### 7.4 Toggle-able Debug Options

Let users enable/disable debug visuals without code changes:

```csharp
[Header("Debug Visualization")]
[Tooltip("Show orientation arrows in Scene view.")]
public bool showOrientationGizmos = true;

[Tooltip("Print debug info to console on actions.")]
public bool debugLogEnabled = true;
```

### 7.5 When to Add Visual Debugging

Add visual debugging when:

- Working with **positions, rotations, bounds, or snapping**
- Debugging **coordinate system mapping** (e.g., import/export)
- Issues are hard to diagnose from console logs alone
- The component will be reused and others may need to debug it

Keep debug code in place (behind toggles) rather than deleting it after fixing the immediate issue.
