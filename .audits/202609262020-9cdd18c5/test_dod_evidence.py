"""DoD evidence tests for run 202609262020-9cdd18c5 (MC 1388 bridge_mvp gate fix).

Grades the evidence file claims mechanically: root-cause classification present,
planted-bad red run recorded, 3 consecutive green runs, VERIFY_EXIT=0 lines,
commit hash. Evidence: .audits/202609261400-bridgefix/evidence.md
"""
import pathlib
import re

EVIDENCE = pathlib.Path(
    "/srv/workspace/last-animal/.audits/202609261400-bridgefix/evidence.md"
)
PROOF = pathlib.Path("/srv/workspace/last-animal/skeleton/ci_proofs/BridgeMvpProof.cs")


def _text() -> str:
    return EVIDENCE.read_text(encoding="utf-8")


def test_evidence_file_exists_and_readable():
    assert EVIDENCE.is_file(), f"missing evidence file: {EVIDENCE}"
    assert len(_text()) > 500, "evidence file suspiciously short"


def test_root_cause_classification_present():
    text = _text()
    assert "STALE PROOF SCENARIO" in text, "no explicit classification"
    assert "a7829ed" in text and "65b769d" in text, "git evidence missing"


def test_planted_bad_red_run_recorded():
    text = _text()
    assert "RED_EXIT=1" in text, "no red-run exit line"
    assert "GATE FAIL" in text, "red run did not show GATE FAIL"


def test_three_consecutive_green_runs():
    text = _text()
    greens = re.findall(r"GREEN\d_EXIT=0", text)
    assert len(greens) >= 3, f"expected 3 green exit lines, found {greens}"
    assert text.count("GATE PASS") >= 3, "fewer than 3 GATE PASS lines"


def test_verify_exit_zero_for_build_and_runtime():
    text = _text()
    assert "BUILD_EXIT=0" in text, "build gate not verified"
    assert "RUNTIME_EXIT=0" in text, "runtime integration gate not verified"


def test_commit_hash_present():
    assert re.search(r"\bdd1dbcb\b", _text()), "commit hash missing"


def test_fix_present_in_proof_source():
    src = PROOF.read_text(encoding="utf-8")
    assert "follow baseline recaptured after player reposition" in src, (
        "fix not present in BridgeMvpProof.cs"
    )
    assert "if (moved < 0.2f || d1 > _followDist0 - 0.25f)" in src, (
        "planted-bad assertion not restored"
    )
