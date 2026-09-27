// Last Animal — MC 1405 N6 (cycle 2): test-only stand-ins for the two engine
// seams SaveLoadController hardwires.
//
// WHY THIS EXISTS: SaveLoadController.Save()/Load() construct a
// GodotSaveStore internally, and GodotSaveStore calls
// ProjectSettings.GlobalizePath — a Godot native API that SEGFAULTS outside
// the engine (verified this session: a plain dotnet console probe calling it
// died with SIGSEGV, exit 139). The headless save test project therefore
// cannot compile the real seam, and without a stand-in the controller cannot
// be constructed at all — which is exactly why the null-HUD path (N6) had no
// test. These stand-ins mirror the codebase's engine-free store pattern
// (TempDirSaveStore): same fully-qualified names as the engine types, but
// writing to the OS temp dir, so the controller's REAL logic — including the
// `_hud?.` guards this run exists to test — runs headless.
//
// The Hud stand-in carries only the three members the controller touches
// (DnaMeter, UpdateDnaMeter, UpdateLife); it is not the real UI class and
// must not grow UI behaviour. Compiled ONLY by LastAnimalSaveTests.csproj.

namespace LastAnimal.Ui
{
    /// <summary>Test stand-in for the engine Hud (src/ui/Hud.cs): surface only.</summary>
    public class Hud
    {
        public int DnaMeter { get; set; }
        public void UpdateDnaMeter(int value) { }
        public void UpdateLife(int value) { }
    }
}

namespace LastAnimal.Save
{
    /// <summary>
    /// Test stand-in for GodotSaveStore: same ISaveStore contract, but SavePath
    /// is a deterministic file under the OS temp dir (never the repo tree), so
    /// Save() and Load() — which each construct their own store instance — meet
    /// on the same path.
    /// </summary>
    public class GodotSaveStore : ISaveStore
    {
        private static readonly string Dir =
            System.IO.Path.Combine(System.IO.Path.GetTempPath(), "lastanimal-savecontroller-tests");

        public static string ControllerSavePath =>
            System.IO.Path.Combine(Dir, SaveSystem.SaveFileName);

        public string SavePath => ControllerSavePath;

        public void WriteAllText(string path, string contents)
        {
            System.IO.Directory.CreateDirectory(Dir);
            System.IO.File.WriteAllText(path, contents);
        }

        public string ReadAllText(string path) => System.IO.File.ReadAllText(path);

        public bool Exists(string path) => System.IO.File.Exists(path);
    }
}
