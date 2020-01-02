# Changelog
The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]
## Changed
 - Improved response handling for breach count to make it more cross-platform compatible

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
 - Big thanks to [Troy Hunt](https://www.troyhunt.com) and [Junade Ali](https://icyapril.com) who make this possible!

[Unreleased]: https://github.com/kapsiR/HaveIBeenPwnedKeePassPlugin/compare/v0.3.0...HEAD
[0.3.0]: https://github.com/kapsiR/HaveIBeenPwnedKeePassPlugin/compare/v0.2.0...v0.3.0
[0.2.0]: https://github.com/kapsiR/HaveIBeenPwnedKeePassPlugin/compare/v0.1.0...v0.2.0
[0.1.0]: https://github.com/kapsiR/HaveIBeenPwnedKeePassPlugin/releases/tag/v0.1.0
