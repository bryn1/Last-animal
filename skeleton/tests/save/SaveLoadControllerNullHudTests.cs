using System.Collections.Generic;
using Xunit;
using LastAnimal.Combat;
using LastAnimal.Dna;
using LastAnimal.Npc;
using LastAnimal.Save;
using LastAnimal.World;

// Last Animal — MC 1405 N6 (cycle 2): the null-HUD Save/Load path, TESTED.
//
// The verify-phase audit (TEST-verdict finding 2, DA-verdict finding 6) found
// that SaveLoadController's nullable `_hud` tolerance was inspection-verified
// only: the controller is constructed exactly once in the tree
// (WorldDirector.cs, always with a real HUD) and no test exercised a no-UI
// composition. This suite constructs the controller with `hud: null` and
// proves Save() and Load() complete without an NRE — the `?.` guards are no
// longer dead tolerance.
//
// The engine seams (Hud, GodotSaveStore) are stood in by TestEngineSeams.cs —
// the real GodotSaveStore segfaults headless (see that file's header). The
// controller logic under test is the real src/world/SaveLoadController.cs.
namespace LastAnimal.Tests.Save;

public class SaveLoadControllerNullHudTests
{
    [Fact]
    public void Save_CompletesWithoutNre_WhenHudIsNull()
    {
        CleanStore();
        var controller = NewControllerWithNullHud();

        Assert.True(controller.Save(), "Save with hud:null must complete (meter falls back to 0)");
    }

    [Fact]
    public void Load_CompletesWithoutNre_WhenHudIsNull()
    {
        CleanStore();
        var controller = NewControllerWithNullHud();
        Assert.True(controller.Save(), "precondition: the save itself must succeed");

        Assert.True(controller.Load(), "Load with hud:null must complete (no meter/life to update)");
    }

    [Fact]
    public void Load_NullHud_RestoresStateAndReentersZone()
    {
        // The null-HUD load must still restore everything a load restores —
        // the HUD guards skip the meter/life writes, nothing else.
        CleanStore();
        int restoredHealth = -1;
        var entered = new List<string>();
        var companion = new CompanionComponent();
        companion.SetCompanion(7);
        companion.ModifyLoyalty(20);
        var controller = new SaveLoadController(
            new List<LanguageSignature>(), companion, hud: null,
            currentZone: () => "meadow",
            enterZone: zoneId => entered.Add(zoneId),
            playerHealth: () => 3,
            restoreHealth: h => restoredHealth = h,
            playerPosition: () => new CombatVec3(4.5f, 0f, -2.25f),
            restorePosition: p => { });

        Assert.True(controller.Save());
        companion.CompanionEntityId = -1;
        companion.Loyalty = 0;
        Assert.True(controller.Load());

        Assert.Equal(7, companion.CompanionEntityId);
        Assert.Equal(70, companion.Loyalty); // SetCompanion seeds 50, +20 saved, restored after reset
        Assert.Equal(3, restoredHealth);
        Assert.Equal(new List<string> { "meadow" }, entered);
        Assert.Equal(0, controller.Progression); // seeded from the loaded save (meadow, 0 chapters)
    }

    // ------------------------------------------------------------------

    private static SaveLoadController NewControllerWithNullHud()
    {
        return new SaveLoadController(
            new List<LanguageSignature>(), new CompanionComponent(), hud: null,
            currentZone: () => "meadow",
            enterZone: _ => { },
            playerHealth: () => 3,
            restoreHealth: _ => { });
    }

    private static void CleanStore()
    {
        if (System.IO.File.Exists(GodotSaveStore.ControllerSavePath))
            System.IO.File.Delete(GodotSaveStore.ControllerSavePath);
    }
}
