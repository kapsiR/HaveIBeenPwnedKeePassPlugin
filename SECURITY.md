# Security Policy

## General information

KeePass [has been audited and is broadly trusted].  
There is a dedicated page about [plugins and plugin security].
This plugin is listed in the [plugin directory] of KeePass.

All accounts with write access to this repository are mandated to use two-factor authentication.

### Releases

Release builds are configured to be deterministic. (Easily reproducible, since binary content is identical for the same input across compilations)
The corresponding Git commit can be read from the product version of the assembly. (e.g. `0.7.1+39ecaf0b99` identifies [39ecaf0b99])  
Integrity hashes are available on the release page. (Since 2023-01)

### Updates

KeePass queries the [KeePass.version] file for updates, but won't install any update automatically.  
It is recommended to specifiy appropriate file permissions for the plugin directory so that non-admin users can't hijack the plugin.

## Reporting a Vulnerability

Please use the [private vulnerability reporting] that GitHub provides.  
I'll do my best to give a timely answer.

For .NET Framework vulnerabilites contact the [Microsoft Security Response Center].  

[private vulnerability reporting]: https://github.com/kapsiR/HaveIBeenPwnedKeePassPlugin/security/advisories
[Microsoft Security Response Center]: https://msrc.microsoft.com/report/vulnerability/new
[has been audited and is broadly trusted]: https://keepass.info/help/kb/trust.html
[plugins and plugin security]: https://keepass.info/help/v2/plugins.html
[39ecaf0b99]: https://github.com/kapsiR/HaveIBeenPwnedKeePassPlugin/commit/39ecaf0b99f4c37139af6485a1bdc4d9a2c5171d
[KeePass.version]: https://github.com/kapsiR/HaveIBeenPwnedKeePassPlugin/blob/v0.7.1/KeePass.version
[plugin directory]: https://keepass.info/plugins.html
