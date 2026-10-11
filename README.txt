CCS Cleaner v2.0.0

CCS Cleaner is a Windows utility for finding large files and reviewing removable temporary data.

Features:
- Scan a selected disk for large files.
- Find supported temp files, crash reports, known caches, and empty folders.
- View installed programs and games; use their registered uninstallers.
- Manually select results; deletion always requires confirmation.
- Recheck each path/category before deletion and recheck empty folders.
- Skip protected Windows/application locations, critical files, and directory links/junctions.
- Skip inaccessible, locked, or changed objects safely.
- Dark theme, configurable themes, text size, and category colors.

Standalone build:
The Windows x64 single-file EXE includes .NET 8 and does not require the .NET 8 Desktop Runtime to be installed. It is approximately 71.6 MB. First launch may extract bundled native runtime components.

Build:
1. Install the .NET 8 SDK for Windows x64.
2. Run Build-EXE.bat.
3. Find CCS-Cleaner.exe in Publish.

Changes in v2.0.0:
- Renamed the app to CCS Cleaner and the executable to CCS-Cleaner.exe.
- Improved scan safety, inaccessible-path handling, and skipping of symbolic links/junctions.
- Expanded cleanup discovery for temp files, error reports, supported caches, and empty folders.
- Revalidated each deletion candidate and skip files that are locked, inaccessible, or changed.
- Fixed dark scrollbars, readable hover text, theme-aware program rows, and the visible Settings button.
- Published settings now produce a compressed, self-contained single-file build.

Author: cireincroinsea
Telegram: https://t.me/cireincroinsea
