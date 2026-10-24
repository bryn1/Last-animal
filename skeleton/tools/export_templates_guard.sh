#!/usr/bin/env bash
# export_templates_guard.sh — mono-vs-standard export-template IDENTITY gate (MC 11137).
#
# WHY THIS EXISTS. Host VM350's export_templates dirs drifted twice (finding class
# MC 11129, shipped repair + MC 11137): a mono/standard byte swap inside
# ~/.local/share/godot/export_templates is SILENT to the engine — the mono editor
# exports happily from standard-drifted mono templates, and a standard dir carrying
# mono bytes exports a wrong-runtime build, both with zero engine errors.
# ci/export_check.sh only checked the templates dir EXISTS; this gate checks what
# the bytes ARE, per template, and is wired into that gate (one spine, both
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
# Exit: 0 = TPL_OK everywhere; non-zero on any TPL_DRIFT.
#
# PINNING: the name map is for the PINNED engine 4.7.2.stable (engine/PIN.txt).
# An engine bump updates this map in the same commit — listing drift on a
# non-matching name is reported as TPL_DRIFT by design (fail closed).
#
# Usage:
#   bash tools/export_templates_guard.sh                       # host dirs
#   bash tools/export_templates_guard.sh <mono_dir> <std_dir>  # explicit dirs
#   bash tools/export_templates_guard.sh --selftest            # RED pair + control
set -uo pipefail

TROOT="${HOME:?}/.local/share/godot/export_templates"
MONO_DIR="${1:-$TROOT/4.7.2.stable.mono}"
STD_DIR="${2:-$TROOT/4.7.2.stable}"

# The 9 files that are byte-identical mono-vs-standard upstream (everything else
# in the 35-file listing is DIVERGED). ONE SPACE-SEPARATED LINE — the membership
# test is space-delimited, and this guard's first live-host run caught exactly a
# newline-terminated-entry false-DRIFT (MC 11137, 2026-10-24; do not re-break).
SHARED=" icudt_godot.dat web_debug.zip web_dlink_debug.zip web_dlink_nothreads_debug.zip web_dlink_nothreads_release.zip web_dlink_release.zip web_nothreads_debug.zip web_nothreads_release.zip web_release.zip "

is_shared() { case "$SHARED" in *" $1 "*) return 0 ;; *) return 1 ;; esac; }

selftest() {
  # One command, three planted-red cases + control green — a gate that has never
  # gone RED is not a check (fleet verify law). Tiny synthetic dirs, same names.
  local T rc
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
  echo "TPL_GUARD SELFTEST: PASS — green control + 3 planted-drift RED cases"
}

if [ "${1:-}" = "--selftest" ]; then selftest; exit $?; fi

drift=0; checked=0
for dir_label in mono std; do
  d="$MONO_DIR"; [ "$dir_label" = std ] && d="$STD_DIR"
  [ -d "$d" ] || { echo "TPL_DRIFT ${dir_label}-dir-missing: $d"; drift=1; }
done
[ $drift -eq 0 ] || exit 1

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
  exit 1
fi
echo "TPL_GUARD: PASS — $checked templates checked (9 shared-identical + 26 diverged, per 4.7.2 baseline map)"
