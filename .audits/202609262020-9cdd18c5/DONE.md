# DONE.md — run 202609262020-9cdd18c5 (MC 1388 bridge_mvp gate fix)

ID | claim | STATUS | evidence
D1 | bridge_mvp gate exits 0 with the companion following | PASS | 4 runs exit 0 this session (3 consecutive + 1 test-phase re-run); logs .audits/202609262020-9cdd18c5/.tmp/green-{1,2,3}.log and .tmp/test-phase-run.log; e.g. "MARKER 5/6 COMPANION_FOLLOWS — dist 9.247 -> 3.516, moved 5.731"
D2 | root cause classified with git evidence | PASS | .audits/202609261400-bridgefix/evidence.md §2: STALE PROOF SCENARIO; `git log -S "FollowDistance … 3.5f"` → only a7829ed; 65b769d moved the baseline to teleport time (dist0=3.499 ≈ ring)
D3 | gate proven able to go RED | PASS | planted-bad run (assertion inverted to always-fail): RED_EXIT=1, "GATE FAIL: headless proof exited 1"; .tmp/red-run.log; assertion restored (grep count 1)
D4 | dotnet build skeleton/LastAnimalPreflight.csproj exit 0 | PASS | BUILD_EXIT=0; .tmp/build.log
D5 | bash skeleton/ci/runtime_integration_test.sh exit 0 | PASS | RUNTIME_EXIT=0; "RUNTIME_INTEGRATION_TEST: GATE PASS"; .tmp/runtime.log
D6 | at least one commit created this run | PASS | dd1dbcb (fix), 18d8eda (evidence); authored coder (MC 1388); not pushed
N1 | a non-following companion passing the gate silently is impossible | PASS | assertion keeps both arms (moved < 0.2f fails a frozen companion; d1 > dist0-0.25f fails a non-closing one); planted-bad run went RED_EXIT=1; pytest test_planted_bad_red_run_recorded green
S1 | surface backend | PASS | no game-code change needed; CompanionFollowBody movement unchanged since a7829ed (git -S evidence in evidence.md §2); fix confined to the proof scenario skeleton/ci_proofs/BridgeMvpProof.cs (commit dd1dbcb)
S2 | surface db | N/A | no data store touched; save seam (src/save/SaveSystem, schema-guarded GameState) unchanged — D5's save round-trip green
S3 | surface frontend | N/A | no UI change; HUD markers (HUD_BOUND, hearts) green in every gate run
S4 | surface api contract | N/A | no API surface; the gate's six-marker contract is unchanged (all six markers asserted green)
S5 | surface tests | PASS | pytest test_dod_evidence.py: 7 passed, exit 0; bridge gate 4x green; runtime_integration gate green
S6 | surface docs | PASS | docs/ARCHITECTURE.md re-derived against the tree this run (ARCH-verdict.md §1): modules, gates, proofs all match; no drift introduced
S7 | surface deploy | N/A | no deploy surface touched; export gates untouched this run
