# CCS Cleaner v2.0.0

CCS Cleaner is a Windows utility for locating large files and carefully reviewing removable temporary data.

## Features

- Scan a selected fixed or removable drive for large files, with a size threshold and name filter.
- Find supported temporary files, crash reports, known shader and Explorer caches, and empty folders in common user folders.
- Browse installed programs and games and launch their registered uninstallers.
- Review and manually select every result before deletion. Deletion asks for confirmation and rechecks each item's path and category.
- Skip protected Windows and application locations, critical system files, and directory reparse points such as symbolic links and junctions.
- Continue scanning when folders are inaccessible; skip locked, inaccessible, or changed items during deletion.
- Use the dark interface, themes, text-size setting, and configurable category colors.

## Download and runtime

The Windows x64 single-file build is self-contained and includes .NET 8. The target PC does not need the .NET 8 Desktop Runtime installed. The compressed executable is about 71.6 MB; bundled native runtime components may be extracted at first launch.

## Build from source

1. Install the .NET 8 SDK on Windows x64.
2. Run `Build-EXE.bat`.
3. Find `CCS-Cleaner.exe` in `Publish`.

The build script restores, builds, and publishes a compressed self-contained single-file executable. It clears the previous contents of `Publish` before publishing.

## Changes in v2.0.0

- Renamed the application from Cire Cleaner to CCS Cleaner and updated the executable name.
- Improved scan safety and handling of inaccessible paths and reparse points.
- Expanded cleanup discovery for user temporary files, crash reports, supported caches, and empty folders.
- Revalidated paths, categories, and empty-folder state immediately before deletion; locked or changed objects are skipped.
- Fixed dark table scrolling, hover text contrast, theme-aware program rows, and visibility of the Settings button.
- Added a compressed standalone Windows x64 build that runs without a separately installed .NET Desktop Runtime.

## Safety

Nothing is deleted automatically. Review each selected path and the confirmation summary. The program does not delete program directories as a substitute for uninstalling; use the registered uninstaller shown in the Programs view. If the safety category cannot be confirmed, the object is skipped.

## Author

[cireincroinsea](https://t.me/cireincroinsea)
