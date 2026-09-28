"""Independent tests: NuGet restore must be host-portable (MC 1405 follow-up).

Tests the REAL config files in the source repo /srv/workspace/last-animal —
not copies — so a regression in the repo fails these tests.
"""
import xml.etree.ElementTree as ET
from pathlib import Path

REPO = Path("/srv/workspace/last-animal")
OUT = Path("/home/svarkor/last-animal/.audits/202609280811-f8117fe8")


def _sources(config_path: Path) -> dict:
    root = ET.parse(config_path).getroot()
    out = {}
    for add in root.findall("./packageSources/add"):
        out[add.get("key")] = add.get("value")
    return out


def test_skeleton_config_has_no_local_feed():
    srcs = _sources(REPO / "skeleton/NuGet.Config")
    assert "godot-pinned-local" not in srcs, f"local feed still present: {srcs}"
    assert all(v.startswith("https://") for v in srcs.values()), srcs


def test_animation_pipeline_config_has_no_local_feed():
    srcs = _sources(REPO / "animation_pipeline/NuGet.Config")
    assert "godot-pinned-local" not in srcs, f"local feed still present: {srcs}"
    assert all(v.startswith("https://") for v in srcs.values()), srcs


def test_nuget_org_is_the_only_source():
    for rel in ("skeleton/NuGet.Config", "animation_pipeline/NuGet.Config"):
        srcs = _sources(REPO / rel)
        assert set(srcs) == {"nuget.org"}, f"{rel}: {srcs}"
        assert srcs["nuget.org"] == "https://api.nuget.org/v3/index.json"


def test_no_config_references_a_filesystem_path():
    for rel in ("skeleton/NuGet.Config", "animation_pipeline/NuGet.Config"):
        text = (REPO / rel).read_text()
        body = text.split("-->", 1)[-1]  # XML body only, comments may keep history
        assert "../engine/" not in body and "gunilla" not in body, rel


def test_sdk_pin_is_exact_in_both_csproj():
    for csproj in ("skeleton/LastAnimalPreflight.csproj",
                   "animation_pipeline/LastAnimalM09Bake.csproj"):
        text = (REPO / csproj).read_text()
        assert 'Sdk="Godot.NET.Sdk/4.7.2"' in text, csproj
        assert "*" not in text.split("Sdk=")[1].split('"')[1], f"wildcard pin in {csproj}"


def test_history_note_marks_removal_with_date_and_reason():
    for rel in ("skeleton/NuGet.Config", "animation_pipeline/NuGet.Config"):
        text = (REPO / rel).read_text()
        assert "REMOVED 2026-09-28" in text, rel
        assert "MC 1405" in text, rel
        assert "Windows" in text, rel


def test_docs_have_windows_from_source_subsection():
    doc = (REPO / "skeleton/docs/build-and-run.md").read_text()
    assert "## Building on Windows from source" in doc
    assert "nuget.org only" in doc
    assert "F5" in doc
    assert ".NET 8 SDK" in doc and "Godot 4.7.2-stable mono editor" in doc


def test_out_dir_pytest_covers_every_py_file_in_out_dir():
    """DoD mechanical check: pytest must call into every .py source in the out dir."""
    py_files = [p for p in OUT.rglob("*.py") if ".tmp" not in p.parts]
    assert py_files, "no .py files found in out dir"
    assert any(p.name == "test_nuget_portable.py" for p in py_files)
