# Roadmap (unordered):
 - [ ] When checking all passwords, add any status indicator
 - [ ] Publish a release on GitHub (including a .plgx file)
     - Note: As of today, KeePass doesn't support compilation of plgx with newer C# versions
 - [ ] Implement any caching or an option to disable check when already known as pwned?
 - [ ] Option for ignoring expired entries
 - [ ] Check for groups

-----------
# Changelog
## 0.2.0
 - [x] Improve handling of "has database been modified" (only indicate it if there is really any change)
 - [x] Removed dependency on System.ValueTuple (target .NET 4.7.2)
 - [x] Publish a release on GitHub
 - [x] Added .NET requirements to Readme 

## 0.1.0 - Initial release
Search againgst the Pwned Passwords service of [HIBP](https://haveibeenpwned.com) with the [k-Anonymity model](https://blog.cloudflare.com/validating-leaked-passwords-with-k-anonymity/)
 - Search the whole database against HIBP Pnwed Passwords
 - Search current selected entry against HIBP Pwned Passwords
 - Check HIBP Pnwed Passwords service when an entry gets modified (optional)
 - Big thanks to [Troy Hunt](https://www.troyhunt.com) and [Junade Ali](https://icyapril.com) who make this possible!
