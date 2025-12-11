# unity-feature-workflow.md

Use this workflow **only for significant Unity changes** in this project:

- New tools/modes.
- Geometry / import / export behavior.
- Multi-script or cross-system refactors.

For small fixes, just follow `cline-global-rules.md` and `unity-cline-rules.md` without this file.

---

## 0. Scope

1. Restate the request.
2. Decide if it’s **significant**:
   - If not, say “This looks small; I’ll treat it as a small task” and proceed normally.
3. If significant, follow the steps below.

---

## 1. Understand Current State

1. Find relevant:
   - Scripts, ScriptableObjects, scenes, prefabs.
   - Any nearby README or docs.
2. Optionally read `docs/APP_DIARY.md`:
   - Mention if you rely on it.
   - Note any assumptions that affect your design.

Summarize briefly:

- What the system does now.
- Which pieces you’ll likely touch.

---

## 2. Plan

Propose a short plan:

- Files to create.
- Files to modify.
- Key data structures/events/interfaces.
- Any known risks.

Example:

- Create `WallSnapController` for snap logic.
- Modify `WallToolController` to delegate to `WallSnapController`.
- Add `snapGridSize` to `BehaviorConfig`.

End with:

> “Plan summary above. Proceed?”

Wait for approval before editing code.

---

## 3. Implement

Work in **small steps**. For each chunk:

1. Make focused changes (a few files).
2. Keep responsibilities tight; avoid god objects.
3. If behavior meaningfully changes:
   - Append an entry to `docs/APP_DIARY.md` (see `unity-cline-rules.md` §1).
4. If you see a big refactor opportunity, propose it before doing it.

After each chunk, give a short progress note (1–3 bullets). No need for a giant essay.

---

## 4. Unity Integration & Testing

When code for the feature is in place:

1. Provide a filled out **Unity Implementation Checklist** (from `unity-cline-rules.md` §2) for this feature.
2. Suggest test steps:
   - Which scene to open.
   - What actions to take.
   - What results/logs to expect.

If you add temporary logs/gizmos to verify behavior, say when it’s safe to remove them.

---

## 5. Git / Checkpoint

Once the feature is stable and tested:

- Suggest:
  - Commit checkpoint.
  - Optional branch name (e.g., `feature/wall-snap-grid`).

Keep this short unless asked otherwise.

---

## 6. Final Change Summary

Finish with a **Final Change Summary** (same structure as the global Change Summary), with emphasis on:

- Files created/modified.
- Public API changes.
- New/changed config fields in `AppConfig` / `VisualConfig` / `BehaviorConfig` / `CatalogConfig`.
- Known limitations or follow-ups.

This is the single place to look to understand what changed for this feature.
