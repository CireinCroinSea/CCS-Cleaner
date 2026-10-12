# Changelog

## v2.0.0 — CCS Cleaner

### Safety and scanning
- Continue scanning when directories or files are inaccessible.
- Skip symbolic links, junctions, and other directory reparse points.
- Exclude protected Windows/application locations and critical system files from removal candidates.
- Improve discovery of large files, user temporary files, crash reports, supported caches, and empty folders.
- Recheck each selected path and safety category before deletion; recheck that folders are empty.
- Skip locked, inaccessible, or changed objects and report the result.

### Interface
- Rename the app from Cire Cleaner to CCS Cleaner.
- Keep the dark table and scrollbar appearance while fixing hover text contrast.
- Make program and game rows follow the active theme and configured category colors.
- Keep the Settings button visible and working.
- Add drag selection that checks multiple rows together.
- Add reversible alphabetical name sorting and numeric size sorting, with a divider below the column headings.

### Distribution
- Build a compressed, self-contained Windows x64 single-file executable.
- Include .NET 8 so the target computer does not need the .NET 8 Desktop Runtime.
- The release build verified with .NET SDK 8.0.425: restore succeeded; build completed with 0 warnings and 0 errors.
