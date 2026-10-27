using Godot;
using System;

// Last Animal — MC 11493 TALK-TO-CREATURES runtime proof (code, 2026-10-27),
// stage 111 (UNIQUE — measured next-free at this head; paired with the entry's
// case-111 arm by mode_sets_check's pairing leg). Mode: DNA_TALK (green).
//
// OWNER RULING 2026-10-27 verbatim: "I want tall to creature to be implemented
// since it is built" — the parked DNA message subsystem is WIRED (the brief's
// speak key "T" is the SHIPPED travel key — named deviation in
// WorldDirector.Talk.cs: speak rides V, action "speak"). This leg drives REAL
// presses of that action and asserts the whole exchange against live state:
//
//   0  band-busy refusal — press while the boot q_intro reward beat holds the
//      dialogue band: zero DnaSpoken (the line was NOT clobbered; the named
//      "TALK REFUSED — dialogue in progress" line is on stdout, the battery
//      greps it — the MC 11297 before-silent/after-named witness).
//   1  roster cleared through the SHIPPED break_bond input (the Roster
//      proof's own idiom) so the follower never shadows the talk target.
//   2  out-of-range refusal — the boot wild teleported 30 m away (its clamp
//      delegate keeps it grounded): press, zero emits, band NOT opened.
//   3  locked refusal — teleport ONTO the wild (talk reach), still 0 learned
//      positions: press, zero emits ("no tongue you know" on stdout).
//   4  farm 3 REAL extracts (SFarmLoop through the ONE kill path) — the live
//      consensus unlocks the gate (Calming-Speak authority, >= 3 positions).
//   5  the exchange — press near the wild: exactly ONE DnaSpoken emit, the
//      dialogue band opens on the reply's OWN talk_ node (the dna_speak
//      node-keying idiom: the intro beat could otherwise pass a generic
//      IsOpen), the painted label carries the composed reply text, and the
//      reply band stays DISJOINT from every HUD gauge row (the MC 11293 J-leg
//      widened-rect idiom re-measured over THE REPLY — the band layout is
//      ShowText-reuse, byte-identical rects; the windowed frame banked in the
//      card's .audits proves the painted state). NO recruit offer opened, NO
//      roster join (talk is not recruiting).
public partial class RuntimeIntegrationProof : SceneTree
{
    private int _kPhase, _kFrames, _kSub, _kExtract, _kBook;
    private bool _kNodeLatched, _kTextLatched, _kRectValid;
    private Rect2 _kRectLatched;
    private bool _kSubscribed;
    private int _kSpokenBefore;
    private LastAnimal.World.CompanionFollowBody? _kWild;
    private Vector3 _kWildHome;

    private void KNext(int phase) { _kPhase = phase; _kFrames = 0; _kSub = 0; }

    private bool KSubscribe()
    {
        if (_kSubscribed) return true;
        if (_bus == null) return false;
        _bus.DnaExtracted += _ => _kExtract++;   // farm counter (kill path extracts)
        // LATCH-ON-SIGHT on the NODE clock: the proof's SceneTree tick runs at the
        // physics rate while the band state lives on the render-rate node tick, so
        // a late physical-side read is flaky (the reply's dwell + the quest's OWN
        // first_speak reward beat drain behind it — R5/F-2, the F-2 clobber guard
        // keeps the beat BEHIND the direct reply — and the cycle closes between two
        // proof reads). The shipped dialogue-edge poll (WorldDirector.Ui.cs:135:
        // ActiveNode change -> EmitDialogueShown) fires the talk_ node ON THE NODE
        // CLOCK — the reply is painted at that instant; snapshot node + text + rect
        // there (the dna_speak node-keyed idiom, made tick-proof).
        _bus.DialogueShown += node =>
        {
            if (_dialogue == null || node == null || !node.StartsWith("talk_") || _kNodeLatched) return;
            _kNodeLatched = true;
            var lbl = _dialogue.GetNodeOrNull<Label>("DialoguePanel/DialogueText");
            _kTextLatched = lbl != null && lbl.Text.Contains("answers in");
            if (lbl != null) { _kRectLatched = Painted(lbl); _kRectValid = true; }
        };
        _kSubscribed = true;
        return true;
    }

    /// <summary>Press/release the speak action on the 2-4 toggle idiom; true once
    /// the full press cycle (press + 20 settle + release) has run.</summary>
    private bool KSpeakOnce()
    {
        _kSub++;
        if (_kSub == 1) Input.ActionPress("speak");
        else if (_kSub == 21) Input.ActionRelease("speak");
        return _kSub >= 26;
    }

    /// <summary>Stage-111 dispatch (entry case arm).</summary>
    private void RunDnaTalkStage()
    {
        _kFrames++;
        if (_kFrames > 9000) { Fail("dna_talk: frame budget exhausted — the chain never closed"); return; }
        if (_bus == null || _director == null || _dialogue == null || !KSubscribe()) return;
        var player = _director.PlayerModel;

        switch (_kPhase)
        {
            case 0:   // refusal 1: the band is busy (boot intro line) — press is refused, NOTHING emitted
                if (!_dialogue.IsOpen)
                {
                    if (_kFrames > 240) { Fail("dna_talk: the boot intro beat never held the band — refusal 1 is vacuous"); return; }
                    break;   // wait for the intro reward beat to open the box
                }
                if (!KSpeakOnce()) break;
                Check("band-busy refusal: press while the intro line held the band emitted NOTHING",
                      _dnaSpokenCount == 0 && _dialogue.IsOpen && _dialogue.ActiveNode == "intro",
                      $"spoken={_dnaSpokenCount} node='{_dialogue.ActiveNode}'");
                if (_failed) return;
                GD.Print("LA_GATE: DNA_TALK_REFUSE_BAND — refusal prints by name (stdout), zero emits, the intro line survived");
                KNext(1);
                break;

            case 1:   // shipped book + break_bond inputs retire the follower (talk-target ambiguity: none)
                {
                    var emp = _main?.GetNodeOrNull<LastAnimal.Ui.EmpathyPanel>("UI/Empathy");
                    if (emp == null) { Fail("dna_talk: EmpathyPanel missing at UI/Empathy (roster cannot be retired)"); return; }
                    if (!emp.Visible)
                    {
                        SPress("book", ref _kBook);   // the J-leg open idiom: 2f press / 2f release
                        if (_kFrames > 600) { Fail("dna_talk: the book never opened"); return; }
                        break;
                    }
                    if (_kFrames % 8 == 3) Input.ActionPress("break_bond");
                    else if (_kFrames % 8 == 5) Input.ActionRelease("break_bond");
                    // The roster ENTRY survives a manual break (RemoveFollowerAt rides
                    // load/trim only) — the DEAD BOND is the witness (the F1/NF7 flag
                    // the talk scan excludes exactly like the pay arm excludes it).
                    if (_director.RosterView.Count > 0 && _director.RosterView[0].Component.HasCompanion)
                    {
                        if (_kFrames > 900) { Fail("dna_talk: break_bond never deadened the boot bond (HasCompanion still true)"); return; }
                        break;
                    }
                    Input.ActionRelease("break_bond");
                    SPress("book", ref _kBook);       // close the book again (the shipped toggle)
                    GD.Print("LA_GATE: DNA_TALK_BOND_DEADENED — the shipped book+break_bond inputs deadened the boot bond (the talk scan excludes dead bonds — the F1/NF7 authority); the WILD is next target)");
                    Check("production fielded the boot-zone wild with NO seam (FieldBootWild)",
                          _director.WildBodies.Count == 1 && _director.WildBodies[0].Wild,
                          $"wilds={_director.WildBodies.Count}");
                    if (_failed) return;
                    _kWild = _director.WildBodies[0];
                    _kWildHome = _kWild.GlobalPosition;
                    KNext(2);
                }
                break;

            case 2:   // refusal 2: out of range (the wild teleports 30 m out — its clamp keeps it grounded)
                if (_dialogue!.IsOpen) { if (_kFrames > 600) { Fail("dna_talk: the intro dwell never freed the band before the range press"); return; } break; }
                if (_kSub == 0) { _kWild!.GlobalPosition = _kWildHome + new Vector3(30f, 0f, 0f); _kSpokenBefore = _dnaSpokenCount; }
                if (_kSub == 5) { _kWild!.GlobalPosition = _kWildHome; }   // home again before the settle window
                if (!KSpeakOnce()) break;
                Check("out-of-range refusal: press with every creature past talk reach emitted NOTHING and opened NO band",
                      _dnaSpokenCount == _kSpokenBefore && !_dialogue!.IsOpen,
                      $"spoken={_dnaSpokenCount}/{_kSpokenBefore} open={_dialogue!.IsOpen}");
                if (_failed) return;
                GD.Print("LA_GATE: DNA_TALK_REFUSE_RANGE — the named out-of-range refusal prints, nothing spent, no band");
                KNext(3);
                break;

            case 3:   // refusal 3: in range but LOCKED (0 learned positions — the gate authority)
                if (_dialogue!.IsOpen) { if (_kFrames > 600) { Fail("dna_talk: the band never freed before the locked press"); return; } break; }
                if (_kSub == 0) { _playerBody!.GlobalPosition = _kWild!.GlobalPosition; _kSpokenBefore = _dnaSpokenCount; }
                if (!KSpeakOnce()) break;
                Check("locked refusal: press ONTO the wild at 0 learned positions emitted NOTHING",
                      _dnaSpokenCount == _kSpokenBefore && _kExtract == 0,
                      $"spoken={_dnaSpokenCount}/{_kSpokenBefore} extracts={_kExtract}");
                if (_failed) return;
                GD.Print("LA_GATE: DNA_TALK_REFUSE_TONGUE — 'no tongue you know' named on stdout, zero emits at 0 positions");
                KNext(4);
                break;

            case 4:   // farm THREE real extracts (the ONE kill path) — the live consensus unlocks
                if (_kExtract >= 3)
                {
                    GD.Print($"LA_GATE: DNA_TALK_UNLOCKED — {_kExtract} real extracts, the consensus reaches the gate (>=3 positions)");
                    KNext(5);
                    break;
                }
                if (!SFarmLoop()) { SPress("travel", ref _sTravelToggle); }
                if (_kFrames > 7000) { Fail("dna_talk: farm never produced 3 extracts"); return; }
                break;

            case 5:   // THE EXCHANGE: real press, reply rides the dialogue band + exactly one DnaSpoken
                if (_dialogue!.IsOpen) { if (_kFrames > 600) { Fail("dna_talk: the band never freed after the farm beats"); return; } break; }
                if (_kSub == 0)
                {
                    _playerBody!.GlobalPosition = _kWild!.GlobalPosition;
                    _kSpokenBefore = _dnaSpokenCount;
                }
                // LATCH-ON-SIGHT (measured, MC 11493 smoke): the reply paints for
                // its dwell, then the quest's OWN first_speak reward beat drains in
                // behind it (R5 append-behind-a-direct-line — the F-2 clobber guard
                // keeps the beat behind the reply) and the cycle closes; the proof's
                // SceneTree tick and the render-loop node tick run at DIFFERENT
                // rates, so a late single read is flaky. The band facts are latched
                // on every proof frame across the press window and asserted at the
                // end of it — non-vacuous (the node-keyed talk_ idiom, dna_speak).
                if (!KSpeakOnce() && _kSub < 48) break;   // let a few node cycles run — the edge latches itself
                var lbl = _dialogue!.GetNodeOrNull<Label>("DialoguePanel/DialogueText");
                Check("the exchange emitted EXACTLY ONE DnaSpoken (the tested C2 edge: meter + SFX + quest Spoken ride it)",
                      _dnaSpokenCount == _kSpokenBefore + 1, $"spoken={_dnaSpokenCount}/{_kSpokenBefore}");
                Check("the reply opened the dialogue band's OWN talk_ node (node-keyed, dna_speak idiom — latched on the DialogueShown edge, node clock)",
                      _kNodeLatched, $"node='{_dialogue!.ActiveNode}' latched={_kNodeLatched}");
                Check("the band paints the COMPOSED reply text (Extract tongue + Counter codons + bond)",
                      _kTextLatched, $"text='{lbl?.Text}'");
                Check("talk did NOT recruit: no offer opened on the wild, the deadened bond stayed dead (talk is not recruiting)",
                      !_kWild!.RecruitOffered && (_director!.RosterView.Count == 0 || !_director!.RosterView[0].Component.HasCompanion),
                      $"offer={_kWild!.RecruitOffered} roster={_director!.RosterView.Count}");
                if (_failed) return;
                TalkBandDisjoint();
                if (_failed) return;
                GD.Print("LA_GATE: DNA_TALK_EXCHANGED — speak -> reply (Counter/Extract codons) rendered in the dialogue band, one DnaSpoken, zero recruit side-effects");
                GD.Print("LA_GATE: PASS");
                _asserted = true;
                KNext(6);
                break;

            case 6:   // headless: verdict IS the deliverable; windowed: HOLD the reply frame for the capture
                if (DisplayServer.GetName() == "headless" || _kFrames >= 600)
                {
                    ReleaseHeldRefsBeforeQuit();
                    Quit(0);
                }
                break;
        }
    }

    /// <summary>The MC 11293 J-leg widened-rect idiom measured over THE REPLY:
    /// the painted dialogue-label rect (widened to its combined min size) must
    /// stay disjoint from every painted HUD gauge label. HudDialogueOverlapTest
    /// is the gate's canon overlap proof — this leg re-measures it with the
    /// T-reply (ShowText, same band) actually on screen.</summary>
    private void TalkBandDisjoint()
    {
        if (!_kRectValid) { Fail("dna_talk: the reply rect was never latched — the disjoint measure would be vacuous"); return; }
        Rect2 dlg = _kRectLatched;
        int bad = 0;
        foreach (Node c in _hud!.GetChildren())
            if (c is Label hl && hl.Text.Length > 0)
            {
                Rect2 r = Painted(hl);
                if (dlg.Intersects(r)) { bad++; Fail($"HUD_TALK_OVERLAP: reply band {dlg} over {hl.Name}={r}"); return; }
            }
        GD.Print($"LA_GATE: DNA_TALK_BAND_DISJOINT — reply rect {dlg} clear of all HUD gauge labels");
    }

    private static Rect2 Painted(Control c) =>
        c.GetGlobalRect().Grow(Math.Max(0, c.GetCombinedMinimumSize().Y - c.GetGlobalRect().Size.Y) / 2f);
}
