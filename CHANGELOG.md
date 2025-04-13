# Changelog

## BETA 0.1

### Notes

We're getting ever closer to a full release of the Plugin!

### Changed

- Improved C# Plugin. ([`ef63d34`](https://github.com/Asviix/F1Manager2024Logger/commit/ef63d34aef19a457a653ec2f0b11132abb495dd3))
- Changed some functions in the C# plugin. ([`3d58fbb`](https://github.com/Asviix/F1Manager2024Logger/commit/3d58fbb2b4731981d852167e87a3b0cef7fb782d))
- Hid some files for improved project Structure. ([`d5f6f25`](https://github.com/Asviix/F1Manager2024Logger/commit/d5f6f253dfbfbc12e73ee81cfb32cc52e861de8c))
- Updated Wiki.

### Added

- Added all properties of the session and driver data. ([`26dfede`](https://github.com/Asviix/F1Manager2024Logger/commit/26dfede5834f50b232bbafe1480c76f3b3cffa23))
- Added Issue template. ([`c903af9`](https://github.com/Asviix/F1Manager2024Logger/commit/c903af917a85dbc0db6019106ed745f27054f399))

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