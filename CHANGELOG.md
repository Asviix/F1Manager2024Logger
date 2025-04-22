## BETA 0.4 - Whatcha Looking at' ?

### Note

The new Beta 0.4 is now available, and with comes a brand new feature, called "CameraFocus"!
This will allow users to see the telemetry of the car they are currently looking at, perfect for building the perfect dashboard!

This update also scraps Cheat Engine Entirely, relying only on a Custom-Written C# Plugin!

I've also added a lot of data points relating to the drivers, their names and team name.

You can refer to the list below or the updated Wiki to know what's been added.

### Changed
- Changed Reading Method for Cheat Engine to standalone Console app. ([`7f09a3b`](https://github.com/Asviix/F1Manager2024Logger/commit/7f09a3bc94112b1ffc027fa390ffad8388df4056))
- Fixed Issue with CSV Reader, appending the first data two times. ([`af845a8`](https://github.com/Asviix/F1Manager2024Logger/commit/af845a8b940629b1fbb2dfc2e91da702c3e60ed0))
- Added Back the Icon into the Repo, in case users want to build it themselves. ([`ecf823d`](https://github.com/Asviix/F1Manager2024Logger/commit/ecf823d8acfd1759b178f38ecd5332cbf1fd009c))
- Changed Exporter Display settings to show actual Driver and Team Name. ([`66c51df`](https://github.com/Asviix/F1Manager2024Logger/commit/66c51df53e2d9373812f45dbdbb039ab1a289e80))
- Changed the Settings page. ([`2069b54`](https://github.com/Asviix/F1Manager2024Logger/commit/2069b54555aad33fe71ecb5df4d5eeb141041618))

### Added
- Added Computed Time Speed Property. ([`d54c8ea`](https://github.com/Asviix/F1Manager2024Logger/commit/d54c8ea0903dd86935f36baa197f98722125b82d))
- Added Driver First Name. ([`66c51df`](https://github.com/Asviix/F1Manager2024Logger/commit/66c51df53e2d9373812f45dbdbb039ab1a289e80))
- Added Driver Last Name. ([`66c51df`](https://github.com/Asviix/F1Manager2024Logger/commit/66c51df53e2d9373812f45dbdbb039ab1a289e80))
- Added Driver Team Name. (Can Input the name of your custom team when needed.) ([`66c51df`](https://github.com/Asviix/F1Manager2024Logger/commit/66c51df53e2d9373812f45dbdbb039ab1a289e80))
- Added Property to know whichever car the camera is currently focused on. ([`66c51df`](https://github.com/Asviix/F1Manager2024Logger/commit/66c51df53e2d9373812f45dbdbb039ab1a289e80))

# Changelog

## BETA 0.3.1

### Changed

- Changed GitHub Repo's Organization for easier reading. ([`b4f6ba`](https://github.com/Asviix/F1Manager2024Logger/commit/b4f6ba52eb9f243d603b775cc28d6f9288f293c8))
- Fixed Fatal Error when opening settings. ([`964f99`](https://github.com/Asviix/F1Manager2024Logger/commit/964f9932013e632fa1df52f9417b7eba5859cd37))

### Added
- Added a link to the discord and the wiki to the settings page. ([`b4f6ba`](https://github.com/Asviix/F1Manager2024Logger/commit/b4f6ba52eb9f243d603b775cc28d6f9288f293c8))

## BETA 0.3

### Changed

- Adjust driver position for 0-based index in telemetry data. ([`f5830cd`](https://github.com/Asviix/F1Manager2024Logger/commit/f5830cd82b083194f1468c9d36c9e6d20a98d5e9))
- Changed naming style to follow C# Naming conventions. ([`c132d47`](https://github.com/Asviix/F1Manager2024Logger/commit/c132d47a9bd4678be4dc660ea4ae5718e62c2686))
- Added read only modifiers to follow C# conventions. ([`c132d47`](https://github.com/Asviix/F1Manager2024Logger/commit/c132d47a9bd4678be4dc660ea4ae5718e62c2686))
- Refactored and removed some code for better optimization. ([`c132d47`](https://github.com/Asviix/F1Manager2024Logger/commit/c132d47a9bd4678be4dc660ea4ae5718e62c2686))
- Fixed Updating the MMF File Path not start reading the data. ([`0585697`](https://github.com/Asviix/F1Manager2024Logger/commit/0585697306b974c5066716d275dd91bbf5f6c0b5))
- Fixed Lap Number not being accurate to the current lap the driver is in. ([`9d4e1e3`](https://github.com/Asviix/F1Manager2024Logger/commit/9d4e1e323e6062165429fed947f84ffbd237b309))
- Changed Session Data Name so that it's more in-line with other data points. ([`73b16f7`](https://github.com/Asviix/F1Manager2024Logger/commit/73b16f7962d7b30e7003215c5e0e9d875951b059))
- Changed All Data Name for easier reading. ([`2f671a0`](https://github.com/Asviix/F1Manager2024Logger/commit/2f671a04514c6625c11f2f31d2fcad8e9ccb57c5))
- Changed Historical Data Name for easier reading. ([`2f671a0`](https://github.com/Asviix/F1Manager2024Logger/commit/2f671a04514c6625c11f2f31d2fcad8e9ccb57c5))
- Changed UI for better readability. ([`1fa7858`](https://github.com/Asviix/F1Manager2024Logger/commit/1fa78583226115a4e1dc9c6c39e356a193a5d97b))
- Updated how the MMF Reading Startup is handled. ([`0585697`](https://github.com/Asviix/F1Manager2024Logger/commit/0585697306b974c5066716d275dd91bbf5f6c0b5))
- Changed UI to add warnings. ([`1fa7858`](https://github.com/Asviix/F1Manager2024Logger/commit/1fa78583226115a4e1dc9c6c39e356a193a5d97b))
- Fixed Plugin Name and Plugin Author Being Switched around. ([`4338b7`](https://github.com/Asviix/F1Manager2024Logger/commit/4338b7fcd2b1c4bef96db89f9fc443fa748eacd6))
- Reworked UI for cleaner look. ([`d44286`](https://github.com/Asviix/F1Manager2024Logger/commit/d442863f2b2ed3b4a2ac52277399b6d1d7b1c761))
- Reworked Driver Selection for CSV Exporter Method. ([`6c6fac`](https://github.com/Asviix/F1Manager2024Logger/commit/6c6fac7be35d64875f1162c1399d2770888c2ba6))

### Added

- Added Historical data ([`b02f39a`](https://github.com/Asviix/F1Manager2024Logger/commit/b02f39a89c07f59e9b3bb1a47106650dfb831546))
- Added the `ROADMAP.md`. ([`3f65fd0`](https://github.com/Asviix/F1Manager2024Logger/commit/3f65fd09f9e3ec96d89e64d424480b11640bb5e7)) & ([`8527f07`](https://github.com/Asviix/F1Manager2024Logger/commit/8527f07bbda45a34538d1eb117fe09cc5e567f54))
- Added Discord Server link to the README.md. ([`7a64b26`](https://github.com/Asviix/F1Manager2024Logger/commit/7a64b26d29f25a4858fcbcdd7708310b6c753ed0))
- Added Button to reset all Historical Data. ([`79dfbd9`](https://github.com/Asviix/F1Manager2024Logger/commit/79dfbd9a72b0c3d2dd02cb14e7ab044181bdfa75))
- Added File Name Validation when setting the MMF File Path. ([`0585697`](https://github.com/Asviix/F1Manager2024Logger/commit/0585697306b974c5066716d275dd91bbf5f6c0b5))
- Added a "Reset to Defaults" for the settings. ([`1fa7858`](https://github.com/Asviix/F1Manager2024Logger/commit/1fa78583226115a4e1dc9c6c39e356a193a5d97b))

### Wiki

- Updated Wiki to better guide new users
- Added Historical Data.
- Reworked a bunch of pages.

## BETA 0.2

### Added

- Added the Exporter. ([`0bdb5b6`](https://github.com/Asviix/F1Manager2024Logger/commit/0bdb5b6324205278f40041c1ccd8d3a2e0d319e8))

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