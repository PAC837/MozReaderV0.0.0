# CRITICAL RULE: Never Overwrite Working Files

## THE MISTAKE THAT WAS MADE
On December 7, 2025, I made a critical error:
- I was asked to DELETE `MozImporter.cs` (an old deprecated file)
- Instead, I OVERWROTE the working `MozImporterBounds.cs` with simplified code
- This destroyed important functionality (red corner spheres, bounds highlighting, etc.)

## THE RULE

### When Asked to "Delete" or "Remove" a File:
1. **ONLY delete that specific file** - do not touch other files
2. Use `execute_command` with `del` command - do NOT use `write_to_file` on other files

### When Asked to "Consolidate" Code:
1. **NEVER use write_to_file to completely rewrite a working file**
2. Instead use `replace_in_file` to add specific functionality
3. ASK the user first: "Should I add this to existing file X or create new file Y?"

### Before Modifying ANY File:
1. **READ the file first** to understand what's there
2. **IDENTIFY what specific lines need to change**
3. **USE replace_in_file** with targeted SEARCH/REPLACE blocks
4. **NEVER rewrite entire files** unless creating new from scratch

### If Classes Are "Shared" Between Files:
1. **DO NOT assume consolidation is needed**
2. The "old" file may be old for a reason - the "new" file was intentionally different
3. ASK before merging: "File X and Y both have class Z. Should I merge them?"

## HOW TO ADD FEATURES TO EXISTING FILES

### WRONG (Destroys existing code):
```
<write_to_file>
<path>SomeWorkingFile.cs</path>
<content>...my simplified version...
