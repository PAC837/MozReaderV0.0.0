# Unity Feature / Refactor Workflow

Use this workflow when implementing or refactoring a **significant Unity feature** in this project.

---

## 0. Scope & Mode

1. Start in **Plan mode**.
2. Restate the task in your own words.
3. Ask any crucial clarifying questions needed to avoid wrong assumptions.

If the request is ambiguous, clarify the **desired outcome** and **constraints** first.

---

## 1. Understand Current State

1. Identify relevant files, scenes, and systems:
   - Use `search_files`, `read_file`, etc. to find related scripts and READMEs.
   - Optionally consult `docs/APP_DIARY.md`, and if you do:
     - Tell Phill you are referencing it.
     - Surface any assumptions from it and ask for confirmation.
2. Summarize the existing architecture relevant to the task:
   - Key scripts and their responsibilities.
   - How they currently interact (including any tight coupling).
3. Confirm back:
   - What you think the current behavior is.
   - What we are trying to add/change.

---

## 2. Plan the Changes

1. Propose a **high-level implementation plan** in bullet points, including:
   - New scripts/classes (names & responsibilities).
   - Existing scripts to modify.
   - Data structures, events, interfaces, or ScriptableObjects you plan to use.
2. Call out:
   - Any potential tight coupling and possible alternatives.
   - Any scripts that may cross the ~300-line soft limit.
3. Ask for approval:

> “Do you want me to proceed with this plan in Act mode?”

Do **not** modify files until the plan is approved.

---

## 3. Implement (Act Mode)

After plan approval, switch to **Act mode** and implement in small, safe steps.

For each step:

1. **Code changes**
   - Modify only the files relevant to that step.
   - Keep scripts small and focused; avoid unnecessary tight coupling.
2. **Documentation**
   - Update or create the relevant `README.md` (Overview, Key Scripts, Unity Setup, Notes).
   - Append a new entry to `docs/APP_DIARY.md` describing:
     - What changed.
     - Which files were touched.
     - Current understanding.
     - Open questions / assumptions.
3. **Script size**
   - If a script approaches or exceeds ~300 lines:
     - Pause and propose a refactor plan.
     - Ask for approval before splitting responsibilities.

After each meaningful chunk of work, summarize progress and check in before moving on.

---

## 4. Unity Integration Checklist

Provide a **Unity Implementation Checklist** (as required by the rules):

1. Scripts to create/edit and their paths.
2. GameObjects to create/modify and components to add.
3. Inspector assignments and recommended default values.
4. Any tags, layers, physics, or Input settings to configure.
5. Concrete test steps in Play mode.

It must be explicit enough to follow without guessing.

---

## 5. Validation & Testing

1. Suggest specific tests:
   - Which scene to open.
   - What actions to perform.
   - What behavior/logs to expect.
2. If helpful, add temporary logging or gizmos for verification.
3. When implementation is complete, state clearly:

> “At this point, the feature/refactor should be working as described, pending your manual testing.”

---

## 6. Git / Checkpoint Recommendation

At a stable point:

1. Recommend a Git checkpoint, for example:

   Suggested Git steps:

   1. Save all in Unity / VS.
   2. Run relevant play-mode or edit-mode tests (if they exist).
   3. In terminal:
      - `git status`
      - `git add <list of changed files>`
      - `git commit -m "Short, clear description of the change"`

2. If no branch is being used yet, optionally suggest a branch name.

---

## 7. Final Change Summary

End with a **Final Change Summary**:

- Files created.
- Files modified.
- New/renamed public APIs (methods, fields, properties).
- Any known limitations or follow-up tasks.

This summary should be concise but detailed enough for future you to understand what was done.
