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