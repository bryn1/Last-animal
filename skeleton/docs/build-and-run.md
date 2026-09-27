# Last Animal — build and run (M14)

This document describes how the delivered Windows artifact is produced and how
to rebuild it from source. For playing the game, see [install.md](install.md)
and [player-guide.md](player-guide.md).

**About the assembly name:** the C# assembly and csproj keep their historical
preflight-era name `LastAnimalPreflight` (from the project's M00 preflight
skeleton, MC 839.1). The name is NOT renamed to match the full game — renaming
would break the export pipeline and the csproj references — so
`LastAnimalPreflight.dll` in the artifacts below is the real game assembly,
not a leftover preflight build.

## The delivered artifact

| File | What it is |
|---|---|
| `build/LastAnimal.exe` | Windows x86_64 release export of the full game (main scene `res://main.tscn`), embedded pck (`binary_format/embed_pck=true`), ~109 MB. |
| `build/data_LastAnimalPreflight_windows_x86_64/` | The .NET assemblies the .exe loads at startup (`coreclr.dll`, `hostfxr.dll`, `GodotSharp.dll`, `LastAnimalPreflight.dll`, …), ~80 MB. Must sit beside the .exe. |
| `build/LastAnimal-windows-x86_64.zip` | Distributable zip containing the .exe AND that data dir, ~73 MB. |
| `build/LastAnimal.x86_64` | Linux x86_64 release export of the full game (main scene `res://main.tscn`), embedded pck, ~76 MB. |
| `build/data_LastAnimalPreflight_linuxbsd_x86_64/` | The .NET assemblies the Linux binary loads at startup. Must sit beside the binary. |
| `build/LastAnimal-linux-x86_64.zip` | Distributable zip containing the Linux binary AND that data dir, ~63 MB. |

All three are produced by one command from `skeleton/` (the project root —
the script lives in `skeleton/tools/`, not at the repo root):

```bash
bash tools/export_windows.sh
```

The script runs three steps and fails loudly on any of them:

1. **Export gate** — `ci/export_check.sh Windows . build/LastAnimal.exe`:
   rebuilds the C# solution through the engine (`--build-solutions`), runs
   `godot --headless --export-release`, and proves the output is a real PE
   (`MZ` magic) that carries the C# assembly, the GodotSharp runtime and the
   godot-mono runtimeconfig markers. Zero export ERROR lines is part of the gate.
2. **Packaging** — zips the .exe AND the `data_LastAnimalPreflight_windows_x86_64/`
   assemblies dir into `build/LastAnimal-windows-x86_64.zip` (python3
   `zipfile`; the `zip` CLI is not installed on the build host). The step
   fails loudly if the data dir is missing or the zip lacks the game
   assembly — an exe-only zip cannot launch (MC 1347).
3. **Smoke** — if `wine` is installed, the packaged binary is headless-launched
   (`wine build/LastAnimal.exe --headless --quit-after 120`, must exit 0).
   The smoke asserts only exit 0 — the C16 contract's ">0 non-blank frames"
   leg needs a wine-capable host and is not asserted by this script. Wine
   states the script distinguishes, all stated on stdout and never faked:
   - **wine absent** → prints `wine UNAVAILABLE ... NOT run (not faked)` and
     exits 0, with the step-1 checks as the executed evidence.
   - **wine present and working** → the smoke runs and must exit 0.
   - **wine present but broken** (the current build host vm105, Wine 9.0:
     `wine32` missing, no X driver — see "Known issue" below) → the smoke
     launches, the exe crashes at startup, and the script exits non-zero.
     That red exit is correct behaviour, not a build defect; the exe itself
     still passes the step-1 PE + embedded-assembly checks.

## Linux export

```bash
bash tools/export_linux.sh
```

Same three-step shape as the Windows script: the `ci/export_check.sh` gate
(preset `Linux`, ELF magic instead of PE), packaging the binary plus the
`data_LastAnimalPreflight_linuxbsd_x86_64/` assemblies dir into
`build/LastAnimal-linux-x86_64.zip`, and a NATIVE launch smoke — the packaged
binary is launched under Xvfb via `tools/launch_linux_smoke.sh` (reusing the
host's `graphical-test-helper.sh` capture mechanism) and must render a
non-blank framebuffer (`RESULT=PASS`). Unlike the Windows smoke, this leg is
asserted, not optional. **Known limit:** the smoke asserts launch and a
non-blank framebuffer only — no gameplay progression is asserted (it does not
check saves, kills, quest or economy state), so a PASS proves the binary
starts and renders, nothing further.

## The C12 animation pipeline: what actually exists

The spec's C12 names a literal `RetargetPipeline` module and a root-level
`tools/retarget_bake.sh`; neither exists under those names. The substance is
carried by two real artifacts instead: `animation_pipeline/` (a standalone
Godot mono project whose `tools/retarget_bake.sh` does the retarget/bake) and
its output `skeleton/resources/animation/walkBaked.tres`, which the game loads
at runtime. Docs and coverage claims therefore treat "C12" as this
bake-pipeline-plus-resource pair, not as a module with that literal name.

The Linux preset uses `export_filter="all_resources"`: the game loads audio,
terrain and textures through dynamic `GD.Load(string)` calls, which the
`scenes` filter's dependency scan does not see — a `scenes` export ships a pck
without them (observed as `No loader found for resource:
res://assets/audio/music_theme.ogg` at startup).

## Building from source (Linux host)

Requirements:

- **Godot 4.7.2-stable mono** plus the matching **mono export templates**
  (installed to `~/.local/share/godot/export_templates/`). The pinned engine
  binary is vendored one level above the repo — see
  `/srv/workspace/last-animal/engine/PIN.txt` and
  `SHA512-SUMS.txt` in the same directory.
- **.NET SDK 8.x** (the pinned line; the build host has 8.0.131).

Steps:

```bash
cd /srv/workspace/last-animal/skeleton
source ci/toolchain.sh          # pins dotnet + GODOT on PATH
dotnet build LastAnimalPreflight.csproj   # must exit 0, 0 errors
bash ci/export_check.sh         # full-game export gate (preset "Windows")
bash tools/export_windows.sh    # gate + zip + (optional) wine smoke
```

`ci/toolchain.sh` resolves `GODOT` to the vendored engine binary; override it
by exporting `GODOT=/path/to/godot` before sourcing.

## Running the game on the build host (Linux)

For a quick local run without exporting:

```bash
source ci/toolchain.sh
"$GODOT" --path .               # opens the game window (graphical session required)
```

Headless CI checks live in `ci/` (`smoke.sh`, `export_check.sh`, the per-module
`*_test.sh` gates); they run under `--headless` and need no display.

## Windows runtime requirements (player machine)

The release export embeds the pck and the Godot runtime in the .exe, but the
C#/.NET assemblies are NOT embedded: the mono template writes them to
`data_LastAnimalPreflight_windows_x86_64/` beside the .exe, and the .exe loads
`hostfxr.dll` and the assemblies from that directory at startup (its own
embedded error string is "Unable to find the .NET assemblies directory.").
The zip ships both, and they must stay side by side after extraction. The
player machine still needs **no Godot editor and no .NET SDK** — the .NET
runtime ships in the data dir — UNVERIFIED on a clean Windows machine: no
Windows host was available this run, and the build host's Wine 9.0 is broken
for this exe (see "Known issue" below). If the game fails to start
on a player machine, check first that the data dir is beside the .exe, then
try installing the .NET 8 desktop runtime.

## Known issue: Wine 9.0 startup crash

`LastAnimal.exe` crashes at startup under Wine 9.0 on the build host (vm105),
while Wine itself works (`cmd.exe` runs). Signature: a null-pointer read
before any game log line is emitted; identical with the OpenGL compatibility
renderer and with `--headless`. Likeliest cause is the bundled .NET 8 host
(`hostfxr`/`coreclr`) under Wine 9.0. Installing Wine 10.x from the apt source
was considered and REJECTED for this run: moderate risk to the shared build
host's package sources for a problem the smoke does not need solved.

Workaround: run the native Linux build (`tools/export_linux.sh`) or the
Windows artifact on a real Windows machine. The Wine crash is a documented
finding, not something the build fixes.
