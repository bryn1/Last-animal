# Last Animal — docs playbook (M14)

Last Animal is a desktop 3D ARPG built with Godot 4.7.2 (mono/C#). It ships as
native Windows and Linux x86_64 binaries; there is **no web host and no web publishing** for this
project — the web-host DoD item is deliberately SKIPPED and that skip is
explicit, per PHASE0.md. The deliverable is a Windows/Linux zip you run locally.

## What is in this directory

| File | Subject |
|---|---|
| [player-guide.md](player-guide.md) | How to play: controls, core loop, systems. |
| [build-and-run.md](build-and-run.md) | How the shipped artifact is built, and how to build it from source. |
| [install.md](install.md) | How a player installs and first-runs the game (Windows and Linux). |
| [release-notes/](release-notes/) | Per-version release notes, committed at the tag (v0.5.0+; earlier versions' notes lived only on the GitHub release page). |

## The delivered artifact (v0.6.0 era — what the GitHub release carries)

The published assets of each release are exactly:

- `LastAnimal-linux-x86_64.zip` — Linux x86_64 distributable: `LastAnimal.x86_64`
  (full-game `res://main.tscn` release export, pck embedded) +
  `data_LastAnimalPreflight_linuxbsd_x86_64/` (the .NET assemblies the binary
  loads at startup — must stay beside it). Produced by `bash tools/export_linux.sh`
  from `skeleton/`; packaged smoke proof is the non-blank Xvfb render bar
  `LAUNCH_LINUX_SMOKE: RESULT=PASS`.
- `LastAnimal-windows-x86_64.zip` — Windows x86_64 distributable: `LastAnimal.exe`
  (full-game release export) + `data_LastAnimalPreflight_windows_x86_64/` +
  `preflight.exe` (the standalone hardware/render preflight check, v0.4.1 asset
  layout — run it first if the game itself will not open) + the two rig launchers
  and the one-page note `LAST-ANIMAL-CLASSIC.bat` / `LAST-ANIMAL-ISO.bat` /
  `OWNER-PLAYTEST.txt` at the zip top level (the MC 11129 owner-playtest extras,
  VERIFIED against the published v0.5.0 asset listing at the v0.6.0 sweep —
  the previous README sentence omitted them; the layout is unchanged since,
  `tools/export_windows.sh` packages them at every release leg). Produced by
  `ci/export_check.sh` (both Windows presets) + `bash tools/export_windows.sh`,
  then the release leg folds `build/preflight.exe` into the published zip.

Exact byte sizes and sha256 per asset are recorded on each release's asset list
(GitHub API) and in that release's evidence dir under `.audits/` — per doctrine
the hashes live in the evidence, not in the docs. The binaries are rebuilt on
every export, so their hashes change by design.

Both are produced from `skeleton/` (the scripts live in `skeleton/tools/`); see
[build-and-run.md](build-and-run.md). (Era re-stamped v0.5.0 → v0.6.0 at the
v0.6.0 release, MC 11587: the asset names and layout above are UNCHANGED from
v0.5.0 — measured against `export_presets.cfg` + the packaging scripts at the
tag — the stamp moved because the release era moved.)

> **History note (replaces the old superseded sizes block, MC 10059 → de-staled
> at the v0.5.0 release, MC 11399):** the per-build byte counts and hashes this
> section carried were the 2026-09-25 M00-chain build and stopped describing
> the export after Increments 1-2. The live truth is the release asset list.

## Known limitation (stated, not hidden)

The packaged Windows binary gets no automated render smoke on the Linux build
host: **wine is not installed** on the current build host (vm350), and it was
broken on the previous build host (vm105, Wine 9.0 — see the history note
below). `tools/export_windows.sh` states `wine UNAVAILABLE ... NOT run (not
faked)` on stdout and relies on the executed evidence that IS run: the export
gate's PE-magic + embedded-C#-assembly checks (`ci/export_check.sh`). The
runtime evidence for Windows is owner playtesting of the published zips
(since v0.4.0), not a build-host launch. It never fakes a launch.

> **History (v0.5.0 de-stale, MC 11399):** the old build host vm105 (destroyed
> 2026-10-14) had a broken Wine 9.0 under which `LastAnimal.exe` crashed at
> startup (null-pointer read before any log line; identical with the OpenGL
> compatibility renderer and `--headless`; likeliest the bundled .NET 8 host).
> The current build host has no wine at all, so the script takes its
> "wine absent" branch. Workaround either way: the native Linux build or a
> real Windows machine. Full write-up:
> [build-and-run.md](build-and-run.md#known-issue-wine-90-startup-crash).
