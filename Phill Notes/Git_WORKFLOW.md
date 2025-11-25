Here’s a little doc you can drop directly into your Unity project folder as e.g. GIT_WORKFLOW.txt or GIT_WORKFLOW.md.

# SingleDraw – Git Workflow (Cheat Sheet)

## 0. Important Rules

- **Always work on a branch, not directly on `main`.**
- **Never run `git add .`**  
  Only add the folders we actually care about:
  - `Assets/`
  - `ProjectSettings/`
  - `Packages/`
  - `.gitignore` (occasionally)

---

## 1. Start a New Work Session

In the project folder:

```bash
cd "C:\Users\info\SingleDraw 0.0.0\SingleDraw V1.0.0"


Make sure you’re on main and up to date:

git checkout main
git pull


Create a new branch for what you’re about to do:

git checkout -b feature/<short-name>
# examples:
# git checkout -b feature/anchor-placement
# git checkout -b feature/jump-tuning


You are now on your own branch (safe to mess around).

2. Work in Unity / VS Code

Make changes in Unity and VS Code as usual.

Save everything.

3. See What Changed

Check status:

git status


Look at the list. You should mostly see files under:

Assets/...

ProjectSettings/...

Packages/...

If you see Library/, Temp/, Logs/, etc., they should be ignored by .gitignore.
If they’re not, fix .gitignore later – don’t commit those.

4. Stage Only the Important Stuff
Simple “include everything important” commit
git add Assets ProjectSettings Packages .gitignore

OR: Smaller, more focused commit

Example: only scripts and scene:

git add Assets/Scripts Assets/Scenes


Then check:

git status


Everything under “Changes to be committed” is what will go into this snapshot.

5. Commit (Save a Snapshot Locally)
git commit -m "Short description of what you just did"
# examples:
# git commit -m "Add basic anchor placement on wall"
# git commit -m "Limit player to one jump"


This does NOT talk to GitHub yet.
This just saves a checkpoint on your machine.

6. Push Your Branch to GitHub

First time pushing this branch:

git push -u origin feature/<short-name>


After that, on later pushes from the same branch:

git push


Now GitHub has your new commits, but main is still clean.

7. Merging Back to main (when ready)
Option A – Use GitHub (recommended)

Go to the repo on GitHub.

It will suggest a Compare & pull request for feature/<short-name>.

Create a Pull Request.

Review changes, then Merge into main.

Option B – Local merge (solo workflow)

Make sure everything is pushed from your branch first:

git checkout feature/<short-name>
git push


Then:

git checkout main
git pull        # get latest main
git merge feature/<short-name>
git push        # update main on GitHub

8. Daily Pattern (TL;DR)

git checkout main

git pull

git checkout -b feature/<short-name>

Work in Unity

git status

git add Assets ProjectSettings Packages

git commit -m "message"

git push -u origin feature/<short-name> (first time)
then just git push on later updates