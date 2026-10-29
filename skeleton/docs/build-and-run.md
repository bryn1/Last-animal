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
| `build/LastAnimal.exe` | Windows x86_64 release export of the full game (main scene `res://main.tscn`), embedded pck (`binary_format/embed_pck=true`). |
| `build/data_LastAnimalPreflight_windows_x86_64/` | The .NET assemblies the .exe loads at startup (`coreclr.dll`, `hostfxr.dll`, `GodotSharp.dll`, `LastAnimalPreflight.dll`, …). Must sit beside the .exe. |
| `build/preflight.exe` | The M00 hardware/render preflight check (preset `Windows-preflight`), produced by `ci/export_check.sh Windows-preflight build/preflight.exe`; the release leg ships it inside the published Windows zip (v0.4.1 asset layout). |
| `build/LastAnimal-windows-x86_64.zip` | Distributable zip containing the .exe AND that data dir (the release-published zip additionally carries `preflight.exe`). |
| `build/LastAnimal.x86_64` | Linux x86_64 release export of the full game (main scene `res://main.tscn`), embedded pck. |
| `build/data_LastAnimalPreflight_linuxbsd_x86_64/` | The .NET assemblies the Linux binary loads at startup. Must sit beside the binary. |
| `build/LastAnimal-linux-x86_64.zip` | Distributable zip containing the Linux binary AND that data dir. |

Sizes and hashes are per-export by design (the binary is rebuilt every time);
the published per-asset numbers live on the GitHub release asset list and in
each release's evidence dir under `.audits/` — not in this doc (de-staled at
v0.5.0, MC 11399; the old ~-MB figures here were the M00-era build).

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
   godot-mono runtimeconfig markers. Zero export ERROR lines is part of the
   gate. The gate's clean-export wipe is scoped to the engine's OWN outputs —
   the .exe and `data_*` dir it re-produces, plus the `.godot` cache — and
   NEVER touches a packaged zip (`tools/package_artifact.sh` owns the zip
   lifecycle: rm + fresh write at every packaging), so the gate — including
   the `Windows-preflight` leg — may run at any point of the release sequence
   without destroying the artifact it certifies (MC 11654; at the v0.6.0 cut
   its clean rm'd the already-packaged Windows zip).
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
   - **wine present but broken** (historically the vm105 build host, Wine
     9.0 — that host is retired 2026-10-14; see "Known issue" below) → the smoke
     launches, the exe crashes at startup, and the script exits non-zero.
     That red exit is correct behaviour, not a build defect; the exe itself
     still passes the step-1 PE + embedded-assembly checks.
   - The CURRENT build host (vm350) has **no wine installed** — the "wine
     absent" branch above is the one that executes on every release leg.

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
  (installed to `~/.local/share/godot/export_templates/`). The engine binary
  is **not in git** (MC 10103, clean-clone truth): the `engine/Godot_v4.7.2-*`
  directories are gitignored, and a clean clone carries only the pin and the
  checksums — `engine/PIN.txt`, `engine/SHA512-SUMS.txt` and
  `engine/local.sha512`. Fetch the pinned official zip, verify its SHA512
  against `engine/local.sha512`, and extract it beside the clone at
  `engine/Godot_v4.7.2-stable_mono_linux_x86_64/` (the location
  `ci/toolchain.sh` looks for), or just export `GODOT=/path/to/godot`.
- **.NET SDK 8.x** (the pinned line; the build host has 8.0.132 — restamped
  by `dotnet --version` at the v0.5.0 release leg, MC 11399).

Steps:

```bash
cd /srv/workspace/last-animal/skeleton
source ci/toolchain.sh          # pins dotnet + GODOT on PATH
dotnet build LastAnimalPreflight.csproj   # must exit 0, 0 errors
bash ci/export_check.sh         # full-game export gate (preset "Windows")
bash tools/export_windows.sh    # gate + zip + (optional) wine smoke
```

`ci/toolchain.sh` resolves `GODOT` to the pinned binary at the `engine/`
location above **if you restored it there** — it warns on stderr when that
binary is missing, because a clean clone never contains it. Either way,
exporting `GODOT=/path/to/godot` before sourcing overrides the lookup.

## Building on Windows from source

Requirements: the **.NET 8 SDK** and the **Godot 4.7.2-stable mono editor**
(Windows build). Then:

1. Open `skeleton/project.godot` in the Godot mono editor.
2. Build the C# solution (the editor does this on import; or run
   `dotnet build LastAnimalPreflight.csproj` from `skeleton/`).
3. Press **F5** to run.

`skeleton/NuGet.Config` (and `animation_pipeline/NuGet.Config`) is
host-portable: it lists **nuget.org only** — no local engine paths. The
Godot.NET.Sdk version cannot float because the csproj pins it exactly
(`Godot.NET.Sdk/4.7.2`, no wildcard).

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
runtime ships in the data dir — UNVERIFIED on a clean Windows machine: the
owner's playtesting of the published zips (since v0.4.0) ran on his own
Windows machines, not a clean test host, and the current build host has no
wine to test with (see "Known issue" below). If the game fails to start
on a player machine, check first that the data dir is beside the .exe, then
try installing the .NET 8 desktop runtime.

## Known issue: Wine 9.0 startup crash

> History (de-staled at v0.5.0, MC 11399): this documents the **vm105** build
> host, retired/destroyed 2026-10-14. The current build host (vm350) has no
> wine installed at all, so `tools/export_windows.sh` executes its
> "wine UNAVAILABLE ... NOT run (not faked)" branch on every release leg.

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
