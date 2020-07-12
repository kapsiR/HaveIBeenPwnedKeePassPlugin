# Changelog
The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]
### 

## [0.6.0] - 2020-07-12
### Added
 - Added a new option to enable a good news (no breach) message, when an entry is manually checked ('check current password')
 - When a password entry has the tag 'pwned-ignore', it will be ignored on all automatic checks (Can be toggled via context menu)

## [0.5.0] - 2020-04-26
### Added
 - New status indicator and counts in the status bar while checking the whole database for pwned passwords

## [0.4.0] - 2020-03-05
### Changed
 - Enhanced privacy by supporting the new [pwned passwords padding](https://www.troyhunt.com/enhancing-pwned-passwords-privacy-with-padding/) by [Troy Hunt], [Junade Ali] and [Matt Weir]

## [0.3.1] - 2020-01-10
### Changed
 - If there are connection problems with the HIBP API, the automatic check when changing an entry is disabled
 - Improved response handling for breach count to make it more cross-platform compatible

### Fixed
 - Added a message for connectivity issues with the HIBP API

## [0.3.0] - 2019-10-23
### Added
 - Option for skipping expired entries (enabled by default)

## [0.2.0] - 2019-01-29
### Added
 - Added .NET requirements to Readme 
 - Publish a release on GitHub

### Changed
 - Improved handling of "has database been modified" (only indicate it if there is really any change)
 - Removed dependency on System.ValueTuple (target .NET 4.7.2)

## [0.1.0] - Initial release
### Added
 - Search againgst the Pwned Passwords service of [HIBP](https://haveibeenpwned.com) with the [k-Anonymity model](https://blog.cloudflare.com/validating-leaked-passwords-with-k-anonymity/)
 - Search the whole database against HIBP Pnwed Passwords
 - Search current selected entry against HIBP Pwned Passwords
 - Check HIBP Pnwed Passwords service when an entry gets modified (optional)
 - Big thanks to [Troy Hunt] and [Junade Ali] who make this possible!

[Unreleased]: https://github.com/kapsiR/HaveIBeenPwnedKeePassPlugin/compare/v0.6.0...HEAD
[0.6.0]: https://github.com/kapsiR/HaveIBeenPwnedKeePassPlugin/compare/v0.5.0...v0.6.0
[0.5.0]: https://github.com/kapsiR/HaveIBeenPwnedKeePassPlugin/compare/v0.4.0...v0.5.0
[0.4.0]: https://github.com/kapsiR/HaveIBeenPwnedKeePassPlugin/compare/v0.3.1...v0.4.0
[0.3.1]: https://github.com/kapsiR/HaveIBeenPwnedKeePassPlugin/compare/v0.3.0...v0.3.1
[0.3.0]: https://github.com/kapsiR/HaveIBeenPwnedKeePassPlugin/compare/v0.2.0...v0.3.0
[0.2.0]: https://github.com/kapsiR/HaveIBeenPwnedKeePassPlugin/compare/v0.1.0...v0.2.0
[0.1.0]: https://github.com/kapsiR/HaveIBeenPwnedKeePassPlugin/releases/tag/v0.1.0

[Troy Hunt]: https://www.troyhunt.com
[Junade Ali]: https://icyapril.com
[Matt Weir]: https://reusablesec.blogspot.com/