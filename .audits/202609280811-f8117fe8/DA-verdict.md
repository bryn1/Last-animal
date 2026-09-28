# DA verdict — NuGet restore portability fix (MC 1405 follow-up)

NOTE (honesty): executed INLINE by the parent session, not by a spawned
`devils-advocate` child — the harness rejected the spawn (`subagent depth 2
exceeds maxDepth 1`). Adversarial stance kept: every claim below was probed
against the repo, not taken from the fix report.

## Attack 1 — does removing the local feed actually float the SDK?

Claimed guard: the exact pin. Probed: both csproj files carry
`Sdk="Godot.NET.Sdk/4.7.2"` (no wildcard) — VERIFIED by grep and by the pytest
suite. nuget.org serves exactly 4.7.2 for that request; a wildcard would float,
an exact pin cannot. The old local feed's real protection was version match
with the vendored engine; that risk is unchanged by WHERE the identical 4.7.2
package comes from. Attack fails.

## Attack 2 — does the fix actually fix the owner's Windows failure?

NU1301 was caused by a nonexistent local source. After the fix there is NO
local source (`dotnet nuget list source` → nuget.org only, VERIFIED this
session), so NU1301 for that path is structurally impossible. Not proven by a
Windows execution (no Windows host in the fleet) — stated as a limit, not
hidden. Attack fails with a documented evidence gap.

## Attack 3 — hidden coupling: does anything else reference the removed feed?

Probed: `grep -rn "godot-pinned-local\|gunilla/engine\|GodotSharp/Tools/nupkgs"`
over the repo outside `.git/` and `engine/`: remaining hits are only history
comments in the two rewritten NuGet.Config files and audit records (which are
historical by design). No build script, csproj or doc consumes the removed
NuGet path. Attack fails — BUT the same grep surfaced a REAL pre-existing
defect outside this task's scope:

- **P2 finding (pre-existing, NOT introduced by this fix, NOT fixed here per
  scope control):** `animation_pipeline/tools/retarget_bake.sh:21` hardcodes
  `ENGINE="/srv/workspace/svarkor-last-animal-phase2/gunilla/engine/.../Godot_v4.7.2-stable_mono_linux.x86_64"`,
  and that path is MISSING on this host (`test -e` → MISSING). The bake script
  fails at its own `[ -x "$ENGINE" ]` guard regardless of NuGet. This is the
  same retired-seat-path disease the NuGet.Configs had, in a different file.
  Needs its own task (likely: resolve the engine via `engine/PIN.txt` /
  `ci/toolchain.sh` instead of a hardcoded seat path). Flagged to the parent.

## Attack 4 — does animation_pipeline still work?

`dotnet build animation_pipeline/LastAnimalM09Bake.csproj` → Build succeeded,
0 errors, exit 0, AFTER the config change. Its SDK pin is exact, so nuget.org
resolution works. Attack fails.

## Attack 5 — regression risk in the CI gates?

`ci/export_check.sh` exit 0 (PE magic OK, 111300104 bytes) and
`ci/runtime_integration_test.sh` exit 0 (full gate PASS) after the change —
both re-run this session. Attack fails.

## Attack 6 — simpler alternative rejected?

Keeping the local feed but making the path conditional (e.g. condition on OS)
would keep two mechanisms and still break hosts without the engine payload
(the owner's zip has none). Removal + exact pin is the smaller, single-
mechanism change. Rejected alternative is worse. Attack fails.

## Findings

- P3 (doc path): the task brief said `docs/build-and-run.md`; the real file is
  `skeleton/docs/build-and-run.md`. The subsection was placed in the real file;
  recorded in the evidence file. No action beyond the record.
- No P0/P1 findings.

# JUDGED: b0b70e76d5c74470970e2fc1c5cc3668971ef1cc78ba050908e1860fd601c022
# VERDICT: SHIP
