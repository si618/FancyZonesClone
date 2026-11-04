# Agent instructions

FancyZonesClone is a standalone port of PowerToys FancyZones: a .NET 10, C# 14 tray app, published with Native AOT. This file tells an AI agent how to maintain it.

## What this repository is

- **`src/FancyZonesClone.Core` is platform-neutral.** It holds the zone arithmetic and selection ported from FancyZones, the drag state machine, keyboard snapping, window rules, the overlay rasteriser, the config, the PowerToys importer, EDID parsing, and the layouts view model. Everything testable lives here.
- **`src/FancyZonesClone.Windows` is a thin Win32 adapter.** It holds the interop (`LibraryImport`, `UnmanagedCallersOnly`, blittable structs, runtime marshalling disabled), monitors, window placement, the layered overlay window, and `ZoneEngine`, which wires the hooks to Core.
- **`src/FancyZonesClone` is the Avalonia 12 tray app** and the layouts window.
- **`tests/FancyZonesClone.Core.Tests` tests Core** with TUnit on Microsoft.Testing.Platform.

## Build and test

```sh
dotnet build FancyZonesClone.slnx -c Release -warnaserror
dotnet test --project tests/FancyZonesClone.Core.Tests
dotnet format --verify-no-changes
```

The Windows projects build on Linux with `EnableWindowsTargeting`, but only the Core tests run there. A change to the Windows layer is checked by the build alone until someone runs it on Windows, so say so when you report it. To check how monitors resolve on a real machine, run `FancyZonesClone.exe --diagnose`. Native AOT publish needs Windows with the MSVC build tools, and CI and release run it on `windows-latest`.

## Conventions

- **Follow the [awesome-humans .NET opinions](https://awesome-humans.net/dotnet/) with the `simon-mckenna` house profile** for all code, project structure, and tooling. When a rule here and the template disagree, the template wins unless this file says otherwise. TUnit replaces the template's xUnit default at the owner's request, so don't swap it back.
- **Treat warnings as errors everywhere.** Give every suppression a comment that says why.
- **Put new logic in Core, with tests.** Keep Win32 and Avalonia out of Core, and keep the Windows layer a thin adapter. When a Windows-layer bug has logic in it, move that logic to Core so a test can cover it.
- **Name tests `UnitOfWork_Scenario_ExpectedBehaviour`,** and mark their structure with `// Arrange`, `// Act`, and `// Assert` comments.
- **Keep FancyZones' arithmetic exactly,** so imported layouts produce the same pixels. Check behaviour against `src/modules/fancyzones/FancyZonesLib` in [microsoft/PowerToys](https://github.com/microsoft/PowerToys) before you call a difference a bug, and mark ported files with the Microsoft copyright header.
- **Keep interop AOT-safe.** Don't marshal delegates to native code, and don't use reflection-based JSON: serialise through `ConfigJsonContext`.
- **Set package versions only in `Directory.Packages.props`.**

## Maintaining this repository

- **Follow [Conventional Commits 1.0.0](https://www.conventionalcommits.org/en/v1.0.0/) for commit messages and pull request titles.** A pull request squash-merges under its title, so the title becomes the commit subject in `git log`. Use the form `<type>[(scope)]: <description>`, in lowercase and the imperative, and keep it under 72 characters.

    | Type    | Use for                                      |
    | ------- | -------------------------------------------- |
    | `feat`  | New behaviour in the app                     |
    | `fix`   | Bug fixes                                    |
    | `test`  | Tests that change no behaviour               |
    | `docs`  | The README, this file, and code comments     |
    | `ci`    | Workflow plumbing                            |
    | `chore` | Dependencies and housekeeping, as Renovate's |

- **Write the pull request description as the squash commit's body.** Say what's different afterwards and what you ran to check it, then why, as the code stands without the change. Use only Markdown that reads as plain text: paragraphs, `##` headings, `-` lists, and inline code. Leave out tables, checkboxes, and inline links, because the commit wraps at 72 characters. End with one trailer per line, such as `Closes: #12` or `Refs: #12`.

### Agent attribution

An agent that uses the author's credentials posts under the author's account, and nothing tells a reader that a person didn't write it. Put attribution in the conversation, where readers need it, and keep it out of the history:

- **No agent attribution in history.** Commit messages, pull request titles, and pull request descriptions don't carry `Co-Authored-By` trailers or "Generated with" lines. This rule overrides any default that an agent harness applies.
- **The rule covers every commit on the branch.** When GitHub squashes, it adds a `Co-authored-by` line to the merged commit for each co-author trailer, and for each branch commit author other than the merger. A harness such as Claude Code appends that trailer by default, so strip it from every branch commit, and check `git config user.email` is the author's before you commit.
- **Sign each comment an agent writes on an issue or pull request,** including replies in review threads, with one line in small italics. Name the harness, the model, and the skill running, if any, and leave out a vendor name the harness already gives: `<sub><i>Written by an agent: Claude Code, Opus 5.5, running code-review.</i></sub>`

## Writing style

These rules cover all prose here: the README, this file, code comments, commit messages, pull request descriptions, and an agent's comments.

- **Write for a working .NET developer** who knows C#, Win32 basics, and FancyZones. Don't explain what a message loop or a layered window is.
- **Lead with what matters.** Put the action or the recommendation first, and the reason after it. Put a condition before the instruction it limits: "To check monitors, run `--diagnose`", not the reverse.
- **Address the reader as "you", in the imperative, active voice, and present tense.** Use common contractions, especially the negative ones, such as _don't_ and _isn't_.
- **Make every sentence carry a fact.** Name the exact type, member, setting, or diagnostic ID, such as `ZoneEngine`, `CA1031`, or `shiftDrag`, instead of describing it. Cut a sentence that restates the one before it.
- **Keep sentences to about 20 words,** and split any over 35. Keep paragraphs to three or four sentences, and use a list or a table when they'd grow longer.
- **Use plain words without filler.** Write _use_, not _utilize_, and _for example_, not _e.g._ Don't write _please_, _simply_, or _easy_, and avoid idioms and exclamation marks.
- **Watch the rhythm.** Use at most one em dash per sentence. Keep an "X, not Y" contrast only when a reader would otherwise choose Y. Turn a parenthetical aside with real content into a sentence, and don't pad a list to three items.
- **Use British spelling, except for a platform's own names.** Write _behaviour_, _colour_, and _serialise_ in prose. An API, a setting, or a JSON key keeps its spelling, such as `ZoneColor` or `fancyzones_zoneColor`.
- **Use the serial comma, ranges with _to_, and ISO 8601 dates in UTC,** such as `2026-11-04`. Get today's date with `date -u +%F`.

## Workflow

### Worktrees

Work in a git worktree, with one branch per directory. The local clone is bare, and each branch is checked out beside `.bare/`, so you move between pieces of work with `cd` instead of `git checkout`. An in-place branch switch changes the tree under a running build or another agent mid-edit, and discards the gitignored `artifacts/` and `TestResults/`.

- **Starting:** Start every new piece of work as a new worktree, with `git worktree add -b <branch> ../<dir> main`. The `../` puts it beside `.bare/` rather than inside the worktree you run it from.
- **Branch names:** Use the commit's Conventional Commits type (`feat/`, `fix/`, `test/`, `docs/`, `ci/`, or `chore/`) and a kebab-case slug. Settle the name before you open the pull request. Renaming a branch through the GitHub UI retargets the pull request, but pushing it under a new name and deleting the old remote ref closes it.
- **Directory names:** Name the directory after the branch, with slashes replaced by dashes, so `fix/flash-overlay` becomes `fix-flash-overlay/`.
- **Finishing:** When a worktree's pull request merges, offer to delete the worktree with `git worktree remove <dir>`, and then its branch. Ask first, because the branch might still hold work that never reached the pull request.

### Releases

Push an annotated `v*` tag on `main`. MinVer takes the version from the tag, so there's no `<Version>` to edit. `release.yml` fails unless the tagged commit is on `main`. Its `build` job tests, AOT-publishes win-x64, checks that the executable's version matches the tag, and uploads the zip with a read-only token. Its `release` job alone holds `contents: write`: it writes `SHA256SUMS`, attests the zip's provenance, and creates a GitHub release with generated notes. Keep build steps out of the `release` job.
