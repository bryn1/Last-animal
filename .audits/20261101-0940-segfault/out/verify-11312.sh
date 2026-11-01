#!/usr/bin/env bash
# verify-11312.sh — re-runnable verification for MC 11312 (named bounded retry
# for the engine --build-solutions quit-segfault class). Every leg asserts on
# NAMED lines + exit codes; the mechanism legs run their own RED plants (the
# battery's full-run outcome rides the banked logs/battery-full.log). Ends
# with the literal VERIFY_EXIT=<n> computed from this process's own legs.
# Usage: bash verify-11312.sh   (from the .audits out dir)
set -u
WT=/home/claudecode/Games/la-work/11312
CI="$WT/skeleton/ci"
OUT="$(cd "$(dirname "$0")" && pwd)"
E=/home/claudecode/Games/.tools/godot/Godot_v4.7.2-stable_mono_linux_x86_64/Godot_v4.7.2-stable_mono_linux.x86_64
HOME_ISO="$WT/.tmp/verify-home"; mkdir -p "$HOME_ISO/data" "$HOME_ISO/cache"
FAILS=0
ok()  { echo "VERIFY: PASS $*"; }
bad() { echo "VERIFY: FAIL $*" >&2; FAILS=$((FAILS+1)); }
check() { # check <label> <cmd...>
  local label="$1"; shift
  if "$@" >/dev/null 2>&1; then ok "$label"; else bad "$label"; fi
}

# (1) engine PIN law: the binary is the MONO 4.7.2 at the pinned commit
V="$("$E" --version 2>/dev/null | tail -1)"
case "$V" in (4.7.2.stable.mono.official.ed1daf0bf) ok "engine is mono 4.7.2 @ ed1daf0bf";; (*) bad "engine version: $V";; esac

# (2) syntax: helper + every wired caller parses
for f in build_solutions.sh smoke.sh boot_test.sh runtime_integration_test.sh ui_test.sh empathy_book_test.sh determinism_cmp.sh export_check.sh audio_test.sh bridge_mvp_test.sh main_composition_test.sh; do
  check "bash -n $f" bash -n "$CI/$f"
done

# (3) grep-to-zero: NO shipped gate script calls the engine build step directly anymore
DIRECT=$(grep -lE 'timeout [0-9]+ .*--build-solutions --quit-after' "$CI"/*.sh 2>/dev/null | grep -v build_solutions.sh | wc -l)
[ "$DIRECT" -eq 0 ] && ok "all ci callers route through build_solutions.sh" || bad "$DIRECT direct callers remain"

# (4) battery file integrity: leg count and file size UNCHANGED (2-for-2 line swap)
RC_COUNT=$(grep -cE '^run_mode ' "$CI/runtime_integration_test.sh")
[ "$RC_COUNT" -eq 49 ] && ok "battery run_mode count 49 (= 38 + 11)" || bad "run_mode count $RC_COUNT != 49"
[ "$(wc -l < "$CI/runtime_integration_test.sh")" -eq 1159 ] && ok "battery file 1159 l (header stamp true)" || bad "battery file grew/shrank"

# (5) mechanism plants — the retry bound + fail-closed teeth (throwaway wrapper)
P="$(mktemp -d)"; trap 'rm -rf "$P"' EXIT
cat > "$P/fake_rc.sh" <<'EOF'
#!/usr/bin/env bash
echo "FakeGodot v0.0.0.plant (throwaway MC 11312 verify wrapper)"
case "$*" in (*--version*) exit 0;; esac
if [ -n "${FAKE_COUNTER:-}" ]; then
  n=$(cat "$FAKE_COUNTER" 2>/dev/null || echo 0); n=$((n+1)); echo $n > "$FAKE_COUNTER"
  rc=$FAKE_FAIL_RC; [ "$n" -ge "$FAKE_FAIL_UNTIL" ] && rc=0
else rc="$FAKE_RC"; fi
exit "$rc"
EOF
chmod +x "$P/fake_rc.sh"
plant() { # plant <label> <want-retries> <want-rc> [env assignments...]
  local label="$1" wantretries="$2" wantrc="$3"; shift 3
  local log="$P/$label.log"
  ( cd "$CI/.." && env HOME="$HOME_ISO" XDG_DATA_HOME="$HOME_ISO/data" XDG_CACHE_HOME="$HOME_ISO/cache" \
      GODOT="$P/fake_rc.sh" FAKE_COUNTER= "$@" bash ci/build_solutions.sh . 1 ) > "$log" 2>&1
  local rc=$? got
  got=$(grep -c 'quit-segfault retry' "$log")
  if [ "$rc" = "$wantrc" ] && [ "$got" = "$wantretries" ]; then
    ok "plant $label (rc=$rc, retries=$got)"
  else bad "plant $label: rc=$rc (want $wantrc) retries=$got (want $wantretries)"; fi
}
plant forced-139-exhaust 2 1 FAKE_RC=139 LA_BUILD_RETRY_MAX=2
grep -q 'retry EXHAUSTED after 2 named retries' "$P/forced-139-exhaust.log" && ok "plant exhaust: EXHAUSTED line present" || bad "plant exhaust: no EXHAUSTED line"
plant forced-139-bound0 0 1 FAKE_RC=139 LA_BUILD_RETRY_MAX=0
plant rc1-not-retried 0 1 FAKE_RC=1
grep -q 'signature=nonzero-rc1 ' "$P/rc1-not-retried.log" && ok "plant rc1: named nonzero-rc1 line" || bad "plant rc1: no named line"
plant budget-kill-124 0 1 FAKE_RC=124
grep -q 'BUDGET-DIED' "$P/budget-kill-124.log" && ok "plant budget: BUDGET-DIED loud" || bad "plant budget: not loud"
rm -f "$P/counter"; plant flaky-139x2-then-green 2 0 FAKE_FAIL_RC=139 FAKE_FAIL_UNTIL=3 FAKE_COUNTER="$P/counter" LA_BUILD_RETRY_MAX=2
grep -q 'signature=exit-0-clean attempt=3' "$P/flaky-139x2-then-green.log" && ok "plant flaky: green on attempt 3 inside the bound" || bad "plant flaky: no clean attempt=3"

# (6) NO-MASKING plant: exit-139 riding a REAL compile error must RED immediately
DNA="$WT/skeleton/src/dna/DnaLanguage.cs"
cp "$DNA" "$P/DnaLanguage.cs.bak"
printf '\nthis is not valid C#;\n' >> "$DNA"
( cd "$WT/skeleton" && env HOME="$HOME_ISO" XDG_DATA_HOME="$HOME_ISO/data" XDG_CACHE_HOME="$HOME_ISO/cache" \
    GODOT="$P/fake_rc.sh" FAKE_RC=139 FAKE_COUNTER= bash ci/build_solutions.sh . 1 ) > "$P/nomask.log" 2>&1
NOMASK_RC=$?
cp "$P/DnaLanguage.cs.bak" "$DNA"
grep -q 'REAL build failure riding a 139 — NOT retried, NOT masked' "$P/nomask.log" \
  && [ "$NOMASK_RC" -eq 1 ] && [ "$(grep -c 'quit-segfault retry' "$P/nomask.log")" -eq 0 ] \
  && ok "plant no-masking: 139 + broken source REDS by name, zero retries" \
  || bad "plant no-masking: rc=$NOMASK_RC log=$P/nomask.log"
git -C "$WT" diff --quiet -- skeleton/src/dna/DnaLanguage.cs && ok "no-masking plant: source restored byte-exact" || bad "plant left the tree dirty"

# (7) GREEN on the REAL pinned engine through the helper (recompile forced)
touch "$WT/skeleton/src/dna/DnaLanguage.cs"
( cd "$WT/skeleton" && env HOME="$HOME_ISO" XDG_DATA_HOME="$HOME_ISO/data" XDG_CACHE_HOME="$HOME_ISO/cache" \
    GODOT="$E" bash ci/build_solutions.sh . 1 ) > "$P/green.log" 2>&1
GREEN_RC=$?
{ [ "$GREEN_RC" -eq 0 ] && grep -q 'signature=exit-0-clean attempt=1' "$P/green.log"; } \
  && ok "real-engine green: exit-0-clean attempt=1" || bad "real-engine green rc=$GREEN_RC"

# (8) SAVEGATE law row: the owner's real savegame.json is byte-identical
NOW=$(sha256sum "$HOME/.local/share/godot/app_userdata/Last Animal/savegame.json" | cut -d' ' -f1)
BEFORE=$(cut -d' ' -f1 "$OUT/owner-save-before.sha256")
[ "$NOW" = "$BEFORE" ] && ok "owner save byte-identical ($NOW)" || bad "owner save CHANGED: $NOW vs $BEFORE"

# (9) the battery's banked FULL-RUN verdict (rc-captured certification run)
BLOG="$OUT/logs/battery-cert.log"
if [ -f "$BLOG" ]; then
  grep -q 'RUNTIME_INTEGRATION_TEST: GATE PASS' "$BLOG" \
    && [ "$(grep -c 'GATE FAIL' "$BLOG")" -eq 0 ] \
    && tail -1 "$BLOG" | grep -q '^BATTERY_EXIT=0$' \
    && ok "battery certification run: rc 0 from the process (BATTERY_EXIT line), tail GATE PASS, 0 GATE FAIL lines" || bad "battery certification log not green — see $BLOG"
else bad "battery certification log not banked yet: $BLOG"; fi

echo "VERIFY_EXIT=$FAILS"
[ "$FAILS" -eq 0 ] && exit 0 || exit 1
