# Last Animal — docs playbook (M14)

Last Animal is a desktop 3D ARPG built with Godot 4.7.2 (mono/C#). It ships as
native Windows and Linux x86_64 binaries; there is **no web host and no web publishing** for this
project — the web-host DoD item is deliberately SKIPPED and that skip is
explicit, per PHASE0.md. The deliverable is a Windows .exe/.zip you run locally.

## What is in this directory

| File | Subject |
|---|---|
| [player-guide.md](player-guide.md) | How to play: controls, core loop, systems. |
| [build-and-run.md](build-and-run.md) | How the shipped artifact is built, and how to build it from source. |
| [install.md](install.md) | How a player installs and first-runs the game (Windows and Linux). |

## The delivered artifact (verified 2026-09-24)

- `build/LastAnimal.exe` — Windows x86_64 release export of the full game
  (`res://main.tscn`), ~109 MB (109,413,144 bytes for the build verified
  2026-09-24). The binary is rebuilt on every export, so its hash changes;
  the hash of the delivered build is recorded in the run's evidence file
  (MC 1344), not here.
- `build/LastAnimal-windows-x86_64.zip` — the distributable package containing
  that .exe and the `data_LastAnimalPreflight_windows_x86_64/` assemblies dir
  beside it, ~73 MB (73,036,740 bytes for the same build repackaged with the
  data dir, MC 1347).

- `build/LastAnimal.x86_64` — Linux x86_64 release export of the full game
  (`res://main.tscn`), ~76 MB, plus `build/LastAnimal-linux-x86_64.zip`
  (~63 MB) containing the binary and the
  `data_LastAnimalPreflight_linuxbsd_x86_64/` assemblies dir. Produced by
  `bash tools/export_linux.sh`; launch-smoked natively under Xvfb
  (RESULT=PASS, non-blank framebuffer, 2026-09-25).

Both are produced by `bash tools/export_windows.sh` / `bash tools/export_linux.sh`
from `skeleton/` (the scripts live in `skeleton/tools/`); see
[build-and-run.md](build-and-run.md).

## Known limitation (stated, not hidden)

The packaged Windows binary has **not** been launch-smoked: the build host has
no wine, and the build script states this explicitly instead of faking a launch.
The executed evidence for the binary is the export gate's PE-magic check plus
its embedded-C#-assembly checks (`ci/export_check.sh`).

## Known issue: Wine 9.0 startup crash

`LastAnimal.exe` crashes at startup under Wine 9.0 on the build host (null-pointer
read before any log line; identical with the OpenGL compatibility renderer and
`--headless`; likeliest the bundled .NET 8 host). Workaround: the native Linux
build or a real Windows machine. Full write-up:
[build-and-run.md](build-and-run.md#known-issue-wine-90-startup-crash).
