#!/usr/bin/env python3
"""mode_sets_check.py — MC 10210 (W2 tail): the LA_GATE_MODE vocabulary drift gate.

Every LA_GATE_MODE-dispatching proof carries its mode vocabulary in up to
THREE places that must never disagree:
  (1) the header doc block ("Modes (env LA_GATE_MODE ...)" down to "// Run:"),
  (2) an allow-list where one exists (RuntimeIntegrationProof.KnownModes),
  (3) the dispatch arms themselves (every `case "x":` inside a `switch (_mode)`
      block, plus every `_mode == "x"` / `_mode is "x" or "y"` comparison —
      the idiom the RuntimeIntegrationProof partials use).
History this gates (F-C + orchestrator ruling 10203 append #5): the S16
keep-both merge SILENTLY DROPPED the passives doc row (MC 10204 found it),
and P1FixProof/ZoneBossProof fell unknown modes THROUGH to a default stage —
a doc/dispatch drift is exactly how a gate goes vacuous-green.

For each proof this check asserts doc == allow-list == dispatch arms and
exits 1 with a named VIOLATION line per drift. --selftest ships the
planted-bad RED demo WITHOUT touching the tree: a temp copy gets one extra
dispatch arm with no doc row (P1FixProof + ZoneBossProof) and one extra
KnownModes entry (RuntimeIntegrationProof); every plant must go RED, naming
its planted mode. stdlib-only; no engine; runs from any cwd.

STAGE PAIRING (MC 10218, ADDED SCOPE — the 781ea66 P0 lesson): the mode
vocabulary above cannot see the stage switch, so the 3-way merge that
dropped the S18 dispatch was INVISIBLE to this gate. Second leg: every
Run*Stage() definition in ci_proofs/RuntimeIntegrationProof*.cs must have a
call-site arm in the entry's switch (_stage), and every entry arm must have
a definition — unpaired stage body or dead arm is a named VIOLATION. --selftest
plants BOTH (an unpaired stage body in Chain.cs, a dead arm in the entry) on
temp copies; the tree is never touched.

MC 10258 (DA F2 / ARCH P3-B): --selftest gained the off-type both-ways pair —
an int-returning stage body PAIRED with its entry arm must stay GREEN, the
same body WITHOUT its arm must go RED UNPAIRED — the two directions the
void|bool-only def regex inverted (paired = false-RED dead arm, unpaired =
silent-GREEN against the pairing law's promise).

Usage:  mode_sets_check.py [--project <skeleton_dir>] [--selftest]
"""
import os
import re
import shutil
import sys
import tempfile

# proof name -> (entry file, class-file prefix, has an allow-list)
#   allow-list: KnownModes parsed from the entry file and required to agree.
#   A proof with no allow-list is gated on doc == dispatch arms — legal here
#   because its dispatcher is an enumerable switch (the roster idiom).
PROOFS = [
    ("RuntimeIntegrationProof", "RuntimeIntegrationProof.cs", "RuntimeIntegrationProof", True),
    ("RosterIntegrationProof", "RosterIntegrationProof.cs", "RosterIntegrationProof", False),
    ("ZoneBossProof", "ZoneBossProof.cs", "ZoneBossProof", False),
    ("P1FixProof", "P1FixProof.cs", "P1FixProof", False),
]


def mask_code(text):
    """Blank string CONTENTS (to 'x') and comments (to spaces) of C# source,
    preserving every character offset (newlines stay). Braces inside strings
    or comments stop interfering with the switch-block walk; interpolation
    holes are treated as opaque (every real hole is brace-balanced)."""
    out = list(text)
    i, n = 0, len(text)
    while i < n:
        c = text[i]
        if text.startswith("//", i):
            j = text.find("\n", i)
            j = n if j < 0 else j
            for k in range(i, j):
                out[k] = " "
            i = j
        elif text.startswith("/*", i):
            j = text.find("*/", i + 2)
            j = n if j < 0 else j + 2
            for k in range(i, j):
                if out[k] != "\n":
                    out[k] = " "
            i = j
        elif (mc := re.match(r"'(?:\\.|[^'\\])'", text[i:])) and c == "'":
            out[i + 1] = "x"                       # char literal body
            i += mc.end()
        elif (sm := re.match(r'(?:\$@?|@\$?)?"', text[i:])):
            tok = sm.group(0)
            verbatim = "@" in tok
            j = body = i + len(tok)
            while j < n:
                if verbatim:
                    if text[j] == '"':
                        if text.startswith('""', j):
                            j += 2
                            continue
                        break
                    j += 1
                elif text[j] == "\\":
                    j += 2
                elif text[j] == '"':
                    break
                else:
                    j += 1
            for k in range(body, min(j, n)):
                if out[k] != "\n":
                    out[k] = "x"
            i = min(j, n) + 1                      # step past the terminator
        else:
            i += 1
    return "".join(out)


def _switch_regions(masked, header_re):
    """Yield (start, end) spans of `switch ... { ... }` blocks whose header
    matches, with brace depth computed on masked text (string-safe)."""
    for m in re.finditer(header_re, masked):
        brace = masked.index("{", m.start())
        depth, j, n = 0, brace, len(masked)
        while j < n:
            ch = masked[j]
            if ch == "{":
                depth += 1
            elif ch == "}":
                depth -= 1
                if depth == 0:
                    break
            j += 1
        yield m.start(), j


def dispatch_arms(text):
    """Mode strings the code can ACTUALLY route on: case labels inside
    switch (_mode) blocks, `_mode == "x"`, `_mode is "x" or "y"` chains, and
    (legacy idiom) arms of `_mode switch { "x" => ... }` expressions."""
    masked = mask_code(text)
    arms = set()

    def literal_in(orig, s, e):
        m = re.search(r'"([^"]*)"', orig[s:e])
        return m.group(1) if m else None

    for a, b in _switch_regions(masked, r"switch\s*\(\s*_mode\s*\)\s*\{"):
        for cm in re.finditer(r'\bcase\s+"[a-zA-Z_0-9]*"', masked[a:b]):
            lit = literal_in(text, a + cm.start(), a + cm.end())
            if lit is not None:
                arms.add(lit)

    for m in re.finditer(r'_mode\s*==\s*', masked):
        lit = literal_in(text, m.end(), m.end() + 80)
        if lit is not None:
            arms.add(lit)

    for m in re.finditer(r'_mode\s+is\s+', masked):
        rest = text[m.end():]
        for cm in re.finditer(r'^\s*"([^"]+)"(?:\s+or\s+"([^"]+)")*', rest):
            for g in cm.groups():
                if g is not None:
                    arms.add(g)
            break

    for a, b in _switch_regions(masked, r'_mode\s+switch\s*\{'):
        for lm in re.finditer(r'"([a-zA-Z_0-9]+)"\s*=>', text[a:b]):
            arms.add(lm.group(1))

    return arms


def doc_modes(text):
    """Mode names from the header block: 'Modes (env LA_GATE_MODE' down to the
    first '// Run:' line; a doc row is '//   <name>[ (parenthetical)] — ...'."""
    lines = text.splitlines()
    start = None
    for idx, l in enumerate(lines):
        if "Modes (env LA_GATE_MODE" in l:
            start = idx
            break
    if start is None:
        return None
    modes, dup = [], []
    for l in lines[start:]:
        if l.startswith("// Run:"):
            break
        m = re.match(r"^// {3}([A-Za-z_][A-Za-z0-9_]*)(?:\s*\([^)]*\))?\s+—", l)
        if m:
            (dup if m.group(1) in modes else modes).append(m.group(1))
    if dup:
        modes.append(f"__duplicate__{dup}")
    return modes


def allow_list(text):
    m = re.search(r"KnownModes\s*=\s*new[^{]*\{(.*?)\};", text, re.S)
    if not m:
        return None
    return re.findall(r'"([^"]+)"', m.group(1))


# ---- stage pairing (MC 10218): Run*Stage bodies <-> entry switch (_stage) ----

RIP_PREFIX, RIP_ENTRY = "RuntimeIntegrationProof", "RuntimeIntegrationProof.cs"
# MC 10258 (DA F2 / ARCH P3-B): ANY return type may precede a Run*Stage def —
# the old void|bool-only form went FALSE-RED ("DEAD dispatch arm") on an
# int-returning stage WITH its arm and SILENT-GREEN on one WITHOUT it. The
# signature suffix (closing paren, then '=>' or '{') keeps plain CALLS out of
# the def set, so e.g. "return RunGhostStage();" can never mask its own dead
# arm; the leading type-token sequence (identifiers, generics, arrays, ?)
# requires a return type to be present.
_TYPE_TOKEN = r"[A-Za-z_][\w<>,\[\]\.?]*"
STAGE_DEF_RE = re.compile(
    rf"\b{_TYPE_TOKEN}(?:\s+{_TYPE_TOKEN})*\s+"
    r"(Run[A-Za-z0-9_]*Stage)\s*\([^()]*\)\s*(?:=>|\{)")
STAGE_CALL_RE = re.compile(r"\b(Run[A-Za-z0-9_]*Stage)\s*\(")


def stage_pairing(skel):
    """(VIOLATION list, "n defs == m arms") for the RuntimeIntegrationProof
    family: every Run*Stage body — ANY return type, MC 10258 widened the def
    regex off void|bool; the Chain stages return bool and their arms can be
    fused away just the same — must have a call-site arm inside the entry's
    switch (_stage), and every arm call must have a body. 781ea66 lesson: a
    merge dropped the S20/S18 stage arms while the mode vocabulary stayed
    intact — this leg sees what doc/allow-list/arms equality cannot."""
    pdir = os.path.join(skel, "ci_proofs")
    epath = os.path.join(pdir, RIP_ENTRY)
    if not os.path.isdir(pdir) or not os.path.isfile(epath):
        return [], "family missing"    # entry-absent is named by check_project
    defs = {}    # stage name -> file it is defined in (first occurrence)
    for fname in sorted(os.listdir(pdir)):
        if not (fname.startswith(RIP_PREFIX) and fname.endswith(".cs")):
            continue
        with open(os.path.join(pdir, fname), encoding="utf-8") as f:
            masked = mask_code(f.read())
        for m in STAGE_DEF_RE.finditer(masked):
            defs.setdefault(m.group(1), fname)
    with open(epath, encoding="utf-8") as f:
        emasked = mask_code(f.read())
    arms = set()
    for a, b in _switch_regions(emasked, r"switch\s*\(\s*_stage\s*\)\s*\{"):
        arms.update(STAGE_CALL_RE.findall(emasked[a:b]))
    vios = [f"{RIP_PREFIX}: UNPAIRED stage body {name} (defined in {defs[name]}) — "
            "no call-site arm in the entry switch (_stage) dispatch"
            for name in sorted(set(defs) - arms)]
    vios += [f"{RIP_PREFIX}: DEAD dispatch arm {name}() in the entry switch (_stage) — "
             "no Run*Stage body is defined in ci_proofs"
             for name in sorted(arms - set(defs))]
    return vios, f"{len(defs)} stage bodies == {len(arms)} entry arms"


def check_project(skel):
    """Return the list of VIOLATION strings for the skeleton dir skel."""
    pdir = os.path.join(skel, "ci_proofs")
    vios = []
    if not os.path.isdir(pdir):
        return [f"ci_proofs/ not found under {skel}"]
    for name, entry, prefix, has_allow in PROOFS:
        epath = os.path.join(pdir, entry)
        if not os.path.isfile(epath):
            vios.append(f"{name}: entry file {entry} not found")
            continue
        with open(epath, encoding="utf-8") as f:
            etext = f.read()
        files = sorted(
            os.path.join(pdir, f) for f in os.listdir(pdir)
            if f.startswith(prefix + ".") and f.endswith(".cs")
        )
        arms = set()
        for fp in files:
            with open(fp, encoding="utf-8") as f:
                arms |= dispatch_arms(f.read())
        doc = doc_modes(etext)
        allow = allow_list(etext) if has_allow else None
        if doc is None:
            vios.append(f"{name}: header 'Modes (env LA_GATE_MODE' doc block not found")
        else:
            dups = [d for d in doc if str(d).startswith("__duplicate__")]
            if dups:
                vios.append(f"{name}: duplicate header doc mode rows {dups}")
            docset = {d for d in doc if not str(d).startswith("__duplicate__")}
            if docset != arms:
                vios.append(
                    f"{name}: dispatch arms != header doc — "
                    f"arms-only={sorted(arms - docset)} doc-only={sorted(docset - arms)}"
                )
        if has_allow:
            if allow is None:
                vios.append(f"{name}: allow-list (KnownModes) not found in {entry}")
            elif set(allow) != arms:
                vios.append(
                    f"{name}: KnownModes allow-list != dispatch arms — "
                    f"allow-only={sorted(set(allow) - arms)} arms-only={sorted(arms - set(allow))}"
                )
        if "unknown mode" not in etext:
            vios.append(
                f"{name}: unknown-mode fail-safe guard missing (must halt an "
                "unrecognized LA_GATE_MODE by name with exit 1 — roster idiom)"
            )
    pvios, _ = stage_pairing(skel)
    vios += pvios
    return vios


# ---- --selftest: planted-bad RED demos on TEMP copies (tree never touched) --

PLANTS = [
    # (edits [(file, find, replace)...], planted symbol, proof it names under,
    #  expectation). "RED": the plant must produce a VIOLATION naming its
    #  symbol under its proof. "GREEN" (MC 10258): the planted def+arm PAIR
    #  must introduce NO violation at all.
    ([("P1FixProof.cs",
       'default: Fail($"unknown mode {_mode}"); return true;',
       'case "planted_no_doc": _stage = 10; break;\n'
       '                    default: Fail($"unknown mode {_mode}"); return true;')],
     "planted_no_doc", "P1FixProof", "RED"),
    ([("ZoneBossProof.cs",
       'default: Fail($"unknown mode {_mode}"); return true;',
       'case "planted_no_doc": _stage = 10; break;\n'
       '                    default: Fail($"unknown mode {_mode}"); return true;')],
     "planted_no_doc", "ZoneBossProof", "RED"),
    ([("RuntimeIntegrationProof.cs",
       '"positive", "no_bus",',
       '"positive", "planted_allow_only", "no_bus",')],
     "planted_allow_only", "RuntimeIntegrationProof", "RED"),
    # MC 10218 pairing leg: an unpaired Run*Stage BODY (no entry arm) and a
    # DEAD entry arm (call with no body) — both on temp copies, tree untouched.
    ([("RuntimeIntegrationProof.Chain.cs",
       "private bool RunMoveStage()",
       "private bool RunPlantedUnpairedStage() => false;\n\n    private bool RunMoveStage()")],
     "RunPlantedUnpairedStage", "RuntimeIntegrationProof", "RED"),
    ([("RuntimeIntegrationProof.cs",
       "case 95: RunJuiceStage(); break;",
       "case 94: RunPlantedDeadArmStage(); break;\n            case 95: RunJuiceStage(); break;")],
     "RunPlantedDeadArmStage", "RuntimeIntegrationProof", "RED"),
    # MC 10258 off-type both-ways (DA plant D class, both directions): a PAIRED
    # int-returning stage body must stay GREEN; the same off-type body WITHOUT
    # its arm must go RED UNPAIRED. Temp copies, tree untouched.
    ([("RuntimeIntegrationProof.Chain.cs",
       "private bool RunMoveStage()",
       "private int RunPlantedIntReturnStage() => 0;\n\n    private bool RunMoveStage()"),
      ("RuntimeIntegrationProof.cs",
       "case 95: RunJuiceStage(); break;",
       "case 93: RunPlantedIntReturnStage(); break;\n            case 95: RunJuiceStage(); break;")],
     "RunPlantedIntReturnStage", "RuntimeIntegrationProof", "GREEN"),
    ([("RuntimeIntegrationProof.Chain.cs",
       "private bool RunMoveStage()",
       "private int RunPlantedIntUnpairedStage() => 0;\n\n    private bool RunMoveStage()")],
     "RunPlantedIntUnpairedStage", "RuntimeIntegrationProof", "RED"),
]


def selftest(skel):
    ok = True
    base = os.path.join(skel, "ci_proofs")
    for i, (edits, mode, proof, expect) in enumerate(PLANTS):
        tmp = tempfile.mkdtemp(prefix=f"la-modesets-{i}-")
        try:
            shutil.copytree(base, os.path.join(tmp, "ci_proofs"))
            bad_site = None
            for fname, needle, repl in edits:
                path = os.path.join(tmp, "ci_proofs", fname)
                with open(path, encoding="utf-8") as f:
                    text = f.read()
                if text.count(needle) != 1:
                    bad_site = fname
                    break
                with open(path, "w", encoding="utf-8") as f:
                    f.write(text.replace(needle, repl))
            if bad_site:
                print(f"MODE_SETS_CHECK: SELFTEST: FAIL: plant site not unique in {bad_site}")
                ok = False
                continue
            control = check_project(tmp)
            hit = [v for v in control if mode in v and proof in v]
            at = " + ".join(f[0] for f in edits)
            if expect == "GREEN":
                if control:
                    print(f"MODE_SETS_CHECK: SELFTEST: FAIL: planted paired {mode} in {at} "
                          f"expected GREEN, got {len(control)} violation(s): {control[0]}")
                    ok = False
                else:
                    print(f"MODE_SETS_CHECK: SELFTEST: planted paired {mode} in {at}: "
                          "GREEN as expected — off-type body and its entry arm pair")
            elif control and hit:
                print(f"MODE_SETS_CHECK: SELFTEST: planted {mode} in {at}: "
                      f"RED as expected -> {hit[0]}")
            else:
                print(f"MODE_SETS_CHECK: SELFTEST: FAIL: planted {mode} in {at} "
                      f"did NOT produce its named VIOLATION (got {len(control)} violations)")
                ok = False
        finally:
            shutil.rmtree(tmp, ignore_errors=True)
    return ok


def main(argv):
    skel, do_selftest, i = os.path.dirname(os.path.dirname(os.path.abspath(__file__))), False, 0
    while i < len(argv):
        if argv[i] == "--project":
            i += 1
            skel = argv[i]
        elif argv[i] == "--selftest":
            do_selftest = True
        else:
            print(f"MODE_SETS_CHECK: unknown arg {argv[i]}")
            return 2
        i += 1

    vios = check_project(skel)
    for v in vios:
        print(f"MODE_SETS_CHECK: VIOLATION: {v}")
    counts = ", ".join(f"{n}={len(set(doc_modes(open(os.path.join(skel, 'ci_proofs', e), encoding='utf-8').read()) or []))}"
                       for n, e, _, _ in PROOFS)
    if vios:
        print(f"MODE_SETS_CHECK: FAIL ({len(vios)} violation(s)) — {counts}")
        return 1
    print(f"MODE_SETS_CHECK: GREEN — {len(PROOFS)} dispatching proofs, "
          f"header-doc == allow-list == dispatch arms ({counts})")
    _, pc = stage_pairing(skel)
    print(f"MODE_SETS_CHECK: PAIRING OK — {RIP_PREFIX}: {pc}")
    if do_selftest:
        return 0 if selftest(skel) else 1
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
