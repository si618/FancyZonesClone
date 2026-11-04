# Security audit, 2026-11-04

This audit covers FancyZonesClone at commit `6e91580` on `main`: the source, the dependencies, CI and release, the repository settings, and how the app behaves on a Windows desktop. It answers [issue 4](https://github.com/si618/FancyZonesClone/issues/4), so someone who approves software for internal use can sign it off from this page.

## Approval summary

**Recommendation:** approve for internal use once FZC-01 to FZC-03 are fixed and a signed release exists. No finding lets the app cross a security boundary. It runs as the signed-in user, makes no network connections, and stores one JSON file. The open risks are in how a release is built and published, and in robustness bugs that stop snapping but can't be exploited across users.

| Question                     | Answer                                                                                                                                                                                                                                                                                                                                                                    |
| ---------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| What can it see?             | Every key press in the user's session, through a `WH_KEYBOARD_LL` hook. It acts only on Win+Left, Win+Right, and Win+Ctrl+Alt+0 to 9, and never stores, logs, or forwards a key. It also sees window styles and positions, the cursor and Shift and Ctrl state during a window drag, the executable path of the window it moves, and monitor IDs and EDID serial numbers. |
| What does it change?         | The position and size of other apps' windows, when the user drags with Shift or presses a snap hotkey. After it swallows Win+arrow, it taps an unassigned key (`0xE8`) so the Start menu doesn't open. It shows a topmost, click-through overlay. It can't move windows of elevated apps.                                                                                 |
| What does it store?          | `%APPDATA%\FancyZonesClone\config.json`, with `config.json.tmp` during a save and `config.json.invalid` when a broken file is set aside. It reads `%LOCALAPPDATA%\Microsoft\PowerToys\FancyZones\*.json` to import layouts. It writes nothing else, and uninstalling leaves `%APPDATA%\FancyZonesClone` behind.                                                           |
| What leaves the machine?     | Nothing at runtime: no telemetry, crash reporting, or update check. Building from source sends Avalonia's build telemetry, and running the tests sends Microsoft.Testing.Platform telemetry, unless you opt out (FZC-09).                                                                                                                                                 |
| What privileges does it use? | None beyond the user's. The manifest sets `asInvoker` and `uiAccess="false"`, and no code path asks for elevation.                                                                                                                                                                                                                                                        |
| How is it installed?         | Unzip and run `FancyZonesClone.exe`. There's no installer, autostart is a shortcut in `shell:startup`, upgrades are manual, and there's no automatic update. The executable isn't signed (FZC-03).                                                                                                                                                                        |

Endpoint tools flag any `WH_KEYBOARD_LL` hook as keylogger behaviour. Section [Input monitoring](#input-monitoring-and-control) records why this one isn't, so you can whitelist it with evidence.

## Threat model

### Assets

- **Keystrokes** that pass through the keyboard hook, which may include passwords typed into other apps.
- **The user's windows,** which the app can move and resize.
- **The release artefact,** `FancyZonesClone-<version>-win-x64.zip`, which users run with their full rights.
- **The repository and its release pipeline,** which decide what goes into that artefact.
- **`config.json`,** which holds layouts, monitor IDs, and excluded app paths.

### Trust boundaries

1. **The same-user desktop.** This is the main boundary. The app runs at medium integrity as the user, in the user's session. Any other medium-integrity process of the same user can already install the same hooks, move the same windows, and edit the same files. A finding that needs such a process to act is informational unless it gains something the process didn't have.
2. **Other users and elevated processes.** These are out of reach. With `asInvoker` and `uiAccess="false"`, User Interface Privilege Isolation stops the app moving elevated windows or injecting input into them, and hooks and window messages don't cross sessions. The app can't touch another user's session, files, or windows.
3. **Lower-integrity processes in the session,** such as a sandboxed browser renderer. They can't write `%APPDATA%`, and UIPI stops them sending messages to the app's windows. The hook ignores injected keys (`LLKHF_INJECTED`), so `SendInput` from another process can't trigger a hotkey.
4. **The network.** The app has no network surface.
5. **The supply chain:** nuget.org, GitHub Actions, the maintainer's account, and anyone with write access to the repository. This boundary carries the real risk, because whoever controls it controls the executable.

### Attackers

| Attacker                                     | Can they harm users through FancyZonesClone?                                                                                |
| -------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------- |
| Remote attacker on the network               | No. There's no listener, client, or update channel.                                                                         |
| Another user on the machine, or over RDP     | No. Per-session hooks, a `Local\` mutex, and a per-user config keep sessions apart.                                         |
| Malware running as the user                  | Gains nothing new. It can already do everything the app does. It could hang the app with a hostile config (FZC-04, FZC-05). |
| Low-integrity or AppContainer process        | No path found. See boundary 3.                                                                                              |
| Compromised NuGet package or build tool      | Yes, by changing the executable at build time, and through FZC-01 by writing to the repository.                             |
| Someone with write access, or a stolen token | Yes: push a `v*` tag on an unreviewed commit and publish a release (FZC-02).                                                |
| Someone who tampers with a download          | Users can't tell, because releases have no signature, provenance, or checksums (FZC-03).                                    |

## Findings

Ratings: **critical** lets an attacker cross a boundary with no user action, **high** crosses a boundary with some precondition, **medium** weakens the supply chain or a control the approver relies on, **low** affects availability or hardening without crossing a boundary, and **informational** records a fact the approver should know.

| ID     | Severity      | Finding                                                                |
| ------ | ------------- | ---------------------------------------------------------------------- |
| FZC-01 | Medium        | The release job's write token is available to every build step         |
| FZC-02 | Medium        | Any `v*` tag on any commit publishes a release                         |
| FZC-03 | Medium        | Releases are unsigned, with no provenance or checksums                 |
| FZC-04 | Low           | A config with a missing or null collection loads, then breaks snapping |
| FZC-05 | Low           | Unbounded zone counts hang the thread that runs the keyboard hook      |
| FZC-06 | Low           | Win+arrow does slow work inside the low-level keyboard hook            |
| FZC-07 | Low           | Exceptions in tray menu handlers end the process                       |
| FZC-08 | Low           | Restore isn't locked, sources aren't mapped, and there's no SBOM       |
| FZC-09 | Low           | Builds and test runs send telemetry                                    |
| FZC-10 | Low           | Repository security settings and policy are incomplete                 |
| FZC-11 | Low           | No code scanning                                                       |
| FZC-12 | Informational | App exclusions fail open for process paths of 260 characters or more   |
| FZC-13 | Informational | The overlay doesn't check that its GDI surface was created             |
| FZC-14 | Informational | Third-party notices miss two compiled-in packages                      |
| FZC-15 | Informational | `--diagnose` prints monitor serial numbers                             |
| FZC-16 | Informational | Trace output goes to the session's debug stream                        |
| FZC-17 | Informational | Another process in the session can stop the app starting               |
| FZC-18 | Informational | "Open config file" runs the user's `.json` handler                     |
| FZC-19 | Informational | `INPUT` is laid out for 64-bit only                                    |

### FZC-01: The release job's write token is available to every build step

**Severity:** medium.

**Evidence:** `release.yml` gives its only job `contents: write`. `actions/checkout` keeps `persist-credentials` at its default of `true`, so it writes that `GITHUB_TOKEN` into `.git/config`. The job then runs `dotnet restore`, `build`, `test`, and `publish`, which execute code from NuGet packages: MSBuild targets such as `Avalonia.BuildServices`' `AvaloniaStatsTask`, analyzers, source generators, the ILCompiler, and TUnit's test host. The workflow also has no top-level `permissions` block.

**Impact:** a compromised package version can read the token and, until the job ends, push branches and tags, or create and edit releases and their assets. Build-time code can already tamper with the executable, but the token lets it also tamper with the repository and with earlier releases.

**Fix:** set `persist-credentials: false` on checkout in both workflows. Add `permissions: {}` at the top of `release.yml`. Split the job in two: a `build` job with `contents: read` that tests, publishes, and uploads the zip as an artefact, and a `release` job with `contents: write` that only downloads it and runs `gh release create`.

### FZC-02: Any `v*` tag on any commit publishes a release

**Severity:** medium.

**Evidence:** `release.yml` runs on any pushed `v*` tag. `gh release create --verify-tag` checks only that the tag exists. The repository has one ruleset, `main`, which targets the default branch: it requires a pull request, squash merges, and the `build` check, and blocks force pushes and deletion. No ruleset targets tags (`GET /repos/si618/FancyZonesClone/rulesets` lists only `main`).

**Impact:** anyone with write access, or a token with `contents: write` (see FZC-01), can tag a commit that never went through review or CI on `main`, and the workflow ships it as a release.

**Fix:** add a tag ruleset for `refs/tags/v*` that restricts creation, update, and deletion to the maintainer. In `release.yml`, fail unless the tagged commit is on `main`:

```sh
git merge-base --is-ancestor "$GITHUB_SHA" origin/main
```

### FZC-03: Releases are unsigned, with no provenance or checksums

**Severity:** medium.

**Evidence:** `release.yml` zips the published files and uploads them. Nothing signs `FancyZonesClone.exe` or the three native DLLs, attests the build, or publishes hashes. A local build reports `NotSigned` from `Get-AuthenticodeSignature`.

**Impact:** a user can't tell a release from a tampered download. SmartScreen warns on first run. AppLocker publisher rules and WDAC policies can't allow the app by signer, so each version needs a hash rule.

**Fix:**

- Sign the executable and DLLs with Authenticode in the release job, for example with Azure Trusted Signing or SignPath's free plan for open-source projects.
- Add `actions/attest-build-provenance` for the zip, with `id-token: write` and `attestations: write` on that job only.
- Publish a `SHA256SUMS` file with each release.
- Check reproducibility: `Deterministic` is on and `ContinuousIntegrationBuild` is on in CI, but this audit couldn't rebuild a tag, because there's no release yet and Native AOT needs MSVC. Rebuild the first tag twice in CI and compare the hashes.

### FZC-04: A config with a missing or null collection loads, then breaks snapping

**Severity:** low.

**Evidence:** `FancyZonesCloneConfig.CustomLayouts`, `Monitors`, and `LayoutHotkeys`, and `FancyZonesCloneSettings.ExcludedApps`, are `init` properties initialised to `[]`. With the source-generated `ConfigJsonContext`, a missing key leaves them `null`, not empty:

```text
{}                  customLayouts=NULL monitors=NULL layoutHotkeys=NULL settings=ok excludedApps=ok
{"settings":{}}     customLayouts=NULL monitors=NULL layoutHotkeys=NULL settings=ok excludedApps=NULL
```

Explicit `null` values do the same, and `ConfigStore.TryLoad` reports success for all of them. The fuzzing harness then threw on use:

| Input                                             | Exception                                                                       |
| ------------------------------------------------- | ------------------------------------------------------------------------------- |
| `monitors` missing or `null`                      | `ArgumentNullException` in `LayoutResolver.FindAssignment`                      |
| `settings: null`                                  | `ArgumentNullException` in `OverlayStyle.From`                                  |
| `defaultHorizontalLayout: null`                   | `NullReferenceException` in `LayoutResolver.ZoneSetFor`                         |
| `excludedApps: [null]`, or `excludedApps` missing | `NullReferenceException` or `ArgumentNullException` in `WindowRules.IsExcluded` |
| A monitor assignment with `monitor: null`         | `NullReferenceException` in `LayoutResolver.FindAssignment`                     |
| A grid with a `null` `cellChildMap` row           | `NullReferenceException` in `ZoneCalculator.IsWellFormed`                       |
| A canvas with `zones: null`                       | `NullReferenceException` in `ZoneCalculator.Canvas`                             |

**Impact:** a hand-edited config that leaves out a section, which the README invites, loads without a warning. `ZoneEngine.Guard` traps each exception, so snapping and the hotkeys silently stop working. `LayoutChoice` reads `config.CustomLayouts`, so opening the layouts window throws on the UI thread and ends the process. A hostile file has the same effect, but writing it needs the user's rights.

**Fix:** set `RespectNullableAnnotations = true` and `RespectRequiredConstructorParameters = true` on `ConfigJsonContext`, or give the collections setters so missing keys keep their defaults. Then validate after loading, and reject or repair `null` entries inside lists. Add `ConfigStore` tests for `{}`, each section missing, and each `null`.

### FZC-05: Unbounded zone counts hang the thread that runs the keyboard hook

**Severity:** low.

**Evidence:** `LayoutSpec.ZoneCount` has no upper bound, in `config.json` or from `PowerToysImporter`, which copies `zone-count` as is. `ZoneCalculator.TryAdd` checks for a duplicate index with `List.Exists`, which is O(n²) over a layout. On a 3840×2100 work area:

| Layout                                         | Time               |
| ---------------------------------------------- | ------------------ |
| `Focus`, 10,000 zones                          | 198 ms             |
| `Focus`, 100,000 zones                         | over 5 s (stopped) |
| `Grid` or `PriorityGrid`, `int.MaxValue` zones | over 5 s (stopped) |
| Canvas, 10,000 zones                           | 899 ms             |
| Canvas, 50,000 zones                           | 5,248 ms           |

`ZoneEngine` builds the zone set on the Avalonia UI thread for every drag onto a monitor, for a layout hotkey, and inside the keyboard hook for Win+arrow. That thread also runs the hook.

**Impact:** while the thread is stuck, each key press in the session waits for the hook's timeout, and Windows then removes the hook without telling the app. A typo such as `"zoneCount": 100000` is enough.

**Fix:** clamp `ZoneCount` to a small maximum, such as 128, when loading and importing, and reject custom layouts with more zones than that. Track seen indexes in a `HashSet<int>` in `TryAdd`. Add tests for the cap.

### FZC-06: Win+arrow does slow work inside the low-level keyboard hook

**Severity:** low.

**Evidence:** with `overrideSnapHotkeys` on, `ZoneEngine.HandleKey` runs inside `OnKeyboard` and calls `IsSnappable`, which opens the foreground window's process and queries its image path. It then calls `Monitors.FromWindow`, which enumerates display devices and reads the EDID from the registry twice, and `GetZoneSet`. The code defers the move itself for this reason, but not the checks that decide whether to swallow the key.

**Impact:** a slow registry or display driver call, or FZC-05, can push the hook past `LowLevelHooksTimeout`. Windows then removes the hook, and Win+arrow and the layout hotkeys stop working until the app restarts.

**Fix:** cache each monitor's descriptor and whether its zone set is empty, keyed by `HMONITOR`, and refresh it outside the hook, for example on `WM_DISPLAYCHANGE` and config changes. Keep the hook to `GetForegroundWindow`, `MonitorFromWindow`, and a cache lookup.

### FZC-07: Exceptions in tray menu handlers end the process

**Severity:** low.

**Evidence:** `App.OnOpenConfig` calls `Process.Start` with `UseShellExecute`, which throws `Win32Exception` when no app is associated with `.json`. `OnImport` calls `PowerToysFiles.ReadFrom` and `ConfigStore.Save`, and `OnMonitorLayoutChanged` calls `ConfigStore.Save`. Both throw `IOException` when an editor or antivirus holds the file. None of the handlers catch, and the app sets no `Dispatcher.UIThread.UnhandledException` handler.

**Impact:** the tray app exits, and snapping stops until the user restarts it.

**Fix:** catch `Win32Exception`, `IOException`, and `UnauthorizedAccessException` in each handler, trace them, and tell the user. Add a last-chance `UnhandledException` handler that traces and keeps running.

### FZC-08: Restore isn't locked, sources aren't mapped, and there's no SBOM

**Severity:** low.

**Evidence:** `RestorePackagesWithLockFile` is `false`, so CI restores whatever bytes nuget.org serves for each pinned version. The repository has no `nuget.config`, so restore uses every source in the machine's and user's NuGet configuration. Releases ship no software bill of materials.

**Impact:** a feed that serves different content for a pinned version goes unnoticed. On a developer machine with an extra feed, a package with the same name on that feed can win. An approver can't list what's inside a release without rebuilding it.

**Fix:**

- Set `RestorePackagesWithLockFile` to `true`, commit the `packages.lock.json` files, and run `dotnet restore --locked-mode` in both workflows. Renovate updates lock files.
- Add a `nuget.config` that clears sources, adds nuget.org, and maps `*` to it with `packageSourceMapping`.
- Generate a CycloneDX SBOM in the release job, for example with `dotnet CycloneDX` or `anchore/sbom-action`, and attach it to the release.

### FZC-09: Builds and test runs send telemetry

**Severity:** low.

**Evidence:** Avalonia pulls in `Avalonia.BuildServices` 11.3.2, whose `AvaloniaStats` target runs before every compile and, by its README, sends hashed project and machine names, the OS, the IDE, and CI detection. TUnit pulls in `Microsoft.Testing.Extensions.Telemetry`. Neither ships in the executable.

**Impact:** every developer build and CI run reports to Avalonia and Microsoft. Some organisations don't allow that from build machines.

**Fix:** set `AVALONIA_TELEMETRY_OPTOUT: 1` and `TESTINGPLATFORM_TELEMETRY_OPTOUT: 1` in both workflows' `env`, and mention both in the README's build section.

### FZC-10: Repository security settings and policy are incomplete

**Severity:** low.

**Evidence:** checked through the public API:

- Private vulnerability reporting is off (`GET .../private-vulnerability-reporting` returns `"enabled": false`).
- There's no `SECURITY.md`.
- There's no tag ruleset (see FZC-02).
- The `main` ruleset is active and requires a pull request, squash merges, and the `build` check, with no force pushes or deletion. It requires no approving review, which suits a one-maintainer repository.

These need an admin token, so the owner should check them: secret scanning and push protection, Dependabot alerts, the ruleset's bypass list, collaborators, installed GitHub Apps and their permissions, deploy keys, 2FA, and whether to require signed commits. Squash merges on `main` already carry GitHub's verified signature, as `6e91580` does.

A scan of all four commits in the history for keys, tokens, and passwords found none.

**Fix:** turn on private vulnerability reporting, secret scanning, push protection, and Dependabot alerts. Add a `SECURITY.md` that points to private reporting and says only the latest release gets fixes. Confirm Renovate's GitHub App is the only app with write access.

Renovate's setup is sound: it pins action digests (`helpers:pinGitHubActionDigests`), no rule enables automerge, and vulnerability alerts open pull requests at any time, which still need the `build` check to merge.

### FZC-11: No code scanning

**Severity:** low.

**Evidence:** the repository has no CodeQL or other code scanning workflow.

**Fix:** add CodeQL with the `csharp` and `actions` languages. Use `build-mode: manual` for C# with `dotnet build`, because the Native AOT publish isn't needed for analysis.

### FZC-12: App exclusions fail open for process paths of 260 characters or more

**Severity:** informational.

**Evidence:** `WindowOperations.ProcessPath` passes a 260-character buffer to `QueryFullProcessImageNameW`. A longer path fails with `ERROR_INSUFFICIENT_BUFFER`, the method returns `""`, and `WindowRules.IsExcluded` never matches. The handle is always closed in a `finally`, and on success `size` excludes the terminator, so the slice is in range. A failed call returns nothing rather than a truncated path, so it can't produce a wrong match.

**Impact:** an excluded app installed under a deep path still snaps. Exclusion is a convenience, not a security control.

**Fix:** on `ERROR_INSUFFICIENT_BUFFER`, retry with a 32,768-character buffer from `ArrayPool<char>`.

### FZC-13: The overlay doesn't check that its GDI surface was created

**Severity:** informational.

**Evidence:** `OverlayWindow.EnsureSurface` doesn't check the results of `CreateCompatibleDC` or `CreateDIBSection`. When `CreateDIBSection` fails, `_pixels` is `null`, and `Show` builds a `Span<uint>` over it with length `width * height`. `EnsureWindow` doesn't check `CreateWindowExW` either. GDI handles are otherwise released correctly: `ReleaseSurface` selects the old bitmap back and deletes the DC and bitmap when the monitor size changes, and every `GetDC` has a matching `ReleaseDC` in a `finally`.

**Impact:** when the process runs out of GDI handles or memory, `OverlayRenderer.Render` clears the span from address 0. The runtime turns that fault into a `NullReferenceException`, which `ZoneEngine.Guard` traps, so the overlay doesn't show and the process keeps running. That outcome rests on the runtime's null-page handling rather than on a check.

**Fix:** check each handle, release what was created on failure, and skip showing the overlay when there's no surface. The `width * height` product can't overflow: a work area is at most 32,767 pixels on a side.

### FZC-14: Third-party notices miss two compiled-in packages

**Severity:** informational.

**Evidence:** Native AOT compiles reachable managed code from the whole graph into the executable. `THIRD-PARTY-NOTICES.md` lists Avalonia, SkiaSharp, HarfBuzzSharp, ANGLE, and the .NET runtime. It doesn't list `MicroCom.Runtime` 0.11.6 or `Tmds.DBus.Protocol` 0.94.1, both MIT, which Avalonia references. The runtime's own third-party notices also apply. The release zip contains `LICENSE` and `THIRD-PARTY-NOTICES.md`, and the native DLLs it ships, `libSkiaSharp.dll`, `libHarfBuzzSharp.dll`, and `av_libglesv2.dll`, match the notices.

**Fix:** generate the notices from the SBOM in FZC-08, or add the two packages and a pointer to the .NET runtime's `THIRD-PARTY-NOTICES.TXT`.

### FZC-15: `--diagnose` prints monitor serial numbers

**Severity:** informational.

**Evidence:** `Diagnose.Run` prints each monitor's EDID serial number. Users are told to paste the output when they report a problem.

**Impact:** a serial number identifies a physical monitor, which can tie an issue to an asset register. It reveals nothing else.

**Fix:** print only the last four characters by default, which is enough to tell monitors apart, and the full value with `--diagnose --full`.

### FZC-16: Trace output goes to the session's debug stream

**Severity:** informational.

**Evidence:** `Program` calls `LogToTrace`, and `App` and `ZoneEngine` call `Trace.TraceInformation`, `TraceWarning`, and `TraceError`. The default listener writes to `OutputDebugString`, which any process in the session can read. Messages hold the config path, which contains the user name, import warnings with layout names and monitor IDs, and exception text. None holds key data.

**Fix:** none needed. Keep key data out of traces when you change `OnKeyboard`.

### FZC-17: Another process in the session can stop the app starting

**Severity:** informational.

**Evidence:** `Program` exits when the `Local\FancyZonesClone` mutex already exists. Any process in the session can create it first.

**Impact:** the app silently doesn't start. That process could kill it anyway.

**Fix:** none needed. To tell the user, show a message when the mutex exists but no tray icon answers.

### FZC-18: "Open config file" runs the user's `.json` handler

**Severity:** informational.

**Evidence:** `App.OnOpenConfig` shell-executes `ConfigStore.DefaultPath`, `%APPDATA%\FancyZonesClone\config.json`. Only the user can change that folder or the `.json` association, so a junction, symlink, or hostile handler there needs the user's own rights. "Re-import from PowerToys" overwrites the config without asking, from files the same user owns.

**Fix:** none needed for security. Asking before "Re-import from PowerToys" overwrites the config would protect hand edits.

### FZC-19: `INPUT` is laid out for 64-bit only

**Severity:** informational.

**Evidence:** `INPUT` puts its union at `FieldOffset(8)`. That's right for x64 and Arm64, where `sizeof(INPUT)` is 40, but an x86 build would pass the wrong `cbSize`, and `SendInput` would fail. Only win-x64 is released.

**Fix:** none needed while only 64-bit ships. Add a comment, or compute the offset from `IntPtr.Size`.

## What the audit checked

### Input monitoring and control

- **`OnKeyboard` reads only what it acts on.** It reads `vkCode` and `flags` from `KBDLLHOOKSTRUCT`, and returns at once unless the key isn't injected, the engine isn't paused, and a Windows key is down. `HandleKey` reads the modifier state with `GetAsyncKeyState` and acts on Left, Right, and 0 to 9 only. No code stores, logs, or forwards a key. The one trace in that path logs a hotkey digit whose layout is missing.
- **WinEvent handlers.** Both hooks are `WINEVENT_OUTOFCONTEXT | WINEVENT_SKIPOWNPROCESS`, so they run on the UI thread through its message loop. `OnWinEvent` filters to `OBJID_WINDOW` and `CHILDID_SELF`. Every callback goes through `Guard`, which catches all exceptions, so none reaches an `UnmanagedCallersOnly` boundary. The code before `Guard` in `OnKeyboard` only reads the hook struct.
- **Re-entrancy.** `IsResizing` waits up to 50 ms in `SendMessageTimeoutW` without `SMTO_BLOCK`, so a keyboard hook callback can run while it waits. `HandleKey` only queues work with `Defer`, so it doesn't disturb the drag in progress. A move/size start that arrives before the previous end replaces that drag, and a leftover drag timer stops itself.
- **Stale handles.** `OnWindowDestroyed` forgets a window's zones and cancels its drag. A deferred Win+arrow move runs on the next timer tick with the `HWND` it captured. If the window closed in between, the Win32 calls fail harmlessly. Handle reuse within one tick is possible in theory and would move a different window of the same user.
- **`SendInput`.** `SuppressStartMenu` sends a key-down and key-up of `VK_UNASSIGNED` (`0xE8`) and nothing else. `SendInput` can't target a window or another session, and UIPI drops it when the foreground window is elevated.
- **Process queries.** `OpenProcess` asks only for `PROCESS_QUERY_LIMITED_INFORMATION`, and `CloseHandle` runs in a `finally`. See FZC-12 for the buffer.
- **Privilege.** The manifest sets `asInvoker` and `uiAccess="false"`. The source has no `runas` verb, token manipulation, service, or scheduled task. UIPI makes `SetWindowPlacement` fail on elevated windows, and `MoveToZone` then returns `false` and the app records no zone.

### Interop and memory safety

- Every `LibraryImport` signature matches its Win32 declaration, and every callback is a function pointer to an `UnmanagedCallersOnly` method. `[assembly: DisableRuntimeMarshalling]` is set, and no delegate reaches native code, as `AGENTS.md` requires.
- The struct layouts match the Windows SDK on x64: `KBDLLHOOKSTRUCT` (24 bytes), `INPUT` (40), `MONITORINFOEXW` (104), `DISPLAY_DEVICEW` (840), `WINDOWPLACEMENT` (44), `BITMAPINFOHEADER` (40), and `WNDCLASSEXW` (80). Every `cbSize`, `cb`, and `length` field is set from `sizeof`.
- `CharBuffers.ToTerminatedString` stops at the buffer's end when there's no terminator.
- `Monitors.ReadSerialNumber` sizes the EDID buffer from a first `RegGetValueW` call, and a value that grows between calls makes the second call fail cleanly.
- `OverlayRenderer` checks the buffer length, clips every fill and stroke to the work area, and returns its `ArrayPool` buffer in a `finally`. Extreme zone rectangles from hostile configs rendered without error.

### Untrusted input

A throwaway harness ran against `FancyZonesClone.Core` and exercised loading, zone calculation, rendering, hit-testing, and exclusion matching:

| Input                                                                                                                        | Result                                       |
| ---------------------------------------------------------------------------------------------------------------------------- | -------------------------------------------- |
| `config.json` nested 100,000 levels deep                                                                                     | Rejected cleanly by the 64-level depth limit |
| `int.MaxValue` and `int.MinValue` spacing, sensitivity radius, grid percentages, and canvas sizes                            | No exception; zones clip to the work area    |
| Missing and `null` sections and entries                                                                                      | See FZC-04                                   |
| Huge zone counts                                                                                                             | See FZC-05                                   |
| 200,000 random and truncated EDID blocks, 0 to 300 bytes, half with a valid header and a serial descriptor                   | No exceptions                                |
| 1,839 mutated sets of this machine's five PowerToys FancyZones files in 20 seconds, each imported, saved, reloaded, and used | No exceptions, and no run over 500 ms        |

`PowerToysImporter` survives malformed files because every reader checks the JSON type first, and `Parse` turns a bad file into a warning. `ConfigStore.Save` writes a temporary file and moves it into place. The config folder is fixed under `%APPDATA%`, so redirecting it needs the user's rights.

### Data and privacy

- **Reads:** keystrokes, as [Input monitoring](#input-monitoring-and-control) describes; window styles, positions, and owners; the process image path of the window being moved or focused; monitor device and instance IDs from `EnumDisplayDevicesW`; the EDID under `HKLM\SYSTEM\CurrentControlSet\Enum\DISPLAY\...\Device Parameters`; `config.json`; and the PowerToys FancyZones files.
- **Writes:** `config.json`, with the `.tmp` and `.invalid` files above.
- **Network:** the source uses no networking API. A framework-dependent Release build of `9b02c0a` had run on this machine for about an hour with no TCP or UDP endpoint, and loaded no `ws2_32.dll`, `winhttp.dll`, or `System.Net.*` assembly. `Avalonia.Remote.Protocol` and `Avalonia.DesignerSupport` contain networking code for the IDE previewer, which only the designer host starts. To confirm the shipped executable, run `dumpbin /imports FancyZonesClone.exe` on the first release and check it doesn't import `ws2_32.dll` or `winhttp.dll`. This audit couldn't build Native AOT locally.

### Dependencies

| Package                                                 | Version            | Role                                                |
| ------------------------------------------------------- | ------------------ | --------------------------------------------------- |
| Avalonia, Avalonia.Desktop, Avalonia.Themes.Fluent      | 12.1.5             | UI; compiled into the executable                    |
| SkiaSharp, with `libSkiaSharp.dll`                      | 3.119.4            | Rendering; native DLL ships                         |
| HarfBuzzSharp, with `libHarfBuzzSharp.dll`              | 8.3.1.3            | Text shaping; native DLL ships                      |
| Avalonia.Angle.Windows.Natives, with `av_libglesv2.dll` | 2.1.27548.20260419 | OpenGL ES on Direct3D; native DLL ships             |
| MicroCom.Runtime                                        | 0.11.6             | COM interop for Avalonia; compiled in               |
| Tmds.DBus.Protocol                                      | 0.94.1             | Linux D-Bus; referenced, unused on Windows          |
| Avalonia.BuildServices                                  | 11.3.2             | Build-time telemetry (FZC-09)                       |
| MinVer                                                  | 8.0.0              | Build-time versioning                               |
| Meziantou.Analyzer                                      | 3.0.320            | Build-time analyzer                                 |
| Microsoft.DotNet.ILCompiler                             | 10.0.14            | Native AOT compiler                                 |
| TUnit                                                   | 1.79.0             | Tests only; brings Microsoft.Testing.Platform 2.5.2 |

`dotnet list package --vulnerable --include-transitive` reports no known vulnerabilities for any project, against nuget.org on 2026-11-04. `TreatWarningsAsErrors` turns NuGet Audit's NU1901 to NU1904 into restore failures, so a new advisory fails CI. Every licence in the shipped graph is MIT or BSD-3-Clause.

### CI and release

- Every action in `ci.yml` and `release.yml` is pinned to a full commit SHA, and Renovate keeps them pinned.
- `ci.yml` sets `contents: read` at the top level. A `pull_request` run from a fork gets a read-only token and no secrets, and neither workflow uses a cache, so a fork can't poison one. `pull_request_target` isn't used.
- The unreleased CI build keeps the default artefact retention, and the release artefact keeps 90 days. The GitHub release itself is the durable copy, so both are fine.

### Deployment on managed machines

Observed on this machine, a domain-joined Windows 11 Enterprise 10.0.26200 workstation:

- The app ran unsigned under the installed endpoint agent, whose module was loaded into the process, and Microsoft Defender recorded no detection for it.
- The effective AppLocker policy has no rules, and user-mode code integrity isn't enforced, so this machine can't show how AppLocker or WDAC treat the app.

Still to test on a machine with the organisation's production policies:

1. Run the release executable under the organisation's EDR, and record any alert on the keyboard hook. Use [Input monitoring](#input-monitoring-and-control) as the justification for an exclusion.
2. Run it under AppLocker and WDAC, unsigned and then signed (after FZC-03).
3. Sign in two users, run the app in both sessions, and check each snaps only its own windows. Repeat over Remote Desktop.
4. Drag an elevated window, such as an elevated terminal, and check the app leaves it alone.

**Lifecycle:** install by unzipping, autostart with a shortcut in `shell:startup`, upgrade by replacing the executable, and uninstall by deleting it and `%APPDATA%\FancyZonesClone`. There's no automatic update, so plan how users get fixes, for example by packaging releases for your software distribution tool.

## Follow-up issues

Each finding that needs a change gets its own issue:

| Finding | Issue                                                         |
| ------- | ------------------------------------------------------------- |
| FZC-01  | `ci: keep the release token away from build steps`            |
| FZC-02  | `ci: release only from protected tags on main`                |
| FZC-03  | `ci: sign, attest, and checksum releases`                     |
| FZC-04  | `fix(config): keep collections non-null when loading`         |
| FZC-05  | `fix(layouts): cap zone counts`                               |
| FZC-06  | `fix(engine): keep keyboard hook work within its timeout`     |
| FZC-07  | `fix(app): handle exceptions in tray menu handlers`           |
| FZC-08  | `chore: lock restore, map package sources, and ship an SBOM`  |
| FZC-09  | `ci: opt out of build and test telemetry`                     |
| FZC-10  | `docs: add SECURITY.md and turn on GitHub security features`  |
| FZC-11  | `ci: add CodeQL code scanning`                                |
| FZC-12  | `fix(windows): read process paths longer than 259 characters` |
| FZC-13  | `fix(overlay): handle GDI surface creation failure`           |
| FZC-14  | `docs: complete third-party notices`                          |
