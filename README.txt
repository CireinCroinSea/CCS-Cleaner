Cire Cleaner v2 (.NET SDK)

1. Extract the ZIP.
2. Run Build-EXE.bat.
3. The app is published to Publish\Cire-Cleaner.exe.

Changes:
- Excludes hiberfil.sys, pagefile.sys, swapfile.sys, memory dumps, boot files and protected Windows/app directories from large-file search.
- Double-click a file result to open its containing folder and select the file.
- Click the author title to open Telegram; separate Telegram button/link removed.
- Programs/games view: green = likely system/Microsoft component, yellow = likely game, red = other/unclassified app.
- Uninstall action runs the registered uninstaller; it does not blindly delete program folders. Some classification is heuristic, so review names/publishers carefully.
- Cleanup scans known temp/cache categories and empty folders in common user folders.

Build target: Windows x64, .NET 8 Windows Forms, self-contained single-file.
First publish may require internet to restore missing .NET targeting packs.

Layout update: app title is plain text (not underlined); only the author nickname is a Telegram link. Disk, size and name filters are on the row below the title; all action buttons are on a separate row below filters.

Version 3 layout/settings: black results/header background, author nickname only is clickable (title has no underline), button text says "Программы и игры", Settings menu includes font size, 9 themes, and configurable colors for system/game/unknown app categories. Settings persist in %LOCALAPPDATA%\CireCleaner\settings.txt.
\nVersion 4: Programs button caption shortened to "Программы"; added dark Windows theme hint for native ListView scrollbar/header. Settings button remains available.\n
Version 5 toolbar fix: action toolbar now wraps onto a second line when needed; button is labelled "Программы" and "Настройки" is explicitly included and visible.

Version 6: explicitly adds the Settings button immediately after Programs and uses a single non-wrapping toolbar row wide enough for all buttons; no hidden second row.

Version 7: toolbar controls rebuilt explicitly in order; Settings is directly after Programs and all buttons are in one visible row. Also restores short labels for Select/Clear/Delete.

Version 8: requests Windows DWM immersive dark mode for the native title bar (window caption and minimize/maximize/close area) on supported Windows 10/11 builds. Actual result depends on Windows version/theme.


Version 9 safety/search improvements:
- Broad large-file scan skips Windows, Program Files, ProgramData, AppData and reparse points/junctions.
- Cleanup scan checks user TEMP, crash dumps, WER reports, DirectX/NVIDIA/AMD shader caches, Explorer icon/thumbnail cache and empty folders in common user folders.
- Removed Windows Temp from selectable cleanup candidates because it is under the protected Windows tree.
- Deletion revalidates each candidate; known cache files are only deletable when still inside the allow-listed cache folders; empty directories are removed only if still empty and not protected.
- Some files may be locked or inaccessible; failures are skipped. This is a conservative scanner, not a guarantee that every junk file is found.
