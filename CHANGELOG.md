# Changelog

All notable changes to this project will be documented in this file.

## [1.1.0] - 2026-08-20
### Added
- You can now unlock locked chests with keys from your networks (remote terminal required)
### Changed
- Minor rewrites of a few functions to improve performance (and maintainability)
### Fixed
- Fixed some obvious issues in the networking code (still untested, I have no friends)

## [1.2.0] - 2026-08-26
### Added
- Fishing & Pets filters, can disable them in config
- Defrag also now also restacks items inside disks (semi-fix for the weird behavior where items sometime create a new stack instead of just stacking)
### Changed
- Made custom filters creation code a lot more flexible
- All of the strings are no longer hardcoded and all have a localization entry
### Fixed
- Nothing

## [1.3.0] - 2026-08-27
### Added
- Hijacked & rewrote the item insertion logic of the main mod, it should fix the issue where blocks/tiles/consummables would not stack sometimes and create new stacks instead (happening with mods adding useless NBT data to all items).
This doesn't affect equipment which is handled in a similar fashion as before to preserve stuff like terracard slots.
- This will be contributed to requisition itself if no issues arise with it.
- You can enable the vanilla item insertion logic in the config if you want to use the original behavior (but you shouldn't want to if you have no issue with my implementation)
### Changed
- Minor code cleanup, still horrible.
### Fixed
- Nothing