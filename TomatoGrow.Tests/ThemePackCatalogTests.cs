using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TomatoGrow.GameContent;
using Xunit;

namespace TomatoGrow.Tests;

public sealed class ThemePackCatalogTests
{
    [Fact]
    public void TomatoGrowDefaultManifest_Exists()
    {
        ThemePackCatalog catalog = TestContentFactory.CreateThemePackCatalog();

        ThemePackManifest manifest = catalog.GetManifest(ThemePackCatalog.TomatoGrowDefaultManifestId);

        Assert.Equal("tomatogrow_default", manifest.Id);
        Assert.Equal("TomatoGrow", manifest.Name);
    }

    [Fact]
    public void TomatoGrowDefaultManifest_UsesExpectedContentModules()
    {
        ThemePackCatalog catalog = TestContentFactory.CreateThemePackCatalog();
        ThemePackManifest manifest = catalog.GetManifest(ThemePackCatalog.TomatoGrowDefaultManifestId);

        Assert.Equal(["BaseGame", "FermentationPack", "PotatoPack"], catalog.GetContentModuleIdsForManifest(manifest));
    }

    [Fact]
    public void TomatoGrowDefaultManifest_ResolvesFarmingVocabularyAndInterfacePack()
    {
        ThemePackCatalog catalog = TestContentFactory.CreateThemePackCatalog();
        ThemePackManifest manifest = catalog.GetManifest(ThemePackCatalog.TomatoGrowDefaultManifestId);

        Assert.Equal(ThemeVocabulary.FarmingDefault, catalog.GetVocabularyForManifest(manifest));
        Assert.Equal(InterfacePack.FarmingDefault, catalog.GetInterfacePackForManifest(manifest));
    }

    [Fact]
    public void MiningDemoManifest_Exists()
    {
        ThemePackCatalog catalog = TestContentFactory.CreateThemePackCatalog();

        ThemePackManifest manifest = catalog.GetManifest(ThemePackCatalog.MiningDemoManifestId);

        Assert.Equal("mining_demo", manifest.Id);
        Assert.Equal("Mining Demo", manifest.Name);
    }

    [Fact]
    public void MiningDemoManifest_UsesMiningTestPackWithSharedBaseSetup()
    {
        ThemePackCatalog catalog = TestContentFactory.CreateThemePackCatalog();
        ThemePackManifest manifest = catalog.GetManifest(ThemePackCatalog.MiningDemoManifestId);

        Assert.Equal(["BaseGame", "MiningTestPack"], catalog.GetContentModuleIdsForManifest(manifest));
    }

    [Fact]
    public void MiningDemoManifest_ResolvesMiningVocabularyAndInterfacePack()
    {
        ThemePackCatalog catalog = TestContentFactory.CreateThemePackCatalog();
        ThemePackManifest manifest = catalog.GetManifest(ThemePackCatalog.MiningDemoManifestId);

        Assert.Equal(ThemeVocabulary.MiningDemo, catalog.GetVocabularyForManifest(manifest));
        Assert.Equal(InterfacePack.MiningDemo, catalog.GetInterfacePackForManifest(manifest));
    }

    [Fact]
    public void UnknownManifestId_FailsClearly()
    {
        ThemePackCatalog catalog = TestContentFactory.CreateThemePackCatalog();

        KeyNotFoundException exception = Assert.Throws<KeyNotFoundException>(() => catalog.GetManifest("ghost_manifest"));

        Assert.Equal("Unknown theme pack manifest 'ghost_manifest'.", exception.Message);
    }

    [Fact]
    public void UnknownVocabularyId_FailsClearly()
    {
        ThemePackCatalog catalog = new(
            manifests:
            [
                new ThemePackManifest
                {
                    Id = "broken_manifest",
                    Name = "Broken Manifest",
                    Description = "Broken manifest for testing.",
                    ContentModuleIds = ["BaseGame"],
                    ThemeVocabularyId = "ghost_vocabulary",
                    InterfacePackId = ThemePackCatalog.FarmingDefaultInterfacePackId
                }
            ]);

        ThemePackManifest manifest = catalog.GetManifest("broken_manifest");
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => catalog.GetVocabularyForManifest(manifest));

        Assert.Equal("Manifest 'broken_manifest' references unknown theme vocabulary 'ghost_vocabulary'.", exception.Message);
    }

    [Fact]
    public void UnknownInterfacePackId_FailsClearly()
    {
        ThemePackCatalog catalog = new(
            manifests:
            [
                new ThemePackManifest
                {
                    Id = "broken_manifest",
                    Name = "Broken Manifest",
                    Description = "Broken manifest for testing.",
                    ContentModuleIds = ["BaseGame"],
                    ThemeVocabularyId = ThemePackCatalog.FarmingDefaultVocabularyId,
                    InterfacePackId = "ghost_interface"
                }
            ]);

        ThemePackManifest manifest = catalog.GetManifest("broken_manifest");
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => catalog.GetInterfacePackForManifest(manifest));

        Assert.Equal("Manifest 'broken_manifest' references unknown interface pack 'ghost_interface'.", exception.Message);
    }

    [Fact]
    public void TomatoGrowDefaultStateCreatedFromManifest_HasFarmingStartersAndPresentation()
    {
        TurnipTestState state = TestContentFactory.CreateStateForManifest(ThemePackCatalog.TomatoGrowDefaultManifestId);

        Assert.Contains("turnip_seed", state.SeedItemIds);
        Assert.Contains("potato_seed", state.SeedItemIds);
        Assert.Contains("grape_seed", state.SeedItemIds);
        Assert.Contains("hops_seed", state.SeedItemIds);
        Assert.Equal(ThemeVocabulary.FarmingDefault, state.ThemeVocabulary);
        Assert.Equal(InterfacePack.FarmingDefault, state.InterfacePack);
    }

    [Fact]
    public void MiningDemoStateCreatedFromManifest_HasMiningStartersAndPresentation()
    {
        TurnipTestState state = TestContentFactory.CreateStateForManifest(ThemePackCatalog.MiningDemoManifestId);

        Assert.Contains("copper_marker", state.SeedItemIds);
        Assert.Contains("iron_marker", state.SeedItemIds);
        Assert.Equal(ThemeVocabulary.MiningDemo, state.ThemeVocabulary);
        Assert.Equal(InterfacePack.MiningDemo, state.InterfacePack);
    }

    [Fact]
    public void MiningDemoStateCreatedFromManifest_CanStartMaintainAndCollectTileProducer()
    {
        ThemePackCatalog catalog = TestContentFactory.CreateThemePackCatalog();
        ContentRegistry registry = TestContentFactory.CreateRegistryForManifest(ThemePackCatalog.MiningDemoManifestId, catalog);
        TurnipTestState state = TestContentFactory.CreateStateForManifest(ThemePackCatalog.MiningDemoManifestId, catalog);
        SelectSeed(state, "copper_marker");
        state.BuySeedItem("copper_marker", buyAsManyAsPossible: true);

        string startMessage = state.StartSelectedTileProducer();
        string maintainMessage = state.MaintainSelectedTile();

        for (int day = 0; day < registry.GetTileProducer("copper_ore").ProductionDays; day++)
        {
            state.AdvanceDay();
            if (day < registry.GetTileProducer("copper_ore").ProductionDays - 1)
                state.MaintainSelectedTile();
        }

        string collectMessage = state.CollectSelectedTile();

        Assert.Equal("Planted copper_ore at 0,0.", startMessage);
        Assert.False(string.IsNullOrWhiteSpace(maintainMessage));
        Assert.False(string.IsNullOrWhiteSpace(collectMessage));
        Assert.True(state.FarmGrid.GetPlot(0, 0).IsEmpty);
        Assert.True(state.GetItemCount("copper_ore") > 0);
    }

    [Fact]
    public void MiningDemoStateCreatedFromManifest_CanRunSmelterAndForge()
    {
        TurnipTestState state = TestContentFactory.CreateStateForManifest(ThemePackCatalog.MiningDemoManifestId);
        AddInventoryItem(state, "copper_ore", 3);
        state.StartProcessorMachineFromInventory("smelter");
        state.AdvanceDay();
        state.AdvanceDay();
        state.CollectProcessorMachineOutput("smelter");
        state.StartProcessorMachineFromInventory("forge");
        state.AdvanceDay();
        state.AdvanceDay();
        state.AdvanceDay();
        state.AdvanceDay();
        state.CollectProcessorMachineOutput("forge");

        Assert.Contains(state.ProcessorMachines, machine => string.Equals(machine.MachineId, "smelter", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(state.ProcessorMachines, machine => string.Equals(machine.MachineId, "forge", StringComparison.OrdinalIgnoreCase));
        Assert.True(state.GetItemCount("copper_parts") > 0);
    }

    [Fact]
    public void TomatoGrowDefaultStartupBehavior_IsUnchanged()
    {
        ContentRegistry directRegistry = new ContentModuleLoader().LoadFromModules(Path.Combine(AppContext.BaseDirectory, "ContentModules"));
        TurnipTestState directState = new(directRegistry);
        TurnipTestState manifestState = TestContentFactory.CreateStateForManifest(ThemePackCatalog.TomatoGrowDefaultManifestId);

        Assert.Equal(directState.SeedItemIds, manifestState.SeedItemIds);
        Assert.Equal(directState.ThemeVocabulary, manifestState.ThemeVocabulary);
        Assert.Equal(directState.InterfacePack, manifestState.InterfacePack);
    }

    [Fact]
    public void ThemePackDisplay_TomatoGrowDefault_ShowsFarmGridTitle()
    {
        TurnipTestState state = TestContentFactory.CreateStateForManifest(ThemePackCatalog.TomatoGrowDefaultManifestId);

        Assert.Equal("FARM GRID", DebugUiTextFormatter.GetScreenTitle(GameScreen.Farm, state.ThemeVocabulary));
    }

    [Fact]
    public void ThemePackDisplay_MiningDemo_ShowsMineGridTitleAndClaimSummary()
    {
        ThemePackCatalog catalog = TestContentFactory.CreateThemePackCatalog();
        ContentRegistry registry = TestContentFactory.CreateRegistryForManifest(ThemePackCatalog.MiningDemoManifestId, catalog);
        TurnipTestState state = TestContentFactory.CreateStateForManifest(ThemePackCatalog.MiningDemoManifestId, catalog);
        state.BuySeedItem("copper_marker", buyAsManyAsPossible: true);
        state.PlantSeedAtPlot(1, 2, "copper_marker");
        FarmPlot plot = state.FarmGrid.GetPlot(1, 2);

        string[] selectedTileLines = DebugUiTextFormatter.GetSelectedPlotSummaryLines(plot, registry, false, themeVocabulary: state.ThemeVocabulary);
        string[] controlLines = DebugUiTextFormatter.GetFarmControlLines(state.ThemeVocabulary);

        Assert.Equal("MINE GRID", DebugUiTextFormatter.GetScreenTitle(GameScreen.Farm, state.ThemeVocabulary));
        Assert.Equal("SELECTED CLAIM 1,2", selectedTileLines[0]);
        Assert.Contains(controlLines, line => line.Contains("W power", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(controlLines, line => line.Contains("H extract", StringComparison.OrdinalIgnoreCase));
    }

    private static void SelectSeed(TurnipTestState state, string seedItemId)
    {
        for (int index = 0; index < state.SeedItemIds.Count; index++)
        {
            if (string.Equals(state.SelectedSeedItemId, seedItemId, StringComparison.OrdinalIgnoreCase))
                return;

            state.SelectNextSeed();
        }

        throw new InvalidOperationException($"Could not select starter item '{seedItemId}'.");
    }

    private static void AddInventoryItem(TurnipTestState state, string itemId, int amount)
    {
        typeof(TurnipTestState)
            .GetMethod("AddItem", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .Invoke(state, [itemId, amount]);
    }
}