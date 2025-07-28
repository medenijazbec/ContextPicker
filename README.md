# ContextPicker -> User master branch for up to date code, im too lazy to prune stuff from main atm

**ContextPicker** is a Windows Forms utility for interactively selecting, naming, and exporting context dumps of files from up to 5 folders at once.
It is especially useful for preparing data for AI models, code review, or other batch analysis.
ContextPicker supports flexible selection, context grouping, CSV import/export, and more.

---

## Features

* **Multi-Folder Selection:**
  Add up to 5 folders (contexts) at a time. Each folder can be browsed, named, and individually configured.

* **Interactive TreeView:**
  Select files and folders visually using a checkable directory tree for each context. Checking a folder checks/unchecks all its children recursively, regardless of expansion state.

* **Context Naming:**
  Each context (folder) can be given a custom name for use in exported files.

* **Live Monitoring:**
  New folders added to any context are detected live using a FileSystemWatcher and shown immediately in the UI.

* **Export Functions:**

  * **Generate Context:** Creates a single `.txt` dump per context, concatenating selected files and noting their source.
  * **Export Selection:** Outputs a CSV of all selected files/folders, grouped by context, root folder, and type.

* **Import Selection:**
  Import a CSV (previously exported) to quickly restore complex multi-folder selections and context names.

---

## Quirks & Details

* **Context Limit:**
  Maximum of 5 folder contexts can be added at a time (enforced by the UI).

* **Folder Checking:**
  Checking a folder checks all its subfolders/files recursively. Unchecking will also propagate.

* **Hidden Files/Folders:**
  Hidden files and directories are skipped and not shown in the tree.

* **UI Layout:**
  Each folder context is visually separated and has its own "Browse", "Remove", "Name", and file tree controls.

* **CSV Import:**

  * CSV import expects a format with columns: ExportPath, ContextName, Path, Type.
  * On import, root folders are determined per-context by shortest path found in the CSV.
  * Already loaded contexts (matching root folder) are skipped on import.
  * Nonexistent root folders are skipped with a warning.

* **FileSystemWatcher:**
  The app live-monitors folder trees for newly created directories and updates the tree, restoring checked state when possible.

* **Progress Bar:**
  The "Generate Context" export shows per-context progress. If no files are checked in a context, it is skipped.

* **Filename Sanitization:**
  Exported filenames are sanitized of invalid Windows characters.

* **Exported File Names:**
  Each exported context dump uses the pattern:
  `<ContextName>_ContextDump_<timestamp>.txt`

---

## Usage

1. **Add a Folder:**
   Click **Add Another Folder** (up to 5 times). Browse or type folder paths.

2. **Name Each Context (Optional):**
   Enter a name for each context/folder in the `Name` box; defaults to the folder's name.

3. **Select Files/Folders:**
   Use the tree view to check which files/folders to include in the export.
   Checking a folder checks all sub-items.

4. **Set Export Folder:**
   Click **Change Export Folder** and pick where exports should be written.

5. **Generate Context Dump:**
   Click **Generate Context** to export selected files from each context to a single, concatenated text file per context.

6. **Export/Import Selection as CSV:**

   * Click **Export Selection** to export your checked files/folders as a CSV.
   * Use **Import Section** to re-load previous selections from a CSV export.

---

## Folders & Files

* **.env** files are always ignored from the repository and export process.
* **obj/Debug/** and **bin/** are ignored by default; only the rest of the `obj/` folder is tracked.
* Hidden files/directories are not visible for selection or export.
* Large folders may take some time to expand or check.

---

## Known Issues / Edge Cases

* **Large directories:**
  Expanding or checking large folders may take several seconds as files are enumerated.
* **Live updates:**
  The directory tree refreshes when folders are created, but rapid filesystem changes may cause flicker or slow updates.
* **Nonexistent Folders on Import:**
  If a root folder referenced by a CSV import does not exist, it is skipped with a warning.
* **Max Contexts:**
  You cannot add more than 5 contexts at once. Remove a context to add another.
* **UI Scaling:**
  Minimum size is enforced; resizing smaller than that may hide controls.

---

## Building & Running

* **Requirements:**

  * .NET Framework (WinForms)
  * Visual Studio (recommended)

* **Steps:**

  1. Open the solution in Visual Studio.
  2. Build and run (`F5`).

---
