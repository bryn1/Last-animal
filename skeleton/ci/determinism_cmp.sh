#!/usr/bin/env bash
# determinism_cmp.sh — F4-CMP determinism comparator (MC 10026.13.2 S7; owner
# of the mechanism per plan 10081 v3 §0 F4-CMP + §S7). This is a TOOL, not a
# battery leg — the gate-LIST/banner restamp happens at wave close.
#
# Runs battery mode(s) TWICE (A/A) and classifies the stdout streams per F4-CMP
# (seeded from the DLQ 44-marker cmp, the ^ROSTER family and the LA_GATE
# precedents). Raw FULL-stream byte identity is impossible as a class (float
# jitter) — never claimed. The classes:
#   (i)   BYTE-STABLE  — discrete-domain lines: ^ROSTER family, ^REWARD_SHOWN,
#          and ^LA_GATE lines WITHOUT a (ii) field below (quest/manna/domain/
#          roster/save markers). Any delta between A and A' is RED.
#   (ii)  NUMERIC-MASK — ^LA_GATE lines carrying a dist/position field
#          (II_FIELD below). KEYED ON FIELD NAMES, not float syntax: the
#          engine's 0.### rendering can collapse 3.997 to "4" across runs, so
#          value-shape keying is class-unstable (measured, this card's own
#          A/A). New body-mover markers join II_KEY here or declare.
#          Compared with numeric tokens masked to '#' (noise-control class
#          precedent, TEST-verdict-10080 §2): numeric drift inside otherwise-
#          equal lines is ALLOWED. A HUNK — a masked-line divergence incl.
#          line-count change — is RED unless declared with --declare <regex>
#          (legit body-movers, e.g. S8 ring-slots, declare their numeric-hunk
#          divergence there).
# Exit 0 prints "DETERMINISM: IDENTICAL"; any RED exits 1 naming the delta.
# Content guards (MC 10111, DA P2-1) run BEFORE the classes at cmp_pair entry:
# a harness FAIL banner on either side is RED (exit-code equality alone is
# not proof — _Finalize prints FAIL without Quit(1)), and a side that keys
# zero lines is RED (an empty-vs-empty diff is vacuously IDENTICAL).
#
# SAVE-TOUCHING: proof runs read/write user://savegame.json — run ONLY while
# holding the savegate mutex (mkdir /tmp/la-savegate.lock); this script never
# touches the lock itself — the caller holds it across the whole run.
#
# Usage:
#   GODOT=<engine> ./ci/determinism_cmp.sh [project_dir] [spec ...] [--declare <re>]... [--logs <dir>]
#     spec = <mode> or <mode>=<res-proof-path>   (default spec: positive)
#   ./ci/determinism_cmp.sh --cmp <logA> <logB> [--declare <re>]...   (compare captured logs)
set -uo pipefail

HERE="$(cd "$(dirname "$0")" && pwd)"
PROOF_DEFAULT="res://ci_proofs/RuntimeIntegrationProof.cs"

fail() { echo "DETERMINISM_CMP: RED: $*" >&2; exit 1; }
MASK='s/[0-9]+(\.[0-9]+)?/#/g'                       # numeric-token mask (F4-CMP (ii))
# Literal harness FAIL banner — copied from ci_proofs/RuntimeIntegrationProof.cs
# :409 (_Finalize, PrintErr, NO Quit(1)) and :442 (Fail()); grep the text, never exit code.
FAIL_BANNER='LA_GATE: FAIL — '
# (ii) stream = ^LA_GATE lines carrying a dist/position field. Keyed on FIELD
# NAMES, not float shape: the engine's 0.### format renders 3.997 as "4" when
# the value lands on an integer, so shape-keying flips a line between classes
# run-to-run (measured on this card's own positive A/A: dist0=3.997 vs dist0=4).
II_KEY='(dist[0-9]*=|\(dist [0-9.]+ ->|aim=\(|player=\(|position=|pos=\(|at \()'
stream_ii() { grep -E "^LA_GATE" "$1" | grep -E "$II_KEY" || true; }
stream_i()  { grep -E '^(ROSTER|REWARD_SHOWN|LA_GATE)' "$1" | grep -vE "$II_KEY" || true; }

# --- classification per F4-CMP, applied to two captured logs ------------------
cmp_pair() {  # cmp_pair <logA> <logB>
  local a b ra rb
  # Content guards (MC 10111, DA P2-1 — .audits/202610041758-1e37b441/DA-verdict.md).
  # (b) The proof harness _Finalize prints its FAIL banner via PrintErr and never
  # Quit(1) (RuntimeIntegrationProof.cs:408-409), so an identical FAIL-pair exits
  # equal — an exit-code-only gate compares a broken battery GREEN. (a) With zero
  # keyed lines both sides, diff finds empty==empty: "0 lines, IDENTICAL" is a
  # vacuous pass. Both --cmp and live A/A route through here, one guard each.
  grep -qF "$FAIL_BANNER" "$1" && fail "log A ($1) carries the harness FAIL banner: $(grep -m1 -F "$FAIL_BANNER" "$1")"
  grep -qF "$FAIL_BANNER" "$2" && fail "log B ($2) carries the harness FAIL banner: $(grep -m1 -F "$FAIL_BANNER" "$2")"
  a="$(stream_i "$1")"; b="$(stream_i "$2")"
  ra="$(stream_ii "$1")"; rb="$(stream_ii "$2")"
  { [ -n "$a" ] || [ -n "$ra" ]; } || fail "log A ($1) keys ZERO lines — empty-vs-empty is vacuous, not a determinism result"
  { [ -n "$b" ] || [ -n "$rb" ]; } || fail "log B ($2) keys ZERO lines — empty-vs-empty is vacuous, not a determinism result"
  if [[ "$a" == "$b" ]]; then
    echo "DETERMINISM_CMP: (i) byte-stable stream: $(printf '%s\n' "$a" | grep -c . ) lines, IDENTICAL"
  else
    diff <(printf '%s\n' "$a") <(printf '%s\n' "$b") | sed 's/^/  (i) /' >&2
    fail "(i)-delta in the byte-stable stream (see diff above)"
  fi
  local ma mb n
  ma="$(printf '%s\n' "$ra" | sed -E "$MASK")"; mb="$(printf '%s\n' "$rb" | sed -E "$MASK")"
  n="$(printf '%s\n' "$ma" | grep -c . )"
  if [[ "$ma" == "$mb" ]]; then
    local drift=""
    [[ "$ra" != "$rb" ]] && drift=", numeric drift inside masked-equal lines (allowed noise class)"
    echo "DETERMINISM_CMP: (ii) numeric stream: $n lines, masked-IDENTICAL$drift"
  else
    local -a hunks=() undeclared=()
    mapfile -t hunks < <(diff <(printf '%s\n' "$ma") <(printf '%s\n' "$mb") | grep -E '^[<>]' | sed 's/^[<>] //' || true)
    local h d declared
    for h in "${hunks[@]}"; do
      declared=0
      for d in "${DECL[@]}"; do [[ "$h" =~ $d ]] && declared=1 && break; done
      [ "$declared" -eq 1 ] || undeclared+=("$h")
    done
    if [ "${#undeclared[@]}" -gt 0 ]; then
      for d in "${undeclared[@]}"; do echo "  (ii) UNDECLARED HUNK: $d" >&2; done
      fail "${#undeclared[@]} undeclared (ii)-hunk(s) in the masked numeric stream (declare with --declare <re>)"
    fi
    echo "DETERMINISM_CMP: (ii) numeric stream: $n lines, ${#hunks[@]} masked hunks DECLARED and accepted"
  fi
  echo "DETERMINISM: IDENTICAL"
}

# --- arg parse ----------------------------------------------------------------
DECL=(); MODES=(); LOGDIR=""; CMP_A=""; CMP_B=""
PROJ=""
while [ $# -gt 0 ]; do
  case "$1" in
    --cmp)      [ $# -ge 3 ] || fail "--cmp needs <logA> <logB>"; CMP_A="$2"; CMP_B="$3"; shift 3 ;;
    --declare)  [ $# -ge 2 ] || fail "--declare needs a regex"; DECL+=("$2"); shift 2 ;;
    --logs)     [ $# -ge 2 ] || fail "--logs needs a dir"; LOGDIR="$2"; shift 2 ;;
    *=*)        MODES+=("$1"); shift ;;
    *)          if [ -z "$PROJ" ]; then PROJ="$1"; else MODES+=("$1"); fi; shift ;;
  esac
done
[ -n "$CMP_A" ] && { cmp_pair "$CMP_A" "$CMP_B"; exit 0; }
: "${GODOT:?set GODOT to the engine binary (see ../engine/PIN.txt)}"
[ -x "$GODOT" ] || fail "GODOT not executable: $GODOT"
PROJ="${PROJ:-$(dirname "$HERE")}"
[ -z "${MODES[*]}" ] && MODES=("positive")
[ -n "$LOGDIR" ] || LOGDIR="$(mktemp -d "${TMPDIR:-/tmp}/detcmp.XXXXXX")"
mkdir -p "$LOGDIR"
echo "DETERMINISM_CMP: project=$PROJ modes=${MODES[*]} logs=$LOGDIR declares=${#DECL[@]} (savegate lock must be HELD by caller)"

# same fixed preamble as the battery: one-time import + build through the pinned engine
if [ ! -d "$PROJ/.godot/imported" ] || [ -z "$(ls -A "$PROJ/.godot/imported" 2>/dev/null)" ]; then
  timeout 600 "$GODOT" --headless --path "$PROJ" --import >/dev/null 2>&1 || fail "engine --import exited nonzero"
fi
timeout 300 "$GODOT" --headless --path "$PROJ" --build-solutions --quit-after 1 \
  || fail "engine --build-solutions exited nonzero (C# build failed)"

for spec in "${MODES[@]}"; do
  mode="${spec%%=*}"; proof="${spec#*=}"
  [ "$proof" = "$spec" ] && proof="$PROOF_DEFAULT"
  echo "DETERMINISM_CMP: mode=$mode (A/A)"
  LA_GATE_MODE="$mode" timeout 240 "$GODOT" --headless --path "$PROJ" --script "$proof" >"$LOGDIR/$mode.A.log" 2>&1 \
    || fail "mode $mode: run A exited nonzero (legs must be GREEN before cmp)"
  LA_GATE_MODE="$mode" timeout 240 "$GODOT" --headless --path "$PROJ" --script "$proof" >"$LOGDIR/$mode.A2.log" 2>&1 \
    || fail "mode $mode: run A' exited nonzero"
  cmp_pair "$LOGDIR/$mode.A.log" "$LOGDIR/$mode.A2.log"
done
