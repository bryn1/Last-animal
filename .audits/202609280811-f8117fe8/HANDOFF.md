# HANDOFF — NuGet restore portability (MC 1405 follow-up), 2026-09-28

WHAT: Removed the hardcoded local NuGet feeds from `skeleton/NuGet.Config` and
`animation_pipeline/NuGet.Config` (nuget.org only); drift guard is the exact
`Godot.NET.Sdk/4.7.2` pin in both csproj files. Added "Building on Windows from
source" to `skeleton/docs/build-and-run.md`. Fixes the owner's Windows NU1301 x5
(the Windows mono editor ships no `engine/.../nupkgs` folder; his source zip has
no engine/ payload).

EVIDENCE:
- Fix commit: 61ed5fc in /srv/workspace/last-animal (not pushed).
- Evidence file: /srv/workspace/last-animal/.audits/202609271500-nuget-portable/evidence.md
- Out dir (verdicts, DONE.md, CYCLES.md, pytest suite):
  /home/svarkor/last-animal/.audits/202609280811-f8117fe8/
  - TEST-verdict.md PASS (8 tests green + red proof), DA-verdict.md SHIP,
    ARCH-verdict.md PASS. JUDGED b0b70e76...c022.
- Decisive checks re-run by the orchestrating session itself: clean wipe +
  restore + build exit 0 (nuget.org only), export_check exit 0,
  runtime_integration_test exit 0, animation_pipeline build exit 0.

OPEN ITEMS:
1. P2 (pre-existing, out of scope): animation_pipeline/tools/retarget_bake.sh:21
   hardcodes a MISSING retired-seat engine path
   (/srv/workspace/svarkor-last-animal-phase2/gunilla/engine/...). The bake
   script fails at its own guard. Needs its own task — resolve the engine via
   engine/PIN.txt / ci/toolchain.sh instead of a hardcoded seat path.
2. Windows execution was not possible (no Windows host in the fleet);
   portability is proven structurally + by the Linux clean-restore proof.
3. Fan-out limitation: this session is a depth-1 child; the harness rejected
   all subagent spawns, so the five phases ran inline instead of as profile
   children. Verdict files carry the honesty note.
