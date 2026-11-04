# FancyZonesClone

A standalone port of [PowerToys FancyZones](https://learn.microsoft.com/windows/powertoys/fancyzones): drag a window with Shift held to snap it into a zone, without installing the rest of PowerToys. It's one Native AOT executable with a tray icon, and it imports your existing FancyZones layouts.

FancyZonesClone isn't affiliated with or endorsed by Microsoft.

## Features

- **Drag to snap.** Hold Shift while dragging a window to show the zones, and drop it on one. Hold Ctrl as well to span every zone between the first one you hit and the current one. With `shiftDrag` off, zones show on every drag and Shift hides them.
- **Keyboard snapping.** With `overrideSnapHotkeys` on, Win+Left and Win+Right move the active window to the previous or next zone, wrapping at the ends.
- **Layout hotkeys.** Win+Ctrl+Alt+digit applies a custom layout to the active monitor, and flashes its zones.
- **Every FancyZones layout type.** Blank, focus, columns, rows, grid, and priority grid templates, and custom grid and canvas layouts. The zone arithmetic is ported from FancyZones, so a layout produces the same pixels.
- **Per-monitor layouts.** Each monitor is identified by device and connection, as FancyZones does, with fallbacks to serial number and device. Unknown monitors get a default layout for their orientation.
- **The FancyZones overlap rules.** Smallest, largest, positional, and closest-centre selection for overlapping canvas zones.
- **PowerToys import.** On first run, FancyZonesClone imports custom layouts, per-monitor assignments, default layouts, layout hotkeys, and settings from `%LOCALAPPDATA%\Microsoft\PowerToys\FancyZones`, if they're there.
- **Tray icon and layouts window.** Pick each monitor's layout, with a live preview. Pause snapping, re-import, or open the config file.

### Not yet

- A layout editor. To create or change a custom layout, edit `config.json`, or design it in FancyZones on another machine and re-import.
- Layouts per virtual desktop. Import keeps the first layout found for each monitor.
- Restoring a window's original size when it's dragged out of a zone, moving windows when the display configuration changes, reopening apps in their last zone, Win+PgUp and Win+PgDn window switching, and middle-click spanning.
- Moving windows of elevated apps. Windows blocks this for unelevated processes, so to move them, run FancyZonesClone elevated.

## Install

Download `FancyZonesClone-<version>-win-x64.zip` from the [releases](../../releases), unzip it anywhere, and run `FancyZonesClone.exe`. It needs no installer, no admin rights, and no .NET runtime. To start it at sign-in, put a shortcut to it in `shell:startup`.

## Configure

The config lives at `%APPDATA%\FancyZonesClone\config.json`. It's created on first run, imported from FancyZones when possible. The tray menu opens and reloads it. The file accepts comments and trailing commas. Layout types are written as names, such as `"kind": "PriorityGrid"`.

To check which layout each monitor resolves to, run this from a terminal:

```powershell
Start-Process .\FancyZonesClone.exe --diagnose -Wait -RedirectStandardOutput diagnose.txt; Get-Content diagnose.txt
```

It prints each monitor's identity, work area, layout, and zone rectangles, then exits without installing any hooks.

## Build

```sh
dotnet build FancyZonesClone.slnx -c Release -warnaserror
dotnet test --project tests/FancyZonesClone.Core.Tests
dotnet publish src/FancyZonesClone -c Release -r win-x64
```

The solution builds on Linux, and the Core tests run there. The Native AOT publish needs Windows with the Visual Studio C++ build tools. For a quick local build without them, add `-p:PublishAot=false --self-contained`.

Avalonia's build tasks and the TUnit test host send usage telemetry. To opt out, set `AVALONIA_TELEMETRY_OPTOUT=1` and `TESTINGPLATFORM_TELEMETRY_OPTOUT=1` in your environment. CI and release set both.

The code follows the [awesome-humans .NET opinions](https://awesome-humans.net/dotnet/) with the `simon-mckenna` house profile, and tests use [TUnit](https://tunit.dev). [AGENTS.md](AGENTS.md) describes the layout, conventions, and workflow for maintaining the repository.

## Releases

[CI](.github/workflows/ci.yml) builds, tests, and AOT-publishes every push to `main` and every pull request. The [Release workflow](.github/workflows/release.yml) runs only when a `v*` tag is pushed, and fails unless the tagged commit is on `main`. [CodeQL](.github/workflows/codeql.yml) scans the C# and the workflows on every push to `main`, every pull request, and weekly.

To publish a release:

1. Make sure `main` is green in CI and contains everything intended for the release.
2. Pick the version following [SemVer](https://semver.org/). The tag is the only place the version is set: [MinVer](https://github.com/adamralph/minver) reads it at build time, so there's no `<Version>` to edit. Builds between releases get a pre-release version above the last tag, such as `1.0.1-alpha.0.3` for 3 commits after `v1.0.0`.
3. Tag the `main` commit and push the tag:

    ```sh
    git tag -a v1.0.0 -m "FancyZonesClone 1.0.0"
    git push origin v1.0.0
    ```

    A pre-release tag such as `v1.1.0-beta.1` publishes a pre-release. The `release tags` ruleset lets only repository admins create, move, or delete `v*` tags.

4. Watch the Release run under the repository's Actions tab. It tests, AOT-publishes win-x64, checks that the executable's version matches the tag, and creates a GitHub release with notes generated from the pull requests merged since the previous tag, with the zipped executable attached. A separate job publishes the release, so the build steps never hold the write token. It attaches a `SHA256SUMS` file and records a build provenance attestation for the zip.

To check a download, compare its hash with `SHA256SUMS`, or verify its provenance with the GitHub CLI:

```sh
gh attestation verify FancyZonesClone-1.0.0-win-x64.zip --repo si618/FancyZonesClone
```

To check that a release rebuilds byte for byte, run the [Reproducibility workflow](.github/workflows/reproducible.yml) on its tag. It AOT-publishes the tag on two runners and compares the SHA-256 of every exe and DLL.

```sh
gh workflow run reproducible.yml -f ref=v1.0.0
```

Releases aren't Authenticode-signed yet, so Windows SmartScreen warns on first run.

To fix a bad release, tag a new version.

## Security

To report a vulnerability, follow [SECURITY.md](SECURITY.md). Only the latest release gets security fixes.

## Licence

[MIT](LICENSE). Portions are ported from Microsoft PowerToys, also MIT. See [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).
