using Godot;
using LastAnimal.Core;
using LastAnimal.Core.Framework;
using LastAnimal.World;

// SIZE reason (file hygiene law, MC 11018/S45): 343 l by wc AT this head,
// past the ~250 house mark. Kept whole: this is ONE stage body — the eight
// phases share witnessed bases and bit-comparisons (the settled witness
// feeds the rebase pin feeds the pulse rest feeds the clearance release),
// and splitting a stage body mid-leg across files has no precedent in the
// pairing gate (one Run*Stage body == one dispatch arm).

// Last Animal — MC 11018 / 10026.44.6 S45 fixed-iso-camera-rig proof
// (stage 107, mode CAMERA_RIG_ISO).
//
// Proves the S45 rig-mode contract on the LIVE playable scene, spawned in
// TWO ways by the battery (the launch flag and its expectation arrive on
// SEPARATE channels — the game reads the cmdline user arg, this leg reads
// LA_RIG_EXPECT — so "flag honored" and "default OFF" are non-circular
// asserts, one input never proves itself):
//   A) plain  (no flag, no LA_RIG_EXPECT): the rig MUST be Follow and the
//      lerp lag MUST be alive (teleport the target: the camera does NOT
//      sit on target+Offset next frame) — marker CAMERA_RIG_DEFAULT_OFF.
//   B) LA_RIG_EXPECT=fixed + `-- --iso-cam`: the rig MUST be FixedIso and:
//      CAMERA_RIG_EXACT_REBASE — teleport re-bases the camera BIT-EQUAL to
//      pivot+Offset with zero lag (a shipped-lerp plant stays RED: the lag
//      survives the teleport);
//      CAMERA_RIG_ROT_LOCKED   — orientation is BIT-identical across the
//      target move (a LookAt-per-frame plant jitters RED);
//      CAMERA_RIG_PULSE_ARMED / CAMERA_RIG_PULSE_AT_BASE — the shipped S20
//      pulse opens and returns BIT-EXACTLY on the FIXED base (the juice
//      retune holds: zero constants moved);
//      CAMERA_RIG_OCCL_CLAMPED / CAMERA_RIG_OCCL_RELEASED — a composed
//      STATIC box on the sight-line parks the camera inside the rest pose
//      (camera collision, the ONE clearance), and its removal returns the
//      camera BIT-EXACTLY onto pivot+Offset.
// Optional env LA_RIG_CAPTURE=<zone> (CAPTURE runs only, NOT a battery
// leg): after the rig pin the leg travel-walks to the zone (shipped Press-
// Travel idiom, park across every mount), asserts the mounted root, and
// PARKS FOREVER so graphical-test-helper lands a real framebuffer capture
// of the composed scene under this rig (frame-budget watchdog lifted for
// this run only — the helper's timeout is the watchdog).
// Spawning is off at boot (bus_emit quiet-leg idiom); the camera prints
// NOTHING but its boot rig line (F4-CMP) — every marker here is proof output.
//
// Run (follow side):  LA_GATE_MODE=CAMERA_RIG_ISO $GODOT --headless --path
//                      <proj> --script res://ci_proofs/RuntimeIntegrationProof.cs
// Run (fixed side):   LA_GATE_MODE=CAMERA_RIG_ISO LA_RIG_EXPECT=fixed \
//                      $GODOT --headless --path <proj> --script <proof> -- --iso-cam
public partial class RuntimeIntegrationProof : SceneTree
{
    private int _crPhase;
    private FollowCamera? _crCam;
    private bool _crExpectFixed;
    private string? _crCaptureZone;      // set by _Initialize from LA_RIG_CAPTURE (capture runs only)
    private Vector3 _crRestPos;          // bit-still base witnessed for the CURRENT pivot
    private Vector3 _crPrevPos;
    private Vector3 _crBootRotation;
    private int _crStillRun;
    private int _crPhaseStart;
    private int _crTravelToggle;
    private int _crPark;
    private int _crTravelStep;
    private StaticBody3D? _crOccluder;

    private const int CrStillWitnessFrames = 2;      // consecutive bit-stable
    private const int CrSettleBudgetFrames = 180;    // bit-still base witness
    private const int CrTeleportX = 3;               // m, deep inside meadow extents
    private const int CrFramesBudget = 40;           // per-phase watchdog
    private const int CrPulseBudgetFrames = 40;      // pulse walk (8f + margin)
    private const int CrParkFrames = 40;             // > 0.5 s cull + deferred bake (S27 bound)
    private const int CrTravelStepBudget = 900;      // per-travel watchdog (mount bake, ZoneMount bound)
    private const int CrMaxTravels = 4;              // one lap of the 4-zone cycle
    private const float CrLagFloorM = 0.05f;         // follow-rig lerp lag floor
    private const float CrClampFloorM = 0.5f;        // clamp must shorten the sight by >= this

    private void CrFail(string why) => Fail("CAMERA_RIG_ISO: " + why);

    private void RunCameraRigStage()
    {
        switch (_crPhase)
        {
            case 0:   // resolve + rig pin + freeze + bit-still rest witness A
            {
                _crCam = _main!.GetNodeOrNull<FollowCamera>("Camera");
                if (_crCam == null) { CrFail("FollowCamera absent at Main/Camera"); break; }
                if (_crCam.IsShaking || _crCam.IsPulsing || _crCam.IsFramingBoss)
                { CrFail("camera boots with a presentation window open"); break; }
                _crExpectFixed = System.Environment.GetEnvironmentVariable("LA_RIG_EXPECT") == "fixed";
                if (_crCam.RigIsFixedIso != _crExpectFixed)
                { CrFail($"boot rig {_crCam.Rig} does not match the spawn contract (LA_RIG_EXPECT={(_crExpectFixed ? "fixed" : "<unset>")}) — the camera's OWN boot read of the cmdline user flag decided {(_crCam.RigIsFixedIso ? "FixedIso" : "Follow")}"); break; }
                GD.Print($"LA_GATE: CAMERA_RIG_BOOT rig={(_crCam.RigIsFixedIso ? "fixed-iso" : "follow")} (expect {(_crExpectFixed ? "fixed" : "follow/follow-default")})");
                _playerBody!.SetPhysicsProcess(false);   // Motion/KillPulse bit-still idiom
                _crBootRotation = _crCam.GlobalRotation;
                _crPrevPos = _crCam.GlobalPosition;
                _crPhaseStart = _frames;
                _crPhase = 1;
                break;
            }

            case 1:   // witness: bit-stable base across CrStillWitnessFrames
            {
                var pos = _crCam!.GlobalPosition;
                _crStillRun = pos == _crPrevPos ? _crStillRun + 1 : 0;
                _crPrevPos = pos;
                if (_crStillRun < CrStillWitnessFrames)
                {
                    if (_frames - _crPhaseStart > CrSettleBudgetFrames)
                        CrFail("follow base never went bit-still after the freeze (base mover?)");
                    break;
                }
                _crRestPos = pos;
                GD.Print($"LA_GATE: CAMERA_RIG_SETTLED base=({_crRestPos.X:0.####},{_crRestPos.Y:0.####},{_crRestPos.Z:0.####})");
                _crPhase = 2;
                break;
            }

            case 2:   // teleport the target; the rig discriminator fires
            {
                var p = _playerBody!.GlobalPosition;
                _playerBody.GlobalPosition = p + new Vector3(CrTeleportX, 0f, 0f);
                GD.Print($"LA_GATE: camera-rig leg — target teleported +{CrTeleportX} m X (pivot mover)");
                _crPhaseStart = _frames;
                _crPhase = 3;
                break;
            }

            case 3:   // give the camera one process frame, then pin the rig
            {
                if (_frames - _crPhaseStart < 2) break;
                var cam = _crCam!;
                var want = _playerBody!.GlobalPosition + cam.Offset;
                var pos = cam.GlobalPosition;
                if (_crExpectFixed)
                {
                    if (pos != want)
                    { CrFail($"CAMERA_RIG_EXACT_REBASE: fixed rig lagged the teleport (pos={pos} != pivot+Offset={want})"); break; }
                    GD.Print($"LA_GATE: CAMERA_RIG_EXACT_REBASE — pivot+Offset bit-equal pos=({pos.X:0.####},{pos.Y:0.####},{pos.Z:0.####}) zero lag");
                    if (cam.GlobalRotation != _crBootRotation)
                    { CrFail($"CAMERA_RIG_ROT_LOCKED: orientation drifted across the target move (boot={_crBootRotation} now={cam.GlobalRotation})"); break; }
                    GD.Print($"LA_GATE: CAMERA_RIG_ROT_LOCKED — orientation bit-identical across the pivot mover rot=({cam.GlobalRotation.X:0.####},{cam.GlobalRotation.Y:0.####},{cam.GlobalRotation.Z:0.####})");
                    // re-witness the base at the NEW pivot (pulse/occlusion pins
                    // measure against it bit-exactly)
                    _crStillRun = 0; _crPrevPos = pos; _crPhaseStart = _frames;
                    _crPhase = 4;
                }
                else
                {
                    float lag = pos.DistanceTo(want);
                    if (lag < CrLagFloorM)
                    { CrFail($"CAMERA_RIG_DEFAULT_OFF: follow rig sat {lag:0.####} m off pivot+Offset — below the {CrLagFloorM} m lag floor (is the fixed rig the DEFAULT?)"); break; }
                    GD.Print($"LA_GATE: CAMERA_RIG_DEFAULT_OFF — follow rig alive, lerp lag {lag:0.###} m off pivot+Offset after the teleport (flag ABSENT -> shipped behaviour)");
                    if (_crCaptureZone != null) { _crPhase = 8; break; }
                    GD.Print("LA_GATE: PASS — S45 default-off verified (no flag -> follow rig, lerp path byte-shared with master)");
                    _asserted = true;
                    _crCam = null;
                    ReleaseHeldRefsBeforeQuit();
                    Quit(0);
                }
                break;
            }

            case 4:   // re-witness the bit-still base at the new pivot
            {
                var pos = _crCam!.GlobalPosition;
                _crStillRun = pos == _crPrevPos ? _crStillRun + 1 : 0;
                _crPrevPos = pos;
                if (_crStillRun < CrStillWitnessFrames)
                {
                    if (_frames - _crPhaseStart > CrSettleBudgetFrames)
                        CrFail("base never went bit-still at the post-teleport pivot");
                    break;
                }
                _crRestPos = pos;
                _crPhase = _crCaptureZone != null ? 8 : 5;
                break;
            }

            case 5:   // shipped S20 pulse holds on the fixed rig
            {
                _bus!.EmitDnaExtracted(new DnaSignature("s45-rig", "proof"));
                if (!_crCam!.IsPulsing) { CrFail("DnaExtracted emit did NOT arm the fixed rig (juice consumer lost)"); break; }
                GD.Print("LA_GATE: CAMERA_RIG_PULSE_ARMED — S20 pulse window open under the fixed rig");
                _crPhaseStart = _frames;
                _crPhase = 6;
                break;
            }

            case 6:   // walk to the exact rest on the fixed base
            {
                var pos = _crCam!.GlobalPosition;
                if (_crCam.IsPulsing || pos != _crRestPos)
                {
                    if (_frames - _crPhaseStart > CrPulseBudgetFrames)
                        CrFail("pulse window never closed / base never returned inside the walk budget");
                    break;
                }
                if (pos != _crRestPos)   // belt: the exact compare IS the assert
                { CrFail($"pulse return NOT bit-equal (pos={pos} rest={_crRestPos})"); break; }
                if (!_crCam.IsAtRestBase) { CrFail("camera not on its exact fixed base past the window"); break; }
                GD.Print($"LA_GATE: CAMERA_RIG_PULSE_AT_BASE — bit-equal return on the FIXED base pos=({pos.X:0.####},{pos.Y:0.####},{pos.Z:0.####})");
                _crPhase = 7;
                break;
            }

            case 7:   // occlusion clearance: compose a STATIC box on the sight-line
            {
                var sight = (_crRestPos - _playerBody!.GlobalPosition);
                var mid = _playerBody.GlobalPosition + sight * 0.5f + Vector3.Up * (RayEyMidpointM(sight));
                _crOccluder = new StaticBody3D { Name = "CrOccluder" };
                _crOccluder.AddChild(new CollisionShape3D { Shape = new BoxShape3D { Size = new Vector3(3f, 6f, 3f) } });
                _main!.AddChild(_crOccluder);
                _crOccluder.GlobalPosition = mid;
                GD.Print($"LA_GATE: camera-rig leg — static occluder composed at ({mid.X:0.##},{mid.Y:0.##},{mid.Z:0.##}) on the sight-line");
                _crPhaseStart = _frames;
                _crPhase = 20;
                break;
            }

            case 20:  // let physics sync the body, then measure the clamp
            {
                if (_frames - _crPhaseStart < 4) break;
                var pos = _crCam!.GlobalPosition;
                float restDist = _crRestPos.DistanceTo(_playerBody!.GlobalPosition);
                float nowDist = pos.DistanceTo(_playerBody!.GlobalPosition);
                if (!(nowDist < restDist - CrClampFloorM))
                { CrFail($"CAMERA_RIG_OCCL_CLAMPED: sight NOT shortened by >= {CrClampFloorM} m under the occluder (now={nowDist:0.###} rest={restDist:0.###}) — clearance dead?"); break; }
                GD.Print($"LA_GATE: CAMERA_RIG_OCCL_CLAMPED — camera collision parked the sight at {nowDist:0.###} m vs rest {restDist:0.###} m");
                _crPhase = 21;
                break;
            }

            case 21:  // remove the occluder; base must return BIT-EXACT
            {
                if (_crOccluder != null)
                {
                    _crOccluder.RemoveChild(_crOccluder.GetChild<CollisionShape3D>(0));
                    _main!.RemoveChild(_crOccluder);
                    _crOccluder.Dispose();
                    _crOccluder = null;
                }
                _crPhaseStart = _frames;
                _crPhase = 22;
                break;
            }

            case 22:
            {
                if (_frames - _crPhaseStart < 4) break;
                var pos = _crCam!.GlobalPosition;
                if (pos != _crRestPos || !_crCam.IsAtRestBase)
                { CrFail($"CAMERA_RIG_OCCL_RELEASED: camera did NOT return BIT-EXACTLY to pivot+Offset after the clearance released (pos={pos} rest={_crRestPos})"); break; }
                GD.Print($"LA_GATE: CAMERA_RIG_OCCL_RELEASED — bit-exact return to pivot+Offset pos=({pos.X:0.####},{pos.Y:0.####},{pos.Z:0.####}) clearance released clean");
                if (_crCaptureZone != null) { _crPhase = 8; break; }
                GD.Print("LA_GATE: PASS — S45 fixed rig verified (flag-honoured boot, exact rebase, locked orientation, pulse exact-rest, static camera collision with bit-exact release)");
                _asserted = true;
                _crCam = null;
                ReleaseHeldRefsBeforeQuit();
                Quit(0);
                break;
            }

            // ---- capture arm (env LA_RIG_CAPTURE=<zone>): pins ran, now
            // travel to the zone and PARK for the framebuffer capture ----
            case 8:
            {
                GD.Print($"LA_GATE: camera-rig CAPTURE — travel-walking to zone '{_crCaptureZone}' (rig={(_crCam!.RigIsFixedIso ? "fixed-iso" : "follow")})");
                _crTravelStep = 0; _crPark = 0; _crTravelToggle = 0;
                _crPhaseStart = _frames;
                _crPhase = 9;
                break;
            }

            case 9:   // one PressTravel idiom per step (ZoneMount verbatim behaviour)
            {
                if (_director!.CurrentZone == _crCaptureZone)
                {
                    var root = _director.MountedZoneRoot;
                    if (root == null || root.Name.ToString().ToLowerInvariant() != _crCaptureZone)
                    { CrFail($"capture parked in zone '{_director.CurrentZone}' but mounted root is {root?.Name.ToString() ?? "<none>"}"); break; }
                    GD.Print($"LA_GATE: CAMERA_RIG_CAPTURE_PARKED zone={_director.CurrentZone} mounted=World/{root.Name} — holding the composed scene for the framebuffer capture");
                    _crPhase = 99;   // park forever (helper's timeout is the watchdog)
                    break;
                }
                if (_frames - _crPhaseStart > CrTravelStepBudget)
                { CrFail($"travel step to '{_crCaptureZone}' budget exhausted at zone {_director.CurrentZone}"); break; }
                _crTravelToggle++;
                if (_crTravelToggle == 1) Input.ActionPress("travel");
                if (_crTravelToggle >= 6)   // ZmPressFrames=6 shipped press-settle window
                {
                    Input.ActionRelease("travel");
                    _crTravelToggle = 0;
                    if (++_crTravelStep > CrMaxTravels)
                    { CrFail($"never reached capture zone '{_crCaptureZone}' within one lap of the travel cycle"); break; }
                    _crPark = 0;
                    _crPhase = 10;
                }
                break;
            }

            case 10:  // park across the mount boundary (bake + cull ride)
            {
                if (++_crPark < CrParkFrames) break;
                _crPhaseStart = _frames;
                _crPhase = 9;
                break;
            }

            case 99:  // park forever: the composed scene stays live until the
                      // helper has captured it and the run's timeout reaps it.
                      // S45 MEASUREMENT (capture runs only): the leg itself —
                      // never the camera, F4-CMP — samples the live sight vs
                      // the rest sight every 30f and SCANS the ray for the
                      // first STATIC hit, so the canyon capture carries a
                      // measured occlusion-clearance row (the tallest relief).
                if (_crCaptureZone != null && _frames % 30 == 0 && _crStillRun < 6)
                {
                    _crStillRun++;
                    var cam = _crCam!; var p = _playerBody!.GlobalPosition;
                    var rest = p + cam.Offset;
                    Vector3 from = p + Vector3.Up * 0.75f;
                    Vector3 dir = rest - from; float len = dir.Length();
                    var q = PhysicsRayQueryParameters3D.Create(from, rest, uint.MaxValue);
                    if (_playerBody is PhysicsBody3D pb) q.Exclude = new Godot.Collections.Array<Rid> { pb.GetRid() };
                    var space = cam.GetWorld3D()?.DirectSpaceState;   // SceneTree has no viewport; the camera's world is the query space
                    var hit = space?.IntersectRay(q) ?? new Godot.Collections.Dictionary();
                    string hitName = "NONE"; float hitD = len;
                    if (hit.Count > 0 && hit["collider"].AsGodotObject() is GodotObject co)
                    {
                        hitName = co is Node nd ? (string)nd.Name : co.GetClass();
                        hitD = from.DistanceTo(hit["position"].AsVector3());
                    }
                    GD.Print($"LA_GATE: CAMERA_RIG_OCCL_SCAN n={_crStillRun} sight={p.DistanceTo(cam.GlobalPosition):0.###}m restSight={p.DistanceTo(rest):0.###}m firstStaticHit={hitName}@{hitD:0.###}m");
                }
                break;
        }
    }

    /// <summary>The occluder mid sits on the camera RAY (pivot + RayEyeM up
    /// toward the rest base), not on the pivot->base segment — box centre
    /// raised to cover the ray's height at the halfway distance.</summary>
    private static float RayEyMidpointM(Vector3 sight) => sight.Y * 0.5f + 0.4f;
}
