# Changelog

## BETA 4/12/2025 - #2


### I didn't have time to update the wiki/Readme etc... Will do once I'm back home

### Changed
- Changed some Logic in the C# Plugin

### Added
- Added a Settings window in the plugin to select the shared memory file and read from it.

## BETA 4/12/2025

### Notes

The next big step of F1 Manager Logger is here!

I've successfully found a way to skip all of the Python scripts previously there, and go directly from Cheat Engine to the C# SimHub Plugin!
You can expected huge updates to come soon, so stay tuned!

### Changed

- Changed the `code.lua` and `LogginTable.CT` codes.
- Changed the C# Plugin to read directly from the Shared Memory File (MMF) to skip Python.

### Removed

- All Python files

## BETA - 4/10/2025

### Changed

- Changed `README.md` slightly
- Updated the Wiki
- Updated [`CHANGELOG.md`](CHANGELOG.md) for better readability
- Changed delay before trying to hide Cheat Engine's Window
- Greatly reduced queue sizes to reduce CPU overhead
- Added delays in multiple functions to reduce CPU usage

### Added

- Added `sessionType` and `sessionTypeShort` to the available data and wiki.

### Removed

- Removed useless print function in `telemetry_plotter.py`

## BETA - 4/9/2025

### Changed

- Changed [`README.md`](README.md) slightly
- Changed the `settings.ini` slightly
- Created Wiki

### Added

- Added all current data points for all cars (22 of them)

### Removed

- Hid Built files to cut on Repo space