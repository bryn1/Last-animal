# DONE.md — NuGet restore portability (MC 1405 follow-up)

ID | claim | STATUS | evidence
D1 | skeleton/NuGet.Config has no local feed, nuget.org only | PASS | `dotnet nuget list source` → "1. nuget.org [Enabled] https://api.nuget.org/v3/index.json"; pytest `test_skeleton_config_has_no_local_feed` + `test_nuget_org_is_the_only_source` green (.tmp/pytest.log)
D2 | animation_pipeline/NuGet.Config has no local feed (retired gunilla path removed) | PASS | pytest `test_animation_pipeline_config_has_no_local_feed` green; grep of XML body: no `gunilla`, no `../engine/`
D3 | Drift guard is the exact SDK pin, not the local feed | PASS | pytest `test_sdk_pin_is_exact_in_both_csproj` green: `Sdk="Godot.NET.Sdk/4.7.2"` no wildcard in both csproj
D4 | Clean-restore proof: obj/bin wiped, restore+build exit 0 with no local feed | PASS | `rm -rf obj bin .godot/mono` (verified absent) → `dotnet restore` "Restored ... (in 608 ms)" exit 0 → `dotnet build` "Build succeeded. 0 Errors" exit 0; config comment states the pin-is-the-guard rationale + Windows reason + dated history note (pytest `test_history_note_marks_removal_with_date_and_reason` green)
D5 | docs "Building on Windows from source" subsection exists | PASS | pytest `test_docs_have_windows_from_source_subsection` green; skeleton/docs/build-and-run.md lines 116-129
D6 | export_check gate passes after the change | PASS | `bash skeleton/ci/export_check.sh` → "EXPORT_CHECK: PASS — build/LastAnimal.exe (111300104 bytes, PE magic OK)", EXPORT_CHECK_EXIT=0 (VERIFY_EXIT=0)
D7 | runtime_integration_test gate passes after the change | PASS | `bash skeleton/ci/runtime_integration_test.sh` → "RUNTIME_INTEGRATION_TEST: GATE PASS", RIT_EXIT=0 (VERIFY_EXIT=0)
D8 | animation_pipeline still builds | PASS | `dotnet build animation_pipeline/LastAnimalM09Bake.csproj` → "Build succeeded. 0 Errors", AP_EXIT=0
D9 | Fix committed, not pushed | PASS | commit 61ed5fc in /srv/workspace/last-animal, author `coder (MC 1405)`; `git log --oneline -1` quoted in evidence.md; no push run
D10 | Evidence file exists, chmod 644 | PASS | /srv/workspace/last-animal/.audits/202609271500-nuget-portable/evidence.md (mode 644 verified at creation)
N1 | NU1301 from a nonexistent local NuGet source is impossible | PASS | `dotnet nuget list source` lists ONLY nuget.org (no filesystem source remains in either config); pytest asserts no non-https source and no `godot-pinned-local` key; red proof: a restored old-style config is rejected by the suite
S1 | surface backend | N/A | no backend service in this change (Godot game repo); the build surface is covered by D4/D8
S2 | surface db | N/A | no data store touched; save/runtime gates (D7) green prove no store regression
S3 | surface frontend | N/A | no UI change; runtime integration gate (D7) covers the running game
S4 | surface api contract | N/A | no API in scope; NuGet.Config is the only "contract" surface and is covered by D1-D3
S5 | surface tests | PASS | pytest suite in out dir, 8 passed, PYTEST_EXIT=0, red-proofed (TEST-verdict.md)
S6 | surface docs | PASS | D5; build-and-run.md 170 lines, subsection present
S7 | surface deploy | N/A | no deploy artifact changed; export gate (D6) proves the export path still works

## Honest limitations

- No Windows host exists in the fleet: Windows portability is proven
  structurally (nuget.org-only config) + Linux clean-restore, not by a Windows
  execution. Recorded as a limit in evidence.md and TEST-verdict.md, not a
  blocked claim (the DoD's clean-restore proof is the named proof and it ran).
- Fan-out was mechanically impossible (this session is a depth-1 child; the
  harness rejected subagent spawns). All phases ran inline; verdict files carry
  the note.
- Pre-existing P2 found, out of scope, NOT fixed: animation_pipeline/tools/
  retarget_bake.sh:21 hardcodes a MISSING retired-seat engine path (see
  DA-verdict.md). Needs its own task.
