# S4-DUCK-EVIDENCE — MC 10122 / 10026.15.3 Inc-3 W1 S4 (Music ducking + bus mix)

Knut round-2 rerun of the prior FAIL. Code is the bound build d7c81e0 (MusicManager +70 l, AudioTest mix stage-machine, audio_test.sh +3 named legs) — unchanged here; this tracked file mirrors the gate outputs so the sealed rerun and the judge can READ them from the bound tree. Gate logs: .audits/20261018-1943-s4-duck/ (audio_green.log sha256 53316b6e…, audio_planted_bad_red.log 6cd4e483…, boot_green.log af6d9029…, verify_project.log cdf52f53…; first-run lines verbatim below).

## Mechanism (bound diff facts, all readable in this tree)
- skeleton/autoload/MusicManager.cs: per-bus const levels table BusBaseDb (Sfx 0 dB, Music 0 dB); consts DialogueDuckDb = -8, BossDuckDb = -12, DuckReleaseFrames = 30. SubscribeMix holds the S0 bracket (DialogueShown -> _dialogueActive, DialogueClosed -> release); SetBossStance overrides (boss wins). _Process: integer frame-counter decay (30 f), no Tween/Timer/delta/wall-clock (F4); prints nothing (marker streams untouched, F4-CMP); legs assert AudioServer bus STATE back. DialogueSystem: ZERO edits. Zero save fields (F2). No new assets (volume automation only).
- skeleton/ci/audio_test.sh: named-leg assertions MIX_DUCK_ON_SHOW / MIX_DUCK_OFF_CLOSE / MIX_BOSS_STANCE (bash-native substring, copied leg pattern).
- skeleton/tests/AudioTest.cs: mix stage-machine on the S0 emits; waits 2f engage / 32f release (30f decay + margin); shadow /root/MusicManager QueueFree'd before legs (one instance per Music bus).

## Knut round-1 rows 9/10/11/13 — the named logs, verbatim
row 9 (three mix legs + SfxRouter legs green; audio_test ran):
MIX_DUCK_ON_SHOW: ok Music bus ducks -8 dB while DialogueShown held (bus=-8.000dB duck=-8.000dB)
MIX_DUCK_OFF_CLOSE: ok Music bus decays back to base 30 frames after DialogueClosed (bus=0.000dB duck=0.000dB after 32f)
MIX_BOSS_STANCE: engage bus=-12.000dB duck=-12.000dB (want -12dB) ok
MIX_BOSS_STANCE: ok Music bus ducks -12 dB under boss stance and decays back on release (released=True bus=0.000dB duck=0.000dB after 32f)
M06_AUDIO_TEST: GATE PASS — … S4 duck legs (show/close/boss) green, S5 mapping table (7 pairs + pitch determinism) green   (existing SfxRouter legs are in the same gate)
row 10 (planted-bad RED, never-release: _releaseFramesLeft = DuckReleaseFrames every frame):
MIX_DUCK_OFF_CLOSE: FAIL … (bus=-8.000dB after 32f); MIX_BOSS_STANCE: FAIL (released=False bus=-12.000dB after 32f) -> M06_AUDIO_TEST: GATE FAIL: AudioTest exited 1 (exit 1). Mutation reverted; the row-9 log is the post-revert re-green.
row 11 (boot_test green): BOOT_TEST: GATE PASS — signal received, DI round-trip, boot order = registration order
row 13 (full project verify): same verify_cmd battery (9 dotnet gates + runtime battery, 44 run_mode legs) run 2026-10-18: 15 "GATE PASS" echoes, 0 "GATE FAIL", tail: …BARK stage green… + "VERIFY_ALL_GREEN", process exit 0. Close evidence carries the tail + VERIFY_EXIT=0. (Prior-round NU1301: the restore must run in the SAME session before the gates — done: fetch-style restore then gates, all green.)

## Cited files (absolute paths on VM350, inlined into the knut CLAIM by the dispatcher)
/srv/workspace/last-animal/skeleton/autoload/MusicManager.cs
/srv/workspace/last-animal/skeleton/ci/audio_test.sh
/srv/workspace/last-animal/skeleton/tests/AudioTest.cs
/srv/workspace/last-animal/skeleton/ci/boot_test.sh
/srv/workspace/last-animal/skeleton/autoload/DialogueSystem.cs
/srv/workspace/last-animal/LEDGER.md
