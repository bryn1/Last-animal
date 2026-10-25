#!/usr/bin/env bash
# export_templates_guard.sh — mono<->standard export-template SWAP-invariant gate (MC 11137)
# + HASHLIST-PIN mode against the committed ground-truth allowlist (MC 11229).
#
# WHY THIS EXISTS. Host VM350's export_templates dirs drifted twice (finding class
# MC 11129, shipped repair + MC 11137): a mono/standard byte swap inside
# ~/.local/share/godot/export_templates is SILENT to the engine — the mono editor
# exports happily from standard-drifted mono templates, and a standard dir carrying
# mono bytes exports a wrong-runtime build, both with zero engine errors.
# ci/export_check.sh only checked the templates dir EXISTS; what this gate checks
# is exactly the mono<->standard SWAP invariant, per file name: a SHARED name must
# be byte-identical across the two dirs, a DIVERGED name must DIFFER across them —
# so a swap cannot pass green. The swap check is INVARIANT to NON-swap drift BY
# DESIGN (shared-equal files compare equal, diverged files differ — a corrupted or
# edited template passes both variants' equality). That gap closed in MC 11229:
# --pinned compares BOTH variant dirs against the committed ground-truth allowlist
# tools/export_templates.sha256 (every DIVERGED name x both variants; SHARED names
# need no pins — byte-equal upstream, covered by the swap check). Wired into the
# ci/export_check.sh pre-export gate beside the swap call (one spine, both
# platforms — MC 1405 dedup idiom, no beside-mechanism).
#
# THE MAP (baseline-proven 2026-10-24 by MC 11137 against the OFFICIAL
# Godot_v4.7.2-stable_export_templates.tpz, sha256 f298490b...48011, and the
# checksum-verified mono .tpz, sha256 92f8681e...15a9d — see
# .audits/20261024-0528-137t/EVIDENCE.md). 35 files per dir:
#   SHARED   — identical mono-vs-standard BY DESIGN (9): icudt_godot.dat + the 8
#              web_* templates. A naive "any shared file identical between the two
#              dirs => mono-clobber" check FALSE-POSITIVES on exactly these files —
#              that is the 11129 false-positive class this map exists to kill.
#   DIVERGED — must DIFFER mono-vs-standard (26): every platform template binary
#              (android/ios/linux/macos/windows) + version.txt (identity stamp).
# Rule per shared template name:
#   DIVERGED but byte-identical  -> TPL_DRIFT <file>  (mono bytes in standard dir
#                                   or standard bytes in mono dir — one signature)
#   SHARED   but byte-divergent  -> TPL_DRIFT <file>  (wrong-build bytes in one dir)
#   missing on either side       -> TPL_DRIFT <file>  (listing drift)
# Pin rule per DIVERGED template file (MC 11229, fail closed — an unproven byte
# is drift, not a pass):
#   live hash != allowlisted hash -> TPL_PIN_DRIFT <variant>/<file> hash-diverged
#   allowlisted file absent live  -> TPL_PIN_DRIFT <variant>/<file> live-file-missing
#   live diverged file unpinned   -> TPL_PIN_DRIFT <variant>/<file> unpinned
#   list file missing / malformed -> TPL_PIN_DRIFT list-missing / list-malformed
# Exit: 0 = TPL_OK everywhere and/or TPL_PIN_OK; non-zero on any drift.
#
# PINNING: the name map is for the PINNED engine 4.7.2.stable (engine/PIN.txt).
# An engine bump updates this map AND the allowlist in the same commit — listing
# drift on a non-matching name is reported by design (fail closed).
#
# Usage:
#   bash tools/export_templates_guard.sh                       # host dirs, swap check
#   bash tools/export_templates_guard.sh <mono_dir> <std_dir>  # explicit dirs
#   bash tools/export_templates_guard.sh --pinned [list] [mono_dir] [std_dir]
#                                       # hash-pin vs allowlist (MC 11229; fail closed)
#   bash tools/export_templates_guard.sh --repin [list] [mono_dir] [std_dir]
#                                       # regenerate allowlist from CURRENT dirs —
#                                       # explicit auditable action; REFUSES while the
#                                       # swap check is RED (never enshrine a swap)
#   bash tools/export_templates_guard.sh --selftest            # RED pairs + controls (swap + pin)
# SIZE: 331 l — restamped by wc at this head (MC 11229 pin mode).
set -uo pipefail

TROOT="${HOME:?}/.local/share/godot/export_templates"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PIN_LIST_DEFAULT="$SCRIPT_DIR/export_templates.sha256"

# Flag-first parsing keeps the MC 11137 positional contract byte-unchanged
# (check mode: [mono_dir] [std_dir]); the pin modes take an optional allowlist
# path AHEAD of the dirs (default: export_templates.sha256 beside this script).
MODE=check
case "${1:-}" in
  --selftest) MODE=selftest; shift ;;
  --pinned)   MODE=pinned;   shift ;;
  --repin)    MODE=repin;    shift ;;
esac
PIN_LIST="$PIN_LIST_DEFAULT"
MONO_DIR="$TROOT/4.7.2.stable.mono"
STD_DIR="$TROOT/4.7.2.stable"
if [ "$MODE" = check ]; then
  MONO_DIR="${1:-$MONO_DIR}"; STD_DIR="${2:-$STD_DIR}"
else
  PIN_LIST="${1:-$PIN_LIST}"; MONO_DIR="${2:-$MONO_DIR}"; STD_DIR="${3:-$STD_DIR}"
fi

# The 9 files that are byte-identical mono-vs-standard upstream (everything else
# in the 35-file listing is DIVERGED). ONE SPACE-SEPARATED LINE — the membership
# test is space-delimited, and this guard's first live-host run caught exactly a
# newline-terminated-entry false-DRIFT (MC 11137, 2026-10-24; do not re-break).
SHARED=" icudt_godot.dat web_debug.zip web_dlink_debug.zip web_dlink_nothreads_debug.zip web_dlink_nothreads_release.zip web_dlink_release.zip web_nothreads_debug.zip web_nothreads_release.zip web_release.zip "

is_shared() { case "$SHARED" in *" $1 "*) return 0 ;; *) return 1 ;; esac; }

swap_check() {
  local drift=0 checked=0 dir_label d names n hm hs
  for dir_label in mono std; do
    d="$MONO_DIR"; [ "$dir_label" = std ] && d="$STD_DIR"
    [ -d "$d" ] || { echo "TPL_DRIFT ${dir_label}-dir-missing: $d"; drift=1; }
  done
  [ $drift -eq 0 ] || return 1

  # Union of listings (file names only; the dirs are flat 35-file sets).
  names="$(cat <(cd "$MONO_DIR" && ls -1) <(cd "$STD_DIR" && ls -1) | sort -u)"
  for n in $names; do
    [ -f "$MONO_DIR/$n" ] && [ -f "$STD_DIR/$n" ] || { echo "TPL_DRIFT $n"; drift=1; continue; }
    hm="$(sha256sum "$MONO_DIR/$n" | cut -d' ' -f1)"
    hs="$(sha256sum "$STD_DIR/$n"  | cut -d' ' -f1)"
    if is_shared "$n"; then
      [ "$hm" = "$hs" ] && echo "TPL_OK $n (shared)" || { echo "TPL_DRIFT $n"; drift=1; }
    else
      [ "$hm" != "$hs" ] && echo "TPL_OK $n" || { echo "TPL_DRIFT $n"; drift=1; }
    fi
    checked=$((checked+1))
  done
  if [ $drift -ne 0 ]; then
    echo "TPL_GUARD: FAIL — mono/standard template identity drift (MC 11137): restore from checksum-verified .tpz (evidence: .audits/20261024-0528-137t/EVIDENCE.md)"
    return 1
  fi
  # Counts in the PASS line below are pinned by the baseline map (9 SHARED names; 26 = the 35-file
  # 4.7.2 listing minus the 9) — --selftest's MAP-INTEGRITY block asserts the map itself is intact.
  echo "TPL_GUARD: PASS — $checked templates checked (9 shared-identical + 26 diverged, per 4.7.2 baseline map)"
}

pinned_check() {
  local drift=0 checked=0 dir_label d var n key want got v h rest
  if [ ! -f "$PIN_LIST" ]; then
    echo "TPL_PIN_DRIFT list-missing: $PIN_LIST — regenerate ONLY from checksum-verified .tpz-restored dirs: bash tools/export_templates_guard.sh --repin (MC 11229 fail-closed)"
    return 1
  fi
  local -A PIN=()
  while read -r v n h rest; do
    case "$v" in ''|'#'*) continue ;; esac
    if { [ "$v" != mono ] && [ "$v" != std ]; } || [ -z "$n" ] || [[ ! "$h" =~ ^[0-9a-f]{64}$ ]] || [ -n "${rest:-}" ]; then
      echo "TPL_PIN_DRIFT list-malformed: $v $n $h (line format: '<variant> <file> <sha256>')"; drift=1; continue
    fi
    if [ -n "${PIN[$v/$n]:-}" ]; then
      echo "TPL_PIN_DRIFT list-malformed: duplicate pin $v/$n"; drift=1; continue
    fi
    PIN[$v/$n]="$h"
  done < "$PIN_LIST"
  for dir_label in mono std; do
    d="$MONO_DIR"; [ "$dir_label" = std ] && d="$STD_DIR"
    [ -d "$d" ] || { echo "TPL_PIN_DRIFT ${dir_label}-dir-missing: $d"; drift=1; }
  done
  [ $drift -eq 0 ] || return 1
  # 1) every allowlisted hash must match its live file byte-for-byte (corrupted,
  #    edited, swapped or DELETED template files all go RED by name).
  for key in $(printf '%s\n' "${!PIN[@]}" | sort); do
    var="${key%%/*}"; n="${key#*/}"; want="${PIN[$key]}"
    d="$MONO_DIR"; [ "$var" = std ] && d="$STD_DIR"
    if [ ! -f "$d/$n" ]; then
      echo "TPL_PIN_DRIFT $key live-file-missing"; drift=1; continue
    fi
    got="$(sha256sum "$d/$n" | cut -d' ' -f1)"
    if [ "$got" = "$want" ]; then
      echo "TPL_PIN_OK $key"; checked=$((checked+1))
    else
      echo "TPL_PIN_DRIFT $key hash-diverged want=${want:0:12}... got=${got:0:12}..."; drift=1
    fi
  done
  # 2) fail closed the other direction: every live DIVERGED file must be pinned —
  #    a pin deleted from the list, or a new template appearing, goes RED by name.
  for dir_label in mono std; do
    d="$MONO_DIR"; [ "$dir_label" = std ] && d="$STD_DIR"
    for n in $(cd "$d" && ls -1); do
      is_shared "$n" && continue
      [ -n "${PIN[$dir_label/$n]:-}" ] || { echo "TPL_PIN_DRIFT $dir_label/$n unpinned"; drift=1; }
    done
  done
  if [ $drift -ne 0 ]; then
    echo "TPL_PIN_GUARD: FAIL — hashlist-pin drift (MC 11229): restore from checksum-verified .tpz, NEVER --repin over suspected drift (lineage: .audits/20261024-0528-137t/EVIDENCE.md)"
    return 1
  fi
  echo "TPL_PIN_OK: $checked pins verified vs $PIN_LIST (diverged names x 2 variants, 4.7.2 baseline map; the 9 shared names are covered by the swap-invariant check)"
}

repin() {
  # Explicit auditable action: the pin list is GROUND TRUTH, so it may only be
  # written from a pair the swap-invariant check accepts — never enshrine a swap.
  if ! swap_check; then
    echo "TPL_GUARD REPIN: REFUSED — swap-invariant check is RED on these dirs; restore from checksum-verified .tpz (MC 11137) BEFORE repinning (MC 11229)" >&2
    return 1
  fi
  local var d n h count
  {
    cat <<'HDR'
# export_templates.sha256 — APPROVED sha256 allowlist of the variant-specific
# (DIVERGED per the export_templates_guard.sh 4.7.2 baseline map) export-template
# files, BOTH variants, engine 4.7.2.stable (engine/PIN.txt). MC 11229.
#
# WHY THIS FILE EXISTS: the swap-invariant guard (MC 11137) compares mono-vs-
# standard and is swap-invariant BY DESIGN — corrupted or wrong-version bytes on
# a DIVERGED file pass both variants' equality. This list is the ground truth the
# swap check cannot have: guard --pinned fails closed (TPL_PIN_DRIFT <name>, non-
# zero exit) on ANY hash mismatch, missing live file, missing pin, or malformed /
# missing list. SHARED files (icudt_godot.dat + the 8 web_* templates) are NOT
# listed: byte-equal mono-vs-standard upstream and covered by the swap-invariant
# check, a pin would duplicate coverage without adding truth.
#
# PROVENANCE of the shipped list: generated (--repin, MC 11229) from the VM350
# host dirs, whose every byte traces to an official checksum-verified artifact —
# standard dir == official .tpz sha256 f298490b...48011; mono dir == official
# mono .tpz sha256 92f8681e...15a9d + the 9 shared names == official standard
# .tpz (lineage: .audits/20261024-0528-137t/EVIDENCE.md).
#
# REGENERATE (explicit auditable action, ONLY from a known-good pair — a
# checksum-verified .tpz restore, never over suspected drift):
#   bash tools/export_templates_guard.sh --repin
# The repin REFUSES while the swap-invariant check is RED. Review the git diff.
#
# Line format: <variant> <template-file-name> <sha256-hex>; variant is mono|std,
# naming 4.7.2.stable.mono/ vs 4.7.2.stable/ under ~/.local/share/godot/export_templates.
HDR
    for var in mono std; do
      d="$MONO_DIR"; [ "$var" = std ] && d="$STD_DIR"
      for n in $(cd "$d" && ls -1 | sort); do
        is_shared "$n" && continue
        [ -f "$d/$n" ] || continue
        h="$(sha256sum "$d/$n" | cut -d' ' -f1)"
        printf '%s %s %s\n' "$var" "$n" "$h"
      done
    done
  } > "$PIN_LIST" || { echo "TPL_GUARD REPIN: cannot write $PIN_LIST" >&2; return 1; }
  count="$(grep -cv '^#' "$PIN_LIST")"
  echo "TPL_GUARD REPIN: wrote $count pins to $PIN_LIST (diverged names x 2 variants — regenerate ONLY from checksum-verified .tpz-restored dirs)"
}

selftest() {
  # One command, three planted-red cases + control green — a gate that has never
  # gone RED is not a check (fleet verify law). Tiny synthetic dirs, same names.
  local T rc out swap_out swap_rc
  T="$(mktemp -d)"; trap "rm -rf '$T'" EXIT
  # Map integrity FIRST: every name declared SHARED must actually match the
  # membership test, and two DIVERGED samples must not (this assertion is why
  # the newline-entry false-DRIFT class cannot come back).
  local s
  for s in $SHARED; do
    is_shared "$s" || { echo "TPL_GUARD SELFTEST: MAP-INTEGRITY '$s' not matched by is_shared"; return 1; }
  done
  for s in linux_release.x86_64 version.txt android_debug.apk; do
    is_shared "$s" && { echo "TPL_GUARD SELFTEST: MAP-INTEGRITY diverged sample '$s' wrongly shared"; return 1; }
  done
  [ "${#SHARED}" -gt 10 ] || { echo "TPL_GUARD SELFTEST: MAP-INTEGRITY empty SHARED list"; return 1; }
  mk_pair() { # $1=root: diverged files differ, shared files identical (ALL 9 shared names covered)
    mkdir -p "$1/mono" "$1/std"
    local n
    for n in linux_release.x86_64 version.txt; do
      printf 'MONO-BYTE-%s\n'  "$n" > "$1/mono/$n";  printf 'STD-BYTE-%s\n'  "$n" > "$1/std/$n"
    done
    for n in $SHARED; do
      printf 'SHARED-UPSTREAM-BYTE-%s\n' "$n" > "$1/mono/$n"; cp "$1/mono/$n" "$1/std/$n"
    done
  }
  mk_pair "$T/green"
  bash "$0" "$T/green/mono" "$T/green/std" >/dev/null \
    && echo "TPL_GUARD SELFTEST: green control OK" || { echo "TPL_GUARD SELFTEST: RED-PAIR FAILED TO GO GREEN (control broken)"; return 1; }
  # RED 1 (MC 11137 class): plant the mono byte into the STANDARD copy.
  mk_pair "$T/red1"; cp "$T/red1/mono/linux_release.x86_64" "$T/red1/std/linux_release.x86_64"
  out="$(bash "$0" "$T/red1/mono" "$T/red1/std")"; rc=$?
  echo "$out" | grep -q '^TPL_DRIFT linux_release.x86_64$' \
    || { echo "TPL_GUARD SELFTEST: RED-1 MISSING DRIFT LINE for linux_release.x86_64"; return 1; }
  [ $rc -ne 0 ] || { echo "TPL_GUARD SELFTEST: RED-1 NOT RED (exit 0 on mono-in-standard)"; return 1; }
  # RED 2 (MC 11129 class): plant the standard identity stamp into the MONO copy.
  mk_pair "$T/red2"; cp "$T/red2/std/version.txt" "$T/red2/mono/version.txt"
  out="$(bash "$0" "$T/red2/mono" "$T/red2/std")"; rc=$?
  echo "$out" | grep -q '^TPL_DRIFT version.txt$' \
    || { echo "TPL_GUARD SELFTEST: RED-2 MISSING DRIFT LINE for version.txt"; return 1; }
  [ $rc -ne 0 ] || { echo "TPL_GUARD SELFTEST: RED-2 NOT RED (exit 0 on standard-in-mono)"; return 1; }
  # RED 3: diverge a shared file (wrong-build bytes in one dir).
  mk_pair "$T/red3"; printf 'FORGED-WEB-BYTES\n' > "$T/red3/std/web_release.zip"
  out="$(bash "$0" "$T/red3/mono" "$T/red3/std")"; rc=$?
  echo "$out" | grep -q '^TPL_DRIFT web_release.zip$' \
    || { echo "TPL_GUARD SELFTEST: RED-3 MISSING DRIFT LINE for web_release.zip"; return 1; }
  [ $rc -ne 0 ] || { echo "TPL_GUARD SELFTEST: RED-3 NOT RED (exit 0 on shared-file divergence)"; return 1; }
  # ---- MC 11229 hashlist-pin plants: green control + 5 RED cases ----
  # GREEN control: repin the synthetic pair, then --pinned must pass its own list.
  mk_pair "$T/pin0"
  bash "$0" --repin "$T/pin0/list" "$T/pin0/mono" "$T/pin0/std" >/dev/null \
    || { echo "TPL_GUARD SELFTEST: PIN-REPIN FAILED (generator broken)"; return 1; }
  out="$(bash "$0" --pinned "$T/pin0/list" "$T/pin0/mono" "$T/pin0/std")"; rc=$?
  { echo "$out" | grep -q '^TPL_PIN_OK' && [ $rc -eq 0 ]; } \
    || { echo "TPL_GUARD SELFTEST: PIN GREEN CONTROL broken (rc=$rc): $out"; return 1; }
  # RED-PIN 1 planted drift: tamper a COPY dir's DIVERGED file with wrong-version
  # bytes — invisible to the swap check (the pair STILL differs), the very drift
  # class the swap gate cannot see. Assert BOTH halves: swap gate GREEN, pin gate
  # RED by name (the gap this mode exists to close, proven per run).
  mk_pair "$T/pin1"
  bash "$0" --repin "$T/pin1/list" "$T/pin1/mono" "$T/pin1/std" >/dev/null
  printf 'WRONG-VERSION-BYTES\n' > "$T/pin1/std/linux_release.x86_64"
  swap_out="$(bash "$0" "$T/pin1/mono" "$T/pin1/std")"; swap_rc=$?
  [ $swap_rc -eq 0 ] \
    || { echo "TPL_GUARD SELFTEST: RED-PIN-1 tamper visible to SWAP gate — invariant premise broken, plant invalid: $swap_out"; return 1; }
  out="$(bash "$0" --pinned "$T/pin1/list" "$T/pin1/mono" "$T/pin1/std")"; rc=$?
  echo "$out" | grep -q '^TPL_PIN_DRIFT std/linux_release.x86_64 hash-diverged' \
    || { echo "TPL_GUARD SELFTEST: RED-PIN-1 MISSING DRIFT LINE (non-swap drift passed the pin check!): $out"; return 1; }
  [ $rc -ne 0 ] || { echo "TPL_GUARD SELFTEST: RED-PIN-1 NOT RED (exit 0 on tampered template)"; return 1; }
  # RED-PIN 2 planted deleted pin: drop the list's std version.txt line — the
  # still-live diverged file is now unpinned and must go RED by name (fail closed).
  mk_pair "$T/pin2"
  bash "$0" --repin "$T/pin2/list" "$T/pin2/mono" "$T/pin2/std" >/dev/null
  grep -v '^std version.txt ' "$T/pin2/list" > "$T/pin2/list.nopin" \
    && mv "$T/pin2/list.nopin" "$T/pin2/list"
  out="$(bash "$0" --pinned "$T/pin2/list" "$T/pin2/mono" "$T/pin2/std")"; rc=$?
  echo "$out" | grep -q '^TPL_PIN_DRIFT std/version.txt unpinned$' \
    || { echo "TPL_GUARD SELFTEST: RED-PIN-2 MISSING DRIFT LINE for deleted pin std/version.txt: $out"; return 1; }
  [ $rc -ne 0 ] || { echo "TPL_GUARD SELFTEST: RED-PIN-2 NOT RED (exit 0 on deleted pin)"; return 1; }
  # RED-PIN 3 planted missing file: allowlist intact, live file deleted.
  mk_pair "$T/pin3"
  bash "$0" --repin "$T/pin3/list" "$T/pin3/mono" "$T/pin3/std" >/dev/null
  rm "$T/pin3/std/version.txt"
  out="$(bash "$0" --pinned "$T/pin3/list" "$T/pin3/mono" "$T/pin3/std")"; rc=$?
  echo "$out" | grep -q '^TPL_PIN_DRIFT std/version.txt live-file-missing$' \
    || { echo "TPL_GUARD SELFTEST: RED-PIN-3 MISSING DRIFT LINE for deleted file std/version.txt: $out"; return 1; }
  [ $rc -ne 0 ] || { echo "TPL_GUARD SELFTEST: RED-PIN-3 NOT RED (exit 0 on missing file)"; return 1; }
  # RED-PIN 4 missing allowlist file: fail closed with a named line.
  mk_pair "$T/pin4"
  out="$(bash "$0" --pinned "$T/pin4/no-such-list" "$T/pin4/mono" "$T/pin4/std")"; rc=$?
  echo "$out" | grep -q '^TPL_PIN_DRIFT list-missing:' \
    || { echo "TPL_GUARD SELFTEST: RED-PIN-4 NOT NAMED RED on missing list: $out"; return 1; }
  [ $rc -ne 0 ] || { echo "TPL_GUARD SELFTEST: RED-PIN-4 NOT RED (exit 0 on missing list file)"; return 1; }
  # RED-PIN 5 repin refuse: never enshrine a swapped pair into ground truth.
  mk_pair "$T/pin5"; cp "$T/pin5/mono/linux_release.x86_64" "$T/pin5/std/linux_release.x86_64"
  bash "$0" --repin "$T/pin5/list" "$T/pin5/mono" "$T/pin5/std" >/dev/null; rc=$?
  [ $rc -ne 0 ] || { echo "TPL_GUARD SELFTEST: RED-PIN-5 NOT RED (repin accepted a swapped pair)"; return 1; }
  echo "TPL_GUARD SELFTEST: PASS — green control + 3 planted-drift RED cases (MC 11137) + pin green control + 5 planted pin-mode RED cases (MC 11229)"
}

case "$MODE" in
  selftest) selftest; exit $? ;;
  pinned)   pinned_check; exit $? ;;
  repin)    repin; exit $? ;;
  *)        swap_check; exit $? ;;
esac
