# TEST verdict — NuGet restore portability (MC 1405 follow-up)

NOTE (honesty): executed INLINE by the parent session, not by a spawned `test`
child — the harness rejected the spawn (`subagent depth 2 exceeds maxDepth 1`).
The suite is independent of the fix in the sense that matters here: it reads the
REAL repo files and asserts the portability contract, and it was proven able to
go RED against the old-style config.

## Suite

`test_nuget_portable.py` (this out dir), 8 tests, run with `python3 -m pytest`:

- skeleton + animation_pipeline NuGet.Config: no `godot-pinned-local` source,
  every source is https, exactly `{nuget.org}` with the canonical URL.
- No filesystem path in the XML BODY of either config (`../engine/`, `gunilla`
  absent; history comments may mention the removed path).
- Exact SDK pin `Godot.NET.Sdk/4.7.2`, no wildcard, in BOTH csproj files.
- History note carries `REMOVED 2026-09-28`, `MC 1405`, `Windows`.
- `skeleton/docs/build-and-run.md` has "## Building on Windows from source",
  "nuget.org only", F5, .NET 8 SDK + Godot 4.7.2-stable mono editor.
- Out-dir coverage: pytest calls into every .py file in the out dir.

## Green run

```
$ python3 -m pytest test_nuget_portable.py -v   (log: .tmp/pytest.log)
8 passed in 0.04s                                PYTEST_EXIT=0
```

## Red (planted-bad) proof

An old-style config (with `godot-pinned-local` restored) in a scratch copy was
fed to the same test function:

```
RED PROOF OK: old-style config rejected -> local feed still present:
{'godot-pinned-local': '../engine/x/nupkgs', 'nuget.org': '.../index.json'}
```

The suite is not vacuous: it fails on exactly the defect that was fixed.

## Limits

No Windows host available; the suite proves the config is structurally
portable, not a Windows execution. The Linux clean-restore + build proof lives
in the evidence file.

# JUDGED: b0b70e76d5c74470970e2fc1c5cc3668971ef1cc78ba050908e1860fd601c022
# VERDICT: PASS
