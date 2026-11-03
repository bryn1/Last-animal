# DONE — MC 11891, dialogue band reflows to fit any window (owner ruling `B`)

Shipped on `bryn1/master` at `43c35b6`, verdicts banked at `3accd90`.

## What changed for the player
The dialogue band derives its geometry from the live visible rect and re-layouts on viewport resize, so at any window width the text stays inside the window. At 800 px the band reflows full-width above the Skills/TAB panel (right edge 712 ≤ 800) instead of being anchored at a constant span and cut off by the window edge. At the shipped default resolution the band rect is byte-identical to before, so nothing moved for a full-size player. No minimum window size was introduced.

The constant-span `PanelViewport` fallback the bug lived in is stood down for the band. Its one remaining user is `EmpathyPanel`, named in code, LEDGER and `MC 11972` — not left as a silent second path.

## What the gate now proves
`skeleton/ci/ui_test.sh` carries `DIALOGUE_IN_VIEW`, which the old gate lacked. It has been seen failing on an independently planted fault, twice, byte-identically, with the exit code taken from the game's own quit rather than from a capture race. The headless face that measured a 64×64 viewport fiction — the root of the original false green — is retired loudly. `ui_test.sh` was split into an entry plus `ui_pixel_battery.sh` and `ui_tracker_battery.sh` at 273/177/191 lines; every leg traced body-for-body from the parent.

## Verdicts
Independent tester: **PASS** (`TEST-verdict.md`, hash-pinned). Adversary: **SHIP** (`DA-verdict.md`, hash-pinned), no P0/P1. Both ran the real engine themselves and treated the producer's logs as claims.

## Accepted imperfections, each with a card
- Below the `MinBandWidth=96` floor (~110 px and under) the band overshoots the edge and a wrapped label can reach the TAB panel. Both fail loudly by name. → **MC 11976**, which also carries the larger gap: the gate has never measured window HEIGHT, so a band-HUD collision from below is inexpressible today.
- `EmpathyPanel` is fully invisible below ~1136 px and its disjointness assert passes vacuously on off-screen rects. → **MC 11972**, severity raised, proved by this run's own committed green 800 px frame.
- `DialogueSystem.cs` at 279 lines sits over the 250-line target.
- The evidence pattern's `gate-*.log` negation admits raw transcripts; only a naming convention kept two 280 KB ones out. → appended to **MC 11840**.
- The frame-vs-verdict question is closed by argument the adversary verified (verdict markers are grepped from the frame run's own log; a divergence between the two runs can only produce a false red, never a green-wash), with one hardening left: the rc run asserts `GAME_RC=0` alone, and rc 0 is also a compose-failure exit.
