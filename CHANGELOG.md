# Changelog

## Update 1.1

### Changed
- Moved away from the Memory.DLL Library. ([`d705fb5`](https://github.com/Asviix/F1Manager2024Logger/commit/d705fb5927843d68a56154d8a628bd7d993e3d0e))
- Changed the menu when the Telemetry Reader is running. ([`12b1132`](https://github.com/Asviix/F1Manager2024Logger/commit/12b113227bb8ff80752d790ef66376795547f613))
- Point Schemes will now be read from the savefile, no need for user input. ([`6709134`](https://github.com/Asviix/F1Manager2024Logger/commit/67091343e853e39d1f4abb4db11e64415df39074))
- Fastest Lap Point addition will now be read from the savefile. ([`1237c32`](https://github.com/Asviix/F1Manager2024Logger/commit/1237c32a4683d2a57788971a06a7cbfd42a719b6))
- Point for Pole Position will now be read from the savefile. ([`f9ee375`](https://github.com/Asviix/F1Manager2024Logger/commit/f9ee375e17900d7c3dc3a49047fe99277ccd636d))
- Retrieved team names from the save file instead of a hard-coded table. ([`ba31dd5`](https://github.com/Asviix/F1Manager2024Logger/commit/ba31dd551a248449f231d2f4b7583a51e3fe2774))
- Retrieved team colors from the save file instead of a user-defined color. ([``]())

### Added
- Added Points Doubling for the last Race if enabled. ([`204700c`](https://github.com/Asviix/F1Manager2024Logger/commit/204700c214d9bce2aadf275cc9c16ec3f38ba14d))
- Added Driver Code. ([`e1f63c1`](https://github.com/Asviix/F1Manager2024Logger/commit/e1f63c18a1426d49546bf46a86aeb37d7ae4d6d8))

## RELEASE 1.0

### Changed
- Fixed Standings Data not updating properly depending grid size. ([`f402d41`](https://github.com/Asviix/F1Manager2024Logger/commit/f402d4198c9e84dbc8bb047d8a4d0f91070abf14))
- Added Version in the settings footer. ([`8cd8fdb`](https://github.com/Asviix/F1Manager2024Logger/commit/8cd8fdb480f0682722e6bd8df614997662674f16))
- Changed how Last Sector Times work to prevent them from resetting to 0. ([`e6339e5`](https://github.com/Asviix/F1Manager2024Logger/commit/e6339e5ecdda9229053540f2dd854e011434bb88))
- Change the Memory Reader to always overwrite the Plugin DLL. ([`#49`](https://github.com/Asviix/F1Manager2024Logger/issues/49))
- Refactored some code to limit cross-file interactions. ([`9aabfd7`](https://github.com/Asviix/F1Manager2024Logger/commit/9aabfd781c90984d16e0cf2a9e49ba41a50a9012))
- Refactor telemetry handling and improve data accuracy ([`cf5ca34`](https://github.com/Asviix/F1Manager2024Logger/commit/cf5ca34abe2dd65449788651807402b77f7a4358))
- Fixed a bug which would add a last turn recording of the data at the start of a new lap. ([`dc04456`](https://github.com/Asviix/F1Manager2024Logger/commit/dc04456ec25095e0820dd434f09a73fdda2a5b8d))
- Fixed a bug where Driver's Best Sector Times wouldn't record. ([`9c75dd3`](https://github.com/Asviix/F1Manager2024Logger/commit/9c75dd35cba9bbad7031784e1b666c5ee75aba24))
- Shortened Pit Stop Statuses for Ease of use. ([`ccf9c5f`](https://github.com/Asviix/F1Manager2024Logger/commit/ccf9c5fc632d8dca0a41766ce330dd59055052e4)) && ([`ac82bf3`](https://github.com/Asviix/F1Manager2024Logger/commit/ac82bf3694eac6a43c20efb442158ab0c047222e))
- Fixed Grid size Logic. ([`1e2b41b`](https://github.com/Asviix/F1Manager2024Logger/commit/1e2b41b8fa1b2dd1bab375f96186b2f1fad5c5e2))
- Fixed Points Gains. ([`41c584f`](https://github.com/Asviix/F1Manager2024Logger/commit/41c584f6a4aa8918b198108436b1c9bbb5285031))
- Fixed Best Session Time not returning the correct data. ([`e490bb7`](https://github.com/Asviix/F1Manager2024Logger/commit/e490bb7468acb3ccc2fef9bb19e7ae105171ba01))
- Change CSV Formatting and naming scheme. ([`df14dd8`](https://github.com/Asviix/F1Manager2024Logger/commit/df14dd811eb98e159f5c38e4478cb188f243eddc))
- Updated the way a new version is checked. ([`90e43a9`](https://github.com/Asviix/F1Manager2024Logger/commit/90e43a9132056b777219754e335570f1ccaece8e))
- Changed the Menus of the memory reader. ([`49f2802`](https://github.com/Asviix/49f280244b945f064044217f0ecdd8a7bcdc583c))

### Added
- Added Best Sector Times. ([`e6339e5`](https://github.com/Asviix/F1Manager2024Logger/commit/e6339e5ecdda9229053540f2dd854e011434bb88))
- Added Dashboard Tracked Drivers. ([`8d526ad`](https://github.com/Asviix/F1Manager2024Logger/commit/8d526ad98c6426c6b20e1b16efddd32be7f933f4)) & ([`9644f93`](https://github.com/Asviix/F1Manager2024Logger/commit/9644f9341fddfb4c7eba41731dde7a2ab991a584))
- Added Speed Trap Data ([`9aabfd7`](https://github.com/Asviix/F1Manager2024Logger/commit/9aabfd781c90984d16e0cf2a9e49ba41a50a9012))
- Added Estimated Position after Pit-Stop. ([`f3dafd1`](https://github.com/Asviix/F1Manager2024Logger/commit/f3dafd1f460a4373b846e4effddbbb79b5beeb13))
- Added Depth of the water on track. ([`722e3ce`](https://github.com/Asviix/F1Manager2024Logger/commit/722e3cea2a37a541a0630c506968cf206aab6256))
- Added Points Gain per driver. ([`1354d14`](https://github.com/Asviix/F1Manager2024Logger/commit/1354d1443c2581666d214b26eb089348f4ec5096))
- Added Best Sector Times of the Session. ([`7d0453d`](https://github.com/Asviix/F1Manager2024Logger/commit/7d0453d3cf9e2691956456f6cc8d00fb747d56dd))
- Added Custom Team Colors. ([`80b8fe9`](https://github.com/Asviix/F1Manager2024Logger/commit/80b8fe9dc5bab27eea0e6f180378656981e347ba))
- Added Custom Tire Enum Mapping. ([`17519ad`](https://github.com/Asviix/F1Manager2024Logger/commit/17519adeb2fbabad8a01ade0427c1f24fc91b557))
- Added ERS Battle Assist Mode. ([`c26ac7a`](https://github.com/Asviix/F1Manager2024Logger/commit/c26ac7aa1ec7f16840e18f4a56924d34a8e7eaa3))
- Added Overtake Aggression. ([`c26ac7a`](https://github.com/Asviix/F1Manager2024Logger/commit/c26ac7aa1ec7f16840e18f4a56924d34a8e7eaa3))
- Added Defend Approach. ([`c26ac7a`](https://github.com/Asviix/F1Manager2024Logger/commit/c26ac7aa1ec7f16840e18f4a56924d34a8e7eaa3))
- Added Drive in Clean Air Mode. ([`c26ac7a`](https://github.com/Asviix/F1Manager2024Logger/commit/c26ac7aa1ec7f16840e18f4a56924d34a8e7eaa3))
- Added Avoid High Risk Kerbs Mode. ([`c26ac7a`](https://github.com/Asviix/F1Manager2024Logger/commit/c26ac7aa1ec7f16840e18f4a56924d34a8e7eaa3))
- Added Don't Fight Teammate Mode. ([`600cbdc`](https://github.com/Asviix/F1Manager2024Logger/commit/600cbdc41c98a231199916e390bf6cf97fbd5e42))
- Added Different Point Schemes. ([`10a4b91`](https://github.com/Asviix/F1Manager2024Logger/commit/10a4b9160adf8c3a3d5130964ede65ff4d1bfc8e))

## BETA 0.5 - Everything is easier!

### Accessibility
For sake of simplicity, I have made a number of key changes to the Script.

Now for installation, all that's needed is to run the setup and follow the instructions, everything will be done automatically and you'll have neat Desktop Shortcut to start it!

Also, the Plugin DLL will now be automatically moved to SimHub's installation folder by the script, it's never been easier to become a pro!

### Changed
- Settings will now reset on a new update. ([`6eae649`](https://github.com/Asviix/F1Manager2024Logger/commit/6eae649fa946cec70f2ed37653ea0d7027fda350))
- Refactored Gaps and Position calculations to save on CPU cycles. ([`3acec76`](https://github.com/Asviix/F1Manager2024Logger/commit/3acec7661abad64e03faf9b90eced4059abc195d))
- Full Revamp of the Wiki.
- Full Revamp of README.md.

### Added
- Added Energy Harvested and Deployed during a lap. ([`82baa8a`](https://github.com/Asviix/F1Manager2024Logger/commit/82baa8a4dc41abd8f1fd89902e78f2a21dcb85d8))
- Added Fuel Delta. ([`b0783a6`](https://github.com/Asviix/F1Manager2024Logger/commit/b0783a607abee1fa749a494bb78694c607bb9db5))
- Added Tire Surface Temperature. ([`2608876`](https://github.com/Asviix/F1Manager2024Logger/commit/2608876f53af7d107ee77e102e66f4fcbabfca0e))
- Added Tire Age. ([`1050c48`](https://github.com/Asviix/F1Manager2024Logger/commit/1050c48699c280d1fdb482fb3f0b647bafe5449d))
- Added Brake Temperature. ([`02aa918`](https://github.com/Asviix/F1Manager2024Logger/commit/02aa918a345bf3e88dde753aee8ab7ff440a2249))
- Added Number of Laps/Time Remaining depending on the type of session and track. ([`76914de`](https://github.com/Asviix/F1Manager2024Logger/commit/76914de90d675aa760aafd7709f40b483614aaac))
- Added Distance Travelled per Lap. ([`66911ca`](https://github.com/Asviix/F1Manager2024Logger/commit/66911ca3112140ea28bc6bc9ddfb1ead31de69de))
- Added Data for in Front and Behind. ([`22fcc60`](https://github.com/Asviix/F1Manager2024Logger/commit/22fcc6087bc5ad49360c9443d9659fb3c2d53fd9))
- Added Gap to Driver in Front/Behind/To Leader. ([`8bf4b61`](https://github.com/Asviix/F1Manager2024Logger/commit/8bf4b6154dabe52fa898357b6aa812818dbb1e9f)) & ([`339e96a`](https://github.com/Asviix/F1Manager2024Logger/commit/339e96a49662e4a0113710b22978ab300e59b3c0))

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