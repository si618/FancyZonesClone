# Security policy

## Report a vulnerability

To report a vulnerability, open a private advisory under the repository's **Security** tab with **Report a vulnerability**, at <https://github.com/si618/FancyZonesClone/security/advisories/new>. Don't open a public issue, pull request, or discussion for it.

Include the version, the steps to reproduce, and what an attacker gains. FancyZonesClone runs unelevated in the user's session, so say what your report reaches beyond that session's own rights.

A fix ships as a new release, and the advisory is published once that release is out.

## Supported versions

Only the latest release gets security fixes. To pick up a fix, download the newest release.

## Scope

In scope:

- `FancyZonesClone.exe` and the native DLLs in the release zip
- How the app reads `config.json` and the PowerToys FancyZones files it imports
- The workflows in `.github/workflows` that build and publish releases

Report a vulnerability in Avalonia, SkiaSharp, or another dependency to its own project. When it reaches FancyZonesClone, report that here as well, so a release can pick up the fix.

To check a download before you report it, compare its hash with the release's `SHA256SUMS`, or verify it with `gh attestation verify`, as the README describes.
